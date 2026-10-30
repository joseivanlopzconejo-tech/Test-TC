using Microsoft.Extensions.Options;

namespace TorreControl.Web.Services;

/// <summary>
/// Calcula las horas de los 4 filtros de revisión (sección 5.4). Solo aplica a servicios
/// de entrada (E). Nadie escribe estas horas a mano: salen de Hip.
/// </summary>
public class CalculadoraFiltros(IOptions<ReglasNegocioOptions> opciones)
{
    public const int TotalFiltros = 4;

    private readonly int _intervaloMin = opciones.Value.FiltroIntervaloMin;

    /// <summary>
    /// Hora programada (que también es la hora límite) de cada filtro, contando hacia atrás
    /// desde Hip: F4 = Hip-20, F3 = Hip-40, F2 = Hip-60, F1 = Hip-80. Puede caer en el día
    /// anterior si el servicio sale muy temprano, por eso se regresa fecha-hora completa.
    /// </summary>
    public IReadOnlyList<(int NumeroFiltro, DateTime HoraProgramada)> HorasProgramadas(DateOnly fecha, TimeOnly hip)
    {
        var salida = fecha.ToDateTime(hip);

        return Enumerable.Range(1, TotalFiltros)
            .Select(n => (n, salida.AddMinutes(-(TotalFiltros - n + 1) * _intervaloMin)))
            .ToList();
    }
}
