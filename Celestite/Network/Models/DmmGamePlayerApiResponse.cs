using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Celestite.Utils.Converters;

namespace Celestite.Network.Models;

public enum DmmGamePlayerApiErrorCode
{
    UNKNOWN_CODE = 0, SUCCESS = 100, INSTALL_NOT_REQUIRED = 110, WRONG_REGISTRATION_CODE = 200, NO_REGISTRATION_CODE = 201, ISSUE_FAILURE = 202, LOGIN_REQUIRED = 203, LOCK_REGISTRATION = 204, LIMIT_REGISTRATION = 205, HARDWARE_NOT_REGISTRATION = 206, NAME_NOT_REGISTRATION = 207, CREATE_SERVICE_TOKEN_FAILURE = 210, AGE_CHECK_REQUIRED = 211, INVALID_GAME_TYPE = 300, INVALID_USER_INFO_TYPE = 301, MEMBER_API_FAILURE = 302, EMONEY_API_FAILURE = 303, BLACK_LISTED_HARDWARE_FAILURE = 304, PLATFORM_NOT_FOUND = 305, PLATFORM_VALIDATE_FAILURE = 306, DEVICE_CHECK_FAILURE = 307, NOT_CONSENT_AGREEMENT = 308, SERIAL_RESEND_MAIL_FAILURE = 309, HARDWARE_AUTH_SEND_MAIL_FAILURE = 310, HAS_PRODUCT_FAILURE = 311, APP_NOT_EXISTS = 312, PROFILE_NOT_REGISTERED = 313, NOT_PURCHASED_FAILURE = 314, APP_NOT_USE_FAILURE = 315, CAN_NOT_START_GAME = 318, BLACK_USER = 319, STORE_API_FAILURE = 320, PRE_REGISTERED = 321, NET_GAME_API_FAILURE = 322, APP_NOT_IN_SERVICE = 323, USER_OS_IS_NOT_SUPPORTED = 324, GAME_UNDER_MAINTENANCE = 325, NOT_INSTALLABLE_GAME = 326, UNABLE_TO_CREATE_SHORTCUT = 327, USER_INSTALL_RESTRICTED = 328, ACTIVATE_COUNT_EXCEEDED_LIMIT = 329, ACTIVATION_CODE_NOT_EXIST = 330, USED_ACTIVATION_CODE = 331, BEFORE_ACTIVATEABLE_PERIOD = 332, END_OF_ACTIVATEABLE_PERIOD = 333, PURCHASED_PRODUCT = 334, SUBSC_PRODUCT_CANNOT_OVERWRITE_BY_ACTIVATION_CODE_WITH_EXPIRATION_AT = 335, BASKET_COUNT_LIMIT = 336, ALREADY_IN_BASKET = 337, BASKET_PRODUCT_OUT_OF_PRINT = 338, PRODUCT_NOT_FOR_SALE = 339, PRODUCT_SALE_SUSPENDED = 340, BASKET_IS_EMPTY = 341, SELLING_PRICE_CHANGED = 342, CASHIER_ID_FAILED = 343, CASHIER_ORDER_FAILED = 344, CALLBACK_PRODUCT_NOT_FOUND = 345, MAINTENANCE = 800, COUNTRY_NOT_ALLOWED = 801, COUNTRY_NOT_ALLOWED_EMULATOR = 805, SANDBOX_USER_ONLY = 802, AREA_NOT_ALLOWED = 803, SHOULD_LOGOUT_WITH_CSAM_CHECK = 804, BASKET_MAINTENANCE = 810, DATA_NOT_FOUND = 901, ERROR_FAILURE = 900, REQUEST_PARSE_FAILURE = 910, REQUEST_READ_FAILURE = 911, ERROR_PANIC_FATAL = 999
}

public class DmmGamePlayerApiResponse
{
    public string? Error { get; set; }
    public DmmGamePlayerApiErrorCode ResultCode { get; set; }
}
public class DmmGamePlayerApiResponse<T> : DmmGamePlayerApiResponse
{
    public T? Data { get; set; }
}

public sealed class UpdateResult
{
    public bool Result { get; set; }
}

public sealed class LoginUrlResponse
{
    public string Url { get; set; } = string.Empty;
}

public sealed class UserInfoResponse : UserInfo
{
    public bool PointValid { get; set; }
    public bool ChipValid { get; set; }
}

public sealed partial class AnnounceInfo
{
    public long Id { get; set; }
    public string Site { get; set; } = string.Empty;
    public DateTimeOffset? DateBegin { get; set; } = null;
    public DateTimeOffset? DateEnd { get; set; } = null;
    public REQUEST_FLOOR Floor { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset? CreatedAt { get; set; } = null;
    public DateTimeOffset? UpdatedAt { get; set; } = null;
    public DateTimeOffset? DeletedAt { get; set; } = null;
}

// TODO: 目前DMM的代码中只是使用image
public sealed partial class ViewBannerRotationInfo
{
    public string Title { get; set; } = string.Empty;
    public string Banner { get; set; } = string.Empty;
    public string Thumbnail { get; set; } = string.Empty;
    public PageLink? Link { get; set; } = null;
    public PageLink? LinkVideo { get; set; } = null;
}
public sealed class PageLink
{
    public string Url { get; set; } = string.Empty;
    public string PageLinkType { get; set; } = string.Empty;
}

public sealed partial class MyGame
{
    public string ProductId { get; set; } = string.Empty;
    public TApiGameType Type { get; set; }
    public bool IsView { get; set; }
    public long TotalPlayTime { get; set; }
    public bool IsSubsc { get; set; }
    public bool IsFavorite { get; set; }
    public string Title { get; set; } = string.Empty;
    public string TitleRuby { get; set; } = string.Empty;
    public string PackageImageUrl { get; set; } = string.Empty;
    public bool IsDeviceAuthentication { get; set; }
    public bool IsOutOfPrint { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public bool IsGameRequiresInstall { get; set; }
    public string ExpirationAt { get; set; } = string.Empty;
    public bool IsExpirationForPlay { get; set; }
    public List<string> TargetOsDisplay { get; set; } = [];
    public DateTimeOffset? LastPlayed { get; set; }
}

public sealed class GameDetailSetting
{
    public bool AllowNotification { get; set; }
    public bool IsShowInProfile { get; set; }
    public bool IsShowSettings { get; set; }
}

// TODO: 现在设置limit=120，但游戏只有77个，如果以后超过这个限制需要增加读页
public sealed class Search
{
    public int TotalHitCount { get; set; }
    public List<SearchGameData> List { get; set; } = [];
}

public sealed class SearchGameData
{
    public string ProductId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public REQUEST_FLOOR Floor { get; set; }
    public PageLink Link { get; set; } = new();

    private string? _transformGameId;

    [JsonIgnore]
    public string TransformGameId => LazyInitializer.EnsureInitialized(ref _transformGameId, () =>
    {
        if (!ProductId.StartsWith("app_"))
            return string.Empty;
        if (Link.PageLinkType != "external")
            return string.Empty;
        var uri = new Uri(Link.Url);
        if (uri.Host != "www.dmm.com" && uri.Host != "www.dmm.co.jp")
            return string.Empty;
        if (uri.Segments is not ["/", "netgame_s/", var gameId])
            return string.Empty;
        var realGameId = gameId.Trim('/');
        return realGameId;
    });

    private Task<Bitmap?>? _image;
    [JsonIgnore]
    public Task<Bitmap?> Image =>
        LazyInitializer.EnsureInitialized(ref _image, () => AvaImageHelper.LoadFromWeb(ImageUrl));
}

public sealed class ModuleFile
{
    public string ExecFileName { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string ModuleFileUrl { get; set; } = string.Empty;
    public string Sign { get; set; } = string.Empty;
    public string DirName { get; set; } = string.Empty;

    // 拼接 Domain + ModuleFileUrl
}

public sealed class InstallerFile
{
    public string InstallerFileListUrl { get; set; } = string.Empty;
    public string ExecFileName { get; set; } = string.Empty;
    public bool IsAdministrator { get; set; }
    public string ExecFileParam { get; set; } = string.Empty;
}

public class ClInfo
{
    public string ProductId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string InstallDir { get; set; } = string.Empty;
    public string FileListUrl { get; set; } = string.Empty;
    public bool IsAdministrator { get; set; }

    // TODO: Celestite有自己的FileCheck行为
    public FILE_CHECK_TYPE FileCheckType { get; set; }
    public long TotalSize { get; set; }
    public string LatestVersion { get; set; } = string.Empty;
}

public sealed class InstallClInfo : ClInfo
{
    public bool HasInstaller { get; set; }
    public InstallerFile? Installer { get; set; }
    public bool HasModules { get; set; }
    public List<ModuleFile>? Modules { get; set; }
    public string SdkType { get; set; } = string.Empty;
}

public sealed class LaunchClInfo : ClInfo
{
    public string ExecFileName { get; set; } = string.Empty;
    public string ExecuteArgs { get; set; } = string.Empty;
    public string[]? ConversionUrl { get; set; }
}

public partial class GameAgreement
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string Agreement { get; set; } = string.Empty;
    public long Sort { get; set; }
}
public sealed class GameAgreementResponse
{
    public List<GameAgreement> AgreementList { get; set; } = [];
}

public sealed class Hardware
{
    public long HardwareManageId { get; set; }
    public string HardwareName { get; set; } = string.Empty;
    public DateTimeOffset LoginDate { get; set; }
    public string Place { get; set; } = string.Empty;
    public bool IsDeviceAuthenticated { get; set; }

    [JsonIgnore]
    public bool IsChecked { get; set; }
}

public sealed class HardwareListResponse
{
    public int DeviceAuthLimitNum { get; set; }
    public List<Hardware>? Hardwares { get; set; }
}

public sealed class TotalPagesResponse
{
    public int TotalPages { get; set; }
}

public sealed class FileListData
{
    public string Domain { get; set; } = string.Empty;
    public List<DownloadFileInfo> FileList { get; set; } = [];
}

public sealed class PolicyDocument
{
    public long DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string Revision { get; set; } = string.Empty;
}

public sealed class DocumentVerifyResponse
{
    public List<PolicyDocument> PolicyDocuments { get; set; } = [];
    public bool Reconsent { get; set; }
}

public sealed class LaunchBrowserGameResponse
{
    public string LaunchUrl { get; set; } = string.Empty;
}

public sealed record UninstallInfo
{
    public string ProductId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public TApiGameType Type { get; set; }
    public string InstallDir { get; set; } = string.Empty;
    public bool IsExecUninstallFile { get; set; }
    public string UninstallFileName { get; set; } = string.Empty;
    public string UninstallFileParam { get; set; } = string.Empty;
}

public sealed record ShortcutResponse
{
    public string InstallDir { get; set; } = string.Empty;
    public string ExecutableFile { get; set; } = string.Empty;
    public string ShortcutFileName { get; set; } = string.Empty;
}

// wsdgp API
public partial class LogNotification
{
    [JsonConverter(typeof(UnixEpochDateTimeConverter))]
    public DateTimeOffset CreatedAt { get; set; }
    public int GeneralAdult { get; set; }
    public string Genre { get; set; } = string.Empty;
    public string HtmlLink { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int IsSuspended { get; set; }
    public int IsViewed { get; set; }
    public string MessageBody { get; set; } = string.Empty;
    public string MsgTitle { get; set; } = string.Empty;
    public int NotificationId { get; set; }
    public string OpenId { get; set; } = string.Empty;
}

public class ArtemisInitGameFrameResponse
{
    [JsonPropertyName("game_frame_url")]
    public string GameFrameUrl { get; set; } = string.Empty;
    [JsonPropertyName("security_token")]
    public string SecurityToken { get; set; } = string.Empty;
    [JsonPropertyName("frame_width")]
    public string FrameWidth { get; set; } = string.Empty;
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    [JsonPropertyName("app_id")]
    public long AppId { get; set; }
    [JsonPropertyName("code")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Code { get; set; } = string.Empty;
}

[JsonSerializable(typeof(DmmGamePlayerApiResponse))]
[JsonSerializable(typeof(DmmGamePlayerApiResponse<LoginUrlResponse>))]
[JsonSerializable(typeof(DmmGamePlayerApiResponse<UserInfoResponse>))]
[JsonSerializable(typeof(DmmGamePlayerApiResponse<UserInfo>))]
[JsonSerializable(typeof(DmmGamePlayerApiResponse<List<AnnounceInfo>>))]
[JsonSerializable(typeof(DmmGamePlayerApiResponse<List<ViewBannerRotationInfo>>))]
[JsonSerializable(typeof(DmmGamePlayerApiResponse<List<MyGame>>))]
[JsonSerializable(typeof(DmmGamePlayerApiResponse<MyGameSearch>))]
[JsonSerializable(typeof(DmmGamePlayerApiResponse<UpdateResult>))]
[JsonSerializable(typeof(DmmGamePlayerApiResponse<GameDetailSetting>))]
[JsonSerializable(typeof(DmmGamePlayerApiResponse<Search>))]
[JsonSerializable(typeof(DmmGamePlayerApiResponse<GameTypeData>))]
[JsonSerializable(typeof(DmmGamePlayerApiResponse<GameInfo>))]
[JsonSerializable(typeof(DmmGamePlayerApiResponse<InstallClInfo>))]
[JsonSerializable(typeof(DmmGamePlayerApiResponse<GameAgreementResponse>))]
[JsonSerializable(typeof(DmmGamePlayerApiResponse<HardwareListResponse>))]
[JsonSerializable(typeof(DmmGamePlayerApiResponse<TotalPagesResponse>))]
[JsonSerializable(typeof(DmmGamePlayerApiResponse<FileListData>))]
[JsonSerializable(typeof(DmmGamePlayerApiResponse<LaunchClInfo>))]
[JsonSerializable(typeof(DmmGamePlayerApiResponse<DocumentVerifyResponse>))]
[JsonSerializable(typeof(DmmGamePlayerApiResponse<LaunchBrowserGameResponse>))]
[JsonSerializable(typeof(DmmGamePlayerApiResponse<UninstallInfo>))]
[JsonSerializable(typeof(DmmGamePlayerApiResponse<ShortcutResponse>))]
[JsonSerializable(typeof(AmazonCookie))]
[JsonSerializable(typeof(ArtemisInitGameFrameResponse))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
public partial class DmmGamePlayerApiResponseBaseContext : JsonSerializerContext
{
}

[JsonSerializable(typeof(LogNotification))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class WsDmmGamePlayerApiResponseBaseContext : JsonSerializerContext
{
}
