using System.Net;
using System.Text.Json;
using FinTrackPro.Application.Common.Exceptions;
using FinTrackPro.Domain.Exceptions;

namespace FinTrackPro.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, title, errors) = exception switch
        {
            ValidationException ve => (
                HttpStatusCode.BadRequest,
                "Validation failed",
                (object)ve.Errors),
            DomainException de => (
                HttpStatusCode.BadRequest,
                de.Message,
                (object)new Dictionary<string, string[]>()),
            NotFoundException nfe => (
                HttpStatusCode.NotFound,
                nfe.Message,
                (object)new Dictionary<string, string[]>()),
            _ => (
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred.",
                (object)new Dictionary<string, string[]>())
        };

        context.Response.StatusCode = (int)statusCode;

        var response = new { title, errors };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
