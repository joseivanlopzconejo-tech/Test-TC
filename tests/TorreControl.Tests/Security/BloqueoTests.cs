using System.Net;

namespace TorreControl.Tests.Security;

/// <summary>Bloqueo temporal por intentos fallidos (en su propia base para no bloquear a los demás tests).</summary>
public class BloqueoTests(AppConBaseDatos app) : IClassFixture<AppConBaseDatos>
{
    [Fact]
    public async Task CincoIntentosFallidos_BloqueanLaCuentaAunConLaContrasenaCorrecta()
    {
        for (var i = 0; i < 5; i++)
        {
            await AppConBaseDatos.IniciarSesionAsync(app.Navegador(), AppConBaseDatos.MonitoristaA, $"Incorrecta{i}AA");
        }

        var navegador = app.Navegador();
        var respuesta = await AppConBaseDatos.IniciarSesionAsync(navegador, AppConBaseDatos.MonitoristaA, app.Contrasena);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("espera 15 minutos", AppConBaseDatos.MensajeDeError(await respuesta.Content.ReadAsStringAsync()));
        Assert.Equal(HttpStatusCode.Redirect, (await navegador.GetAsync("/")).StatusCode);
    }

    [Fact]
    public async Task CuatroIntentosFallidos_TodaviaPermitenEntrar()
    {
        for (var i = 0; i < 4; i++)
        {
            await AppConBaseDatos.IniciarSesionAsync(app.Navegador(), AppConBaseDatos.MonitoristaB, $"Incorrecta{i}AA");
        }

        var respuesta = await AppConBaseDatos.IniciarSesionAsync(app.Navegador(), AppConBaseDatos.MonitoristaB, app.Contrasena);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
    }
}

/// <summary>Límite de intentos por IP, con un límite bajo solo para esta prueba.</summary>
public class LimitePorIpTests(LimitePorIpTests.AppConLimiteBajo app) : IClassFixture<LimitePorIpTests.AppConLimiteBajo>
{
    public class AppConLimiteBajo : AppConBaseDatos
    {
        protected override int LoginPeticionesPorIp => 3;
    }

    [Fact]
    public async Task DemasiadosIntentosDesdeLaMismaIp_Responde429()
    {
        var codigos = new List<HttpStatusCode>();
        for (var i = 0; i < 4; i++)
        {
            codigos.Add((await AppConBaseDatos.IniciarSesionAsync(app.Navegador(), "NO-EXISTE", "Incorrecta1AA")).StatusCode);
        }

        Assert.Equal([HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.TooManyRequests], codigos);
    }
}
