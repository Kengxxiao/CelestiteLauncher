using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime;
using Avalonia;
using Celestite.Utils;
using ZeroLog;
using ZeroLog.Appenders;
using ZeroLog.Configuration;
using ZeroLog.Formatting;

namespace Celestite.Desktop;

internal class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    private static Log Logger { get; set; } = null!;

    [STAThread]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(LaunchCommandLine))]
    public static void Main(string[] args)
    {
        if (!CommandLineHelper.ParseLaunchCommand(args).GetAwaiter().GetResult()) return;
        CommandLineHelper.CreatePipe();
        GCSettings.LatencyMode = GCLatencyMode.LowLatency;
        LogManager.Initialize(new ZeroLogConfiguration
        {
            RootLogger =
            {
                Appenders =
                {
                    new DateAndSizeRollingFileAppender(FileUtils.LogFolder)
                    {
                        Formatter = new DefaultFormatter
                        {
                            PrefixPattern = "[%{time}] [THREAD-#%thread/%{level:3}] %logger: " // :hh\\:mm\\:ss
                        },
                        FileNamePrefix = "Celestite",
                        FileExtension = "utc.log"
                    }
                }
            }
        });
        Logger = LogManager.GetLogger("R3");
#if !DEBUG
        try
        {
#endif
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
#if !DEBUG
        }
        catch (Exception ex)
        {
            Logger.Fatal(string.Empty, ex);
        }
#endif
        LogManager.Shutdown();
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace().UseR3(ex =>
            {
                Logger.Error(string.Empty, ex);
            });
    }
}
