namespace TorreControl.Web.Models;

// Se guardan como texto en la base ("E", "Inicio", "Fijo"...) y no como número,
// para que una consulta directa a SQL o un reporte se entiendan sin tabla de traducción.

/// <summary>E = entrada (llevan personal a la planta) · S = salida (lo regresan).</summary>
public enum TipoServicio
{
    E,
    S
}

public enum MomentoIncidencia
{
    Inicio,
    Llegada
}

/// <summary>Fijo = módulo de lunes a viernes · Guardia = perfil de fin de semana armado desde cero.</summary>
public enum TipoModulo
{
    Fijo,
    Guardia
}

public enum AccionAuditoria
{
    Crear,
    Modificar,
    Eliminar,
    InicioSesion,
    CierreSesion,
    Bloqueo
}
