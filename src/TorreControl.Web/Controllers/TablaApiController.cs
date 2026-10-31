using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TorreControl.Web.Data;
using TorreControl.Web.Models.ViewModels;
using TorreControl.Web.Security;
using TorreControl.Web.Services;

namespace TorreControl.Web.Controllers;

/// <summary>Datos de la tabla del monitorista para un día y un turno.</summary>
[ApiController]
[Route("api/tabla")]
public class TablaApiController(
    ConsultaTabla consulta,
    AccesoPlantas acceso,
    VentanaServicios ventanas,
    ReglasTurno turnos,
    RelojNegocio reloj,
    IOptions<ReglasNegocioOptions> reglas) : ControllerBase
{
    /// <param name="dia">yyyy-MM-dd (por defecto, hoy).</param>
    /// <param name="turno">1, 2, 3, o 0 = día completo (por defecto, el turno en curso).</param>
    [HttpGet]
    // Nunca se guarda en caché: el estado de cada servicio depende de la hora exacta.
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Obtener(string? dia, string? turno)
    {
        var ahora = reloj.Ahora();
        var turnoActual = turnos.TurnoDe(TimeOnly.FromDateTime(ahora));

        // Validación en el servidor: formato estricto, nada de "lo que el navegador mande".
        var d = DateOnly.FromDateTime(ahora);
        if (!string.IsNullOrWhiteSpace(dia)
            && !DateOnly.TryParseExact(dia, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
        {
            return Problem("Día con formato inválido. Usa yyyy-MM-dd.", statusCode: 400);
        }

        var t = turnoActual;
        if (!string.IsNullOrWhiteSpace(turno) && (!int.TryParse(turno, out t) || t is < 0 or > 3))
        {
            return Problem("Turno inválido. Usa 1, 2, 3 o 0 (día completo).", statusCode: 400);
        }

        var (desde, hasta) = ventanas.VentanaDeTurno(d, t);
        var grupos = await consulta.GruposAsync(await acceso.PlantasPermitidasAsync(User), d, t);

        return Ok(new TablaRespuesta(
            reloj.AhoraUtc(),
            turnoActual,
            new VentanaDto(d.ToString("yyyy-MM-dd"), t, desde.ToString("HH:mm"), hasta.ToString("HH:mm")),
            new ReglasVistaDto(reglas.Value.ToleranciaMin),
            grupos));
    }
}
