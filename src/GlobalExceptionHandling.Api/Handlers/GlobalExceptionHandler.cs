using GlobalExceptionHandling.Api.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace GlobalExceptionHandling.Api.Handlers;

/// <summary>
/// Central exception handler that intercepts all unhandled exceptions and converts them into
/// RFC 7807 Problem Details responses. Registered via <c>app.UseExceptionHandler()</c>.
///
/// Supported exception types:
/// <list type="bullet">
///   <item><see cref="NotFoundException"/> → 404 Not Found</item>
///   <item><see cref="ValidationException"/> → 400 Bad Request with field errors</item>
///   <item><see cref="ForbiddenException"/> → 403 Forbidden</item>
///   <item>Any other <see cref="Exception"/> → 500 Internal Server Error</item>
/// </list>
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Attempts to handle the exception by writing an appropriate Problem Details response.
    /// Always returns <c>true</c> so the middleware pipeline does not propagate further.
    /// </summary>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Log every unhandled exception for observability
        _logger.LogError(
            exception,
            "Unhandled exception [{ExceptionType}] on {Method} {Path}: {Message}",
            exception.GetType().Name,
            httpContext.Request.Method,
            httpContext.Request.Path,
            exception.Message);

        var (statusCode, title, detail, errors) = exception switch
        {
            NotFoundException notFound =>
                (StatusCodes.Status404NotFound, "Not Found", notFound.Message, (IDictionary<string, string[]>?)null),

            ValidationException validation =>
                (StatusCodes.Status400BadRequest, "Validation Failed", validation.Message, validation.Errors),

            ForbiddenException forbidden =>
                (StatusCodes.Status403Forbidden, "Forbidden", forbidden.Message, (IDictionary<string, string[]>?)null),

            // Catch-all: never leak internal details to the client
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error",
                  "An unexpected error occurred. Please try again later.", (IDictionary<string, string[]>?)null)
        };

        // Set status code and content-type BEFORE writing the body
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        object responseBody;

        if (errors is not null)
        {
            // ValidationProblemDetails for field-level errors
            responseBody = new ValidationProblemDetails(errors)
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = httpContext.Request.Path
            };
        }
        else
        {
            responseBody = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = httpContext.Request.Path
            };
        }

        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(responseBody, responseBody.GetType(), JsonOptions),
            cancellationToken);

        // Return true: exception is handled; pipeline should stop here.
        return true;
    }
}
