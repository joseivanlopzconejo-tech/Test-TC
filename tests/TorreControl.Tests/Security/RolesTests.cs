using TorreControl.Web.Security;

namespace TorreControl.Tests.Security;

public class RolesTests
{
    [Theory]
    [InlineData(Roles.Sistemas, Roles.JefeMonitoreo, true)]
    [InlineData(Roles.JefeMonitoreo, Roles.SupervisorApoyo, true)]
    [InlineData(Roles.JefeMonitoreo, Roles.Monitorista, true)]
    [InlineData(Roles.SupervisorApoyo, Roles.Monitorista, true)]
    [InlineData(Roles.JefeMonitoreo, Roles.JefeMonitoreo, false)]          // igual: no
    [InlineData(Roles.JefeMonitoreo, Roles.Sistemas, false)]               // superior: no
    [InlineData(Roles.SupervisorApoyo, Roles.SupervisorOperaciones, false)] // mismo nivel: no
    [InlineData(Roles.Monitorista, Roles.Monitorista, false)]
    [InlineData(Roles.Sistemas, Roles.Sistemas, false)]
    public void PuedeAsignar_SoloRolesEstrictamenteMenores(string propio, string nuevo, bool esperado)
    {
        Assert.Equal(esperado, Roles.PuedeAsignar([propio], nuevo));
    }

    [Fact]
    public void PuedeAsignar_RolInventado_NuncaSePuede()
    {
        Assert.False(Roles.PuedeAsignar([Roles.Sistemas], "SuperAdmin"));
    }

    [Fact]
    public void PuedeAsignar_SinRoles_NoPuedeNada()
    {
        Assert.False(Roles.PuedeAsignar([], Roles.Monitorista));
    }
}
