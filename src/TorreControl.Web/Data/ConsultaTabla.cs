using Microsoft.EntityFrameworkCore;
using TorreControl.Web.Models;
using TorreControl.Web.Models.ViewModels;
using TorreControl.Web.Services;

namespace TorreControl.Web.Data;

/// <summary>
/// Arma los datos de la tabla del monitorista: lee de la base y aplica las reglas puras
/// (ventana de turno, estatus, diferencias). Se calcula en cada petición, sin caché, para que
/// nadie vea un estado viejo (el bug del sistema actual al cambiar de turno).
/// </summary>
public class ConsultaTabla(AppDbContext db, VentanaServicios ventanas)
{
    private static readonly int[] Turnos = [1, 2, 3, 0];

    public async Task<List<GrupoTablaDto>> GruposAsync(IReadOnlySet<int> plantasPermitidas, DateOnly dia, int turno)
    {
        // Se piden ayer, hoy y mañana y luego la regla de la ventana decide; así entran los que
        // cruzan la medianoche, los de precarga de madrugada y las llegadas cuya Htr aún se captura.
        var primerDia = dia.AddDays(-1);
        var ultimoDia = dia.AddDays(1);

        var plantas = await db.Plantas
            .AsNoTracking()
            .Where(p => plantasPermitidas.Contains(p.Id))
            .Include(p => p.Cliente)
            .OrderBy(p => p.Cliente!.Nombre).ThenBy(p => p.Nombre)
            .ToListAsync();

        var servicios = await db.Servicios
            .AsNoTracking()
            .AsSplitQuery()
            .Where(s => plantasPermitidas.Contains(s.Ruta!.PlantaId) && s.Fecha >= primerDia && s.Fecha <= ultimoDia)
            .Include(s => s.Ruta)
            .Include(s => s.UnidadTitular)
            .Include(s => s.UnidadesApoyo).ThenInclude(a => a.Unidad)
            .Include(s => s.Operador)
            .Include(s => s.Filtros).ThenInclude(f => f.Estatus)
            .Include(s => s.Filtros).ThenInclude(f => f.Modificaciones).ThenInclude(m => m.Estatus)
            .Include(s => s.Incidencias)
            .ToListAsync();

        // Para cada turno (y el día completo), qué servicios le tocan.
        var porTurno = Turnos.ToDictionary(
            t => t,
            t =>
            {
                var (desde, hasta) = ventanas.VentanaDeTurno(dia, t);
                return servicios
                    .Where(s => ventanas.SeMuestra(s.Fecha, s.Hip, s.Htp, s.Ruta!.TipoServicio, desde, hasta))
                    .ToList();
            });

        var grupos = new List<GrupoTablaDto>();
        foreach (var planta in plantas)
        {
            foreach (var esMixto in new[] { false, true })
            {
                bool DelGrupo(Servicio s) => s.Ruta!.PlantaId == planta.Id && s.EsMixto == esMixto;

                // La pestaña MIXTO solo aparece si la planta tiene mixtos ese día.
                if (esMixto && !porTurno[0].Any(DelGrupo))
                {
                    continue;
                }

                // El contador cuenta lo que opera: lo marcado como "no operó" no suma.
                var conteos = Turnos.ToDictionary(t => t.ToString(), t => porTurno[t].Count(s => DelGrupo(s) && !s.NoOpero));

                var filas = porTurno[turno]
                    .Where(DelGrupo)
                    .OrderBy(s => s.Fecha.ToDateTime(s.Hip))
                    .ThenBy(s => s.Ruta!.NombreRuta)
                    .Select(Fila)
                    .ToList();

                var nombreCorto = string.IsNullOrWhiteSpace(planta.NombreCorto) ? planta.Nombre : planta.NombreCorto;
                grupos.Add(new GrupoTablaDto(
                    esMixto ? $"{planta.Id}-M" : planta.Id.ToString(),
                    planta.Id,
                    esMixto,
                    nombreCorto,
                    $"{planta.Cliente!.Nombre} · {planta.Nombre}",
                    planta.Cliente.Nombre,
                    planta.Municipio,
                    conteos,
                    filas));
            }
        }

        return grupos;
    }

    private static FilaServicioDto Fila(Servicio s)
    {
        var (salida, llegada) = ReglasTurno.Intervalo(s.Fecha, s.Hip, s.Htp);

        // Siempre primero el inicio y luego la llegada (sección 5.5).
        var incidencias = s.Incidencias.OrderBy(i => i.Momento).Select(i => i.Texto).ToList();

        // Estatus de cada filtro capturado (el F4 usa su última modificación) → estatus del servicio.
        var filtrosCapturados = s.Filtros
            .Select(f => (f.NumeroFiltro, Estatus: UltimaModificacion(f)?.Estatus ?? f.Estatus))
            .Where(f => f.Estatus is not null)
            .ToDictionary(f => f.NumeroFiltro, f => f.Estatus!);
        var estatus = EstatusServicio.Calcular(s.Ruta!.TipoServicio, s.Hir, s.Htr, filtrosCapturados);

        return new FilaServicioDto(
            s.Id,
            s.Ruta.NombreRuta,
            s.Ruta.TipoServicio.ToString(),
            s.Fecha.ToString("yyyy-MM-dd"),
            s.Turno,
            s.Htp < s.Hip,
            s.EsEspecial,
            s.NoOpero,
            s.Hip.ToString("HH:mm"),
            s.Htp.ToString("HH:mm"),
            s.Hir?.ToString("HH:mm"),
            s.Htr?.ToString("HH:mm"),
            Diferencia(s.Hir, salida),
            Diferencia(s.Htr, llegada),
            estatus is null ? null : new EstatusDto(estatus.Nombre, estatus.Color),
            s.Aforo,
            s.UnidadTitular?.Numero,
            s.UnidadesApoyo.OrderBy(a => a.Orden).Select(a => a.Unidad!.Numero).ToList(),
            s.Operador is { } o ? new OperadorDto(o.Clave, o.Nombre, o.Supervisor, o.Coordinador) : null,
            s.Filtros.OrderBy(f => f.NumeroFiltro).Select(Filtro).ToList(),
            incidencias.Count > 0 ? string.Join(" ", incidencias) : null,
            s.Incidencias.Any(i => !i.EsSinNovedad));
    }

    // DifIni = Hir - Hip y DifFin = Htr - Htp, en minutos con signo (positivo = tarde).
    private static int? Diferencia(DateTime? real, DateTime programada) =>
        real is { } r ? (int)Math.Round((r - programada).TotalMinutes) : null;

    private static Modificacion? UltimaModificacion(Filtro f) =>
        f.Modificaciones.OrderByDescending(m => m.NumeroModificacion).FirstOrDefault();

    private static FiltroDto Filtro(Filtro f)
    {
        // El Filtro 4 muestra siempre su última modificación capturada.
        var ultima = UltimaModificacion(f);
        var estatus = ultima?.Estatus ?? f.Estatus;

        return new FiltroDto(
            f.NumeroFiltro,
            f.HoraProgramada.ToString("HH:mm"),
            estatus is null ? null : new EstatusDto(estatus.Nombre, estatus.Color),
            ultima?.Observacion ?? f.Observacion,
            f.Modificaciones.Count);
    }
}
