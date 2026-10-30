using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using TorreControl.Web.Security;

namespace TorreControl.Tests.Security;

public class PoliticasTests
{
    private readonly IAuthorizationService _autorizacion = new ServiceCollection()
        .AddLogging()
        .AddAuthorization(Politicas.Configurar)
        .BuildServiceProvider()
        .GetRequiredService<IAuthorizationService>();

    private async Task<bool> Permite(string politica, string rol) =>
        (await _autorizacion.AuthorizeAsync(Usuarios.ConRoles(1, rol), politica)).Succeeded;

    [Theory]
    [InlineData(Roles.Monitorista, false)]
    [InlineData(Roles.SupervisorApoyo, true)]
    [InlineData(Roles.SupervisorOperaciones, true)]
    [InlineData(Roles.JefeMonitoreo, true)]
    [InlineData(Roles.Sistemas, true)]
    public async Task AgregarServicio_SoloSupervisoresEnAdelante(string rol, bool esperado)
    {
        Assert.Equal(esperado, await Permite(Politicas.AgregarServicio, rol));
    }

    [Theory]
    [InlineData(Roles.Monitorista, false)]
    [InlineData(Roles.SupervisorOperaciones, false)]
    [InlineData(Roles.JefeMonitoreo, true)]
    [InlineData(Roles.Sistemas, true)]
    public async Task AdministrarMonitoreo_SoloJefeYSistemas(string rol, bool esperado)
    {
        Assert.Equal(esperado, await Permite(Politicas.AdministrarMonitoreo, rol));
    }

    [Theory]
    [InlineData(Roles.JefeMonitoreo, false)]
    [InlineData(Roles.Sistemas, true)]
    public async Task AdministrarSistema_SoloSistemas(string rol, bool esperado)
    {
        Assert.Equal(esperado, await Permite(Politicas.AdministrarSistema, rol));
    }

    [Fact]
    public async Task PoliticaPorDefecto_RechazaAnonimos()
    {
        var opciones = new AuthorizationOptions();
        Politicas.Configurar(opciones);

        var resultado = await _autorizacion.AuthorizeAsync(Usuarios.Anonimo(), opciones.FallbackPolicy!);

        Assert.False(resultado.Succeeded);
    }
}
