using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TorreControl.Web.Models;

namespace TorreControl.Web.Data;

/// <summary>
/// Puerta de entrada a la base de datos. Hereda de IdentityDbContext para incluir
/// las tablas de usuarios y roles; las tablas del negocio se agregan en la Fase 1.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<Usuario, IdentityRole<int>, int>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Primero la configuración de Identity; después la renombramos.
        base.OnModelCreating(builder);

        // NumeroEmpleado es solo un alias de UserName: no es una columna aparte.
        builder.Entity<Usuario>().Ignore(u => u.NumeroEmpleado);

        // Nombres de tabla en español y en singular, igual que el resto del modelo,
        // en lugar de los "AspNetUsers", "AspNetRoles"... que trae Identity.
        builder.Entity<Usuario>().ToTable("Usuario");
        builder.Entity<IdentityRole<int>>().ToTable("Rol");
        builder.Entity<IdentityUserRole<int>>().ToTable("UsuarioRol");
        builder.Entity<IdentityUserClaim<int>>().ToTable("UsuarioClaim");
        builder.Entity<IdentityUserLogin<int>>().ToTable("UsuarioLogin");
        builder.Entity<IdentityUserToken<int>>().ToTable("UsuarioToken");
        builder.Entity<IdentityRoleClaim<int>>().ToTable("RolClaim");
    }
}
