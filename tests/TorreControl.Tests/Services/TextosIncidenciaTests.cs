using TorreControl.Web.Models;
using TorreControl.Web.Services;

namespace TorreControl.Tests.Services;

public class TextosIncidenciaTests
{
    [Fact]
    public void SinNovedad_UsaLosTextosEstandar()
    {
        Assert.Equal("Inicio de ruta sin novedad, dentro del horario establecido.", TextosIncidencia.SinNovedad(MomentoIncidencia.Inicio));
        Assert.Equal("Arribo a planta sin novedad, dentro del tiempo establecido.", TextosIncidencia.SinNovedad(MomentoIncidencia.Llegada));
    }

    [Fact]
    public void InicioTardio_IncluyeTipoYMotivo()
    {
        Assert.Equal("Inicio tardío por tráfico: cierre parcial en la carretera federal.",
            TextosIncidencia.ConIncidente(MomentoIncidencia.Inicio, tarde: true, "Tráfico", "cierre parcial en la carretera federal"));
    }

    [Fact]
    public void ArriboTardio_NoDuplicaElPuntoFinal()
    {
        Assert.Equal("Arribo tardío a planta por falla mecánica: ponchadura.",
            TextosIncidencia.ConIncidente(MomentoIncidencia.Llegada, tarde: true, "Falla mecánica", " ponchadura. "));
    }

    [Fact]
    public void Adelanto_DiceAnticipado()
    {
        Assert.Equal("Inicio anticipado por cliente: la planta pidió salir antes.",
            TextosIncidencia.ConIncidente(MomentoIncidencia.Inicio, tarde: false, "Cliente", "la planta pidió salir antes"));
        Assert.StartsWith("Arribo anticipado a planta por",
            TextosIncidencia.ConIncidente(MomentoIncidencia.Llegada, tarde: false, "Otro", "x"));
    }
}
