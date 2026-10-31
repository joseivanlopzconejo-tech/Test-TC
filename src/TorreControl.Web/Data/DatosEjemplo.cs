using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
    IOptions<ReglasNegocioOptions> reglas,
    IConfiguration config,
    ILogger<DatosEjemplo> logger)
{
    private static readonly (string Codigo, string Nombre)[] Clientes =
    [
        ("NVA", "Norvial Autopartes"),
        ("QTK", "Quimtek Industrial"),
        ("LMB", "Lumbra Electrodomésticos")
    ];

    // Hora de inicio del 1er turno de cada planta: unas cambian a las 7, 15 y 23 h y otras a las
    // 6, 14 y 22 h, como en la realidad. "Rutas" = rutas normales; "Mixtas" = rutas 12x12 (16:00
    // a 04:00) que el supervisor marca como MIXTO y van en su propia pestaña.
    private static readonly (string Cliente, string Planta, string Corto, string Municipio, int HoraTurno, int Rutas, int Mixtas)[] Plantas =
    [
        ("NVA", "Norvial Silao", "NORVIAL SLO", "Silao", 7, 15, 2),
        ("NVA", "Norvial Irapuato", "NORVIAL IRA", "Irapuato", 6, 13, 0),
        ("QTK", "Quimtek Salamanca", "QUIMTEK SAL", "Salamanca", 7, 14, 3),
        ("QTK", "Quimtek León", "QUIMTEK LEÓN", "León", 6, 12, 0),
        ("LMB", "Lumbra Celaya", "LUMBRA CEL", "Celaya", 7, 16, 2),
        ("LMB", "Lumbra Guanajuato", "LUMBRA GTO", "Guanajuato", 6, 12, 0)
    ];

    // Nombres de ruta inventados (colonias genéricas). Cada planta toma un tramo distinto.
    private static readonly string[] NombresRuta =
    [
        "Centro", "Las Arboledas", "San Nicolás", "Valle Verde", "Los Olivos", "Santa Fe", "El Mirador",
        "La Cantera", "Jardines", "Puerta Real", "Las Huertas", "Villa del Sol", "Los Pinos", "El Roble",
        "La Joya", "San Miguel", "Lomas del Río", "El Sauz", "La Presa", "Las Palmas", "Arroyo Seco",
        "Real del Bosque", "Los Ángeles", "El Carmen", "San Isidro", "La Loma", "Valle Dorado",
        "Campo Verde", "Las Flores", "El Llano", "Rancho Nuevo", "Tres Cruces", "La Estancia",
        "Buenavista", "El Encino", "Santa Rita", "Los Fresnos", "Monte Alto", "La Ermita", "Nueva Era",
        "El Pedregal", "Las Teresas", "San Pablo", "Los Laureles", "El Molino", "Prados del Sur"
    ];

    private static readonly string[] Nombres =
        ["Raúl", "Martín", "Jorge", "Sergio", "Hugo", "Iván", "Óscar", "Felipe", "Ramón", "Emilio", "Diego", "Javier"];

    private static readonly string[] Apellidos =
        ["Ibarra", "Cisneros", "Valdés", "Robles", "Navarro", "Duarte", "Lara", "Arriaga", "Tapia", "Paredes"];

    // Ids fijos del catálogo de estatus que viene en la migración.
    private const int Apagada = 1, Trayecto = 2, Encendida = 3, EnBase = 4, SinRespuesta = 8, PerdidaCobertura = 6;

    // Minutos de diferencia simulados: la mayoría a tiempo, algunos tarde y pocos muy tarde.
    private static readonly int[] Diferencias = [-4, -2, -1, 0, 0, 0, 1, 2, 3, 4, 5, 6, 8, 9, 12, 16, 24];

    private static readonly (int TipoId, string Tipo, string Motivo)[] Motivos =
    [
        (1, "Tráfico", "carga vehicular en el acceso a la autopista"),
        (1, "Tráfico", "cierre parcial por obra en el bulevar"),
        (6, "Cruce de tren", "tren detenido sobre la vía en el cruce"),
        (7, "Accidente vial", "choque en el carril de baja velocidad"),
        (2, "Falla mecánica", "revisión de frenos en el trayecto"),
        (4, "Cliente / planta", "la planta retrasó el acceso de la unidad")
    ];

    private static readonly (int Estatus, string Observacion)[][] CapturasFiltro =
    [
        // F1, F2, F3: lo típico conforme se acerca la hora de salida.
        [(Apagada, "Operador confirma unidad en base."), (Encendida, "Unidad encendida, operador revisando."), (SinRespuesta, "Sin respuesta del operador, se reintenta.")],
        [(Encendida, "Unidad encendida."), (EnBase, "Unidad lista en patio."), (PerdidaCobertura, "GPS sin reporte, se confirma por llamada.")],
        [(EnBase, "Lista para salir."), (Trayecto, "Operador en camino al primer punto.")],
        [(Trayecto, "Unidad en trayecto al primer punto.")]
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
            NombreCorto = p.Corto,
            Municipio = p.Municipio
        }).ToList();

        for (var i = 0; i < plantas.Count; i++)
        {
            var definicion = Plantas[i];
            var nombres = NombresRuta.Skip(i * 5).Concat(NombresRuta).Take(definicion.Rutas + definicion.Mixtas);
            foreach (var nombre in nombres)
            {
                plantas[i].Rutas.Add(new Ruta { NombreRuta = nombre, TipoServicio = TipoServicio.E });
                plantas[i].Rutas.Add(new Ruta { NombreRuta = nombre, TipoServicio = TipoServicio.S });
            }
        }

        db.Plantas.AddRange(plantas);
        db.Unidades.AddRange(Enumerable.Range(1, 150).Select(n => new Unidad { Numero = (1100 + n).ToString() }));
        db.Operadores.AddRange(Enumerable.Range(0, 40).Select(i => new Operador
        {
            Clave = $"OP-{i + 1:000}",
            Nombre = $"{Nombres[i % Nombres.Length]} {Apellidos[(i * 7) % Apellidos.Length]}",
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
            if (await usuarios.FindByNameAsync(demo.Numero) is { } existente)
            {
                await SincronizarContrasenaAsync(existente, contrasena);
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
    /// En desarrollo, la contraseña de user-secrets manda. Si cambió (por ejemplo, al reconstruir un
    /// Codespace se genera una nueva pero la base de datos se conserva), se actualiza en el usuario
    /// demo y se le quita cualquier bloqueo por intentos fallidos. Solo corre con datos de ejemplo.
    /// </summary>
    private async Task SincronizarContrasenaAsync(Usuario usuario, string contrasena)
    {
        if (await usuarios.CheckPasswordAsync(usuario, contrasena))
        {
            return;
        }

        await usuarios.RemovePasswordAsync(usuario);
        var resultado = await usuarios.AddPasswordAsync(usuario, contrasena);
        if (!resultado.Succeeded)
        {
            logger.LogError("Datos de ejemplo: no se pudo actualizar la contraseña de {Numero}: {Errores}",
                usuario.NumeroEmpleado, string.Join(" ", resultado.Errors.Select(e => e.Description)));
            return;
        }

        await usuarios.SetLockoutEndDateAsync(usuario, null);
        await usuarios.ResetAccessFailedCountAsync(usuario);
        logger.LogInformation("Datos de ejemplo: contraseña de {Numero} actualizada a la de user-secrets.", usuario.NumeroEmpleado);
    }

    /// <summary>
    /// Servicios de un día. Rutas normales: por cada turno de la planta en que corre, una entrada
    /// que llega ~10 min antes del cambio y una salida ~5 min después (la del turno nocturno cruza
    /// la medianoche). Rutas mixtas (12x12, de 16:00 a 04:00): entrada 14:30 y salida 04:05, marcadas
    /// como MIXTO.
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
        var definiciones = Plantas.ToDictionary(p => p.Planta);
        var capturista = (await usuarios.FindByNameAsync("900005"))?.Id;
        var ahora = reloj.Ahora();
        // Semilla fija por día: los datos simulados salen iguales cada vez que se regeneran.
        var azar = new Random(dia.DayNumber);
        var contador = 0;

        foreach (var grupo in rutas.GroupBy(r => r.Planta!.Nombre))
        {
            var definicion = definiciones[grupo.Key];
            var nombres = grupo.Select(r => r.NombreRuta).Distinct().ToList();

            for (var i = 0; i < nombres.Count; i++)
            {
                var entrada = grupo.Single(r => r.NombreRuta == nombres[i] && r.TipoServicio == TipoServicio.E);
                var salida = grupo.Single(r => r.NombreRuta == nombres[i] && r.TipoServicio == TipoServicio.S);
                // Cada ruta sale a una hora un poco distinta, como en la realidad (no todas a la vez).
                var desfase = (i % 7 - 3) * 5;
                var esMixta = i >= definicion.Rutas;

                if (esMixta)
                {
                    Agregar(entrada, new TimeOnly(14, 30).AddMinutes(desfase), 80, esMixto: true);
                    Agregar(salida, new TimeOnly(4, 5).AddMinutes(Math.Abs(desfase)), 70, esMixto: true);
                    continue;
                }

                // Todas corren el 1º y 2º turno de la planta; una de cada tres también el nocturno.
                var turnosDePlanta = i % 3 == 2 ? new[] { 0, 1, 2 } : [0, 1];
                foreach (var t in turnosDePlanta)
                {
                    var cambio = new TimeOnly((definicion.HoraTurno + t * 8) % 24, 0);
                    var fin = new TimeOnly((definicion.HoraTurno + t * 8 + 8) % 24, 0);
                    Agregar(entrada, cambio.AddMinutes(-90 + desfase), 80 - Math.Abs(desfase), esMixto: false);
                    Agregar(salida, fin.AddMinutes(5 + Math.Abs(desfase)), 70, esMixto: false);
                }
            }
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Datos de ejemplo: {Cantidad} servicios creados para el {Dia}.", contador, dia);

        void Agregar(Ruta ruta, TimeOnly hip, int duracionMin, bool esMixto)
        {
            var servicio = new Servicio
            {
                Ruta = ruta,
                Fecha = dia,
                Hip = hip,
                Htp = hip.AddMinutes(duracionMin),
                Turno = turnos.TurnoDe(hip),
                EsMixto = esMixto,
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

            SimularCapturas(servicio, ahora, azar, capturista, unidades, contador);
            db.Servicios.Add(servicio);
            contador++;
        }
    }

    /// <summary>
    /// Simula lo que un monitorista habría capturado en los servicios que ya pasaron, para que la
    /// tabla de ejemplo se vea como un turno real y no todo "Sin acción". Solo datos de ejemplo:
    /// las capturas reales pasarán por las reglas de negocio de la Fase 2.
    /// </summary>
    private void SimularCapturas(Servicio s, DateTime ahora, Random azar, int? capturista, List<int> unidades, int indice)
    {
        var (salida, llegada) = ReglasTurno.Intervalo(s.Fecha, s.Hip, s.Htp);

        // Uno de cada 30 servicios "no operó" ese día (para probar las filas ocultas).
        s.NoOpero = indice % 30 == 7;

        // Filtros ya vencidos: casi todos capturados; uno de cada ocho quedó "Sin acción".
        foreach (var filtro in s.Filtros.Where(f => f.HoraProgramada <= ahora))
        {
            if (azar.Next(8) == 0)
            {
                continue;
            }

            var opciones = CapturasFiltro[filtro.NumeroFiltro - 1];
            var (estatus, observacion) = opciones[azar.Next(opciones.Length)];
            var capturadoEn = filtro.HoraProgramada.AddMinutes(-azar.Next(1, 12));

            // El Filtro 4 se captura como "modificaciones" (necesitan un usuario que las firme).
            if (filtro.NumeroFiltro == CalculadoraFiltros.TotalFiltros && capturista is { } id)
            {
                filtro.Modificaciones.Add(new Modificacion { NumeroModificacion = 1, EstatusId = estatus, Observacion = observacion, CapturadoEn = capturadoEn, CapturadoPorId = id });
                if (azar.Next(3) == 0)
                {
                    filtro.Modificaciones.Add(new Modificacion { NumeroModificacion = 2, EstatusId = Trayecto, Observacion = "Unidad saliendo de la base.", CapturadoEn = capturadoEn.AddMinutes(4), CapturadoPorId = id });
                }
            }
            else
            {
                filtro.EstatusId = estatus;
                filtro.Observacion = observacion;
                filtro.CapturadoEn = capturadoEn;
                filtro.CapturadoPorId = capturista;
            }
        }

        // De vez en cuando, unidades de apoyo.
        if (indice % 11 == 3)
        {
            s.UnidadesApoyo.Add(new UnidadApoyo { UnidadId = unidades[(indice + 17) % unidades.Count], Orden = 1 });
            if (indice % 22 == 3)
            {
                s.UnidadesApoyo.Add(new UnidadApoyo { UnidadId = unidades[(indice + 41) % unidades.Count], Orden = 2 });
            }
        }

        if (s.NoOpero || salida > ahora)
        {
            return;
        }

        // La hora de inicio ya no lleva observación (decisión del 31/10/2026); el estatus pasa a
        // "En ruta" / "En salida" solo con tener Hir.
        s.Hir = salida.AddMinutes(Diferencias[azar.Next(Diferencias.Length)]);
        s.HirModificaciones = 1;

        if (llegada > ahora)
        {
            return;
        }

        var retrasoLlegada = Diferencias[azar.Next(Diferencias.Length)];
        s.Htr = llegada.AddMinutes(retrasoLlegada);
        s.HtrModificaciones = 1;
        s.Aforo = azar.Next(18, 46);
        if (Incidencia(retrasoLlegada, azar) is { } incidencia)
        {
            s.Incidencias.Add(incidencia);
        }
    }

    /// <summary>
    /// Solo una llegada con más de ToleranciaMin de retraso genera incidencia (motivo + observación).
    /// A tiempo no se escribe nada: la columna Observaciones queda limpia.
    /// </summary>
    private Incidencia? Incidencia(int minutos, Random azar)
    {
        if (minutos <= reglas.Value.ToleranciaMin)
        {
            return null;
        }

        var (tipoId, tipo, motivo) = Motivos[azar.Next(Motivos.Length)];
        return new Incidencia
        {
            Momento = MomentoIncidencia.Llegada,
            TipoIncidenciaId = tipoId,
            Motivo = motivo,
            Texto = TextosIncidencia.ConIncidente(MomentoIncidencia.Llegada, tarde: true, tipo, motivo)
        };
    }
}
