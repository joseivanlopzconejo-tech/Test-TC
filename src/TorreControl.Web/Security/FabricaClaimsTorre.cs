using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TorreControl.Web.Models;

namespace TorreControl.Web.Security;

/// <summary>
/// Decide qué datos viajan dentro de la cookie de sesión. Agregamos el nombre para mostrarlo
/// en la barra superior sin consultar la base en cada página.
/// </summary>
public class FabricaClaimsTorre(
    UserManager<Usuario> userManager,
    RoleManager<IdentityRole<int>> roleManager,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<Usuario, IdentityRole<int>>(userManager, roleManager, options)
{
    public const string ClaimNombre = "tc:nombre";

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(Usuario user)
    {
        var identidad = await base.GenerateClaimsAsync(user);
        identidad.AddClaim(new Claim(ClaimNombre, user.Nombre));
        return identidad;
    }
}
