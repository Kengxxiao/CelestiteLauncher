using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Celestite.Utils;
using Celestite.Views;
using Cysharp.Threading.Tasks;
using Splat;
using Splat.ModeDetection;
using ZeroLog;

[assembly: Fody.ConfigureAwait(false)]
namespace Celestite;
public partial class App : Application
{
    private static readonly Log Log = LogManager.GetLogger("App");
    public override void Initialize()
    {
        CommandLineHelper.RegisterUriScheme();
        CommandLineHelper.CreatePipe();
        ModeDetector.OverrideModeDetector(Mode.Run);
        Log.Info("Preparing for Avalonia env");
        ConfigUtils.Init();
        AvaloniaXamlLoader.Load(this);
        // ThemeUtils.ApplyTheme(ConfigUtils.Config.Theme.BaseThemeMode, ConfigUtils.Config.Theme.PrimaryColor, ConfigUtils.Config.Theme.SecondaryColor);
        Log.Info("Avalonia is loaded");

        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            Log.Fatal(string.Empty, args.ExceptionObject as Exception);
        };
        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            Log.Fatal(string.Empty, args.Exception);
        };
        UniTaskScheduler.UnobservedTaskException += (exception) =>
        {
            Log.Fatal(string.Empty, exception);
        };
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
