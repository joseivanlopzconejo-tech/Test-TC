using TorreControl.Web.Models;
using TorreControl.Web.Services;

namespace TorreControl.Tests.Services;

public class VentanaServiciosTests
{
    private readonly VentanaServicios _ventana = new(Configuracion.Opciones());
    private static readonly DateOnly Dia = new(2026, 3, 10);
    private static readonly DateOnly Ayer = Dia.AddDays(-1);
    private static readonly DateOnly Manana = Dia.AddDays(1);

    // Ventanas de los tres turnos del día 10.
    private static readonly (DateTime, DateTime) Turno1 = VentanaServicios.Ventana(Dia, new(0, 0), new(8, 0));
    private static readonly (DateTime, DateTime) Turno2 = VentanaServicios.Ventana(Dia, new(8, 0), new(16, 0));
    private static readonly (DateTime, DateTime) Turno3 = VentanaServicios.Ventana(Dia, new(16, 0), new(0, 0));

    private bool SeMuestra(DateOnly fecha, string hip, string htp, TipoServicio tipo, (DateTime Desde, DateTime Hasta) v) =>
        _ventana.SeMuestra(fecha, TimeOnly.Parse(hip), TimeOnly.Parse(htp), tipo, v.Desde, v.Hasta);

    [Fact]
    public void Ventana_QueTerminaAMedianoche_TerminaAlDiaSiguiente()
    {
        Assert.Equal((new DateTime(2026, 3, 10, 16, 0, 0), new DateTime(2026, 3, 11, 0, 0, 0)), Turno3);
    }

    [Theory]
    [InlineData(3, 0, 0, 8)]
    [InlineData(8, 0, 8, 16)]
    [InlineData(15, 59, 8, 16)]
    [InlineData(23, 30, 16, 0)]
    public void VentanaPorDefecto_EsElTurnoEnCurso(int hora, int minuto, int desde, int hasta)
    {
        var (dia, d, h) = _ventana.VentanaDelTurnoActual(Dia.ToDateTime(new TimeOnly(hora, minuto)));

        Assert.Equal(Dia, dia);
        Assert.Equal(new TimeOnly(desde, 0), d);
        Assert.Equal(new TimeOnly(hasta, 0), h);
    }

    [Fact]
    public void ServicioMixto_SeVeEnAmbosTurnos()
    {
        Assert.True(SeMuestra(Dia, "07:05", "08:15", TipoServicio.S, Turno1));
        Assert.True(SeMuestra(Dia, "07:05", "08:15", TipoServicio.S, Turno2));
    }

    [Fact]
    public void LlegadaPocoAntesDelCambio_SigueVisibleParaElTurnoQueCapturaLaHtr()
    {
        // Llega 07:15; su Htr se puede capturar hasta 08:15, ya en el 2º turno.
        Assert.True(SeMuestra(Dia, "06:05", "07:15", TipoServicio.S, Turno2));
    }

    [Fact]
    public void ServicioYaCerrado_NoSeMuestra()
    {
        // Llega 06:50; su plazo de Htr vence 07:50, antes de que empiece el 2º turno.
        Assert.False(SeMuestra(Dia, "05:30", "06:50", TipoServicio.E, Turno2));
    }

    [Fact]
    public void ServicioQueSaleProntoDespuesDeLaVentana_SePrecarga()
    {
        Assert.True(SeMuestra(Dia, "17:30", "18:50", TipoServicio.S, Turno2));  // sale 1.5 h después
        Assert.False(SeMuestra(Dia, "18:30", "19:50", TipoServicio.S, Turno2)); // sale 2.5 h después
    }

    [Fact]
    public void ServicioDeEntrada_SeVeDesdeQueAbreSuFiltro1()
    {
        // Hip 17:40: el F1 abre a las 16:00 (Hip - 80 - 20) y la precarga a las 15:40.
        var (desde, _) = _ventana.PeriodoDeAtencion(Dia, new(17, 40), new(19, 0), TipoServicio.E);

        Assert.Equal(new DateTime(2026, 3, 10, 15, 40, 0), desde);
    }

    [Fact]
    public void ServicioDeAyerQueCruzaMedianoche_SeVeEnElPrimerTurnoDeHoy()
    {
        Assert.True(SeMuestra(Ayer, "23:05", "00:15", TipoServicio.S, Turno1));
        Assert.False(SeMuestra(Ayer, "23:05", "00:15", TipoServicio.S, Turno2));
    }

    [Fact]
    public void ServicioDeMananaDeMadrugada_SeVeAlFinalDelTercerTurno()
    {
        Assert.True(SeMuestra(Manana, "00:30", "01:40", TipoServicio.E, Turno3));
    }
}
