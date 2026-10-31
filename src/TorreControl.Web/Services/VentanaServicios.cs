using Microsoft.Extensions.Options;
using TorreControl.Web.Models;

namespace TorreControl.Web.Services;

/// <summary>
/// Decide qué servicios aparecen en la tabla para una ventana de horas (sección 5.3).
/// Clase pura: recibe fechas y horas y responde sí o no, sin tocar la base de datos.
/// </summary>
public class VentanaServicios(IOptions<ReglasNegocioOptions> opciones)
{
    private readonly ReglasNegocioOptions _o = opciones.Value;

    /// <summary>
    /// Ventana pedida por el usuario: [día + desde, día + hasta). Si "hasta" es menor o igual que
    /// "desde", la ventana termina al día siguiente (ej. 3er turno: 16:00 a 00:00).
    /// </summary>
    public static (DateTime Desde, DateTime Hasta) Ventana(DateOnly dia, TimeOnly desde, TimeOnly hasta)
    {
        var inicio = dia.ToDateTime(desde);
        var fin = dia.ToDateTime(hasta);
        if (hasta <= desde)
        {
            fin = fin.AddDays(1);
        }

        return (inicio, fin);
    }

    /// <summary>Turno completo del día elegido. Turnos fijos: 1º 00–08, 2º 08–16, 3º 16–24.</summary>
    /// <param name="turno">1, 2 o 3; 0 = el día completo.</param>
    public (DateTime Desde, DateTime Hasta) VentanaDeTurno(DateOnly dia, int turno)
    {
        var medianoche = dia.ToDateTime(TimeOnly.MinValue);
        return turno switch
        {
            1 => (medianoche, medianoche.AddHours(_o.HoraInicioTurno2)),
            2 => (medianoche.AddHours(_o.HoraInicioTurno2), medianoche.AddHours(_o.HoraInicioTurno3)),
            3 => (medianoche.AddHours(_o.HoraInicioTurno3), medianoche.AddDays(1)),
            0 => (medianoche, medianoche.AddDays(1)),
            _ => throw new ArgumentOutOfRangeException(nameof(turno), "El turno debe ser 0 (día), 1, 2 o 3.")
        };
    }

    /// <summary>
    /// Periodo en que un servicio "necesita atención" de la torre:
    /// - Empieza en lo que ocurra primero: cuando abre su Filtro 1 (solo tipo E) o PrecargaMin
    ///   antes de su salida (para verlo con tiempo y no tener que recargar).
    /// - Termina cuando vence el plazo para capturar su Htr (Htp + PlazoHirHtrMin). Así, un servicio
    ///   que llega a las 07:50 sigue visible para el 2º turno, que es quien captura su llegada.
    /// </summary>
    public (DateTime Desde, DateTime Hasta) PeriodoDeAtencion(DateOnly fecha, TimeOnly hip, TimeOnly htp, TipoServicio tipo)
    {
        var (salida, llegada) = ReglasTurno.Intervalo(fecha, hip, htp);

        var desde = salida.AddMinutes(-_o.PrecargaMin);
        if (tipo == TipoServicio.E)
        {
            var aperturaFiltro1 = salida.AddMinutes(
                -(CalculadoraFiltros.TotalFiltros * _o.FiltroIntervaloMin + _o.FiltroVentanaAperturaMin));
            desde = aperturaFiltro1 < desde ? aperturaFiltro1 : desde;
        }

        return (desde, llegada.AddMinutes(_o.PlazoHirHtrMin));
    }

    /// <summary>Se muestra si su periodo de atención se traslapa con la ventana pedida.</summary>
    public bool SeMuestra(DateOnly fecha, TimeOnly hip, TimeOnly htp, TipoServicio tipo, DateTime ventanaDesde, DateTime ventanaHasta)
    {
        var (desde, hasta) = PeriodoDeAtencion(fecha, hip, htp, tipo);
        return desde < ventanaHasta && hasta > ventanaDesde;
    }
}
