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

    public static void Configurar(CookieAuthenticationOptions opciones)
    {
        DateTimeOffset Ahora() => opciones.TimeProvider?.GetUtcNow() ?? DateTimeOffset.UtcNow;

        // 1) Revisión normal de expiración deslizante.
        opciones.Events.OnCheckSlidingExpiration = contexto =>
        {
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
            if (contexto.ShouldRenew)
            {
                contexto.HttpContext.Items[ClaveExpira] = Ahora().Add(opciones.ExpireTimeSpan);
            }
        };
    }
}
