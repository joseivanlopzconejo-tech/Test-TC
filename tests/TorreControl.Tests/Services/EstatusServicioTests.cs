using TorreControl.Web.Models;
using TorreControl.Web.Services;

namespace TorreControl.Tests.Services;

public class EstatusServicioTests
{
    private static readonly DateTime Hora = new(2026, 3, 10, 6, 0, 0);
    private static readonly EstatusUnidad Apagada = new() { Nombre = "Apagada", Color = "#F4A6A6" };
    private static readonly EstatusUnidad Trayecto = new() { Nombre = "Trayecto", Color = "#F6D88A" };
    private static readonly Dictionary<int, EstatusUnidad> SinFiltros = [];

    [Fact]
    public void Entrada_SinNadaCapturado_EsSinAccion()
    {
        Assert.Null(EstatusServicio.Calcular(TipoServicio.E, null, null, SinFiltros));
    }

    [Fact]
    public void Entrada_AntesDeSalir_TomaElUltimoFiltroCapturado()
    {
        var filtros = new Dictionary<int, EstatusUnidad> { [1] = Apagada, [3] = Trayecto };

        var estatus = EstatusServicio.Calcular(TipoServicio.E, null, null, filtros);

        Assert.Equal("Trayecto", estatus?.Nombre);
        Assert.Equal("#F6D88A", estatus?.Color);
    }

    [Fact]
    public void Entrada_ConHoraDeInicio_EsEnRuta_AunqueHayaFiltros()
    {
        var filtros = new Dictionary<int, EstatusUnidad> { [4] = Trayecto };

        Assert.Equal(EstatusServicio.EnRuta, EstatusServicio.Calcular(TipoServicio.E, Hora, null, filtros));
    }

    [Fact]
    public void Entrada_ConHoraDeLlegada_EsEnPlanta()
    {
        Assert.Equal(EstatusServicio.EnPlanta, EstatusServicio.Calcular(TipoServicio.E, Hora, Hora.AddHours(1), SinFiltros));
    }

    [Fact]
    public void Salida_SinHoras_EsSinAccion()
    {
        Assert.Null(EstatusServicio.Calcular(TipoServicio.S, null, null, SinFiltros));
    }

    [Fact]
    public void Salida_ConHoraDeInicio_EsEnSalida()
    {
        Assert.Equal(EstatusServicio.EnSalida, EstatusServicio.Calcular(TipoServicio.S, Hora, null, SinFiltros));
    }

    [Fact]
    public void Salida_ConHoraDeLlegada_EsResguardo()
    {
        Assert.Equal(EstatusServicio.Resguardo, EstatusServicio.Calcular(TipoServicio.S, Hora, Hora.AddHours(1), SinFiltros));
    }
}
