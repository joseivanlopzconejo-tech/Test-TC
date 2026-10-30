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

    [Theory]
    [InlineData("07:05", "08:15", true)]  // cruza el cambio de 1er a 2º turno
    [InlineData("15:05", "16:15", true)]  // cruza de 2º a 3º
    [InlineData("23:05", "00:15", true)]  // cruza la medianoche (3º a 1º)
    [InlineData("05:30", "06:50", false)] // todo dentro del 1er turno
    [InlineData("16:00", "17:10", false)] // sale justo al iniciar el 3º: no cruza nada
    [InlineData("06:50", "08:00", false)] // llega justo en el cambio: no es mixto
    public void EsMixto_DetectaElCruceDeTurno(string hip, string htp, bool esperado)
    {
        Assert.Equal(esperado, _reglas.EsMixto(Dia, TimeOnly.Parse(hip), TimeOnly.Parse(htp)));
    }
}
