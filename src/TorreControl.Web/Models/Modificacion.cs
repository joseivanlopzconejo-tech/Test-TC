using System.ComponentModel.DataAnnotations;

namespace TorreControl.Web.Models;

/// <summary>
/// Cada captura del Filtro 4. Es un bloque independiente: una vez guardado queda fijo.
/// La celda muestra siempre la última.
/// </summary>
public class Modificacion
{
    public int Id { get; set; }

    public int FiltroId { get; set; }
    public Filtro? Filtro { get; set; }

    public int NumeroModificacion { get; set; }

    public int EstatusId { get; set; }
    public EstatusUnidad? Estatus { get; set; }

    [Required, MaxLength(500)]
    public string Observacion { get; set; } = "";

    public DateTime CapturadoEn { get; set; }

    public int CapturadoPorId { get; set; }
    public Usuario? CapturadoPor { get; set; }
}
