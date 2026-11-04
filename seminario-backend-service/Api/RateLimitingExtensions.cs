using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Seminario.Application.Common;

namespace Seminario.Api;

public static class RateLimitingExtensions
{
    /// <summary>Peticiones permitidas por ventana en los endpoints de autenticacion.</summary>
    public const int AuthPermitLimit = 5;

    /// <summary>Duracion de la ventana fija.</summary>
    public static readonly TimeSpan AuthWindow = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Rate limiting de ventana fija particionado por IP: defensa perimetral contra fuerza bruta.
    /// Complementa (no sustituye) el bloqueo de cuenta por intentos fallidos de AuthService.
    /// </summary>
    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(RateLimitPolicies.Auth, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = AuthPermitLimit,
                        Window = AuthWindow,
                        QueueLimit = 0
                    }));

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.Headers.RetryAfter =
                    ((int)AuthWindow.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                context.HttpContext.Response.ContentType = "application/json";

                await context.HttpContext.Response.WriteAsJsonAsync(new ApiErrorResponse
                {
                    Success = false,
                    Message = "Demasiados intentos. Espere un minuto e intente de nuevo.",
                    StatusCode = StatusCodes.Status429TooManyRequests
                }, cancellationToken);
            };
        });

        return services;
    }
}
