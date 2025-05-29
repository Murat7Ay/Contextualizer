using Contextualizer.Core.Services;
using Contextualizer.PluginContracts;
using SharpHook;
using SharpHook.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace Contextualizer.Core
{
    public class KeyboardHook : IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private IGlobalHook? _hook;
        private bool _isDisposed;
        private ISettingsService _settingsService;

        public KeyboardHook(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public event EventHandler<ClipboardCapturedEventArgs>? TextCaptured;
        public event EventHandler<LogMessageEventArgs>? LogMessage;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private void Log(LogType level, string message)
        {
            LogMessage?.Invoke(this, new LogMessageEventArgs(level, message));
        }

        public Task StartAsync()
        {
            if (_hook != null) throw new InvalidOperationException("Hook is already started.");

            _hook = new SimpleGlobalHook();
            _hook.KeyPressed += OnKeyPressed;

            var modifiers = new List<string>();
            if (_settingsService.HasModifierKey("Ctrl")) modifiers.Add("Ctrl");
            if (_settingsService.HasModifierKey("Alt")) modifiers.Add("Alt");
            if (_settingsService.HasModifierKey("Shift")) modifiers.Add("Shift");
            if (_settingsService.HasModifierKey("Win")) modifiers.Add("Win");

            var shortcutText = modifiers.Count > 0 
                ? string.Join("+", modifiers) + "+" + _settingsService.ShortcutKey
                : _settingsService.ShortcutKey;

            Log(LogType.Info, $"Press {shortcutText} to capture selected text...");
            return _hook.RunAsync();
        }

        public void Stop()
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            _hook.KeyPressed -= OnKeyPressed;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            _hook?.Dispose(); // Important: Dispose of the hook
            _hook = null;
        }

        private void OnKeyPressed(object? sender, KeyboardHookEventArgs args)
        {
            bool ctrlPressed = args.RawEvent.Mask.HasCtrl();
            bool altPressed = args.RawEvent.Mask.HasAlt();
            bool shiftPressed = args.RawEvent.Mask.HasShift();
            bool winPressed = args.RawEvent.Mask.HasMeta();

            bool ctrlRequired = _settingsService.HasModifierKey("Ctrl");
            bool altRequired = _settingsService.HasModifierKey("Alt");
            bool shiftRequired = _settingsService.HasModifierKey("Shift");
            bool winRequired = _settingsService.HasModifierKey("Win");

            if (Enum.TryParse<KeyCode>("Vc"+_settingsService.ShortcutKey.ToUpper(), true, out KeyCode requiredKey))
            {
                if (ctrlPressed == ctrlRequired &&
                altPressed == altRequired &&
                shiftPressed == shiftRequired &&
                winPressed == winRequired &&
    args.Data.KeyCode == requiredKey)
                {
                    args.SuppressEvent = true;
                    _ = Task.Run(() => ProcessKeyPressAsync(args.Data.KeyCode));
                }
            }
        }

        private async Task ProcessKeyPressAsync(KeyCode keyCode)
        {
            if (await _semaphore.WaitAsync(0))
            {
                try
                {
                    IntPtr activeWindowHandle = GetForegroundWindow();
                    if (activeWindowHandle != IntPtr.Zero)
                    {
                        ClipboardContent clipboardContent = GetSelectedText(activeWindowHandle);
                        if (!clipboardContent.Success)
                        {
                            Log(LogType.Warning, "No text was selected");
                            return;
                        }
                        OnTextCaptured(clipboardContent);
                    }
                    else
                    {
                        Log(LogType.Warning, "No active window found.");
                    }
                }
                catch (Exception ex)
                {
                    Log(LogType.Error, $"Error processing key press: {ex.Message}");
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            else
            {
                Log(LogType.Error, "A capture operation is already in progress.");
            }
        }

        private ClipboardContent GetSelectedText(IntPtr hwnd)
        {
            try
            {
                SetForegroundWindow(hwnd);
                Thread.Sleep(_settingsService.WindowActivationDelay); // Consider making this configurable

                WindowsClipboard.ClearClipboard();
                Thread.Sleep(_settingsService.ClipboardClearDelay);  // Consider making this configurable

                KeyboardSimulator.SimulateCtrlC();
                if (!WindowsClipboard.ClipWait(_settingsService.ClipboardWaitTimeout))
                {
                    Log(LogType.Error, "Clipboard timeout."); // Report the timeout
                    return new ClipboardContent();
                }

                return WindowsClipboard.GetClipboardContent();
            }
            catch (Exception ex)
            {
                Log(LogType.Error, $"Error getting selected text: {ex.Message}");
                return new ClipboardContent();
            }
        }

        protected virtual void OnTextCaptured(ClipboardContent clipboardContent)
        {
            TextCaptured?.Invoke(this, new ClipboardCapturedEventArgs(clipboardContent));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    Stop(); // Stop the hook
                    _semaphore.Dispose();
                }

                _isDisposed = true;
            }
        }
    }

    public class LogMessageEventArgs : EventArgs
    {
        public LogType Level { get; }
        public string Message { get; }

        public LogMessageEventArgs(LogType level, string message)
        {
            Level = level;
            Message = message;
        }
    }
}
