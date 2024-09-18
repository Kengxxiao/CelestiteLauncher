using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Celestite.I18N;
using Celestite.Network;
using Celestite.Network.Downloader;
using Celestite.Network.Models;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using FluentAvalonia.UI.Controls;

namespace Celestite.Utils
{
    public class LaunchHelper
    {
        public record ProductIdWithType(string ProductId, TApiGameType Type);
        public static bool IsInGuiMode() => Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime a && a.MainWindow != null;
        public static async UniTask<ProductIdWithType?> TransformDmmAppIdToProductId(string appId, bool adult, string gameTypeRequired = "PC")
        {
            var hermesData = await DmmHermesHelper.GetGameList(adult, gameTypeRequired);
            if (hermesData.Count == 0) return null;

            var targetGame = hermesData.FirstOrDefault(x => x.Id.StartsWith(appId) && x.Id.EndsWith(gameTypeRequired, StringComparison.OrdinalIgnoreCase));
            if (targetGame == null || !targetGame.Id.Contains("social")) return null;

            var uri = new Uri(targetGame.Link);
            if (uri.Segments.Length != 3 || !uri.Host.StartsWith("games.dmm")) return null;
            var gameId = uri.Segments[^1].Trim('/');
            return new ProductIdWithType(gameId, uri.Host.EndsWith("co.jp") ? TApiGameType.ASBROWSER : TApiGameType.GSBROWSER);
        }

        public static UniTask EmulateLaunch(string productId, TApiGameType gameType, string env = "PC") => EmulateLaunch(productId,
            DmmTypeApiGameHelper.GetGameTypeDataFromApiGameType(gameType), env);

        public static async UniTask EmulateLaunch(string productId, DmmTypeApiGame gameType, string browserEnv = "PC")
        {
            if (gameType.IsCl)
            {
                var clInfo = await DmmGamePlayerApiHelper.LaunchClGame(productId, gameType.GameType, default);
                if (clInfo.Failed) return;
                NotificationHelper.Success(Localization.EmuLaunchSuccess);
                return;
            }

            if (gameType.IsSocialBrowser)
            {
                var context = await DmmBrowserGameLaunchHelper.GetBrowserPageContext(productId, gameType, browserEnv);
                if (context == null) return;
                if (!DmmBrowserGameLaunchHelper.FindAndInvokeMissionCall(context.Scripts))
                {
                    // 一些游戏使用 Artemis API 获取启动参数和完成任务
                    var startPlayingSuccess = await DmmBrowserGameLaunchHelper.ArtemisStartGamePlaying(productId, gameType, browserEnv);
                    if (!startPlayingSuccess)
                    {
                        NotificationHelper.Warn(Localization.EmuLaunchFailed);
                        return;
                    }
                }
                NotificationHelper.Success(Localization.EmuLaunchSuccess);
                return;
            }

            NotificationHelper.Warn(Localization.GameNotSupported);
        }

        public static async UniTask LaunchGame(string productId, TApiGameType type, string? packageImageUrl = null, bool checkGameExist = false)
        {
            // Dispatcher.UIThread.Post(WindowTrayHelper.ReShowWindowCore);
            var apiType = DmmTypeApiGameHelper.GetGameTypeDataFromApiGameType(type);
            if (apiType.IsCl)
            {

                if (KashimaDownloadManager.IsProductDownloading(productId))
                {
                    NotificationHelper.Warn(Localization.GameIsDownloading);
                    return;
                }

                if (checkGameExist)
                {
                    var clientGames = await DmmGamePlayerApiHelper.MyGameList();
                    if (clientGames.Failed) return;

                    if (!clientGames.Value.Any(x => x.ProductId == productId && x.Type == type))
                    {
                        var advancedType = DmmTypeApiGameHelper.GetGameTypeDataFromApiGameType(type);
                        var targetGameType = await DmmGamePlayerApiHelper.GetGameTypeFromOldKeys(productId, REQUEST_FLOOR.FREE, adult: advancedType.IsAdult);
                        if (targetGameType.Failed) return;
                        var addGame = await DmmGamePlayerApiHelper.AddStoreProductToMyGame(targetGameType.Value);
                        if (addGame.Failed) return;
                    }
                }

                ClInfo clInfo;

                var launchAfterUpdate = true;
                var createShortcutAfterUpdate = false;
                var nextOpenUpdateWindow = false;

                if (ConfigUtils.TryGetContent(productId, type, out var content))
                {
                    // TODO: 注意需要解析conversion_url
                    var launchCl = await DmmGamePlayerApiHelper.LaunchClGame(productId, type);
                    if (launchCl.Failed) return;
                    if (content!.Detail.Installed && content.Detail.Version == launchCl.Value.LatestVersion)
                    {
                        var result = ProcessUtils.LaunchProcessWithProductId(productId,
                            Path.Combine(content.Detail.Path, launchCl.Value.ExecFileName), launchCl.Value.ExecuteArgs, out var p,
                            launchCl.Value.IsAdministrator);
                        p?.Dispose();
                        if (result is ProcessFailedReason.Success or ProcessFailedReason.SystemFailed)
                            return;
                    }
                    clInfo = launchCl.Value;
                    nextOpenUpdateWindow = true;
                }
                else
                {
                    var installCl = await DmmGamePlayerApiHelper.GetInstallClInfo(productId, type);
                    if (installCl.Failed) return;
                    clInfo = installCl.Value;

                    if (!IsInGuiMode())
                    {
                        Console.WriteLine("Download required, cannot process it in console mode.");
                        return;
                    }
                    (launchAfterUpdate, createShortcutAfterUpdate, ContentDialogResult result) = await KashimaDownloadManager.OpenInstallDialog(clInfo);
                    if (result != ContentDialogResult.Primary) return;

                    if (installCl.Value.HasInstaller)
                    {
                        NotificationHelper.Warn(Localization.DEBUG_InstallerNotImplemented);
                        return;
                    }
                }

                if (!IsInGuiMode())
                {
                    Console.WriteLine("Download required, cannot process it in console mode.");
                    return;
                }
                packageImageUrl ??= ZString.Format("https://pics.dmm.com/digital/gameplayer/{0}/{0}pt.jpg", productId);

                KashimaDownloadManager.PushDownloadInfo(clInfo, type, ConfigUtils.DmmGamesDefaultInstallFolder, packageImageUrl, nextOpenUpdateWindow,
                    launchAfterUpdate, createShortcutAfterUpdate).Forget();
                return;
            }

            if (apiType.IsChannelBrowser)
            {
                var launch = await DmmGamePlayerApiHelper.LaunchBrowserGame(productId, type);
                if (launch.Failed) return;
                ProcessUtils.OpenExternalLink(launch.Value.LaunchUrl);
                return;
            }

            if (apiType.IsSocialBrowser)
            {
                if (ConfigUtils.GetDisableIFrameEx())
                {
                    ProcessUtils.OpenExternalLink(DmmBrowserGameLaunchHelper.GetBrowserGameUrl(productId, apiType));
                    return;
                }
                var context = await DmmBrowserGameLaunchHelper.GetBrowserPageContext(productId, apiType);
                if (context == null) return;
                var missionFindSuccess = DmmBrowserGameLaunchHelper.FindAndInvokeMissionCall(context.Scripts);
                if (!missionFindSuccess)
                    await DmmBrowserGameLaunchHelper.ArtemisStartGamePlaying(productId, apiType); // 尝试使用 Artemis API
                var launchUrl = DmmBrowserGameLaunchHelper.FindAndInvokeLaunchGame(context);
                if (string.IsNullOrEmpty(launchUrl))
                {
                    var artemisLaunchInfo = await DmmBrowserGameLaunchHelper.ArtemisInitGameFrame(productId, apiType);
                    if (artemisLaunchInfo == null || !string.IsNullOrEmpty(artemisLaunchInfo.Code))
                    {
                        NotificationHelper.Warn(Localization.LaunchBrowserGameFailed);
                        return;
                    }
                    launchUrl = ZString.Concat("https:", artemisLaunchInfo.GameFrameUrl);
                }
                ProcessUtils.OpenExternalLink(launchUrl);
                return;
            }

            NotificationHelper.Warn(Localization.GameNotImplemented);
        }

        public static async UniTask CreateShortcut(string productId, TApiGameType type, string title, string imagePictureUrl)
        {
            if (ConfigUtils.IsContentInstalled(productId, type, out var content))
            {
                var shortcut = await DmmGamePlayerApiHelper.GetShortcutInfo(productId, type);
                if (shortcut.Failed) return;

                var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                if (string.IsNullOrEmpty(desktop))
                {
                    NotificationHelper.Warn(Localization.GetDesktopPathFailed);
                    return;
                }

                if (OperatingSystem.IsWindows())
                {
                    var internetShortcut = ZString.Format("""
                                                          [InternetShortcut]
                                                          URL=celestite://launch/{0}/{1}
                                                          IconFile={2}
                                                          HotKey=0
                                                          IconIndex=0
                                                          """, productId, type.ToString(), Path.Combine(content!.Detail.Path, shortcut.Value.ExecutableFile));
                    var shortcutPath = Path.Combine(desktop, $"{shortcut.Value.ShortcutFileName}.url");
                    await File.WriteAllTextAsync(shortcutPath, internetShortcut);
                    NotificationHelper.Success(Localization.CreateShortcutSuccess);
                }

                if (OperatingSystem.IsLinux())
                {
                    var shortcutData = await HttpHelper.GetByteArrayWithCacheFromWebAsync(imagePictureUrl);
                    if (shortcutData.Failed) return;

                    using var bitmap = shortcutData.Value;
                    var pngName = $"{ConfigUtils.GetFilePath(productId)}.png";
                    bitmap.Save(pngName);

                    var xdgShortcut = ZString.Format("""
                                                     [Desktop Entry]
                                                     Version=1.0
                                                     Type=Application
                                                     Exec=xdg-open celestite://launch/{0}/{1}
                                                     Icon={2}
                                                     NoDisplay=true
                                                     Terminal=false
                                                     Categories=Application
                                                     Name={3}
                                                     """, productId, type.ToString(), pngName, shortcut.Value.ShortcutFileName);
                    var shortcutPath = Path.Combine(desktop, $"{productId}.desktop");
                    await File.WriteAllTextAsync(shortcutPath, xdgShortcut);
                    Process.Start("gio", $"set {shortcutPath} metadata::trusted true");
                    File.SetUnixFileMode(shortcutPath, File.GetUnixFileMode(shortcutPath) | UnixFileMode.UserExecute);
                    NotificationHelper.Success(Localization.CreateShortcutSuccess);
                }
            }
            else
            {
                NotificationHelper.Warn(Localization.GameIsNotInstalled);
            }
        }
    }
}
