using System.ComponentModel.DataAnnotations;

namespace TorreControl.Web.Models;

/// <summary>Empresa a la que se le da el servicio de transporte. Una empresa tiene varias plantas.</summary>
public class Cliente
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string Codigo { get; set; } = "";

    [Required, MaxLength(100)]
    public string Nombre { get; set; } = "";

    public List<Planta> Plantas { get; set; } = [];
}
