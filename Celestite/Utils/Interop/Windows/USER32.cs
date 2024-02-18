using System.Runtime.InteropServices;

namespace Celestite.Utils.Interop.Windows
{
    public static partial class USER32
    {
        private const string DllName = "User32";

        [LibraryImport(DllName, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        public static partial HWND CreateWindowExW(
            WINDOWSTYLES_EX dwExStyle,
            [MarshalAs(UnmanagedType.LPWStr)] string lpClassName,
            [MarshalAs(UnmanagedType.LPWStr)] string lpWindowName,
            WINDOWSTYLES dwStyle,
            int x,
            int y,
            int nWidth,
            int nHeight,
            HWND hWndParent,
            nint hMenu,
            HINSTANCE hInstance,
            nint lpParam
        );

        [LibraryImport(DllName, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool DestroyWindow(HWND hWnd);

        [LibraryImport(DllName, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool GetClientRect(HWND hWnd, out RECT lpRect);
    }
}
