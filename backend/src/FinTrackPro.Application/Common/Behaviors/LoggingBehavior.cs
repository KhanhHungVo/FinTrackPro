using System.Collections;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FinTrackPro.Application.Common.Models;

namespace FinTrackPro.Application.Common.Behaviors;

public class LoggingBehaviorOptions
{
    public const string SectionName = "LoggingBehavior";
    public int SlowHandlerThresholdMs { get; set; } = 500;
}

public class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    IOptions<LoggingBehaviorOptions> options)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        if (logger.IsEnabled(LogLevel.Debug))
        {
            var serialisedRequest = JsonSerializer.Serialize(request, JsonOptions);
            logger.LogDebug("{RequestName} request: {Request}", requestName, serialisedRequest);
        }

        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        var elapsed = sw.ElapsedMilliseconds;
        var threshold = options.Value.SlowHandlerThresholdMs;

        if (elapsed >= threshold)
        {
            if (logger.IsEnabled(LogLevel.Warning))
                logger.LogWarning(
                    "Slow handler {RequestName} completed in {ElapsedMs}ms (threshold: {ThresholdMs}ms){Summary}",
                    requestName, elapsed, threshold, BuildSummary(response));
        }
        else
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation(
                    "Handled {RequestName} in {ElapsedMs}ms{Summary}",
                    requestName, elapsed, BuildSummary(response));
        }

        if (logger.IsEnabled(LogLevel.Debug))
        {
            var serialisedResponse = JsonSerializer.Serialize(response, JsonOptions);
            logger.LogDebug("{RequestName} response: {Response}", requestName, serialisedResponse);
        }

        return response;
    }

    private static string BuildSummary(TResponse response)
    {
        if (response is null) return string.Empty;

        var type = response.GetType();

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(PagedResult<>))
        {
            var items = type.GetProperty("Items")!.GetValue(response);
            var count = items is ICollection col ? col.Count.ToString() : "?";
            var total = type.GetProperty("TotalCount")!.GetValue(response);
            return $" | Items: {count} / {total}";
        }

        if (response is ICollection collection)
            return $" | Count: {collection.Count}";

        if (response is Guid id)
            return $" | Id: {id}";

        return string.Empty;
    }
}
