using System.Net;
using Microsoft.EntityFrameworkCore;
using TorreControl.Web.Models;

namespace TorreControl.Tests.Security;

public class LoginTests(AppConBaseDatos app) : IClassFixture<AppConBaseDatos>
{
    [Fact]
    public async Task SinSesion_CualquierPaginaMandaAlLoginConservandoLaVista()
    {
        var respuesta = await app.Navegador().GetAsync("/?planta=3&dia=2026-03-10");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        var destino = respuesta.Headers.Location!.ToString();
        Assert.Contains("/Cuenta/Login", destino);
        // La dirección de regreso conserva los parámetros de la vista.
        Assert.Contains(Uri.EscapeDataString("/?planta=3&dia=2026-03-10"), destino);
    }

    [Fact]
    public async Task LoginCorrecto_EntraYMuestraElNombre()
    {
        var navegador = app.Navegador();

        var respuesta = await AppConBaseDatos.IniciarSesionAsync(navegador, AppConBaseDatos.MonitoristaA, app.Contrasena);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        var inicio = await navegador.GetStringAsync("/");
        Assert.Contains($"Usuario {AppConBaseDatos.MonitoristaA}", inicio);
    }

    [Fact]
    public async Task LoginCorrecto_QuedaEnLaAuditoria()
    {
        await AppConBaseDatos.IniciarSesionAsync(app.Navegador(), AppConBaseDatos.Supervisor, app.Contrasena);

        await using var db = app.CrearContexto();
        Assert.True(await db.Auditorias.AnyAsync(a => a.Accion == AccionAuditoria.InicioSesion));
    }

    [Fact]
    public async Task CookieDeSesion_EsSeguraYDeNavegador()
    {
        var respuesta = await AppConBaseDatos.IniciarSesionAsync(app.Navegador(), AppConBaseDatos.MonitoristaB, app.Contrasena);

        var cookie = Assert.Single(respuesta.Headers.GetValues("Set-Cookie"), c => c.StartsWith("TorreControl.Sesion="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        // Sin "expires": muere al cerrar el navegador (equipos compartidos entre turnos).
        Assert.DoesNotContain("expires=", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NoRevelaSiElNumeroDeEmpleadoExiste()
    {
        var existeContrasenaMala = await AppConBaseDatos.IniciarSesionAsync(app.Navegador(), AppConBaseDatos.Supervisor, "Incorrecta123");
        var noExiste = await AppConBaseDatos.IniciarSesionAsync(app.Navegador(), "NO-EXISTE-999", "Incorrecta123");
        var inactivo = await AppConBaseDatos.IniciarSesionAsync(app.Navegador(), AppConBaseDatos.Inactivo, app.Contrasena);

        var mensajes = new[] { existeContrasenaMala, noExiste, inactivo }
            .Select(r => AppConBaseDatos.MensajeDeError(r.Content.ReadAsStringAsync().Result))
            .ToList();

        Assert.All(new[] { existeContrasenaMala, noExiste, inactivo }, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.NotEmpty(mensajes[0]);
        Assert.All(mensajes, m => Assert.Equal(mensajes[0], m));
    }

    [Fact]
    public async Task UsuarioDesactivado_NoEntraAunqueLaContrasenaSeaCorrecta()
    {
        var navegador = app.Navegador();

        var respuesta = await AppConBaseDatos.IniciarSesionAsync(navegador, AppConBaseDatos.Inactivo, app.Contrasena);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode); // se queda en el login
        Assert.Equal(HttpStatusCode.Redirect, (await navegador.GetAsync("/")).StatusCode);
    }

    [Fact]
    public async Task ReturnUrlExterno_NoSacaAlUsuarioDeLaApp()
    {
        var respuesta = await AppConBaseDatos.IniciarSesionAsync(
            app.Navegador(), AppConBaseDatos.MonitoristaA, app.Contrasena, returnUrl: "https://sitio-falso.ejemplo/login");

        Assert.Equal("/", respuesta.Headers.Location?.ToString());
    }

    [Fact]
    public async Task PostSinTokenAntiforgery_SeRechaza()
    {
        var respuesta = await app.Navegador().PostAsync("/Cuenta/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["NumeroEmpleado"] = AppConBaseDatos.MonitoristaA,
            ["Contrasena"] = app.Contrasena
        }));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task CerrarSesion_TerminaLaSesion()
    {
        var navegador = app.Navegador();
        await AppConBaseDatos.IniciarSesionAsync(navegador, AppConBaseDatos.MonitoristaB, app.Contrasena);
        var token = await AppConBaseDatos.TokenDeFormularioAsync(navegador, "/");

        await navegador.PostAsync("/Cuenta/Logout", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.Redirect, (await navegador.GetAsync("/")).StatusCode);
    }

    [Fact]
    public async Task ConSesion_LaPaginaTraeLaHoraDeVencimientoDeLaSesion()
    {
        var navegador = app.Navegador();
        await AppConBaseDatos.IniciarSesionAsync(navegador, AppConBaseDatos.MonitoristaA, app.Contrasena);

        var html = await navegador.GetStringAsync("/");

        var inicio = html.IndexOf("name=\"sesion-expira\" content=\"", StringComparison.Ordinal);
        Assert.True(inicio >= 0, "Falta la etiqueta meta sesion-expira");
        var valor = html[(inicio + 30)..html.IndexOf('"', inicio + 30)];
        var faltan = DateTimeOffset.Parse(valor) - DateTimeOffset.UtcNow;
        Assert.InRange(faltan, TimeSpan.FromHours(7.9), TimeSpan.FromHours(8));
    }
}
