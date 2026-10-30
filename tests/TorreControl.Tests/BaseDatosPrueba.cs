using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using TorreControl.Web.Data;

namespace TorreControl.Tests;

/// <summary>
/// Crea una base de datos SQL Server nueva y vacía (con todas las migraciones) para un grupo
/// de pruebas, y la borra al terminar. Se usa SQL Server real y no SQLite porque RowVersion,
/// los índices y las restricciones se comportan distinto en otros motores.
///
/// ¿Dónde está el servidor? En la variable de entorno TORRECONTROL_PRUEBAS_SQL (cadena de
/// conexión SIN "Database="). Si no existe, en Windows se usa LocalDB.
/// </summary>
public class BaseDatosPrueba : IAsyncLifetime
{
    public string CadenaConexion { get; }

    public FakeTimeProvider Reloj { get; } = new(new DateTimeOffset(2026, 3, 10, 18, 0, 0, TimeSpan.Zero));

    public BaseDatosPrueba()
    {
        var servidor = Environment.GetEnvironmentVariable("TORRECONTROL_PRUEBAS_SQL")
            ?? (OperatingSystem.IsWindows()
                ? @"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True"
                : throw new InvalidOperationException(
                    "Define TORRECONTROL_PRUEBAS_SQL con la cadena de conexión a un SQL Server de pruebas (ver README)."));

        var cadena = new SqlConnectionStringBuilder(servidor)
        {
            // Nombre único: varias clases de prueba pueden correr al mismo tiempo sin pisarse.
            InitialCatalog = $"TorreControl_Pruebas_{Guid.NewGuid():N}"
        };
        CadenaConexion = cadena.ConnectionString;
    }

    public AppDbContext CrearContexto(int? usuarioId = null) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(CadenaConexion).Options,
            new UsuarioFijo(usuarioId),
            Reloj);

    public async Task InitializeAsync()
    {
        await using var db = CrearContexto();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using var db = CrearContexto();
        await db.Database.EnsureDeletedAsync();
    }

    private sealed class UsuarioFijo(int? id) : IUsuarioActual
    {
        public int? Id => id;
    }
}
