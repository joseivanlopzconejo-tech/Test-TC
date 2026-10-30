using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using TorreControl.Web.Security;

namespace TorreControl.Tests.Security;

/// <summary>El modo proxy (Codespaces) solo se activa con su configuración.</summary>
public class ProxyHttpsTests
{
    private sealed class App(bool detrasDeProxy) : FabricaApp
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting(ProxyHttps.Clave, detrasDeProxy.ToString());
        }
    }

    private static HttpClient ClienteHttp(WebApplicationFactory<Program> app) => app.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("http://torre.ejemplo"),
        AllowAutoRedirect = false
    });

    [Fact]
    public async Task SinModoProxy_UnaPeticionHttpSeRedirigeAHttps()
    {
        await using var app = new App(detrasDeProxy: false);

        var respuesta = await ClienteHttp(app).GetAsync("/Home/Error");

        Assert.Equal(HttpStatusCode.TemporaryRedirect, respuesta.StatusCode);
    }

    [Fact]
    public async Task ConModoProxy_ElLoginRedirigeSinApuntarALocalhost()
    {
        await using var app = new App(detrasDeProxy: true);
        var peticion = new HttpRequestMessage(HttpMethod.Get, "/");
        peticion.Headers.Add("X-Forwarded-Host", "mi-codespace-5178.app.github.dev");

        var respuesta = await ClienteHttp(app).SendAsync(peticion);

        // Sin sesión manda al login con una dirección relativa: el navegador se queda en la
        // dirección pública del Codespace (nunca lo manda a localhost).
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.StartsWith("/Cuenta/Login", respuesta.Headers.Location!.ToString());
    }

    [Fact]
    public async Task ConModoProxy_LaCookieAntiforgerySeEmiteComoSegura()
    {
        await using var app = new App(detrasDeProxy: true);

        var respuesta = await ClienteHttp(app).GetAsync("/Home/Error");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains(respuesta.Headers.GetValues("Set-Cookie"), c => c.StartsWith("TorreControl.Antiforgery=") && c.Contains("secure"));
    }
}
