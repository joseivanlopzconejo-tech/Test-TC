using System.Linq.Expressions;
using TorreControl.Web.Models;

namespace TorreControl.Web.Services;

/// <summary>Reglas puras de acceso a plantas por módulo.</summary>
public static class ReglasAcceso
{
    /// <summary>
    /// Una asignación a módulo cuenta si "hoy" cae dentro de su rango de fechas y el módulo
    /// está activo. Es una expresión (no un método normal) para que la MISMA regla sirva
    /// tanto en memoria como traducida a SQL por EF Core: una sola definición, sin copias.
    /// </summary>
    public static Expression<Func<ModuloUsuario, bool>> AsignacionVigente(DateOnly hoy) =>
        a => a.FechaInicio <= hoy
             && (a.FechaFin == null || a.FechaFin >= hoy)
             && a.Modulo!.Activo;
}
