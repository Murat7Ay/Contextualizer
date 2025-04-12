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
        private const int ClipboardWaitTimeout = 5;
        private bool _isDisposed;

        public event EventHandler<TextCapturedEventArgs>? TextCaptured;
        public event EventHandler<LogMessageEventArgs>? LogMessage;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private void Log(NotificationType level, string message)
        {
            LogMessage?.Invoke(this, new LogMessageEventArgs(level, message));
        }
        public Task StartAsync()
        {
            if (_hook != null) throw new InvalidOperationException("Hook is already started.");

            _hook = new SimpleGlobalHook();
            _hook.KeyPressed += OnKeyPressed;

            Log(NotificationType.Info, "Press Ctrl+W to capture selected text...");
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
            if (args.RawEvent.Mask.HasMeta() && args.Data.KeyCode == KeyCode.VcW)
            {
                args.SuppressEvent = true;

                // Use Task.Run to avoid blocking the UI thread.
                _ = Task.Run(() => ProcessKeyPressAsync(args.Data.KeyCode));
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
                        string? selectedText = GetSelectedText(activeWindowHandle);
                        if (string.IsNullOrWhiteSpace(selectedText))
                        {
                            Log(NotificationType.Warning, "No text was selected");
                            return;
                        }
                        OnTextCaptured(selectedText);
                    }
                    else
                    {
                        Log(NotificationType.Warning, "No active window found.");
                    }
                }
                catch (Exception ex)
                {
                    Log(NotificationType.Error, $"Error processing key press: {ex.Message}");
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            else
            {
                Log(NotificationType.Error, "A capture operation is already in progress.");
            }
        }

        private string? GetSelectedText(IntPtr hwnd)
        {
            try
            {
                SetForegroundWindow(hwnd);
                Thread.Sleep(100); // Consider making this configurable

                WindowsClipboard.ClearClipboard();
                Thread.Sleep(800);  // Consider making this configurable

                KeyboardSimulator.SimulateCtrlC();
                if (!WindowsClipboard.ClipWait(ClipboardWaitTimeout))
                {
                    Log(NotificationType.Error, "Clipboard timeout."); // Report the timeout
                    return "";
                }

                return WindowsClipboard.GetClipboardContent();
            }
            catch (Exception ex)
            {
                Log(NotificationType.Error, $"Error getting selected text: {ex.Message}");
                return "";
            }
        }


        protected virtual void OnTextCaptured(string text)
        {
            TextCaptured?.Invoke(this, new TextCapturedEventArgs(text));
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
        public NotificationType Level { get; }
        public string Message { get; }

        public LogMessageEventArgs(NotificationType level, string message)
        {
            Level = level;
            Message = message;
        }
    }
}
