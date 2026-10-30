using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using TorreControl.Web.Models.ViewModels;
using TorreControl.Web.Security;

namespace TorreControl.Web.Controllers;

public class CuentaController(ServicioInicioSesion inicioSesion, IOptions<IdentityOptions> identidad) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirigirLocal(returnUrl);
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting(LimiteLogin.Politica)]
    public async Task<IActionResult> Login(LoginViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        if (await inicioSesion.IniciarAsync(modelo.NumeroEmpleado, modelo.Contrasena))
        {
            return RedirigirLocal(modelo.ReturnUrl);
        }

        // Mismo mensaje para TODO fallo: no revela si el número existe, si está bloqueado
        // o desactivado. Se menciona la espera para que un usuario real sepa qué hacer.
        var minutos = (int)identidad.Value.Lockout.DefaultLockoutTimeSpan.TotalMinutes;
        ModelState.AddModelError("",
            $"Número de empleado o contraseña incorrectos. Si fallaste varias veces seguidas, espera {minutos} minutos o pide apoyo a tu supervisor.");
        modelo.Contrasena = "";
        return View(modelo);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        int? id = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var valor) ? valor : null;
        await inicioSesion.CerrarAsync(id);
        return RedirectToAction(nameof(Login));
    }

    /// <summary>
    /// "Seguir trabajando" del aviso de sesión: cualquier petición con sesión renueva la cookie
    /// (expiración deslizante), así que solo hay que contestar la nueva hora de vencimiento.
    /// </summary>
    [HttpPost]
    public IActionResult MantenerSesion() =>
        Json(new { expiraUtc = HttpContext.Items[SesionDeslizante.ClaveExpira] });

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccesoDenegado() => View();

    // Solo se regresa a direcciones de ESTA app. Sin esta revisión, un enlace como
    // /Cuenta/Login?returnUrl=https://sitio-falso.com mandaría al usuario a otro sitio
    // justo después de escribir su contraseña (redirección abierta).
    private IActionResult RedirigirLocal(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction("Index", "Home");
}
