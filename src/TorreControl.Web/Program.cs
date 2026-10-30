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

// ---- Base de datos ----
// La cadena de conexión NUNCA va en appsettings.json (que sí está en Git): se guarda con
// "dotnet user-secrets" en desarrollo o en variables de entorno en un servidor.
var cadenaConexion = builder.Configuration.GetConnectionString("TorreControl")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexión 'TorreControl'. Configúrala con dotnet user-secrets (ver README).");

builder.Services.AddDbContext<AppDbContext>(opciones => opciones.UseSqlServer(cadenaConexion));

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
    .AddEntityFrameworkStores<AppDbContext>();

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
    opciones.LoginPath = "/Cuenta/Login";                     // pantallas de la Fase 1
    opciones.AccessDeniedPath = "/Cuenta/AccesoDenegado";
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

app.UseCabecerasSeguridad(app.Environment.IsDevelopment());

if (!app.Environment.IsDevelopment())
{
    // Al usuario solo se le muestra un mensaje genérico; el detalle del error queda en el log.
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

// Permite que las pruebas de integración levanten la app completa (WebApplicationFactory).
public partial class Program;
