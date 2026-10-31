using TorreControl.Web.Services;

namespace TorreControl.Tests.Services;

public class ReglasTurnoTests
{
    private readonly ReglasTurno _reglas = new(Configuracion.Opciones());
    private static readonly DateOnly Dia = new(2026, 3, 10);

    [Theory]
    [InlineData("00:00", 1)]
    [InlineData("07:59", 1)]
    [InlineData("08:00", 2)]
    [InlineData("15:59", 2)]
    [InlineData("16:00", 3)]
    [InlineData("23:59", 3)]
    public void TurnoDe_RespetaLasFronteras(string hora, int turnoEsperado)
    {
        Assert.Equal(turnoEsperado, _reglas.TurnoDe(TimeOnly.Parse(hora)));
    }

    [Fact]
    public void Intervalo_QueCruzaMedianoche_LlegaAlDiaSiguiente()
    {
        var (inicio, fin) = ReglasTurno.Intervalo(Dia, new TimeOnly(23, 0), new TimeOnly(0, 30));

        Assert.Equal(new DateTime(2026, 3, 10, 23, 0, 0), inicio);
        Assert.Equal(new DateTime(2026, 3, 11, 0, 30, 0), fin);
    }

    [Fact]
    public void Intervalo_Normal_QuedaElMismoDia()
    {
        var (_, fin) = ReglasTurno.Intervalo(Dia, new TimeOnly(5, 30), new TimeOnly(6, 50));

        Assert.Equal(new DateTime(2026, 3, 10, 6, 50, 0), fin);
    }
}
