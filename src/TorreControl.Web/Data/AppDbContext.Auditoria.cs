using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TorreControl.Web.Models;

namespace TorreControl.Web.Data;

/// <summary>
/// Auditoría automática: cada vez que se guarda algo, antes de escribirlo se anota quién cambió
/// qué, cuándo y los valores antes/después. Así nadie "se olvida" de auditar un cambio nuevo.
/// </summary>
public partial class AppDbContext
{
    // Columnas técnicas que cambian solas y solo meterían ruido en la bitácora.
    private static readonly HashSet<string> CamposIgnorados =
    [
        nameof(Usuario.ConcurrencyStamp),
        nameof(Usuario.SecurityStamp),
        nameof(Usuario.NormalizedUserName),
        nameof(Usuario.NormalizedEmail),
        nameof(Usuario.AccessFailedCount),
        nameof(Servicio.RowVersion)
    ];

    // Se anota QUE cambió, pero nunca su valor (aunque sea un hash, no debe salir de su tabla).
    private static readonly HashSet<string> CamposSecretos = [nameof(Usuario.PasswordHash)];

    private const string ValorOculto = "(oculto)";

    /// <summary>Anota un evento que no es un cambio de datos (inicio de sesión, cierre, bloqueo).</summary>
    public void RegistrarEvento(AccionAuditoria accion, int? usuarioId, string entidad, string? entidadId)
    {
        Auditorias.Add(new Auditoria
        {
            FechaUtc = _reloj.GetUtcNow().UtcDateTime,
            UsuarioId = usuarioId,
            Transaccion = Guid.NewGuid(),
            Accion = accion,
            Entidad = entidad,
            EntidadId = entidadId
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        SaveChangesAsync(acceptAllChangesOnSuccess).GetAwaiter().GetResult();

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var pendientes = PrepararAuditoria();
        if (pendientes.Count == 0)
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        // El cambio y su bitácora se guardan juntos o no se guarda ninguno: no puede quedar
        // un cambio sin auditar ni una auditoría de algo que no pasó.
        await using var transaccion = Database.CurrentTransaction is null
            ? await Database.BeginTransactionAsync(cancellationToken)
            : null;

        var filas = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        // Se completa DESPUÉS de guardar porque hasta entonces SQL Server no ha dado
        // el Id de los registros nuevos.
        Auditorias.AddRange(pendientes.SelectMany(p => p()));
        await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        if (transaccion is not null)
        {
            await transaccion.CommitAsync(cancellationToken);
        }

        return filas;
    }

    /// <summary>
    /// Revisa qué va a cambiar y prepara las filas de auditoría. Regresa "funciones pendientes"
    /// en lugar de filas porque los Id de lo nuevo se conocen hasta después de guardar.
    /// </summary>
    private List<Func<IEnumerable<Auditoria>>> PrepararAuditoria()
    {
        ChangeTracker.DetectChanges();

        var ahora = _reloj.GetUtcNow().UtcDateTime;
        var usuarioId = _usuarioActual?.Id;
        var grupo = Guid.NewGuid();
        var pendientes = new List<Func<IEnumerable<Auditoria>>>();

        var cambios = ChangeTracker.Entries()
            .Where(e => e.Entity is not Auditoria)
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entrada in cambios)
        {
            var entidad = entrada.Metadata.GetTableName() ?? entrada.Metadata.ClrType.Name;

            Auditoria Fila(AccionAuditoria accion, string? id, string? campo = null, string? anterior = null, string? nuevo = null) => new()
            {
                FechaUtc = ahora,
                UsuarioId = usuarioId,
                Transaccion = grupo,
                Accion = accion,
                Entidad = entidad,
                EntidadId = id,
                Campo = campo,
                ValorAnterior = anterior,
                ValorNuevo = nuevo
            };

            switch (entrada.State)
            {
                case EntityState.Added:
                    // Una sola fila con todos los valores iniciales en JSON.
                    pendientes.Add(() =>
                    [
                        Fila(AccionAuditoria.Crear, LlaveDe(entrada), nuevo: Instantanea(entrada, original: false))
                    ]);
                    break;

                case EntityState.Deleted:
                    var filaBorrado = Fila(AccionAuditoria.Eliminar, LlaveDe(entrada), anterior: Instantanea(entrada, original: true));
                    pendientes.Add(() => [filaBorrado]);
                    break;

                case EntityState.Modified:
                    // Una fila por campo que de verdad cambió de valor.
                    var id = LlaveDe(entrada);
                    var filas = entrada.Properties
                        .Where(p => p.IsModified && !CamposIgnorados.Contains(p.Metadata.Name))
                        .Where(p => !Equals(p.OriginalValue, p.CurrentValue))
                        .Select(p =>
                        {
                            var secreto = CamposSecretos.Contains(p.Metadata.Name);
                            return Fila(
                                AccionAuditoria.Modificar,
                                id,
                                campo: p.Metadata.Name,
                                anterior: secreto ? ValorOculto : Formatear(p.OriginalValue),
                                nuevo: secreto ? ValorOculto : Formatear(p.CurrentValue));
                        })
                        .ToList();

                    if (filas.Count > 0)
                    {
                        pendientes.Add(() => filas);
                    }

                    break;
            }
        }

        return pendientes;
    }

    private static string LlaveDe(EntityEntry entrada) =>
        string.Join(",", entrada.Metadata.FindPrimaryKey()!.Properties
            .Select(p => Formatear(entrada.Property(p.Name).CurrentValue)));

    private static string Instantanea(EntityEntry entrada, bool original)
    {
        var valores = entrada.Properties
            .Where(p => !CamposIgnorados.Contains(p.Metadata.Name) && !CamposSecretos.Contains(p.Metadata.Name))
            .ToDictionary(p => p.Metadata.Name, p => Formatear(original ? p.OriginalValue : p.CurrentValue));

        return JsonSerializer.Serialize(valores);
    }

    // Formato fijo e independiente del idioma del servidor, para que la bitácora se lea igual siempre.
    private static string? Formatear(object? valor) => valor switch
    {
        null => null,
        DateTime f => f.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        TimeOnly t => t.ToString("HH:mm", CultureInfo.InvariantCulture),
        DateTimeOffset o => o.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        IFormattable x => x.ToString(null, CultureInfo.InvariantCulture),
        _ => valor.ToString()
    };
}
