using TorreControl.Web.Services;

namespace TorreControl.Tests.Services;

public class CalculadoraFiltrosTests
{
    private readonly CalculadoraFiltros _calculadora = new(Configuracion.Opciones());

    [Fact]
    public void HorasProgramadas_CuentaHaciaAtrasDesdeHip()
    {
        var horas = _calculadora.HorasProgramadas(new DateOnly(2026, 3, 10), new TimeOnly(6, 0));

        Assert.Equal(
            [
                (1, new DateTime(2026, 3, 10, 4, 40, 0)), // Hip - 80
                (2, new DateTime(2026, 3, 10, 5, 0, 0)),  // Hip - 60
                (3, new DateTime(2026, 3, 10, 5, 20, 0)), // Hip - 40
                (4, new DateTime(2026, 3, 10, 5, 40, 0))  // Hip - 20
            ],
            horas);
    }

    [Fact]
    public void HorasProgramadas_PuedenCaerEnElDiaAnterior()
    {
        var horas = _calculadora.HorasProgramadas(new DateOnly(2026, 3, 10), new TimeOnly(0, 30));

        Assert.Equal(new DateTime(2026, 3, 9, 23, 10, 0), horas[0].HoraProgramada); // F1 = 00:30 - 80 min
        Assert.Equal(new DateTime(2026, 3, 10, 0, 10, 0), horas[3].HoraProgramada); // F4 = 00:30 - 20 min
    }
}
