using System.ComponentModel.DataAnnotations;

namespace TorreControl.Web.Models;

/// <summary>Camión o camioneta. El número es texto (no int) para conservar ceros a la izquierda y permitir búsqueda parcial.</summary>
public class Unidad
{
    public int Id { get; set; }

    [Required, MaxLength(10)]
    public string Numero { get; set; } = "";

    // Dar de baja en lugar de borrar: los servicios viejos siguen apuntando a esta unidad.
    public bool Activa { get; set; } = true;
}
