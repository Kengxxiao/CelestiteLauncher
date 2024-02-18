using Avalonia;
using Avalonia.Controls;
using Celestite.Network.Models;
using Celestite.ViewModels.Dialogs;
using Celestite.ViewModels.Navigation;
using Celestite.Views;

namespace Celestite.Dialogs.Games;

public partial class GameSettingsDialog : UserControl
{
    public GameSettingsDialog(MyGame gameData)
    {
        InitializeComponent();
        DataContext = new GameSettingsDialogViewModel(gameData);
    }

    // Œ¥ π”√
    public GameSettingsDialog()
    {
        InitializeComponent();
        DataContext = new GameSettingsDialogViewModel(new MyGame
        {
            Title = "testGame"
        });
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (e.Root is not MainWindow) return;
        GameNavigationFactory.Instance.GetFrame().Navigating += GameSettingsDialog_Navigating;

        if (DataContext is GameSettingsDialogViewModel vm)
            vm.ContentInstalled = vm.IsContentInstalled();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (e.Root is not MainWindow) return;
        GameNavigationFactory.Instance.GetFrame().Navigating -= GameSettingsDialog_Navigating;
    }

    private void GameSettingsDialog_Navigating(object sender, FluentAvalonia.UI.Navigation.NavigatingCancelEventArgs e)
    {
        if (DataContext is not GameSettingsDialogViewModel { IsBackEnabled: true })
        {
            e.Cancel = true;
            return;
        }
        GameNavigationFactory.Instance.GetFrame().BackStack.Clear();
    }
}