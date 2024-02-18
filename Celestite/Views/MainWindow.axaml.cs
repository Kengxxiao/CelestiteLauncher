using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Celestite.Utils;
using Cysharp.Text;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;

namespace Celestite.Views;

public partial class MainWindowWin32
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal unsafe struct RTL_OSVERSIONINFOEX
    {
        internal uint dwOSVersionInfoSize;
        internal uint dwMajorVersion;
        internal uint dwMinorVersion;
        internal uint dwBuildNumber;
        internal uint dwPlatformId;
        internal fixed char szCSDVersion[128];
    }
    [LibraryImport("ntdll")]
    private static partial int RtlGetVersion(ref RTL_OSVERSIONINFOEX lpVersionInformation);

    internal static Version RtlGetVersion()
    {
        var v = new RTL_OSVERSIONINFOEX
        {
            dwOSVersionInfoSize = (uint)Marshal.SizeOf<RTL_OSVERSIONINFOEX>()
        };
        if (RtlGetVersion(ref v) == 0)
            return new Version((int)v.dwMajorVersion, (int)v.dwMinorVersion, (int)v.dwBuildNumber);
        throw new Exception("RtlGetVersion failed!");
    }
}
public partial class MainWindow : AppWindow
{
    private static readonly Version MinAcrylicVersion = new(10, 0, 15063);
    private static readonly Version MinHostBackdropVersion = new(10, 0, 22000);

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        Environment.Exit(0);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        NotificationHelper.Init();
        Title = ZString.Format("Celestite {0}", Build.BuildId);
        Closing += (sender, args) =>
        {
            if (sender is not Window window) return;
            if (!ConfigUtils.GetMinimizeToTray()) return;
            window.Hide();
            window.ShowInTaskbar = false;
            args.Cancel = true;
            GCUtils.CollectGeneration2();
        };
        ContentDialog.ContentDialogWantToShow += (_, _) =>
        {
            WindowTrayHelper.RequestShow();
        };
        if (!OperatingSystem.IsWindows()) return;
        var windowsVersion = MainWindowWin32.RtlGetVersion();
        if (windowsVersion >= MinHostBackdropVersion && ConfigUtils.UseMica())
        {
            TransparencyLevelHint = [WindowTransparencyLevel.Mica];
            Background = null;
        }
        else if (windowsVersion >= MinAcrylicVersion && ConfigUtils.UseAcrylic())
        {
            TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur];
            Background = null;
        }
    }
}
