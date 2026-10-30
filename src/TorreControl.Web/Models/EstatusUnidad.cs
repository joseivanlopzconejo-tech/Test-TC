using System.ComponentModel.DataAnnotations;

namespace TorreControl.Web.Models;

/// <summary>
/// Catálogo de estados de la unidad (Apagada, Trayecto, Finalizado). "Sin acción" NO es una fila:
/// es simplemente no tener estatus (null).
/// </summary>
public class EstatusUnidad
{
    public int Id { get; set; }

    [Required, MaxLength(30)]
    public string Nombre { get; set; } = "";

    // Color del chip en la tabla, en formato #RRGGBB.
    [Required, MaxLength(7)]
    public string Color { get; set; } = "";
}
