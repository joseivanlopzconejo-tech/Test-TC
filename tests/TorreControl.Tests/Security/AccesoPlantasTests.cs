using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using TorreControl.Web.Models;
using TorreControl.Web.Security;
using TorreControl.Web.Services;

namespace TorreControl.Tests.Security;

/// <summary>Pruebas anti-IDOR contra una base real: quién ve qué planta.</summary>
public class AccesoPlantasTests(BaseDatosPrueba bd) : IClassFixture<BaseDatosPrueba>, IAsyncLifetime
{
    private readonly IAuthorizationService _autorizacion = new ServiceCollection()
        .AddLogging()
        .AddAuthorization(Politicas.Configurar)
        .BuildServiceProvider()
        .GetRequiredService<IAuthorizationService>();

    private int _monitoristaA, _monitoristaB, _sinModulo, _plantaA1, _plantaA2, _plantaB;

    // Hoy en las pruebas = 10/03/2026 en CDMX (ver BaseDatosPrueba.Reloj).
    // xUnit crea una instancia por prueba y todas comparten la misma base:
    // cada una arma sus propios datos con un sufijo único para no chocar.
    public async Task InitializeAsync()
    {
        var x = Guid.NewGuid().ToString("N")[..8];
        await using var db = bd.CrearContexto();

        var cliente = new Cliente { Codigo = $"ACC-{x}", Nombre = "Cliente acceso" };
        var a1 = new Planta { Cliente = cliente, Nombre = "A1", Municipio = "Silao" };
        var a2 = new Planta { Cliente = cliente, Nombre = "A2", Municipio = "León" };
        var b = new Planta { Cliente = cliente, Nombre = "B", Municipio = "Irapuato" };
        var usuarioA = new Usuario { NumeroEmpleado = $"A-{x}", Nombre = "Monitorista A" };
        var usuarioB = new Usuario { NumeroEmpleado = $"B-{x}", Nombre = "Monitorista B" };
        var usuarioSinModulo = new Usuario { NumeroEmpleado = $"C-{x}", Nombre = "Sin módulo" };

        var moduloA = new Modulo { Nombre = $"Módulo A {x}", Tipo = TipoModulo.Fijo, Plantas = [new() { Planta = a1 }, new() { Planta = a2 }] };
        var moduloB = new Modulo { Nombre = $"Módulo B {x}", Tipo = TipoModulo.Fijo, Plantas = [new() { Planta = b }] };
        var guardiaVencida = new Modulo { Nombre = $"Guardia vencida {x}", Tipo = TipoModulo.Guardia, Plantas = [new() { Planta = b }] };

        db.AddRange(
            new ModuloUsuario { Modulo = moduloA, Usuario = usuarioA, FechaInicio = new(2026, 1, 1) },
            new ModuloUsuario { Modulo = moduloB, Usuario = usuarioB, FechaInicio = new(2026, 1, 1) },
            // A tuvo una guardia con la planta B el fin de semana pasado: ya no debe verla.
            new ModuloUsuario { Modulo = guardiaVencida, Usuario = usuarioA, FechaInicio = new(2026, 3, 7), FechaFin = new(2026, 3, 8) });
        db.Add(usuarioSinModulo);
        await db.SaveChangesAsync();

        (_monitoristaA, _monitoristaB, _sinModulo) = (usuarioA.Id, usuarioB.Id, usuarioSinModulo.Id);
        (_plantaA1, _plantaA2, _plantaB) = (a1.Id, a2.Id, b.Id);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<HashSet<int>> PlantasDe(int usuarioId, params string[] roles)
    {
        await using var db = bd.CrearContexto();
        var acceso = new AccesoPlantas(db, _autorizacion, new RelojNegocio(bd.Reloj, Configuracion.Opciones()));
        return await acceso.PlantasPermitidasAsync(Usuarios.ConRoles(usuarioId, roles));
    }

    [Fact]
    public async Task Monitorista_SoloVeLasPlantasDeSuModulo()
    {
        var plantas = await PlantasDe(_monitoristaA, Roles.Monitorista);

        Assert.Equal([_plantaA1, _plantaA2], plantas.Order());
    }

    [Fact]
    public async Task Monitorista_NoVeLaPlantaDeOtroModuloNiDeUnaGuardiaVencida()
    {
        Assert.DoesNotContain(_plantaB, await PlantasDe(_monitoristaA, Roles.Monitorista));
        Assert.Equal([_plantaB], await PlantasDe(_monitoristaB, Roles.Monitorista));
    }

    [Fact]
    public async Task MonitoristaSinModulo_NoVeNada()
    {
        Assert.Empty(await PlantasDe(_sinModulo, Roles.Monitorista));
    }

    [Theory]
    [InlineData(Roles.SupervisorApoyo)]
    [InlineData(Roles.JefeMonitoreo)]
    public async Task Supervisores_VenTodasLasPlantas(string rol)
    {
        var plantas = await PlantasDe(_sinModulo, rol);

        Assert.Superset(new HashSet<int> { _plantaA1, _plantaA2, _plantaB }, plantas);
    }
}
