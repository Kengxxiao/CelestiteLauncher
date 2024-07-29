using Celestite.Utils;

namespace Celestite.Network.Models;

public sealed class MyGameSearch
{
    public MYGAME_SEARCH_PAGE Page { get; set; }
    public MYGAME_FILTER_STATE State { get; set; }
    public MYGAME_FILTER_GENERAL_ADULT Store { get; set; }
    public MYGAME_LIST_VIEW_STYLE_TYPE ViewStyle { get; set; }
}

public record GameTypeData : ProductBaseRequest
{
    public string GameOs { get; set; } = string.Empty;
    public TApiGameType GameType { get; set; }

    public GameTypeData(GameInfo gameInfo)
    {
        ProductId = gameInfo.ProductId;
        GameType = gameInfo.Type;
        GameOs = SystemInfoUtils.UserOs;
    }

    public GameTypeData()
    {
        GameOs = SystemInfoUtils.UserOs;
    }
}


public partial class GameInfo
{
    public TApiGameType Type { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public bool HasDetail { get; set; }
    public bool HasUninstall { get; set; }
    public bool AllowShortcut { get; set; }
    public bool AllowVisibleSetting { get; set; }
    // registered_time 未使用
    // last_played 类型不确定

    // TODO: 各类ENUM不完整
    public string State { get; set; } = string.Empty;
    public string UserState { get; set; } = string.Empty;
    public string[] Actions { get; set; } = [];
    public bool IsShowLatestVersion { get; set; }
    public string LatestVersion { get; set; } = string.Empty;
    public bool IsShowFileSize { get; set; }
    public long FileSize { get; set; }
    public bool IsShowAgreementLink { get; set; }
    public string UpdateDate { get; set; } = string.Empty;
}

public sealed class DownloadFileInfo
{
    public string LocalPath { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Hash { get; set; } = string.Empty;
    public bool ProtectedFlg { get; set; }
    public bool ForceDeleteFlg { get; set; }
    public bool CheckHashFlg { get; set; }
    public long Timestamp { get; set; }
}

public sealed class AmazonCookie
{
    public string Policy { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
}