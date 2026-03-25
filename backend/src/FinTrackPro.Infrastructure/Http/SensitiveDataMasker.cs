using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace FinTrackPro.Infrastructure.Http;

internal static partial class SensitiveDataMasker
{
    private const string Redacted = "[REDACTED]";

    private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "x-cg-demo-api-key",
        "Cookie"
    };

    private static readonly HashSet<string> SensitiveFormFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "client_secret",
        "client_id"
    };

    [GeneratedRegex(
        @"""(?:email|phone_number|access_token|refresh_token)""\s*:\s*""[^""]*""",
        RegexOptions.IgnoreCase,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex JsonFieldRegex();

    public static Dictionary<string, string> MaskHeaders(HttpHeaders headers)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, values) in headers)
        {
            result[name] = SensitiveHeaders.Contains(name)
                ? Redacted
                : string.Join(", ", values);
        }
        return result;
    }

    public static string MaskFormBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return body;

        var parts = body.Split('&');
        var masked = new string[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            var eqIdx = parts[i].IndexOf('=');
            if (eqIdx < 0) { masked[i] = parts[i]; continue; }

            var key = parts[i][..eqIdx];
            masked[i] = SensitiveFormFields.Contains(Uri.UnescapeDataString(key))
                ? $"{key}={Redacted}"
                : parts[i];
        }
        return string.Join('&', masked);
    }

    public static string MaskJsonBody(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return json;

        return JsonFieldRegex().Replace(json, match =>
        {
            var colonIdx = match.Value.IndexOf(':');
            var keyPart = match.Value[..colonIdx];
            return $"{keyPart}: \"{Redacted}\"";
        });
    }
}
