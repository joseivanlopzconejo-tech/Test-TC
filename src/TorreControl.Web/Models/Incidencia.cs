using System.ComponentModel.DataAnnotations;

namespace TorreControl.Web.Models;

/// <summary>
/// Bitácora automática de un servicio: una para el inicio y otra para la llegada. No se llena a mano;
/// se genera al capturar Hir/Htr. Si se corrige la hora, esta fila se reemplaza.
/// </summary>
public class Incidencia
{
    public int Id { get; set; }

    public int ServicioId { get; set; }
    public Servicio? Servicio { get; set; }

    public MomentoIncidencia Momento { get; set; }

    public int? TipoIncidenciaId { get; set; }
    public TipoIncidencia? TipoIncidencia { get; set; }

    // Lo que escribió el monitorista cuando la hora salió de tolerancia.
    [MaxLength(500)]
    public string? Motivo { get; set; }

    // Texto final que se muestra y se exporta ("Inicio de ruta sin novedad, ...").
    [Required, MaxLength(700)]
    public string Texto { get; set; } = "";

    public bool EsSinNovedad { get; set; }
}
