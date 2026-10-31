using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TorreControl.Web.Data;
using TorreControl.Web.Models;
using TorreControl.Web.Security;

namespace TorreControl.Tests;

/// <summary>
/// La app completa (entorno Production) conectada a una base de pruebas nueva, con usuarios y
/// datos mínimos para probar login y permisos de punta a punta. La contraseña se genera al
/// azar en cada corrida: nunca está escrita en el repositorio.
/// </summary>
public partial class AppConBaseDatos : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly BaseDatosPrueba _bd = new();

    public string Contrasena { get; } = $"Pba{Guid.NewGuid():N}"[..16] + "9z";

    public const string MonitoristaA = "T-MON-A";
    public const string MonitoristaB = "T-MON-B";
    public const string Supervisor = "T-SUP";
    public const string Inactivo = "T-INACTIVO";

    public int ServicioPlantaA { get; private set; }
    public int ServicioPlantaB { get; private set; }
    public int ServicioMixtoA { get; private set; }
    public int ServicioSalidaA { get; private set; }

    // Límite de intentos por IP; una prueba lo baja para comprobar el 429.
    protected virtual int LoginPeticionesPorIp => 1000;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("ConnectionStrings:TorreControl", _bd.CadenaConexion);
        builder.UseSetting("https_port", "443");
        builder.UseSetting("Seguridad:LoginPeticionesPorIp", LoginPeticionesPorIp.ToString());
    }

    public async Task InitializeAsync()
    {
        await _bd.InitializeAsync();

        using var alcance = Services.CreateScope();
        var usuarios = alcance.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        var db = alcance.ServiceProvider.GetRequiredService<AppDbContext>();

        async Task<Usuario> Crear(string numero, string rol, bool activo = true)
        {
            var u = new Usuario { NumeroEmpleado = numero, Nombre = $"Usuario {numero}", Activo = activo };
            var r = await usuarios.CreateAsync(u, Contrasena);
            if (!r.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", r.Errors.Select(e => e.Description)));
            }

            await usuarios.AddToRoleAsync(u, rol);
            return u;
        }

        var monA = await Crear(MonitoristaA, Roles.Monitorista);
        var monB = await Crear(MonitoristaB, Roles.Monitorista);
        await Crear(Supervisor, Roles.SupervisorApoyo);
        await Crear(Inactivo, Roles.Monitorista, activo: false);

        var cliente = new Cliente { Codigo = "PRB", Nombre = "Cliente de prueba" };
        var plantaA = new Planta { Cliente = cliente, Nombre = "Planta A", NombreCorto = "PLANTA A", Municipio = "Silao" };
        var plantaB = new Planta { Cliente = cliente, Nombre = "Planta B", Municipio = "León" };
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var servicioA = new Servicio { Ruta = new Ruta { Planta = plantaA, NombreRuta = "Centro", TipoServicio = TipoServicio.E }, Fecha = hoy, Turno = 1, Hip = new(5, 30), Htp = new(6, 50) };
        var servicioB = new Servicio { Ruta = new Ruta { Planta = plantaB, NombreRuta = "Norte", TipoServicio = TipoServicio.E }, Fecha = hoy, Turno = 1, Hip = new(5, 30), Htp = new(6, 50) };
        // Salida de la planta A en el 2º turno y un servicio MIXTO (va en la pestaña "PLANTA A MIXTO").
        var salidaA = new Servicio { Ruta = new Ruta { Planta = plantaA, NombreRuta = "Centro", TipoServicio = TipoServicio.S }, Fecha = hoy, Turno = 2, Hip = new(15, 5), Htp = new(16, 15) };
        var mixtoA = new Servicio { Ruta = new Ruta { Planta = plantaA, NombreRuta = "Doce por doce", TipoServicio = TipoServicio.E }, Fecha = hoy, Turno = 2, Hip = new(14, 30), Htp = new(15, 50), EsMixto = true };

        db.AddRange(
            new ModuloUsuario { Modulo = new Modulo { Nombre = "Módulo A", Tipo = TipoModulo.Fijo, Plantas = [new() { Planta = plantaA }] }, Usuario = monA, FechaInicio = hoy.AddDays(-30) },
            new ModuloUsuario { Modulo = new Modulo { Nombre = "Módulo B", Tipo = TipoModulo.Fijo, Plantas = [new() { Planta = plantaB }] }, Usuario = monB, FechaInicio = hoy.AddDays(-30) },
            servicioA,
            servicioB,
            salidaA,
            mixtoA);
        await db.SaveChangesAsync();

        (ServicioPlantaA, ServicioPlantaB) = (servicioA.Id, servicioB.Id);
        (ServicioSalidaA, ServicioMixtoA) = (salidaA.Id, mixtoA.Id);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _bd.DisposeAsync();
        await base.DisposeAsync();
    }

    public AppDbContext CrearContexto() => _bd.CrearContexto();

    /// <summary>Un "navegador" nuevo: guarda cookies y NO sigue redirecciones (para poder revisarlas).</summary>
    public HttpClient Navegador() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://torre.ejemplo"),
        AllowAutoRedirect = false
    });

    /// <summary>Hace lo mismo que una persona: abre el login, toma el token CSRF del formulario y lo envía.</summary>
    public static async Task<HttpResponseMessage> IniciarSesionAsync(HttpClient navegador, string numero, string contrasena, string? returnUrl = null)
    {
        var token = await TokenDeFormularioAsync(navegador, "/Cuenta/Login");
        var campos = new Dictionary<string, string>
        {
            ["NumeroEmpleado"] = numero,
            ["Contrasena"] = contrasena,
            ["__RequestVerificationToken"] = token
        };
        if (returnUrl is not null)
        {
            campos["ReturnUrl"] = returnUrl;
        }

        return await navegador.PostAsync("/Cuenta/Login", new FormUrlEncodedContent(campos));
    }

    public static async Task<string> TokenDeFormularioAsync(HttpClient navegador, string url)
    {
        var html = await navegador.GetStringAsync(url);
        return InputToken().Match(html).Groups[1].Value;
    }

    /// <summary>POST con JSON y el token antiforgery en la cabecera, como lo hace tc.post() en el navegador.</summary>
    public static async Task<HttpResponseMessage> PostJsonAsync(HttpClient navegador, string url, object datos, bool conToken = true)
    {
        var peticion = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(datos) };
        if (conToken)
        {
            var html = await navegador.GetStringAsync("/Home/Error");
            peticion.Headers.Add("RequestVerificationToken", MetaToken().Match(html).Groups[1].Value);
        }
        return await navegador.SendAsync(peticion);
    }

    /// <summary>Texto del mensaje de error del login (o vacío si no hay).</summary>
    public static string MensajeDeError(string html) =>
        WebUtility.HtmlDecode(MensajeError().Match(html).Groups[1].Value).Trim();

    [GeneratedRegex("<meta name=\"csrf-token\" content=\"([^\"]+)\"")]
    private static partial Regex MetaToken();

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex InputToken();

    [GeneratedRegex("<div class=\"tc-error-login validation-summary-errors\"[^>]*><ul><li>(.*?)</li>", RegexOptions.Singleline)]
    private static partial Regex MensajeError();
}
