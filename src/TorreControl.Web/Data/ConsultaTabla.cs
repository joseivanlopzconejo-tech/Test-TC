using Microsoft.EntityFrameworkCore;
using TorreControl.Web.Models;
using TorreControl.Web.Models.ViewModels;
using TorreControl.Web.Services;

namespace TorreControl.Web.Data;

/// <summary>
/// Arma los datos de la tabla del monitorista: lee de la base y aplica las reglas puras
/// (ventana, mixtos, diferencias). Se calcula en cada petición, sin caché, para que nadie vea
/// un estado viejo (el bug del sistema actual al cambiar de turno).
/// </summary>
public class ConsultaTabla(AppDbContext db, VentanaServicios ventanas, ReglasTurno turnos)
{
    public async Task<List<PlantaTablaDto>> PlantasAsync(IReadOnlySet<int> plantasPermitidas, DateTime ventanaDesde, DateTime ventanaHasta)
    {
        // Se piden unos días de más (ayer y mañana) y luego la regla de la ventana decide;
        // así entran los que cruzan la medianoche y los de precarga de madrugada.
        var primerDia = DateOnly.FromDateTime(ventanaDesde).AddDays(-1);
        var ultimoDia = DateOnly.FromDateTime(ventanaHasta).AddDays(1);

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
            .Include(s => s.Estatus)
            .Include(s => s.UnidadTitular)
            .Include(s => s.UnidadesApoyo).ThenInclude(a => a.Unidad)
            .Include(s => s.Operador)
            .Include(s => s.Filtros).ThenInclude(f => f.Estatus)
            .Include(s => s.Filtros).ThenInclude(f => f.Modificaciones).ThenInclude(m => m.Estatus)
            .Include(s => s.Incidencias)
            .ToListAsync();

        var visibles = servicios
            .Where(s => ventanas.SeMuestra(s.Fecha, s.Hip, s.Htp, s.Ruta!.TipoServicio, ventanaDesde, ventanaHasta))
            .OrderBy(s => s.Fecha.ToDateTime(s.Hip))
            .ThenBy(s => s.Ruta!.NombreRuta)
            .ToLookup(s => s.Ruta!.PlantaId);

        return plantas
            .Select(p => new PlantaTablaDto(p.Id, p.Nombre, p.Cliente!.Nombre, visibles[p.Id].Select(Fila).ToList()))
            .ToList();
    }

    private FilaServicioDto Fila(Servicio s)
    {
        var (salida, llegada) = ReglasTurno.Intervalo(s.Fecha, s.Hip, s.Htp);

        // Siempre primero el inicio y luego la llegada (sección 5.5).
        var incidencias = s.Incidencias.OrderBy(i => i.Momento).Select(i => i.Texto).ToList();

        return new FilaServicioDto(
            s.Id,
            s.Ruta!.NombreRuta,
            s.Ruta.TipoServicio.ToString(),
            s.Fecha.ToString("yyyy-MM-dd"),
            s.Turno,
            turnos.EsMixto(s.Fecha, s.Hip, s.Htp),
            s.Htp < s.Hip,
            s.Hip.ToString("HH:mm"),
            s.Htp.ToString("HH:mm"),
            s.Hir?.ToString("HH:mm"),
            s.Htr?.ToString("HH:mm"),
            Diferencia(s.Hir, salida),
            Diferencia(s.Htr, llegada),
            Estatus(s.Estatus),
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

    private static FiltroDto Filtro(Filtro f)
    {
        // El Filtro 4 muestra siempre su última modificación capturada.
        var ultima = f.Modificaciones.OrderByDescending(m => m.NumeroModificacion).FirstOrDefault();

        return new FiltroDto(
            f.NumeroFiltro,
            f.HoraProgramada.ToString("HH:mm"),
            Estatus(ultima?.Estatus ?? f.Estatus),
            ultima?.Observacion ?? f.Observacion,
            f.Modificaciones.Count);
    }

    private static EstatusDto? Estatus(EstatusUnidad? e) => e is null ? null : new EstatusDto(e.Nombre, e.Color);
}
