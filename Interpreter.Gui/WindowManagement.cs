using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace Vltk.Interpreter.Gui
{
    public sealed class WindowManagement
    {
        public sealed record NativeWindow(IntPtr Handle, string Title);

        // Based on https://stackoverflow.com/a/43640787/2928
        public static ICollection<NativeWindow> GetOpenWindows()
        {
            var shellWindow = GetShellWindow();
            var result = new List<NativeWindow>();

            EnumWindows(delegate (IntPtr hwnd, int lparam)
            {
                if (hwnd == shellWindow) return true;
                if (!IsWindowVisible(hwnd)) return true;

                int titleLength = GetWindowTextLength(hwnd);
                if (titleLength == 0) return true;

                var titleBuffer = new StringBuilder(titleLength);
                GetWindowText(hwnd, titleBuffer, titleLength + 1);

                result.Add(new NativeWindow(hwnd, titleBuffer.ToString()));
                return true;
            }, 0);

            return result;
        }

        private delegate bool EnumWindowsProc(IntPtr IntPtr, int lParam);

        [DllImport("USER32.DLL")]
        private static extern bool EnumWindows(EnumWindowsProc enumFunc, int lParam);

        [DllImport("USER32.DLL")]
        private static extern int GetWindowText(IntPtr IntPtr, StringBuilder lpString, int nMaxCount);

        [DllImport("USER32.DLL")]
        private static extern int GetWindowTextLength(IntPtr IntPtr);

        [DllImport("USER32.DLL")]
        private static extern bool IsWindowVisible(IntPtr IntPtr);

        [DllImport("USER32.DLL")]
        private static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, out WindowRect rectangle);

        public struct WindowRect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }

            public int Width => Right - Left;
            public int Height => Bottom - Top;

            public Point TopLeft => new Point(Left, Top);
            public Size Size => new Size(Width, Height);
        }

        public static WindowRect? GetWindowRect(IntPtr hwnd)
        {
            WindowRect rect;
            if (!GetWindowRect(hwnd, out rect))
                return null;

            return rect;
        }
    }
}
