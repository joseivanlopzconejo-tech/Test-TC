using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TorreControl.Web.Data;
using TorreControl.Web.Services;

namespace TorreControl.Web.Security;

/// <summary>
/// Autorización POR RECURSO: además del rol, revisa que la planta sea de un módulo del usuario.
/// Esto evita el IDOR: un monitorista que cambia el id en la URL o en la petición no puede ver
/// ni editar servicios de otra planta. Todo controlador que reciba un id debe pasar por aquí.
/// </summary>
public class AccesoPlantas(AppDbContext db, IAuthorizationService autorizacion, RelojNegocio reloj)
{
    /// <summary>Ids de las plantas que este usuario puede ver hoy.</summary>
    public async Task<HashSet<int>> PlantasPermitidasAsync(ClaimsPrincipal usuario)
    {
        if ((await autorizacion.AuthorizeAsync(usuario, Politicas.VerTodasLasPlantas)).Succeeded)
        {
            return (await db.Plantas.Select(p => p.Id).ToListAsync()).ToHashSet();
        }

        if (!int.TryParse(usuario.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
        {
            return [];
        }

        var plantas = await db.ModulosUsuario
            .Where(mu => mu.UsuarioId == usuarioId)
            .Where(ReglasAcceso.AsignacionVigente(reloj.Hoy()))
            .SelectMany(mu => mu.Modulo!.Plantas.Select(mp => mp.PlantaId))
            .Distinct()
            .ToListAsync();

        return plantas.ToHashSet();
    }

    public async Task<bool> PuedeVerPlantaAsync(ClaimsPrincipal usuario, int plantaId) =>
        (await PlantasPermitidasAsync(usuario)).Contains(plantaId);
}
