using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TorreControl.Tests.Security;

public class CabecerasSeguridadTests(FabricaApp fabrica) : IClassFixture<FabricaApp>
{
    // "localhost" está excluido de HSTS a propósito (para no romper otros proyectos locales),
    // por eso las pruebas usan un dominio de ejemplo.
    private HttpClient Cliente(string esquema = "https") => fabrica.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri($"{esquema}://torre.ejemplo"),
        AllowAutoRedirect = false
    });

    [Fact]
    public async Task Inicio_EnviaCabecerasDeSeguridad()
    {
        var respuesta = await Cliente().GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var csp = Assert.Single(respuesta.Headers.GetValues("Content-Security-Policy"));
        Assert.Contains("default-src 'self'", csp);
        Assert.Contains("script-src 'self'", csp);
        Assert.Contains("frame-ancestors 'none'", csp);
        Assert.DoesNotContain("unsafe-inline", csp);
        Assert.Equal("nosniff", Assert.Single(respuesta.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("DENY", Assert.Single(respuesta.Headers.GetValues("X-Frame-Options")));
        Assert.Equal("same-origin", Assert.Single(respuesta.Headers.GetValues("Referrer-Policy")));
    }

    [Fact]
    public async Task Produccion_NoPermiteWebSocketsDeDesarrolloEnCsp()
    {
        var respuesta = await Cliente().GetAsync("/");

        var csp = Assert.Single(respuesta.Headers.GetValues("Content-Security-Policy"));
        Assert.DoesNotContain("ws://", csp);
    }

    [Fact]
    public async Task Https_EnviaHsts()
    {
        var respuesta = await Cliente().GetAsync("/");

        var hsts = Assert.Single(respuesta.Headers.GetValues("Strict-Transport-Security"));
        Assert.Contains("max-age=31536000", hsts); // 365 días
    }

    [Fact]
    public async Task Http_RedirigeAHttps()
    {
        var respuesta = await Cliente("http").GetAsync("/");

        Assert.Equal(HttpStatusCode.TemporaryRedirect, respuesta.StatusCode);
        Assert.Equal("https", respuesta.Headers.Location?.Scheme);
    }

    [Fact]
    public async Task CookieAntiforgery_EsHttpOnlySecureYSameSiteStrict()
    {
        var respuesta = await Cliente().GetAsync("/");

        var cookie = Assert.Single(respuesta.Headers.GetValues("Set-Cookie"), c => c.StartsWith("TorreControl.Antiforgery="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Respuesta_NoRevelaElServidor()
    {
        var respuesta = await Cliente().GetAsync("/");

        Assert.False(respuesta.Headers.Contains("Server"));
    }
}
