using System.Net;
using System.Text.Json;

namespace AssignmentSystem.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
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
            _logger.LogError(ex, "An unhandled exception has occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            message = exception.Message,
            details = _env.IsDevelopment() ? exception.StackTrace : null
        };

        switch (exception)
        {
            case UnauthorizedAccessException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                break;
            case KeyNotFoundException:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                break;
            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response = new
                {
                    message = "An unexpected error occurred.",
                    details = _env.IsDevelopment() ? exception.StackTrace : null
                };
                break;
        }

        var result = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(result);
    }
}
