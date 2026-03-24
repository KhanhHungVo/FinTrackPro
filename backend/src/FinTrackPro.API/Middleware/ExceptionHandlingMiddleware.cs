using System.Net;
using System.Text.Json;
using FinTrackPro.Application.Common.Exceptions;
using FinTrackPro.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace FinTrackPro.API.Middleware;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment env)
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogDebug("Request cancelled by client: {Method} {Path}",
                context.Request.Method, context.Request.Path);

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 499; // Client Closed Request
            }
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            logger.LogWarning(exception,
                "Response already started, cannot write error response for {Method} {Path}",
                context.Request.Method, context.Request.Path);
            return;
        }

        var (statusCode, title, logLevel, errors) = exception switch
        {
            ValidationException ve => (
                HttpStatusCode.BadRequest,
                "Validation failed",
                LogLevel.Warning,
                ve.Errors),
            AuthorizationException ae => (
                HttpStatusCode.Forbidden,
                ae.Message,
                LogLevel.Warning,
                (IDictionary<string, string[]>)new Dictionary<string, string[]>()),
            ConflictException ce => (
                HttpStatusCode.Conflict,
                ce.Message,
                LogLevel.Warning,
                (IDictionary<string, string[]>)new Dictionary<string, string[]>()),
            DomainException de => (
                HttpStatusCode.BadRequest,
                de.Message,
                LogLevel.Warning,
                (IDictionary<string, string[]>)new Dictionary<string, string[]>()),
            NotFoundException nfe => (
                HttpStatusCode.NotFound,
                nfe.Message,
                LogLevel.Warning,
                (IDictionary<string, string[]>)new Dictionary<string, string[]>()),
            JsonException => (
                HttpStatusCode.BadRequest,
                "Invalid request format.",
                LogLevel.Warning,
                (IDictionary<string, string[]>)new Dictionary<string, string[]>()),
            ArgumentNullException { ParamName: "request" } => (
                HttpStatusCode.BadRequest,
                "Request body is required.",
                LogLevel.Warning,
                (IDictionary<string, string[]>)new Dictionary<string, string[]>()),
            _ => (
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred.",
                LogLevel.Error,
                (IDictionary<string, string[]>)new Dictionary<string, string[]>())
        };

        logger.Log(logLevel, exception, "Exception on {Method} {Path}", context.Request.Method, context.Request.Path);

        var problem = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;

        if (errors.Count > 0)
            problem.Extensions["errors"] = errors;

        if (env.IsDevelopment() && statusCode == HttpStatusCode.InternalServerError)
            problem.Detail = exception.ToString();

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problem, _jsonOptions),
            context.RequestAborted);
    }
}
