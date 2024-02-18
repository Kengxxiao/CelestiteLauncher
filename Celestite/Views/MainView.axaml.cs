using Avalonia;
using Avalonia.Controls;
using Celestite.Network;
using Celestite.ViewModels;

namespace Celestite.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        DmmGamePlayerApiResult.NotBroadcastErrorOccured += (_, errorCodeArgs) =>
        {

        };
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        NavigationFactory.OuterInstance.Navigate(NavigationFactory.Login);
    }
}
