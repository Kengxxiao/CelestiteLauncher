using System.Text.Json.Serialization;

namespace Celestite.Network.Models;

public class TokenRequestPassword
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string GrantType => "password";
}

public class TokenRequestAccessToken
{
    public string AccessToken { get; set; } = string.Empty;
    public string GrantType => "exchange_token";
}

public class IssueSessionIdRequest
{
    public string UserId { get; set; } = string.Empty;
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
    public string GrantType { get; } = "refresh_token";
}

[JsonSerializable(typeof(TokenRequestPassword))]
[JsonSerializable(typeof(TokenRequestAccessToken))]
[JsonSerializable(typeof(IssueSessionIdRequest))]
[JsonSerializable(typeof(RefreshTokenRequest))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
public partial class DmmOpenApiRequestBaseContext : JsonSerializerContext { }
