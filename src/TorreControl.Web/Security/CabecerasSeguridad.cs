namespace TorreControl.Web.Security;

/// <summary>
/// Agrega a CADA respuesta las cabeceras que le dicen al navegador qué puede y qué no
/// puede hacer con nuestra página. Es una defensa extra: si algún día se cuela un bug
/// de inyección, el navegador igual se niega a ejecutar código ajeno.
/// </summary>
public static class CabecerasSeguridad
{
    public static IApplicationBuilder UseCabecerasSeguridad(this IApplicationBuilder app, bool esDesarrollo)
    {
        // En desarrollo, "dotnet watch" recarga la página por un WebSocket en otro puerto;
        // solo ahí se permite. En producción, únicamente nuestro propio origen.
        var conexiones = esDesarrollo ? "'self' ws://localhost:* wss://localhost:*" : "'self'";

        // CSP (Content-Security-Policy): solo scripts, estilos e imágenes servidos por la
        // propia app. Por eso no usamos <script> ni <style> en línea ni CDNs.
        // connect-src 'self' cubre las peticiones fetch y el WebSocket de SignalR (Fase 2).
        var csp = string.Join("; ",
            "default-src 'self'",
            "script-src 'self'",
            "style-src 'self'",
            "img-src 'self' data:", // Bootstrap dibuja algunos íconos con imágenes data:
            "font-src 'self'",
            $"connect-src {conexiones}",
            "object-src 'none'",
            "base-uri 'self'",
            "form-action 'self'",
            "frame-ancestors 'none'");

        return app.Use(async (contexto, siguiente) =>
        {
            var cabeceras = contexto.Response.Headers;
            cabeceras["Content-Security-Policy"] = csp;
            // Que el navegador no "adivine" el tipo de archivo (evita ejecutar un .txt como script).
            cabeceras["X-Content-Type-Options"] = "nosniff";
            // Nadie puede meter la torre dentro de un <iframe> (evita clickjacking).
            // Es redundante con frame-ancestors, pero lo entienden navegadores viejos.
            cabeceras["X-Frame-Options"] = "DENY";
            // Al salir a otro sitio no se manda la URL (puede traer ids de servicios).
            cabeceras["Referrer-Policy"] = "same-origin";
            // La app no usa cámara, micrófono ni ubicación: se apagan.
            cabeceras["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            cabeceras["Cross-Origin-Opener-Policy"] = "same-origin";

            await siguiente();
        });
    }
}
