using System.Runtime.InteropServices;

namespace Celestite.Utils.Interop.Windows
{
    public partial class Carleen
    {
        private const string DLL_NAME = "Carleen";

        [LibraryImport(DLL_NAME, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        public static partial int GetAvailableVersion(
            [MarshalAs(UnmanagedType.LPWStr)] string browserExecutableFolder,
            [MarshalAs(UnmanagedType.LPWStr)] out string versionInfo);
        [LibraryImport(DLL_NAME, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool VersionCheck();

        // HWND parentHandle, PCWSTR browserExecutableFolder, PCWSTR userDataFolder, PCWSTR additionalBrowserArguments
        [LibraryImport(DLL_NAME, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        public static partial int CreateWebView2Environment(
            [MarshalAs(UnmanagedType.LPWStr)] string browserExecutableFolder,
            [MarshalAs(UnmanagedType.LPWStr)] string userDataFolder,
            [MarshalAs(UnmanagedType.LPWStr)] string additionalBrowserArguments);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void TabHistoryUpdateCallback(nint pNativeWindow, int tabId, bool canGoForward, bool canGoBack);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void TabNavigatingStatusChangedCallback(nint pNativeWindow, int tabId, [MarshalAs(UnmanagedType.LPWStr)] string uri, bool toReload);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void LogCallback(int level, [MarshalAs(UnmanagedType.LPWStr)] string logString);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void TabDocumentTitleChangedCallback(nint pNativeWindow, int tabId, [MarshalAs(UnmanagedType.LPWStr)] string documentTitle);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void NewWindowRequestedCallback(nint pNativeWindow, [MarshalAs(UnmanagedType.LPWStr)] string uri, nint pCallerTab);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void UriProcessedCallback([MarshalAs(UnmanagedType.LPWStr)] string uri);

        [LibraryImport(DLL_NAME)]
        public static partial void InitTabHistoryUpdateCallback(TabHistoryUpdateCallback callback);
        [LibraryImport(DLL_NAME)]
        public static partial void InitTabNavigationStatusChangedCallback(TabNavigatingStatusChangedCallback callback);
        [LibraryImport(DLL_NAME)]
        public static partial void InitLogCallback(LogCallback logCallback);
        [LibraryImport(DLL_NAME)]
        public static partial void InitTabDocumentTitleChangedCallback(TabDocumentTitleChangedCallback logCallback);
        [LibraryImport(DLL_NAME)]
        public static partial void InitNewWindowRequestedCallback(NewWindowRequestedCallback logCallback);
        [LibraryImport(DLL_NAME)]
        public static partial void InitUriProcessedCallback(UriProcessedCallback logCallback);

        [LibraryImport(DLL_NAME)]
        [return: MarshalAs(UnmanagedType.SysInt)]
        public static partial nint CreateNativeWindow(HWND hWnd);
        [LibraryImport(DLL_NAME)]
        public static partial void DestroyNativeWindow(nint pNativeWindow);
        [LibraryImport(DLL_NAME)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool OnNativeControlSizeChanged(nint pNativeWindow, RECT rect);

        [LibraryImport(DLL_NAME)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool AvaCreateTab(nint pNativeWindow, long tabId, [MarshalAs(UnmanagedType.LPWStr)] string navigateUrl, [MarshalAs(UnmanagedType.Bool)] bool shouldBeActive, nint pCallerTab);
        [LibraryImport(DLL_NAME)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool AvaSwitchTab(nint pNativeWindow, long tabId);
        [LibraryImport(DLL_NAME)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool AvaRemoveTab(nint pNativeWindow, long tabId);

        [LibraryImport(DLL_NAME)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool AvaGoForward(nint pNativeWindow);

        [LibraryImport(DLL_NAME)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool AvaGoBack(nint pNativeWindow);

        [LibraryImport(DLL_NAME)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool AvaRefresh(nint pNativeWindow);
        [LibraryImport(DLL_NAME)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool AvaNavigate(nint pNativeWindow, [MarshalAs(UnmanagedType.LPWStr)] string uri);

        [LibraryImport(DLL_NAME)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool AvaDropTabOutside(nint pNativeWindow, long tabId, nint pTargetNativeWindow);

        [LibraryImport(DLL_NAME)]
        public static partial void UpdateCurrentUser([MarshalAs(UnmanagedType.LPWStr)] string email, [MarshalAs(UnmanagedType.LPWStr)] string loginSecureId, [MarshalAs(UnmanagedType.LPWStr)] string loginSessionId);
    }
}
