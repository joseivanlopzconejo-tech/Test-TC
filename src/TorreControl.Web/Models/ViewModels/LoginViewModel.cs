using System.ComponentModel.DataAnnotations;

namespace TorreControl.Web.Models.ViewModels;

/// <summary>Lo que llega del formulario de inicio de sesión. Las longitudes se validan en el servidor.</summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "Escribe tu número de empleado.")]
    [MaxLength(20)]
    [Display(Name = "Número de empleado")]
    public string NumeroEmpleado { get; set; } = "";

    [Required(ErrorMessage = "Escribe tu contraseña.")]
    [MaxLength(128)]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Contrasena { get; set; } = "";

    // Página a la que regresar después de entrar (conserva planta, día y horas de la vista).
    public string? ReturnUrl { get; set; }
}
