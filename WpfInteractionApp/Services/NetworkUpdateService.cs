using Contextualizer.Core;
using Contextualizer.PluginContracts;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Security.AccessControl;
using System.Security.Principal;

namespace WpfInteractionApp.Services
{
    public class NetworkUpdateService
    {
        private readonly string _currentVersion;
        private readonly string _networkUpdatePath;
        private readonly string _versionInfoFile = "version.json";
        private readonly string _changeLogFile = "changelog.txt";
        private readonly SettingsService _settingsService;
        private readonly IUserInteractionService? _userInteractionService;

        public NetworkUpdateService(SettingsService settingsService, string networkUpdatePath = @"\\server\share\Contextualizer\Updates", IUserInteractionService? userInteractionService = null)
        {
            _settingsService = settingsService;
            _currentVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0.0";
            _networkUpdatePath = networkUpdatePath;
            _userInteractionService = userInteractionService ?? ServiceLocator.SafeGet<IUserInteractionService>();
        }

        public async Task<NetworkUpdateInfo?> CheckForUpdatesAsync()
        {
            try
            {
                // Check if network path is accessible
                if (!IsNetworkPathAccessible(_networkUpdatePath))
                {
                    return new NetworkUpdateInfo 
                    { 
                        IsUpdateAvailable = false, 
                        CurrentVersion = _currentVersion,
                        ErrorMessage = "Network update path not accessible"
                    };
                }

                var versionFilePath = Path.Combine(_networkUpdatePath, _versionInfoFile);
                
                if (!File.Exists(versionFilePath))
                {
                    return new NetworkUpdateInfo 
                    { 
                        IsUpdateAvailable = false, 
                        CurrentVersion = _currentVersion,
                        ErrorMessage = "Version file not found on network share"
                    };
                }

                // Read version info from network
                var versionJson = await File.ReadAllTextAsync(versionFilePath, System.Text.Encoding.UTF8);
                var versionInfo = JsonSerializer.Deserialize<NetworkVersionInfo>(versionJson);

                if (versionInfo != null && !string.IsNullOrEmpty(versionInfo.Version))
                {
                    if (IsNewerVersion(versionInfo.Version, _currentVersion))
                    {
                        // Check if executable exists
                        var executablePath = Path.Combine(_networkUpdatePath, versionInfo.ExecutableFileName);
                        
                        if (File.Exists(executablePath))
                        {
                            // Read changelog if available
                            var changelogPath = Path.Combine(_networkUpdatePath, _changeLogFile);
                            string changelog = "";
                            
                            if (File.Exists(changelogPath))
                            {
                                changelog = await File.ReadAllTextAsync(changelogPath, System.Text.Encoding.UTF8);
                            }

                            var fileInfo = new FileInfo(executablePath);

                            return new NetworkUpdateInfo
                            {
                                IsUpdateAvailable = true,
                                CurrentVersion = _currentVersion,
                                LatestVersion = versionInfo.Version,
                                NetworkPath = executablePath,
                                ReleaseNotes = changelog,
                                ReleaseDate = versionInfo.ReleaseDate,
                                FileSize = fileInfo.Length,
                                IsMandatory = versionInfo.IsMandatory,
                                MinimumRequiredVersion = versionInfo.MinimumRequiredVersion
                            };
                        }
                        else
                        {
                            return new NetworkUpdateInfo 
                            { 
                                IsUpdateAvailable = false, 
                                CurrentVersion = _currentVersion,
                                ErrorMessage = $"Update executable not found: {versionInfo.ExecutableFileName}"
                            };
                        }
                    }
                }

                return new NetworkUpdateInfo 
                { 
                    IsUpdateAvailable = false, 
                    CurrentVersion = _currentVersion 
                };
            }
            catch (UnauthorizedAccessException)
            {
                return new NetworkUpdateInfo 
                { 
                    IsUpdateAvailable = false, 
                    CurrentVersion = _currentVersion,
                    ErrorMessage = "Access denied to network update path"
                };
            }
            catch (DirectoryNotFoundException)
            {
                return new NetworkUpdateInfo 
                { 
                    IsUpdateAvailable = false, 
                    CurrentVersion = _currentVersion,
                    ErrorMessage = "Network update directory not found"
                };
            }
            catch (Exception ex)
            {
                return new NetworkUpdateInfo 
                { 
                    IsUpdateAvailable = false, 
                    CurrentVersion = _currentVersion,
                    ErrorMessage = $"Update check failed: {ex.Message}"
                };
            }
        }

        public async Task<bool> InstallNetworkUpdateAsync(NetworkUpdateInfo updateInfo, 
            IProgress<CopyProgress>? progress = null)
        {
            try
            {
                var currentExePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(currentExePath)) return false;

                var tempPath = Path.GetTempPath();
                var tempUpdatePath = Path.Combine(tempPath, $"Contextualizer_Update_{Guid.NewGuid()}.exe");

                // Copy file from network to temp with progress
                await CopyFileWithProgressAsync(updateInfo.NetworkPath, tempUpdatePath, progress);

                // Verify copied file
                var sourceInfo = new FileInfo(updateInfo.NetworkPath);
                var tempInfo = new FileInfo(tempUpdatePath);

                if (sourceInfo.Length != tempInfo.Length)
                {
                    File.Delete(tempUpdatePath);
                    return false;
                }

                // Install update
                return await InstallUpdateFromTempAsync(tempUpdatePath, currentExePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Network update installation failed: {ex.Message}");
                return false;
            }
        }

        private async Task CopyFileWithProgressAsync(string sourcePath, string destPath, 
            IProgress<CopyProgress>? progress = null)
        {
            const int bufferSize = 1024 * 1024; // 1MB buffer
            var buffer = new byte[bufferSize];

            using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
            using var destStream = new FileStream(destPath, FileMode.Create, FileAccess.Write);

            var totalBytes = sourceStream.Length;
            var totalBytesRead = 0L;

            int bytesRead;
            while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await destStream.WriteAsync(buffer, 0, bytesRead);
                totalBytesRead += bytesRead;

                progress?.Report(new CopyProgress
                {
                    BytesCopied = totalBytesRead,
                    TotalBytes = totalBytes,
                    ProgressPercentage = (int)((totalBytesRead * 100) / totalBytes)
                });
            }
        }

        private async Task<bool> InstallUpdateFromTempAsync(string tempUpdatePath, string currentExePath)
        {
            try
            {
                var backupPath = currentExePath + ".backup";
                
                // Get network update script path from settings
                var networkScriptPath = _settingsService.Settings.UISettings.NetworkUpdateSettings.UpdateScriptPath;
                
                // Check if network script exists
                if (!File.Exists(networkScriptPath))
                {
                    _userInteractionService?.ShowNotification(
                        $"Network update script not found: {networkScriptPath}",
                        LogType.Error,
                        "Update Script Missing",
                        durationInSeconds: 12,
                        onActionClicked: null);
                    return false;
                }

                // Ask confirmation via UI (React) if available; otherwise, default to "no" to avoid surprise installs.
                var confirmed = _userInteractionService != null &&
                                await _userInteractionService.ShowConfirmationAsync(
                                    "Install Network Update",
                                    "Network update is ready to install.\n\nThe application will close and restart automatically.\n\nInstall the update now?");

                if (confirmed)
                {
                    // Run network update script with parameters
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = networkScriptPath,
                        Arguments = $"\"{currentExePath}\" \"{tempUpdatePath}\" \"{backupPath}\"",
                        UseShellExecute = true,
                        Verb = "runas" // Run as administrator if needed
                    });

                    try { System.Windows.Application.Current.Shutdown(); } catch { /* ignore */ }
                    return true;
                }

                // Clean up temp file if user cancelled
                File.Delete(tempUpdatePath);
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Network update installation failed: {ex.Message}");
                return false;
            }
        }

        private bool IsNetworkPathAccessible(string networkPath)
        {
            try
            {
                return Directory.Exists(networkPath);
            }
            catch
            {
                return false;
            }
        }

        private bool IsNewerVersion(string latestVersion, string currentVersion)
        {
            try
            {
                var latest = new Version(latestVersion);
                var current = new Version(currentVersion);
                return latest > current;
            }
            catch
            {
                return false;
            }
        }

        public bool TestNetworkAccess()
        {
            try
            {
                if (!IsNetworkPathAccessible(_networkUpdatePath))
                    return false;

                // Try to access version file
                var versionFilePath = Path.Combine(_networkUpdatePath, _versionInfoFile);
                return File.Exists(versionFilePath);
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GetNetworkUpdateStatusAsync()
        {
            try
            {
                if (!IsNetworkPathAccessible(_networkUpdatePath))
                    return $"❌ Network path not accessible: {_networkUpdatePath}";

                var versionFilePath = Path.Combine(_networkUpdatePath, _versionInfoFile);
                
                if (!File.Exists(versionFilePath))
                    return $"⚠️ Version file missing: {versionFilePath}";

                var versionJson = await File.ReadAllTextAsync(versionFilePath, System.Text.Encoding.UTF8);
                var versionInfo = JsonSerializer.Deserialize<NetworkVersionInfo>(versionJson);

                if (versionInfo != null)
                {
                    var executablePath = Path.Combine(_networkUpdatePath, versionInfo.ExecutableFileName);
                    var executableExists = File.Exists(executablePath);

                    return $"✅ Network update available\n" +
                           $"   Latest Version: {versionInfo.Version}\n" +
                           $"   Current Version: {_currentVersion}\n" +
                           $"   Executable: {(executableExists ? "✅" : "❌")} {versionInfo.ExecutableFileName}\n" +
                           $"   Release Date: {versionInfo.ReleaseDate:yyyy-MM-dd}\n" +
                           $"   Mandatory: {(versionInfo.IsMandatory ? "Yes" : "No")}";
                }

                return "⚠️ Invalid version file format";
            }
            catch (Exception ex)
            {
                return $"❌ Error: {ex.Message}";
            }
        }
    }

    public class NetworkUpdateInfo
    {
        public bool IsUpdateAvailable { get; set; }
        public string CurrentVersion { get; set; } = "";
        public string LatestVersion { get; set; } = "";
        public string NetworkPath { get; set; } = "";
        public string ReleaseNotes { get; set; } = "";
        public DateTime ReleaseDate { get; set; }
        public long FileSize { get; set; }
        public bool IsMandatory { get; set; }
        public string? MinimumRequiredVersion { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class CopyProgress
    {
        public long BytesCopied { get; set; }
        public long TotalBytes { get; set; }
        public int ProgressPercentage { get; set; }
    }

    // Network share version info format
    public class NetworkVersionInfo
    {
        public string Version { get; set; } = "";
        public string ExecutableFileName { get; set; } = "Contextualizer.exe";
        public DateTime ReleaseDate { get; set; } = DateTime.Now;
        public bool IsMandatory { get; set; } = false;
        public string? MinimumRequiredVersion { get; set; }
        public string Description { get; set; } = "";
        public string[] Features { get; set; } = Array.Empty<string>();
        public string[] BugFixes { get; set; } = Array.Empty<string>();
    }
}
