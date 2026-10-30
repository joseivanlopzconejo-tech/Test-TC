namespace TorreControl.Web.Data;

/// <summary>
/// "¿Quién está haciendo este cambio?". La base de datos lo pregunta para llenar la Auditoría
/// sin saber nada de páginas web; en las pruebas se reemplaza por un usuario fijo.
/// </summary>
public interface IUsuarioActual
{
    /// <summary>Id del usuario con sesión iniciada; null si es el sistema (sin sesión).</summary>
    int? Id { get; }
}
