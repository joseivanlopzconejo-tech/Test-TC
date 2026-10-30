# Torre de Control

Aplicación web para monitorear rutas de transporte de personal hacia plantas industriales.
Los monitoristas trabajan por turnos (24/7) y, en cada servicio, capturan filtros de revisión,
horas reales de inicio y llegada, aforo, unidades de apoyo e incidencias.

Es un proyecto de portafolio. **Todos los datos son ficticios.**

## Stack

- .NET 10 (LTS) · ASP.NET Core MVC + Razor
- Entity Framework Core (code-first, migraciones) sobre SQL Server
- ASP.NET Core Identity (hash de contraseñas, bloqueo por intentos, roles)
- xUnit para pruebas

## Estructura

```
src/TorreControl.Web/
  Models/      Entidades (Usuario, y en la Fase 1 Servicio, Ruta, Filtro…)
  Data/        AppDbContext y migraciones
  Services/    Reglas de negocio puras y testeables + ReglasNegocioOptions
  Security/    Cabeceras de seguridad; en la Fase 1, políticas de autorización
  Controllers/ Views/ wwwroot/
tests/TorreControl.Tests/   Pruebas xUnit (unitarias y de integración)
```

## Cómo correrlo (Windows con LocalDB)

Requisitos: [.NET 10 SDK](https://dotnet.microsoft.com/download) y SQL Server LocalDB
(viene con Visual Studio, o se instala con SQL Server Express).

1. Restaurar herramientas y guardar la cadena de conexión **fuera del repositorio**:

   ```powershell
   dotnet tool restore
   dotnet user-secrets set "ConnectionStrings:TorreControl" "Server=(localdb)\MSSQLLocalDB;Database=TorreControl;Trusted_Connection=True;TrustServerCertificate=True" --project src/TorreControl.Web
   ```

2. Crear la base de datos y confiar en el certificado HTTPS de desarrollo:

   ```powershell
   dotnet ef database update --project src/TorreControl.Web
   dotnet dev-certs https --trust
   ```

3. Arrancar la app y abrir https://localhost:7045:

   ```powershell
   dotnet run --project src/TorreControl.Web
   ```

Pruebas: `dotnet test`

> En Linux o macOS no existe LocalDB. Ahí se usa SQL Server en Docker con la misma
> migración; solo cambia la cadena de conexión que guardas en user-secrets.

## Configuración

Las constantes de negocio viven en `appsettings.json`, en la sección `ReglasNegocio`:
tolerancias, plazos, intervalos de filtros y horas de turno. Se validan al arrancar, así que
la app no inicia si falta un valor o si dos valores se contradicen. Los valores marcados
como **PROVISIONAL** están pendientes de confirmar.

Los secretos (cadenas de conexión, llaves de API, contraseñas de desarrollo) **nunca** se
guardan en el repositorio. En desarrollo van en `dotnet user-secrets` y en un servidor en
variables de entorno.

## Seguridad

- HTTPS obligatorio: redirección + HSTS.
- Cabeceras CSP, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy` y
  `Permissions-Policy`.
- Cookies `HttpOnly`, `Secure` y `SameSite=Strict`. Sesión deslizante de 8 horas (un turno).
- Token antiforgery (CSRF) en todos los POST, incluidos los `fetch` de JavaScript.
- Contraseñas con el hasher de Identity y bloqueo temporal tras 5 intentos fallidos.
- Solo consultas parametrizadas de EF Core y mensajes de error genéricos para el usuario.

## Avance

- [x] Fase 0 — Cimientos
- [ ] Fase 1 — Datos y acceso
- [ ] Fase 2 — Tabla del monitorista
- [ ] Fase 3 — Supervisores y Jefe
- [ ] Fase 4 — Exportaciones
- [ ] Fase 5 — Tráfico por zona
- [ ] Fase 6 — Panel del Jefe de Monitoreo
