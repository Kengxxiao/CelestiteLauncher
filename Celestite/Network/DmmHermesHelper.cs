using System.Collections.Generic;
using System.Text.Json.Serialization;
using Celestite.Utils;
using Cysharp.Threading.Tasks;

namespace Celestite.Network
{
    public class HermesGameData
    {
        public string Id { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
    }

    public class HermesGraphQlNewGames
    {
        public List<HermesGameData> Games { get; set; } = [];
    }

    public class HermesGraphQlCompleteMission
    {
        public bool CompleteMyGameAccessMission { get; set; }
    }

    public class HermesData<T> where T : class
    {
        public T? Data { get; set; } = null;
    }

    [JsonSerializable(typeof(HermesData<HermesGraphQlNewGames>))]
    [JsonSerializable(typeof(HermesData<HermesGraphQlCompleteMission>))]
    [JsonSerializable(typeof(HermesRequest))]
    [JsonSerializable(typeof(int))]
    [JsonSerializable(typeof(bool))]
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    public partial class HermesJsonContext : JsonSerializerContext { }

    public class HermesRequest
    {
        public string OperationName { get; set; } = string.Empty;
        public Dictionary<string, object> Variables { get; set; } = [];
        public string Query { get; set; } = string.Empty;
    }

    public class DmmHermesHelper
    {
        private const string HermesBaseGeneral = "https://hermes.games.dmm.com";
        private const string HermesBaseAdult = "https://hermes.games.dmm.co.jp";

        private const string HimariHermesProxyGeneral =
            "https://celestite-himari-proxy.kengxxiao.com/2a9f5cbbebe7e113e1b019ca8d732df6609d46be";
        private const string HimariHermesProxyAdult =
            "https://celestite-himari-proxy.kengxxiao.com/f5d5ed8372ccfc82ce7c467eb6bd56b9b0ec652e";

        private static string GetUrl(bool isAdult)
        {
            string hermesTarget;
            if (ConfigUtils.GetHimariProxyStatus())
                hermesTarget = isAdult ? HimariHermesProxyAdult : HimariHermesProxyGeneral;
            else
                hermesTarget = isAdult ? HermesBaseAdult : HermesBaseGeneral;
            return hermesTarget;
        }

        public static async UniTask<List<HermesGameData>> GetGameList(bool isAdult, string type = "PC")
        {
            var hermesResponse = await HttpHelper.PostJsonAsync(GetUrl(isAdult), string.Empty, new HermesRequest
            {
                OperationName = "GetGameList",
                Variables = new Dictionary<string, object>
                {
                    {"viewerDevice", "PC"},
                    {"gameCategory", $"{type}GAME"}
                },
                Query = "query GetGameList($viewerDevice: ViewerDevice!, $gameCategory: GameCategory!) { games(viewerDevice: $viewerDevice, gameCategory: $gameCategory) { link, id } }"
            }, HermesJsonContext.Default.HermesRequest, HermesJsonContext.Default.HermesDataHermesGraphQlNewGames);
            if (hermesResponse.Failed || hermesResponse.Value.Data == null) return [];
            return hermesResponse.Value.Data.Games;
        }

        public static async UniTask<bool> CompleteMyGameAccessMission(bool isAdult, bool forceMobileUserAgent)
        {
            var hermesResponse = await HttpHelper.PostJsonAsync(GetUrl(isAdult), string.Empty, new HermesRequest
            {
                OperationName = "ClientMissionMutation",
                Variables = [],
                Query = "mutation ClientMissionMutation {completeMyGameAccessMission}"
            }, HermesJsonContext.Default.HermesRequest, HermesJsonContext.Default.HermesDataHermesGraphQlCompleteMission, forceMobileUserAgent: forceMobileUserAgent, addSessionId: ConfigUtils.GetHimariProxyStatus());
            return !hermesResponse.Failed && hermesResponse.Value.Data is { CompleteMyGameAccessMission: true };
        }
    }
}
