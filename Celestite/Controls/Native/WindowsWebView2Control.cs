using System;
using Avalonia.Controls;
using Avalonia.Platform;
using Celestite.Network.CelestiteBypassCore;
using Celestite.Utils.Interop.Windows;

namespace Celestite.Controls.Native
{
    public class WindowsWebView2Control : NativeControlHost
    {
        private PlatformHandle? _platformHandle;
        public nint NativeWindowPtr { get; set; } = IntPtr.Zero;

        private double RenderScaling { get; set; } = 1;

        static WindowsWebView2Control()
        {
            var userFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WebView2Cache");
            Carleen.CreateWebView2Environment(string.Empty, userFolder, HttpHelperImplementation.HttpClientImpl3());
        }

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);
            if (NativeWindowPtr == IntPtr.Zero) return;
            var widthWithDpi = (int)Math.Floor(e.NewSize.Width * RenderScaling);
            var heightWithDpi = (int)Math.Floor(e.NewSize.Height * RenderScaling);
            Carleen.OnNativeControlSizeChanged(NativeWindowPtr, new RECT { left = 0, top = 0, right = widthWithDpi, bottom = heightWithDpi });
        }

        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            USER32.GetClientRect(parent.Handle, out var lpRect);
            var window = USER32.CreateWindowExW(WINDOWSTYLES_EX.WS_EX_TRANSPARENT, "static", string.Empty, WINDOWSTYLES.WS_VISIBLE | WINDOWSTYLES.WS_CHILD, 0, 0, lpRect.right - lpRect.left, lpRect.bottom - lpRect.top, parent.Handle, IntPtr.Zero, new HMODULE { Handle = parent.Handle }, IntPtr.Zero);

            NativeWindowPtr = Carleen.CreateNativeWindow(window);
            _platformHandle = new PlatformHandle(window, null);

            RenderScaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;

            return _platformHandle;
        }

        protected override void DestroyNativeControlCore(IPlatformHandle control)
        {
            if (_platformHandle != null)
                USER32.DestroyWindow(_platformHandle.Handle);
            if (NativeWindowPtr != IntPtr.Zero)
                Carleen.DestroyNativeWindow(NativeWindowPtr);
        }
    }
}
