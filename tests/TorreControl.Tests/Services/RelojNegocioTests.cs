using Microsoft.Extensions.Time.Testing;
using TorreControl.Web.Services;

namespace TorreControl.Tests.Services;

public class RelojNegocioTests
{
    [Fact]
    public void Ahora_ConvierteUtcAHoraDeCiudadDeMexico()
    {
        // 06:30 UTC = 00:30 en CDMX (UTC-6, sin horario de verano desde 2022).
        var reloj = new FakeTimeProvider(new DateTimeOffset(2026, 3, 10, 6, 30, 0, TimeSpan.Zero));

        var negocio = new RelojNegocio(reloj, Configuracion.Opciones());

        Assert.Equal(new DateTime(2026, 3, 10, 0, 30, 0), negocio.Ahora());
        Assert.Equal(new DateOnly(2026, 3, 10), negocio.Hoy());
    }

    [Fact]
    public void Hoy_CambiaConLaMedianocheDeMexicoNoConLaDeUtc()
    {
        // 03:00 UTC del día 11 todavía es el día 10 en CDMX (21:00).
        var reloj = new FakeTimeProvider(new DateTimeOffset(2026, 3, 11, 3, 0, 0, TimeSpan.Zero));

        var negocio = new RelojNegocio(reloj, Configuracion.Opciones());

        Assert.Equal(new DateOnly(2026, 3, 10), negocio.Hoy());
    }
}
