using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using TorreControl.Web.Data;
using TorreControl.Web.Models.ViewModels;
using TorreControl.Web.Security;
using TorreControl.Web.Services;

namespace TorreControl.Web.Controllers;

/// <summary>Datos de la tabla del monitorista para una ventana de horas.</summary>
[ApiController]
[Route("api/tabla")]
public class TablaApiController(
    ConsultaTabla consulta,
    AccesoPlantas acceso,
    VentanaServicios ventanas,
    ReglasTurno turnos,
    RelojNegocio reloj) : ControllerBase
{
    /// <param name="dia">yyyy-MM-dd (por defecto, hoy).</param>
    /// <param name="desde">HH:mm en 24 h (por defecto, inicio del turno actual).</param>
    /// <param name="hasta">HH:mm en 24 h (por defecto, fin del turno actual).</param>
    [HttpGet]
    // Nunca se guarda en caché: el estado de cada servicio depende de la hora exacta.
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Obtener(string? dia, string? desde, string? hasta)
    {
        var ahora = reloj.Ahora();
        var (diaTurno, desdeTurno, hastaTurno) = ventanas.VentanaDelTurnoActual(ahora);

        // Validación en el servidor: formato estricto, nada de "lo que el navegador mande".
        if (!Leer(dia, "yyyy-MM-dd", diaTurno, DateOnly.TryParseExact, out var d)
            || !Leer(desde, "HH:mm", desdeTurno, TimeOnly.TryParseExact, out var h1)
            || !Leer(hasta, "HH:mm", hastaTurno, TimeOnly.TryParseExact, out var h2))
        {
            return Problem("Día u horas con formato inválido. Usa yyyy-MM-dd y HH:mm (24 h).", statusCode: 400);
        }

        var (ventanaDesde, ventanaHasta) = VentanaServicios.Ventana(d, h1, h2);
        var plantas = await consulta.PlantasAsync(await acceso.PlantasPermitidasAsync(User), ventanaDesde, ventanaHasta);

        return Ok(new TablaRespuesta(
            reloj.AhoraUtc(),
            turnos.TurnoDe(TimeOnly.FromDateTime(ahora)),
            new VentanaDto(d.ToString("yyyy-MM-dd"), h1.ToString("HH:mm"), h2.ToString("HH:mm"), turnos.TurnoDe(h1)),
            plantas));
    }

    private delegate bool Parser<T>(string texto, string formato, IFormatProvider proveedor, DateTimeStyles estilo, out T valor);

    private static bool Leer<T>(string? texto, string formato, T porDefecto, Parser<T> parser, out T valor)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            valor = porDefecto;
            return true;
        }

        return parser(texto, formato, CultureInfo.InvariantCulture, DateTimeStyles.None, out valor);
    }
}
