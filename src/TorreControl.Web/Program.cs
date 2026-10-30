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
