using Avalonia;
using Avalonia.Controls;
using Celestite.ViewModels;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;

namespace Celestite.Pages.Default;

public partial class MainNavigationView : UserControl
{
    public MainNavigationView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        var firstControl = (Nav.MenuItems.ElementAt(0) as NavigationViewItem)!;
        firstControl.IsSelected = true;
        NavigationFactory.Instance.Navigate(firstControl.TagHash);

        FrameView.Navigated += ScrollToHome;
    }

    private void ScrollToHome(object? sender, NavigationEventArgs args) => Scroll.ScrollToHome();

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        FrameView.Navigated -= ScrollToHome;
    }

    private void OnNavigationViewItemInvoked(object sender, NavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer is not NavigationViewItem navigationViewItem) return;
        navigationViewItem.IsSelected = true;
        NavigationFactory.Instance.Navigate(navigationViewItem.TagHash);
    }
}