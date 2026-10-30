using System.ComponentModel.DataAnnotations;

namespace TorreControl.Web.Models;

/// <summary>Catálogo (Tráfico, Falla mecánica...). Existe para poder hacer reportes por tipo.</summary>
public class TipoIncidencia
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Nombre { get; set; } = "";
}
