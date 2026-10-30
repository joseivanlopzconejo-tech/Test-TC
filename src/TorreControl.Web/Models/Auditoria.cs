using System.ComponentModel.DataAnnotations;

namespace TorreControl.Web.Models;

/// <summary>
/// Bitácora de cambios: quién, cuándo, qué entidad y valores antes/después. Se escribe sola en
/// cada SaveChanges (ver AppDbContext). Es "solo agregar": nunca se edita ni se borra.
/// Una fila por campo modificado, para que reportes y el futuro agente de IA puedan consultarla
/// con SQL simple.
/// </summary>
public class Auditoria
{
    public long Id { get; set; }

    // Siempre UTC: sin ambigüedad aunque México volviera a cambiar de horario.
    public DateTime FechaUtc { get; set; }

    // null = lo hizo el sistema (datos de ejemplo, procesos automáticos). Sin llave foránea a
    // propósito: la bitácora no debe impedir nada ni depender de otras tablas.
    public int? UsuarioId { get; set; }

    // Agrupa todas las filas de un mismo guardado (un cambio que tocó 3 campos = 3 filas, 1 grupo).
    public Guid Transaccion { get; set; }

    public AccionAuditoria Accion { get; set; }

    [Required, MaxLength(50)]
    public string Entidad { get; set; } = "";

    [MaxLength(50)]
    public string? EntidadId { get; set; }

    [MaxLength(100)]
    public string? Campo { get; set; }

    public string? ValorAnterior { get; set; }

    public string? ValorNuevo { get; set; }
}
