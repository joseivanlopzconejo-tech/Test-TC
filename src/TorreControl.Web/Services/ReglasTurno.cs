using Microsoft.Extensions.Options;

namespace TorreControl.Web.Services;

/// <summary>
/// Reglas de turnos (sección 5.2). Clase pura: no toca la base de datos, solo hace cuentas,
/// por eso se puede probar con xUnit sin levantar nada.
/// </summary>
public class ReglasTurno(IOptions<ReglasNegocioOptions> opciones)
{
    private readonly ReglasNegocioOptions _o = opciones.Value;

    /// <summary>
    /// Turno al que pertenece una hora: 1er turno desde las 00:00, el 2º desde HoraInicioTurno2
    /// y el 3º desde HoraInicioTurno3. El turno de un servicio se asigna por su Hip.
    /// </summary>
    public int TurnoDe(TimeOnly hora)
    {
        if (hora.Hour < _o.HoraInicioTurno2)
        {
            return 1;
        }

        return hora.Hour < _o.HoraInicioTurno3 ? 2 : 3;
    }

    /// <summary>
    /// Convierte las horas programadas en fecha-hora completas. Si Htp es menor que Hip,
    /// el servicio cruza la medianoche y llega al día siguiente (ej. sale 23:05, llega 00:15).
    /// </summary>
    public static (DateTime Inicio, DateTime Fin) Intervalo(DateOnly fecha, TimeOnly hip, TimeOnly htp)
    {
        var inicio = fecha.ToDateTime(hip);
        var fin = fecha.ToDateTime(htp);
        if (htp < hip)
        {
            fin = fin.AddDays(1);
        }

        return (inicio, fin);
    }
}
