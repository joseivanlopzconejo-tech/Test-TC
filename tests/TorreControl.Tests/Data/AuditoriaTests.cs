using Microsoft.EntityFrameworkCore;
using TorreControl.Web.Models;

namespace TorreControl.Tests.Data;

public class AuditoriaTests(BaseDatosPrueba bd) : IClassFixture<BaseDatosPrueba>
{
    private const int UsuarioQueEdita = 77;

    private async Task<List<Auditoria>> AuditoriaDe(string entidad, string id)
    {
        await using var db = bd.CrearContexto();
        return await db.Auditorias.Where(a => a.Entidad == entidad && a.EntidadId == id).OrderBy(a => a.Id).ToListAsync();
    }

    private async Task<Cliente> CrearClienteAsync(string codigo)
    {
        await using var db = bd.CrearContexto(UsuarioQueEdita);
        var cliente = new Cliente { Codigo = codigo, Nombre = "Cliente de prueba" };
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();
        return cliente;
    }

    [Fact]
    public async Task Crear_GuardaUnaFilaConElIdRealYLosValores()
    {
        var cliente = await CrearClienteAsync("AUD-C1");

        var fila = Assert.Single(await AuditoriaDe("Cliente", cliente.Id.ToString()));
        Assert.Equal(AccionAuditoria.Crear, fila.Accion);
        Assert.Equal(UsuarioQueEdita, fila.UsuarioId);
        Assert.Contains("AUD-C1", fila.ValorNuevo);
        Assert.Equal(bd.Reloj.GetUtcNow().UtcDateTime, fila.FechaUtc);
    }

    [Fact]
    public async Task Modificar_GuardaUnaFilaPorCampoConAntesYDespues()
    {
        var cliente = await CrearClienteAsync("AUD-C2");

        await using (var db = bd.CrearContexto(UsuarioQueEdita))
        {
            var c = await db.Clientes.SingleAsync(x => x.Id == cliente.Id);
            c.Nombre = "Nombre corregido";
            await db.SaveChangesAsync();
        }

        var fila = (await AuditoriaDe("Cliente", cliente.Id.ToString())).Last();
        Assert.Equal(AccionAuditoria.Modificar, fila.Accion);
        Assert.Equal("Nombre", fila.Campo);
        Assert.Equal("Cliente de prueba", fila.ValorAnterior);
        Assert.Equal("Nombre corregido", fila.ValorNuevo);
    }

    [Fact]
    public async Task GuardarSinCambiosReales_NoEscribeAuditoria()
    {
        var cliente = await CrearClienteAsync("AUD-C3");

        await using (var db = bd.CrearContexto(UsuarioQueEdita))
        {
            var c = await db.Clientes.SingleAsync(x => x.Id == cliente.Id);
            c.Nombre = c.Nombre; // mismo valor
            await db.SaveChangesAsync();
        }

        Assert.Single(await AuditoriaDe("Cliente", cliente.Id.ToString()));
    }

    [Fact]
    public async Task Contrasena_NuncaSeGuardaEnLaAuditoria()
    {
        int usuarioId;
        await using (var db = bd.CrearContexto())
        {
            var u = new Usuario { NumeroEmpleado = "AUD-U1", Nombre = "Prueba", PasswordHash = "HASH-SECRETO-1" };
            db.Users.Add(u);
            await db.SaveChangesAsync();
            usuarioId = u.Id;

            u.PasswordHash = "HASH-SECRETO-2";
            await db.SaveChangesAsync();
        }

        var filas = await AuditoriaDe("Usuario", usuarioId.ToString());
        Assert.All(filas, f => Assert.DoesNotContain("HASH-SECRETO", (f.ValorAnterior ?? "") + (f.ValorNuevo ?? "")));
        // Pero sí queda constancia de que la contraseña cambió.
        Assert.Contains(filas, f => f.Campo == nameof(Usuario.PasswordHash) && f.ValorNuevo == "(oculto)");
    }

    [Fact]
    public async Task SiElGuardadoFalla_NoQuedaAuditoria()
    {
        await CrearClienteAsync("AUD-DUP");
        int antes;
        await using (var db = bd.CrearContexto())
        {
            antes = await db.Auditorias.CountAsync();
        }

        // Código repetido: el índice único hace fallar el guardado.
        await Assert.ThrowsAsync<DbUpdateException>(() => CrearClienteAsync("AUD-DUP"));

        await using (var db = bd.CrearContexto())
        {
            Assert.Equal(antes, await db.Auditorias.CountAsync());
        }
    }
}
