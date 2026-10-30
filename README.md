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
  Models/      Entidades (Servicio, Ruta, Filtro, Usuario…) y ViewModels
  Data/        AppDbContext, auditoría automática, migraciones y datos de ejemplo
  Services/    Reglas de negocio puras y testeables (turnos, filtros, reloj de CDMX…)
  Security/    Roles, políticas, acceso por planta (anti-IDOR), login y cabeceras
  Controllers/ Views/ wwwroot/
tests/TorreControl.Tests/   Pruebas xUnit (unitarias y de integración con SQL Server real)
```

## Cómo correrlo (Windows con LocalDB)

Requisitos: [.NET 10 SDK](https://dotnet.microsoft.com/download) y SQL Server LocalDB
(viene con Visual Studio, o se instala con SQL Server Express).

1. Restaurar herramientas y guardar los secretos **fuera del repositorio**: la cadena de
   conexión y la contraseña de los usuarios demo (inventa una de 10+ caracteres con
   mayúscula, minúscula y número):

   ```powershell
   dotnet tool restore
   dotnet user-secrets set "ConnectionStrings:TorreControl" "Server=(localdb)\MSSQLLocalDB;Database=TorreControl;Trusted_Connection=True;TrustServerCertificate=True" --project src/TorreControl.Web
   dotnet user-secrets set "DatosEjemplo:Contrasena" "<tu contraseña de desarrollo>" --project src/TorreControl.Web
   ```

2. Confiar en el certificado HTTPS de desarrollo:

   ```powershell
   dotnet dev-certs https --trust
   ```

3. Arrancar la app y abrir https://localhost:7045. En desarrollo, al arrancar se aplican
   las migraciones y se cargan los datos de ejemplo:

   ```powershell
   dotnet run --project src/TorreControl.Web
   ```

### Usuarios demo

Todos usan la contraseña que guardaste en `DatosEjemplo:Contrasena`.

| Número | Rol | Ve |
|---|---|---|
| 900001 | Sistemas | Todas las plantas |
| 900002 | Jefe de Monitoreo | Todas las plantas |
| 900003 | Supervisor de Apoyo | Todas las plantas |
| 900004 | Supervisor de Operaciones | Todas las plantas |
| 900005 | Monitorista | Módulo 1 (Norvial Silao, Norvial Irapuato) |
| 900006 | Monitorista | Módulo 2 (Quimtek Salamanca, Quimtek León) |

### Pruebas

```powershell
dotnet test
```

Las pruebas de base de datos crean una base temporal en SQL Server y la borran al terminar.
En Windows usan LocalDB sin configurar nada. En otro sistema, define la variable de entorno
`TORRECONTROL_PRUEBAS_SQL` con una cadena de conexión **sin** `Database=`.

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
- Cookies `HttpOnly`, `Secure` y `SameSite=Strict`. Sesión deslizante de 8 horas (un turno),
  con aviso antes de vencer.
- Token antiforgery (CSRF) en todos los POST, incluidos los `fetch` de JavaScript.
- Contraseñas con el hasher de Identity y bloqueo temporal tras 5 intentos fallidos.
- El login nunca revela si un número de empleado existe: mismo mensaje y mismo tiempo de
  respuesta en todos los fallos. Límite generoso de intentos por IP.
- Toda página exige sesión por defecto. Los permisos se validan en el servidor con políticas
  por rol y por planta, así que cambiar un id en la URL no da acceso a otra planta (anti-IDOR).
- Auditoría automática de cada cambio (quién, cuándo, antes y después), sin guardar contraseñas.
- Solo consultas parametrizadas de EF Core y mensajes de error genéricos para el usuario.

## Avance

- [x] Fase 0 — Cimientos
- [x] Fase 1 — Datos y acceso
- [ ] Fase 2 — Tabla del monitorista
- [ ] Fase 3 — Supervisores y Jefe
- [ ] Fase 4 — Exportaciones
- [ ] Fase 5 — Tráfico por zona
- [ ] Fase 6 — Panel del Jefe de Monitoreo
