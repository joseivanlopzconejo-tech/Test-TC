import { obtenerTabla, SesionVencida } from './api.js';
import { BarraVentana } from './barra.js';
import { Pestanas } from './pestanas.js';
import { Reloj } from './reloj.js';
import { TablaServicios } from './tabla.js';
import { formatoHora } from './util.js';

// Orquesta la pantalla: pide los datos, pinta pestañas y tabla, y actualiza cada 30 s.
// La vista (día, horas y planta) vive en la dirección de la página: así, si la sesión vence,
// al volver a entrar se regresa exactamente a la misma vista.

const SEGUNDOS_ENTRE_ACTUALIZACIONES = 30;

const estado = document.getElementById('tc-estado');
const vacio = document.getElementById('tc-vacio');
const reloj = new Reloj(document.getElementById('tc-reloj'));

let datos = null;
let tabla = null;
let actualizando = false;

const parametros = new URLSearchParams(location.search);
const vista = {
    dia: parametros.get('dia'),
    desde: parametros.get('desde'),
    hasta: parametros.get('hasta'),
    planta: Number(parametros.get('planta')) || null
};

// Si el usuario no eligió una ventana, la tabla "sigue" al turno en curso: al cambiar de turno
// pasa sola a la nueva ventana (y los servicios MIXTOS siguen visibles porque cruzan ambas).
const ventanaElegida = () => Boolean(vista.dia && vista.desde && vista.hasta);

function guardarVistaEnLaDireccion() {
    const p = new URLSearchParams();
    if (ventanaElegida()) {
        p.set('dia', vista.dia);
        p.set('desde', vista.desde);
        p.set('hasta', vista.hasta);
    }
    if (vista.planta) {
        p.set('planta', vista.planta);
    }
    history.replaceState(null, '', `${location.pathname}${p.size ? `?${p}` : ''}`);
}

const barra = new BarraVentana({
    alAplicar: ventana => {
        Object.assign(vista, ventana);
        cargar(false);
    },
    alTurnoActual: () => {
        // Sin día ni horas, el servidor usa el turno en curso.
        Object.assign(vista, { dia: null, desde: null, hasta: null });
        cargar(false);
    }
});

const pestanas = new Pestanas({
    alElegir: id => {
        vista.planta = id;
        guardarVistaEnLaDireccion();
        mostrarPlanta();
    }
});

function mostrarPlanta() {
    const planta = datos?.plantas.find(p => p.id === vista.planta);
    const servicios = planta?.servicios ?? [];
    vacio.hidden = !planta || servicios.length > 0;
    tabla.mostrar(servicios);
}

async function cargar(automatica) {
    if (actualizando) {
        return;
    }
    actualizando = true;
    try {
        datos = await obtenerTabla(ventanaElegida() ? vista : {}, automatica);
        reloj.sincronizar(datos.ahoraUtc);
        // La barra muestra la ventana que realmente usó el servidor (la elegida o la del turno).
        barra.mostrar(datos.ventana);

        tabla ??= new TablaServicios(datos.reglas);
        vista.planta = pestanas.pintar(datos.plantas, vista.planta);
        guardarVistaEnLaDireccion();

        if (datos.plantas.length === 0) {
            vacio.textContent = 'No tienes plantas asignadas hoy. Pide a tu supervisor que te asigne un módulo.';
            vacio.hidden = false;
        }
        mostrarPlanta();

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
    }
}

// Respaldo de actualización en vivo (SignalR llega en una pieza posterior de la Fase 2).
setInterval(() => {
    if (document.visibilityState === 'visible') {
        cargar(true);
    }
}, SEGUNDOS_ENTRE_ACTUALIZACIONES * 1000);

// Al volver a la pestaña del navegador, actualizar de inmediato.
document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'visible') {
        cargar(true);
    }
});

cargar(false);
