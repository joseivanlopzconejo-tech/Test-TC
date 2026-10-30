using System.Net;

namespace TorreControl.Tests.Security;

/// <summary>De punta a punta: cambiar el id en la URL no da acceso a servicios de otra planta.</summary>
public class IdorTests(AppConBaseDatos app) : IClassFixture<AppConBaseDatos>
{
    private async Task<HttpClient> ConSesion(string numero)
    {
        var navegador = app.Navegador();
        await AppConBaseDatos.IniciarSesionAsync(navegador, numero, app.Contrasena);
        return navegador;
    }

    [Fact]
    public async Task Monitorista_VeElServicioDeSuPlanta()
    {
        var navegador = await ConSesion(AppConBaseDatos.MonitoristaA);

        var respuesta = await navegador.GetAsync($"/api/servicios/{app.ServicioPlantaA}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("Planta A", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Monitorista_NoVeElServicioDeOtraPlantaCambiandoElId()
    {
        var navegador = await ConSesion(AppConBaseDatos.MonitoristaA);

        var respuesta = await navegador.GetAsync($"/api/servicios/{app.ServicioPlantaB}");

        // Igual que si no existiera: no confirma que el id es válido.
        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task Supervisor_VeServiciosDeCualquierPlanta()
    {
        var navegador = await ConSesion(AppConBaseDatos.Supervisor);

        Assert.Equal(HttpStatusCode.OK, (await navegador.GetAsync($"/api/servicios/{app.ServicioPlantaB}")).StatusCode);
    }

    [Fact]
    public async Task SinSesion_LaApiResponde401YNoElLogin()
    {
        var respuesta = await app.Navegador().GetAsync($"/api/servicios/{app.ServicioPlantaA}");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }
}
