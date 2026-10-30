namespace TorreControl.Web.Models;

/// <summary>
/// Asignación de un usuario a un módulo durante un rango de fechas. Con fechas, una guardia de
/// fin de semana "vence sola" el lunes sin que nadie tenga que acordarse de quitarla.
/// </summary>
public class ModuloUsuario
{
    public int Id { get; set; }

    public int ModuloId { get; set; }
    public Modulo? Modulo { get; set; }

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public DateOnly FechaInicio { get; set; }

    // null = sin fecha de fin (módulo fijo vigente).
    public DateOnly? FechaFin { get; set; }
}
