using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TorreControl.Web.Models;
using TorreControl.Web.Security;
using TorreControl.Web.Services;

namespace TorreControl.Web.Data;

/// <summary>
/// Carga datos de ejemplo FICTICIOS para desarrollo y demostración (solo corre en Development).
/// Ningún nombre, número de unidad, clave ni número de empleado viene de la empresa real.
///
/// - Catálogos (clientes, plantas, rutas, unidades, operadores, módulos): una sola vez.
/// - Servicios: se aseguran ayer, hoy y mañana en cada arranque, para que siempre haya
///   datos "de hoy" con los cuales probar la tabla.
/// - Usuarios: uno por rol (dos monitoristas, para probar permisos entre módulos). La contraseña
///   sale de user-secrets ("DatosEjemplo:Contrasena"); si no está, no se crean usuarios.
/// </summary>
public class DatosEjemplo(
    AppDbContext db,
    UserManager<Usuario> usuarios,
    ReglasTurno turnos,
    CalculadoraFiltros filtros,
    RelojNegocio reloj,
    IConfiguration config,
    ILogger<DatosEjemplo> logger)
{
    private static readonly (string Codigo, string Nombre)[] Clientes =
    [
        ("NVA", "Norvial Autopartes"),
        ("QTK", "Quimtek Industrial"),
        ("LMB", "Lumbra Electrodomésticos")
    ];

    // (cliente, planta, municipio, hora de inicio del 1er turno de la planta)
    // Unas plantas cambian de turno a las 7, 15 y 23 h y otras a las 6, 14 y 22 h; así hay
    // servicios de salida que cruzan el cambio de turno de la torre (MIXTOS) y otros que no.
    private static readonly (string Cliente, string Planta, string Municipio, int HoraTurno)[] Plantas =
    [
        ("NVA", "Norvial Silao", "Silao", 7),
        ("NVA", "Norvial Irapuato", "Irapuato", 6),
        ("QTK", "Quimtek Salamanca", "Salamanca", 7),
        ("QTK", "Quimtek León", "León", 6),
        ("LMB", "Lumbra Celaya", "Celaya", 7),
        ("LMB", "Lumbra Guanajuato", "Guanajuato", 6)
    ];

    // Dos rutas por planta (en el mismo orden que Plantas); cada una existe como E y como S.
    private static readonly string[] NombresRuta =
    [
        "Centro", "Las Arboledas", "San Nicolás", "Valle Verde", "Los Olivos", "Santa Fe",
        "El Mirador", "La Cantera", "Jardines", "Puerta Real", "Las Huertas", "Villa del Sol"
    ];

    private static readonly string[] NombresOperador =
    [
        "Raúl Ibarra", "Martín Cisneros", "Jorge Valdés", "Sergio Robles", "Hugo Navarro", "Iván Duarte",
        "Óscar Lara", "Felipe Arriaga", "Ramón Tapia", "Emilio Paredes", "Diego Quiroz", "Javier Solís"
    ];

    private static readonly (string Numero, string Nombre, string Rol, int? Modulo)[] UsuariosDemo =
    [
        ("900001", "Demo Sistemas", Roles.Sistemas, null),
        ("900002", "Demo Jefe de Monitoreo", Roles.JefeMonitoreo, null),
        ("900003", "Demo Supervisor de Apoyo", Roles.SupervisorApoyo, null),
        ("900004", "Demo Supervisor de Operaciones", Roles.SupervisorOperaciones, null),
        ("900005", "Demo Monitorista Módulo 1", Roles.Monitorista, 1),
        ("900006", "Demo Monitorista Módulo 2", Roles.Monitorista, 2)
    ];

    public async Task SembrarAsync()
    {
        await db.Database.MigrateAsync();

        if (!await db.Clientes.AnyAsync())
        {
            await SembrarCatalogosAsync();
        }

        await SembrarUsuariosAsync();

        var hoy = reloj.Hoy();
        foreach (var dia in new[] { hoy.AddDays(-1), hoy, hoy.AddDays(1) })
        {
            await SembrarServiciosAsync(dia);
        }
    }

    private async Task SembrarCatalogosAsync()
    {
        var clientes = Clientes.ToDictionary(c => c.Codigo, c => new Cliente { Codigo = c.Codigo, Nombre = c.Nombre });

        var plantas = Plantas.Select(p => new Planta
        {
            Cliente = clientes[p.Cliente],
            Nombre = p.Planta,
            Municipio = p.Municipio
        }).ToList();

        for (var i = 0; i < plantas.Count; i++)
        {
            foreach (var nombre in NombresRuta.Skip(i * 2).Take(2))
            {
                plantas[i].Rutas.Add(new Ruta { NombreRuta = nombre, TipoServicio = TipoServicio.E });
                plantas[i].Rutas.Add(new Ruta { NombreRuta = nombre, TipoServicio = TipoServicio.S });
            }
        }

        db.Plantas.AddRange(plantas);
        db.Unidades.AddRange(Enumerable.Range(1, 40).Select(n => new Unidad { Numero = (1100 + n).ToString() }));
        db.Operadores.AddRange(NombresOperador.Select((nombre, i) => new Operador
        {
            Clave = $"OP-{i + 1:000}",
            Nombre = nombre,
            Supervisor = i % 2 == 0 ? "Supervisor Demo Norte" : "Supervisor Demo Sur",
            Coordinador = "Coordinación Demo"
        }));

        // Módulos fijos: cada uno con las dos plantas de un cliente.
        for (var m = 0; m < 3; m++)
        {
            db.Modulos.Add(new Modulo
            {
                Nombre = $"Módulo {m + 1}",
                Tipo = TipoModulo.Fijo,
                Plantas = plantas.Skip(m * 2).Take(2).Select(p => new ModuloPlanta { Planta = p }).ToList()
            });
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Datos de ejemplo: catálogos creados.");
    }

    private async Task SembrarUsuariosAsync()
    {
        var contrasena = config["DatosEjemplo:Contrasena"];
        if (string.IsNullOrWhiteSpace(contrasena))
        {
            logger.LogWarning("Datos de ejemplo: no se crearon usuarios porque falta 'DatosEjemplo:Contrasena' en user-secrets (ver README).");
            return;
        }

        foreach (var demo in UsuariosDemo)
        {
            if (await usuarios.FindByNameAsync(demo.Numero) is not null)
            {
                continue;
            }

            var usuario = new Usuario { NumeroEmpleado = demo.Numero, Nombre = demo.Nombre };
            var resultado = await usuarios.CreateAsync(usuario, contrasena);
            if (!resultado.Succeeded)
            {
                // Por ejemplo, la contraseña de user-secrets no cumple la política (10+ caracteres, etc.).
                logger.LogError("Datos de ejemplo: no se pudo crear {Numero}: {Errores}",
                    demo.Numero, string.Join(" ", resultado.Errors.Select(e => e.Description)));
                return;
            }

            await usuarios.AddToRoleAsync(usuario, demo.Rol);

            if (demo.Modulo is { } numeroModulo)
            {
                var modulo = await db.Modulos.SingleAsync(m => m.Nombre == $"Módulo {numeroModulo}");
                db.ModulosUsuario.Add(new ModuloUsuario { Modulo = modulo, Usuario = usuario, FechaInicio = reloj.Hoy().AddDays(-30) });
                await db.SaveChangesAsync();
            }
        }
    }

    /// <summary>
    /// Por cada turno de la planta: un servicio de entrada que llega 10 min antes del cambio y uno
    /// de salida que sale 5 min después. La salida del turno nocturno cruza la medianoche.
    /// </summary>
    private async Task SembrarServiciosAsync(DateOnly dia)
    {
        if (await db.Servicios.AnyAsync(s => s.Fecha == dia))
        {
            return;
        }

        var rutas = await db.Rutas.Include(r => r.Planta).OrderBy(r => r.Id).ToListAsync();
        var unidades = await db.Unidades.OrderBy(u => u.Id).Select(u => u.Id).ToListAsync();
        var operadores = await db.Operadores.OrderBy(o => o.Id).Select(o => o.Id).ToListAsync();
        var horaTurnoPorPlanta = Plantas.ToDictionary(p => p.Planta, p => p.HoraTurno);
        var contador = 0;

        foreach (var ruta in rutas)
        {
            var primerTurno = horaTurnoPorPlanta.GetValueOrDefault(ruta.Planta!.Nombre, 7);

            foreach (var cambio in new[] { primerTurno, primerTurno + 8, primerTurno + 16 })
            {
                var cambioDeTurno = new TimeOnly(cambio % 24, 0);
                var (hip, htp) = ruta.TipoServicio == TipoServicio.E
                    ? (cambioDeTurno.AddMinutes(-90), cambioDeTurno.AddMinutes(-10))
                    : (cambioDeTurno.AddMinutes(5), cambioDeTurno.AddMinutes(75));

                var servicio = new Servicio
                {
                    Ruta = ruta,
                    Fecha = dia,
                    Hip = hip,
                    Htp = htp,
                    Turno = turnos.TurnoDe(hip),
                    UnidadTitularId = unidades[contador % unidades.Count],
                    OperadorId = operadores[contador % operadores.Count]
                };

                // Los filtros solo existen para servicios de entrada.
                if (ruta.TipoServicio == TipoServicio.E)
                {
                    servicio.Filtros = filtros.HorasProgramadas(dia, hip)
                        .Select(f => new Filtro { NumeroFiltro = f.NumeroFiltro, HoraProgramada = f.HoraProgramada })
                        .ToList();
                }

                db.Servicios.Add(servicio);
                contador++;
            }
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Datos de ejemplo: {Cantidad} servicios creados para el {Dia}.", contador, dia);
    }
}
