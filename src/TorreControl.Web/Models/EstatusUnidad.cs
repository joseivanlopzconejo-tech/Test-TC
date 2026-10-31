using System.ComponentModel.DataAnnotations;

namespace TorreControl.Web.Models;

/// <summary>
/// Catálogo de estatus que el monitorista elige en los filtros (Apagada, Encendida, Trayecto...).
/// "Sin acción" NO es una fila: es simplemente no tener estatus (null). Los estatus del servicio
/// después de salir (En ruta, En planta, En salida, Resguardo) no están aquí: los calcula
/// EstatusServicio.
/// </summary>
public class EstatusUnidad
{
    public int Id { get; set; }

    [Required, MaxLength(30)]
    public string Nombre { get; set; } = "";

    // Color del chip en la tabla, en formato #RRGGBB (tonos pastel).
    [Required, MaxLength(7)]
    public string Color { get; set; } = "";
}
