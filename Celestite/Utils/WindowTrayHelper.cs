using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Celestite.Views;

namespace Celestite.Utils
{
    public class WindowTrayHelper
    {
        internal static bool IsOnTop() => Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime
        {
            MainWindow: MainWindow { IsVisible: true }
        };
        internal static void ReShowWindowCore()
        {
            if (Application.Current!.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime
                {
                    MainWindow: MainWindow mw
                }) return;
            if (mw.IsVisible) return;
            mw.Show();
            mw.ShowInTaskbar = true;
        }

        internal static void RequestShow()
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!IsOnTop())
                    ReShowWindowCore();
            });
        }
    }
}
