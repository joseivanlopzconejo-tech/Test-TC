using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TorreControl.Web.Data;
using TorreControl.Web.Models.ViewModels;
using TorreControl.Web.Security;

namespace TorreControl.Web.Controllers;

/// <summary>
/// Acciones sobre servicios. Toda acción que recibe un id revisa primero que la planta sea del
/// usuario (anti-IDOR); los POST además exigen el token antiforgery (filtro global).
/// </summary>
[ApiController]
[Route("api/servicios")]
public class ServiciosApiController(AppDbContext db, AccesoPlantas acceso) : ControllerBase
{
    // Tope de servicios por petición: suficiente para una planta completa y evita abusos.
    private const int MaxIdsPorPeticion = 200;

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detalle(int id)
    {
        var servicio = await db.Servicios
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new
            {
                s.Id,
                s.Ruta!.PlantaId,
                Planta = s.Ruta.Planta!.Nombre,
                Ruta = s.Ruta.NombreRuta,
                Tipo = s.Ruta.TipoServicio.ToString(),
                s.Fecha,
                s.Turno,
                s.Hip,
                s.Htp,
                s.EsMixto
            })
            .SingleOrDefaultAsync();

        // "No existe" y "no es tuyo" responden igual (404): así nadie puede ir probando ids
        // para descubrir qué servicios existen en plantas ajenas.
        if (servicio is null || !await acceso.PuedeVerPlantaAsync(User, servicio.PlantaId))
        {
            return NotFound();
        }

        return Ok(new
        {
            servicio.Id,
            servicio.Planta,
            servicio.Ruta,
            servicio.Tipo,
            Fecha = servicio.Fecha.ToString("yyyy-MM-dd"),
            servicio.Turno,
            Hip = servicio.Hip.ToString("HH:mm"),
            Htp = servicio.Htp.ToString("HH:mm"),
            servicio.EsMixto
        });
    }

    /// <summary>
    /// Marca uno o varios servicios como "no operó hoy" (o los reactiva). La Auditoría guarda
    /// quién y cuándo, por eso no hace falta una columna extra.
    /// </summary>
    [HttpPost("no-opero")]
    public async Task<IActionResult> NoOpero(NoOperoPeticion peticion)
    {
        var ids = peticion.Ids.Distinct().ToList();
        if (ids.Count is 0 or > MaxIdsPorPeticion)
        {
            return Problem($"Envía entre 1 y {MaxIdsPorPeticion} servicios.", statusCode: 400);
        }

        var servicios = await db.Servicios
            .Include(s => s.Ruta)
            .Where(s => ids.Contains(s.Id))
            .ToListAsync();

        // Todo o nada: si alguno no existe o es de una planta ajena, no se toca ninguno y se
        // responde igual que "no existe" (no se revela cuál falló).
        var permitidas = await acceso.PlantasPermitidasAsync(User);
        if (servicios.Count != ids.Count || servicios.Any(s => !permitidas.Contains(s.Ruta!.PlantaId)))
        {
            return NotFound();
        }

        servicios.ForEach(s => s.NoOpero = peticion.NoOpero);
        await db.SaveChangesAsync();

        return Ok(new { actualizados = servicios.Count });
    }
}
