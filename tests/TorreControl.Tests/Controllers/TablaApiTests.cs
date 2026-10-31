using System.Net;
using System.Text.Json;

namespace TorreControl.Tests.Controllers;

public class TablaApiTests(AppConBaseDatos app) : IClassFixture<AppConBaseDatos>
{
    // Los servicios de prueba son de hoy (UTC): entrada 05:30 (1er turno), salida 15:05 y un
    // mixto 14:30 (2º turno). Se piden explícitamente con su día.
    private static readonly string Hoy = DateTime.UtcNow.ToString("yyyy-MM-dd");

    private async Task<HttpClient> ConSesion(string numero)
    {
        var navegador = app.Navegador();
        await AppConBaseDatos.IniciarSesionAsync(navegador, numero, app.Contrasena);
        return navegador;
    }

    private static async Task<JsonElement> Json(HttpResponseMessage r) =>
        JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;

    private static List<string> Pestanas(JsonElement tabla) =>
        tabla.GetProperty("grupos").EnumerateArray().Select(p => p.GetProperty("nombreCorto").GetString()!).ToList();

    [Fact]
    public async Task Monitorista_SoloRecibeLasPlantasDeSuModulo_ConSuPestanaMixto()
    {
        var navegador = await ConSesion(AppConBaseDatos.MonitoristaA);

        var tabla = await Json(await navegador.GetAsync($"/api/tabla?dia={Hoy}&turno=1"));

        // Planta A y su pestaña de mixtos; nada de la planta B. Sin nombre corto se usa el nombre.
        Assert.Equal(2, Pestanas(tabla).Count);
        Assert.All(tabla.GetProperty("grupos").EnumerateArray(), g => Assert.StartsWith("Cliente de prueba · Planta A", g.GetProperty("nombreCompleto").GetString()));
        var servicio = Assert.Single(tabla.GetProperty("grupos")[0].GetProperty("servicios").EnumerateArray());
        Assert.Equal("05:30", servicio.GetProperty("hip").GetString());
    }

    [Fact]
    public async Task Supervisor_RecibeTodasLasPlantas()
    {
        var navegador = await ConSesion(AppConBaseDatos.Supervisor);

        var tabla = await Json(await navegador.GetAsync($"/api/tabla?dia={Hoy}&turno=1"));

        Assert.Equal(["PLANTA A", "PLANTA A", "Planta B"], Pestanas(tabla));
    }

    [Fact]
    public async Task LosMixtosVanSoloEnSuPestana_YCadaPestanaCuentaPorTurno()
    {
        var navegador = await ConSesion(AppConBaseDatos.MonitoristaA);

        var tabla = await Json(await navegador.GetAsync($"/api/tabla?dia={Hoy}&turno=2"));
        var grupos = tabla.GetProperty("grupos").EnumerateArray().ToList();
        var normal = grupos.Single(g => !g.GetProperty("esMixto").GetBoolean());
        var mixto = grupos.Single(g => g.GetProperty("esMixto").GetBoolean());

        // 2º turno: en la pestaña normal solo la salida 15:05; el mixto, solo en su pestaña.
        Assert.Equal("S", Assert.Single(normal.GetProperty("servicios").EnumerateArray()).GetProperty("tipo").GetString());
        Assert.Equal("Doce por doce", Assert.Single(mixto.GetProperty("servicios").EnumerateArray()).GetProperty("ruta").GetString());
        Assert.EndsWith("-M", mixto.GetProperty("clave").GetString());

        // Contadores por turno ("0" = día completo). La salida 15:05 llega 16:15: el 3er turno aún
        // la ve para capturar su llegada.
        Assert.Equal(Conteos(1, 1, 1, 2), Conteos(normal));
        Assert.Equal(Conteos(0, 1, 1, 1), Conteos(mixto));
    }

    private static Dictionary<string, int> Conteos(int t1, int t2, int t3, int dia) =>
        new() { ["1"] = t1, ["2"] = t2, ["3"] = t3, ["0"] = dia };

    private static Dictionary<string, int> Conteos(JsonElement grupo) =>
        grupo.GetProperty("conteos").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetInt32());

    [Fact]
    public async Task TurnoInvalidoODiaInvalido_Responden400()
    {
        var navegador = await ConSesion(AppConBaseDatos.MonitoristaA);

        foreach (var consulta in new[] { "dia=10-03-2026", "turno=4", "turno=-1", "turno=dos" })
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await navegador.GetAsync($"/api/tabla?{consulta}")).StatusCode);
        }
    }

    [Fact]
    public async Task Respuesta_NoSeGuardaEnCache()
    {
        var navegador = await ConSesion(AppConBaseDatos.MonitoristaA);

        var respuesta = await navegador.GetAsync("/api/tabla");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(respuesta.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task SinSesion_Responde401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await app.Navegador().GetAsync("/api/tabla")).StatusCode);
    }
}
