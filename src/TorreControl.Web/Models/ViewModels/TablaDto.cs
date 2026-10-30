namespace TorreControl.Web.Models.ViewModels;

// Lo que recibe el JavaScript de la tabla. Son "fotos" de solo lectura (records): se arman en el
// servidor con las horas ya calculadas, para que el navegador solo muestre y nunca decida.
// Todas las horas van en 24 h ("HH:mm") y en hora de CDMX.

public record TablaRespuesta(
    DateTime AhoraUtc,
    int TurnoActual,
    VentanaDto Ventana,
    List<PlantaTablaDto> Plantas);

public record VentanaDto(string Dia, string Desde, string Hasta, int Turno);

public record PlantaTablaDto(int Id, string Nombre, string Cliente, List<FilaServicioDto> Servicios);

public record FilaServicioDto(
    int Id,
    string Ruta,
    string Tipo,
    string Fecha,
    int Turno,
    bool EsMixto,
    bool CruzaMedianoche,
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
