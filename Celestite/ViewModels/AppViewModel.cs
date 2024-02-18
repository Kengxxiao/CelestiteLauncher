
using System;
using Avalonia.Controls;
using Avalonia.Threading;
using Celestite.Network.Models;
using Celestite.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Celestite.ViewModels;

public record LaunchCommandLine
{
    public string ProductId { get; set; } = string.Empty;
    public TApiGameType Type { get; set; }
}
public partial class AppViewModel : ViewModelBase
{
    public static string TrayText => $"Celestite\n{Build.BuildId}";
    [ObservableProperty]
    private NativeMenu _menu = [];

    private readonly IRelayCommand _launchGameCommand = new AsyncRelayCommand<LaunchCommandLine>(async line =>
    {
        if (line == null) return;
        await LaunchHelper.LaunchGame(line.ProductId, line.Type);
    });

    private void UpdateNewGame()
    {
        var newMenu = new NativeMenu();
        foreach (var game in ConfigUtils.GetAllInstalledContent())
        {
            var item = new NativeMenuItem(game.ProductId)
            {
                Command = _launchGameCommand,
                CommandParameter = new LaunchCommandLine { ProductId = game.ProductId, Type = game.GameType }
            };
            newMenu.Add(item);
        }

        Menu = newMenu;
    }

    public AppViewModel()
    {
        UpdateNewGame();
        ConfigUtils.OnGameContentChanged += (_, _) => Dispatcher.UIThread.Invoke(UpdateNewGame);
    }

    [RelayCommand]
    internal static void ReShowMainWindow() => WindowTrayHelper.ReShowWindowCore();

    [RelayCommand]
    internal static void Exit() => Environment.Exit(0);
}