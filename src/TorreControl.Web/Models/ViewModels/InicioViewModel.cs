namespace TorreControl.Web.Models.ViewModels;

/// <summary>Pantalla de inicio provisional: qué plantas puede ver el usuario hoy y cuántos servicios tienen.</summary>
public class InicioViewModel
{
    public DateOnly Hoy { get; set; }

    public List<PlantaResumen> Plantas { get; set; } = [];

    public record PlantaResumen(string Cliente, string Planta, string Municipio, int ServiciosHoy);
}
