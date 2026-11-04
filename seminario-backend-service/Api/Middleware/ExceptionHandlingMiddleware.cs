using Seminario.Application.Common;
using Seminario.Infrastructure.Logging;

namespace Seminario.Api.Middleware;

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
        catch (ApiException ex)
        {
            await WriteResponseAsync(context, ex.StatusCode, ex.Message, ex.Errors);
        }
        catch (Exception ex)
        {
            _logger.ErrorNoControlado(ex, context.Request.Path);
            await WriteResponseAsync(context, StatusCodes.Status500InternalServerError,
                "Ocurrio un error interno. Intente nuevamente.", []);
        }
    }

    private static Task WriteResponseAsync(HttpContext context, int statusCode, string message, List<string> errors)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = new ApiErrorResponse
        {
            Success = false,
            Message = message,
            StatusCode = statusCode,
            Errors = errors
        };

        return context.Response.WriteAsJsonAsync(response, context.RequestAborted);
    }
}
