using Microsoft.AspNetCore.HttpOverrides;

namespace TorreControl.Web.Security;

/// <summary>
/// Modo "detrás de un proxy HTTPS" (por ejemplo, GitHub Codespaces). El navegador habla HTTPS
/// con el proxy, y el proxy le pasa la petición a la app por HTTP dentro de la misma máquina.
/// Sin esto la app creería que la conexión no es segura: no podría poner cookies Secure ni
/// validar el token CSRF.
///
/// Se activa SOLO con la configuración "Proxy:DetrasDeProxyHttps" = true (la pone el
/// devcontainer). Nunca debe activarse si la app recibe conexiones directas de internet, porque
/// confía en las cabeceras X-Forwarded-* que manda el proxy.
/// </summary>
public static class ProxyHttps
{
    public const string Clave = "Proxy:DetrasDeProxyHttps";

    public static IApplicationBuilder UseProxyHttps(this IApplicationBuilder app, IConfiguration config)
    {
        if (!config.GetValue<bool>(Clave))
        {
            return app;
        }

        var opciones = new ForwardedHeadersOptions
        {
            // IP real del usuario (para el límite de intentos de login) y dirección pública.
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedHost
        };
        // El proxy del Codespace no tiene una IP fija conocida; solo él puede llegar a este puerto.
        opciones.KnownIPNetworks.Clear();
        opciones.KnownProxies.Clear();
        app.UseForwardedHeaders(opciones);

        // Hacia el navegador la conexión SIEMPRE es HTTPS (lo garantiza el proxy).
        return app.Use((contexto, siguiente) =>
        {
            contexto.Request.Scheme = "https";
            return siguiente(contexto);
        });
    }
}
