using TorreControl.Web.Models;

namespace TorreControl.Web.Services;

/// <summary>
/// Calcula el estatus que muestra la columna Estatus (decisión del 31/10/2026). Nunca se captura:
/// se deduce de lo que ya pasó, así es imposible que diga "En planta" sin hora de llegada.
///
/// | Servicio    | Antes de salir                     | Con Hir    | Con Htr   |
/// |-------------|------------------------------------|------------|-----------|
/// | Entrada (E) | Estatus del último filtro capturado | En ruta    | En planta |
/// | Salida (S)  | Sin acción (null)                  | En salida  | Resguardo |
/// </summary>
public static class EstatusServicio
{
    public record Estatus(string Nombre, string Color);

    // Colores pastel fijos: estos estatus son del negocio, no del catálogo editable.
    public static readonly Estatus EnRuta = new("En ruta", "#8EC5FF");      // azul
    public static readonly Estatus EnPlanta = new("En planta", "#9FE2BF");  // verde
    public static readonly Estatus EnSalida = new("En salida", "#FFC48C");  // naranja
    public static readonly Estatus Resguardo = new("Resguardo", "#AFBCCF"); // gris azulado

    /// <param name="filtrosCapturados">
    /// Estatus capturado de cada filtro (número de filtro → estatus). Para el Filtro 4 va el de su
    /// última modificación. Los filtros sin captura no se incluyen.
    /// </param>
    /// <returns>El estatus a mostrar, o null = "Sin acción".</returns>
    public static Estatus? Calcular(
        TipoServicio tipo,
        DateTime? hir,
        DateTime? htr,
        IReadOnlyDictionary<int, EstatusUnidad> filtrosCapturados)
    {
        if (tipo == TipoServicio.S)
        {
            return htr is not null ? Resguardo : hir is not null ? EnSalida : null;
        }

        if (htr is not null)
        {
            return EnPlanta;
        }

        if (hir is not null)
        {
            return EnRuta;
        }

        // Antes de salir: el del filtro más reciente (se capturan en orden F1 → F4).
        if (filtrosCapturados.Count == 0)
        {
            return null;
        }

        var ultimo = filtrosCapturados[filtrosCapturados.Keys.Max()];
        return new Estatus(ultimo.Nombre, ultimo.Color);
    }
}
