using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TorreControl.Web.Data;
using TorreControl.Web.Security;
using TorreControl.Web.Services;

namespace TorreControl.Web.Controllers;

/// <summary>
/// Datos de servicios en JSON para la tabla del monitorista (la tabla llega en la Fase 2).
/// Toda acción que recibe un id revisa primero que la planta sea del usuario.
/// </summary>
[ApiController]
[Route("api/servicios")]
public class ServiciosApiController(AppDbContext db, AccesoPlantas acceso, ReglasTurno turnos) : ControllerBase
{
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
                s.Htp
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
            EsMixto = turnos.EsMixto(servicio.Fecha, servicio.Hip, servicio.Htp)
        });
    }
}
