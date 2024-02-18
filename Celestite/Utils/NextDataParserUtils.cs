using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using AngleSharp;
using AngleSharp.Html.Dom;
using Celestite.Network;
using Cysharp.Threading.Tasks;
using ZeroLog;

namespace Celestite.Utils
{
    public class DmmNextDataPageProps
    {
        public string Token { get; set; } = string.Empty;
        public string[] Error { get; set; } = [];
        public string RegistrationId { get; set; } = string.Empty;
    }
    public class NextDataProps<T>
    {
        public T? PageProps { get; set; }
    }

    public class GameListPageProps
    {
        [JsonPropertyName("__APOLLO_STATE__")]
        public Dictionary<string, Dictionary<string, JsonElement>> ApolloState { get; set; } = [];
    }

    public class NextData<T>
    {
        public NextDataProps<T>? Props { get; set; } = null;
    }

    [JsonSerializable(typeof(NextData<DmmNextDataPageProps>))]
    [JsonSerializable(typeof(NextData<GameListPageProps>))]
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    public partial class NextDataJsonSerializerContext : JsonSerializerContext
    {
    }

    public static class NextDataParserUtils
    {
        private static readonly Log Logger = LogManager.GetLogger("NextData");
        public static async UniTask<DmmNextDataPageProps?> GetDmmNextData(string htmlWeb)
        {
            try
            {
                using var content = await HttpHelper.HtmlParserContext.OpenAsync(req => req.Content(htmlWeb))
                    .ConfigureAwait(false);

                var nextDataElement = content.GetElementById("__NEXT_DATA__");
                if (nextDataElement is not IHtmlScriptElement nextDataScript) return null;
                var nextData = JsonSerializer.Deserialize(nextDataScript.Text,
                    NextDataJsonSerializerContext.Default.NextDataDmmNextDataPageProps);
                return nextData?.Props?.PageProps;
            }
            catch (Exception ex)
            {
                Logger.Error(string.Empty, ex);
                return null;
            }
        }
    }
}
