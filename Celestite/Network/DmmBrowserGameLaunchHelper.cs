using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using AngleSharp;
using AngleSharp.Html.Dom;
using Celestite.I18N;
using Celestite.Network.Models;
using Celestite.Utils;
using Cysharp.Text;
using Cysharp.Threading.Tasks;

namespace Celestite.Network
{
    public class DmmBrowserGameLaunchHelper
    {
        private const string PcPlayBaseGeneral = "https://{0}-play.games.dmm.com";
        private const string PcPlayBaseAdult = "https://{0}-play.games.dmm.co.jp";

        private const string ActivateDmmAddress = "activate.games.dmm.com/browser-game/install";
        private const string HimariProxyActivateAddress =
            "celestite-himari-proxy.kengxxiao.com/3292efe9917f4eafc58f271cfb5b14fe71e53f3d";

        public record BrowserGameLaunchContext(IEnumerable<string> Scripts, string Schema);

        public static async UniTask<bool> TryAddBrowserGame(string productId, bool isNotificationAllowed, bool isDisplayAllowed, bool isSpGame = false)
        {
            using var activateRequest = await HttpHelper.GetAsync(
                $"https://{(ConfigUtils.GetHimariProxyStatus() ? HimariProxyActivateAddress : ActivateDmmAddress)}/{productId}?isNotificationAllowed={isNotificationAllowed.ToString().ToLower()}&isDisplayAllowed={isDisplayAllowed.ToString().ToLower()}", forceMobileUserAgent: isSpGame, addSessionId: ConfigUtils.GetHimariProxyStatus());
            if (activateRequest.Failed) return false;
            if (activateRequest.Value.StatusCode == HttpStatusCode.SeeOther) return true;
            NotificationHelper.Warn(Localization.AddBrowserGameFailed);
            return false;
        }

        public static string GetBrowserGameUrl(string productId, DmmTypeApiGame advancedType, string browserEnv = "PC")
            => ZString.Format("{0}/play/{1}/", ZString.Format(advancedType.IsAdult ? PcPlayBaseAdult : PcPlayBaseGeneral, browserEnv.ToLower()), productId);

        public static async UniTask<BrowserGameLaunchContext?> GetBrowserPageContext(string productId, DmmTypeApiGame advancedType, string browserEnv = "PC")
        {
            if (!advancedType.IsBrowser || !advancedType.IsSocialBrowser)
                throw new InvalidOperationException();

            var request = await HttpHelper.AdvancedGetStringAsync(string.Empty, GetBrowserGameUrl(productId, advancedType, browserEnv));
            if (request.Failed) return null;

            using var page = await HttpHelper.HtmlParserContext.OpenAsync(x => x.Content(request.Value.Item1));
            var scripts = page.QuerySelectorAll("script[type='text/javascript']").Where(x => x is IHtmlScriptElement script && !string.IsNullOrEmpty(script.InnerHtml)).Cast<IHtmlScriptElement>().Select(x => x.InnerHtml);
            return new BrowserGameLaunchContext(scripts.ToList(), request.Value.Item2);
        }

        public static bool FindAndInvokeMissionCall(IEnumerable<string> scripts)
        {
            var missionCall = scripts.FirstOrDefault(x => x.Contains("api-mission.games.dmm.com"));
            if (missionCall == null) return false;
            var apiUrl = ExtractStringVariableValue(missionCall, "apiUrl");
            var data = ExtractStringVariableValue(missionCall, "data");
            if (!string.IsNullOrEmpty(apiUrl) && !string.IsNullOrEmpty(data))
            {
                UniTask.Run(async () => await HttpHelper.PostRawStringAsJsonAsync(apiUrl, data));
                return true;
            }

            return false;
        }

        public static string FindAndInvokeLaunchGame(BrowserGameLaunchContext context)
        {
            var osApiFrame = context.Scripts.FirstOrDefault(x => x.Contains("var gadgetInfo ="));
            if (osApiFrame == null)
                return string.Empty;
            var gadgetInfo = ExtractObject(osApiFrame, "gadgetInfo");
            return gadgetInfo.TryGetValue("URL", out var url) && !string.IsNullOrEmpty(url) ? ZString.Concat(context.Schema, ':', WebUtility.HtmlDecode(url)) : string.Empty;
        }

        private static string ExtractStringVariableValue(string javascriptCode, string variableName)
        {
            var varDeclaration = $"var {variableName} = '";
            var startIndex = javascriptCode.IndexOf(varDeclaration, StringComparison.Ordinal);

            if (startIndex == -1) return string.Empty;
            startIndex += varDeclaration.Length;
            var endIndex = javascriptCode.IndexOf("'", startIndex, StringComparison.Ordinal);
            return endIndex != -1 ? javascriptCode[startIndex..endIndex] : string.Empty;
        }

        private static Dictionary<string, string> ExtractObject(string javascriptCode, string objectName)
        {
            var extractedValues = new Dictionary<string, string>();
            var objectStart = $"var {objectName} = {{";
            const string objectEnd = "};";

            var startIndex = javascriptCode.IndexOf(objectStart, StringComparison.Ordinal);
            var endIndex = javascriptCode.IndexOf(objectEnd, startIndex, StringComparison.Ordinal);

            if (startIndex == -1 || endIndex == -1) return extractedValues;
            var objectContent = javascriptCode.Substring(startIndex + objectStart.Length, endIndex - startIndex - objectStart.Length);

            // Split the object content by commas
            var keyValuePairs = objectContent.Split(',');

            foreach (var pair in keyValuePairs)
            {
                var parts = pair.Split(':');

                if (parts.Length != 2) continue;
                var key = parts[0].Trim();
                var value = parts[1].Trim().Trim('\'', '"', ',', ' ');

                extractedValues[key] = value;
            }

            return extractedValues;
        }
    }
}
