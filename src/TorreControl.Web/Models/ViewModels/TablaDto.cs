namespace TorreControl.Web.Models.ViewModels;

// Lo que recibe el JavaScript de la tabla. Son "fotos" de solo lectura (records): se arman en el
// servidor con las horas y estatus ya calculados, para que el navegador solo muestre y nunca decida.
// Todas las horas van en 24 h ("HH:mm") y en hora de CDMX.

public record TablaRespuesta(
    DateTime AhoraUtc,
    int TurnoActual,
    VentanaDto Ventana,
    ReglasVistaDto Reglas,
    List<GrupoTablaDto> Grupos);

/// <param name="Turno">1, 2, 3, o 0 = día completo.</param>
public record VentanaDto(string Dia, int Turno, string Desde, string Hasta);

// Solo para colorear las diferencias en pantalla; las decisiones (bloqueos, ventanas) las toma el servidor.
public record ReglasVistaDto(int ToleranciaMin);

/// <summary>
/// Una pestaña de la tabla: los servicios normales de una planta, o sus servicios MIXTOS
/// (que van en una pestaña aparte, ej. "DANONE MIXTO").
/// </summary>
/// <param name="Clave">Identificador de la pestaña: "12" (planta 12) o "12-M" (sus mixtos).</param>
/// <param name="Conteos">Servicios que operan en cada turno ("1", "2", "3" y "0" = día completo).</param>
public record GrupoTablaDto(
    string Clave,
    int PlantaId,
    bool EsMixto,
    string NombreCorto,
    string NombreCompleto,
    string Cliente,
    string Municipio,
    Dictionary<string, int> Conteos,
    List<FilaServicioDto> Servicios);

public record FilaServicioDto(
    int Id,
    string Ruta,
    string Tipo,
    string Fecha,
    int Turno,
    bool CruzaMedianoche,
    bool EsEspecial,
    bool NoOpero,
    string Hip,
    string Htp,
    string? Hir,
    string? Htr,
    int? DifIni,
    int? DifFin,
    EstatusDto? Estatus,
    int? Aforo,
    string? Unidad,
    List<string> Apoyos,
    OperadorDto? Operador,
    List<FiltroDto> Filtros,
    string? Incidencias,
    bool TieneIncidencia);

public record EstatusDto(string Nombre, string Color);

public record OperadorDto(string Clave, string Nombre, string? Supervisor, string? Coordinador);

public record FiltroDto(int Numero, string Hora, EstatusDto? Estatus, string? Observacion, int Modificaciones);

/// <summary>Petición para marcar (o reactivar) servicios como "no operó hoy".</summary>
public record NoOperoPeticion(List<int> Ids, bool NoOpero);
