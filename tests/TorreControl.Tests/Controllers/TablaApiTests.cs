using System.Net;
using System.Text.Json;

namespace TorreControl.Tests.Controllers;

public class TablaApiTests(AppConBaseDatos app) : IClassFixture<AppConBaseDatos>
{
    // Los servicios de prueba son de hoy (UTC) de 05:30 a 06:50: se piden explícitamente con su día.
    private static readonly string Hoy = DateTime.UtcNow.ToString("yyyy-MM-dd");

    private async Task<HttpClient> ConSesion(string numero)
    {
        var navegador = app.Navegador();
        await AppConBaseDatos.IniciarSesionAsync(navegador, numero, app.Contrasena);
        return navegador;
    }

    private static async Task<JsonElement> Json(HttpResponseMessage r) =>
        JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;

    private static List<string> Plantas(JsonElement tabla) =>
        tabla.GetProperty("plantas").EnumerateArray().Select(p => p.GetProperty("nombre").GetString()!).ToList();

    [Fact]
    public async Task Monitorista_SoloRecibeLasPlantasDeSuModulo()
    {
        var navegador = await ConSesion(AppConBaseDatos.MonitoristaA);

        var tabla = await Json(await navegador.GetAsync($"/api/tabla?dia={Hoy}&desde=00:00&hasta=08:00"));

        Assert.Equal(["Planta A"], Plantas(tabla));
        var servicio = Assert.Single(tabla.GetProperty("plantas")[0].GetProperty("servicios").EnumerateArray());
        Assert.Equal("Centro", servicio.GetProperty("ruta").GetString());
        Assert.Equal("05:30", servicio.GetProperty("hip").GetString());
    }

    [Fact]
    public async Task Supervisor_RecibeTodasLasPlantas()
    {
        var navegador = await ConSesion(AppConBaseDatos.Supervisor);

        var tabla = await Json(await navegador.GetAsync($"/api/tabla?dia={Hoy}&desde=00:00&hasta=08:00"));

        Assert.Equal(["Planta A", "Planta B"], Plantas(tabla));
    }

    [Fact]
    public async Task VentanaSinServicios_DevuelveLaPlantaVacia()
    {
        var navegador = await ConSesion(AppConBaseDatos.MonitoristaA);

        var tabla = await Json(await navegador.GetAsync($"/api/tabla?dia={Hoy}&desde=16:00&hasta=00:00"));

        // La pestaña sigue existiendo (con contador 0) aunque no tenga servicios en esa ventana.
        Assert.Empty(tabla.GetProperty("plantas")[0].GetProperty("servicios").EnumerateArray());
        Assert.Equal(3, tabla.GetProperty("ventana").GetProperty("turno").GetInt32());
    }

    [Theory]
    [InlineData("dia=10-03-2026")]
    [InlineData("desde=8:00pm")]
    [InlineData("hasta=25:00")]
    public async Task ParametrosInvalidos_Responden400(string consulta)
    {
        var navegador = await ConSesion(AppConBaseDatos.MonitoristaA);

        Assert.Equal(HttpStatusCode.BadRequest, (await navegador.GetAsync($"/api/tabla?{consulta}")).StatusCode);
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
