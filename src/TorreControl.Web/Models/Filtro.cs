using System.ComponentModel.DataAnnotations;

namespace TorreControl.Web.Models;

/// <summary>
/// Revisión previa a la salida (solo servicios tipo E). Hay 4 por servicio; su hora la calcula
/// CalculadoraFiltros y funciona como hora límite.
/// </summary>
public class Filtro
{
    public int Id { get; set; }

    public int ServicioId { get; set; }
    public Servicio? Servicio { get; set; }

    [Range(1, 4)]
    public int NumeroFiltro { get; set; }

    // Fecha-hora completa porque puede caer en el día anterior.
    public DateTime HoraProgramada { get; set; }

    // Filtros 1-3: se capturan una vez. null = "Sin acción".
    public int? EstatusId { get; set; }
    public EstatusUnidad? Estatus { get; set; }

    [MaxLength(500)]
    public string? Observacion { get; set; }

    public DateTime? CapturadoEn { get; set; }

    public int? CapturadoPorId { get; set; }
    public Usuario? CapturadoPor { get; set; }

    // Solo el Filtro 4 usa modificaciones (hasta MaxModificacionesFiltro4).
    public List<Modificacion> Modificaciones { get; set; } = [];
}
