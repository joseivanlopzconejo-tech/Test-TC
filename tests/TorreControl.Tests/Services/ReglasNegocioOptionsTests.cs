using System.ComponentModel.DataAnnotations;
using TorreControl.Web.Services;

namespace TorreControl.Tests.Services;

public class ReglasNegocioOptionsTests
{
    private static ReglasNegocioOptions LeerConfiguracionReal() => Configuracion.ReglasReales();

    // Mismo validador que usa la app al arrancar (DataAnnotations + IValidatableObject).
    private static List<ValidationResult> Validar(ReglasNegocioOptions opciones)
    {
        var errores = new List<ValidationResult>();
        Validator.TryValidateObject(opciones, new ValidationContext(opciones), errores, validateAllProperties: true);
        return errores;
    }

    [Fact]
    public void ConfiguracionReal_EsValida()
    {
        var errores = Validar(LeerConfiguracionReal());

        Assert.Empty(errores);
    }

    [Fact]
    public void ConfiguracionReal_TieneLosValoresDeLaEspecificacion()
    {
        var o = LeerConfiguracionReal();

        Assert.Equal("America/Mexico_City", o.ZonaHoraria);
        Assert.Equal(20, o.FiltroIntervaloMin);
        Assert.Equal(5, o.FiltroAdvertenciaMin);
        Assert.Equal(10, o.ToleranciaMin);
        Assert.Equal(20, o.SospechosaMin);
        Assert.Equal(60, o.PlazoHirHtrMin);
        Assert.Equal(2, o.MaxModificacionesHirHtr);
        Assert.Equal(3, o.MaxModificacionesFiltro4);
        Assert.Equal(3, o.MaxApoyos);
        Assert.Equal(50, o.AforoMax);
        Assert.Equal(120, o.PrecargaMin);
    }

    [Fact]
    public void SinConfiguracion_EsInvalida()
    {
        // Sin valores por defecto: una configuración vacía no debe pasar como "todo en 0".
        var errores = Validar(new ReglasNegocioOptions());

        Assert.NotEmpty(errores);
    }

    [Fact]
    public void ToleranciaIgualASospechosa_EsInvalida()
    {
        var o = LeerConfiguracionReal();
        o.ToleranciaMin = 20;
        o.SospechosaMin = 20;

        var errores = Validar(o);

        Assert.Contains(errores, e => e.MemberNames.Contains(nameof(ReglasNegocioOptions.ToleranciaMin)));
    }

    [Fact]
    public void AdvertenciaMayorQueVentanaDeApertura_EsInvalida()
    {
        var o = LeerConfiguracionReal();
        o.FiltroAdvertenciaMin = 30;
        o.FiltroVentanaAperturaMin = 20;

        Assert.NotEmpty(Validar(o));
    }

    [Fact]
    public void TurnosEnDesorden_EsInvalido()
    {
        var o = LeerConfiguracionReal();
        o.HoraInicioTurno2 = 16;
        o.HoraInicioTurno3 = 8;

        Assert.NotEmpty(Validar(o));
    }

    [Fact]
    public void ZonaHorariaInexistente_EsInvalida()
    {
        var o = LeerConfiguracionReal();
        o.ZonaHoraria = "Marte/Olympus_Mons";

        Assert.Contains(Validar(o), e => e.MemberNames.Contains(nameof(ReglasNegocioOptions.ZonaHoraria)));
    }
}
