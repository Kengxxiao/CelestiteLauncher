using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Celestite.Configs;
using Celestite.I18N;
using Celestite.Network;
using Celestite.Network.Downloader;
using Celestite.Network.Models;
using Celestite.Utils;
using Celestite.ViewModels.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using FluentAvalonia.UI.Controls;
using ZeroLog;

namespace Celestite.ViewModels.Dialogs
{
    public partial class GameSettingsDialogViewModel : ViewModelBase
    {
        [ObservableProperty] private MyGame _gameData;
        [ObservableProperty] private bool _isBackEnabled = true;
        [ObservableProperty] private GameDetailSetting? _detailSetting;

        private readonly Log Logger = LogManager.GetLogger("GameSettingsVM");

        public bool IsCl => GameData.ApiType.IsCl;
        [ObservableProperty] private string _extraCommandLine = string.Empty;
        [ObservableProperty] private bool _forceSkipFileCheck;
        [ObservableProperty] private bool _forcePin;

        [ObservableProperty] private bool _contentInstalled;
        private DmmGameCnfContent? _content;

        public GameSettingsDialogViewModel(MyGame gameData)
        {
            GameData = gameData;
            ContentInstalled = IsContentInstalled();
            UniTask.Run(async () =>
            {
                var detailSettings = await GameData.GetDetailSettingList();
                if (detailSettings.Success) DetailSetting = detailSettings.Value;
            });
            ExtraCommandLine = ConfigUtils.GetGameSettingsCommandLine(GameData.ProductId);
            ForceSkipFileCheck = ConfigUtils.GetGameSettingsForceSkipFileCheck(GameData.ProductId);
            ForcePin = ConfigUtils.GetGameSettingsForcePin(GameData.ProductId);
        }

        public bool IsContentInstalled() => ConfigUtils.TryGetContent(GameData.ProductId, GameData.Type, out _content);

        [RelayCommand]
        private void UpdateCommandLine() =>
            ConfigUtils.UpdateGameSettingsCommandLine(GameData.ProductId, ExtraCommandLine);

        partial void OnForceSkipFileCheckChanged(bool value) =>
            ConfigUtils.UpdateGameSettingsForceSkipCheck(GameData.ProductId, value);

        partial void OnForcePinChanged(bool value) => GameData.UpdateForcePinCelestite(value);

        [RelayCommand]
        private void Back()
        {
            Dispatcher.UIThread.Invoke(() => GameNavigationFactory.Instance.GetFrame().GoBack());
        }

        [RelayCommand]
        private async Task UpdateIsViewOption() => await GameData.UpdateMyGameViewFlag();

        [RelayCommand]
        private async Task UpdateIsFavoriteOption() => await GameData.UpdateMyGameFavorite();

        [RelayCommand]
        private async Task UpdateAllowNotification() => await GameData.UpdateAllowReceiveNotification(DetailSetting!.AllowNotification);

        [RelayCommand]
        private async Task UpdateShowInProfile() => await GameData.UpdateIsDisplayProfile(DetailSetting!.IsShowInProfile);

        [RelayCommand]
        private async Task RemoveGame()
        {
            IsBackEnabled = false;
            try
            {
                if (GameData.ApiType.IsCl || GameData.ApiType.IsEmulator)
                {
                    var gameInfo = await DmmGamePlayerApiHelper.GetGameInfo(GameData);
                    if (gameInfo.Failed) return;
                    var removeResult = await DmmGamePlayerApiHelper.RemoveGameFromMyGame(gameInfo.Value.ProductId,
                        gameInfo.Value.Type, SystemInfoUtils.UserOs);
                    if (removeResult.Failed) return;
                }

                if (GameData.ApiType.IsBrowser)
                {
                    var context = await DmmPersonalAccessHelper.SearchAppIdOnPersonal(GameData.Title);
                    if (context == null)
                    {
                        NotificationHelper.Warn(Localization.CannotFindTargetGame);
                        return;
                    }
                    // if (!await DmmPersonalAccessHelper.GetUninstallConfirmPageToken(context)) return;
                    if (!await DmmPersonalAccessHelper.UninstallGame(context)) return;
                    DmmGamePlayerApiHelper.ForceRaiseReGetGamesRequiredEvent();
                }
            }
            finally
            {
                IsBackEnabled = true;
            }

            Dispatcher.UIThread.Invoke(GameNavigationFactory.Instance.GetFrame().GoBack);
        }

        [RelayCommand]
        private async Task CheckGameFileList()
        {
            if (IsCl)
            {
                if (KashimaDownloadManager.IsProductDownloading(GameData.ProductId))
                {
                    NotificationHelper.Warn(Localization.GameIsDownloading);
                    return;
                }
                var clInfo = await DmmGamePlayerApiHelper.LaunchClGame(GameData.ProductId, GameData.Type, default);
                if (clInfo.Failed) return;
                if (ConfigUtils.TryGetContent(GameData.ProductId, GameData.Type, out var content))
                {
                    KashimaDownloadManager.PushDownloadInfo(clInfo.Value, GameData.Type, content!.Detail.Path,
                        GameData.PackageImageUrl, true, false, false).Forget();
                }
            }
        }

        [RelayCommand]
        private async Task UseGameFilesOnDisk()
        {
            if (IsCl)
            {
                var clInfo = await DmmGamePlayerApiHelper.LaunchClGame(GameData.ProductId, GameData.Type);
                if (clInfo.Failed) return;

                var picker =
                await ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow!
                    .StorageProvider.OpenFilePickerAsync(
                        new FilePickerOpenOptions()
                        {
                            Title = Localization.PickWinePathTitle,
                            AllowMultiple = false,
                            FileTypeFilter = [new FilePickerFileType(clInfo.Value.ExecFileName) { Patterns = [clInfo.Value.ExecFileName] }]
                        }
                    );
                if (picker.Count == 0) return;

                var gameFolder = Path.GetDirectoryName(picker[0].Path.LocalPath);
                if (string.IsNullOrEmpty(gameFolder)) return;

                var result = await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var contentDialog = new ContentDialog
                    {
                        Title = Localization.DialogWarning,
                        Content = ZString.Format(Localization.UseGameFilesOnDiskWarning, gameFolder),
                        IsPrimaryButtonEnabled = true,
                        PrimaryButtonText = Localization.ButtonOk,
                        CloseButtonText = Localization.ButtonCancel,
                        DefaultButton = ContentDialogButton.Primary
                    };
                    return contentDialog.ShowAsync();
                });
                if (result == ContentDialogResult.None)
                    return;

                Logger.Info($"UpdateLocalContent triggered by UseGameFilesOnDisk, gameId = {GameData.ProductId}");
                ConfigUtils.UpdateLocalContent(GameData.ProductId, GameData.Type, new DmmGameCnfContentDetail
                {
                    Installed = true,
                    KeyBindSettingVer = string.Empty,
                    Path = gameFolder,
                    Shortcut = string.Empty,
                    Version = clInfo.Value.LatestVersion
                });
                ContentInstalled = IsContentInstalled();
                NotificationHelper.Success(Localization.ModifyGameFolderSuccess);
            }
        }

        [RelayCommand]
        private async Task UninstallGame()
        {
            if (IsCl)
            {
                if (KashimaDownloadManager.IsProductDownloading(GameData.ProductId))
                {
                    NotificationHelper.Warn(Localization.GameIsDownloading);
                    return;
                }

                if (!ConfigUtils.TryGetContent(GameData.ProductId, GameData.Type, out var content))
                {
                    NotificationHelper.Warn(Localization.GameIsNotInstalled);
                    return;
                }

                var dirTarget = string.Empty;
                var uninstallInfo = await DmmGamePlayerApiHelper.GetUninstallGameInfo(GameData.ProductId, GameData.Type);
                if (uninstallInfo.Failed) return;

                var directoryName = new DirectoryInfo(content!.Detail.Path);
                if (directoryName.Name != uninstallInfo.Value.InstallDir)
                    dirTarget = Path.Combine(content!.Detail.Path, uninstallInfo.Value.InstallDir);
                if (string.IsNullOrEmpty(dirTarget))
                    dirTarget = content!.Detail.Path;

                var dir = new DirectoryInfo(dirTarget);
                var result = await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var contentDialog = new ContentDialog
                    {
                        Title = Localization.DialogWarning,
                        Content = ZString.Format(Localization.UninstallWarning, dir.FullName),
                        IsPrimaryButtonEnabled = true,
                        PrimaryButtonText = Localization.ButtonDeleteAgree,
                        CloseButtonText = Localization.ButtonCancel,
                        DefaultButton = ContentDialogButton.Primary
                    };
                    return contentDialog.ShowAsync();
                });
                if (result == ContentDialogResult.None)
                    return;

                if (uninstallInfo.Value.IsExecUninstallFile)
                {
                    var path = Path.Combine(dirTarget, uninstallInfo.Value.UninstallFileName);
                    if (File.Exists(path))
                    {
                        ProcessUtils.LaunchProcess(path, uninstallInfo.Value.UninstallFileParam, out var process);
                        if (process != null)
                        {
                            await process.WaitForExitAsync();
                            var exitCode = process.ExitCode;
                            process.Dispose();
                            if (exitCode != 0)
                            {
                                NotificationHelper.Warn(ZString.Format(Localization.UninstallFailed, exitCode));
                                return;
                            }
                        }
                    }
                }

                if (dir.Exists)
                    dir.Delete(true);
                ConfigUtils.RemoveLocalContent(GameData.ProductId, GameData.Type);
                ContentInstalled = false;
                NotificationHelper.Success(Localization.UninstallSuccess);
            }
        }

        [RelayCommand]
        private async Task EmulateLaunch() => await LaunchHelper.EmulateLaunch(GameData.ProductId, GameData.ApiType);

        [RelayCommand]
        private async Task CreateShortcut() => await LaunchHelper.CreateShortcut(GameData.ProductId, GameData.Type, GameData.Title, GameData.PackageImageUrl);
    }
}
