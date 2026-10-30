using TorreControl.Web.Models;

namespace TorreControl.Web.Services;

/// <summary>
/// Textos estándar de la columna Observaciones (bitácora automática, sección 5.5). Se generan
/// solos al capturar Hir/Htr: nadie los escribe a mano, así todos quedan con la misma redacción
/// profesional y se pueden contar en reportes.
/// </summary>
public static class TextosIncidencia
{
    public const string InicioSinNovedad = "Inicio de ruta sin novedad, dentro del horario establecido.";
    public const string LlegadaSinNovedad = "Arribo a planta sin novedad, dentro del tiempo establecido.";

    public static string SinNovedad(MomentoIncidencia momento) =>
        momento == MomentoIncidencia.Inicio ? InicioSinNovedad : LlegadaSinNovedad;

    /// <summary>
    /// Texto con incidente. tarde = la hora real fue después de la programada; si no, fue antes
    /// (solo llega aquí un adelanto mayor a SospechosaMin que el monitorista confirmó).
    /// </summary>
    public static string ConIncidente(MomentoIncidencia momento, bool tarde, string tipo, string motivo)
    {
        var cuando = tarde ? "tardío" : "anticipado";
        var que = momento == MomentoIncidencia.Inicio ? $"Inicio {cuando}" : $"Arribo {cuando} a planta";
        var detalle = motivo.Trim().TrimEnd('.');
        return $"{que} por {tipo.ToLowerInvariant()}: {detalle}.";
    }
}
