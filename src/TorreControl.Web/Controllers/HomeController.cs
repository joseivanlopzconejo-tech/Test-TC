using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TorreControl.Web.Models;

namespace TorreControl.Web.Controllers;

public class HomeController : Controller
{
    /// <summary>
    /// La tabla del monitorista. La página solo trae la estructura; los datos llegan de
    /// /api/tabla (ya filtrados por permisos) y se actualizan solos.
    /// </summary>
    public IActionResult Index() => View();

    // La página de error debe verse aunque la sesión haya expirado.
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
