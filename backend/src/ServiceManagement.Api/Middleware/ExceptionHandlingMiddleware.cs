using System.Text.Json;
using FluentValidation;
using ServiceManagement.Application.Common.Exceptions;
using ServiceManagement.Application.Common.Models;

namespace ServiceManagement.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = 500;
        var message = "An unexpected error occurred.";
        IReadOnlyList<string>? errors = null;

        switch (exception)
        {
            case AppException appException:
                statusCode = appException.StatusCode;
                message = appException.Message;
                break;
            case ValidationException validationException:
                statusCode = 400;
                message = "Validation failed.";
                errors = validationException.Errors.Select(e => e.ErrorMessage).ToList();
                break;
            default:
                _logger.LogError(exception, "Unhandled exception");
                if (_env.IsDevelopment())
                {
                    message = exception.Message;
                }
                break;
        }

        if (statusCode is 401 or 403)
        {
            _logger.LogWarning("Auth/authorization failure: {Message}", message);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        // Belt-and-suspenders: ensure browsers can read API error bodies cross-origin.
        var origin = context.Request.Headers.Origin.ToString();
        if (!string.IsNullOrWhiteSpace(origin) && !context.Response.Headers.ContainsKey("Access-Control-Allow-Origin"))
        {
            context.Response.Headers.Append("Access-Control-Allow-Origin", origin);
            context.Response.Headers.Append("Vary", "Origin");
        }

        var payload = ApiResponse<object>.Fail(message, errors);
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
