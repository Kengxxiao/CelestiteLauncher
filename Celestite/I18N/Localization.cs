using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Platform;
using Celestite.Utils;
using ZeroLog;

namespace Celestite.I18N
{
    public static partial class Localization
    {
        public static FrozenDictionary<string, string> LocalizationData { get; set; }
        private const string DefaultLocaleCode = "zh-CN";

        private static readonly Log Logger = LogManager.GetLogger("Localization");

        static Localization()
        {
            ReloadLocale();
        }

        public static void ReloadLocale()
        {
            var localeCode = ConfigUtils.GetLocale();
            if (string.IsNullOrEmpty(localeCode))
                localeCode = CultureInfo.CurrentCulture.Name;
            if (!AssetLoader.Exists(new Uri($"avares://Celestite/Assets/I18N/{localeCode}.json")))
                localeCode = "en-US";
            if (string.IsNullOrEmpty(ConfigUtils.GetLocale()))
                ConfigUtils.SetLanguageCode(localeCode);
            while (true)
            {
                Stream stream;

                var avaResPath = new Uri($"avares://Celestite/Assets/I18N/{localeCode}.json");
                if (AssetLoader.Exists(avaResPath))
                    stream = AssetLoader.Open(avaResPath);
                else
                {
                    var tryFileReadPath = Path.Combine(FileUtils.LocaleFolder, $"{localeCode}.json");
                    if (File.Exists(tryFileReadPath))
                        stream = File.OpenRead(tryFileReadPath);
                    else
                    {
                        localeCode = DefaultLocaleCode;
                        continue;
                    }
                }

                var jsonData = JsonSerializer.Deserialize(stream,
                    DictionaryJsonSerializerContext.Default.DictionaryStringString);
                if (jsonData == null)
                {
                    Logger.Warn("JsonData is null");
                    LocalizationData = new Dictionary<string, string>().ToFrozenDictionary();
                }
                else LocalizationData = jsonData.ToFrozenDictionary();
                stream.Dispose();
                break;
            }
        }

        [JsonSerializable(typeof(Dictionary<string, string>))]
        public partial class DictionaryJsonSerializerContext : JsonSerializerContext { }
    }
}
