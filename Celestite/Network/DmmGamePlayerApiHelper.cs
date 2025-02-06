using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using Celestite.Network.Models;
using Celestite.Utils;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using ZeroLog;

namespace Celestite.Network
{
    public class DmmGamePlayerApiErrorEventArgs(DmmGamePlayerApiErrorCode errorCode, string productId, TApiGameType gameType) : EventArgs
    {
        public DmmGamePlayerApiErrorCode ErrorCode = errorCode;
        public string ProductId = productId;
        public TApiGameType GameType = gameType;
    }
    public class DmmGamePlayerApiResult
    {
        public bool Success => ErrorCode == DmmGamePlayerApiErrorCode.SUCCESS;
        public bool Failed => !Success;
        public DmmGamePlayerApiErrorCode ErrorCode { get; }
        public string? ErrorMessage { get; }
        public Exception? Error { get; }

        private static readonly Log Logger = LogManager.GetLogger("DmmGamePlayerApi");

        public readonly HashSet<DmmGamePlayerApiErrorCode> ErrorCodeNotBroadcast =
        [
            DmmGamePlayerApiErrorCode.DEVICE_CHECK_FAILURE,
            DmmGamePlayerApiErrorCode.NOT_CONSENT_AGREEMENT
        ];

        public static event EventHandler<DmmGamePlayerApiErrorEventArgs>? NotBroadcastErrorOccured;
        protected DmmGamePlayerApiResult(DmmGamePlayerApiErrorCode errorCode, string? error)
        {
            var success = errorCode == DmmGamePlayerApiErrorCode.SUCCESS;
            switch (success)
            {
                case true when error != null:
                    throw new InvalidOperationException();
                case false when error == null:
                    throw new InvalidOperationException();
            }

            ErrorCode = errorCode;
            ErrorMessage = error;

            if (success) return;
            Logger.Warn(error!);
            NotificationHelper.Error(error!);
        }
        protected DmmGamePlayerApiResult(DmmGamePlayerApiErrorCode errorCode, string? error, string productId, TApiGameType gameType)
        {
            var success = errorCode == DmmGamePlayerApiErrorCode.SUCCESS;
            switch (success)
            {
                case true when error != null:
                    throw new InvalidOperationException();
                case false when error == null:
                    throw new InvalidOperationException();
            }

            ErrorCode = errorCode;
            ErrorMessage = error;

            if (success) return;
            if (ErrorCodeNotBroadcast.Contains(errorCode))
            {
                NotBroadcastErrorOccured?.Invoke(null, new DmmGamePlayerApiErrorEventArgs(ErrorCode, productId, gameType));
                return;
            }
            Logger.Warn(error!);
            NotificationHelper.Error(error!);
        }

        protected DmmGamePlayerApiResult(Exception error)
        {
            ErrorCode = DmmGamePlayerApiErrorCode.ERROR_PANIC_FATAL;
            Error = error;
            ErrorMessage = error.Message;
        }

        public static DmmGamePlayerApiResult Ok(DmmGamePlayerApiResponse data)
        {
            return new DmmGamePlayerApiResult(data.ResultCode, data.Error);
        }

        public static DmmGamePlayerApiResult Fail(Exception error)
        {
            return new DmmGamePlayerApiResult(error);
        }

        public static DmmGamePlayerApiResult<T> Ok<T>(DmmGamePlayerApiResponse<T> data) where T : class
        {
            return new DmmGamePlayerApiResult<T>(data.Data, data.ResultCode, data.Error);
        }

        public static DmmGamePlayerApiResult<T> Ok<T>(DmmGamePlayerApiResponse<T> data, string productId, TApiGameType gameType) where T : class
        {
            return new DmmGamePlayerApiResult<T>(data.Data, data.ResultCode, data.Error, productId, gameType);
        }

        public static DmmGamePlayerApiResult<T> Ok<T>(T data) where T : class
        {
            return new DmmGamePlayerApiResult<T>(data, DmmGamePlayerApiErrorCode.SUCCESS, null);
        }

        public static DmmGamePlayerApiResult<T> Fail<T>(DmmGamePlayerApiResponse error) where T : class
        {
            return new DmmGamePlayerApiResult<T>(null, error.ResultCode, error.Error);
        }

        public static DmmGamePlayerApiResult<T> Fail<T>(DmmGamePlayerApiResponse error, string productId, TApiGameType gameType) where T : class
        {
            return new DmmGamePlayerApiResult<T>(null, error.ResultCode, error.Error, productId, gameType);
        }

        public static DmmGamePlayerApiResult<T> Fail<T>(Exception error) where T : class
        {
            return new DmmGamePlayerApiResult<T>(error);
        }
        public static DmmGamePlayerApiResult<T> FailAndTraceError<T>(Exception error) where T : class
        {
            return new DmmGamePlayerApiResult<T>(error);
        }
    }

    public class DmmGamePlayerApiResult<T> : DmmGamePlayerApiResult where T : class
    {
        private readonly T? _value;
        public T Value => _value!;

        protected internal DmmGamePlayerApiResult(T? value, DmmGamePlayerApiErrorCode errorCode, string? error = null) :
            base(errorCode, error)
        {
            _value = value;
        }

        protected internal DmmGamePlayerApiResult(T? value, DmmGamePlayerApiErrorCode errorCode, string? error, string productId, TApiGameType gameType) :
            base(errorCode, error, productId, gameType)
        {
            _value = value;
        }

        protected internal DmmGamePlayerApiResult(Exception error) : base(error)
        {
        }
    }

    public class UserInfo
    {
        public string OpenId { get; set; } = string.Empty;
        public long Point { get; set; }
        public long Chip { get; set; }
        public bool IsProfileRegistered { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool IsDeviceAuthenticationAll { get; set; }
        public bool IsAllowBackgroundProcess { get; set; }
        public bool IsAllowBannerPopup { get; set; }
        public bool IsAllowMinimizeOnLaunch { get; set; }
        public bool IsStoreStartToAdult { get; set; }
        public string ServiceToken { get; set; } = string.Empty;
        public ProfileObject Profile { get; set; } = null!;

        public class ProfileObject
        {
            public string DmmGamesId { get; set; } = string.Empty;
            public string Nickname { get; set; } = string.Empty;
            public string AvatarImage { get; set; } = string.Empty;
        }
    }

    public static class DmmGamePlayerApiHelper
    {
        public static UserInfo? CurrentUserInfo;

        public static bool IsAdultMode { get; set; }
        public static void SetDmmApiAge(bool startFromAdult)
        {
            IsAdultMode = startFromAdult;
            AdultModeChangedEvent?.Invoke(null, EventArgs.Empty);
        }
        public static string GetDmmApiDomain(bool fanza) => fanza ? string.Intern("app-gameplayer.dmm.co.jp") : string.Intern("app-gameplayer.dmm.com");

        // wsapi
        private const string WsApiEndpoint = "https://api-wsdgp.games.dmm.com";


        public static void SetUserCookies(string secureId, string sessionId)
        {
            foreach (Cookie c in HttpHelper.GlobalCookieContainer.GetAllCookies())
                c.Expired = true;
            HttpHelper.GlobalCookieContainer.Add([
                new Cookie("login_secure_id", secureId, "/", ".dmm.com"),
                new Cookie("login_secure_id", secureId, "/", ".dmm.co.jp"),
                new Cookie("login_session_id", sessionId, "/", ".dmm.com"),
                new Cookie("login_session_id", sessionId, "/", ".dmm.co.jp")
            ]);
            HttpHelper.LoginSecureId = secureId;
            HttpHelper.LoginSessionId = sessionId;
            LoginSessionChangedEvent?.Invoke(null, EventArgs.Empty);
        }

        public static void SetAgeCheckDone()
        {
            HttpHelper.GlobalCookieContainer.Add([
                new Cookie("age_check_done", "1", "/", ".dmm.com"),
                new Cookie("age_check_done", "1", "/", ".dmm.co.jp")
            ]);
        }

        public static event EventHandler<EventArgs>? ReGetGamesRequiredEvent;
        public static event EventHandler<EventArgs>? LoginSessionChangedEvent;
        public static event EventHandler<EventArgs>? UserInfoChangedEvent;
        public static event EventHandler<EventArgs>? AdultModeChangedEvent;

        private static readonly Log Logger = LogManager.GetLogger("DmmGamePlayerApi");

        static DmmGamePlayerApiHelper()
        {
            LoginSessionChangedEvent += async (_, _) =>
            {
                var document = await DocumentAgreementVerify();
                if (document.Failed) return;
                if (document.Value.Reconsent)
                    await DocumentAgreementAgree(document.Value.PolicyDocuments.Select(x => x.DocumentId));
                await LoginRecord();

                // 登录修复
                var loginUrl = await GetLoginUrl();
                if (loginUrl.Failed) return;

                using var loginRequest = new HttpRequestMessage(HttpMethod.Get, loginUrl.Value.Url);
                using var loginResponse = await HttpHelper.SendRawAsync(loginRequest, [HttpStatusCode.Found]);
                if (loginResponse.Failed) return;

                if (loginResponse.Value.StatusCode != HttpStatusCode.Found || loginResponse.Value.Headers.Location == null)
                {
                    Logger.Warn($"fix account got {loginResponse.Value.StatusCode}");
                    return;
                }

                using var httpRequest = new HttpRequestMessage(HttpMethod.Get, loginResponse.Value.Headers.Location);
                httpRequest.Headers.TryAddWithoutValidation("Cookie", $"secid={HttpHelper.LoginSecureId};");
                using var response = await HttpHelper.SendRawAsync(httpRequest, [HttpStatusCode.MovedPermanently]);
                if (response.Failed || response.Value.Headers.Location == null) return;

                var raw = await HttpHelper.GetAsync(response.Value.Headers.Location);
                if (response.Failed || response.Value.StatusCode != HttpStatusCode.MovedPermanently || response.Value.Headers.Location == null) return;
            };
        }

        public static async UniTask<DmmGamePlayerApiResult<List<LogNotification>>> GetLogNotification(CancellationToken cancellationToken = default)
        {
            // TODO: 这里是否需要优化成外部也是IAsyncEnumerable接口的？
            try
            {
                var response = HttpHelper.PostAndGetAsyncEnumerableJson(WsApiEndpoint, "/getLogNotification",
                    new WsTokenRequest
                    {
                        Token = CurrentUserInfo!.ServiceToken
                    }, WsDmmGamePlayerApiRequestBaseContext.Default.WsTokenRequest,
                    WsDmmGamePlayerApiResponseBaseContext.Default.LogNotification, cancellationToken: cancellationToken);
                var list = new List<LogNotification>();
                await foreach (var item in response)
                    list.Add(item);
                return DmmGamePlayerApiResult.Ok(list);
            }
            catch (Exception ex)
            {
                return DmmGamePlayerApiResult.FailAndTraceError<List<LogNotification>>(ex);
            }
        }

        public static async UniTask<DmmGamePlayerApiResult<UserInfoResponse>> GetUserInfo()
        {
            var response = await HttpHelper.DgpPostJsonAsync("/v5/userinfo", UserOsBaseRequest.Empty, DmmGamePlayerApiRequestBaseContext.Default.UserOsBaseRequest,
                DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponseUserInfoResponse);
            if (response.Failed)
                return DmmGamePlayerApiResult.Fail<UserInfoResponse>(response.Exception);
            CurrentUserInfo = response.Value.Data;
            UserInfoChangedEvent?.Invoke(response.Value.Data, EventArgs.Empty);

            if (CurrentUserInfo != null)
                SetDmmApiAge(CurrentUserInfo!.IsStoreStartToAdult);
            return DmmGamePlayerApiResult.Ok(response.Value);
        }

        public static async UniTask<DmmGamePlayerApiResult<LoginUrlResponse>> GetLoginUrl()
        {
            var response = await HttpHelper.DgpGetJsonAsync("/v5/loginurl",
                DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponseLoginUrlResponse);
            return response.Failed ? DmmGamePlayerApiResult.Fail<LoginUrlResponse>(response.Exception) : DmmGamePlayerApiResult.Ok(response.Value);
        }

        public static async UniTask<DmmGamePlayerApiResult<List<AnnounceInfo>>> AnnounceInfo()
        {
            var response = await HttpHelper.DgpPostJsonAsync("/v5/view/announce", new FloorBaseRequest
            {
                IsAdult = IsAdultMode,
                Floor = REQUEST_FLOOR.HOME
            }, DmmGamePlayerApiRequestBaseContext.Default.FloorBaseRequest, DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponseListAnnounceInfo);
            return response.Failed ? DmmGamePlayerApiResult.Fail<List<AnnounceInfo>>(response.Exception) : DmmGamePlayerApiResult.Ok(response.Value);
        }

        public static async UniTask<DmmGamePlayerApiResult<List<ViewBannerRotationInfo>>> BannerRotationList()
        {
            var response = await HttpHelper.DgpPostJsonAsync("/v5/view/banner/rotation", new FloorBaseRequest
            {
                IsAdult = IsAdultMode,
                Floor = REQUEST_FLOOR.HOME
            }, DmmGamePlayerApiRequestBaseContext.Default.FloorBaseRequest, DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponseListViewBannerRotationInfo);
            return response.Failed ? DmmGamePlayerApiResult.Fail<List<ViewBannerRotationInfo>>(response.Exception) : DmmGamePlayerApiResult.Ok(response.Value);
        }

        public static async UniTask<DmmGamePlayerApiResult<List<MyGame>>> MyGameList(CancellationToken cancellationToken = default)
        {
            var response = await HttpHelper.DgpPostJsonAsync("/v5/mygames", UserOsBaseRequest.Empty,
                DmmGamePlayerApiRequestBaseContext.Default.UserOsBaseRequest, DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponseListMyGame, cancellationToken: cancellationToken);
            return response.Failed ? DmmGamePlayerApiResult.Fail<List<MyGame>>(response.Exception) : DmmGamePlayerApiResult.Ok(response.Value);
        }

        public static async UniTask<DmmGamePlayerApiResult<List<MyGame>>> MyBrowserGameList(CancellationToken cancellationToken = default)
        {
            var response = await HttpHelper.DgpPostJsonAsync("/v5/mybrowsergames", UserOsBaseRequest.Empty,
                DmmGamePlayerApiRequestBaseContext.Default.UserOsBaseRequest, DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponseListMyGame, cancellationToken: cancellationToken);
            return response.Failed ? DmmGamePlayerApiResult.Fail<List<MyGame>>(response.Exception) : DmmGamePlayerApiResult.Ok(response.Value);
        }

        public static async UniTask<DmmGamePlayerApiResult<Search>> Search(REQUEST_FLOOR floor, string article, int limit = 120, int page = 1, CancellationToken cancellationToken = default)
        {
            var requestQuery = new Dictionary<string, string>
            {
                {"floor", floor.ToCamelCaseString()},
                {"article", article},
                {"limit", limit.ToString()},
                {"sort", "date"},
                {"os", SystemInfoUtils.UserOs},
                {"page", page.ToString()},
                {"isAdult", IsAdultMode.ToString().ToLower() }
            };
            var response = await HttpHelper.DgpGetJsonWithFormQueryAsync("/v5/view/search", requestQuery,
                DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponseSearch, cancellationToken);
            return response.Failed
                ? DmmGamePlayerApiResult.Fail<Search>(response.Exception)
                : DmmGamePlayerApiResult.Ok(response.Value);
        }

        public static async UniTask<DmmGamePlayerApiResult<GameTypeData>> GetGameTypeFromOldKeys(string productId, REQUEST_FLOOR floor, bool adult = false, CancellationToken cancellationToken = default)
        {
            var response = await HttpHelper.DgpPostJsonAsync("/v5/gametype", new GameTypeRequest
            {
                Domain = GetDmmApiDomain(adult),
                FloorType = floor,
                ProductId = productId
            }, DmmGamePlayerApiRequestBaseContext.Default.GameTypeRequest,
                DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponseGameTypeData,
                cancellationToken);
            return response.Failed
                ? DmmGamePlayerApiResult.Fail<GameTypeData>(response.Exception)
                : DmmGamePlayerApiResult.Ok(response.Value);
        }

        public static async UniTask<DmmGamePlayerApiResult> AddStoreProductToMyGame(GameTypeData gameTypeData,
            CancellationToken cancellationToken = default)
        {
            var response = await HttpHelper.DgpPostJsonAsync("/v5/detail/mygame/add", gameTypeData,
                DmmGamePlayerApiRequestBaseContext.Default.GameTypeData, DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponse, cancellationToken);
            if (response.Failed)
                return DmmGamePlayerApiResult.Fail(response.Exception);
            ReGetGamesRequiredEvent?.Invoke(null, EventArgs.Empty);
            return DmmGamePlayerApiResult.Ok(response.Value);
        }

        public static async UniTask<DmmGamePlayerApiResult> AddStoreProductToMyGame(string productId,
            TApiGameType gameType, string gameOs, CancellationToken cancellationToken = default)
            => await AddStoreProductToMyGame(new GameTypeData
            { ProductId = productId, GameOs = gameOs, GameType = gameType }, cancellationToken);

        public static async UniTask<DmmGamePlayerApiResult<GameInfo>> GetGameInfo(MyGame game,
            CancellationToken cancellationToken = default) => await GetGameInfo(game.ProductId, game.Type,
            SystemInfoUtils.UserOs, cancellationToken);
        public static async UniTask<DmmGamePlayerApiResult<GameInfo>> GetGameInfo(string productId,
            TApiGameType gameType, string gameOs, CancellationToken cancellationToken = default)
        {
            var response = await HttpHelper.DgpPostJsonAsync("/v5/gameinfo", new GameTypeDataWithSystemInfo
            {
                ProductId = productId,
                GameType = gameType,
                GameOs = gameOs
            },
                DmmGamePlayerApiRequestBaseContext.Default.GameTypeDataWithSystemInfo, DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponseGameInfo, cancellationToken);
            return response.Failed ? DmmGamePlayerApiResult.Fail<GameInfo>(response.Exception) : DmmGamePlayerApiResult.Ok(response.Value);
        }

        public static async UniTask<DmmGamePlayerApiResult> RemoveGameFromMyGame(string productId,
            TApiGameType gameType, string gameOs, CancellationToken cancellationToken = default) =>
            await RemoveGameFromMyGame(new GameTypeData
            {
                GameOs = gameOs,
                GameType = gameType,
                ProductId = productId
            }, cancellationToken);
        public static async UniTask<DmmGamePlayerApiResult> RemoveGameFromMyGame(GameTypeData game,
            CancellationToken cancellationToken = default) => await RemoveGameFromMyGame([game], cancellationToken);
        public static async UniTask<DmmGamePlayerApiResult> RemoveGameFromMyGame(GameTypeData[] games,
            CancellationToken cancellationToken = default)
        {
            var response = await HttpHelper.DgpPostJsonAsync("/v5/mygame/remove", new RemoveGameRequest
            {
                MyGames = games
            },
                DmmGamePlayerApiRequestBaseContext.Default.RemoveGameRequest, DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponse, cancellationToken);
            if (response.Failed)
                return DmmGamePlayerApiResult.Fail(response.Exception);
            ReGetGamesRequiredEvent?.Invoke(null, EventArgs.Empty);
            return DmmGamePlayerApiResult.Ok(response.Value);
        }

        public static void ForceRaiseReGetGamesRequiredEvent() => ReGetGamesRequiredEvent?.Invoke(null, EventArgs.Empty);

        public static async UniTask<DmmGamePlayerApiResult<InstallClInfo>> GetInstallClInfo(string productId, TApiGameType type,
            CancellationToken cancellationToken = default)
        {
            var response = await HttpHelper.DgpPostJsonAsync("/v5/r2/install/cl", new GameTypeDataWithSystemInfo(productId, type),
                DmmGamePlayerApiRequestBaseContext.Default.GameTypeDataWithSystemInfo, DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponseInstallClInfo, cancellationToken);
            return response.Failed ? DmmGamePlayerApiResult.Fail<InstallClInfo>(response.Exception) : DmmGamePlayerApiResult.Ok(response.Value, productId, type);
        }

        public static async UniTask<DmmGamePlayerApiResult<LaunchClInfo>> LaunchClGame(string productId, TApiGameType type,
            CancellationToken cancellationToken = default)
        {
            var response = await HttpHelper.DgpPostJsonAsync("/v5/r2/launch/cl", new LaunchClRequest(productId, type),
                DmmGamePlayerApiRequestBaseContext.Default.LaunchClRequest, DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponseLaunchClInfo, cancellationToken);
            return response.Failed ? DmmGamePlayerApiResult.Fail<LaunchClInfo>(response.Exception) : DmmGamePlayerApiResult.Ok(response.Value, productId, type);
        }

        public static async UniTask<DmmGamePlayerApiResult<GameAgreementResponse>> GetAgreement(string productId,
            CancellationToken cancellationToken = default)
        {
            var response = await HttpHelper.DgpPostJsonAsync("/v5/agreement", new ProductBaseRequest
            {
                ProductId = productId
            },
                DmmGamePlayerApiRequestBaseContext.Default.ProductBaseRequest, DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponseGameAgreementResponse, cancellationToken);
            return response.Failed ? DmmGamePlayerApiResult.Fail<GameAgreementResponse>(response.Exception) : DmmGamePlayerApiResult.Ok(response.Value);
        }

        public static async UniTask<DmmGamePlayerApiResult> ConfirmAgreement(string productId, bool isNotification,
            bool isMyApp, CancellationToken cancellationToken = default)
        {
            var response = await HttpHelper.DgpPostJsonAsync("/v5/agreement/confirm/client", new ConfirmAgreementRequest
            {
                ProductId = productId,
                IsNotification = isNotification,
                IsMyApp = isMyApp
            },
                DmmGamePlayerApiRequestBaseContext.Default.ConfirmAgreementRequest, DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponse, cancellationToken);
            return response.Failed ? DmmGamePlayerApiResult.Fail(response.Exception) : DmmGamePlayerApiResult.Ok(response.Value);
        }

        public static async UniTask<DmmGamePlayerApiResult> UpdateUserSetting(UserSettingRequest request, CancellationToken cancellationToken = default)
        {
            var response = await HttpHelper.DgpPostJsonAsync("/v5/usersetting", request,
                DmmGamePlayerApiRequestBaseContext.Default.UserSettingRequest, DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponse, cancellationToken);
            if (response.Failed) return DmmGamePlayerApiResult.Fail(response.Exception);
            if (request.IsStoreStartToAdult != IsAdultMode)
            {
                SetDmmApiAge(request.IsStoreStartToAdult);
                CurrentUserInfo!.IsStoreStartToAdult = request.IsStoreStartToAdult;
            }
            if (request.IsDeviceAuthenticationAll != CurrentUserInfo!.IsDeviceAuthenticationAll)
            {
                CurrentUserInfo!.IsDeviceAuthenticationAll = request.IsDeviceAuthenticationAll;
            }
            return DmmGamePlayerApiResult.Ok(response.Value);
        }

        public static async UniTask<DmmGamePlayerApiResult<TotalPagesResponse>> GetTotalPages(string fileListUrl,
            CancellationToken cancellationToken = default)
        {
            var response = await HttpHelper.DgpGetJsonAsync(ZString.Concat(fileListUrl, "/totalpages"),
                DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponseTotalPagesResponse,
                cancellationToken);
            return response.Failed
                ? DmmGamePlayerApiResult.Fail<TotalPagesResponse>(response.Exception)
                : DmmGamePlayerApiResult.Ok(response.Value);
        }

        // TODO: 一定要全部加载进内存吗？
        public static async UniTask<DmmGamePlayerApiResult<FileListData>> GetFileList(string fileListUrl, int page = 1,
            CancellationToken cancellationToken = default)
        {
            var response = await HttpHelper.DgpGetJsonAsync(ZString.Concat(fileListUrl, "?page=", page),
                DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponseFileListData,
                cancellationToken);
            return response.Failed
                ? new DmmGamePlayerApiResult<FileListData>(response.Exception)
                : DmmGamePlayerApiResult.Ok(response.Value);
        }

        public static async UniTask<NetworkOperationResult<AmazonCookie>> GetDownloadCookie(string domain,
            CancellationToken cancellationToken = default)
        {
            return await HttpHelper.DgpPostJsonAsync("/getCookie", new GetCookieRequest { Url = domain }, DmmGamePlayerApiRequestBaseContext.Default.GetCookieRequest,
                DmmGamePlayerApiResponseBaseContext.Default.AmazonCookie,
                cancellationToken);
        }

        public static async UniTask<DmmGamePlayerApiResult<HardwareListResponse>> GetHardwareList(
            CancellationToken cancellationToken = default)
        {
            var response = await HttpHelper.DgpPostJsonAsync("/v5/hardwarelist", new SystemInfoBaseRequest(),
                DmmGamePlayerApiRequestBaseContext.Default.SystemInfoBaseRequest, DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponseHardwareListResponse, cancellationToken);
            return response.Failed
                ? DmmGamePlayerApiResult.Fail<HardwareListResponse>(response.Exception)
                : DmmGamePlayerApiResult.Ok(response.Value);
        }

        public static async UniTask<DmmGamePlayerApiResult> PublishHardwareAuthCode(CancellationToken cancellationToken = default)
        {
            var response = await HttpHelper.DgpPostJsonAsync("/v5/hardwarecode", new SystemInfoBaseRequest(),
                DmmGamePlayerApiRequestBaseContext.Default.SystemInfoBaseRequest, DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponse, cancellationToken);
            return response.Failed
                ? DmmGamePlayerApiResult.Fail(response.Exception)
                : DmmGamePlayerApiResult.Ok(response.Value);
        }

        public static async UniTask<DmmGamePlayerApiResult> ConfirmHardwareAuthCode(string hostName, string authCode,
            CancellationToken cancellationToken = default)
        {
            var response = await HttpHelper.DgpPostJsonAsync("/v5/hardwareconf", new ConfirmHardwareAuthRequest
            {
                HardwareName = hostName,
                AuthCode = authCode
            },
                DmmGamePlayerApiRequestBaseContext.Default.ConfirmHardwareAuthRequest, DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponse, cancellationToken);
            return response.Failed
                ? DmmGamePlayerApiResult.Fail(response.Exception)
                : DmmGamePlayerApiResult.Ok(response.Value);
        }

        public static async UniTask<DmmGamePlayerApiResult<DocumentVerifyResponse>> DocumentAgreementVerify(CancellationToken cancellationToken = default)
        {
            var response = await HttpHelper.DgpPostAsync("/v5/documentagreement/verify", DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponseDocumentVerifyResponse, cancellationToken);
            return response.Failed
                ? DmmGamePlayerApiResult.Fail<DocumentVerifyResponse>(response.Exception)
                : DmmGamePlayerApiResult.Ok(response.Value);
        }

        public static async UniTask<DmmGamePlayerApiResult> DocumentAgreementAgree(IEnumerable<long> agreeIds, CancellationToken cancellationToken = default)
        {
            var response = await HttpHelper.DgpPostJsonAsync("/v5/documentagreement/agree", new DocumentAgreementAgreeRequest
            {
                DocumentIds = agreeIds
            }, DmmGamePlayerApiRequestBaseContext.Default.DocumentAgreementAgreeRequest, DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponse, cancellationToken);
            return response.Failed
                ? DmmGamePlayerApiResult.Fail(response.Exception)
                : DmmGamePlayerApiResult.Ok(response.Value);
        }

        public static async UniTask<DmmGamePlayerApiResult<LaunchBrowserGameResponse>> LaunchBrowserGame(
            string productId, TApiGameType type, CancellationToken cancellationToken = default)
        {
            var response = await HttpHelper.DgpPostJsonAsync("/v5/launch/browsergame", new ProductBaseWithGameOsRequest
            {
                ProductId = productId,
                GameType = type
            },
                DmmGamePlayerApiRequestBaseContext.Default.ProductBaseWithGameOsRequest, DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponseLaunchBrowserGameResponse, cancellationToken);
            return response.Failed ? DmmGamePlayerApiResult.Fail<LaunchBrowserGameResponse>(response.Exception) : DmmGamePlayerApiResult.Ok(response.Value, productId, type);
        }

        public static async UniTask<DmmGamePlayerApiResult<UninstallInfo>> GetUninstallGameInfo(string productId,
            TApiGameType type, CancellationToken cancellationToken = default)
        {
            var response = await HttpHelper.DgpPostJsonAsync("/v5/uninstall", new ProductBaseWithGameOsRequest
            {
                ProductId = productId,
                GameType = type
            },
                DmmGamePlayerApiRequestBaseContext.Default.ProductBaseWithGameOsRequest, DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponseUninstallInfo, cancellationToken);
            return response.Failed ? DmmGamePlayerApiResult.Fail<UninstallInfo>(response.Exception) : DmmGamePlayerApiResult.Ok(response.Value, productId, type);
        }

        public static async UniTask<DmmGamePlayerApiResult<ShortcutResponse>> GetShortcutInfo(string productId,
            TApiGameType type, string launchType = "cl", CancellationToken cancellationToken = default)
        {
            var response = await HttpHelper.DgpPostJsonAsync("/v5/shortcut/info", new ShortcutRequest
            {
                ProductId = productId,
                GameType = type,
                LaunchType = launchType
            },
                DmmGamePlayerApiRequestBaseContext.Default.ShortcutRequest, DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponseShortcutResponse, cancellationToken);
            return response.Failed ? DmmGamePlayerApiResult.Fail<ShortcutResponse>(response.Exception) : DmmGamePlayerApiResult.Ok(response.Value);
        }

        public static async UniTask<DmmGamePlayerApiResult> HardwareReject(IEnumerable<long> hardwareManageId, CancellationToken cancellationToken = default)
        {
            var response = await HttpHelper.DgpPostJsonAsync("/v5/hardwarereject", new RejectCertifiedDeviceRequest
            {
                HardwareManageId = hardwareManageId
            },
                DmmGamePlayerApiRequestBaseContext.Default.RejectCertifiedDeviceRequest, DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponse, cancellationToken);
            return response.Failed ? DmmGamePlayerApiResult.Fail(response.Exception) : DmmGamePlayerApiResult.Ok(response.Value);
        }

        public static async UniTask<DmmGamePlayerApiResult> LoginRecord(CancellationToken cancellationToken = default)
        {
            var response = await HttpHelper.DgpPostJsonAsync("/v5/loginrecord", new SystemInfoBaseRequest { },
                DmmGamePlayerApiRequestBaseContext.Default.SystemInfoBaseRequest, DmmGamePlayerApiResponseBaseContext.Default.DmmGamePlayerApiResponse, cancellationToken);
            return response.Failed ? DmmGamePlayerApiResult.Fail(response.Exception) : DmmGamePlayerApiResult.Ok(response.Value);
        }
    }
}