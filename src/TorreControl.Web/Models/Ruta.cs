using System.ComponentModel.DataAnnotations;

namespace TorreControl.Web.Models;

/// <summary>
/// Recorrido fijo hacia (E) o desde (S) una planta. NO cambia día a día: cada corrida diaria
/// es un Servicio que apunta a esta ruta.
/// </summary>
public class Ruta
{
    public int Id { get; set; }

    public int PlantaId { get; set; }
    public Planta? Planta { get; set; }

    [Required, MaxLength(100)]
    public string NombreRuta { get; set; } = "";

    public TipoServicio TipoServicio { get; set; }

    public bool Activa { get; set; } = true;
}
