using System.Collections.Generic;
using System.Text.Json.Serialization;
using Celestite.Utils;

namespace Celestite.Network.Models;

public class UserOsBaseRequest
{
    public string UserOs => SystemInfoUtils.UserOs;

    public static readonly UserOsBaseRequest Empty = new();
}

public class FloorBaseRequest
{
    public bool IsAdult { get; set; }
    public REQUEST_FLOOR Floor { get; set; }
}

public record ProductBaseRequest
{
    public string ProductId { get; set; } = string.Empty;
}

public record ProductGameTypeBaseRequest : ProductBaseRequest
{
    public TApiGameType GameType { get; set; }
}

public record ProductBaseWithGameOsRequest : ProductGameTypeBaseRequest
{
    public string GameOs => SystemInfoUtils.UserOs;
}

public record MyGameFavoriteRequest : ProductGameTypeBaseRequest
{
    public bool IsFavorite { get; set; }
}

public record MyGameViewFlagRequest : ProductBaseWithGameOsRequest
{
    public bool IsView { get; set; }
}

public record UpdateNotificationRequest : ProductBaseRequest
{
    public bool IsAdult { get; set; }
    public bool IsNotification { get; set; }
}

public record UpdateDisplayProfileRequest : ProductBaseRequest
{
    public bool IsAdult { get; set; }
    public bool IsDisplay { get; set; }
}

public record SystemInfoBaseRequest
{
    public string UserOs => SystemInfoUtils.UserOs;
    public string Motherboard => SystemInfoUtils.Motherboard;
    public string HddSerial => SystemInfoUtils.HddSerial;
    public string MacAddress => SystemInfoUtils.MacAddress;
}

public record GameTypeRequest : SystemInfoBaseRequest
{
    public string ProductId { get; set; } = string.Empty;
    public REQUEST_FLOOR FloorType { get; set; }
    public string Domain { get; set; } = string.Empty;
}

public record GameTypeDataWithSystemInfo : SystemInfoBaseRequest
{
    public string ProductId { get; set; } = string.Empty;
    public string GameOs { get; set; } = string.Empty;
    public TApiGameType GameType { get; set; }
    public GameTypeDataWithSystemInfo(GameTypeData gameTypeData)
    {
        ProductId = gameTypeData.ProductId;
        GameOs = gameTypeData.GameOs;
        GameType = gameTypeData.GameType;
    }

    public GameTypeDataWithSystemInfo(GameInfo gameInfo)
    {
        ProductId = gameInfo.ProductId;
        GameOs = SystemInfoUtils.UserOs;
        GameType = gameInfo.Type;
    }

    public GameTypeDataWithSystemInfo(string productId, TApiGameType type)
    {
        ProductId = productId;
        GameOs = SystemInfoUtils.UserOs;
        GameType = type;
    }

    public GameTypeDataWithSystemInfo()
    {
    }
}

public class RemoveGameRequest
{
    public GameTypeData[] MyGames { get; set; } = [];
}

public record ConfirmAgreementRequest : ProductBaseRequest
{
    public bool IsNotification { get; set; }
    [JsonPropertyName("is_myapp")]
    public bool IsMyApp { get; set; }
}

public record UserSettingRequest : SystemInfoBaseRequest
{
    public bool IsDeviceAuthenticationAll { get; set; }
    public bool IsAllowBackgroundProcess { get; set; }
    public bool IsAllowBannerPopup { get; set; }
    public bool IsAllowMinimizeOnLaunch { get; set; }
    public bool IsStoreStartToAdult { get; set; }
}

public class GetCookieRequest
{
    public string Url { get; set; } = string.Empty;
}

public record LaunchClRequest : GameTypeDataWithSystemInfo
{
    public string LaunchType { get; set; } = string.Empty;

    public LaunchClRequest(string productId, TApiGameType type, string launchType = "LIB") : base(productId, type)
    {
        LaunchType = launchType;
    }
}

public record ConfirmHardwareAuthRequest : SystemInfoBaseRequest
{
    public string HardwareName { get; set; } = string.Empty;
    public string AuthCode { get; set; } = string.Empty;
}

public record DocumentAgreementAgreeRequest
{
    public IEnumerable<long> DocumentIds { get; set; } = [];
}

public record ShortcutRequest : GameTypeData
{
    [JsonPropertyName("launchType")]
    public string LaunchType { get; set; } = string.Empty;

    public ShortcutRequest() : base() { }
}

public record RejectCertifiedDeviceRequest
{
    public IEnumerable<long> HardwareManageId { get; set; } = [];
}

public record SearchRequest
{
    public REQUEST_FLOOR RequestFloor { get; set; }
    public string Article { get; set; } = string.Empty;
    public int Limit { get; set; }
    public string Sort { get; set; } = string.Empty;
    public string Os { get; set; } = SystemInfoUtils.UserOs;
    public int Page { get; set; }
    public bool IsAdult { get; set; }
}

// wsapi
public class WsTokenRequest
{
    public string Token { get; set; } = string.Empty;
}

[JsonSerializable(typeof(UserOsBaseRequest))]
[JsonSerializable(typeof(FloorBaseRequest))]
[JsonSerializable(typeof(MyGameSearch))]
[JsonSerializable(typeof(MyGameFavoriteRequest))]
[JsonSerializable(typeof(MyGameViewFlagRequest))]
[JsonSerializable(typeof(ProductBaseWithGameOsRequest))]
[JsonSerializable(typeof(UpdateNotificationRequest))]
[JsonSerializable(typeof(UpdateDisplayProfileRequest))]
[JsonSerializable(typeof(GameTypeRequest))]
[JsonSerializable(typeof(GameTypeData))]
[JsonSerializable(typeof(GameTypeDataWithSystemInfo))]
[JsonSerializable(typeof(RemoveGameRequest))]
[JsonSerializable(typeof(ProductBaseRequest))]
[JsonSerializable(typeof(ConfirmAgreementRequest))]
[JsonSerializable(typeof(UserSettingRequest))]
[JsonSerializable(typeof(GetCookieRequest))]
[JsonSerializable(typeof(LaunchClRequest))]
[JsonSerializable(typeof(SystemInfoBaseRequest))]
[JsonSerializable(typeof(ConfirmHardwareAuthRequest))]
[JsonSerializable(typeof(DocumentAgreementAgreeRequest))]
[JsonSerializable(typeof(ShortcutRequest))]
[JsonSerializable(typeof(RejectCertifiedDeviceRequest))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
public partial class DmmGamePlayerApiRequestBaseContext : JsonSerializerContext
{
}

[JsonSerializable(typeof(WsTokenRequest))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class WsDmmGamePlayerApiRequestBaseContext : JsonSerializerContext
{
}
