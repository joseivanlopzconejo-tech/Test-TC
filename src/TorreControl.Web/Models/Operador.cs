using System.ComponentModel.DataAnnotations;

namespace TorreControl.Web.Models;

/// <summary>
/// Chofer de la unidad. Supervisor y Coordinador son texto libre por ahora; en la Fase 3
/// se decide si pasan a ser catálogo.
/// </summary>
public class Operador
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string Clave { get; set; } = "";

    [Required, MaxLength(100)]
    public string Nombre { get; set; } = "";

    [MaxLength(100)]
    public string? Supervisor { get; set; }

    [MaxLength(100)]
    public string? Coordinador { get; set; }

    public bool Activo { get; set; } = true;
}
