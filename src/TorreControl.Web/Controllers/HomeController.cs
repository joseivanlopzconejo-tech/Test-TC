using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TorreControl.Web.Data;
using TorreControl.Web.Models;
using TorreControl.Web.Models.ViewModels;
using TorreControl.Web.Security;
using TorreControl.Web.Services;

namespace TorreControl.Web.Controllers;

public class HomeController(AppDbContext db, AccesoPlantas acceso, RelojNegocio reloj) : Controller
{
    public async Task<IActionResult> Index()
    {
        var hoy = reloj.Hoy();
        var permitidas = await acceso.PlantasPermitidasAsync(User);

        var plantas = await db.Plantas
            .AsNoTracking()
            .Where(p => permitidas.Contains(p.Id))
            .OrderBy(p => p.Cliente!.Nombre).ThenBy(p => p.Nombre)
            .Select(p => new InicioViewModel.PlantaResumen(
                p.Cliente!.Nombre,
                p.Nombre,
                p.Municipio,
                db.Servicios.Count(s => s.Ruta!.PlantaId == p.Id && s.Fecha == hoy)))
            .ToListAsync();

        return View(new InicioViewModel { Hoy = hoy, Plantas = plantas });
    }

    // La página de error debe verse aunque la sesión haya expirado.
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
