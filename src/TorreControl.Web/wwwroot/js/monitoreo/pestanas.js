import { el, leerGuardado, guardar } from './util.js';

const CLAVE_ORDEN = 'tc-orden-plantas';

// Pestañas de planta con contador de servicios. Se reordenan arrastrando (por ejemplo, para
// empezar por la planta con la que arranca el turno). El orden vale SOLO durante la sesión del
// navegador (sessionStorage), no se guarda en la base de datos.
export class Pestanas {
    constructor({ alElegir }) {
        this.contenedor = document.getElementById('tc-pestanas');
        this.alElegir = alElegir;
        this.activa = null;
        this.arrastrada = null;

        this.contenedor.addEventListener('keydown', evento => this.teclado(evento));
    }

    ordenar(plantas) {
        const orden = leerGuardado(sessionStorage, CLAVE_ORDEN, []);
        const posicion = id => {
            const i = orden.indexOf(id);
            return i === -1 ? Number.MAX_SAFE_INTEGER : i;
        };
        return [...plantas].sort((a, b) => posicion(a.id) - posicion(b.id));
    }

    pintar(plantas, activaPedida) {
        const ordenadas = this.ordenar(plantas);
        const ids = ordenadas.map(p => p.id);
        this.activa = ids.includes(activaPedida) ? activaPedida : (ids.includes(this.activa) ? this.activa : ids[0] ?? null);

        this.contenedor.replaceChildren(...ordenadas.map(planta => this.pestana(planta)));
        return this.activa;
    }

    pestana(planta) {
        const seleccionada = planta.id === this.activa;
        const boton = el('button', 'tc-pestana');
        boton.type = 'button';
        boton.role = 'tab';
        boton.draggable = true;
        boton.dataset.id = planta.id;
        boton.setAttribute('aria-selected', String(seleccionada));
        boton.tabIndex = seleccionada ? 0 : -1;
        boton.title = `${planta.cliente} · ${planta.nombre} — arrastra para reordenar`;
        boton.append(el('span', null, planta.nombre), el('span', 'tc-cliente', planta.cliente), el('span', 'tc-contador', planta.servicios.length));

        boton.addEventListener('click', () => this.elegir(planta.id));

        // Reordenar arrastrando.
        boton.addEventListener('dragstart', evento => {
            this.arrastrada = boton;
            boton.classList.add('arrastrando');
            evento.dataTransfer.effectAllowed = 'move';
        });
        boton.addEventListener('dragend', () => {
            boton.classList.remove('arrastrando');
            this.contenedor.querySelectorAll('.destino-arrastre').forEach(p => p.classList.remove('destino-arrastre'));
        });
        boton.addEventListener('dragover', evento => {
            evento.preventDefault();
            boton.classList.add('destino-arrastre');
        });
        boton.addEventListener('dragleave', () => boton.classList.remove('destino-arrastre'));
        boton.addEventListener('drop', evento => {
            evento.preventDefault();
            boton.classList.remove('destino-arrastre');
            if (this.arrastrada && this.arrastrada !== boton) {
                this.contenedor.insertBefore(this.arrastrada, boton);
                this.guardarOrden();
            }
        });

        return boton;
    }

    guardarOrden() {
        const orden = [...this.contenedor.children].map(p => Number(p.dataset.id));
        guardar(sessionStorage, CLAVE_ORDEN, orden);
    }

    elegir(id) {
        this.activa = id;
        this.contenedor.querySelectorAll('.tc-pestana').forEach(p => {
            const seleccionada = Number(p.dataset.id) === id;
            p.setAttribute('aria-selected', String(seleccionada));
            p.tabIndex = seleccionada ? 0 : -1;
        });
        this.alElegir(id);
    }

    /** Flechas izquierda/derecha para cambiar de planta; Ctrl+flecha mueve la pestaña. */
    teclado(evento) {
        if (!['ArrowLeft', 'ArrowRight'].includes(evento.key)) {
            return;
        }
        const pestanas = [...this.contenedor.children];
        const actual = pestanas.findIndex(p => Number(p.dataset.id) === this.activa);
        const paso = evento.key === 'ArrowRight' ? 1 : -1;
        const siguiente = pestanas[(actual + paso + pestanas.length) % pestanas.length];
        if (!siguiente) {
            return;
        }
        evento.preventDefault();

        if (evento.ctrlKey) {
            const referencia = paso > 0 ? siguiente.nextSibling : siguiente;
            this.contenedor.insertBefore(pestanas[actual], referencia);
            this.guardarOrden();
            pestanas[actual].focus();
        } else {
            this.elegir(Number(siguiente.dataset.id));
            siguiente.focus();
        }
    }
}
