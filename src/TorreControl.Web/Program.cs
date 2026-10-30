using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TorreControl.Web.Data;
using TorreControl.Web.Models;
using TorreControl.Web.Security;
using TorreControl.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// No anunciar qué servidor usamos ("Server: Kestrel"): es información gratis para un atacante.
builder.WebHost.ConfigureKestrel(opciones => opciones.AddServerHeader = false);

// Reglas de negocio desde configuración. ValidateOnStart hace que la app NO arranque
// si falta un valor o hay uno contradictorio, en vez de fallar a mitad de un turno.
builder.Services.AddOptions<ReglasNegocioOptions>()
    .Bind(builder.Configuration.GetSection(ReglasNegocioOptions.Seccion))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Reloj del sistema inyectable: las reglas de tiempo lo reciben como dependencia
// para que las pruebas puedan "congelar" la hora.
builder.Services.AddSingleton(TimeProvider.System);

// Reglas puras: no guardan estado, así que una sola instancia sirve para toda la app.
builder.Services.AddSingleton<RelojNegocio>();
builder.Services.AddSingleton<ReglasTurno>();
builder.Services.AddSingleton<CalculadoraFiltros>();
builder.Services.AddSingleton<VentanaServicios>();

// ---- Base de datos ----
// La cadena de conexión NUNCA va en appsettings.json (que sí está en Git): se guarda con
// "dotnet user-secrets" en desarrollo o en variables de entorno en un servidor.
var cadenaConexion = builder.Configuration.GetConnectionString("TorreControl")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexión 'TorreControl'. Configúrala con dotnet user-secrets (ver README).");

builder.Services.AddDbContext<AppDbContext>(opciones => opciones.UseSqlServer(cadenaConexion));

// La Auditoría necesita saber quién hace cada cambio: se lo dice la sesión de la petición web.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioActual, UsuarioActualHttp>();
builder.Services.AddScoped<DatosEjemplo>();

// ---- Permisos ----
builder.Services.AddAuthorization(Politicas.Configurar);
builder.Services.AddScoped<AccesoPlantas>();

// ---- Identity: usuarios, contraseñas y roles ----
builder.Services.AddIdentity<Usuario, IdentityRole<int>>(opciones =>
    {
        // Contraseñas: se privilegia la longitud sobre los símbolos raros, que la gente
        // termina escribiendo en un papel pegado al monitor.
        opciones.Password.RequiredLength = 10;
        opciones.Password.RequireDigit = true;
        opciones.Password.RequireLowercase = true;
        opciones.Password.RequireUppercase = true;
        opciones.Password.RequireNonAlphanumeric = false;

        // Bloqueo temporal: 5 intentos fallidos = 15 minutos. Es corto a propósito porque
        // los números de empleado son fáciles de adivinar y un bloqueo largo permitiría
        // dejar fuera a un monitorista a mitad de su turno.
        opciones.Lockout.MaxFailedAccessAttempts = 5;
        opciones.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        opciones.Lockout.AllowedForNewUsers = true;

        // No hay correos en este sistema; el identificador es el número de empleado.
        opciones.User.RequireUniqueEmail = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager<SignInManagerTorre>()            // + regla "usuario desactivado no entra"
    .AddClaimsPrincipalFactory<FabricaClaimsTorre>();  // + nombre del usuario en la cookie

// Cada 5 minutos se revisa que la sesión siga siendo válida (usuario activo, contraseña no
// cambiada). Así, desactivar a alguien le cierra la sesión en minutos y no hasta 8 horas después.
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(5));

builder.Services.AddScoped<ServicioInicioSesion>();
builder.Services.AddLimiteLogin(builder.Configuration);

// ---- Cookie de sesión ----
builder.Services.ConfigureApplicationCookie(opciones =>
{
    opciones.Cookie.Name = "TorreControl.Sesion";
    opciones.Cookie.HttpOnly = true;                          // JavaScript no puede leerla (si hay XSS, no se la roban)
    opciones.Cookie.SecurePolicy = CookieSecurePolicy.Always; // solo viaja por HTTPS
    opciones.Cookie.SameSite = SameSiteMode.Strict;           // otro sitio no puede usarla para mandar peticiones
    // Un turno dura 8 horas. "Deslizante": cada vez que el usuario trabaja, el plazo se renueva,
    // así no lo saca a media captura como el sistema actual.
    opciones.ExpireTimeSpan = TimeSpan.FromHours(8);
    opciones.SlidingExpiration = true;
    opciones.LoginPath = "/Cuenta/Login";
    opciones.AccessDeniedPath = "/Cuenta/AccesoDenegado";
    // Para que la página avise antes de que la sesión venza (ver sesion.js).
    SesionDeslizante.Configurar(opciones);

    // Las llamadas de JavaScript a /api no deben recibir la página de login como respuesta:
    // se les contesta 401 (sin sesión) o 403 (sin permiso) para que la tabla lo maneje.
    opciones.Events.OnRedirectToLogin = contexto => ResponderApi(contexto, StatusCodes.Status401Unauthorized);
    opciones.Events.OnRedirectToAccessDenied = contexto => ResponderApi(contexto, StatusCodes.Status403Forbidden);

    static Task ResponderApi(RedirectContext<CookieAuthenticationOptions> contexto, int codigo)
    {
        if (contexto.Request.Path.StartsWithSegments("/api"))
        {
            contexto.Response.StatusCode = codigo;
        }
        else
        {
            contexto.Response.Redirect(contexto.RedirectUri);
        }

        return Task.CompletedTask;
    }
});

// ---- Antiforgery (CSRF) ----
// Todo POST debe traer un token secreto que solo nuestra página conoce. Los fetch() de
// JavaScript lo mandan en esta cabecera (lo lee site.js de la etiqueta <meta> del layout).
builder.Services.AddAntiforgery(opciones =>
{
    opciones.HeaderName = "RequestVerificationToken";
    opciones.Cookie.Name = "TorreControl.Antiforgery";
    opciones.Cookie.HttpOnly = true;
    opciones.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    opciones.Cookie.SameSite = SameSiteMode.Strict;
});

// HSTS: el navegador recuerda por un año que este sitio solo se abre con HTTPS.
builder.Services.AddHsts(opciones => opciones.MaxAge = TimeSpan.FromDays(365));

builder.Services.AddControllersWithViews(opciones =>
{
    // Valida el token antiforgery en TODOS los POST/PUT/DELETE sin tener que acordarse
    // de poner [ValidateAntiForgeryToken] en cada acción (los GET no lo necesitan).
    opciones.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

var app = builder.Build();

// Solo en desarrollo: crea/actualiza la base y carga datos FICTICIOS para poder probar.
if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("DatosEjemplo:Sembrar"))
{
    using var alcance = app.Services.CreateScope();
    await alcance.ServiceProvider.GetRequiredService<DatosEjemplo>().SembrarAsync();
}

app.UseCabecerasSeguridad(app.Environment.IsDevelopment());

if (!app.Environment.IsDevelopment())
{
    // Al usuario solo se le muestra un mensaje genérico; el detalle del error queda en el log.
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// CSS, JS e imágenes son públicos: la pantalla de login también los necesita.
app.MapStaticAssets().AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

// Permite que las pruebas de integración levanten la app completa (WebApplicationFactory).
public partial class Program;
