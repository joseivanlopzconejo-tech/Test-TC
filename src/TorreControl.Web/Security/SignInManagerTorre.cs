using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TorreControl.Web.Models;

namespace TorreControl.Web.Security;

/// <summary>
/// El SignInManager de Identity con una regla extra: un usuario desactivado (Activo = false)
/// no puede iniciar sesión, y si ya tenía una sesión abierta, se le cierra en la siguiente
/// revalidación de la cookie.
/// </summary>
public class SignInManagerTorre(
    UserManager<Usuario> userManager,
    IHttpContextAccessor contextAccessor,
    IUserClaimsPrincipalFactory<Usuario> claimsFactory,
    IOptions<IdentityOptions> optionsAccessor,
    ILogger<SignInManager<Usuario>> logger,
    IAuthenticationSchemeProvider schemes,
    IUserConfirmation<Usuario> confirmation)
    : SignInManager<Usuario>(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
{
    public override async Task<bool> CanSignInAsync(Usuario user) =>
        user.Activo && await base.CanSignInAsync(user);

    public override async Task<Usuario?> ValidateSecurityStampAsync(ClaimsPrincipal? principal)
    {
        var usuario = await base.ValidateSecurityStampAsync(principal);
        return usuario is { Activo: true } ? usuario : null;
    }
}
