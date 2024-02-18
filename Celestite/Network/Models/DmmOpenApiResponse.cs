using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Celestite.Network.Models;

public class DmmOpenApiResponse
{
    public class ResponseHeader
    {
        public string ResponseId { get; set; } = string.Empty;
        public string ResultCode { get; set; } = string.Empty;
    }

    public ResponseHeader Header { get; set; } = null!;
    public JsonElement Body { get; set; }
}

public class DmmOpenApiErrorBody
{
    public string Code { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public List<ValidationError> ValidationErrors { get; set; } = [];
}
public class ValidationError
{
    public string Key { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public long ExpiresIn { get; set; }
    public string IdToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string TokenType { get; set; } = string.Empty;
}

public class SessionIdResponse
{
    public string SecureId { get; set; } = string.Empty;
    public string UniqueId { get; set; } = string.Empty;
}

[JsonSerializable(typeof(DmmOpenApiResponse))]
[JsonSerializable(typeof(DmmOpenApiErrorBody))]

[JsonSerializable(typeof(TokenResponse))]
[JsonSerializable(typeof(SessionIdResponse))]

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
public partial class DmmOpenApiResponseBaseContext : JsonSerializerContext
{
}
