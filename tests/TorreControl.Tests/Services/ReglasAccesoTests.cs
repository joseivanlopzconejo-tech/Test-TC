using TorreControl.Web.Models;
using TorreControl.Web.Services;

namespace TorreControl.Tests.Services;

public class ReglasAccesoTests
{
    private static readonly DateOnly Hoy = new(2026, 3, 14); // sábado

    private static bool Vigente(DateOnly inicio, DateOnly? fin, bool moduloActivo = true) =>
        ReglasAcceso.AsignacionVigente(Hoy).Compile()(new ModuloUsuario
        {
            FechaInicio = inicio,
            FechaFin = fin,
            Modulo = new Modulo { Activo = moduloActivo }
        });

    [Fact]
    public void SinFechaFin_EsVigenteDesdeSuInicio() => Assert.True(Vigente(new(2026, 1, 1), null));

    [Fact]
    public void GuardiaDeEsteFinDeSemana_EsVigente() => Assert.True(Vigente(new(2026, 3, 14), new(2026, 3, 15)));

    [Fact]
    public void GuardiaDelFinDeSemanaPasado_YaVencio() => Assert.False(Vigente(new(2026, 3, 7), new(2026, 3, 8)));

    [Fact]
    public void AsignacionQueEmpiezaManana_TodaviaNo() => Assert.False(Vigente(new(2026, 3, 15), null));

    [Fact]
    public void ModuloDesactivado_NoCuenta() => Assert.False(Vigente(new(2026, 1, 1), null, moduloActivo: false));
}
