using Microsoft.AspNetCore.Authentication.Cookies;

namespace TorreControl.Web.Security;

/// <summary>
/// Calcula a qué hora vence la sesión en cada petición, para que la página pueda avisar antes
/// de que expire. Con expiración deslizante la cookie se renueva sola cuando ya pasó más de la
/// mitad del tiempo, así que la hora real de vencimiento cambia y hay que recalcularla.
/// </summary>
public static class SesionDeslizante
{
    public const string ClaveExpira = "tc:sesion-expira-utc";

    /// <summary>
    /// Cabecera que manda la tabla en sus actualizaciones automáticas (cada 30 s). Esas peticiones
    /// NO cuentan como actividad: si nadie está frente a la pantalla, la sesión debe vencer igual.
    /// Solo los clics y capturas reales renuevan la sesión.
    /// </summary>
    public const string CabeceraAutomatica = "X-Actualizacion-Automatica";

    private static bool EsAutomatica(HttpContext contexto) =>
        contexto.Request.Headers.ContainsKey(CabeceraAutomatica);

    public static void Configurar(CookieAuthenticationOptions opciones)
    {
        DateTimeOffset Ahora() => opciones.TimeProvider?.GetUtcNow() ?? DateTimeOffset.UtcNow;

        // 1) Revisión normal de expiración deslizante.
        opciones.Events.OnCheckSlidingExpiration = contexto =>
        {
            if (EsAutomatica(contexto.HttpContext))
            {
                contexto.ShouldRenew = false;
            }

            var ahora = Ahora();
            contexto.HttpContext.Items[ClaveExpira] = contexto.ShouldRenew
                ? ahora.Add(opciones.ExpireTimeSpan)
                : ahora.Add(contexto.RemainingTime);
            return Task.CompletedTask;
        };

        // 2) Identity revalida la sesión cada cierto tiempo y, si sale bien, también la renueva.
        //    Se "envuelve" su validación original para no perderla y solo anotar la nueva hora.
        var validacionIdentity = opciones.Events.OnValidatePrincipal;
        opciones.Events.OnValidatePrincipal = async contexto =>
        {
            await validacionIdentity(contexto);
            if (EsAutomatica(contexto.HttpContext))
            {
                // Se valida igual (usuario activo, etc.), pero sin alargar la sesión.
                contexto.ShouldRenew = false;
            }
            else if (contexto.ShouldRenew)
            {
                contexto.HttpContext.Items[ClaveExpira] = Ahora().Add(opciones.ExpireTimeSpan);
            }
        };
    }
}
