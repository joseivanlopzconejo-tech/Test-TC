using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using TorreControl.Web.Security;

namespace TorreControl.Tests.Security;

/// <summary>Las actualizaciones automáticas de la tabla no deben mantener viva la sesión para siempre.</summary>
public class SesionAutomaticaTests(SesionAutomaticaTests.AppConRelojFalso app) : IClassFixture<SesionAutomaticaTests.AppConRelojFalso>
{
    public class AppConRelojFalso : AppConBaseDatos
    {
        public FakeTimeProvider Reloj { get; } = new(DateTimeOffset.UtcNow);

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(s => s.AddSingleton<TimeProvider>(Reloj));
        }
    }

    private static bool RenovoLaSesion(HttpResponseMessage r) =>
        r.Headers.TryGetValues("Set-Cookie", out var cookies) && cookies.Any(c => c.StartsWith("TorreControl.Sesion="));

    [Fact]
    public async Task ActualizacionAutomatica_NoRenuevaLaSesion_PeroUnClicSi()
    {
        var navegador = app.Navegador();
        await AppConBaseDatos.IniciarSesionAsync(navegador, AppConBaseDatos.MonitoristaA, app.Contrasena);

        // Pasan 5 horas: más de la mitad de las 8, así que cualquier actividad renovaría la cookie.
        app.Reloj.Advance(TimeSpan.FromHours(5));

        var automatica = new HttpRequestMessage(HttpMethod.Get, $"/api/servicios/{app.ServicioPlantaA}");
        automatica.Headers.Add(SesionDeslizante.CabeceraAutomatica, "1");
        var respuestaAutomatica = await navegador.SendAsync(automatica);
        var respuestaManual = await navegador.GetAsync($"/api/servicios/{app.ServicioPlantaA}");

        Assert.True(respuestaAutomatica.IsSuccessStatusCode);
        Assert.False(RenovoLaSesion(respuestaAutomatica));
        Assert.True(RenovoLaSesion(respuestaManual));
    }
}
