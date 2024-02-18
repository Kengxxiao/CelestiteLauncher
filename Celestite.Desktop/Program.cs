using System;
using System.IO.Pipes;
using System.Runtime;
using System.Text;
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
    public static void Main(string[] args)
    {
        if (SingletonInstanceHelper.IsRunning())
        {
            using var pipeClient = new NamedPipeClientStream(".", $"{Environment.UserName}_celestite", PipeDirection.Out);
            pipeClient.Connect();
            var bytes = Encoding.UTF8.GetBytes(Environment.CommandLine);
            pipeClient.Write(bytes, 0, bytes.Length);
            Environment.Exit(0);
            return;
        }
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
