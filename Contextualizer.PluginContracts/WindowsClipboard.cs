using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    public class WindowsClipboard
    {
        public static async Task SetTextAsync(string text, CancellationToken cancellation)
        {
            await TryOpenClipboardAsync(cancellation);

            InnerSet(text);
        }

        public static void SetText(string text)
        {
            TryOpenClipboard();

            InnerSet(text);
        }

        static void InnerSet(string text)
        {
            EmptyClipboard();
            IntPtr hGlobal = default;
            try
            {
                var bytes = (text.Length + 1) * 2;
                hGlobal = Marshal.AllocHGlobal(bytes);

                if (hGlobal == default)
                {
                    ThrowWin32();
                }

                var target = GlobalLock(hGlobal);

                if (target == default)
                {
                    ThrowWin32();
                }

                try
                {
                    Marshal.Copy(text.ToCharArray(), 0, target, text.Length);
                }
                finally
                {
                    GlobalUnlock(target);
                }

                if (SetClipboardData(cfUnicodeText, hGlobal) == default)
                {
                    ThrowWin32();
                }

                hGlobal = default;
            }
            finally
            {
                if (hGlobal != default)
                {
                    Marshal.FreeHGlobal(hGlobal);
                }

                CloseClipboard();
            }
        }

        public static bool ClipWait(int seconds)
        {
            int timeout = seconds * 1000; // Milisaniyeye çevir
            int elapsed = 0;

            while (elapsed < timeout)
            {
                if (IsClipboardFormatAvailable(cfUnicodeText) || IsClipboardFormatAvailable(CF_HDROP))
                    return true;

                Thread.Sleep(50);
                elapsed += 50;
            }

            return false;
        }

        static async Task TryOpenClipboardAsync(CancellationToken cancellation)
        {
            var num = 10;
            while (true)
            {
                if (OpenClipboard(default))
                {
                    break;
                }

                if (--num == 0)
                {
                    ThrowWin32();
                }

                await Task.Delay(100, cancellation);
            }
        }

        static void TryOpenClipboard()
        {
            var num = 10;
            while (true)
            {
                if (OpenClipboard(default))
                {
                    break;
                }

                if (--num == 0)
                {
                    ThrowWin32();
                }

                Thread.Sleep(100);
            }
        }

        public static void ClearClipboard()
        {
            TryOpenClipboard();
            EmptyClipboard();
            CloseClipboard();
        }

        public static async Task<string?> GetTextAsync(CancellationToken cancellation)
        {
            if (!IsClipboardFormatAvailable(cfUnicodeText))
            {
                return null;
            }
            await TryOpenClipboardAsync(cancellation);

            return InnerGet();
        }

        public static string? GetText()
        {
            if (!IsClipboardFormatAvailable(cfUnicodeText))
            {
                return null;
            }
            TryOpenClipboard();

            return InnerGet();
        }

        public static ClipboardContent GetClipboardContent()
        {
            ClipboardContent content = new ClipboardContent();

            if (IsClipboardFormatAvailable(cfUnicodeText))
            {
                content.Success = true;
                content.Text = GetText() ?? string.Empty;
                content.IsText = true;
            }
            else if(IsClipboardFormatAvailable(CF_HDROP))
            {
                content.Success = true;
                content.Files = GetFiles() ?? Array.Empty<string>();
                content.IsFile = true;
            }
            else
            {
                content.Text = string.Empty;
                content.IsText = false;
                content.IsFile = false;
                content.Success = false;
            }

            return content;
        }

        static string? InnerGet()
        {
            IntPtr handle = default;

            IntPtr pointer = default;
            try
            {
                handle = GetClipboardData(cfUnicodeText);
                if (handle == default)
                {
                    return null;
                }

                pointer = GlobalLock(handle);
                if (pointer == default)
                {
                    return null;
                }

                var size = GlobalSize(handle);
                var buff = new byte[size];

                Marshal.Copy(pointer, buff, 0, size);

                return Encoding.Unicode.GetString(buff).TrimEnd('\0');
            }
            finally
            {
                if (pointer != default)
                {
                    GlobalUnlock(handle);
                }

                CloseClipboard();
            }
        }

        public static async Task<string[]?> GetFilesAsync(CancellationToken cancellation)
        {
            if (!IsClipboardFormatAvailable(CF_HDROP))
            {
                return null;
            }

            await TryOpenClipboardAsync(cancellation);
            return GetFilesFromClipboard();
        }

        public static string[]? GetFiles()
        {
            if (!IsClipboardFormatAvailable(CF_HDROP))
            {
                return null;
            }

            TryOpenClipboard();
            return GetFilesFromClipboard() ?? Array.Empty<string>();
        }

        // Dosya okuma işlemi (Windows API)
        private static string[]? GetFilesFromClipboard()
        {
            IntPtr handle = GetClipboardData(CF_HDROP);
            if (handle == IntPtr.Zero)
            {
                return null;
            }

            int fileCount = DragQueryFile(handle, 0xFFFFFFFF, null, 0);  // Get number of files
            string[] files = new string[fileCount];
            for (int i = 0; i < fileCount; i++)
            {
                int length = DragQueryFile(handle, (uint)i, null, 0);  // Get the length of the file path
                StringBuilder filePath = new StringBuilder(length + 1);
                DragQueryFile(handle, (uint)i, filePath, (uint)filePath.Capacity);
                files[i] = filePath.ToString();
            }
            return files;
        }

        const uint cfUnicodeText = 13;
        private const uint CF_HDROP = 15;
        static void ThrowWin32()
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        [DllImport("User32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsClipboardFormatAvailable(uint format);

        [DllImport("User32.dll", SetLastError = true)]
        static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetClipboardData(uint uFormat, IntPtr data);

        [DllImport("user32.dll")]
        static extern bool EmptyClipboard();

        [DllImport("Kernel32.dll", SetLastError = true)]
        static extern int GlobalSize(IntPtr hMem);

        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int DragQueryFile(IntPtr hDrop, uint iFile, StringBuilder lpszFile, uint cch);

    }
}
