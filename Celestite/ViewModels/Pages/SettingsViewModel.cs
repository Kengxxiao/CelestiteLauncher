using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Celestite.Dialogs;
using Celestite.I18N;
using Celestite.Network;
using Celestite.Network.Models;
using Celestite.Utils;
using Celestite.ViewModels.Dialogs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cysharp.Text;
using FluentAvalonia.UI.Controls;

namespace Celestite.ViewModels.Pages
{
    public class LocaleObject
    {
        public string LocaleCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
    public partial class SettingsViewModel : ViewModelBase
    {
        [ObservableProperty] private string _defaultGameInstallFolder = ConfigUtils.DmmGamesDefaultInstallFolder;

        public string BuildData => ZString.Format("{0} ({1})", Build.BuildId, RuntimeInformation.FrameworkDescription);

        [ObservableProperty] private bool _isAdult;
        [ObservableProperty] private bool _isDeviceAuthAll;

        [ObservableProperty] private bool _useMica = ConfigUtils.UseMica();
        [ObservableProperty] private bool _useAcrylic = ConfigUtils.UseAcrylic();
        [ObservableProperty] private bool _minimizeToTray = ConfigUtils.GetMinimizeToTray();
        [ObservableProperty] private bool _himariProxy = ConfigUtils.GetHimariProxyStatus();
        [ObservableProperty] private bool _banSystemProxy = ConfigUtils.GetBypassSystemProxy();
        [ObservableProperty] private string _languageCode = ConfigUtils.GetLanguageCode();
        [ObservableProperty] private bool _enableEmbeddedWebView = ConfigUtils.GetEnableEmbeddedWebView();
        [ObservableProperty] private bool _disableIFrameEx = ConfigUtils.GetDisableIFrameEx();

        public LocaleObject[] SupportedLanguage => [
            new LocaleObject { LocaleCode = "zh-CN", Name = "简体中文 (zh-CN)" },
            new LocaleObject { LocaleCode = "en-US", Name = "English (en-US)" }
        ];

        public SettingsViewModel()
        {
            UpdateFromUserInfo(DmmGamePlayerApiHelper.CurrentUserInfo!);
            DmmGamePlayerApiHelper.UserInfoChangedEvent += (currentUserInfo, _) =>
            {
                if (currentUserInfo is not UserInfo userInfo) return;
                UpdateFromUserInfo(userInfo);
            };
        }

        private void UpdateFromUserInfo(UserInfo userInfo)
        {
            IsAdult = userInfo.IsStoreStartToAdult;
            IsDeviceAuthAll = userInfo.IsDeviceAuthenticationAll;
        }

        [ObservableProperty]
        private long _pictureCacheSize = CalcCacheSize();

        [ObservableProperty]
        private string _winePath = !OperatingSystem.IsWindows() ? ConfigUtils.GetWinePath() : string.Empty;

        private static ContentDialog? _hardwareListDialog = null;

        public static ContentDialog HardwareListDialog => LazyInitializer.EnsureInitialized(ref _hardwareListDialog,
            () => Dispatcher.UIThread.Invoke(
                () => new ContentDialog()
                {
                    IsVisible = false,
                    Title = Localization.HardwareRejectTitle,
                    PrimaryButtonText = Localization.ButtonOk,
                    IsPrimaryButtonEnabled = true,
                    CloseButtonText = Localization.ButtonCancel,
                    DefaultButton = ContentDialogButton.Primary,
                    Content = new HardwareListDialog()
                }));

        private static long CalcCacheSize()
        {
            var directory = new DirectoryInfo(FileUtils.HttpCacheFolder);
            return !directory.Exists ? 0 : directory.EnumerateFiles().Sum(x => x.Length);
        }

        partial void OnUseMicaChanged(bool value) => ConfigUtils.SetMica(value);
        partial void OnUseAcrylicChanged(bool value) => ConfigUtils.SetAcrylic(value);

        partial void OnMinimizeToTrayChanged(bool value) => ConfigUtils.SetMinimizeToTray(value);
        partial void OnHimariProxyChanged(bool value) => ConfigUtils.SetHimariProxyStatus(value);
        partial void OnBanSystemProxyChanged(bool value) => ConfigUtils.SetBypassSystemProxy(value);
        partial void OnEnableEmbeddedWebViewChanged(bool value) => ConfigUtils.SetEnableEmbeddedWebView(value);
        partial void OnDisableIFrameExChanged(bool value) => ConfigUtils.SetDisableIFrameEx(value);
        partial void OnLanguageCodeChanged(string value) => ConfigUtils.SetLanguageCode(value);

        [RelayCommand]
        private void UpdateCacheSize()
        {
            PictureCacheSize = CalcCacheSize();
        }

        [RelayCommand]
        private void Logout()
        {
            ConfigUtils.ClearLastLogin();
            Environment.Exit(0);
        }

        [RelayCommand]
        private async Task SetDefaultGameFolder()
        {
            var picker =
                await ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow!
                    .StorageProvider.OpenFolderPickerAsync(
                        new FolderPickerOpenOptions()
                        {
                            Title = Localization.PickDefaultInstallDirTitle,
                            AllowMultiple = false
                        }
                    );
            if (picker.Count == 1)
            {
                var folder = picker[0].Path.LocalPath;
                DefaultGameInstallFolder = folder;
                ConfigUtils.UpdateDmmGamesDefaultInstallFolder(folder);
            }
        }

        [RelayCommand]
        private async Task OpenWinePathFinder()
        {
            var picker =
                await ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow!
                    .StorageProvider.OpenFilePickerAsync(
                        new FilePickerOpenOptions()
                        {
                            Title = Localization.PickWinePathTitle,
                            AllowMultiple = false
                        }
                    );
            if (picker.Count == 1)
            {
                var folder = picker[0].Path.LocalPath;
                WinePath = folder;
                ConfigUtils.SetWinePath(folder);
            }
        }

        [RelayCommand]
        private async Task OpenHardwareList()
        {
            var dataContext = new HardwareListDialogViewModel();
            var result = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                HardwareListDialog.DataContext = dataContext;
                return HardwareListDialog.ShowAsync();
            });
            if (result == ContentDialogResult.Primary)
            {
                var target = dataContext.Hardware.Where(x => x.IsChecked)
                    .Select(x => x.HardwareManageId).ToArray();
                if (target.Length == 0) return;
                var rejectResult = await DmmGamePlayerApiHelper.HardwareReject(target);
                if (rejectResult.Failed) return;
                NotificationHelper.Success(Localization.HardwareRejectSuccess);
            }
        }

        [RelayCommand]
        private async Task ChangeDeviceAuthentication()
        {
            await DmmGamePlayerApiHelper.UpdateUserSetting(new UserSettingRequest
            {
                IsStoreStartToAdult = DmmGamePlayerApiHelper.CurrentUserInfo!.IsStoreStartToAdult,
                IsAllowBackgroundProcess = DmmGamePlayerApiHelper.CurrentUserInfo.IsAllowBackgroundProcess,
                IsAllowBannerPopup = DmmGamePlayerApiHelper.CurrentUserInfo.IsAllowBannerPopup,
                IsAllowMinimizeOnLaunch = DmmGamePlayerApiHelper.CurrentUserInfo.IsAllowMinimizeOnLaunch,
                IsDeviceAuthenticationAll = IsDeviceAuthAll,
            });
        }

        [RelayCommand]
        private async Task ChangeAdultMode()
        {
            await DmmGamePlayerApiHelper.UpdateUserSetting(new UserSettingRequest
            {
                IsStoreStartToAdult = IsAdult,
                IsAllowBackgroundProcess = DmmGamePlayerApiHelper.CurrentUserInfo!.IsAllowBackgroundProcess,
                IsAllowBannerPopup = DmmGamePlayerApiHelper.CurrentUserInfo!.IsAllowBannerPopup,
                IsAllowMinimizeOnLaunch = DmmGamePlayerApiHelper.CurrentUserInfo!.IsAllowMinimizeOnLaunch,
                IsDeviceAuthenticationAll = DmmGamePlayerApiHelper.CurrentUserInfo!.IsDeviceAuthenticationAll,
            });
        }
    }
}
