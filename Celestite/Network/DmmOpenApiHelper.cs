using System;
using System.Net.Http.Headers;
using System.Text.Json;
using Celestite.Network.Models;
using Celestite.Utils;
using Cysharp.Threading.Tasks;
using ZeroLog;

namespace Celestite.Network
{
    public class DmmOpenApiResult
    {
        private static readonly Log Logger = LogManager.GetLogger("DmmOpenApi");
        public bool Success { get; }
        public bool Failed => !Success;
        public DmmOpenApiErrorBody? Error { get; }

        protected DmmOpenApiResult(bool success, DmmOpenApiErrorBody? error = null)
        {
            switch (success)
            {
                case true when error != null:
                    throw new InvalidOperationException();
                case false when error == null:
                    throw new InvalidOperationException();
            }

            Success = success;
            Error = error;

            if (!success)
            {
                Logger.Warn(error!.Reason);
                NotificationHelper.Error(error!.Reason);
            }
        }

        protected DmmOpenApiResult(Exception error)
        {
            Success = false;
            Error = new DmmOpenApiErrorBody
            {
                Code = "E999001",
                Reason = error!.Message
            };
            /*
            Logger.Error(string.Empty, error);
            NotificationHelper.Error(error!.Message);*/
        }

        public static DmmOpenApiResult<T> Ok<T>(T value) where T : class
        {
            return new DmmOpenApiResult<T>(value, true, null);
        }

        public static DmmOpenApiResult<T> Fail<T>(DmmOpenApiErrorBody error) where T : class
        {
            return new DmmOpenApiResult<T>(null, false, error);
        }
        public static DmmOpenApiResult<T> Fail<T>(Exception error) where T : class
        {
            return new DmmOpenApiResult<T>(error);
        }
        public static DmmOpenApiResult<T> Fail<T>(string errorMessage) where T : class
        {
            return new DmmOpenApiResult<T>(false, new DmmOpenApiErrorBody
            {
                Code = "E999002",
                Reason = errorMessage
            });
        }
        public static DmmOpenApiResult Fail(DmmOpenApiErrorBody error)
        {
            return new DmmOpenApiResult(false, error);
        }
        public static DmmOpenApiResult Fail(Exception error)
        {
            return new DmmOpenApiResult(error);
        }
        public static DmmOpenApiResult Fail(string errorMessage)
        {
            return new DmmOpenApiResult(false, new DmmOpenApiErrorBody
            {
                Code = "E999002",
                Reason = errorMessage
            });
        }
    }

    public class DmmOpenApiResult<T> : DmmOpenApiResult where T : class
    {
        private readonly T? _value;
        public T Value => _value!;

        protected internal DmmOpenApiResult(T? value, bool success, DmmOpenApiErrorBody? error = null) : base(success, error)
        {
            _value = value;
        }
        protected internal DmmOpenApiResult(bool success, DmmOpenApiErrorBody? error = null) : base(success, error)
        {
        }

        protected internal DmmOpenApiResult(Exception error) : base(error)
        {
        }
    }
    public static class DmmOpenApiHelper
    {
        private const string ApiBaseUrl = "https://evo.dmmapis.com";
        private static readonly string DefaultAuthKey = Convert.ToBase64String("9p69sajOH9pdsHatYIDGebf81AgR:yaVBs60OAp4vR4XqR0S1DpotRCYiJkft"u8);
        private static readonly AuthenticationHeaderValue DefaultAuthValue = new("Basic", DefaultAuthKey);
        private static string _currentAccessToken = string.Empty;
        private static string _currentRefreshToken = string.Empty;

        public static void UpdateAccessToken(string accessToken)
        {
            _currentAccessToken = accessToken;
        }
        public static void UpdateRefreshToken(string refreshToken)
        {
            _currentRefreshToken = refreshToken;
        }


        public static async UniTask<DmmOpenApiResult<TokenResponse>> Login(string email, string password)
        {
            var json = await HttpHelper.PostJsonWithAuthorizationAsync(ApiBaseUrl, "/connect/v1/token",
                new TokenRequestPassword
                {
                    Email = email,
                    Password = password
                }, DmmOpenApiRequestBaseContext.Default.TokenRequestPassword, DmmOpenApiResponseBaseContext.Default.DmmOpenApiResponse, DefaultAuthValue);
            if (json.Failed) return DmmOpenApiResult.Fail<TokenResponse>(json.Exception);
            if (json.Value.Header.ResultCode.Equals("0"))
            {
                var tokenResponse = json.Value.Body.Deserialize(DmmOpenApiResponseBaseContext.Default.TokenResponse)!;
                if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
                    UpdateRefreshToken(tokenResponse.RefreshToken);
                return DmmOpenApiResult.Ok(tokenResponse);
            }
            var error = json.Value.Body.Deserialize(DmmOpenApiResponseBaseContext.Default.DmmOpenApiErrorBody)!;
            return DmmOpenApiResult.Fail<TokenResponse>(error);
        }

        public static async UniTask<DmmOpenApiResult<TokenResponse>> ExchangeAccessToken(string accessToken)
        {
            var json = await HttpHelper.PostJsonWithAuthorizationAsync(ApiBaseUrl, "/connect/v1/token",
                new TokenRequestAccessToken
                {
                    AccessToken = accessToken
                }, DmmOpenApiRequestBaseContext.Default.TokenRequestAccessToken, DmmOpenApiResponseBaseContext.Default.DmmOpenApiResponse, DefaultAuthValue);
            if (json.Failed) return DmmOpenApiResult.Fail<TokenResponse>(json.Exception);
            if (json.Value.Header.ResultCode.Equals("0"))
            {
                var tokenResponse = json.Value.Body.Deserialize(DmmOpenApiResponseBaseContext.Default.TokenResponse)!;
                if (!string.IsNullOrEmpty(tokenResponse.AccessToken))
                    UpdateAccessToken(tokenResponse.AccessToken);
                return DmmOpenApiResult.Ok(tokenResponse);
            }
            var error = json.Value.Body.Deserialize(DmmOpenApiResponseBaseContext.Default.DmmOpenApiErrorBody)!;
            return DmmOpenApiResult.Fail<TokenResponse>(error);
        }

        public static async UniTask<DmmOpenApiResult<SessionIdResponse>> IssueSessionId(string userId)
        {
            if (string.IsNullOrEmpty(_currentAccessToken))
                throw new InvalidOperationException();
            var json = await HttpHelper.PostJsonWithAuthorizationAsync(ApiBaseUrl, "/connect/v1/issueSessionId",
                new IssueSessionIdRequest
                {
                    UserId = userId
                }, DmmOpenApiRequestBaseContext.Default.IssueSessionIdRequest, DmmOpenApiResponseBaseContext.Default.DmmOpenApiResponse, new AuthenticationHeaderValue("Bearer", _currentAccessToken));
            if (json.Failed) return DmmOpenApiResult.Fail<SessionIdResponse>(json.Exception);
            if (json.Value.Header.ResultCode.Equals("0"))
            {
                return DmmOpenApiResult.Ok(json.Value.Body.Deserialize(DmmOpenApiResponseBaseContext.Default.SessionIdResponse)!);
            }
            var error = json.Value.Body.Deserialize(DmmOpenApiResponseBaseContext.Default.DmmOpenApiErrorBody)!;
            return DmmOpenApiResult.Fail<SessionIdResponse>(error);
        }

        public static async UniTask<DmmOpenApiResult<TokenResponse>> RefreshToken()
        {
            var json = await HttpHelper.PostJsonWithAuthorizationAsync(ApiBaseUrl, "/connect/v1/token",
                new RefreshTokenRequest()
                {
                    RefreshToken = _currentRefreshToken
                }, DmmOpenApiRequestBaseContext.Default.RefreshTokenRequest, DmmOpenApiResponseBaseContext.Default.DmmOpenApiResponse, DefaultAuthValue);
            if (json.Failed) return DmmOpenApiResult.Fail<TokenResponse>(json.Exception);
            if (json.Value.Header.ResultCode.Equals("0"))
            {
                var tokenResponse = json.Value.Body.Deserialize(DmmOpenApiResponseBaseContext.Default.TokenResponse)!;
                if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
                    UpdateRefreshToken(tokenResponse.RefreshToken);
                if (!string.IsNullOrEmpty(tokenResponse.AccessToken))
                    UpdateAccessToken(tokenResponse.AccessToken);
                return DmmOpenApiResult.Ok(tokenResponse);
            }
            var error = json.Value.Body.Deserialize(DmmOpenApiResponseBaseContext.Default.DmmOpenApiErrorBody)!;
            return DmmOpenApiResult.Fail<TokenResponse>(error);
        }
    }
}
