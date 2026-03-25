using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinTrackPro.Infrastructure.Http;

internal sealed class LoggingDelegatingHandler(
    ILogger<LoggingDelegatingHandler> logger,
    IOptions<HttpLoggingOptions> options) : DelegatingHandler
{
    private const int MaxBodyLength = 2000;
    private const long MaxBufferBytes = 256 * 1024; // 256 KB

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var mask = options.Value.MaskSensitiveData;

        var requestHeaders = mask
            ? SensitiveDataMasker.MaskHeaders(request.Headers)
            : request.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value));
        var requestBody = await ReadBodyAsync(request.Content);
        var loggedRequestBody = mask ? MaskBody(request.Content, requestBody) : requestBody;

        logger.LogInformation(
            "HTTP {Method} {Uri} | Headers: {@RequestHeaders} | Body: {RequestBody}",
            request.Method,
            request.RequestUri,
            requestHeaders,
            loggedRequestBody ?? "(no body)");

        var stopwatch = Stopwatch.StartNew();
        var response = await base.SendAsync(request, cancellationToken);
        stopwatch.Stop();

        var responseBody = await ReadBodyAsync(response.Content);
        var loggedResponseBody = mask ? MaskBody(response.Content, responseBody) : responseBody;
        var responseHeaders = mask
            ? SensitiveDataMasker.MaskHeaders(response.Headers)
            : response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value));

        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation(
                "HTTP {Method} {Uri} responded {StatusCode} in {ElapsedMs}ms | Headers: {@ResponseHeaders} | Body: {ResponseBody}",
                request.Method,
                request.RequestUri,
                (int)response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                responseHeaders,
                loggedResponseBody ?? "(no body)");
        }
        else
        {
            logger.LogWarning(
                "HTTP {Method} {Uri} responded {StatusCode} in {ElapsedMs}ms | Headers: {@ResponseHeaders} | Body: {ResponseBody}",
                request.Method,
                request.RequestUri,
                (int)response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                responseHeaders,
                loggedResponseBody ?? "(no body)");
        }

        return response;
    }

    private static async Task<string?> ReadBodyAsync(HttpContent? content)
    {
        if (content is null) return null;
        if (!IsTextContent(content)) return null;

        if (content.Headers.ContentLength is long len && len > MaxBufferBytes)
            return $"[body too large to log, {len} bytes]";

        await content.LoadIntoBufferAsync();
        var raw = await content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(raw)) return null;

        return raw.Length > MaxBodyLength
            ? $"{raw[..MaxBodyLength]}... [truncated, total {raw.Length} chars]"
            : raw;
    }

    private static string? MaskBody(HttpContent? content, string? body)
    {
        if (content is null || body is null)
        {
            if (content is not null && !IsTextContent(content))
            {
                var size = content.Headers.ContentLength is long len
                    ? $"{len} bytes"
                    : "unknown size";
                return $"[binary content, {size}]";
            }
            return body;
        }

        var mediaType = content.Headers.ContentType?.MediaType ?? string.Empty;

        if (mediaType.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
            return SensitiveDataMasker.MaskFormBody(body);

        if (mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase)
            || mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
            return SensitiveDataMasker.MaskJsonBody(body);

        return body;
    }

    private static bool IsTextContent(HttpContent? content)
    {
        if (content is null) return false;
        var mediaType = content.Headers.ContentType?.MediaType;
        if (mediaType is null) return false;
        return mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
            || mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase)
            || mediaType.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);
    }
}
