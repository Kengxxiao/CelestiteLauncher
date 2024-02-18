using System.Collections.Concurrent;
using System.Net.Http;
using static Celestite.Network.HttpHelper;

namespace Celestite.Network.CelestiteBypassCore
{
    public static class HttpHelperImplementation
    {
        public static void HttpClientImpl(this HttpClient httpClient)
        {
        }

        public static void HttpClientImpl2(ConcurrentDictionary<string, ARecordWithExpiredTime> dnsTimeToLiveCache)
        {
        }

        public static string HttpClientImpl3() => string.Empty;
    }
}
