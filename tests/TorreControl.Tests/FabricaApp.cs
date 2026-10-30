using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TorreControl.Tests;

/// <summary>
/// Levanta la app completa en memoria (sin abrir puertos) para probarla como lo haría
/// un navegador. Se usa el entorno "Production" para probar la configuración real
/// de seguridad, no la relajada de desarrollo.
/// </summary>
public class FabricaApp : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        // Estas pruebas no tocan la base de datos; basta una cadena que nunca se usa.
        builder.UseSetting("ConnectionStrings:TorreControl", "Server=no-se-usa;Database=no-se-usa");
        // Puerto HTTPS conocido para que la redirección http -> https sepa a dónde mandar.
        builder.UseSetting("https_port", "443");
    }
}
