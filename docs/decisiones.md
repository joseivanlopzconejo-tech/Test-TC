# Decisiones del proyecto

Registro de lo acordado con el responsable del proyecto. Cuando una decisión cambia algo de la
especificación original (CLAUDE.md, privada), manda lo que está aquí. Cada punto indica en qué
pieza o fase se implementa.

## Fase 1 (cerradas)

- Módulos: versión mínima `Modulo` / `ModuloPlanta` / `ModuloUsuario` con `FechaInicio` y `FechaFin`.
  El diseño completo (lunes a viernes, guardias por turno) es de la Fase 3.
- Supervisores, Jefe de Monitoreo y Sistemas ven todas las plantas; el monitorista solo las de sus
  módulos vigentes.
- Solo se pueden asignar roles estrictamente menores al propio.
- Bloqueo de cuenta: 5 intentos / 15 min. Límite por IP generoso. El login nunca revela si un
  número de empleado existe.
- La sesión es de navegador (muere al cerrarlo) y deslizante de 8 h, con aviso antes de vencer.
  Las actualizaciones automáticas de la tabla no renuevan la sesión.
- Fechas de negocio en hora de CDMX; Auditoría en UTC.

## Tabla del monitorista (Fase 2)

### Turnos — Pieza A
- **Fijos**: 1º 00:00–08:00, 2º 08:00–16:00, 3º 16:00–00:00. No se editan.
- Al entrar, la tabla abre en el turno de la hora actual. Selector de turno (1º · 2º · 3º · Día
  completo) con contador de servicios. Se elige también el día.
- El turno de un servicio se asigna por su Hip.

### Mixtos — Pieza A (marcado en Fase 3)
- Un servicio es **mixto porque el supervisor lo marca así** al agregarlo (turno 1º, 2º, 3º o
  mixto). Ya **no** se detecta por horario.
- Los mixtos de una planta van en **su propia pestaña** junto a la de la planta
  (ej. `DANONE` · `DANONE MIXTO`).

### Pestañas de planta — Pieza A
- Muestran el **nombre corto**; al elegirla, arriba aparece el nombre completo.
- Se pueden arrastrar, ocultar (si la planta no se activó o ya terminó) y ponerles etiqueta de
  color y nota. Orden, ocultas, etiquetas y notas valen **solo durante la sesión**.
- Pestañas, selector de turno, búsqueda y encabezados de la tabla quedan **fijos arriba** al bajar.

### Tabla — Pieza A
- Página larga tipo Excel: se ven **todos** los servicios, sin caja de alto fijo.
- **Sin flechas de orden**: siempre por hora de salida.
- Dos bloques plegables: primero **Entradas**, luego **Salidas**.
- Numeración de filas a la izquierda. Ocultar una fila (o varias seleccionadas, como en Excel)
  **registra "no operó hoy"**: lo ven todos, sale de reportes, queda en Auditoría y se puede
  reactivar.
- Orden de columnas: Ruta · Unidad · Estatus (congeladas) | Filtros 1–4 | Hora de inicio (prog.,
  real, dif.) | Hora de llegada (prog., real, dif.) | Aforo | Observaciones | Operador.
- Diferencias (inicio y llegada): en hora o antes = verde; 1–10 min tarde = naranja;
  más de 10 = rojo.
- Diseño con tonos pastel, movimiento sutil y tarjetas de resumen. Tema oscuro y claro.

### Estatus — Pieza A (cálculo) / Pieza E (captura)
- La columna Estatus **se calcula sola**, nunca se captura:

  | Servicio | Antes de salir | Al capturar Hir | Al capturar Htr |
  |---|---|---|---|
  | Entrada (E) | Estatus del último filtro capturado | EN RUTA (azul) | EN PLANTA (verde) |
  | Salida (S) | Sin acción | EN SALIDA (naranja) | RESGUARDO (gris azulado) |

- Estatus de los filtros (catálogo editable en Fase 3): Apagada, Encendida, En base, Trayecto,
  Detenida, Pérdida de cobertura, Falla GPS, Sin respuesta del operador, Falla mecánica.

### Filtros — Pieza E
- Solo servicios de entrada; en salidas las celdas quedan en blanco.
- Doble clic → ventana: Hora (bloqueada), Estatus, Observación (opcional), Cancelar / Guardar.
- F1–F3 se capturan **una vez** y quedan fijos. F4 permite hasta 3 modificaciones.

### Unidad y apoyos — Pieza C
- La celda muestra la titular y `+N` apoyos. **Solo el botón de camión** abre la ventana
  (cambiar titular, hasta 3 apoyos). Sin doble clic en esa celda.
- Agregar apoyos deja una nota automática en la bitácora.

### Horas de inicio y llegada — Pieza D
- Doble clic para capturar (24 h). Aviso de "hora sospechosa" (más de 20 min): Cancelar /
  Modificar / Correcta.
- La hora de **inicio no pide motivo ni observación**.
- Ya no se escriben textos de "sin novedad": la columna Observaciones solo muestra incidentes y
  notas (queda limpia cuando todo va a tiempo).
- La **llegada** con más de 10 min de retraso pide **motivo** (Tráfico, Cruce de tren, Accidente
  vial, Bloqueo o manifestación, Clima, Falla mecánica, Operador, Cliente/planta, Otro) y
  observación → queda como INCIDENTE.

### Aforo — Pieza B
- Doble clic → una ventana con **Usuarios de entrada** y **Usuarios de salida** de la ruta; cada
  número se guarda en su servicio. Entero de **1 a 49**, validado en el servidor.

### Observaciones (bitácora) — Pieza F
- Bitácora con hora ("14:05 — …"): notas manuales (doble clic), incidentes de llegada y notas
  automáticas (ej. apoyos agregados). Le llega al cliente en reportes.

### Capturas — Pieza G
- **Captura rápida** (PNG): Ruta, Unidad y Estatus de lo visible, sin filas ocultas.
- **Reporte de turno** (PNG): Ruta, Unidad, Estatus, horas con diferencias, aforo de entrada y
  salida en la misma fila de la ruta, e incidencias detalladas (motivo y observación).
- "Agregar servicio" solo para Supervisores, Jefe y Sistemas (Fase 3). "Exportar" = Excel AFORO
  (Fase 4).

### Fuera de alcance por ahora
- Columna "Km".
