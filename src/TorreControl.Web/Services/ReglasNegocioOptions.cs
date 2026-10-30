using System.ComponentModel.DataAnnotations;

namespace TorreControl.Web.Services;

/// <summary>
/// Constantes de las reglas de negocio. Se leen de la sección "ReglasNegocio" de
/// appsettings.json; aquí NO hay valores por defecto a propósito, para que exista
/// una sola fuente de verdad. Si falta un valor, la app no arranca (ValidateOnStart)
/// en lugar de trabajar en silencio con un 0.
/// </summary>
public class ReglasNegocioOptions : IValidatableObject
{
    public const string Seccion = "ReglasNegocio";

    // Todas las reglas de tiempo se evalúan con la hora del servidor en esta zona,
    // nunca con la del navegador (sección 5.1).
    [Required]
    public string ZonaHoraria { get; set; } = "";

    // ---- Filtros (5.4) ----

    // Separación entre filtros, contada hacia atrás desde Hip (F4 = Hip-20, F3 = Hip-40...).
    [Range(1, 120)]
    public int FiltroIntervaloMin { get; set; }

    // PROVISIONAL: cuántos minutos antes de su hora límite se abre cada filtro.
    // Falta confirmarlo en la torre real; la hora límite sí es firme.
    [Range(1, 240)]
    public int FiltroVentanaAperturaMin { get; set; }

    // Últimos minutos antes de la hora límite en que el botón palpita con cuenta regresiva.
    [Range(1, 60)]
    public int FiltroAdvertenciaMin { get; set; }

    [Range(1, 10)]
    public int MaxModificacionesFiltro4 { get; set; }

    // ---- Hir / Htr (5.5) ----

    // Retraso de hasta este valor se guarda directo con "sin novedad".
    [Range(0, 120)]
    public int ToleranciaMin { get; set; }

    // Diferencia absoluta mayor a este valor dispara la ventana de "hora sospechosa".
    [Range(1, 240)]
    public int SospechosaMin { get; set; }

    // Plazo después de Hip (o Htp) para capturar Hir (o Htr); pasado esto se bloquea.
    [Range(1, 720)]
    public int PlazoHirHtrMin { get; set; }

    // 1ª captura = llenado, 2ª = corrección. Después se bloquea.
    [Range(1, 10)]
    public int MaxModificacionesHirHtr { get; set; }

    // ---- Unidades y aforo (5.6, 5.7) ----

    [Range(0, 10)]
    public int MaxApoyos { get; set; }

    [Range(1, 500)]
    public int AforoMax { get; set; }

    // ---- Tabla (5.3) ----

    // Servicios que arrancan dentro de estos minutos se muestran desde antes.
    [Range(0, 720)]
    public int PrecargaMin { get; set; }

    // ---- Turnos (5.2) ----
    // El 1er turno siempre empieza a las 00:00; estas son las horas en que empiezan el 2º y el 3º.
    // Quedan en configuración porque el Jefe de Monitoreo podrá editar horarios y turnos.

    [Range(1, 23)]
    public int HoraInicioTurno2 { get; set; }

    [Range(1, 23)]
    public int HoraInicioTurno3 { get; set; }

    /// <summary>
    /// Reglas que relacionan dos valores entre sí. Los [Range] de arriba solo revisan
    /// cada valor por separado; aquí se evita una configuración que se contradiga.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ToleranciaMin >= SospechosaMin)
        {
            // Si no, no existiría el tramo "pide motivo" (entre la tolerancia y la sospechosa).
            yield return new ValidationResult(
                "ToleranciaMin debe ser menor que SospechosaMin.",
                [nameof(ToleranciaMin), nameof(SospechosaMin)]);
        }

        if (FiltroAdvertenciaMin > FiltroVentanaAperturaMin)
        {
            // El aviso ocurre dentro de la ventana de captura; no puede ser más largo que ella.
            yield return new ValidationResult(
                "FiltroAdvertenciaMin no puede ser mayor que FiltroVentanaAperturaMin.",
                [nameof(FiltroAdvertenciaMin), nameof(FiltroVentanaAperturaMin)]);
        }

        if (HoraInicioTurno2 >= HoraInicioTurno3)
        {
            yield return new ValidationResult(
                "HoraInicioTurno2 debe ser menor que HoraInicioTurno3.",
                [nameof(HoraInicioTurno2), nameof(HoraInicioTurno3)]);
        }

        if (!string.IsNullOrWhiteSpace(ZonaHoraria) && !TimeZoneInfo.TryFindSystemTimeZoneById(ZonaHoraria, out _))
        {
            yield return new ValidationResult(
                $"La zona horaria '{ZonaHoraria}' no existe en este servidor.",
                [nameof(ZonaHoraria)]);
        }
    }
}
