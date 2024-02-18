using System;
using System.Text.Json;
using Celestite.Configs;

namespace Celestite.Utils
{
    public static class JwtUtils
    {
        private static void Split(ReadOnlySpan<char> text, out ReadOnlySpan<char> header, out ReadOnlySpan<char> payload, out ReadOnlySpan<char> headerAndPayload, out ReadOnlySpan<char> signature)
        {
            header = default;
            payload = default;
            signature = default;
            headerAndPayload = default;

            var foundHeader = false;
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] != '.') continue;
                if (!foundHeader)
                {
                    header = text[..i];
                    foundHeader = true;
                }
                else
                {
                    var offset = header.Length + 1;
                    payload = text[offset..i];
                    headerAndPayload = text[..(offset + i - offset)];
                    signature = text[(i + 1)..];
                    break;
                }
            }
        }
        public static bool TryParseIdToken(string idToken, out IdToken? outIdToken)
        {
            outIdToken = null;
            Split(idToken.AsSpan(), out var header, out var payload, out var headerAndPayload,
                out var signature);
            Span<byte> bytes = stackalloc byte[CyBase64.GetMaxBase64UrlDecodeLength(payload.Length)];
            if (!CyBase64.TryFromBase64UrlChars(payload, bytes, out var bytesWritten)) return false;
            outIdToken =
                JsonSerializer.Deserialize(bytes[..bytesWritten], IdTokenJsonSerializerContext.Default.IdToken);
            return true;
        }
    }
}
