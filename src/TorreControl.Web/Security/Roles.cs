namespace TorreControl.Web.Security;

/// <summary>
/// Los roles del sistema y su jerarquía. El nivel sirve para una regla de seguridad clave:
/// nadie puede dar un rol igual o superior al suyo (así un Jefe no puede crear otro Jefe
/// ni hacerse Sistemas).
/// </summary>
public static class Roles
{
    public const string Monitorista = "Monitorista";
    public const string SupervisorApoyo = "SupervisorApoyo";
    public const string SupervisorOperaciones = "SupervisorOperaciones";
    public const string JefeMonitoreo = "JefeMonitoreo";
    public const string Sistemas = "Sistemas";

    public static readonly IReadOnlyList<string> Todos =
        [Monitorista, SupervisorApoyo, SupervisorOperaciones, JefeMonitoreo, Sistemas];

    // Los dos supervisores tienen los mismos permisos, por eso comparten nivel.
    public static int Nivel(string rol) => rol switch
    {
        Monitorista => 1,
        SupervisorApoyo or SupervisorOperaciones => 2,
        JefeMonitoreo => 3,
        Sistemas => 4,
        _ => 0 // rol desconocido: no vale nada
    };

    public static string NombreVisible(string rol) => rol switch
    {
        SupervisorApoyo => "Supervisor de Apoyo",
        SupervisorOperaciones => "Supervisor de Operaciones",
        JefeMonitoreo => "Jefe de Monitoreo",
        _ => rol
    };

    /// <summary>Solo se pueden asignar roles ESTRICTAMENTE menores al rol más alto que uno tiene.</summary>
    public static bool PuedeAsignar(IEnumerable<string> rolesPropios, string rolNuevo)
    {
        var nivelPropio = rolesPropios.Select(Nivel).DefaultIfEmpty(0).Max();
        var nivelNuevo = Nivel(rolNuevo);
        return nivelNuevo > 0 && nivelNuevo < nivelPropio;
    }
}
