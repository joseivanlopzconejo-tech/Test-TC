using System.ComponentModel.DataAnnotations;

namespace TorreControl.Web.Models;

/// <summary>
/// Una sola corrida de una Ruta: un día y un turno. Si una ruta corre 30 días, son 30 Servicios.
/// Es la fila de la tabla del monitorista.
/// </summary>
public class Servicio
{
    public int Id { get; set; }

    public int RutaId { get; set; }
    public Ruta? Ruta { get; set; }

    public DateOnly Fecha { get; set; }

    // Se calcula desde Hip (ReglasTurno.TurnoDe); se guarda para poder filtrar y exportar por turno.
    [Range(1, 3)]
    public int Turno { get; set; }

    /// <summary>Hora de inicio programada.</summary>
    public TimeOnly Hip { get; set; }

    /// <summary>Hora de llegada programada. Si es menor que Hip, llega al día siguiente.</summary>
    public TimeOnly Htp { get; set; }

    /// <summary>Hora de inicio real, fecha-hora completa en hora de CDMX.</summary>
    public DateTime? Hir { get; set; }

    /// <summary>Hora de llegada real, fecha-hora completa en hora de CDMX.</summary>
    public DateTime? Htr { get; set; }

    // Cuántas veces se capturó Hir/Htr (1ª = llenado, 2ª = corrección; luego se bloquea).
    public int HirModificaciones { get; set; }
    public int HtrModificaciones { get; set; }

    // null = "Sin acción".
    public int? EstatusId { get; set; }
    public EstatusUnidad? Estatus { get; set; }

    // null = todavía no se captura. El rango real (0..AforoMax) se valida en el servicio de negocio
    // porque el máximo viene de configuración.
    public int? Aforo { get; set; }

    public int? UnidadTitularId { get; set; }
    public Unidad? UnidadTitular { get; set; }

    public int? OperadorId { get; set; }
    public Operador? Operador { get; set; }

    // Servicio agregado a mano por un supervisor (especial, 12x12...) en vez de la programación base.
    public bool EsEspecial { get; set; }

    // null = lo generó el sistema (programación base o datos de ejemplo), no una persona.
    public int? CreadoPorId { get; set; }
    public Usuario? CreadoPor { get; set; }

    // Concurrencia optimista: SQL Server cambia este valor solo en cada UPDATE. Si al guardar
    // ya no coincide con el que leímos, alguien más modificó la fila y no se sobrescribe a ciegas.
    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    public List<UnidadApoyo> UnidadesApoyo { get; set; } = [];
    public List<Filtro> Filtros { get; set; } = [];
    public List<Incidencia> Incidencias { get; set; } = [];
}
