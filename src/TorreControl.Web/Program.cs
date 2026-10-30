using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TorreControl.Web.Data;
using TorreControl.Web.Models;
using TorreControl.Web.Services;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
