using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace TorreControl.Web.Security;

/// <summary>
/// Límite de intentos de inicio de sesión por dirección IP, para frenar a quien prueba
/// contraseñas en masa. Es GENEROSO a propósito: en una oficina muchas personas salen a
/// internet con la misma IP y todas inician sesión al cambio de turno. El bloqueo por cuenta
/// (5 intentos / 15 min) es la defensa principal; esto solo frena ataques automatizados.
/// </summary>
public static class LimiteLogin
{
    public const string Politica = "login";

    public static IServiceCollection AddLimiteLogin(this IServiceCollection services, IConfiguration config)
    {
        var permitidas = config.GetValue("Seguridad:LoginPeticionesPorIp", 100);
        var ventana = TimeSpan.FromMinutes(config.GetValue("Seguridad:LoginVentanaMinutos", 5));

        return services.AddRateLimiter(opciones =>
        {
            opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            opciones.OnRejected = async (contexto, cancelacion) =>
            {
                contexto.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
                await contexto.HttpContext.Response.WriteAsync(
                    "Demasiados intentos de inicio de sesión desde esta red. Espera unos minutos.", cancelacion);
            };

            opciones.AddPolicy(Politica, contexto => RateLimitPartition.GetSlidingWindowLimiter(
                // Un "bote" de intentos por IP. (Si algún día la app va detrás de un proxy,
                // hay que configurar ForwardedHeaders para leer la IP real del usuario.)
                contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
                _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = permitidas,
                    Window = ventana,
                    SegmentsPerWindow = 5,
                    QueueLimit = 0
                }));
        });
    }
}
