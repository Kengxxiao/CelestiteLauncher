using System;
using System.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Celestite.Utils.Interop.Windows;
using Celestite.ViewModels.WebBrowser;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;

namespace Celestite.Views;

public partial class WebViewBrowser : AppWindow
{
    private bool IsInited;
    private WebBrowserViewModel VM => (WebBrowserViewModel)DataContext!;
    private string CreateTabUrl { get; init; } = "https://www.dmm.com/";
    public WebViewBrowser()
    {
        InitializeComponent();
        var vm = new WebBrowserViewModel();
        DataContext = vm;
    }

    public WebViewBrowser(string createTabUrl)
    {
        CreateTabUrl = createTabUrl;
        InitializeComponent();
        var vm = new WebBrowserViewModel();
        DataContext = vm;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        if (!IsInited)
        {
            IsInited = true;
            VM.InitFromView(WebViewControl.NativeWindowPtr, CreateTabUrl);
        }

        base.OnLoaded(e);

        if (TitleBar != null)
        {
            TitleBar.ExtendsContentIntoTitleBar = true;
            TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;

            var dragRegion = this.FindControl<Panel>("CustomDragRegion");
            dragRegion!.MinWidth = FlowDirection == Avalonia.Media.FlowDirection.LeftToRight ?
                TitleBar.RightInset : TitleBar.LeftInset;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        VM.Crash();
    }

    private void TabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Item is BrowserTab browserTab)
        {
            browserTab.Close();
            (sender.TabItems as IList)!.Remove(args.Item);
            VM.RemoveDict(browserTab.Id);
        }
    }

    private void TabView_TabItemsChanged(TabView sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs args)
    {
        if (sender.TabItems.Count() == 0)
        {
            Close();
        }
    }

    private void TabView_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
    {
        var s = new WebViewBrowser(string.Empty);

        if (args.Item is BrowserTab browserTab)
        {
            (sender.TabItems as IList)!.Remove(args.Item);
            VM.RemoveDict(browserTab.Id);

            void S_Loaded(object? sender, RoutedEventArgs e)
            {
                s.VM.MoveTab(browserTab);
                browserTab.Parent = new(s.VM);
                Carleen.AvaDropTabOutside(VM.NativeWindowPtr, browserTab.Id, s.VM.NativeWindowPtr);
                s.VM.SelectedTabData = browserTab;
                s.Loaded -= S_Loaded;
            }
            s.Loaded += S_Loaded;
        }
        s.Show();
    }

    public static readonly string DataIdentifier = "CelestiteWebViewTabItem";

    private void TabView_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
    {
        if (sender.TabItems.Count() <= 1) { args.Cancel = true; return; }
        args.Data.SetData(DataIdentifier, args.Tab);
        args.Data.RequestedOperation = DragDropEffects.Move;
    }

    private void TabView_TabStripDrop(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataIdentifier) && e.Data.Get(DataIdentifier) is TabViewItem tvi)
        {
            var destinationTabView = sender as TabView;

            // While the TabView's internal ListView handles placing an insertion point gap, it 
            // doesn't actually hold that position upon drop - meaning you now must calculate
            // the approximate position of where to insert the tab
            int index = -1;

            for (int i = 0; i < destinationTabView!.TabItems.Count(); i++)
            {
                var item = destinationTabView.ContainerFromIndex(i) as TabViewItem;

                if (e.GetPosition(item!).X - item!.Bounds.Width < 0)
                {
                    index = i;
                    break;
                }
            }

            // Now remove the item from the source TabView
            var srcTabView = tvi.FindAncestorOfType<TabView>();
            var srcIndex = srcTabView!.IndexFromContainer(tvi);
            (srcTabView.TabItems as IList)!.RemoveAt(srcIndex);

            // Now add it to the new TabView
            if (index < 0)
            {
                (destinationTabView.TabItems as IList)!.Add(tvi);
            }
            else if (index < destinationTabView.TabItems.Count())
            {
                (destinationTabView.TabItems as IList)!.Insert(index, tvi);
            }

            destinationTabView.SelectedItem = tvi;
            e.Handled = true;

            // Remember, TabItemsChanged won't fire during DragDrop so we need to check
            // here if we should close the window if TabItems.Count() == 0
            if (srcTabView.TabItems.Count() == 0)
            {
                var wnd = srcTabView.FindAncestorOfType<AppWindow>();
                wnd!.Close();
            }
        }
    }
}