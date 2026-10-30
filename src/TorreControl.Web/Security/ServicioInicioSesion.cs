using Microsoft.AspNetCore.Identity;
using TorreControl.Web.Data;
using TorreControl.Web.Models;

namespace TorreControl.Web.Security;

/// <summary>
/// Inicio y cierre de sesión. Regla de oro: desde afuera NUNCA se debe poder saber si un
/// número de empleado existe. Por eso todos los fallos (no existe, contraseña mala, bloqueado,
/// desactivado) se ven iguales y tardan lo mismo.
/// </summary>
public class ServicioInicioSesion(
    SignInManager<Usuario> signInManager,
    UserManager<Usuario> userManager,
    AppDbContext db,
    ILogger<ServicioInicioSesion> logger)
{
    // Hash de una contraseña al azar, calculado una sola vez. Cuando el usuario no existe se
    // "verifica" contra este hash para gastar el mismo tiempo que con un usuario real; si no,
    // un atacante podría distinguir "no existe" (respuesta rápida) de "contraseña mala" (lenta).
    private static readonly Lazy<string> HashSenuelo = new(() =>
        new PasswordHasher<Usuario>().HashPassword(new Usuario(), Guid.NewGuid().ToString()));

    public async Task<bool> IniciarAsync(string numeroEmpleado, string contrasena)
    {
        var usuario = await userManager.FindByNameAsync(numeroEmpleado.Trim());

        if (usuario is null)
        {
            GastarTiempoDeVerificacion(contrasena);
            logger.LogWarning("Inicio de sesión fallido: número de empleado inexistente.");
            return false;
        }

        // lockoutOnFailure: cada fallo suma; al llegar al máximo, Identity bloquea la cuenta.
        // isPersistent: false = la sesión muere al cerrar el navegador. En una torre con equipos
        // compartidos evita que el siguiente turno entre con la sesión (y el nombre) del anterior.
        var resultado = await signInManager.PasswordSignInAsync(usuario, contrasena, isPersistent: false, lockoutOnFailure: true);

        if (resultado.Succeeded)
        {
            db.RegistrarEvento(AccionAuditoria.InicioSesion, usuario.Id, "Usuario", usuario.Id.ToString());
            await db.SaveChangesAsync();
            return true;
        }

        // Bloqueado o desactivado: Identity contesta sin revisar la contraseña (rápido);
        // igualamos el tiempo para no delatar que la cuenta existe.
        if (resultado.IsLockedOut || resultado.IsNotAllowed)
        {
            GastarTiempoDeVerificacion(contrasena);
        }

        logger.LogWarning("Inicio de sesión fallido para el usuario {UsuarioId} (bloqueado: {Bloqueado}, no permitido: {NoPermitido}).",
            usuario.Id, resultado.IsLockedOut, resultado.IsNotAllowed);
        return false;
    }

    public async Task CerrarAsync(int? usuarioId)
    {
        await signInManager.SignOutAsync();
        if (usuarioId is not null)
        {
            db.RegistrarEvento(AccionAuditoria.CierreSesion, usuarioId, "Usuario", usuarioId.ToString());
            await db.SaveChangesAsync();
        }
    }

    private static void GastarTiempoDeVerificacion(string contrasena) =>
        new PasswordHasher<Usuario>().VerifyHashedPassword(new Usuario(), HashSenuelo.Value, contrasena);
}
