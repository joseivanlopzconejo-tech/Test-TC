using System.Security.Claims;

namespace TorreControl.Tests;

/// <summary>Arma un usuario "con sesión iniciada" en memoria, sin pasar por el login.</summary>
public static class Usuarios
{
    public static ClaimsPrincipal ConRoles(int id, params string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, id.ToString()) };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Prueba"));
    }

    public static ClaimsPrincipal Anonimo() => new(new ClaimsIdentity());
}
