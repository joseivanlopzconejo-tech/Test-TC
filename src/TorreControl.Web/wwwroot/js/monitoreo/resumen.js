import { FILTROS } from './filtros.js';

// Tarjetas de resumen de la pestaña elegida. Un clic en una tarjeta filtra la tabla (la tarjeta
// activa queda resaltada). Los números "brincan" cuando cambian, para notar el movimiento.

export class Resumen {
    constructor({ alFiltrar }) {
        this.tarjetas = [...document.querySelectorAll('.tc-tarjeta-kpi')];
        this.relleno = document.getElementById('tc-avance-relleno');
        this.porcentaje = document.getElementById('tc-avance-porcentaje');
        this.tarjetas.forEach(tarjeta => tarjeta.addEventListener('click', () => alFiltrar(tarjeta.dataset.filtro)));
    }

    marcar(filtro) {
        this.tarjetas.forEach(t => t.classList.toggle('activo', t.dataset.filtro === filtro));
    }

    /** filas: servicios que operan (sin los "no operó"). */
    actualizar(filas) {
        Object.entries(FILTROS).forEach(([clave, condicion]) => {
            const destino = document.querySelector(`[data-kpi="${clave}"]`);
            if (destino) {
                this.animar(destino, filas.filter(condicion).length);
            }
        });

        const total = filas.length;
        const listos = filas.filter(FILTROS.finalizados).length;
        const avance = total ? Math.round((listos / total) * 100) : 0;
        this.relleno.style.width = `${avance}%`;
        this.porcentaje.textContent = `${avance}%`;
    }

    /** Cuenta de un número al otro en ~400 ms (y un pequeño brinco al terminar). */
    animar(destino, valor) {
        const inicial = Number(destino.textContent) || 0;
        if (inicial === valor) {
            return;
        }
        const reducido = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        const inicio = performance.now();
        const paso = ahora => {
            const t = reducido ? 1 : Math.min(1, (ahora - inicio) / 400);
            destino.textContent = Math.round(inicial + (valor - inicial) * (1 - (1 - t) ** 3));
            if (t < 1) {
                requestAnimationFrame(paso);
            } else {
                destino.classList.remove('cambio');
                void destino.offsetWidth; // reinicia la animación
                destino.classList.add('cambio');
            }
        };
        requestAnimationFrame(paso);
    }
}
