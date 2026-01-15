using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace WpfInteractionApp.Services
{
    internal static class WindowActivationHelper
    {
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

        public static void BringToFrontBestEffort(Window window)
        {
            if (window == null) return;

            if (!window.Dispatcher.CheckAccess())
            {
                try { window.Dispatcher.Invoke(() => BringToFrontBestEffort(window)); } catch { /* ignore */ }
                return;
            }

            try
            {
                if (window.WindowState == WindowState.Minimized)
                    window.WindowState = WindowState.Normal;

                if (!window.IsVisible)
                    window.Show();

                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero)
                    return;

                try { ShowWindowAsync(hwnd, SW_RESTORE); } catch { /* ignore */ }

                bool attached = false;
                uint fgThread = 0;
                uint thisThread = 0;
                try
                {
                    var fg = GetForegroundWindow();
                    fgThread = fg != IntPtr.Zero ? GetWindowThreadProcessId(fg, out _) : 0;
                    thisThread = GetCurrentThreadId();
                    if (fgThread != 0 && thisThread != 0 && fgThread != thisThread)
                        attached = AttachThreadInput(thisThread, fgThread, true);
                }
                catch { /* ignore */ }

                try { BringWindowToTop(hwnd); } catch { /* ignore */ }
                try { SetForegroundWindow(hwnd); } catch { /* ignore */ }
                try { SetActiveWindow(hwnd); } catch { /* ignore */ }
                try { SetFocus(hwnd); } catch { /* ignore */ }

                try
                {
                    window.Activate();
                    var wasTopmost = window.Topmost;
                    window.Topmost = true;
                    window.Dispatcher.BeginInvoke(new Action(() => window.Topmost = wasTopmost));
                    window.Focus();
                }
                catch { /* ignore */ }

                if (attached)
                {
                    try { AttachThreadInput(thisThread, fgThread, false); } catch { /* ignore */ }
                }

                // If Windows refuses activation (focus-stealing rules), at least flash the taskbar button.
                if (!window.IsActive)
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
    }
}


