using System.Security.Claims;
using TorreControl.Web.Data;

namespace TorreControl.Web.Security;

/// <summary>Obtiene el usuario de la petición web en curso (la cookie de sesión ya validada).</summary>
public class UsuarioActualHttp(IHttpContextAccessor accesor) : IUsuarioActual
{
    public int? Id =>
        int.TryParse(accesor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
