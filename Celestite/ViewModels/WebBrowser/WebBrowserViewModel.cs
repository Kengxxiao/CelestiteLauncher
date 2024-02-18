using System;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Threading;
using Celestite.I18N;
using Celestite.Utils;
using Celestite.Utils.Interop.Windows;
using Celestite.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cysharp.Threading.Tasks;
using ZeroLog;

namespace Celestite.ViewModels.WebBrowser
{
    public partial class WebBrowserViewModel : ViewModelBase
    {
        [ObservableProperty]
        public AvaloniaList<BrowserTab> _browserTabs = [];

        [ObservableProperty]
        private AvaloniaDictionary<int, BrowserTab> _browserTabsDict = [];

        internal nint NativeWindowPtr { get; set; } = IntPtr.Zero;
        private int _maxTabId = 1;

        [ObservableProperty]
        private BrowserTab _selectedTabData = new();

        [RelayCommand]
        private void GoForward() => Carleen.AvaGoForward(NativeWindowPtr);
        [RelayCommand]
        private void GoBack() => Carleen.AvaGoBack(NativeWindowPtr);
        [RelayCommand]
        private void Refresh() => Carleen.AvaRefresh(NativeWindowPtr);
        [RelayCommand]
        private void Navigate()
        {
            var str = SelectedTabData.Source;
            if (Uri.TryCreate(str, new UriCreationOptions(), out var uri))
            {
                if (string.IsNullOrEmpty(uri.Scheme))
                    str = "https://" + str;
            }
            else str = "https://" + str;
            Carleen.AvaNavigate(NativeWindowPtr, str);
        }

        [RelayCommand]
        private void FullScreen()
        {
            ;
        }

        private static readonly Carleen.TabNavigatingStatusChangedCallback? _tabNavigatingStatusChangedCallback;
        private static readonly Carleen.TabHistoryUpdateCallback? _tabHistoryUpdateCallback;
        private static readonly Carleen.TabDocumentTitleChangedCallback? _tabDocumentTitleChangedCallback;
        private static readonly Carleen.NewWindowRequestedCallback? _newWindowRequestedCallback;

        private static readonly Carleen.UriProcessedCallback? _uriProcessedCallback;
        private static readonly Carleen.LogCallback? _logCallback;

        private static readonly AvaloniaDictionary<nint, WebBrowserViewModel> _viewModel = [];

        public void RemoveDict(int id) => BrowserTabsDict.Remove(id);

        public void MoveTab(BrowserTab tab)
        {
            BrowserTabs.Add(tab);
            BrowserTabsDict.Add(tab.Id, tab);
        }

        static WebBrowserViewModel()
        {
            var logger = LogManager.GetLogger("WebView");
            _logCallback = new((level, logString) =>
            {
                switch (level)
                {
                    case 1:
                        logger.Info(logString);
                        break;
                    case 2:
                        logger.Warn(logString);
                        break;
                    case 3:
                        logger.Error(logString);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            });
            Carleen.InitLogCallback(_logCallback);

            _uriProcessedCallback = new((uri) =>
            {
                var uri2 = new Uri(uri);
                if (uri2.Scheme == "dmmgameplayer")
                {
                    var productId = uri2.Authority;
                    var param = uri2.AbsolutePath.Split('/');
                    if (param.Length < 3 || !string.IsNullOrEmpty(param[0]) || param[1] != "cl")
                    {
                        logger.Warn($"Unsupported uri: {uri}");
                        return;
                    }
                    var generalType = param[2];

                    switch (generalType)
                    {
                        case "general":
                            LaunchHelper.LaunchGame(productId, Network.Models.TApiGameType.GCL, checkGameExist: true).Forget();
                            break;
                        case "adult":
                            LaunchHelper.LaunchGame(productId, Network.Models.TApiGameType.ACL, checkGameExist: true).Forget();
                            break;
                        default:
                            logger.Warn($"Unsupported uri: {uri}");
                            break;
                    }
                }
            }
            );
            Carleen.InitUriProcessedCallback(_uriProcessedCallback);

            _tabNavigatingStatusChangedCallback = new((pNativeWindow, tabId, uri, reload) =>
            {
                if (_viewModel.TryGetValue(pNativeWindow, out var webBrowserViewModel))
                {
                    if (webBrowserViewModel.BrowserTabsDict.TryGetValue(tabId, out var tab))
                    {
                        if (!string.IsNullOrEmpty(uri)) tab.Source = uri;
                        tab.ToReload = reload;
                    }
                }
            });
            Carleen.InitTabNavigationStatusChangedCallback(_tabNavigatingStatusChangedCallback);

            _tabHistoryUpdateCallback = new((pNativeWindow, tabId, canGoForward, canGoBack) =>
            {
                if (_viewModel.TryGetValue(pNativeWindow, out var webBrowserViewModel))
                {
                    if (webBrowserViewModel.BrowserTabsDict.TryGetValue(tabId, out var tab))
                    {
                        tab.CanGoForawrd = canGoForward;
                        tab.CanGoBack = canGoBack;
                    }
                }
            });
            Carleen.InitTabHistoryUpdateCallback(_tabHistoryUpdateCallback);

            _tabDocumentTitleChangedCallback = new((pNativeWindow, tabId, documentTitle) =>
            {
                if (_viewModel.TryGetValue(pNativeWindow, out var webBrowserViewModel))
                {
                    if (webBrowserViewModel.BrowserTabsDict.TryGetValue(tabId, out var tab))
                        tab.Title = documentTitle;
                }
            });
            Carleen.InitTabDocumentTitleChangedCallback(_tabDocumentTitleChangedCallback);

            _newWindowRequestedCallback = new((pNativeWindow, uri, pCallerTab) =>
            {
                if (_viewModel.TryGetValue(pNativeWindow, out var webBrowserViewModel))
                {
                    webBrowserViewModel.CreateNewTab(uri, pCallerTab);
                }
            });
            Carleen.InitNewWindowRequestedCallback(_newWindowRequestedCallback);
        }

        public void InitFromView(nint pNativeWindow, string createTabUrl)
        {
            NativeWindowPtr = pNativeWindow;
            _viewModel[NativeWindowPtr] = this;
            if (!string.IsNullOrEmpty(createTabUrl)) CreateNewTab(createTabUrl, IntPtr.Zero);
        }

        public void Crash()
        {
            if (NativeWindowPtr != IntPtr.Zero)
                _viewModel.Remove(NativeWindowPtr);
        }

        partial void OnSelectedTabDataChanged(BrowserTab value)
        {
            if (value == null)
            {
                SelectedTabData = new BrowserTab { Id = 0, ToReload = true, CanGoBack = false, CanGoForawrd = false, Source = string.Empty, Title = string.Empty };
                return;
            }
            if (value.Id == 0) return;
            Carleen.AvaSwitchTab(NativeWindowPtr, value.Id);
        }

        public void CreateNewTab(string uri, nint pCallerTab)
        {
            var nextId = _maxTabId++;
            var tab = new BrowserTab { Id = nextId, Parent = new(this), Source = uri };
            BrowserTabsDict[nextId] = tab;
            BrowserTabs.Add(tab);
            tab.JustCreated = true;
            Carleen.AvaCreateTab(NativeWindowPtr, nextId, uri, true, pCallerTab);
            SelectedTabData = tab;
        }

        [RelayCommand]
        private void CreateNewTab() => CreateNewTab("https://www.dmm.com", IntPtr.Zero);

        public static void OpenBrowser(string url)
        {
            if (!OperatingSystem.IsWindows()) return;
            var versionCheckResult = Carleen.VersionCheck();
            if (!versionCheckResult)
            {
                NotificationHelper.Warn(Localization.HigherWebView2VersionRequired, () =>
                {
                    ProcessUtils.OpenExternalLink("https://go.microsoft.com/fwlink/p/?LinkId=2124703", true);
                });
                return;
            }
            Dispatcher.UIThread.Invoke(() =>
            {
                if (_viewModel.Count == 0)
                {
                    var browser = new WebViewBrowser(url);
                    browser.Show();
                }
                else
                    _viewModel.Last()!.Value.CreateNewTab(url, IntPtr.Zero);
            });
        }
    }

    public partial class BrowserTab : ViewModelBase
    {
        [ObservableProperty]
        private int _id;
        [ObservableProperty]
        private string _source = string.Empty;
        [ObservableProperty]
        private bool _canGoForawrd;
        [ObservableProperty]
        private bool _canGoBack;
        [ObservableProperty]
        private bool _toReload;

        [ObservableProperty]
        private string _title = "Loading...";

        internal bool JustCreated = false;
        public WeakReference<WebBrowserViewModel>? Parent { get; set; }

        public void Close()
        {
            if (Parent != null && Parent.TryGetTarget(out var target))
                Carleen.AvaRemoveTab(target.NativeWindowPtr, Id);
        }
    }
}
