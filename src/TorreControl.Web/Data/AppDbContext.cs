using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TorreControl.Web.Models;

namespace TorreControl.Web.Data;

/// <summary>
/// Puerta de entrada a la base de datos. Hereda de IdentityDbContext para incluir
/// las tablas de usuarios y roles, y agrega las del negocio.
/// La escritura automática de la Auditoría está en AppDbContext.Auditoria.cs.
/// </summary>
public partial class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IUsuarioActual? usuarioActual = null,
    TimeProvider? reloj = null)
    : IdentityDbContext<Usuario, IdentityRole<int>, int>(options)
{
    private readonly IUsuarioActual? _usuarioActual = usuarioActual;
    private readonly TimeProvider _reloj = reloj ?? TimeProvider.System;

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Planta> Plantas => Set<Planta>();
    public DbSet<Unidad> Unidades => Set<Unidad>();
    public DbSet<Ruta> Rutas => Set<Ruta>();
    public DbSet<Servicio> Servicios => Set<Servicio>();
    public DbSet<UnidadApoyo> UnidadesApoyo => Set<UnidadApoyo>();
    public DbSet<Filtro> Filtros => Set<Filtro>();
    public DbSet<Modificacion> Modificaciones => Set<Modificacion>();
    public DbSet<Incidencia> Incidencias => Set<Incidencia>();
    public DbSet<TipoIncidencia> TiposIncidencia => Set<TipoIncidencia>();
    public DbSet<EstatusUnidad> EstatusUnidad => Set<EstatusUnidad>();
    public DbSet<Operador> Operadores => Set<Operador>();
    public DbSet<Modulo> Modulos => Set<Modulo>();
    public DbSet<ModuloPlanta> ModulosPlanta => Set<ModuloPlanta>();
    public DbSet<ModuloUsuario> ModulosUsuario => Set<ModuloUsuario>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Primero la configuración de Identity; después la renombramos.
        base.OnModelCreating(builder);

        ConfigurarIdentity(builder);
        ConfigurarNegocio(builder);
        ConfigurarBorrados(builder);
        SembrarCatalogos(builder);
    }

    private static void ConfigurarIdentity(ModelBuilder builder)
    {
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

    private static void ConfigurarNegocio(ModelBuilder builder)
    {
        // Las tablas se llaman como la entidad, en singular (Cliente, Planta...), igual que en la especificación.
        builder.Entity<Cliente>(e =>
        {
            e.ToTable("Cliente");
            e.HasIndex(c => c.Codigo).IsUnique();
        });

        builder.Entity<Planta>().ToTable("Planta");

        builder.Entity<Unidad>(e =>
        {
            e.ToTable("Unidad");
            e.HasIndex(u => u.Numero).IsUnique();
        });

        builder.Entity<Ruta>(e =>
        {
            e.ToTable("Ruta");
            e.Property(r => r.TipoServicio).HasConversion<string>().HasMaxLength(1);
            // La misma ruta existe una vez como entrada y otra como salida en cada planta.
            e.HasIndex(r => new { r.PlantaId, r.NombreRuta, r.TipoServicio }).IsUnique();
        });

        builder.Entity<Servicio>(e =>
        {
            e.ToTable("Servicio", t =>
            {
                t.HasCheckConstraint("CK_Servicio_Turno", "[Turno] BETWEEN 1 AND 3");
                t.HasCheckConstraint("CK_Servicio_Aforo", "[Aforo] IS NULL OR [Aforo] >= 0");
            });
            // La tabla del monitorista siempre busca por fecha (día actual y anterior).
            e.HasIndex(s => s.Fecha);
            e.HasIndex(s => new { s.RutaId, s.Fecha });
        });

        builder.Entity<UnidadApoyo>(e =>
        {
            e.ToTable("UnidadApoyo");
            // Una unidad no puede apoyar dos veces al mismo servicio, ni repetir posición.
            e.HasIndex(a => new { a.ServicioId, a.UnidadId }).IsUnique();
            e.HasIndex(a => new { a.ServicioId, a.Orden }).IsUnique();
        });

        builder.Entity<Filtro>(e =>
        {
            e.ToTable("Filtro", t => t.HasCheckConstraint("CK_Filtro_Numero", "[NumeroFiltro] BETWEEN 1 AND 4"));
            e.HasIndex(f => new { f.ServicioId, f.NumeroFiltro }).IsUnique();
        });

        builder.Entity<Modificacion>(e =>
        {
            e.ToTable("Modificacion");
            e.HasIndex(m => new { m.FiltroId, m.NumeroModificacion }).IsUnique();
        });

        builder.Entity<Incidencia>(e =>
        {
            e.ToTable("Incidencia");
            e.Property(i => i.Momento).HasConversion<string>().HasMaxLength(10);
            // Una sola incidencia de inicio y una de llegada por servicio.
            e.HasIndex(i => new { i.ServicioId, i.Momento }).IsUnique();
        });

        builder.Entity<TipoIncidencia>(e =>
        {
            e.ToTable("TipoIncidencia");
            e.HasIndex(t => t.Nombre).IsUnique();
        });

        builder.Entity<EstatusUnidad>(e =>
        {
            e.ToTable("EstatusUnidad");
            e.HasIndex(s => s.Nombre).IsUnique();
        });

        builder.Entity<Operador>(e =>
        {
            e.ToTable("Operador");
            e.HasIndex(o => o.Clave).IsUnique();
        });

        builder.Entity<Modulo>(e =>
        {
            e.ToTable("Modulo");
            e.Property(m => m.Tipo).HasConversion<string>().HasMaxLength(10);
            e.HasIndex(m => m.Nombre).IsUnique();
        });

        builder.Entity<ModuloPlanta>(e =>
        {
            e.ToTable("ModuloPlanta");
            e.HasIndex(mp => new { mp.ModuloId, mp.PlantaId }).IsUnique();
        });

        builder.Entity<ModuloUsuario>(e =>
        {
            e.ToTable("ModuloUsuario");
            e.HasIndex(mu => new { mu.UsuarioId, mu.FechaInicio });
        });

        builder.Entity<Auditoria>(e =>
        {
            e.ToTable("Auditoria");
            e.Property(a => a.Accion).HasConversion<string>().HasMaxLength(20);
            // Preguntas típicas: "historia de este servicio", "qué hizo este usuario", "qué pasó ayer".
            e.HasIndex(a => new { a.Entidad, a.EntidadId });
            e.HasIndex(a => a.UsuarioId);
            e.HasIndex(a => a.FechaUtc);
        });
    }

    /// <summary>
    /// Por defecto NADA se borra en cascada: borrar una planta no debe llevarse en silencio sus rutas,
    /// servicios y capturas. Las únicas excepciones son las piezas que solo existen dentro de su
    /// servicio (apoyos, filtros, incidencias, modificaciones).
    /// </summary>
    private static void ConfigurarBorrados(ModelBuilder builder)
    {
        var llavesDelNegocio = builder.Model.GetEntityTypes()
            .Where(t => t.ClrType.Namespace == typeof(Servicio).Namespace)
            .SelectMany(t => t.GetForeignKeys());

        foreach (var llave in llavesDelNegocio)
        {
            llave.DeleteBehavior = DeleteBehavior.Restrict;
        }

        builder.Entity<UnidadApoyo>().HasOne(a => a.Servicio).WithMany(s => s.UnidadesApoyo).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Filtro>().HasOne(f => f.Servicio).WithMany(s => s.Filtros).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Incidencia>().HasOne(i => i.Servicio).WithMany(s => s.Incidencias).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Modificacion>().HasOne(m => m.Filtro).WithMany(f => f.Modificaciones).OnDelete(DeleteBehavior.Cascade);
    }

    /// <summary>
    /// Catálogos fijos que necesita la app en cualquier ambiente (no son datos de ejemplo):
    /// van dentro de la migración para que existan desde el primer "database update".
    /// </summary>
    private static void SembrarCatalogos(ModelBuilder builder)
    {
        // Estatus que se eligen en los filtros, en tonos pastel (fáciles de distinguir y que no
        // cansan la vista). El Id 3 era "Finalizado": ese estado ahora se calcula (En planta /
        // Resguardo), así que se reutiliza. En la Fase 3 este catálogo será editable.
        builder.Entity<EstatusUnidad>().HasData(
            new EstatusUnidad { Id = 1, Nombre = "Apagada", Color = "#F4A6A6" },
            new EstatusUnidad { Id = 2, Nombre = "Trayecto", Color = "#F6D88A" },
            new EstatusUnidad { Id = 3, Nombre = "Encendida", Color = "#B5E8B0" },
            new EstatusUnidad { Id = 4, Nombre = "En base", Color = "#D9C3A5" },
            new EstatusUnidad { Id = 5, Nombre = "Detenida", Color = "#CDB8F2" },
            new EstatusUnidad { Id = 6, Nombre = "Pérdida de cobertura", Color = "#C8D0DA" },
            new EstatusUnidad { Id = 7, Nombre = "Falla GPS", Color = "#A7B1BE" },
            new EstatusUnidad { Id = 8, Nombre = "Sin respuesta del operador", Color = "#F2A27E" },
            new EstatusUnidad { Id = 9, Nombre = "Falla mecánica", Color = "#E89393" });

        // Motivos de retraso que se piden al capturar una llegada tarde.
        builder.Entity<TipoIncidencia>().HasData(
            new TipoIncidencia { Id = 1, Nombre = "Tráfico" },
            new TipoIncidencia { Id = 2, Nombre = "Falla mecánica" },
            new TipoIncidencia { Id = 3, Nombre = "Operador" },
            new TipoIncidencia { Id = 4, Nombre = "Cliente / planta" },
            new TipoIncidencia { Id = 5, Nombre = "Otro" },
            new TipoIncidencia { Id = 6, Nombre = "Cruce de tren" },
            new TipoIncidencia { Id = 7, Nombre = "Accidente vial" },
            new TipoIncidencia { Id = 8, Nombre = "Bloqueo o manifestación" },
            new TipoIncidencia { Id = 9, Nombre = "Clima" });

        // Los roles son parte del sistema (no datos de ejemplo). El ConcurrencyStamp es fijo
        // porque HasData necesita valores que no cambien entre una migración y otra.
        builder.Entity<IdentityRole<int>>().HasData(
            Security.Roles.Todos.Select((rol, i) => new IdentityRole<int>
            {
                Id = i + 1,
                Name = rol,
                NormalizedName = rol.ToUpperInvariant(),
                ConcurrencyStamp = $"rol-{i + 1}"
            }));
    }
}
