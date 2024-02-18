using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Platform;

namespace FluentAvalonia.UI;

/// <summary>
/// Helper class for storing localized string for FluentAvalonia/WinUI controls
/// </summary>
/// <remarks>
/// The string resources are taken from the WinUI repo. Not all resources in WinUI
/// may be available here, only those that are known to be used in a control
/// </remarks>
public partial class FALocalizationHelper
{
    [JsonSerializable(typeof(LocalizationMap))]
    private partial class FALocalizationJsonSerializerContext : JsonSerializerContext
    {
    }

    private FALocalizationHelper()
    {
        using var al = AssetLoader.Open(new Uri("avares://FluentAvalonia/Assets/ControlStrings.json"));
        _mappings = JsonSerializer.Deserialize(al, FALocalizationJsonSerializerContext.Default.LocalizationMap);
    }

    static FALocalizationHelper()
    {
        Instance = new FALocalizationHelper();
    }

    public static FALocalizationHelper Instance { get; }

    /// <summary>
    /// Gets a string resource by the specified name using the CurrentCulture
    /// </summary>
    public string GetLocalizedStringResource(string resName) =>
        GetLocalizedStringResource(CultureInfo.CurrentCulture, resName);

    /// <summary>
    /// Gets a string resource by the specified name and using the specified culture
    /// </summary>
    /// <remarks>
    /// InvariantCulture is not supported here and will default to en-US
    /// </remarks>
    public string GetLocalizedStringResource(CultureInfo ci, string resName)
    {
        // Don't allow InvariantCulture - default to en-us in that case
        if (Equals(ci, CultureInfo.InvariantCulture))
            ci = new CultureInfo(s_enUS);

        if (!_mappings.TryGetValue(resName, out var mappingsResName))
            return string.Empty;
        if (mappingsResName.TryGetValue(ci.Name, out var ciName))
            return ciName;
        return mappingsResName.TryGetValue(s_enUS, out var ciEnUs) ? ciEnUs : string.Empty;
    }

    // <ResourceName, Entries>
    private readonly LocalizationMap _mappings;
    private static readonly string s_enUS = "en-US";

    /// <summary>
    /// Dictionary of language entries for a resource name. &lt;language, value&gt; where
    /// language is the abbreviated name, e.g., en-US
    /// </summary>
    public class LocalizationEntry() : Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

    private class LocalizationMap() : Dictionary<string, LocalizationEntry>(StringComparer.InvariantCultureIgnoreCase);
}
