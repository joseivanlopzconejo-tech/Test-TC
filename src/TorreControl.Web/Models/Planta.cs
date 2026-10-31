using System.ComponentModel.DataAnnotations;

namespace TorreControl.Web.Models;

/// <summary>Planta industrial de un cliente. Es la unidad de permiso: un monitorista ve plantas, no clientes.</summary>
public class Planta
{
    public int Id { get; set; }

    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    [Required, MaxLength(100)]
    public string Nombre { get; set; } = "";

    // Como se le dice en la torre (ej. "DANONE"); es lo que muestra la pestaña.
    // El nombre completo aparece arriba al elegirla.
    [MaxLength(30)]
    public string NombreCorto { get; set; } = "";

    [Required, MaxLength(100)]
    public string Municipio { get; set; } = "";

    public List<Ruta> Rutas { get; set; } = [];
}
