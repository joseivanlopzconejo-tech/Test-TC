using System.ComponentModel.DataAnnotations;

namespace TorreControl.Web.Models;

/// <summary>
/// Grupo de plantas que se asigna a monitoristas. VERSIÓN MÍNIMA de la Fase 1: alcanza para
/// decidir qué plantas ve cada quien. El diseño completo (guardias por turno, etc.) es de la Fase 3.
/// </summary>
public class Modulo
{
    public int Id { get; set; }

    [Required, MaxLength(60)]
    public string Nombre { get; set; } = "";

    public TipoModulo Tipo { get; set; }

    public bool Activo { get; set; } = true;

    public List<ModuloPlanta> Plantas { get; set; } = [];
    public List<ModuloUsuario> Usuarios { get; set; } = [];
}
