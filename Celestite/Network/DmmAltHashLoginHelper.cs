using System;
using System.Linq;
using System.Net;
using Celestite.I18N;
using Cysharp.Text;
using Cysharp.Threading.Tasks;

namespace Celestite.Network
{
    public class AltHashLoginResponse
    {
        public Cookie HasAltHash { get; set; } = null!;
        public Cookie AltHash { get; set; } = null!;
        public string LoginSecureId { get; set; } = string.Empty;
        public string LoginSessionId { get; set; } = string.Empty;
    }
    public static class DmmAltHashLoginHelper
    {
        public static async UniTask<DmmOpenApiResult<AltHashLoginResponse>> LoginFromAltHash(string altHash, string hasAltHash)
        {
            var loginUrl = await DmmGamePlayerApiHelper.GetLoginUrl();
            if (loginUrl.Failed) return DmmOpenApiResult.Fail<AltHashLoginResponse>(loginUrl.Error!);

            HttpHelper.ClearCookies();
            HttpHelper.PushCookieToContainer([
                new Cookie("has_althash", hasAltHash, "/", ".dmm.com"),
                new Cookie("althash", altHash, "/", ".dmm.com")
            ]);

            using var response = await HttpHelper.GetResponseHeaderAsync(loginUrl.Value.Url);
            if (response.Failed) return DmmOpenApiResult.Fail<AltHashLoginResponse>(response.Exception);
            if (response.Value.StatusCode != HttpStatusCode.Found || response.Value.Headers.Location == null)
                return DmmOpenApiResult.Fail<AltHashLoginResponse>(Localization.DmmLoginLocationNotFound);
            // TODO: 这里是否可以优化？
            var nextUrl = response.Value.Headers.Location.ToString().StartsWith("http") ? response.Value.Headers.Location : new Uri(ZString.Format("https://accounts.dmm.com{0}", response.Value.Headers.Location));
            using var authenticationResponse = await HttpHelper.GetResponseHeaderAsync(nextUrl);
            if (authenticationResponse.Failed) return DmmOpenApiResult.Fail<AltHashLoginResponse>(authenticationResponse.Exception);
            if (authenticationResponse.Value.StatusCode != HttpStatusCode.OK)
                return DmmOpenApiResult.Fail<AltHashLoginResponse>(Localization.DmmLoginLocationNotFound);
            var cookies = HttpHelper.GetAllCookies();
            return DmmOpenApiResult.Ok(new AltHashLoginResponse
            {
                HasAltHash = cookies.Single(x => x.Name == "has_althash"),
                AltHash = cookies.Single(x => x.Name == "althash"),
                LoginSecureId = cookies.Single(x => x.Name == "login_secure_id").Value,
                LoginSessionId = cookies.Single(x => x.Name == "login_session_id").Value
            });
        }
    }
}
