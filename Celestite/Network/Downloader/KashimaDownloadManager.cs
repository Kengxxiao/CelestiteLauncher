using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Avalonia.Threading;
using Celestite.Configs;
using Celestite.Dialogs.Games;
using Celestite.I18N;
using Celestite.Network.DynamicProxy;
using Celestite.Network.Models;
using Celestite.Utils;
using Celestite.Utils.Algorithm;
using Celestite.ViewModels.Dialogs;
using Celestite.ViewModels.Pages;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using FluentAvalonia.UI.Controls;
using ZeroLog;

namespace Celestite.Network.Downloader
{
    public record DownloadFileInfoStruct(string Domain, DownloadFileInfo[] DownloadFileInfos);

    public class KashimaDownloadFinishedEventArgs(string title, string productId, TApiGameType type, string latestVersion, string installDir, string imagePictureUrl, bool launchAfterUpdate = false, bool createShortcutAfterUpdate = false, Exception? exception = null) : EventArgs
    {
        public string Title = title;
        public string ProductId = productId;
        public TApiGameType GameType = type;
        public string InstallDir = installDir;
        public string LatestVersion = latestVersion;
        public Exception? Exception = exception;
        public string ImagePictureUrl = imagePictureUrl;

        public bool LaunchAfterUpdate = launchAfterUpdate;
        public bool CreateShortcutAfterUpdate = createShortcutAfterUpdate;
    }
    public class KashimaDownloadManager
    {
        internal static readonly ConcurrentDictionary<string, (IKashimaDownloader downloader, CancellationTokenSource cts)> DownloaderInstance = [];

        private static ContentDialog? _installGameDialog;
        private static ContentDialog? _updateGameDialog;

        public static ContentDialog InstallGameDialog => LazyInitializer.EnsureInitialized(ref _installGameDialog, () =>
            Dispatcher.UIThread.Invoke(() => new ContentDialog()
            {
                IsVisible = false,
                PrimaryButtonText = Localization.DownloadButton,
                CloseButtonText = Localization.ButtonCancel,
                IsPrimaryButtonEnabled = true,
                DefaultButton = ContentDialogButton.Primary,
                IsHitTestVisible = false,
                DataContext = null,
                Content = new InstallGameDialog()
            }));
        public static ContentDialog UpdateGameDialog => LazyInitializer.EnsureInitialized(ref _updateGameDialog, () =>
            Dispatcher.UIThread.Invoke(() => new ContentDialog()
            {
                IsVisible = false,
                PrimaryButtonText = Localization.UpdateButton,
                CloseButtonText = Localization.ButtonCancel,
                IsPrimaryButtonEnabled = true,
                DefaultButton = ContentDialogButton.Primary,
                IsHitTestVisible = false,
                Content = new UpdateGameDialog()
            }));

        public static bool IsProductDownloading(string productId) => DownloaderInstance.ContainsKey(productId);

        public static event EventHandler<KashimaDownloadFinishedEventArgs>? KashimaDownloadFinished;

        private static Log Logger = LogManager.GetLogger("Downloader");
        static KashimaDownloadManager()
        {
            KashimaDownloadFinished += async (sender, args) =>
            {
                if (sender is not IKashimaDownloader downloader)
                    return;
                if (args.Exception != null)
                {
                    switch (args.Exception)
                    {
                        case KashimaException kashimaException:
                            NotificationHelper.Warn(kashimaException.Message);
                            Logger.Warn($"error occured when downloading {args.ProductId}: {kashimaException.Message}");
                            break;
                        case OperationCanceledException ex:
                            NotificationHelper.Warn(Localization.DownloadIsCancelled);
                            break;
                        default:
                            NotificationHelper.Error(args.Exception.Message);
                            Logger.Error(string.Empty, args.Exception);
                            break;
                    }

                    return;
                }
                Logger.Info($"UpdateLocalContent triggered by KashimaDownloadFinished, gameId = {args.ProductId}");
                var content = ConfigUtils.UpdateLocalContent(args.ProductId, args.GameType, new DmmGameCnfContentDetail
                {
                    Installed = true,
                    KeyBindSettingVer = string.Empty,
                    Path = args.InstallDir,
                    Version = args.LatestVersion,
                    Shortcut = string.Empty // TODO: 尚未实现
                });

                if (args.LaunchAfterUpdate)
                {
                    var launchCl = await DmmGamePlayerApiHelper.LaunchClGame(args.ProductId, args.GameType, default);
                    if (launchCl.Failed) return;

                    ProcessUtils.LaunchProcessWithProductId(args.ProductId,
                        Path.Combine(content.Detail.Path, launchCl.Value.ExecFileName), launchCl.Value.ExecuteArgs, out var p,
                        launchCl.Value.IsAdministrator);
                    p?.Dispose();
                }

                if (args.CreateShortcutAfterUpdate)
                {
                    await LaunchHelper.CreateShortcut(args.ProductId, args.GameType, args.Title, args.ImagePictureUrl);
                }

                NotificationHelper.Success(ZString.Format(Localization.GameDownloadCompleted, args.Title));
            };
        }

        private static void ClearDownloader(string productId)
        {
            if (!DownloaderInstance.TryRemove(productId, out var state)) return;
            state.downloader.Dispose();
            if (!state.cts.IsCancellationRequested)
                state.cts.Cancel();
            state.cts.Dispose();
        }

        public static async UniTask PushDownloadInfo(ClInfo clInfo, TApiGameType gameType, string installDir, string imagePictureUrl, bool nextOpenUpdateWindow = false, bool launchAfterUpdate = false, bool createShortcutAfterUpdate = false)
        {
            var downloadStatistic = new DownloadStatistic(clInfo.ProductId, clInfo.Title, imagePictureUrl);
            var downloader = new KashimaDownloaderV1(downloadStatistic);
            var cts = new CancellationTokenSource();
            DownloaderInstance[clInfo.ProductId] = (downloader, cts);

            var baseInstallDir = installDir;
            var directoryName = new DirectoryInfo(installDir);
            if (directoryName.Name != clInfo.InstallDir)
                installDir = Path.Combine(installDir, clInfo.InstallDir);

            try
            {
                NotificationHelper.Success(ZString.Format(Localization.GameDownloadStart, clInfo.Title));

                downloader.InitDefault(installDir, () => new SocketsHttpHandler() { Proxy = DynamicProxyImpl.GetProxy() },
                    false, cts.Token);
                if (clInfo is InstallClInfo { HasModules: true, Modules: not null } installClInfo)
                {
                    var moduleInstallResult = await downloader.DownloadModules(installClInfo.Modules);
                    if (!moduleInstallResult && !downloader.IsCancelled())
                    {
                        NotificationHelper.Warn(Localization.KashimaErrorFailedModules);
                        return;
                    }
                }

                if (downloader.IsCancelled())
                    return;

                var debugFileName = $"debug_filelist_id_{clInfo.ProductId}.txt";
                if (File.Exists(debugFileName))
                {
                    var fileListId = File.ReadAllText(debugFileName);
                    if (int.TryParse(fileListId, out var fId)) { clInfo.FileListUrl = $"/gameplayer/filelist/{fId}"; }
                }

                var array = await downloader.GetDiffGameFiles(clInfo, installDir, cts.Token);
                if (array == null)
                    return;

                if (array.DownloadFileInfos.Length != 0)
                {
                    if (nextOpenUpdateWindow)
                    {
                        var updateDialog = await OpenUpdateDialog(clInfo, array);
                        if (updateDialog != ContentDialogResult.Primary) return;
                    }

                    var defineCommonPath =
                        PathCommonPrefixFinder.GetCommonPath(array.DownloadFileInfos.Select(x => x.Path), true);
                    if (string.IsNullOrEmpty(clInfo.Sign))
                    {
                        var cookie =
                        await DmmGamePlayerApiHelper.GetDownloadCookie(ZString.Concat(array.Domain, defineCommonPath),
                            cancellationToken: cts.Token);
                        if (cookie.Failed) return;

                        downloader.InitDownloader(array.Domain, cookie.Value, cts.Token);
                    }
                    else
                        downloader.InitDownloaderR2(array.Domain, clInfo.Sign, cts.Token);

                    try
                    {
                        Logger.Info($"UpdateLocalContent triggered by PushDownloadInfo, gameId = {clInfo.ProductId}");
                        if (!nextOpenUpdateWindow)
                        {
                            ConfigUtils.UpdateLocalContent(clInfo.ProductId, gameType, new DmmGameCnfContentDetail
                            {
                                Installed = false,
                                KeyBindSettingVer = string.Empty,
                                Path = baseInstallDir,
                                Version = clInfo.LatestVersion,
                                Shortcut = string.Empty // TODO: 尚未实现
                            });
                        }
                        await downloader.DownloadBulkDmmFiles(array.DownloadFileInfos);
                    }
                    catch (Exception ex)
                    {
                        KashimaDownloadFinished?.Invoke(downloader,
                            new KashimaDownloadFinishedEventArgs(clInfo.Title, clInfo.ProductId, gameType, clInfo.LatestVersion, installDir, imagePictureUrl, launchAfterUpdate,
                                createShortcutAfterUpdate, ex));
                        return;
                    }
                }

                KashimaDownloadFinished?.Invoke(downloader,
                    new KashimaDownloadFinishedEventArgs(clInfo.Title, clInfo.ProductId, gameType, clInfo.LatestVersion, installDir, imagePictureUrl, launchAfterUpdate,
                        createShortcutAfterUpdate));
            }
            finally
            {
                ClearDownloader(clInfo.ProductId);
            }
        }

        public static async UniTask<ContentDialogResult> OpenUpdateDialog(ClInfo clInfo, DownloadFileInfoStruct downloadInfoStruct)
        {
            var dataContext = new UpdateGameDialogViewModel(
                downloadInfoStruct.DownloadFileInfos.Sum(x => x.Size));
            return await Dispatcher.UIThread.InvokeAsync(() =>
            {
                UpdateGameDialog.DataContext = dataContext;
                UpdateGameDialog.Title =
                    ZString.Format(Localization.UpdateGameDialogTitle, clInfo.Title);
                return UpdateGameDialog.ShowAsync();
            });
        }

        public static async UniTask<(bool launchAfterInstall, bool createShortcut, ContentDialogResult result)>
            OpenInstallDialog(ClInfo clInfo)
        {
            var dataContext = new InstallGameDialogViewModel(
                FileUtils.CalcFileSizeString(clInfo.TotalSize));
            var result = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                InstallGameDialog.DataContext = dataContext;
                InstallGameDialog.Title =
                    ZString.Format(Localization.InstallGameDialogTitle, clInfo.Title);
                return InstallGameDialog.ShowAsync();
            });
            return (dataContext.LaunchAfterInstall, dataContext.CreateShortcutAfterInstall, result);
        }
    }
}
