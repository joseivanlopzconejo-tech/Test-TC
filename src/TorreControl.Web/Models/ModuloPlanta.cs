namespace TorreControl.Web.Models;

/// <summary>Qué plantas forman un módulo (una fila por planta).</summary>
public class ModuloPlanta
{
    public int Id { get; set; }

    public int ModuloId { get; set; }
    public Modulo? Modulo { get; set; }

    public int PlantaId { get; set; }
    public Planta? Planta { get; set; }
}
