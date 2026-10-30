using Microsoft.AspNetCore.Authorization;

namespace TorreControl.Web.Security;

/// <summary>
/// Políticas de autorización: qué rol puede hacer qué. Se revisan EN EL SERVIDOR en cada
/// petición; ocultar un botón en la página no cuenta como seguridad.
/// </summary>
public static class Politicas
{
    /// <summary>Ver todas las plantas sin depender de un módulo (supervisores en adelante).</summary>
    public const string VerTodasLasPlantas = nameof(VerTodasLasPlantas);

    /// <summary>Agregar servicios especiales y armar perfiles de guardia.</summary>
    public const string AgregarServicio = nameof(AgregarServicio);

    /// <summary>Usuarios, módulos, operadores, horarios y el panel de calificación.</summary>
    public const string AdministrarMonitoreo = nameof(AdministrarMonitoreo);

    /// <summary>Catálogos y configuración del sistema.</summary>
    public const string AdministrarSistema = nameof(AdministrarSistema);

    private static readonly string[] Supervisores =
        [Roles.SupervisorApoyo, Roles.SupervisorOperaciones, Roles.JefeMonitoreo, Roles.Sistemas];

    public static void Configurar(AuthorizationOptions opciones)
    {
        // Política "por defecto": TODA página exige sesión iniciada, salvo las marcadas con
        // [AllowAnonymous] (login, error). Si alguien olvida proteger una página nueva, ya está protegida.
        opciones.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

        opciones.AddPolicy(VerTodasLasPlantas, p => p.RequireRole(Supervisores));
        opciones.AddPolicy(AgregarServicio, p => p.RequireRole(Supervisores));
        opciones.AddPolicy(AdministrarMonitoreo, p => p.RequireRole(Roles.JefeMonitoreo, Roles.Sistemas));
        opciones.AddPolicy(AdministrarSistema, p => p.RequireRole(Roles.Sistemas));
    }
}
