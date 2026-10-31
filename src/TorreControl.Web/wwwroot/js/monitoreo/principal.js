import { obtenerTabla, marcarNoOpero, SesionVencida } from './api.js';
import { confirmar } from './dialogo.js';
import { FILTROS } from './filtros.js';
import { Pestanas } from './pestanas.js';
import { Reloj } from './reloj.js';
import { Resumen } from './resumen.js';
import { TablaServicios } from './tabla.js';
import { SelectorTurno } from './turnos.js';
import { el, formatoHora } from './util.js';

// Orquesta la pantalla: pide los datos, pinta pestañas, resumen y tabla, y actualiza cada 30 s.
// La vista (día, turno y pestaña) vive en la dirección de la página: si la sesión vence, al
// volver a entrar se regresa exactamente a lo mismo.

const SEGUNDOS_ENTRE_ACTUALIZACIONES = 30;

const estado = document.getElementById('tc-estado');
const vacio = document.getElementById('tc-vacio');
const inputDia = document.getElementById('tc-dia');
const botonVerOcultas = document.getElementById('tc-ver-ocultas');
const botonMarcar = document.getElementById('tc-marcar-no-opero');
const bloqueFijo = document.getElementById('tc-fijo');
const reloj = new Reloj(document.getElementById('tc-reloj'));

let datos = null;
let tabla = null;
let actualizando = false;
let pendiente = false;
let seleccion = [];

const parametros = new URLSearchParams(location.search);
const vista = {
    // null = hoy / turno en curso: la tabla "sigue" sola al turno que corre.
    dia: parametros.get('dia'),
    turno: parametros.has('turno') ? Number(parametros.get('turno')) : null,
    grupo: parametros.get('planta'),
    filtro: 'todos',
    verOcultas: false
};

function guardarVistaEnLaDireccion() {
    const p = new URLSearchParams();
    if (vista.dia) p.set('dia', vista.dia);
    if (vista.turno !== null) p.set('turno', vista.turno);
    if (vista.grupo) p.set('planta', vista.grupo);
    history.replaceState(null, '', `${location.pathname}${p.size ? `?${p}` : ''}`);
}

// ---------- Componentes ----------

const pestanas = new Pestanas({
    alElegir: clave => {
        vista.grupo = clave;
        guardarVistaEnLaDireccion();
        tabla?.olvidarCambios();
        mostrarGrupo();
    }
});

const selectorTurno = new SelectorTurno({
    alElegir: turno => {
        // Elegir el turno que corre ahora (en el día de hoy) vuelve al modo "seguir el turno".
        vista.turno = !vista.dia && turno === datos?.turnoActual ? null : turno;
        tabla?.olvidarCambios();
        cargar(false);
    }
});

const resumen = new Resumen({
    alFiltrar: filtro => {
        vista.filtro = vista.filtro === filtro ? 'todos' : filtro; // segundo clic quita el filtro
        resumen.marcar(vista.filtro);
        tabla?.filtrar(vista.filtro);
    }
});

inputDia.addEventListener('change', () => {
    vista.dia = inputDia.value || null;
    tabla?.olvidarCambios();
    cargar(false);
});

botonVerOcultas.addEventListener('click', () => {
    vista.verOcultas = !vista.verOcultas;
    tabla?.mostrarOcultas(vista.verOcultas);
    pintarBotonOcultas();
});

botonMarcar.addEventListener('click', () => ocultarFilas(seleccion, true));

// El encabezado de la tabla se pega justo debajo del bloque fijo: se mide su alto.
new ResizeObserver(() => {
    document.querySelector('.tc-monitoreo').style.setProperty('--tc-alto-fijo', `${bloqueFijo.offsetHeight}px`);
}).observe(bloqueFijo);

// Sombra en el bloque fijo cuando ya se despegó de la portada.
new IntersectionObserver(([entrada]) => bloqueFijo.classList.toggle('despegado', !entrada.isIntersecting))
    .observe(document.querySelector('.tc-portada'));

// ---------- Pintar ----------

function grupoActivo() {
    return datos?.grupos.find(g => g.clave === vista.grupo) ?? null;
}

function mostrarGrupo() {
    const grupo = grupoActivo();
    const servicios = grupo?.servicios ?? [];
    const operan = servicios.filter(s => !s.noOpero);

    // Portada: nombre completo de la planta elegida.
    document.getElementById('tc-planta-cliente').textContent = grupo?.cliente ?? '';
    document.getElementById('tc-planta-municipio').textContent = grupo?.municipio ?? '';
    const titulo = document.getElementById('tc-planta-nombre');
    titulo.replaceChildren(grupo ? grupo.nombreCompleto.split(' · ').pop() : 'Sin plantas asignadas');
    if (grupo?.esMixto) {
        titulo.append(el('span', 'tc-etiqueta tc-etiqueta-mixto', 'MIXTO'));
    }

    selectorTurno.pintar(grupo?.conteos, datos.ventana.turno, datos.turnoActual);
    resumen.actualizar(operan);
    tabla.mostrar(servicios);
    pintarBotonOcultas();

    vacio.hidden = Boolean(grupo);
    vacio.textContent = 'No tienes plantas asignadas hoy. Pide a tu supervisor que te asigne un módulo.';
}

function pintarBotonOcultas() {
    const ocultas = (grupoActivo()?.servicios ?? []).filter(s => s.noOpero).length;
    botonVerOcultas.hidden = ocultas === 0;
    botonVerOcultas.setAttribute('aria-pressed', String(vista.verOcultas));
    botonVerOcultas.textContent = vista.verOcultas ? `Ocultar las ${ocultas} que no operaron` : `${ocultas} no operó · Ver`;
}

async function cargar(automatica) {
    // Si llega otra petición mientras se carga (ej. cambiar rápido de turno), se repite al final.
    if (actualizando) {
        pendiente = true;
        return;
    }
    actualizando = true;
    try {
        datos = await obtenerTabla({ dia: vista.dia, turno: vista.turno }, automatica);
        reloj.sincronizar(datos.ahoraUtc);
        inputDia.value = datos.ventana.dia;

        tabla ??= new TablaServicios({
            reglas: datos.reglas,
            ahoraNegocio: () => reloj.ahoraNegocio(),
            alOcultar: ocultarFilas,
            alSeleccionar: ids => {
                seleccion = ids;
                botonMarcar.hidden = ids.length < 2;
                botonMarcar.textContent = `Marcar ${ids.length} como "no operó"`;
            }
        });

        vista.grupo = pestanas.pintar(datos.grupos, vista.grupo, datos.ventana.turno);
        guardarVistaEnLaDireccion();
        mostrarGrupo();

        estado.classList.remove('error');
        estado.textContent = `Actualizado ${formatoHora.format(reloj.ahora())}`;
    } catch (error) {
        estado.classList.add('error');
        if (error instanceof SesionVencida) {
            estado.textContent = 'Tu sesión terminó. Vuelve a iniciar sesión.';
        } else {
            estado.textContent = 'Sin conexión con el servidor. Reintentando…';
            if (!automatica) {
                window.tc.toast('No se pudo cargar la tabla. Se reintentará en unos segundos.', 'error');
            }
        }
    } finally {
        actualizando = false;
        if (pendiente) {
            pendiente = false;
            cargar(false);
        }
    }
}

/** "No operó hoy": confirma, guarda en el servidor y ofrece deshacer. */
async function ocultarFilas(ids, noOpero) {
    if (ids.length === 0) {
        return;
    }
    const varios = ids.length > 1;
    if (noOpero) {
        const ok = await confirmar({
            titulo: varios ? `¿Marcar ${ids.length} servicios como "no operó hoy"?` : '¿Marcar este servicio como "no operó hoy"?',
            texto: 'Se ocultará para todos, no contará en reportes y quedará registrado en la auditoría. Puedes reactivarlo después.',
            aceptar: varios ? `Marcar ${ids.length}` : 'Marcar'
        });
        if (!ok) {
            return;
        }
    }

    try {
        await marcarNoOpero(ids, noOpero);
        await cargar(false);
        const texto = noOpero
            ? `${ids.length} ${varios ? 'servicios marcados' : 'servicio marcado'} como "no operó".`
            : `${ids.length} ${varios ? 'servicios reactivados' : 'servicio reactivado'}.`;
        window.tc.toast(texto, 'exito', noOpero ? { texto: 'Deshacer', alHacer: () => ocultarFilas(ids, false) } : null);
    } catch {
        window.tc.toast('No se pudo guardar el cambio. Intenta de nuevo.', 'error');
    }
}

// ---------- Actualización periódica ----------

// Respaldo de actualización en vivo (SignalR llega en una pieza posterior de la Fase 2).
setInterval(() => {
    if (document.visibilityState === 'visible') {
        cargar(true);
    }
}, SEGUNDOS_ENTRE_ACTUALIZACIONES * 1000);

document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'visible') {
        cargar(true);
    }
});

// Filtro inicial y primera carga.
resumen.marcar(vista.filtro);
if (!FILTROS[vista.filtro]) vista.filtro = 'todos';
cargar(false);
