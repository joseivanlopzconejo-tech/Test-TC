using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TorreControl.Web.Models;

namespace TorreControl.Tests.Controllers;

/// <summary>"No operó hoy": ocultar servicios que no se activaron (con su propia base, porque cambia datos).</summary>
public class NoOperoTests(AppConBaseDatos app) : IClassFixture<AppConBaseDatos>
{
    private static readonly string Hoy = DateTime.UtcNow.ToString("yyyy-MM-dd");

    private async Task<HttpClient> ConSesion(string numero)
    {
        var navegador = app.Navegador();
        await AppConBaseDatos.IniciarSesionAsync(navegador, numero, app.Contrasena);
        return navegador;
    }

    private static async Task<JsonElement> GrupoNormal(HttpClient navegador, int turno)
    {
        var tabla = JsonDocument.Parse(await navegador.GetStringAsync($"/api/tabla?dia={Hoy}&turno={turno}")).RootElement;
        return tabla.GetProperty("grupos").EnumerateArray().First(g => !g.GetProperty("esMixto").GetBoolean());
    }

    [Fact]
    public async Task Marcar_OcultaYDescuentaDelContador_YSePuedeReactivar()
    {
        var navegador = await ConSesion(AppConBaseDatos.MonitoristaA);

        var marcar = await AppConBaseDatos.PostJsonAsync(navegador, "/api/servicios/no-opero", new { ids = new[] { app.ServicioPlantaA }, noOpero = true });

        Assert.Equal(HttpStatusCode.OK, marcar.StatusCode);
        var grupo = await GrupoNormal(navegador, 1);
        // Sigue llegando (para poder reactivarlo), pero marcado y sin contar.
        Assert.True(grupo.GetProperty("servicios")[0].GetProperty("noOpero").GetBoolean());
        Assert.Equal(0, grupo.GetProperty("conteos").GetProperty("1").GetInt32());

        await AppConBaseDatos.PostJsonAsync(navegador, "/api/servicios/no-opero", new { ids = new[] { app.ServicioPlantaA }, noOpero = false });

        Assert.Equal(1, (await GrupoNormal(navegador, 1)).GetProperty("conteos").GetProperty("1").GetInt32());
    }

    [Fact]
    public async Task Marcar_QuedaEnLaAuditoriaConQuienLoHizo()
    {
        var navegador = await ConSesion(AppConBaseDatos.MonitoristaA);

        await AppConBaseDatos.PostJsonAsync(navegador, "/api/servicios/no-opero", new { ids = new[] { app.ServicioSalidaA }, noOpero = true });

        await using var db = app.CrearContexto();
        var usuarioId = (await db.Users.SingleAsync(u => u.UserName == AppConBaseDatos.MonitoristaA)).Id;
        Assert.True(await db.Auditorias.AnyAsync(a =>
            a.Entidad == "Servicio" && a.EntidadId == app.ServicioSalidaA.ToString() &&
            a.Campo == nameof(Servicio.NoOpero) && a.ValorNuevo == "True" && a.UsuarioId == usuarioId));
    }

    [Fact]
    public async Task ConUnServicioDeOtraPlanta_NoSeTocaNinguno()
    {
        var navegador = await ConSesion(AppConBaseDatos.MonitoristaA);

        var respuesta = await AppConBaseDatos.PostJsonAsync(navegador, "/api/servicios/no-opero",
            new { ids = new[] { app.ServicioMixtoA, app.ServicioPlantaB }, noOpero = true });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        await using var db = app.CrearContexto();
        Assert.False((await db.Servicios.SingleAsync(s => s.Id == app.ServicioMixtoA)).NoOpero);
        Assert.False((await db.Servicios.SingleAsync(s => s.Id == app.ServicioPlantaB)).NoOpero);
    }

    [Fact]
    public async Task SinTokenAntiforgery_SeRechaza()
    {
        var navegador = await ConSesion(AppConBaseDatos.MonitoristaA);

        var respuesta = await AppConBaseDatos.PostJsonAsync(navegador, "/api/servicios/no-opero",
            new { ids = new[] { app.ServicioMixtoA }, noOpero = true }, conToken: false);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task ListaVacia_Responde400()
    {
        var navegador = await ConSesion(AppConBaseDatos.MonitoristaA);

        var respuesta = await AppConBaseDatos.PostJsonAsync(navegador, "/api/servicios/no-opero", new { ids = Array.Empty<int>(), noOpero = true });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }
}
