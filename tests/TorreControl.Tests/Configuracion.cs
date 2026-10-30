using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TorreControl.Web.Services;

namespace TorreControl.Tests;

/// <summary>Lee las reglas de negocio del appsettings.json real de la app, para que las pruebas usen los mismos números.</summary>
public static class Configuracion
{
    public static ReglasNegocioOptions ReglasReales()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        return config.GetSection(ReglasNegocioOptions.Seccion).Get<ReglasNegocioOptions>()
            ?? throw new InvalidOperationException("Falta la sección ReglasNegocio.");
    }

    public static IOptions<ReglasNegocioOptions> Opciones() => Options.Create(ReglasReales());
}
