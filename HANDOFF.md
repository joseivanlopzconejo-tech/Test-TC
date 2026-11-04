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
