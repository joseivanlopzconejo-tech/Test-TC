using Microsoft.Extensions.Options;

namespace TorreControl.Web.Services;

/// <summary>
/// La hora "oficial" del negocio: la del servidor convertida a la zona de Ciudad de México.
/// Toda regla de tiempo (bloqueos, plazos, turnos) pregunta la hora aquí, nunca al navegador
/// ni con DateTime.Now, para que todos vean exactamente la misma hora.
/// </summary>
public class RelojNegocio
{
    private readonly TimeProvider _reloj;
    private readonly TimeZoneInfo _zona;

    public RelojNegocio(TimeProvider reloj, IOptions<ReglasNegocioOptions> opciones)
    {
        _reloj = reloj;
        _zona = TimeZoneInfo.FindSystemTimeZoneById(opciones.Value.ZonaHoraria);
    }

    /// <summary>Fecha y hora actuales en CDMX (sin zona: así se guardan Hir, Htr y capturas).</summary>
    public DateTime Ahora() =>
        DateTime.SpecifyKind(TimeZoneInfo.ConvertTime(_reloj.GetUtcNow(), _zona).DateTime, DateTimeKind.Unspecified);

    public DateOnly Hoy() => DateOnly.FromDateTime(Ahora());

    /// <summary>Hora UTC: solo para la Auditoría, donde no debe haber ambigüedad.</summary>
    public DateTime AhoraUtc() => _reloj.GetUtcNow().UtcDateTime;
}
