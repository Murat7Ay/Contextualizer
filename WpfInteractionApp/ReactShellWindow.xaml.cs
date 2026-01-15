using Contextualizer.Core;
using Contextualizer.PluginContracts;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using WpfInteractionApp.Services;

namespace WpfInteractionApp
{
    public partial class ReactShellWindow : Window, IDisposable
    {
        private const int UiProtocolVersion = 1;
        private const string VirtualHostName = "contextualizer-ui.local";

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        private bool _disposed;
        private bool _webViewInitialized;
        private string? _lastThemeSentToUi;
        private readonly TaskCompletionSource<bool> _uiReady = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _pendingConfirms = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, TaskCompletionSource<UserInputResponse>> _pendingUserInputs = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, TaskCompletionSource<NavigationResult>> _pendingNavInputs = new(StringComparer.Ordinal);
        // Store actions for tabs: tabId -> List<(actionId, label, callback)>
        private readonly ConcurrentDictionary<string, List<(string actionId, string label, Action<Dictionary<string, string>> callback)>> _tabActions = new(StringComparer.Ordinal);
        // Store actions for toasts: toastId -> List<(actionId, label, style, closeOnClick, isDefaultAction, callback)>
        private readonly ConcurrentDictionary<string, List<(string actionId, string label, ToastActionStyle style, bool closeOnClick, bool isDefaultAction, Action callback)>> _toastActions = new(StringComparer.Ordinal);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr SetActiveWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr SetFocus(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential)]
        private struct FLASHWINFO
        {
            public uint cbSize;
            public IntPtr hwnd;
            public uint dwFlags;
            public uint uCount;
            public uint dwTimeout;
        }

        private const uint FLASHW_ALL = 3;
        private const uint FLASHW_TIMERNOFG = 12;
        private const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        private void BringToFrontSafe()
        {
            if (!Dispatcher.CheckAccess())
            {
                try { Dispatcher.Invoke(BringToFrontSafe); } catch { /* ignore */ }
                return;
            }

            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd == IntPtr.Zero)
                    return;

                if (WindowState == WindowState.Minimized)
                    WindowState = WindowState.Normal;

                if (!IsVisible)
                    Show();

                // Restore (Win32) as well; some window styles ignore WindowState changes.
                try { ShowWindowAsync(hwnd, SW_RESTORE); } catch { /* ignore */ }

                // Try hard to get foreground activation. Windows may block focus stealing, so we also attach input
                // to the current foreground thread as a best-effort workaround.
                bool attached = false;
                uint fgThread = 0;
                uint thisThread = 0;
                try
                {
                    var fg = GetForegroundWindow();
                    fgThread = fg != IntPtr.Zero ? GetWindowThreadProcessId(fg, out _) : 0;
                    thisThread = GetCurrentThreadId();
                    if (fgThread != 0 && thisThread != 0 && fgThread != thisThread)
                    {
                        attached = AttachThreadInput(thisThread, fgThread, true);
                    }
                }
                catch { /* ignore */ }

                try { BringWindowToTop(hwnd); } catch { /* ignore */ }
                try { SetForegroundWindow(hwnd); } catch { /* ignore */ }
                try { SetActiveWindow(hwnd); } catch { /* ignore */ }
                try { SetFocus(hwnd); } catch { /* ignore */ }

                try
                {
                    Activate();

                    // Keep Topmost for a dispatcher tick; reverting immediately is sometimes ignored.
                    var wasTopmost = Topmost;
                    Topmost = true;
                    Dispatcher.BeginInvoke(new Action(() => Topmost = wasTopmost));

                    Focus();
                    WebView?.Focus();
                }
                catch { /* ignore */ }

                if (attached)
                {
                    try { AttachThreadInput(thisThread, fgThread, false); } catch { /* ignore */ }
                }

                // If Windows refuses to foreground-focus (focus-stealing rules), at least flash the taskbar button.
                if (!IsActive)
                {
                    try
                    {
                        var fi = new FLASHWINFO
                        {
                            cbSize = (uint)Marshal.SizeOf(typeof(FLASHWINFO)),
                            hwnd = hwnd,
                            dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG,
                            uCount = 3,
                            dwTimeout = 0
                        };
                        FlashWindowEx(ref fi);
                    }
                    catch { /* ignore */ }
                }
            }
            catch { /* ignore */ }
        }

        public ReactShellWindow()
        {
            InitializeComponent();
            Loaded += ReactShellWindow_Loaded;
            Closed += ReactShellWindow_Closed;

            ThemeManager.Instance.ThemeChanged += ThemeManager_ThemeChanged;
        }

        private async void ReactShellWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeWebViewAsync();
        }

        private void ReactShellWindow_Closed(object? sender, EventArgs e)
        {
            Dispose();
        }

        private async Task InitializeWebViewAsync()
        {
            if (_webViewInitialized)
                return;

            try
            {
                // Use a stable user data folder so WebView2 profile persists across runs.
                var userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Contextualizer",
                    "WebView2");

                Directory.CreateDirectory(userDataFolder);

                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await WebView.EnsureCoreWebView2Async(env);

                _webViewInitialized = true;

                WebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

                // Optional: keep DevTools available in Debug builds.
#if DEBUG
                WebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
#endif

                NavigateToUi();

                // Send a startup log early so the UI shows host activity.
                PostLogToUi(LogType.Info, "WPF host initialized");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ReactShellWindow.InitializeWebViewAsync error: {ex}");
                ShowShellError(
                    "WebView2 initialization failed",
                    "Contextualizer UI could not be started because WebView2 failed to initialize.",
                    ex);
            }
        }

        private void ShowShellError(string title, string message, Exception ex)
        {
            try
            {
                ErrorTitleText.Text = title;
                ErrorMessageText.Text = message;
                ErrorDetailsText.Text = ex.ToString();
                ErrorOverlay.Visibility = Visibility.Visible;
            }
            catch
            {
                // ignore
            }
        }

        private void NavigateToUi()
        {
            if (WebView.CoreWebView2 == null)
                return;

            var distFolder = ResolveUiDistFolder();
            if (string.IsNullOrWhiteSpace(distFolder) || !File.Exists(Path.Combine(distFolder, "index.html")))
            {
                var appDir = GetAppDirectory();
                var expected = Path.Combine(appDir, "Assets", "Ui", "dist", "index.html");
                var html = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8' />
  <title>Contextualizer UI Missing</title>
  <style>
    body {{ font-family: 'Segoe UI', system-ui, sans-serif; margin: 0; background: #0b0b0b; color: #f4f4f4; }}
    .wrap {{ max-width: 900px; margin: 0 auto; padding: 48px 24px; }}
    .card {{ background: #161616; border: 1px solid #262626; border-radius: 12px; padding: 24px; }}
    h1 {{ margin: 0 0 8px; font-size: 22px; }}
    p {{ margin: 0 0 12px; line-height: 1.5; color: #c6c6c6; }}
    code, pre {{ font-family: Consolas, 'IBM Plex Mono', monospace; }}
    pre {{ background: #0f0f0f; border: 1px solid #262626; padding: 12px; border-radius: 8px; overflow: auto; }}
  </style>
</head>
<body>
  <div class='wrap'>
    <div class='card'>
      <h1>React UI build not found</h1>
      <p>Contextualizer is configured to run with a packaged React UI only.</p>
      <p>Please ensure the UI build exists at:</p>
      <pre>{expected}</pre>
      <p>Build the UI and package it into <code>Assets\\Ui\\dist</code> (build-release.ps1 already does this).</p>
    </div>
  </div>
</body>
</html>";

                WebView.NavigateToString(html);
                return;
            }

            WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                VirtualHostName,
                distFolder,
                CoreWebView2HostResourceAccessKind.Allow);

            // Cache-bust index.html so WebView2 doesn't keep an older UI build in its disk cache.
            // This is important because index.html controls which hashed JS bundle is loaded.
            long stamp = 0;
            try
            {
                var indexPath = Path.Combine(distFolder, "index.html");
                stamp = File.Exists(indexPath) ? new FileInfo(indexPath).LastWriteTimeUtc.Ticks : DateTime.UtcNow.Ticks;
            }
            catch
            {
                stamp = DateTime.UtcNow.Ticks;
            }

            WebView.Source = new Uri($"https://{VirtualHostName}/index.html?v={stamp}");
            PostLogToUi(LogType.Info, $"UI loaded from Assets dist: {distFolder} (v={stamp})");
        }

        private static string? ResolveUiDistFolder()
        {
            // Packaged build output shipped next to the executable (preferred).
            var baseDir = GetAppDirectory();
            var candidates = new[]
            {
                Path.Combine(baseDir, "Assets", "Ui", "dist"),
                Path.Combine(baseDir, "Assets", "ui", "dist"),
            };

            foreach (var folder in candidates)
            {
                if (File.Exists(Path.Combine(folder, "index.html")))
                {
                    return folder;
                }
            }

            return null;
        }

        private static string GetAppDirectory()
        {
            try
            {
                var processPath = Environment.ProcessPath;
                if (!string.IsNullOrWhiteSpace(processPath))
                {
                    var dir = Path.GetDirectoryName(processPath);
                    if (!string.IsNullOrWhiteSpace(dir))
                        return dir;
                }
            }
            catch
            {
                // ignore
            }

            try
            {
                var fileName = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    var dir = Path.GetDirectoryName(fileName);
                    if (!string.IsNullOrWhiteSpace(dir))
                        return dir;
                }
            }
            catch
            {
                // ignore
            }

            return AppDomain.CurrentDomain.BaseDirectory;
        }

        private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                // JS posts stringified JSON (see `Contextualizer.UI/src/app/host/webview2Bridge.ts`).
                // WebMessageAsJson is always JSON; if the message is a string, the root value kind will be String.
                var rawJson = e.WebMessageAsJson;
                using var doc = JsonDocument.Parse(rawJson);

                var payloadJson = doc.RootElement.ValueKind == JsonValueKind.String
                    ? (doc.RootElement.GetString() ?? string.Empty)
                    : doc.RootElement.GetRawText();

                if (string.IsNullOrWhiteSpace(payloadJson))
                    return;

                using var payloadDoc = JsonDocument.Parse(payloadJson);
                if (payloadDoc.RootElement.ValueKind != JsonValueKind.Object)
                    return;

                var root = payloadDoc.RootElement;
                var type = root.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : null;
                if (string.IsNullOrWhiteSpace(type))
                    return;

                switch (type)
                {
                    case "ui_ready":
                        _uiReady.TrySetResult(true);
                        SendHostReady();
                        break;

                    case "ping":
                        SendPong(root);
                        break;

                    case "set_theme":
                        HandleSetTheme(root);
                        break;

                    case "ui_confirm_response":
                        HandleConfirmResponse(root);
                        break;

                    case "ui_user_input_response":
                        HandleUserInputResponse(root);
                        break;

                    case "ui_user_input_navigation_response":
                        HandleNavigationInputResponse(root);
                        break;

                    case "ui_open_file_dialog_request":
                        HandleOpenFileDialogRequest(root);
                        break;

                    case "open_external":
                        HandleOpenExternal(root);
                        break;

                    case "handlers_list_request":
                        HandleHandlersListRequest();
                        break;

                    case "handler_set_enabled":
                        HandleHandlerSetEnabled(root);
                        break;

                    case "handler_set_mcp":
                        HandleHandlerSetMcp(root);
                        break;

                    case "handlers_reload":
                        HandleHandlersReload(root);
                        break;

                    case "manual_handler_execute":
                        HandleManualHandlerExecute(root);
                        break;

                    case "cron_list_request":
                        HandleCronListRequest();
                        break;

                    case "cron_set_enabled":
                        HandleCronSetEnabled(root);
                        break;

                    case "cron_trigger":
                        HandleCronTrigger(root);
                        break;

                    case "cron_update":
                        HandleCronUpdate(root);
                        break;

                    case "app_settings_request":
                        HandleAppSettingsRequest();
                        break;

                    case "app_settings_save":
                        HandleAppSettingsSave(root);
                        break;

                    case "ui_open_folder_dialog_request":
                        HandleOpenFolderDialogRequest(root);
                        break;

                    case "logging_test_request":
                        HandleLoggingTestRequest();
                        break;

                    case "usage_test_request":
                        _ = HandleUsageTestRequestAsync();
                        break;

                    case "log_clear_request":
                        HandleClearLogsRequest(root);
                        break;

                    case "tab_action_execute":
                        HandleTabActionExecute(root);
                        break;

                    case "tab_closed":
                        HandleTabClosed(root);
                        break;

                    case "toast_action_execute":
                        HandleToastActionExecute(root);
                        break;

                    case "toast_closed":
                        HandleToastClosed(root);
                        break;

                    // ─────────────────────────────────────────────────────────────────
                    // Handler Exchange / Marketplace
                    // ─────────────────────────────────────────────────────────────────
                    case "exchange_list_request":
                        _ = HandleExchangeListRequestAsync(root);
                        break;

                    case "exchange_tags_request":
                        _ = HandleExchangeTagsRequestAsync();
                        break;

                    case "exchange_details_request":
                        _ = HandleExchangeDetailsRequestAsync(root);
                        break;

                    case "exchange_install":
                        _ = HandleExchangeInstallAsync(root);
                        break;

                    case "exchange_update":
                        _ = HandleExchangeUpdateAsync(root);
                        break;

                    case "exchange_remove":
                        _ = HandleExchangeRemoveAsync(root);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ReactShellWindow message handling error: {ex}");
            }
        }

        private void HandleCronUpdate(JsonElement root)
        {
            try
            {
                if (!root.TryGetProperty("jobId", out var jobIdProp) || jobIdProp.ValueKind != JsonValueKind.String)
                    return;
                if (!root.TryGetProperty("cronExpression", out var cronProp) || cronProp.ValueKind != JsonValueKind.String)
                    return;

                var jobId = jobIdProp.GetString() ?? string.Empty;
                var cronExpression = cronProp.GetString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(jobId) || string.IsNullOrWhiteSpace(cronExpression))
                    return;

                string? timezone = null;
                if (root.TryGetProperty("timezone", out var tzProp) && tzProp.ValueKind == JsonValueKind.String)
                    timezone = tzProp.GetString();

                var cron = ServiceLocator.SafeGet<ICronService>();
                if (cron == null)
                {
                    PostToUi(new { type = "cron_update_result", jobId, success = false, error = "Cron service not available" });
                    return;
                }

                if (!cron.ValidateCronExpression(cronExpression))
                {
                    PostToUi(new { type = "cron_update_result", jobId, success = false, error = "Invalid cron expression" });
                    return;
                }

                var info = cron.GetJobInfo(jobId);
                if (info == null || info.HandlerConfig == null)
                {
                    PostToUi(new { type = "cron_update_result", jobId, success = false, error = "Cron job not found" });
                    return;
                }

                var effectiveTz = string.IsNullOrWhiteSpace(timezone) ? info.Timezone : timezone;
                var ok = cron.UpdateJob(jobId, cronExpression, info.HandlerConfig, effectiveTz);
                PostToUi(new { type = "cron_update_result", jobId, success = ok, error = ok ? null : "Failed to update cron job" });

                PostToastToUi(ok ? LogType.Success : LogType.Error,
                    ok ? $"Cron job '{jobId}' updated" : $"Failed to update cron job '{jobId}'",
                    "Cron",
                    6);

                if (ok)
                {
                    // Refresh the cron list so UI reflects updated expression immediately
                    HandleCronListRequest();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"HandleCronUpdate error: {ex}");
                try
                {
                    PostToastToUi(LogType.Error, "Failed to update cron job", "Cron", 8, ex.Message);
                }
                catch { /* ignore */ }
            }
        }

        private void HandleAppSettingsRequest()
        {
            try
            {
                var s = ServiceLocator.SafeGet<Services.SettingsService>()?.Settings;
                if (s == null)
                {
                    PostToUi(new { type = "app_settings", settings = (object?)null, error = "Settings service not available" });
                    return;
                }

                PostToUi(new
                {
                    type = "app_settings",
                    settings = new
                    {
                        handlersFilePath = s.HandlersFilePath,
                        pluginsDirectory = s.PluginsDirectory,
                        exchangeDirectory = s.ExchangeDirectory,
                        keyboardShortcut = new
                        {
                            modifierKeys = s.KeyboardShortcut?.ModifierKeys ?? Array.Empty<string>(),
                            key = s.KeyboardShortcut?.Key ?? "W"
                        },
                        clipboardWaitTimeout = s.ClipboardWaitTimeout,
                        windowActivationDelay = s.WindowActivationDelay,
                        clipboardClearDelay = s.ClipboardClearDelay,
                        configSystem = new
                        {
                            enabled = s.ConfigSystem?.Enabled ?? true,
                            configFilePath = s.ConfigSystem?.ConfigFilePath,
                            secretsFilePath = s.ConfigSystem?.SecretsFilePath,
                            autoCreateFiles = s.ConfigSystem?.AutoCreateFiles ?? true,
                            fileFormat = s.ConfigSystem?.FileFormat ?? "ini"
                        },
                        uiSettings = new
                        {
                            toastPositionX = s.UISettings?.ToastPositionX ?? 0,
                            toastPositionY = s.UISettings?.ToastPositionY ?? 0,
                            theme = MapWpfThemeToUi(s.UISettings?.Theme ?? ThemeManager.Instance.CurrentTheme),
                            skippedUpdateVersion = s.UISettings?.SkippedUpdateVersion,
                            lastUpdateCheck = s.UISettings?.LastUpdateCheck,
                            networkUpdateSettings = new
                            {
                                enableNetworkUpdates = s.UISettings?.NetworkUpdateSettings?.EnableNetworkUpdates ?? true,
                                networkUpdatePath = s.UISettings?.NetworkUpdateSettings?.NetworkUpdatePath,
                                updateScriptPath = s.UISettings?.NetworkUpdateSettings?.UpdateScriptPath,
                                checkIntervalHours = s.UISettings?.NetworkUpdateSettings?.CheckIntervalHours ?? 24,
                                autoInstallNonMandatory = s.UISettings?.NetworkUpdateSettings?.AutoInstallNonMandatory ?? false,
                                autoInstallMandatory = s.UISettings?.NetworkUpdateSettings?.AutoInstallMandatory ?? true,
                            },
                            initialDeploymentSettings = new
                            {
                                enabled = s.UISettings?.InitialDeploymentSettings?.Enabled ?? true,
                                sourcePath = s.UISettings?.InitialDeploymentSettings?.SourcePath,
                                isCompleted = s.UISettings?.InitialDeploymentSettings?.IsCompleted ?? false,
                                copyExchangeHandlers = s.UISettings?.InitialDeploymentSettings?.CopyExchangeHandlers ?? true,
                                copyInstalledHandlers = s.UISettings?.InitialDeploymentSettings?.CopyInstalledHandlers ?? true,
                                copyPlugins = s.UISettings?.InitialDeploymentSettings?.CopyPlugins ?? true,
                            }
                        },
                        loggingSettings = new
                        {
                            enableLocalLogging = s.LoggingSettings?.EnableLocalLogging ?? true,
                            enableUsageTracking = s.LoggingSettings?.EnableUsageTracking ?? true,
                            localLogPath = s.LoggingSettings?.LocalLogPath,
                            usageEndpointUrl = s.LoggingSettings?.UsageEndpointUrl,
                            minimumLogLevel = (s.LoggingSettings?.MinimumLogLevel ?? LogLevel.Info).ToString(),
                            maxLogFileSizeMB = s.LoggingSettings?.MaxLogFileSizeMB ?? 10,
                            maxLogFileCount = s.LoggingSettings?.MaxLogFileCount ?? 5,
                            enableDebugMode = s.LoggingSettings?.EnableDebugMode ?? false
                        },
                        mcpSettings = new
                        {
                            enabled = s.McpSettings?.Enabled ?? false,
                            port = s.McpSettings?.Port ?? 5000
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                PostToUi(new { type = "app_settings", settings = (object?)null, error = ex.Message });
            }
        }

        private void HandleAppSettingsSave(JsonElement root)
        {
            if (!root.TryGetProperty("settings", out var settingsEl) || settingsEl.ValueKind != JsonValueKind.Object)
            {
                PostToUi(new { type = "app_settings_saved", ok = false, error = "Missing settings" });
                return;
            }

            var settingsService = ServiceLocator.SafeGet<Services.SettingsService>();
            if (settingsService?.Settings == null)
            {
                PostToUi(new { type = "app_settings_saved", ok = false, error = "Settings service not available" });
                return;
            }

            try
            {
                var s = settingsService.Settings;

                if (settingsEl.TryGetProperty("handlersFilePath", out var hfp) && hfp.ValueKind == JsonValueKind.String)
                    s.HandlersFilePath = hfp.GetString() ?? s.HandlersFilePath;
                if (settingsEl.TryGetProperty("pluginsDirectory", out var pd) && pd.ValueKind == JsonValueKind.String)
                    s.PluginsDirectory = pd.GetString() ?? s.PluginsDirectory;
                if (settingsEl.TryGetProperty("exchangeDirectory", out var ed) && ed.ValueKind == JsonValueKind.String)
                    s.ExchangeDirectory = ed.GetString() ?? s.ExchangeDirectory;

                if (settingsEl.TryGetProperty("keyboardShortcut", out var ks) && ks.ValueKind == JsonValueKind.Object)
                {
                    if (s.KeyboardShortcut == null) s.KeyboardShortcut = new Settings.KeyboardShortcut();
                    if (ks.TryGetProperty("key", out var k) && k.ValueKind == JsonValueKind.String)
                        s.KeyboardShortcut.Key = (k.GetString() ?? s.KeyboardShortcut.Key).Trim();
                    if (ks.TryGetProperty("modifierKeys", out var mods) && mods.ValueKind == JsonValueKind.Array)
                    {
                        var list = new List<string>();
                        foreach (var m in mods.EnumerateArray())
                        {
                            if (m.ValueKind == JsonValueKind.String)
                                list.Add(m.GetString() ?? string.Empty);
                        }
                        s.KeyboardShortcut.ModifierKeys = list.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
                    }
                }

                if (settingsEl.TryGetProperty("clipboardWaitTimeout", out var cwt) && cwt.TryGetInt32(out var cwtVal))
                    s.ClipboardWaitTimeout = cwtVal;
                if (settingsEl.TryGetProperty("windowActivationDelay", out var wad) && wad.TryGetInt32(out var wadVal))
                    s.WindowActivationDelay = wadVal;
                if (settingsEl.TryGetProperty("clipboardClearDelay", out var ccd) && ccd.TryGetInt32(out var ccdVal))
                    s.ClipboardClearDelay = ccdVal;

                if (settingsEl.TryGetProperty("configSystem", out var cs) && cs.ValueKind == JsonValueKind.Object)
                {
                    if (s.ConfigSystem == null) s.ConfigSystem = new Settings.ConfigSystemSettings();
                    if (cs.TryGetProperty("enabled", out var csEnabled) && csEnabled.ValueKind is JsonValueKind.True or JsonValueKind.False)
                        s.ConfigSystem.Enabled = csEnabled.ValueKind == JsonValueKind.True;
                    if (cs.TryGetProperty("configFilePath", out var cfp) && cfp.ValueKind == JsonValueKind.String)
                        s.ConfigSystem.ConfigFilePath = cfp.GetString() ?? s.ConfigSystem.ConfigFilePath;
                    if (cs.TryGetProperty("secretsFilePath", out var sfp) && sfp.ValueKind == JsonValueKind.String)
                        s.ConfigSystem.SecretsFilePath = sfp.GetString() ?? s.ConfigSystem.SecretsFilePath;
                    if (cs.TryGetProperty("autoCreateFiles", out var acf) && acf.ValueKind is JsonValueKind.True or JsonValueKind.False)
                        s.ConfigSystem.AutoCreateFiles = acf.ValueKind == JsonValueKind.True;
                    if (cs.TryGetProperty("fileFormat", out var ff) && ff.ValueKind == JsonValueKind.String)
                        s.ConfigSystem.FileFormat = ff.GetString() ?? s.ConfigSystem.FileFormat;
                }

                if (settingsEl.TryGetProperty("uiSettings", out var ui) && ui.ValueKind == JsonValueKind.Object)
                {
                    if (s.UISettings == null) s.UISettings = new Settings.UISettings();
                    if (ui.TryGetProperty("toastPositionX", out var tpx) && tpx.TryGetDouble(out var tpxVal))
                        s.UISettings.ToastPositionX = tpxVal;
                    if (ui.TryGetProperty("toastPositionY", out var tpy) && tpy.TryGetDouble(out var tpyVal))
                        s.UISettings.ToastPositionY = tpyVal;

                    if (ui.TryGetProperty("theme", out var themeEl) && themeEl.ValueKind == JsonValueKind.String)
                    {
                        var uiTheme = (themeEl.GetString() ?? string.Empty).Trim().ToLowerInvariant();
                        var wpfTheme = MapUiThemeToWpf(uiTheme);
                        if (!string.IsNullOrWhiteSpace(wpfTheme))
                        {
                            // Persist to settings so it survives restart (App.xaml.cs applies Settings.UISettings.Theme on startup).
                            s.UISettings.Theme = wpfTheme;
                            ThemeManager.Instance.ApplyTheme(wpfTheme);
                        }
                    }

                    if (ui.TryGetProperty("networkUpdateSettings", out var nus) && nus.ValueKind == JsonValueKind.Object)
                    {
                        s.UISettings.NetworkUpdateSettings ??= new Settings.NetworkUpdateSettings();
                        if (nus.TryGetProperty("enableNetworkUpdates", out var enu) && enu.ValueKind is JsonValueKind.True or JsonValueKind.False)
                            s.UISettings.NetworkUpdateSettings.EnableNetworkUpdates = enu.ValueKind == JsonValueKind.True;
                        if (nus.TryGetProperty("networkUpdatePath", out var nup) && nup.ValueKind == JsonValueKind.String)
                            s.UISettings.NetworkUpdateSettings.NetworkUpdatePath = nup.GetString() ?? s.UISettings.NetworkUpdateSettings.NetworkUpdatePath;
                        if (nus.TryGetProperty("updateScriptPath", out var usp) && usp.ValueKind == JsonValueKind.String)
                            s.UISettings.NetworkUpdateSettings.UpdateScriptPath = usp.GetString() ?? s.UISettings.NetworkUpdateSettings.UpdateScriptPath;
                        if (nus.TryGetProperty("checkIntervalHours", out var cih) && cih.TryGetInt32(out var cihVal))
                            s.UISettings.NetworkUpdateSettings.CheckIntervalHours = cihVal;
                        if (nus.TryGetProperty("autoInstallNonMandatory", out var ainm) && ainm.ValueKind is JsonValueKind.True or JsonValueKind.False)
                            s.UISettings.NetworkUpdateSettings.AutoInstallNonMandatory = ainm.ValueKind == JsonValueKind.True;
                        if (nus.TryGetProperty("autoInstallMandatory", out var aim) && aim.ValueKind is JsonValueKind.True or JsonValueKind.False)
                            s.UISettings.NetworkUpdateSettings.AutoInstallMandatory = aim.ValueKind == JsonValueKind.True;
                    }

                    if (ui.TryGetProperty("initialDeploymentSettings", out var ids) && ids.ValueKind == JsonValueKind.Object)
                    {
                        s.UISettings.InitialDeploymentSettings ??= new Settings.InitialDeploymentSettings();
                        if (ids.TryGetProperty("enabled", out var ide) && ide.ValueKind is JsonValueKind.True or JsonValueKind.False)
                            s.UISettings.InitialDeploymentSettings.Enabled = ide.ValueKind == JsonValueKind.True;
                        if (ids.TryGetProperty("sourcePath", out var sp) && sp.ValueKind == JsonValueKind.String)
                            s.UISettings.InitialDeploymentSettings.SourcePath = sp.GetString() ?? s.UISettings.InitialDeploymentSettings.SourcePath;
                        if (ids.TryGetProperty("isCompleted", out var ic) && ic.ValueKind is JsonValueKind.True or JsonValueKind.False)
                            s.UISettings.InitialDeploymentSettings.IsCompleted = ic.ValueKind == JsonValueKind.True;
                        if (ids.TryGetProperty("copyExchangeHandlers", out var ceh) && ceh.ValueKind is JsonValueKind.True or JsonValueKind.False)
                            s.UISettings.InitialDeploymentSettings.CopyExchangeHandlers = ceh.ValueKind == JsonValueKind.True;
                        if (ids.TryGetProperty("copyInstalledHandlers", out var cih2) && cih2.ValueKind is JsonValueKind.True or JsonValueKind.False)
                            s.UISettings.InitialDeploymentSettings.CopyInstalledHandlers = cih2.ValueKind == JsonValueKind.True;
                        if (ids.TryGetProperty("copyPlugins", out var cp) && cp.ValueKind is JsonValueKind.True or JsonValueKind.False)
                            s.UISettings.InitialDeploymentSettings.CopyPlugins = cp.ValueKind == JsonValueKind.True;
                    }
                }

                if (settingsEl.TryGetProperty("loggingSettings", out var ls) && ls.ValueKind == JsonValueKind.Object)
                {
                    s.LoggingSettings ??= new Settings.LoggingSettings();
                    if (ls.TryGetProperty("enableLocalLogging", out var ell) && ell.ValueKind is JsonValueKind.True or JsonValueKind.False)
                        s.LoggingSettings.EnableLocalLogging = ell.ValueKind == JsonValueKind.True;
                    if (ls.TryGetProperty("enableUsageTracking", out var eut) && eut.ValueKind is JsonValueKind.True or JsonValueKind.False)
                        s.LoggingSettings.EnableUsageTracking = eut.ValueKind == JsonValueKind.True;
                    if (ls.TryGetProperty("localLogPath", out var llp) && llp.ValueKind == JsonValueKind.String)
                        s.LoggingSettings.LocalLogPath = llp.GetString() ?? s.LoggingSettings.LocalLogPath;
                    if (ls.TryGetProperty("usageEndpointUrl", out var ue) && ue.ValueKind == JsonValueKind.String)
                        s.LoggingSettings.UsageEndpointUrl = ue.GetString();
                    if (ls.TryGetProperty("minimumLogLevel", out var mll) && mll.ValueKind == JsonValueKind.String)
                    {
                        if (Enum.TryParse<LogLevel>(mll.GetString(), true, out var lvl))
                            s.LoggingSettings.MinimumLogLevel = lvl;
                    }
                    if (ls.TryGetProperty("maxLogFileSizeMB", out var mfs) && mfs.TryGetInt32(out var mfsVal))
                        s.LoggingSettings.MaxLogFileSizeMB = mfsVal;
                    if (ls.TryGetProperty("maxLogFileCount", out var mfc) && mfc.TryGetInt32(out var mfcVal))
                        s.LoggingSettings.MaxLogFileCount = mfcVal;
                    if (ls.TryGetProperty("enableDebugMode", out var edm) && edm.ValueKind is JsonValueKind.True or JsonValueKind.False)
                        s.LoggingSettings.EnableDebugMode = edm.ValueKind == JsonValueKind.True;

                    // Apply to runtime logging service
                    try
                    {
                        var log = ServiceLocator.SafeGet<ILoggingService>();
                        log?.SetConfiguration(s.LoggingSettings.ToLoggingConfiguration());
                    }
                    catch { /* ignore */ }
                }

                if (settingsEl.TryGetProperty("mcpSettings", out var mcp) && mcp.ValueKind == JsonValueKind.Object)
                {
                    s.McpSettings ??= new Settings.McpSettings();
                    if (mcp.TryGetProperty("enabled", out var me) && me.ValueKind is JsonValueKind.True or JsonValueKind.False)
                        s.McpSettings.Enabled = me.ValueKind == JsonValueKind.True;
                    if (mcp.TryGetProperty("port", out var mp) && mp.TryGetInt32(out var mpVal))
                        s.McpSettings.Port = mpVal;
                }

                settingsService.SaveSettings();

                PostToUi(new { type = "app_settings_saved", ok = true });
                // Send fresh settings back so UI stays in sync
                HandleAppSettingsRequest();
            }
            catch (Exception ex)
            {
                PostToUi(new { type = "app_settings_saved", ok = false, error = ex.Message });
            }
        }

        private void HandleOpenFolderDialogRequest(JsonElement root)
        {
            if (!TryGetString(root, "requestId", out var requestId))
                return;

            string title = root.TryGetProperty("title", out var tEl) && tEl.ValueKind == JsonValueKind.String ? (tEl.GetString() ?? "Select Folder") : "Select Folder";
            string? initialPath = root.TryGetProperty("initialPath", out var pEl) && pEl.ValueKind == JsonValueKind.String ? pEl.GetString() : null;

            try
            {
                using var dialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = title,
                    UseDescriptionForTitle = true,
                    SelectedPath = !string.IsNullOrWhiteSpace(initialPath) && Directory.Exists(initialPath) ? initialPath : string.Empty,
                    ShowNewFolderButton = true
                };

                var ok = dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK;
                if (!ok)
                {
                    PostToUi(new { type = "ui_open_folder_dialog_response", requestId, cancelled = true });
                    return;
                }

                PostToUi(new { type = "ui_open_folder_dialog_response", requestId, cancelled = false, path = dialog.SelectedPath });
            }
            catch (Exception ex)
            {
                PostToUi(new { type = "ui_open_folder_dialog_response", requestId, cancelled = true, error = ex.Message });
            }
        }

        private void HandleLoggingTestRequest()
        {
            try
            {
                var log = ServiceLocator.SafeGet<ILoggingService>();
                log?.LogError("Test error log message", new Exception("This is a test exception"));
                log?.LogWarning("Test warning log message");
                log?.LogInfo("Test info log message");
                log?.LogDebug("Test debug log message");
                PostToastToUi(LogType.Success, "Test log messages written. Check the log folder.", "Logging", 6);
            }
            catch (Exception ex)
            {
                PostToastToUi(LogType.Error, $"Failed to write test logs: {ex.Message}", "Logging", 8);
            }
        }

        private async Task HandleUsageTestRequestAsync()
        {
            try
            {
                var log = ServiceLocator.SafeGet<ILoggingService>();
                if (log == null)
                {
                    PostToastToUi(LogType.Error, "Logging service not available", "Usage Analytics", 8);
                    return;
                }

                await log.LogUserActivityAsync("test_activity", new Dictionary<string, object>
                {
                    ["test_parameter"] = "test_value",
                    ["timestamp"] = DateTime.UtcNow
                });

                PostToastToUi(LogType.Success, "Test usage analytics event sent.", "Usage Analytics", 6);
            }
            catch (Exception ex)
            {
                PostToastToUi(LogType.Error, $"Failed to send test event: {ex.Message}", "Usage Analytics", 8);
            }
        }

        private void HandleTabClosed(JsonElement root)
        {
            if (!TryGetString(root, "tabId", out var tabId))
                return;

            try
            {
                _tabActions.TryRemove(tabId, out _);
                PostLogToUi(LogType.Debug, $"tab_closed: cleared actions for tab {tabId}");
            }
            catch
            {
                // ignore
            }
        }

        private void HandleToastClosed(JsonElement root)
        {
            if (!TryGetString(root, "toastId", out var toastId))
                return;

            try
            {
                _toastActions.TryRemove(toastId, out _);
                PostLogToUi(LogType.Debug, $"toast_closed: cleared actions for toast {toastId}");
            }
            catch
            {
                // ignore
            }
        }

        private void HandleToastActionExecute(JsonElement root)
        {
            if (!TryGetString(root, "toastId", out var toastId) || !TryGetString(root, "actionId", out var actionId))
            {
                PostLogToUi(LogType.Warning, "toast_action_execute: missing toastId or actionId");
                return;
            }

            if (!_toastActions.TryGetValue(toastId, out var actions) || actions == null)
            {
                PostLogToUi(LogType.Warning, $"toast_action_execute: no actions found for toast {toastId}");
                return;
            }

            var action = actions.FirstOrDefault(a => a.actionId == actionId);
            if (action.callback == null)
            {
                PostLogToUi(LogType.Warning, $"toast_action_execute: action {actionId} not found for toast {toastId}");
                return;
            }

            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    action.callback();
                });

                PostLogToUi(LogType.Debug, $"toast_action_execute: executed action {actionId} for toast {toastId}");
            }
            catch (Exception ex)
            {
                PostLogToUi(LogType.Error, $"toast_action_execute: error executing action {actionId}: {ex.Message}");
            }
        }

        private void HandleTabActionExecute(JsonElement root)
        {
            if (!TryGetString(root, "tabId", out var tabId) || !TryGetString(root, "actionId", out var actionId))
            {
                PostLogToUi(LogType.Warning, "tab_action_execute: missing tabId or actionId");
                return;
            }

            if (!_tabActions.TryGetValue(tabId, out var actions) || actions == null)
            {
                PostLogToUi(LogType.Warning, $"tab_action_execute: no actions found for tab {tabId}");
                return;
            }

            var action = actions.FirstOrDefault(a => a.actionId == actionId);
            if (action.callback == null)
            {
                PostLogToUi(LogType.Warning, $"tab_action_execute: action {actionId} not found for tab {tabId}");
                return;
            }

            // Get context from the message or use empty dictionary
            Dictionary<string, string> context = new(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (root.TryGetProperty("context", out var contextEl) && contextEl.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in contextEl.EnumerateObject())
                    {
                        context[prop.Name] = JsonElementToString(prop.Value);
                    }
                }
            }
            catch
            {
                // ignore context parsing issues; callbacks can still run with empty context
            }

            try
            {
                // Execute callback on dispatcher thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    action.callback(context);
                });
                PostLogToUi(LogType.Debug, $"tab_action_execute: executed action {actionId} for tab {tabId}");
            }
            catch (Exception ex)
            {
                PostLogToUi(LogType.Error, $"tab_action_execute: error executing action {actionId}: {ex.Message}");
            }
        }

        private void HandleClearLogsRequest(JsonElement root)
        {
            string? path = null;
            if (root.TryGetProperty("path", out var pEl) && pEl.ValueKind == JsonValueKind.String)
                path = pEl.GetString();

            try
            {
                var settings = ServiceLocator.SafeGet<Services.SettingsService>()?.Settings;
                var logPath = path ?? settings?.LoggingSettings?.LocalLogPath;
                if (string.IsNullOrWhiteSpace(logPath))
                {
                    PostToastToUi(LogType.Error, "Log path is not set", "Logging", 8);
                    return;
                }

                if (!Directory.Exists(logPath))
                {
                    PostToastToUi(LogType.Warning, "Log folder does not exist", "Logging", 6);
                    return;
                }

                var files = Directory.GetFiles(logPath, "*.log");
                var deleted = 0;
                foreach (var f in files)
                {
                    try { File.Delete(f); deleted++; } catch { /* ignore */ }
                }

                PostToUi(new { type = "log_clear_result", deletedCount = deleted });
                PostToastToUi(LogType.Success, $"Deleted {deleted} log files.", "Logging", 6);
            }
            catch (Exception ex)
            {
                PostToastToUi(LogType.Error, $"Failed to clear logs: {ex.Message}", "Logging", 8);
            }
        }

        private void HandleOpenExternal(JsonElement root)
        {
            if (!TryGetString(root, "url", out var url))
                return;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                PostToastToUi(LogType.Error, $"Failed to open URL: {url}", "Open External", 8, ex.Message);
            }
        }

        private void HandleHandlersListRequest()
        {
            try
            {
                SendHandlersList();
            }
            catch (Exception ex)
            {
                PostToastToUi(LogType.Error, "Failed to load handlers list", "Handlers", 8, ex.Message);
            }
        }

        private void HandleHandlerSetEnabled(JsonElement root)
        {
            if (!TryGetString(root, "name", out var name))
                return;

            if (!TryGetBool(root, "enabled", out var enabled))
                return;

            var manager = ServiceLocator.SafeGet<HandlerManager>();
            if (manager == null)
            {
                PostToastToUi(LogType.Error, "Handler manager is not available", "Handlers");
                return;
            }

            var ok = manager.UpdateHandlerEnabledState(name, enabled);
            PostToastToUi(ok ? LogType.Success : LogType.Error, ok
                ? $"Handler '{name}' {(enabled ? "enabled" : "disabled")}"
                : $"Failed to update handler '{name}'", "Handlers", 6);

            SendHandlersList();
        }

        private void HandleHandlerSetMcp(JsonElement root)
        {
            if (!TryGetString(root, "name", out var name))
                return;

            if (!TryGetBool(root, "mcpEnabled", out var mcpEnabled))
                return;

            var manager = ServiceLocator.SafeGet<HandlerManager>();
            if (manager == null)
            {
                PostToastToUi(LogType.Error, "Handler manager is not available", "Handlers");
                return;
            }

            var ok = manager.UpdateHandlerMcpEnabledState(name, mcpEnabled);
            PostToastToUi(ok ? LogType.Success : LogType.Error, ok
                ? $"Handler '{name}' MCP {(mcpEnabled ? "enabled" : "disabled")}"
                : $"Failed to update MCP state for '{name}'", "Handlers", 6);

            SendHandlersList();
        }

        private void HandleHandlersReload(JsonElement root)
        {
            bool reloadPlugins = true;
            if (TryGetBool(root, "reloadPlugins", out var rp))
                reloadPlugins = rp;

            var manager = ServiceLocator.SafeGet<HandlerManager>();
            if (manager == null)
            {
                PostToastToUi(LogType.Error, "Handler manager is not available", "Handlers");
                return;
            }

            var (handlersReloaded, newPluginsLoaded) = manager.ReloadHandlers(reloadPlugins);
            PostToastToUi(LogType.Success, $"Reloaded {handlersReloaded} handlers, {newPluginsLoaded} new plugins", "Handlers", 6);
            SendHandlersList();
        }

        private void SendHandlersList()
        {
            var manager = ServiceLocator.SafeGet<HandlerManager>();
            if (manager == null)
                return;

            var manualNames = new HashSet<string>(manager.GetManualHandlerNames(), StringComparer.OrdinalIgnoreCase);
            var configs = manager.GetAllHandlerConfigs();
            var handlers = configs.Select(c => new
            {
                name = c.Name,
                description = c.Description,
                type = c.Type,
                enabled = c.Enabled,
                mcpEnabled = c.McpEnabled,
                screenId = c.ScreenId,
                title = c.Title,
                isManual = manualNames.Contains(c.Name)
            }).ToList();

            PostToUi(new
            {
                type = "handlers_list",
                handlers
            });
        }

        private void HandleManualHandlerExecute(JsonElement root)
        {
            if (!TryGetString(root, "name", out var name))
                return;

            var manager = ServiceLocator.SafeGet<HandlerManager>();
            if (manager == null)
            {
                PostToastToUi(LogType.Error, "Handler manager is not available", "Manual Handlers", 6);
                return;
            }

            // Run in background so we don't block the UI thread.
            _ = Task.Run(async () =>
            {
                try
                {
                    await manager.ExecuteManualHandlerAsync(name);
                }
                catch (Exception ex)
                {
                    PostToastToUi(LogType.Error, $"Failed to execute manual handler '{name}'", "Manual Handlers", 8, ex.Message);
                }
            });
        }

        private void HandleCronListRequest()
        {
            try
            {
                SendCronList();
            }
            catch (Exception ex)
            {
                PostToastToUi(LogType.Error, "Failed to load cron jobs", "Cron", 8, ex.Message);
            }
        }

        private void HandleCronSetEnabled(JsonElement root)
        {
            if (!TryGetString(root, "jobId", out var jobId))
                return;

            if (!TryGetBool(root, "enabled", out var enabled))
                return;

            var cron = ServiceLocator.SafeGet<ICronService>();
            if (cron == null)
            {
                PostToastToUi(LogType.Error, "Cron service is not available", "Cron");
                return;
            }

            var ok = cron.SetJobEnabled(jobId, enabled);
            PostToastToUi(ok ? LogType.Success : LogType.Error, ok
                ? $"Cron job '{jobId}' {(enabled ? "enabled" : "disabled")}"
                : $"Failed to update cron job '{jobId}'", "Cron", 6);

            SendCronList();
        }

        private void HandleCronTrigger(JsonElement root)
        {
            if (!TryGetString(root, "jobId", out var jobId))
                return;

            var cron = ServiceLocator.SafeGet<ICronService>();
            if (cron == null)
            {
                PostToastToUi(LogType.Error, "Cron service is not available", "Cron");
                return;
            }

            var ok = cron.TriggerJob(jobId);
            PostToastToUi(ok ? LogType.Success : LogType.Error, ok
                ? $"Triggered cron job '{jobId}'"
                : $"Failed to trigger cron job '{jobId}'", "Cron", 6);

            SendCronList();
        }

        private void SendCronList()
        {
            var cron = ServiceLocator.SafeGet<ICronService>();
            if (cron == null)
                return;

            var jobs = cron.GetScheduledJobs()?.Values?.Select(j => new
            {
                jobId = j.JobId,
                cronExpression = j.CronExpression,
                timezone = j.Timezone,
                enabled = j.Enabled,
                lastExecution = j.LastExecution,
                nextExecution = j.NextExecution,
                executionCount = j.ExecutionCount,
                lastError = j.LastError,
                handlerName = j.HandlerConfig?.Name
            }).ToList() ?? new();

            PostToUi(new
            {
                type = "cron_list",
                isRunning = cron.IsRunning,
                jobs
            });
        }

        private static bool TryGetBool(JsonElement root, string propertyName, out bool value)
        {
            value = false;
            if (!root.TryGetProperty(propertyName, out var el))
                return false;

            if (el.ValueKind == JsonValueKind.True) { value = true; return true; }
            if (el.ValueKind == JsonValueKind.False) { value = false; return true; }

            if (el.ValueKind == JsonValueKind.String && bool.TryParse(el.GetString(), out var parsed))
            {
                value = parsed;
                return true;
            }

            return false;
        }

        private void HandleOpenFileDialogRequest(JsonElement root)
        {
            if (!TryGetString(root, "requestId", out var requestId))
                return;

            bool multiSelect = root.TryGetProperty("multiSelect", out var mEl) && mEl.ValueKind == JsonValueKind.True;
            string title = root.TryGetProperty("title", out var tEl) && tEl.ValueKind == JsonValueKind.String ? (tEl.GetString() ?? "Select File") : "Select File";
            string filter = root.TryGetProperty("filter", out var fEl) && fEl.ValueKind == JsonValueKind.String ? (fEl.GetString() ?? "All Files|*.*") : "All Files|*.*";

            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = title,
                    Filter = filter,
                    Multiselect = multiSelect
                };

                var ok = dialog.ShowDialog(this) == true;
                if (!ok)
                {
                    PostToUi(new { type = "ui_open_file_dialog_response", requestId, cancelled = true });
                    return;
                }

                if (multiSelect)
                {
                    PostToUi(new
                    {
                        type = "ui_open_file_dialog_response",
                        requestId,
                        cancelled = false,
                        paths = dialog.FileNames
                    });
                }
                else
                {
                    PostToUi(new
                    {
                        type = "ui_open_file_dialog_response",
                        requestId,
                        cancelled = false,
                        path = dialog.FileName
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OpenFileDialog error: {ex}");
                PostToUi(new { type = "ui_open_file_dialog_response", requestId, cancelled = true, error = ex.Message });
            }
        }

        private void HandleConfirmResponse(JsonElement root)
        {
            if (!TryGetString(root, "requestId", out var requestId))
                return;

            bool confirmed = root.TryGetProperty("confirmed", out var cEl) && cEl.ValueKind == JsonValueKind.True;

            if (_pendingConfirms.TryRemove(requestId, out var tcs))
            {
                tcs.TrySetResult(confirmed);
            }
        }

        private void HandleUserInputResponse(JsonElement root)
        {
            if (!TryGetString(root, "requestId", out var requestId))
                return;

            bool cancelled = root.TryGetProperty("cancelled", out var cancelledEl) && cancelledEl.ValueKind == JsonValueKind.True;

            string? value = null;
            if (root.TryGetProperty("value", out var vEl) && vEl.ValueKind == JsonValueKind.String)
                value = vEl.GetString();

            List<string>? selectedValues = null;
            if (root.TryGetProperty("selectedValues", out var arrEl) && arrEl.ValueKind == JsonValueKind.Array)
            {
                selectedValues = new List<string>();
                foreach (var item in arrEl.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                        selectedValues.Add(item.GetString() ?? string.Empty);
                }
            }

            if (_pendingUserInputs.TryRemove(requestId, out var tcs))
            {
                tcs.TrySetResult(new UserInputResponse
                {
                    Cancelled = cancelled,
                    Value = value,
                    SelectedValues = selectedValues
                });
            }
        }

        private void HandleNavigationInputResponse(JsonElement root)
        {
            if (!TryGetString(root, "requestId", out var requestId))
                return;

            var result = new NavigationResult { Action = NavigationAction.Cancel };

            if (TryGetString(root, "action", out var action))
            {
                result.Action = action.Trim().ToLowerInvariant() switch
                {
                    "next" => NavigationAction.Next,
                    "back" => NavigationAction.Back,
                    _ => NavigationAction.Cancel
                };
            }

            if (root.TryGetProperty("value", out var vEl) && vEl.ValueKind == JsonValueKind.String)
                result.Value = vEl.GetString();

            if (root.TryGetProperty("selectedValues", out var arrEl) && arrEl.ValueKind == JsonValueKind.Array)
            {
                var list = new List<string>();
                foreach (var item in arrEl.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                        list.Add(item.GetString() ?? string.Empty);
                }
                result.SelectedValues = list;
                // Ensure Value is populated for older consumers that only read Value.
                if (string.IsNullOrWhiteSpace(result.Value) && list.Count > 0)
                    result.Value = string.Join(",", list);
            }

            if (_pendingNavInputs.TryRemove(requestId, out var tcs))
            {
                tcs.TrySetResult(result);
            }
        }

        private static bool TryGetString(JsonElement root, string propertyName, out string value)
        {
            value = string.Empty;
            if (!root.TryGetProperty(propertyName, out var el) || el.ValueKind != JsonValueKind.String)
                return false;
            value = el.GetString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(value);
        }

        private static string NormalizeTabId(string screenId, string title)
        {
            return $"{(screenId ?? string.Empty).Trim()}_{(title ?? string.Empty).Trim()}";
        }

        private static string JsonElementToString(JsonElement el)
        {
            try
            {
                return el.ValueKind switch
                {
                    JsonValueKind.String => el.GetString() ?? string.Empty,
                    JsonValueKind.Number => el.ToString(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => string.Empty,
                    JsonValueKind.Undefined => string.Empty,
                    JsonValueKind.Object => el.GetRawText(),
                    JsonValueKind.Array => el.GetRawText(),
                    _ => el.ToString()
                };
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string MapToastActionStyle(ToastActionStyle style)
        {
            return style switch
            {
                ToastActionStyle.Primary => "primary",
                ToastActionStyle.Danger => "danger",
                _ => "secondary"
            };
        }

        private void SendHostReady()
        {
            var appVersion = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
            var wpfTheme = ThemeManager.Instance.CurrentTheme;
            var uiTheme = MapWpfThemeToUi(wpfTheme);

            Debug.WriteLine($"SendHostReady: wpfTheme={wpfTheme}, uiTheme={uiTheme}");
            PostLogToUi(LogType.Debug, $"Host ready: theme={uiTheme} (WPF: {wpfTheme})");

            string? mcpSseUrl = null;
            try
            {
                var settingsService = ServiceLocator.SafeGet<Services.SettingsService>();
                if (settingsService?.Settings?.McpSettings?.Enabled == true)
                {
                    var port = settingsService.Settings.McpSettings.Port;
                    mcpSseUrl = $"http://127.0.0.1:{port}/mcp/sse";
                }
            }
            catch
            {
                // ignore
            }

            PostToUi(new
            {
                type = "host_ready",
                protocolVersion = UiProtocolVersion,
                appVersion,
                theme = uiTheme,
                mcpSseUrl,
            });

            _lastThemeSentToUi = uiTheme;
        }

        private void SendPong(JsonElement root)
        {
            string? id = null;
            if (root.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String)
            {
                id = idEl.GetString();
            }

            PostToUi(new { type = "pong", id });
        }

        private void HandleSetTheme(JsonElement root)
        {
            if (!root.TryGetProperty("theme", out var themeEl) || themeEl.ValueKind != JsonValueKind.String)
                return;

            var uiTheme = (themeEl.GetString() ?? string.Empty).Trim().ToLowerInvariant();
            var wpfTheme = MapUiThemeToWpf(uiTheme);

            if (string.IsNullOrWhiteSpace(wpfTheme))
                return;

            // Avoid loops: only apply if theme actually changes.
            if (string.Equals(ThemeManager.Instance.CurrentTheme, wpfTheme, StringComparison.OrdinalIgnoreCase))
                return;

            // Use PreviewTheme instead of ApplyTheme so we don't persist immediately.
            // The user must click Save in Settings for the theme to be written to disk.
            ThemeManager.Instance.PreviewTheme(wpfTheme);
        }

        private void ThemeManager_ThemeChanged(object? sender, string theme)
        {
            try
            {
                var uiTheme = MapWpfThemeToUi(theme);
                if (string.IsNullOrWhiteSpace(uiTheme))
                    return;

                if (string.Equals(_lastThemeSentToUi, uiTheme, StringComparison.OrdinalIgnoreCase))
                    return;

                PostToUi(new { type = "theme_changed", theme = uiTheme });
                _lastThemeSentToUi = uiTheme;
            }
            catch
            {
                // ignore
            }
        }

        public void PostLogToUi(LogType level, string message, string? details = null)
        {
            PostToUi(new
            {
                type = "log",
                level = MapLogType(level),
                message,
                details
            });
        }

        public void PostToastToUi(LogType level, string message, string? title = null, int durationSeconds = 5, string? details = null)
        {
            PostToastToUi(level, message, title, durationSeconds, details, null);
        }

        public void PostToastToUi(LogType level, string message, string? title, int durationSeconds, string? details, ToastAction[]? actions)
        {
            if (durationSeconds < 1) durationSeconds = 1;
            if (durationSeconds > 600) durationSeconds = 600;

            string? toastId = null;
            object? actionsPayload = null;

            try
            {
                if (actions != null && actions.Length > 0)
                {
                    toastId = Guid.NewGuid().ToString("N");
                    var list = new List<(string actionId, string label, ToastActionStyle style, bool closeOnClick, bool isDefaultAction, Action callback)>();

                    for (int i = 0; i < actions.Length; i++)
                    {
                        var a = actions[i];
                        if (a == null) continue;

                        var label = (a.Text ?? string.Empty).Trim();
                        if (string.IsNullOrWhiteSpace(label))
                            label = $"Action {i + 1}";

                        var actionId = $"{toastId}_action_{i}";
                        list.Add((actionId, label, a.Style, a.CloseOnClick, a.IsDefaultAction, a.Action ?? (() => { })));
                    }

                    if (list.Count > 0)
                    {
                        _toastActions[toastId] = list;
                        actionsPayload = list.Select(a => new
                        {
                            id = a.actionId,
                            label = a.label,
                            style = MapToastActionStyle(a.style),
                            closeOnClick = a.closeOnClick,
                            isDefaultAction = a.isDefaultAction
                        }).ToList();
                    }
                }
            }
            catch
            {
                // ignore toast action serialization issues; fall back to simple toast
                toastId = null;
                actionsPayload = null;
            }

            PostToUi(new
            {
                type = "toast",
                toastId,
                level = MapLogType(level),
                title,
                message,
                details,
                durationSeconds,
                actions = actionsPayload
            });

            // If the toast is actionable, mimic old WPF behavior (Topmost toast window) by bringing the shell forward.
            if (actionsPayload != null)
            {
                BringToFrontSafe();
            }
        }

        public void PostOpenTabToUi(string screenId, string title, object? context = null, bool autoFocus = false, bool bringToFront = false)
        {
            PostOpenTabToUi(screenId, title, context, null, autoFocus, bringToFront);
        }

        public void PostOpenTabToUi(string screenId, string title, object? context, List<KeyValuePair<string, Action<Dictionary<string, string>>>>? actions, bool autoFocus = false, bool bringToFront = false)
        {
            // Ensure context is always serializable and stable.
            // Some callers may pass custom dictionary types (e.g. ContextWrapper). We copy into a plain dictionary
            // to avoid surprises, and to prevent concurrent modification issues during JSON serialization.
            object? safeContext = context;
            try
            {
                if (context is System.Collections.Generic.IDictionary<string, string> dict)
                {
                    var copy = new Dictionary<string, string>(dict.Count, StringComparer.OrdinalIgnoreCase);
                    foreach (var kv in dict)
                        copy[kv.Key] = kv.Value ?? string.Empty;
                    safeContext = copy;

                    // Helpful debug breadcrumb for "No context provided" issues.
                    var hasBody = copy.TryGetValue(ContextKey._body, out var body);
                    PostLogToUi(LogType.Debug, $"open_tab: {screenId} / {title} (autoFocus={autoFocus}, bringToFront={bringToFront}, keys={copy.Count}, has_body={hasBody}, body_len={(hasBody ? body?.Length ?? 0 : 0)})");
                }
                else if (context == null)
                {
                    // Still include a context object so the UI can show an empty payload instead of "No context provided."
                    safeContext = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    PostLogToUi(LogType.Warning, $"open_tab: {screenId} / {title} (autoFocus={autoFocus}, bringToFront={bringToFront}, context=null)");
                }
            }
            catch
            {
                // If anything goes wrong, fall back to original context object.
                safeContext = context;
            }

            var tabId = NormalizeTabId(screenId, title);

            // Store actions if provided
            List<(string actionId, string label, Action<Dictionary<string, string>> callback)>? serializedActions = null;
            if (actions != null && actions.Count > 0)
            {
                serializedActions = new List<(string, string, Action<Dictionary<string, string>>)>();
                for (int i = 0; i < actions.Count; i++)
                {
                    var action = actions[i];
                    var actionId = $"{tabId}_action_{i}";
                    serializedActions.Add((actionId, action.Key, action.Value));
                }
                _tabActions[tabId] = serializedActions;
            }
            else
            {
                // Clear any existing actions for this tab
                _tabActions.TryRemove(tabId, out _);
            }

            // Serialize actions for React (only IDs and labels)
            object? actionsPayload = null;
            if (serializedActions != null && serializedActions.Count > 0)
            {
                actionsPayload = serializedActions.Select(a => new { id = a.actionId, label = a.label }).ToList();
            }

            PostToUi(new
            {
                type = "open_tab",
                screenId,
                title,
                context = safeContext,
                actions = actionsPayload,
                autoFocus,
                bringToFront
            });

            // Practical UX: if we explicitly want the tab to auto-focus, we almost always want the window visible too.
            // This also helps when users set auto_focus_tab=true but forget bring_window_to_front.
            if (bringToFront || autoFocus)
            {
                BringToFrontSafe();
            }
        }

        private void PostToUi(object payload)
        {
            if (!_webViewInitialized)
                return;

            if (!Dispatcher.CheckAccess())
            {
                try
                {
                    Dispatcher.Invoke(() => PostToUi(payload));
                }
                catch { /* ignore */ }
                return;
            }

            if (WebView.CoreWebView2 == null)
                return;

            try
            {
                var json = JsonSerializer.Serialize(payload, _jsonOptions);
                WebView.CoreWebView2.PostWebMessageAsString(json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ReactShellWindow.PostToUi error: {ex}");
            }
        }

        public async Task<bool> RequestConfirmAsync(string title, string message, TimeSpan? timeout = null)
        {
            await WaitForUiReadyAsync(timeout);
            BringToFrontSafe();

            var requestId = Guid.NewGuid().ToString("N");
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingConfirms[requestId] = tcs;

            PostToUi(new
            {
                type = "ui_confirm_request",
                requestId,
                title,
                message
            });

            return await WaitWithTimeoutAsync(requestId, tcs.Task, _pendingConfirms, timeout);
        }

        public async Task<UserInputResponse> RequestUserInputAsync(UserInputRequest request, Dictionary<string, string>? context, TimeSpan? timeout = null)
        {
            await WaitForUiReadyAsync(timeout);
            BringToFrontSafe();

            var requestId = Guid.NewGuid().ToString("N");
            var tcs = new TaskCompletionSource<UserInputResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingUserInputs[requestId] = tcs;

            PostToUi(new
            {
                type = "ui_user_input_request",
                requestId,
                request,
                context
            });

            return await WaitWithTimeoutAsync(requestId, tcs.Task, _pendingUserInputs, timeout);
        }

        public async Task<NavigationResult> RequestUserInputWithNavigationAsync(UserInputRequest request, Dictionary<string, string> context, bool canGoBack, int currentStep, int totalSteps, TimeSpan? timeout = null)
        {
            await WaitForUiReadyAsync(timeout);
            BringToFrontSafe();

            var requestId = Guid.NewGuid().ToString("N");
            var tcs = new TaskCompletionSource<NavigationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingNavInputs[requestId] = tcs;

            PostToUi(new
            {
                type = "ui_user_input_navigation_request",
                requestId,
                request,
                context,
                canGoBack,
                currentStep,
                totalSteps
            });

            return await WaitWithTimeoutAsync(requestId, tcs.Task, _pendingNavInputs, timeout);
        }

        private async Task WaitForUiReadyAsync(TimeSpan? timeout)
        {
            if (_uiReady.Task.IsCompleted)
                return;

            if (timeout == null)
            {
                await _uiReady.Task;
                return;
            }

            var completed = await Task.WhenAny(_uiReady.Task, Task.Delay(timeout.Value));
            if (completed != _uiReady.Task)
                throw new TimeoutException("UI handshake timed out.");
        }

        private static async Task<T> WaitWithTimeoutAsync<T>(
            string requestId,
            Task<T> task,
            ConcurrentDictionary<string, TaskCompletionSource<T>> pending,
            TimeSpan? timeout)
        {
            if (timeout == null)
                return await task;

            var completed = await Task.WhenAny(task, Task.Delay(timeout.Value));
            if (completed == task)
                return await task;

            if (pending.TryRemove(requestId, out var tcs))
            {
                tcs.TrySetException(new TimeoutException($"UI request timed out: {requestId}"));
            }

            throw new TimeoutException($"UI request timed out: {requestId}");
        }

        private static string MapWpfThemeToUi(string? theme)
        {
            var t = (theme ?? string.Empty).Trim().ToLowerInvariant();
            return t switch
            {
                "light" => "light",
                "dark" => "dark",
                _ => "dark"
            };
        }

        private static string MapUiThemeToWpf(string uiTheme)
        {
            return uiTheme switch
            {
                "light" => "Light",
                "dark" => "Dark",
                _ => string.Empty
            };
        }

        private static string MapLogType(LogType logType)
        {
            return logType switch
            {
                LogType.Success => "success",
                LogType.Error => "error",
                LogType.Warning => "warning",
                LogType.Debug => "debug",
                LogType.Critical => "critical",
                _ => "info"
            };
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Handler Exchange / Marketplace Methods
        // ─────────────────────────────────────────────────────────────────────────

        private async Task HandleExchangeListRequestAsync(JsonElement root)
        {
            try
            {
                var exchange = ServiceLocator.SafeGet<Contextualizer.PluginContracts.Interfaces.IHandlerExchange>();
                if (exchange == null)
                {
                    PostToUi(new { type = "exchange_list", packages = Array.Empty<object>(), error = "Handler exchange service not available" });
                    return;
                }

                string? searchTerm = null;
                string[]? tags = null;

                if (root.TryGetProperty("searchTerm", out var searchProp) && searchProp.ValueKind == JsonValueKind.String)
                    searchTerm = searchProp.GetString();

                if (root.TryGetProperty("tags", out var tagsProp) && tagsProp.ValueKind == JsonValueKind.Array)
                    tags = tagsProp.EnumerateArray().Where(t => t.ValueKind == JsonValueKind.String).Select(t => t.GetString()!).ToArray();

                var packages = await exchange.ListAvailableHandlersAsync(searchTerm, tags);
                var packageDtos = packages.Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    description = p.Description,
                    version = p.Version,
                    author = p.Author,
                    tags = p.Tags ?? Array.Empty<string>(),
                    dependencies = p.Dependencies ?? Array.Empty<string>(),
                    isInstalled = p.IsInstalled,
                    hasUpdate = p.HasUpdate,
                    metadata = p.Metadata
                }).ToArray();

                PostToUi(new { type = "exchange_list", packages = packageDtos });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"HandleExchangeListRequestAsync error: {ex}");
                PostToUi(new { type = "exchange_list", packages = Array.Empty<object>(), error = ex.Message });
            }
        }

        private async Task HandleExchangeTagsRequestAsync()
        {
            try
            {
                var exchange = ServiceLocator.SafeGet<Contextualizer.PluginContracts.Interfaces.IHandlerExchange>();
                if (exchange == null)
                {
                    PostToUi(new { type = "exchange_tags", tags = Array.Empty<string>() });
                    return;
                }

                var tags = await exchange.GetAvailableTagsAsync();
                PostToUi(new { type = "exchange_tags", tags = tags.ToArray() });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"HandleExchangeTagsRequestAsync error: {ex}");
                PostToUi(new { type = "exchange_tags", tags = Array.Empty<string>() });
            }
        }

        private async Task HandleExchangeDetailsRequestAsync(JsonElement root)
        {
            try
            {
                if (!root.TryGetProperty("handlerId", out var idProp) || idProp.ValueKind != JsonValueKind.String)
                    return;

                var handlerId = idProp.GetString();
                if (string.IsNullOrEmpty(handlerId)) return;

                var exchange = ServiceLocator.SafeGet<Contextualizer.PluginContracts.Interfaces.IHandlerExchange>();
                if (exchange == null)
                {
                    PostToUi(new { type = "exchange_details", handlerId, package = (object?)null, error = "Service not available" });
                    return;
                }

                var package = await exchange.GetHandlerDetailsAsync(handlerId);
                if (package == null)
                {
                    PostToUi(new { type = "exchange_details", handlerId, package = (object?)null, error = "Package not found" });
                    return;
                }

                PostToUi(new
                {
                    type = "exchange_details",
                    handlerId,
                    package = new
                    {
                        id = package.Id,
                        name = package.Name,
                        description = package.Description,
                        version = package.Version,
                        author = package.Author,
                        tags = package.Tags ?? Array.Empty<string>(),
                        dependencies = package.Dependencies ?? Array.Empty<string>(),
                        isInstalled = package.IsInstalled,
                        hasUpdate = package.HasUpdate,
                        metadata = package.Metadata
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"HandleExchangeDetailsRequestAsync error: {ex}");
            }
        }

        private async Task HandleExchangeInstallAsync(JsonElement root)
        {
            string? handlerId = null;
            try
            {
                if (!root.TryGetProperty("handlerId", out var idProp) || idProp.ValueKind != JsonValueKind.String)
                    return;

                handlerId = idProp.GetString();
                if (string.IsNullOrEmpty(handlerId)) return;

                var exchange = ServiceLocator.SafeGet<Contextualizer.PluginContracts.Interfaces.IHandlerExchange>();
                if (exchange == null)
                {
                    PostToUi(new { type = "exchange_install_result", handlerId, success = false, error = "Service not available" });
                    return;
                }

                var success = await exchange.InstallHandlerAsync(handlerId);
                PostToUi(new { type = "exchange_install_result", handlerId, success });

                if (success)
                {
                    // Reload handlers after install
                    var handlerManager = ServiceLocator.SafeGet<HandlerManager>();
                    handlerManager?.ReloadHandlers(reloadPlugins: false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"HandleExchangeInstallAsync error: {ex}");
                PostToUi(new { type = "exchange_install_result", handlerId = handlerId ?? "", success = false, error = ex.Message });
            }
        }

        private async Task HandleExchangeUpdateAsync(JsonElement root)
        {
            string? handlerId = null;
            try
            {
                if (!root.TryGetProperty("handlerId", out var idProp) || idProp.ValueKind != JsonValueKind.String)
                    return;

                handlerId = idProp.GetString();
                if (string.IsNullOrEmpty(handlerId)) return;

                var exchange = ServiceLocator.SafeGet<Contextualizer.PluginContracts.Interfaces.IHandlerExchange>();
                if (exchange == null)
                {
                    PostToUi(new { type = "exchange_update_result", handlerId, success = false, error = "Service not available" });
                    return;
                }

                var success = await exchange.UpdateHandlerAsync(handlerId);
                PostToUi(new { type = "exchange_update_result", handlerId, success });

                if (success)
                {
                    // Reload handlers after update
                    var handlerManager = ServiceLocator.SafeGet<HandlerManager>();
                    handlerManager?.ReloadHandlers(reloadPlugins: false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"HandleExchangeUpdateAsync error: {ex}");
                PostToUi(new { type = "exchange_update_result", handlerId = handlerId ?? "", success = false, error = ex.Message });
            }
        }

        private async Task HandleExchangeRemoveAsync(JsonElement root)
        {
            string? handlerId = null;
            try
            {
                if (!root.TryGetProperty("handlerId", out var idProp) || idProp.ValueKind != JsonValueKind.String)
                    return;

                handlerId = idProp.GetString();
                if (string.IsNullOrEmpty(handlerId)) return;

                var exchange = ServiceLocator.SafeGet<Contextualizer.PluginContracts.Interfaces.IHandlerExchange>();
                if (exchange == null)
                {
                    PostToUi(new { type = "exchange_remove_result", handlerId, success = false, error = "Service not available" });
                    return;
                }

                var success = await exchange.RemoveHandlerAsync(handlerId);
                PostToUi(new { type = "exchange_remove_result", handlerId, success });

                if (success)
                {
                    // Reload handlers after removal
                    var handlerManager = ServiceLocator.SafeGet<HandlerManager>();
                    handlerManager?.ReloadHandlers(reloadPlugins: false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"HandleExchangeRemoveAsync error: {ex}");
                PostToUi(new { type = "exchange_remove_result", handlerId = handlerId ?? "", success = false, error = ex.Message });
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                ThemeManager.Instance.ThemeChanged -= ThemeManager_ThemeChanged;
            }
            catch { /* ignore */ }

            if (!disposing) return;

            try
            {
                foreach (var kvp in _pendingConfirms)
                    kvp.Value.TrySetCanceled();
                foreach (var kvp in _pendingUserInputs)
                    kvp.Value.TrySetCanceled();
                foreach (var kvp in _pendingNavInputs)
                    kvp.Value.TrySetCanceled();
            }
            catch { /* ignore */ }

            try
            {
                if (WebView?.CoreWebView2 != null)
                {
                    WebView.CoreWebView2.WebMessageReceived -= CoreWebView2_WebMessageReceived;
                    WebView.CoreWebView2.Stop();
                }
            }
            catch { /* ignore */ }

            try { WebView?.Dispose(); } catch { /* ignore */ }
        }

        public sealed class UserInputResponse
        {
            public bool Cancelled { get; set; }
            public string? Value { get; set; }
            public List<string>? SelectedValues { get; set; }
        }
    }
}


