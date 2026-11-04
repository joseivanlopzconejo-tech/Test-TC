# HANDOFF — Torre de Control

Documento de traspaso entre sesiones de trabajo con Claude. Resume TODO lo ocurrido en la primera
conversación (30/10/2026 → 04/11/2026). La sesión nueva no ve ese chat: solo este archivo y el repo.

Marcas:
- **[VERIFICADO]** = lo comprobé en el repo/entorno al escribir este archivo (04/11/2026) con
  `git`, `ls`, `dotnet build` o `dotnet test`.
- **[DE MEMORIA]** = viene de la conversación; no lo pude volver a comprobar en este momento.

Privacidad: el repositorio es público. Por decisión del usuario, `CLAUDE.md` (la especificación) NO
está en Git porque contiene nombres reales de clientes y rutas. En este archivo los nombres reales
de empresas se sustituyen por **[cliente real]** y las contraseñas demo por **[omitida]**.

---

## 1. DÓNDE NOS QUEDAMOS

**Lo último que hice** [VERIFICADO: commit `ee3d1f4`]: arreglé que los usuarios demo no aceptaran la
contraseña en Codespaces. Al reconstruir un Codespace se genera una contraseña demo nueva, pero la
base de datos se conserva y los usuarios seguían con la anterior. Ahora `DatosEjemplo` sincroniza la
contraseña de user-secrets con los usuarios demo existentes y les quita el bloqueo por intentos.

**Lo último que me dijo el usuario** [DE MEMORIA], en orden:
1. "Listo loco la verdad el diseno es mucho mejor que el anterior me llama la antencion que yo no
   puedo agregar horas o midificarlas. No me sale el icono del camion pero asi esta bien por el
   momento solo es conseptual y pienso ensenarsela a mi jede".
2. "Me refiero que me deje simular un turno que me deje hacer modificaciones porque se lo quiero
   mostrar a mi jefe".
3. Luego pidió este HANDOFF.

**Qué le respondí y qué estoy esperando de él** [DE MEMORIA]: le propuse construir las piezas de
EDICIÓN en este orden para una demo: **D (Horas) → B (Aforo) → E (Filtros) → C (Unidad/camión)**, más
un botón opcional **"Reiniciar demo"** (solo rol Sistemas) para repetir la presentación. Le hice
DOS preguntas que **NO ha respondido**:
1. ¿Cuándo se lo va a mostrar a su jefe? (para saber si hago las 4 piezas seguidas o pauso en cada una)
2. ¿Le sirve ese orden y el botón "Reiniciar demo"?
Quedé esperando su **OK** para empezar con la **Pieza D (Horas)**.

**Otra pregunta pendiente** [DE MEMORIA]: si paso la Pieza A a `main`. No respondió. `main` sigue en
la Fase 1 [VERIFICADO: `main` = `origin/main` = `2f4f9b3`].

**Qué sigue exactamente**: esperar su respuesta → Pieza D (captura de Hir/Htr con doble clic,
hora sospechosa, motivo en llegada tarde, cambio de estatus automático). Detalle en la sección 10.

**Bloqueado / riesgos**:
- Nada bloquea el código. La rama `claude/new-session-y5mvhr` está al día con GitHub [VERIFICADO].
- En el Codespace del usuario: el puerto **5178 quedó dañado** (GitHub guarda el estado de cada
  puerto y sobrevive a reconstrucciones). Por eso la app ahora usa el **puerto 3000**. Si algún
  puerto vuelve a dar 502 aunque la app responda 200 por dentro, cambiar de número de puerto.
- No sé si el usuario ya logró entrar después del último arreglo de contraseñas: me dijo que le
  gustó el diseño, así que **sí entró** antes de pedir el HANDOFF [DE MEMORIA].
- En este entorno de Claude (contenedor en la nube), Docker se cae seguido; hay que levantar
  `containerd` + `dockerd` + el contenedor `tc-sql` antes de correr las pruebas de base de datos
  (ver sección 7).

---

## 2. LÍNEA DE TIEMPO COMPLETA

Los hashes de commit están [VERIFICADO] (`git log`). El resto es [DE MEMORIA] salvo que diga otra cosa.

### Tramo 0 — Arranque (30/10/2026)
1. **El usuario subió `CLAUDE.md`** (especificación completa del proyecto "Torre de Control"). La
   sección 12 pedía: arrancar solo con Fases 0 y 1, y ANTES de escribir código confirmar la
   estructura de carpetas y las dudas sobre las ASUNCIONES.
2. **Revisé el repo**: estaba vacío (sin commits) y el contenedor no tenía .NET. Respondí con la
   estructura propuesta y 8 dudas (ver Anexo), sin escribir código.
3. **El usuario preguntó** "No puedes instalar lo que haga falta en el repositorio".
   - El instalador oficial de .NET (`builds.dotnet.microsoft.com`) estaba **bloqueado (403)** por el
     proxy del entorno.
   - Instalé **.NET 10 SDK 10.0.114 desde apt de Ubuntu** (`dotnet-sdk-10.0`).
   - Comprobé que NuGet funcionaba.
   - `dockerd` no corría: lo arranqué a mano, bajé `mcr.microsoft.com/mssql/server:2022-latest` y
     levanté el contenedor **`tc-sql`** (puerto 1433, contraseña SA aleatoria guardada FUERA del
     repo, en `/tmp/claude-0/sqlpw`).
   - Expliqué que .NET/SQL viven en la máquina y los paquetes NuGet en los `.csproj`.
4. **El usuario respondió las 8 dudas** y dijo "Arranca la Fase 0" (respuestas literales en el Anexo).

### Tramo 1 — Fase 0 (Cimientos)
- `c2e1358` Estructura inicial: `dotnet new sln --format sln`, `mvc`, `xunit`. Se borró
  `UnitTest1.cs` y `Privacy.cshtml`. Layout en español, tema oscuro base. `.gitignore`
  (`dotnet new gitignore` + `CLAUDE.md` + archivos de secretos). `.editorconfig`.
- `c903a4a` `Services/ReglasNegocioOptions.cs` (sin valores por defecto en C#; todo en
  `appsettings.json`; `[Range]` + `IValidatableObject` + `ValidateOnStart`). `TimeProvider.System`
  inyectable. 7 pruebas.
- `58d976f` EF Core + SQL Server + Identity con llaves `int`:
  - `Models/Usuario.cs` y `Data/AppDbContext.cs`, con tablas en español y singular (`Usuario`,
    `Rol`, `UsuarioRol`…);
  - política de contraseñas y bloqueo 5 / 15 min;
  - `dotnet-ef` como herramienta local (`dotnet-tools.json`);
  - user-secrets inicializado;
  - migración `InicialIdentity`.
- `159dae3` Seguridad:
  - `Security/CabecerasSeguridad.cs`: CSP sin `unsafe-inline`, nosniff, X-Frame-Options DENY,
    Referrer-Policy y Permissions-Policy;
  - HSTS de 365 días y sin cabecera `Server`;
  - cookies `HttpOnly` / `Secure` / `SameSite=Strict`; sesión deslizante de 8 h;
  - antiforgery global (`AutoValidateAntiforgeryTokenAttribute`), con cabecera `RequestVerificationToken`;
  - meta `csrf-token` en el layout y helper JS `tc.post()`;
  - perfil `http` quitado de `launchSettings`;
  - `tests/.../FabricaApp.cs` y 6 pruebas de integración.
- `fc51b12` README.
- Verifiqué en navegador headless (Playwright): página sin errores de consola y redirección http→https.
- Verifiqué que la app NO arranca con configuración inválida ni sin cadena de conexión.
- **Push falló con 403** ("Claude doesn't have GitHub access…"). Le expliqué cómo instalar la app
  de Claude en GitHub. Preparé un `git bundle` como plan B.

### Tramo 2 — GitHub y rama `main`
- El usuario dijo que le daba 404 al intentar dar acceso. Diagnóstico:
  - `list_repos` decía `can_push: true` y el MCP de GitHub lo reconocía;
  - git seguía con 403.
  Le di pasos (iniciar sesión en github.com, instalar la app de Claude) y le mandé el bundle.
- "Intenta de nuevo" → **el push funcionó**.
- Me autorizó a **borrar el bundle** y **crear `main`**. Creé `main` desde la rama y la subí.
- No pude cambiar la rama por defecto con mis herramientas; le di los pasos.
- Revisamos varias veces con `git ls-remote --symref origin HEAD`. Al principio seguía la rama de
  trabajo; el usuario mandó una captura de "Comparar cambios" (no era el lugar). Al final quedó
  **`main` como rama por defecto** [DE MEMORIA, verificado en su momento con ls-remote].

### Tramo 3 — Fase 1 (Datos y acceso)
- `ce4753f` Reglas puras: `RelojNegocio` (hora CDMX), `ReglasTurno` (turno por Hip, intervalo que
  cruza medianoche y en ese momento también la detección de MIXTO, que después se quitó) y
  `CalculadoraFiltros` (F1..F4 hacia atrás desde Hip). 18 pruebas con `FakeTimeProvider`.
- `7dc57bd` Modelo completo de la sección 4 + `Modulo`/`ModuloPlanta`/`ModuloUsuario` (mínimo, con
  fechas). Nada en cascada, salvo las piezas internas de un servicio. Enums como texto. Catálogos
  `EstatusUnidad` (3) y `TipoIncidencia` (5) en la migración `ModeloNegocio`.
  - **Aquí Docker se cayó por primera vez**: `containerd` no arrancaba y el contenedor `tc-sql`
    quedó corrupto ("RWLayer … unexpectedly nil"). Lo recreé.
- `73c34de` Auditoría automática:
  - `Data/AppDbContext.Auditoria.cs` (`SaveChangesAsync`): una fila por campo modificado; altas y
    bajas en JSON; misma transacción; `PasswordHash` se anota como "(oculto)";
  - `IUsuarioActual` / `UsuarioActualHttp`;
  - `tests/BaseDatosPrueba.cs` crea una base SQL Server temporal por clase;
  - 5 pruebas.
- `4354e9d` `Security/Roles.cs` (jerarquía, `PuedeAsignar` estricto), `Security/Politicas.cs`
  (política por defecto: exige sesión), `Services/ReglasAcceso.cs` (expresión reutilizable en SQL)
  y `Security/AccesoPlantas.cs` (anti-IDOR). 31 pruebas según el mensaje del commit.
- `d1d400c` Login:
  - `SignInManagerTorre` (usuario inactivo no entra), `FabricaClaimsTorre` (claim con el nombre) y
    `ServicioInicioSesion` (mismo mensaje y mismo tiempo en todos los fallos, con hash señuelo);
  - `LimiteLogin` (100 / 5 min por IP) y `SesionDeslizante` (calcula el vencimiento y avisa con
    `sesion.js`);
  - `CuentaController` (Login / Logout / MantenerSesion / AccesoDenegado) y sus vistas;
  - migración `RolesBase` (roles con HasData);
  - `/api/servicios/{id}` (404 si es de otra planta, 401 sin sesión);
  - pruebas de login, bloqueo, límite por IP e IDOR.
- `4db40b7` `Data/DatosEjemplo.cs`, solo en Development:
  - 3 clientes y 6 plantas ficticios, 40 unidades y 72 servicios por día;
  - usuarios demo 900001–900006;
  - la contraseña sale de user-secrets.
  También: aviso de sesión con la hora de CDMX.
- `2f4f9b3` README de la Fase 1. **Este es el commit que está en `main`** [VERIFICADO].
- Le expliqué la Fase 1 y le hice 6 preguntas (ver Anexo).

### Tramo 4 — Respuestas a la Fase 1 y paso a `main`
- El usuario: "Sí a todo, pasa la Fase 1 a main. 3 explícate mejor. 4 los módulos pendientes a la
  fase 3…".
- Hice fast-forward de `main` → `2f4f9b3` y lo subí.
- Le expliqué mejor el punto 3 (el servicio que llega justo en el cambio de turno no cuenta como
  mixto). Propuse separar la etiqueta MIXTO de la visibilidad por plazo de captura.
- El usuario explicó que "mixto/12x12" es un tipo de turno de planta (después de las 14:00,
  normalmente desde las 16:00) y dijo "de momento te dejo todo ok".

### Tramo 5 — Fase 2, tabla en solo lectura
- `4eb51f4`:
  - `Services/VentanaServicios.cs`: periodo de atención, desde la apertura del F1 o la precarga
    hasta Htp + plazo;
  - las peticiones con cabecera `X-Actualizacion-Automatica` no renuevan la sesión;
  - 13 pruebas.
- `23b91b9` `/api/tabla` (`Data/ConsultaTabla.cs`, `Models/ViewModels/TablaDto.cs`, sin caché) y 8 pruebas.
- `a21efe8` Tabla con **Tabulator 6.6.1** copiado a `wwwroot/lib/tabulator` (no usé la 6.7.x
  porque tenía menos de 2 semanas):
  - barra de ventana y reloj; pestañas que se arrastran; tema claro/oscuro (`tema.js`);
  - módulos JS `wwwroot/js/monitoreo/*`.
  - Verifiqué con Playwright y le mandé capturas.
- Pregunté A/B sobre MIXTO. Respondió: que se señale como MIXTO y "deja que uses también tus recomendaciones".
- El usuario pidió un diseño más profesional ("emulación de Excel pero sin el formato de oficina")
  y otro orden de columnas.
- `3539a67`:
  - **Inter 5.2.8** (OFL) servida localmente;
  - encabezados agrupados, números de fila, selección de rangos y Ctrl+C que pega en Excel;
  - chips con tinte suave y barra de estado;
  - columnas: Ruta | Unidad | Estatus | F1–F4 | Inicio (prog., real, dif.) | Llegada (prog., real,
    dif.) | Aforo | Observaciones | Operador;
  - `Services/TextosIncidencia.cs` con 4 pruebas;
  - datos de ejemplo con capturas simuladas.
  - Bugs encontrados y corregidos en el proceso: `getParentColumn()` devuelve `false`, la celda
    activa no se desplazaba, títulos cortados.

### Tramo 6 — Codespaces (probarlo sin instalar nada)
- El usuario preguntó "Como la prueba?". Le di pasos para Windows; respondió "no hay una manera…
  desde el git".
- `e755a57` `.devcontainer/` con `devcontainer.json`, `docker-compose.yml` (app + SQL Server con
  contraseña aleatoria compartida por un volumen), `preparar.sh`, `iniciar.sh` y `LEEME.md`.
  - `Security/ProxyHttps.cs`: modo detrás de proxy HTTPS, solo con `Proxy:DetrasDeProxyHttps`.
  - Redirecciones al login relativas.
  - **Parche de seguridad**: `System.Security.Cryptography.Pkcs` fijado en 10.0.14 (la 9.0.11 venía
    de `Microsoft.Data.SqlClient`; GHSA-846f-f764-76mx alta y GHSA-2fmv-rv2c-59x7 moderada).
  - Imagen cambiada de `1-10.0-noble` (era un SDK RC) a `2-10.0-noble` (SDK 10.0.403).
- **Larga cadena de error 502** en el Codespace del usuario (detalle en la sección 7):
  - `bb4a914` detectar la app por puerto (no por nombre), esperar a SQL, `setsid nohup`;
  - `b018c0a` `--urls http://*:PUERTO` y variables en el script;
  - `c20c18c` `probar.sh`, un solo comando que arranca y revisa;
  - `83460b5` diagnóstico de red en `probar.sh`;
  - `4651a79` quitar `forwardPorts` (hipótesis del reenvío "pegado");
  - `ee9d8ef` `network_mode: service:db`, el patrón de las plantillas oficiales; las pruebas fijan
    `Proxy:DetrasDeProxyHttps=false`;
  - `a2ded7d` `diagnostico-puertos.sh`, dos copias de prueba en los puertos 3000 y 3001;
  - **Resultado del diagnóstico (usuario): 3000 y 3001 abrieron, 5178 no** → el 5178 quedó dañado;
  - `e94c3dd` la app pasa al puerto **3000** y se borra el script de diagnóstico.
  - Entre medio, `04e6f39`: `probar.sh` solo detiene la app de su puerto (antes también mataba la
    copia del 3000).
- Problemas de contraseña: después de reconstruir, la contraseña nueva no coincidía con la base →
  `ee3d1f4` sincronización de la contraseña demo. Lo verifiqué: un usuario bloqueado entra con la
  nueva y la vieja deja de servir.

### Tramo 7 — Rondas de ajustes para la Pieza A (antes del OK)
El usuario mandó, en varios mensajes, correcciones que fui anotando hasta su "OK, empieza con la
Pieza A" (texto literal en el Anexo):
- lista larga como Excel y sin flechas de orden;
- ocultar filas = "no operó";
- servicios de supervisor con turno 1, 2, 3 o mixto;
- diseño pastel, "sorpréndeme";
- estatus automático EN RUTA / EN PLANTA / EN SALIDA / RESGUARDO;
- filtros con estatus y observación;
- bloques Entradas / Salidas;
- botón de camión;
- observaciones como bitácora;
- aforo de entrada y salida juntos (1–49);
- capturas PNG (captura rápida y reporte de turno);
- pestañas con nombre corto y etiqueta/nota de sesión;
- turnos fijos 00/08/16;
- mixtos en su propia pestaña con bloque fijo y buscador.

Escribí `docs/decisiones.md` con todo eso.

### Tramo 8 — Pieza A
- `a3878f8` Servidor:
  - `EstatusServicio` (regla pura);
  - `Servicio.EsMixto` (lo marca el supervisor), `Servicio.NoOpero` y `Planta.NombreCorto`; se quitó
    `Servicio.EstatusId`;
  - 9 estatus de filtro pastel y 9 motivos de retraso; migración `PiezaA_EstatusMixtosNoOpero`;
  - `/api/tabla?dia&turno` con grupos (planta y su MIXTO) y contadores por turno;
  - `POST /api/servicios/no-opero`;
  - datos de ejemplo con 26–36 servicios por planta y turno;
  - 18 pruebas.
- `0036a9f` Interfaz:
  - página larga y bloque fijo (pestañas, turno, búsqueda, encabezados pegados);
  - portada con nombre completo y tarjetas KPI que filtran;
  - pestañas que se arrastran, ocultan y llevan etiqueta/nota de sesión;
  - selector 1º / 2º / 3º / Día;
  - bloques Entradas / Salidas plegables;
  - ocultar filas (una o varias) con confirmación y Deshacer;
  - "sale en X min", destello al cambiar un dato y barra horizontal fija;
  - `probar.sh --reiniciar-datos`.
  - Bugs encontrados al probar con Playwright y corregidos:
    - la página se desplazaba sola al cargar;
    - los encabezados de grupo salían en rojo (CSS de Tabulator);
    - la numeración se saltaba las filas ocultas;
    - el botón de ocultar estaba encima del número (elegir la fila la ocultaba);
    - el conteo de columnas incluía la de números de fila;
    - el azul fuerte de la selección no dejaba leer;
    - "1 servicios".
- Le mandé capturas: oscuro, claro y media pantalla.

### Tramo 9 — Cierre
- Arreglos de Codespaces (502 y contraseñas) descritos arriba.
- El usuario logró entrar y dijo que el diseño "es mucho mejor".
- Pidió poder simular un turno con modificaciones para su jefe. Propuse el plan D → B → E → C y
  quedé esperando su respuesta (sección 1).
- Pidió este HANDOFF.
