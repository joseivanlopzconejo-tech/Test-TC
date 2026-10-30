namespace TorreControl.Web.Models;

/// <summary>
/// Unidad extra que apoya a un servicio. Una fila por apoyo (no columnas Apoyo1, Apoyo2...).
/// Máximo MaxApoyos por servicio, sin repetir unidad y sin repetir al titular: eso lo valida
/// el servicio de negocio; la base además impide duplicados.
/// </summary>
public class UnidadApoyo
{
    public int Id { get; set; }

    public int ServicioId { get; set; }
    public Servicio? Servicio { get; set; }

    public int UnidadId { get; set; }
    public Unidad? Unidad { get; set; }

    // Posición del apoyo (1, 2, 3), para mostrarlos siempre en el mismo orden.
    public int Orden { get; set; }
}
