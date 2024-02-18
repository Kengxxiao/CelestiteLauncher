using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Celestite.Network.Models;

public class CamelCaseJsonStringEnumConverter<TEnum>()
    : JsonStringEnumConverter<TEnum>(JsonNamingPolicy.CamelCase) where TEnum : struct, Enum;

[JsonConverter(typeof(CamelCaseJsonStringEnumConverter<MYGAME_FILTER_FLOOR>))]
public enum MYGAME_FILTER_FLOOR
{
    ALL, FREE, PAID, SUBSCRIPTION
}

[JsonConverter(typeof(CamelCaseJsonStringEnumConverter<REQUEST_FLOOR>))]
public enum REQUEST_FLOOR
{
    [Display(Name = "home")]
    HOME,
    [Display(Name = "free")]
    FREE,
    [Display(Name = "paid")]
    PAID,
    [Display(Name = "premium")]
    PREMIUM
}

public static class REQUEST_FLOOR_Extension
{
    public static string ToCamelCaseString(this REQUEST_FLOOR floor)
    {
        return floor switch
        {
            REQUEST_FLOOR.HOME => "home",
            REQUEST_FLOOR.FREE => "free",
            REQUEST_FLOOR.PAID => "paid",
            REQUEST_FLOOR.PREMIUM => "premium",
            _ => throw new NotSupportedException()
        };
    }
}


[JsonConverter(typeof(CamelCaseJsonStringEnumConverter<MYGAME_SEARCH_PAGE>))]
public enum MYGAME_SEARCH_PAGE
{
    DOWNLOAD, BROWSER
}

[JsonConverter(typeof(CamelCaseJsonStringEnumConverter<MYGAME_FILTER_STATE>))]
public enum MYGAME_FILTER_STATE
{
    ALL, AVAILABLE, NOT_INSTALLED
}

[JsonConverter(typeof(CamelCaseJsonStringEnumConverter<MYGAME_FILTER_GENERAL_ADULT>))]
public enum MYGAME_FILTER_GENERAL_ADULT
{
    ALL, GENERAL, ADULT
}

[JsonConverter(typeof(CamelCaseJsonStringEnumConverter<MYGAME_LIST_VIEW_STYLE_TYPE>))]
public enum MYGAME_LIST_VIEW_STYLE_TYPE
{
    NORMAL_THUMBNAIL, SMALL_THUMBNAIL
}

[JsonConverter(typeof(JsonStringEnumConverter<TApiGameType>))]
public enum TApiGameType
{
    [Display(Name = "GeneralSocialBrowser")]
    GSBROWSER,
    [Display(Name = "AdultSocialBrowser")]
    ASBROWSER,
    [Display(Name = "GeneralChannelBrowser")]
    GCBROWSER,
    [Display(Name = "AdultChannelBrowser")]
    ACBROWSER,
    [Display(Name = "GeneralCl")]
    GCL,
    [Display(Name = "AdultCl")]
    ACL,
    [Display(Name = "GeneralMain")]
    GMAIN,
    [Display(Name = "AdultMain")]
    AMAIN,
    [Display(Name = "GeneralEmulator")]
    GEMULATOR,
    [Display(Name = "AdultEmulator")]
    AEMULATOR,
    [Display(Name = "GeneralDlc")]
    GDLC,
    [Display(Name = "AdultDlc")]
    ADLC,
    [Display(Name = "GeneralFree")]
    GFREE,
    [Display(Name = "AdultFree")]
    AFREE,
    [Display(Name = "GeneralCloud")]
    GCLOUD,
    [Display(Name = "AdultCloud")]
    ACLOUD,
    [Display(Name = "GeneralClCloud")]
    GCLCLOUD,
    [Display(Name = "AdultClCloud")]
    ACLCLOUD,
    [Display(Name = "GeneralMainCloud")]
    GMAINCLOUD,
    [Display(Name = "AdultMainCloud")]
    AMAINCLOUD
}

public static class TApiGameTypeExtension
{
    public static TApiGameType FromString(string type) => type switch
    {
        "GSBROWSER" => TApiGameType.GSBROWSER,
        "ASBROWSER" => TApiGameType.ASBROWSER,
        "GCBROWSER" => TApiGameType.GCBROWSER,
        "ACBROWSER" => TApiGameType.ACBROWSER,
        "GCL" => TApiGameType.GCL,
        "ACL" => TApiGameType.ACL,
        "GMAIN" => TApiGameType.GMAIN,
        "AMAIN" => TApiGameType.AMAIN,
        "GEMULATOR" => TApiGameType.GEMULATOR,
        "AEMULATOR" => TApiGameType.AEMULATOR,
        "GDLC" => TApiGameType.GDLC,
        "ADLC" => TApiGameType.ADLC,
        "GFREE" => TApiGameType.GFREE,
        "AFREE" => TApiGameType.AFREE,
        "GCLOUD" => TApiGameType.GCLOUD,
        "ACLOUD" => TApiGameType.ACLOUD,
        "GCLCLOUD" => TApiGameType.GCLCLOUD,
        "ACLCLOUD" => TApiGameType.ACLCLOUD,
        "GMAINCLOUD" => TApiGameType.GMAINCLOUD,
        "AMAINCLOUD" => TApiGameType.AMAINCLOUD,
        _ => throw new NotImplementedException()
    };
}

[JsonConverter(typeof(CamelCaseJsonStringEnumConverter<GAME_STATE>))]
public enum GAME_STATE
{
    AVAILABLE,
    MAINTENANCE,
    NEW,
    PRE_BEGIN,
    OPEN_BETA,
    CLOSED_BETA,
    OUT_OF_PRINT,
    RESERVABLE,
    UNAVAILABLE
}

[JsonConverter(typeof(CamelCaseJsonStringEnumConverter<USER_STATE>))]
public enum USER_STATE
{
    NONE,
    RESERVED,
    PRE_REGISTERED,
    PREMIUM_EXPIRED
}

[JsonConverter(typeof(CamelCaseJsonStringEnumConverter<TAPI_GAME_ACTION>))]
public enum TAPI_GAME_ACTION
{
    PLAYABLE,
    CLOUD_PLAYABLE,
    SUSPEND,
    PREMIUM_SUGGEST,
    INACTIVE_DOWNLOAD,
    UNINSTALLABLE,
    NONE
}

[JsonConverter(typeof(JsonStringEnumConverter<FILE_CHECK_TYPE>))]
public enum FILE_CHECK_TYPE
{
    FILELIST,
    VERSION_CODE
}

public readonly struct DmmTypeApiGame
{
    public int Flag { get; init; }
    public TApiGameType GameType { get; init; }
    public bool IsAdult => (Flag & 0x1) > 0;
    public bool IsCl => (Flag & 0x8) > 0;
    public bool IsBrowser => (Flag & 0x2) > 0 || (Flag & 0x4) > 0;
    public bool IsSocialBrowser => (Flag & 0x2) > 0;
    public bool IsChannelBrowser => (Flag & 0x4) > 0;
    public bool IsPkg => (Flag & 0x10) > 0;
    public bool IsEmulator => (Flag & 0x20) > 0;
    public bool IsTrail => (Flag & 0x100) > 0;
    public bool IsDlc => (Flag & 0x80) > 0;

    public static DmmTypeApiGame Get(TApiGameType gameType, int flag) => new()
    {
        GameType = gameType,
        Flag = flag
    };
}

public static class DmmTypeApiGameHelper
{
    private static readonly Dictionary<TApiGameType, DmmTypeApiGame> GameTypes = new()
        {
            { TApiGameType.ASBROWSER, DmmTypeApiGame.Get(TApiGameType.ASBROWSER, 0x3) },
            { TApiGameType.GSBROWSER, DmmTypeApiGame.Get(TApiGameType.GSBROWSER, 0x2) },
            { TApiGameType.ACBROWSER, DmmTypeApiGame.Get(TApiGameType.ACBROWSER, 0x5) },
            { TApiGameType.GCBROWSER, DmmTypeApiGame.Get(TApiGameType.GCBROWSER, 0x4) },
            { TApiGameType.GCL, DmmTypeApiGame.Get(TApiGameType.GCL, 0x8) },
            { TApiGameType.ACL, DmmTypeApiGame.Get(TApiGameType.ACL, 0x9) },
            { TApiGameType.GMAIN, DmmTypeApiGame.Get(TApiGameType.GMAIN, 0x10) },
            { TApiGameType.AMAIN, DmmTypeApiGame.Get(TApiGameType.AMAIN, 0x11) },
            { TApiGameType.GEMULATOR, DmmTypeApiGame.Get(TApiGameType.GEMULATOR, 0x20) },
            { TApiGameType.AEMULATOR, DmmTypeApiGame.Get(TApiGameType.AEMULATOR, 0x21) },
            { TApiGameType.GDLC, DmmTypeApiGame.Get(TApiGameType.GDLC, 0x80) },
            { TApiGameType.ADLC, DmmTypeApiGame.Get(TApiGameType.ADLC, 0x81) },
            { TApiGameType.GFREE, DmmTypeApiGame.Get(TApiGameType.GFREE, 0x110) },
            { TApiGameType.AFREE, DmmTypeApiGame.Get(TApiGameType.AFREE, 0x111) },
            { TApiGameType.GCLOUD, DmmTypeApiGame.Get(TApiGameType.GCLOUD, 0) },
            { TApiGameType.ACLOUD, DmmTypeApiGame.Get(TApiGameType.ACLOUD, 0) },
            { TApiGameType.GCLCLOUD, DmmTypeApiGame.Get(TApiGameType.GCLCLOUD, 0x8) },
            { TApiGameType.ACLCLOUD, DmmTypeApiGame.Get(TApiGameType.ACLCLOUD, 0x9) },
            { TApiGameType.GMAINCLOUD, DmmTypeApiGame.Get(TApiGameType.GMAINCLOUD, 0x10) },
            { TApiGameType.AMAINCLOUD, DmmTypeApiGame.Get(TApiGameType.AMAINCLOUD, 0x11) },
        };

    public static DmmTypeApiGame GetGameTypeDataFromApiGameType(TApiGameType apiType) =>
        GameTypes.GetValueOrDefault(apiType);

    public static string GetSuffix(DmmTypeApiGame apiType)
    {
        if (apiType.IsCl) return string.Intern("cl");
        if (apiType.IsBrowser) return string.Intern("browser");
        if (apiType.IsEmulator) return string.Intern("emulator");
        throw new NotImplementedException();
    }
}
