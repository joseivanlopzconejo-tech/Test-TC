using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace TorreControl.Web.Models;

/// <summary>
/// Usuario del sistema. Hereda de IdentityUser para que Identity se encargue del hash
/// de contraseña, el bloqueo por intentos y los roles (no inventamos criptografía propia).
/// La llave es int (no el GUID de Identity) para seguir la regla de "Id autoincremental propio".
/// </summary>
public class Usuario : IdentityUser<int>
{
    // El número de empleado ES el UserName de Identity (con eso inicia sesión);
    // esta propiedad solo le da un nombre claro en el código.
    public string NumeroEmpleado
    {
        get => UserName ?? "";
        set => UserName = value;
    }

    [Required, MaxLength(100)]
    public string Nombre { get; set; } = "";

    // Desactivar en lugar de borrar: la Auditoría sigue apuntando a quién hizo cada cambio.
    public bool Activo { get; set; } = true;
}
