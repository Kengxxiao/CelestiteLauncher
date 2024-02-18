using System.Collections.Generic;
using System.Linq;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Cysharp.Threading.Tasks;

namespace Celestite.Network
{
    public class PersonalUninstallContext(string formName, string appId, string token)
    {
        public string FormName { get; set; } = formName;
        public string AppId { get; set; } = appId;
        public string Token { get; set; } = token;
    }
    public static class DmmPersonalAccessHelper
    {
        private const string ApiBase = "https://personal.games.dmm.com";

        public static async UniTask<PersonalUninstallContext?> SearchAppIdOnPersonal(string gameName)
        {
            var pageData = await HttpHelper.GetStringAsync(ApiBase, $"/uninstall/list/");
            if (pageData.Failed) return null;
            using var content = await HttpHelper.HtmlParserContext.OpenAsync(req => req.Content(pageData.Value));
            var pagerArea = content.QuerySelectorAll("ul.area-pager li a").Where(x => x is IHtmlAnchorElement).Cast<IHtmlAnchorElement>()
                .Select(x => x.Href).Distinct();

            var appId = SearchAppIdWorker(content, gameName);
            if (appId != null) return appId;
            var tasks = pagerArea.Select(x => GetAppIdWorker(x, gameName));
            var result = await UniTask.WhenAll(tasks)
                .ContinueWith(results => results.FirstOrDefault(x => x != null));
            return result;
        }

        public static async UniTask<bool> GetUninstallConfirmPageToken(PersonalUninstallContext context)
        {
            var confirmPage = await HttpHelper.PostFormDataThenReceiveStringAsync(ApiBase, "/uninstall/list/confirm", new Dictionary<string, string>
            {
                { context.FormName, context.AppId},
                { "_token", context.Token}
            });
            if (confirmPage.Failed) return false;
            using var content = await HttpHelper.HtmlParserContext.OpenAsync(req => req.Content(confirmPage.Value));
            if (!TryGetToken(content, out var token)) return false;
            context.Token = token!;
            return true;

        }

        public static async UniTask<bool> UninstallGame(PersonalUninstallContext context)
        {
            var uninstallPage = await HttpHelper.PostFormDataThenReceiveStringAsync(ApiBase, "/uninstall/list/uninstall", new Dictionary<string, string>
            {
                { context.FormName, context.AppId},
                { "_token", context.Token}
            });
            return uninstallPage.Success;
        }

        private static bool TryGetToken(IParentNode content, out string? token)
        {
            token = null;
            if (content.QuerySelector("input[name=_token]") is not IHtmlInputElement tokenInput) return false;
            token = tokenInput.DefaultValue;
            return true;
        }

        private static async UniTask<PersonalUninstallContext?> GetAppIdWorker(string path, string gameName)
        {
            var pageData = await HttpHelper.GetStringAsync(string.Empty, path);
            if (pageData.Failed) return null;
            using var content = await HttpHelper.HtmlParserContext.OpenAsync(req => req.Content(pageData.Value));
            return SearchAppIdWorker(content, gameName);
        }

        private static PersonalUninstallContext? SearchAppIdWorker(IParentNode content, string gameName)
        {
            var ntgTable = content.QuerySelectorAll("table.ntg-table tbody tr");
            var appId = ntgTable.FirstOrDefault(x =>
            {
                if (x.Children is not [IHtmlTableDataCellElement _, IHtmlTableDataCellElement name])
                    return false;
                return name.Children is [IHtmlAnchorElement nameAnchor] && nameAnchor.Text == gameName;
            });
            if (appId?.Children[0].Children is not [IHtmlInputElement input] || string.IsNullOrEmpty(input.Name) || string.IsNullOrEmpty(input.Value))
                return null;
            var token = content.QuerySelector("input[name=_token]");
            return TryGetToken(content, out var tokenInput) ? new PersonalUninstallContext(input.Name, input.Value, tokenInput!) : null;
        }
    }
}
