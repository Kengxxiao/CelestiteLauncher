using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Threading;
using Celestite.Dialogs.Games;
using Celestite.I18N;
using Celestite.Network;
using Celestite.Network.Models;
using Celestite.Utils;
using Celestite.Utils.Interop.Windows;
using Celestite.ViewModels;
using Celestite.ViewModels.Dialogs;
using Celestite.ViewModels.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cysharp.Threading.Tasks;
using FluentAvalonia.UI.Controls;

namespace Celestite.ViewModels.Pages
{
    public enum GameType : int
    {
        Download = 0, Browser = 1
    }

    public class GameTypeConverter : IValueConverter
    {
        public static readonly GameTypeConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is GameType gameType && parameter is string a && int.TryParse(a, out var p))
                return (int)gameType == p;
            throw new NotSupportedException();
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
    public partial class GamesPageViewModel : ViewModelBase
    {
        [ObservableProperty] private AvaloniaList<MyGame> _gamesSource = [];
        [ObservableProperty] private AvaloniaList<MyGame> _games = [];
        [ObservableProperty] private GameType _selectedIndex = GameType.Download;

        [ObservableProperty] private bool _isOnlyShowViewable = true;
        [ObservableProperty] private string _filterText = string.Empty;
        public bool SafeIconEnabled { get; set; } = ConfigUtils.GetSafeFanzaIcon();

        private static AddClientGameDialogViewModel? _permanentAddClientGameDialogViewModel;
        private static AddBrowserGameDialogViewModel? _permanentAddBrowserGameDialogViewModel;

        private static AddClientGameDialogViewModel PermanentAddClientGameDialogViewModel => LazyInitializer.EnsureInitialized(ref _permanentAddClientGameDialogViewModel, () => new());
        private static AddBrowserGameDialogViewModel PermanentAddBrowserGameDialogViewModel => LazyInitializer.EnsureInitialized(ref _permanentAddBrowserGameDialogViewModel, () => new());

        private static ContentDialog? _addGameDialog = null;
        private static ContentDialog? _addBrowserGameDialog = null;
        private static ContentDialog? _agreementDialog = null;
        private static ContentDialog? _hardwareRegDialog = null;
        public static AutoCompleteFilterPredicate<object> AutoCompleteFilter =>
            (search, item) =>
            {
                if (string.IsNullOrEmpty(search) || item is not MyGame x) return false;
                return x.Title.Contains(search, StringComparison.OrdinalIgnoreCase) || x.Romaji.Contains(search, StringComparison.OrdinalIgnoreCase) || x.TitleRuby.Contains(search);
            };

        public static ContentDialog AddGameDialog => LazyInitializer.EnsureInitialized(ref _addGameDialog, () => Dispatcher.UIThread.Invoke(() => new ContentDialog()
        {
            IsVisible = false,
            Title = Localization.AddClientGameDialogTitle,
            PrimaryButtonText = Localization.ButtonOk,
            IsPrimaryButtonEnabled = true,
            CloseButtonText = Localization.ButtonCancel,
            DataContext = PermanentAddClientGameDialogViewModel,
            DefaultButton = ContentDialogButton.Primary,
            Content = new AddClientGameDialog() { DataContext = PermanentAddClientGameDialogViewModel }
        }));
        public static ContentDialog AddBrowserGameDialog => LazyInitializer.EnsureInitialized(ref _addBrowserGameDialog, () => Dispatcher.UIThread.Invoke(() =>
                new ContentDialog()
                {
                    IsVisible = false,
                    Title = Localization.AddBrowserGameDialogTitle,
                    PrimaryButtonText = Localization.ButtonOk,
                    IsPrimaryButtonEnabled = true,
                    CloseButtonText = Localization.ButtonCancel,
                    DataContext = PermanentAddBrowserGameDialogViewModel,
                    DefaultButton = ContentDialogButton.Primary,
                    Content = new AddBrowserGameDialog() { DataContext = PermanentAddBrowserGameDialogViewModel }
                }));
        public static ContentDialog AgreementDialog => LazyInitializer.EnsureInitialized(ref _agreementDialog, () => Dispatcher.UIThread.Invoke(() => new ContentDialog()
        {
            IsVisible = false,
            PrimaryButtonText = Localization.AgreementButtonAgree,
            CloseButtonText = Localization.FormClose,
            IsPrimaryButtonEnabled = true,
            DefaultButton = ContentDialogButton.Primary,
            IsHitTestVisible = false,
            Content = new AgreementCheckDialog()
        }));
        public static ContentDialog HardwareRegDialog => LazyInitializer.EnsureInitialized(ref _hardwareRegDialog, () => Dispatcher.UIThread.Invoke(() => new ContentDialog()
        {
            IsVisible = false,
            Title = Localization.HardwareRegDialogTitle,
            PrimaryButtonText = Localization.HardwareRegButton,
            CloseButtonText = Localization.ButtonCancel,
            IsPrimaryButtonEnabled = true,
            DefaultButton = ContentDialogButton.Primary,
            IsHitTestVisible = false,
            Content = new HardwareRegDialog()
        }));

        public GamesPageViewModel()
        {
            DmmGamePlayerApiHelper.AdultModeChangedEvent += (_, _) =>
            {
                PermanentAddClientGameDialogViewModel.Games.Clear();
                PermanentAddBrowserGameDialogViewModel.Games.Clear();
            };
            DmmGamePlayerApiHelper.ReGetGamesRequiredEvent += (_, _) => Dispatcher.UIThread.Invoke(RemoteUpdate);
            DmmGamePlayerApiHelper.LoginSessionChangedEvent += (_, _) => Dispatcher.UIThread.Invoke(RemoteUpdate);
            MyGame.GameStatusChangedEvent += (_, _) => Dispatcher.UIThread.Invoke(NotifyCollectionChanged);
            ConfigUtils.OnSafeFanzaIconChanged += (_, _) => SafeIconEnabled = ConfigUtils.GetSafeFanzaIcon();

            DmmGamePlayerApiResult.NotBroadcastErrorOccured += async (_, args) =>
            {
                switch (args.ErrorCode)
                {
                    case DmmGamePlayerApiErrorCode.NOT_CONSENT_AGREEMENT:
                        await InvokeNotConsentAgreement(args.ProductId, args.GameType);
                        return;
                    case DmmGamePlayerApiErrorCode.DEVICE_CHECK_FAILURE:
                        await InvokeDeviceAuth();
                        return;
                }
            };

            if (OperatingSystem.IsWindows() && DmmGamePlayerApiHelper.CurrentUserInfo != null)
                Carleen.UpdateCurrentUser(DmmGamePlayerApiHelper.CurrentUserInfo.OpenId, HttpHelper.LoginSecureId, HttpHelper.LoginSessionId);
            DmmGamePlayerApiHelper.UserInfoChangedEvent += (currentUserInfo, _) =>
            {
                if (OperatingSystem.IsWindows() && currentUserInfo is UserInfo userInfo)
                    Carleen.UpdateCurrentUser(userInfo.OpenId, HttpHelper.LoginSecureId, HttpHelper.LoginSessionId);
            };

            GamesSource.CollectionChanged += (_, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Reset)
                    GCUtils.CollectGeneration2();
                GamesShouldBeChanged();
            };

            RemoteUpdate();
        }

        private void GamesShouldBeChanged()
        {
            Games.Clear();
            var games = GamesSource
                .Where(x => x.IsView == IsOnlyShowViewable)
                .OrderByDescending(x => x.IsFavorite)
                .ThenByDescending(x => ConfigUtils.GetGameSettingsForcePin(x.ProductId))
                .Where(x => x.Title.Contains(FilterText, StringComparison.OrdinalIgnoreCase) || x.Romaji.Contains(FilterText, StringComparison.OrdinalIgnoreCase) || x.TitleRuby.Contains(FilterText));
            Games.AddRange(games);
        }

        public static async UniTask InvokeNotConsentAgreement(string productId, TApiGameType gameType)
        {
            var agreement = await DmmGamePlayerApiHelper.GetAgreement(productId);
            if (agreement.Failed) return;
            var vm = new AgreementDialogViewModel(agreement.Value);
            var result = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                AgreementDialog.Title = agreement.Value.AgreementList[0].Title;
                AgreementDialog.DataContext = vm;
                return AgreementDialog.ShowAsync();
            });
            if (result != ContentDialogResult.Primary) return; // 同意
            var resp = await DmmGamePlayerApiHelper.ConfirmAgreement(productId, vm.IsNotification,
                vm.IsProfileApp);
            if (resp.Failed) return;
            await LaunchHelper.LaunchGame(productId, gameType);
        }
        private static async UniTask InvokeDeviceAuth()
        {
            var dataContext = new HardwareRegDialogViewModel();
            var result = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                HardwareRegDialog.DataContext = dataContext;
                return HardwareRegDialog.ShowAsync();
            });
            if (result != ContentDialogResult.Primary) return;
            var result2 =
                await DmmGamePlayerApiHelper.ConfirmHardwareAuthCode(dataContext.Hostname, dataContext.VerificationCode);
            if (result2.Failed) return;
        }

        [RelayCommand]
        private void NotifyCollectionChanged() => GamesShouldBeChanged();
        partial void OnFilterTextChanged(string value) => NotifyCollectionChanged();
        private void RemoteUpdate() => UpdateGameList(SelectedIndex).Forget();

        private CancellationTokenSource? _cancellationTokenSource = null;
        private async UniTask UpdateGameList(GameType gameType)
        {
            GamesSource.Clear();
            Games.Clear();
            if (_cancellationTokenSource != null)
            {
                await _cancellationTokenSource.CancelAsync();
                _cancellationTokenSource.Dispose();
            }
            _cancellationTokenSource = new CancellationTokenSource();
            var gameList = gameType switch
            {
                GameType.Download => await DmmGamePlayerApiHelper.MyGameList(_cancellationTokenSource.Token),
                GameType.Browser => await DmmGamePlayerApiHelper.MyBrowserGameList(_cancellationTokenSource.Token),
                _ => throw new NotImplementedException()
            };
            if (gameList.Failed) return;
            Dispatcher.UIThread.Invoke(() => GamesSource.AddRange(gameList.Value));
        }

        partial void OnSelectedIndexChanged(GameType value)
        {
            UpdateGameList(value).Forget();
        }

        partial void OnIsOnlyShowViewableChanged(bool value)
        {
            GamesShouldBeChanged();
        }

        private const int Limit = 120;

        [RelayCommand]
        private async Task OpenAddClientGameDialog()
        {
            if (PermanentAddClientGameDialogViewModel.Games.Count == 0)
            {
                var search = await DmmGamePlayerApiHelper.Search(REQUEST_FLOOR.FREE, "download:1", limit: Limit, page: 1);
                if (search.Failed) return;
                var list = new List<SearchGameData>(search.Value.List);
                if (search.Value.TotalHitCount > Limit)
                {
                    var tasks = new UniTask<DmmGamePlayerApiResult<Search>>[(int)Math.Ceiling((double)(search.Value.TotalHitCount - Limit) / Limit)];
                    for (var i = 0; i < tasks.Length; i++)
                        tasks[i] = DmmGamePlayerApiHelper.Search(REQUEST_FLOOR.FREE, "download:1", Limit, page: 2 + i);
                    var completed = await UniTask.WhenAll(tasks);
                    if (completed.Any(x => x.Failed)) return;
                    foreach (var dmmGamePlayerApiResult in completed)
                    {
                        list.AddRange(dmmGamePlayerApiResult.Value.List);
                    }
                }
                PermanentAddClientGameDialogViewModel.Games = list;
            }

            if (await Dispatcher.UIThread.InvokeAsync(AddGameDialog.ShowAsync) !=
                ContentDialogResult.Primary) return;
            try
            {
                var validCheck = PermanentAddClientGameDialogViewModel.Games.FirstOrDefault(x =>
                    x.ProductId == PermanentAddClientGameDialogViewModel.GameId);
                if (validCheck != null)
                {
                    var gameType =
                        await DmmGamePlayerApiHelper.GetGameTypeFromOldKeys(
                            PermanentAddClientGameDialogViewModel.GameId,
                            validCheck.Floor, adult: DmmGamePlayerApiHelper.IsAdultMode);
                    if (gameType.Failed) return;
                    var addResult = await DmmGamePlayerApiHelper.AddStoreProductToMyGame(gameType.Value);
                    if (addResult.Failed) return;
                }
            }
            finally
            {
                PermanentAddClientGameDialogViewModel.GameId = string.Empty;
            }
        }

        [RelayCommand]
        private async Task OpenAddBrowserGameDialog()
        {
            if (PermanentAddBrowserGameDialogViewModel.Games.Count == 0)
            {
                var search = await DmmGamePlayerApiHelper.Search(REQUEST_FLOOR.FREE, "browser:1", limit: Limit, page: 1);
                if (search.Failed) return;
                var list = new List<SearchGameData>(search.Value.List.Where(x => !string.IsNullOrEmpty(x.TransformGameId)));
                if (search.Value.TotalHitCount > Limit)
                {
                    var tasks = new UniTask<DmmGamePlayerApiResult<Search>>[(int)Math.Ceiling((double)(search.Value.TotalHitCount - Limit) / Limit)];
                    for (var i = 0; i < tasks.Length; i++)
                        tasks[i] = DmmGamePlayerApiHelper.Search(REQUEST_FLOOR.FREE, "browser:1", Limit, page: 2 + i);
                    var completed = await UniTask.WhenAll(tasks);
                    if (completed.Any(x => x.Failed)) return;
                    foreach (var dmmGamePlayerApiResult in completed)
                    {
                        list.AddRange(dmmGamePlayerApiResult.Value.List.Where(x => !string.IsNullOrEmpty(x.TransformGameId)));
                    }
                }
                PermanentAddBrowserGameDialogViewModel.Games = list;
            }
            if (await Dispatcher.UIThread.InvokeAsync(AddBrowserGameDialog.ShowAsync) !=
                ContentDialogResult.Primary) return;
            var result = await DmmBrowserGameLaunchHelper.TryAddBrowserGame(PermanentAddBrowserGameDialogViewModel.GameId,
                PermanentAddBrowserGameDialogViewModel.IsNotificationAllowed,
                PermanentAddBrowserGameDialogViewModel.IsDisplayAllowed);
            PermanentAddBrowserGameDialogViewModel.GameId = string.Empty;
            if (result)
                DmmGamePlayerApiHelper.ForceRaiseReGetGamesRequiredEvent();
        }
    }

}

namespace Celestite.Network.Models
{
    public partial class MyGame : ViewModelBase
    {
        [RelayCommand]
        private async Task EmulateLaunch() => await LaunchHelper.EmulateLaunch(ProductId, ApiType);

        [RelayCommand]
        private void OpenGameSettings() => GameNavigationFactory.Instance.GetFrame().NavigateFromObject(this);

        [RelayCommand]
        private async Task LaunchGame() => await LaunchHelper.LaunchGame(ProductId, Type, PackageImageUrl);
    }
}