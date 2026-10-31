import { el, leerGuardado, guardar } from './util.js';

// Pestañas de planta (y de sus MIXTOS). Cada monitorista puede, solo durante su sesión:
// - reordenarlas arrastrando (o Ctrl + flecha), para empezar por la planta con la que arranca;
// - ocultarlas (si la planta no se activó o ya terminó) y volver a mostrarlas;
// - ponerles una etiqueta de color y una nota (ej. "inicia 05:30"), como los grupos de pestañas
//   que usan en el navegador.
// Todo vive en sessionStorage: no se guarda en la base de datos.

const CLAVE_ORDEN = 'tc-pestanas-orden';
const CLAVE_OCULTAS = 'tc-pestanas-ocultas';
const CLAVE_ETIQUETAS = 'tc-pestanas-etiquetas';

const COLORES = ['#8ec5ff', '#9fe2bf', '#ffc48c', '#f4a6a6', '#f6d88a', '#cdb8f2'];

const ICONO_ETIQUETA = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M20.6 13.4 13.4 20.6a2 2 0 0 1-2.8 0L3 13V3h10l7.6 7.6a2 2 0 0 1 0 2.8Z"/><circle cx="7.5" cy="7.5" r="1.5"/></svg>';
const ICONO_OCULTAR = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><path d="M18 6 6 18M6 6l12 12"/></svg>';

/** Botón pequeño con ícono (SVG fijo, no viene de datos). */
function boton(clase, icono, titulo) {
    const b = el('button', clase);
    b.type = 'button';
    b.title = titulo;
    b.setAttribute('aria-label', titulo);
    b.innerHTML = icono;
    return b;
}

export class Pestanas {
    constructor({ alElegir }) {
        this.contenedor = document.getElementById('tc-pestanas');
        this.botonOcultas = document.getElementById('tc-boton-ocultas');
        this.menuOcultas = document.getElementById('tc-menu-ocultas');
        this.editor = document.getElementById('tc-editor-pestana');
        this.alElegir = alElegir;
        this.activa = null;
        this.grupos = [];
        this.turno = 0;
        this.arrastrada = null;

        this.contenedor.addEventListener('keydown', evento => this.teclado(evento));
        document.addEventListener('click', evento => {
            if (!this.editor.hidden && !this.editor.contains(evento.target) && !evento.target.closest('.tc-pestana-accion')) {
                this.editor.hidden = true;
            }
        });
    }

    get ocultas() { return new Set(leerGuardado(sessionStorage, CLAVE_OCULTAS, [])); }
    get etiquetas() { return leerGuardado(sessionStorage, CLAVE_ETIQUETAS, {}); }

    /** Pinta las pestañas y regresa la clave de la que queda elegida. */
    pintar(grupos, activaPedida, turno) {
        this.grupos = grupos;
        this.turno = turno;
        const ocultas = this.ocultas;
        const orden = leerGuardado(sessionStorage, CLAVE_ORDEN, []);
        const posicion = clave => {
            const i = orden.indexOf(clave);
            return i === -1 ? Number.MAX_SAFE_INTEGER : i;
        };

        // Orden de la sesión; lo nuevo, al final y en el orden del servidor (la de MIXTO junto a su planta).
        const visibles = grupos
            .map((g, i) => ({ g, i }))
            .filter(({ g }) => !ocultas.has(g.clave))
            .sort((a, b) => posicion(a.g.clave) - posicion(b.g.clave) || a.i - b.i)
            .map(({ g }) => g);

        const claves = visibles.map(g => g.clave);
        this.activa = [activaPedida, this.activa].find(c => claves.includes(c)) ?? claves[0] ?? null;

        this.contenedor.replaceChildren(...visibles.map(g => this.pestana(g)));
        this.pintarOcultas(grupos.filter(g => ocultas.has(g.clave)));
        return this.activa;
    }

    pestana(grupo) {
        const seleccionada = grupo.clave === this.activa;
        const etiqueta = this.etiquetas[grupo.clave];

        const p = el('div', `tc-pestana${grupo.esMixto ? ' mixto' : ''}${etiqueta?.color ? ' con-etiqueta' : ''}`);
        p.setAttribute('role', 'tab');
        p.setAttribute('aria-selected', String(seleccionada));
        p.tabIndex = seleccionada ? 0 : -1;
        p.draggable = true;
        p.dataset.clave = grupo.clave;
        p.title = `${grupo.nombreCompleto}${grupo.esMixto ? ' · MIXTO' : ''}${etiqueta?.nota ? ` — ${etiqueta.nota}` : ''}\nArrastra para reordenar`;
        if (etiqueta?.color) {
            p.style.setProperty('--etiqueta', etiqueta.color);
        }

        const textos = el('span', 'tc-pestana-textos');
        const nombre = el('span', null, grupo.nombreCorto);
        if (grupo.esMixto) {
            nombre.append(' ', el('span', 'tc-etiqueta tc-etiqueta-mixto', 'MIXTO'));
        }
        textos.append(nombre);
        if (etiqueta?.nota) {
            textos.append(el('span', 'tc-pestana-nota', etiqueta.nota));
        }

        const accionEtiqueta = boton('tc-pestana-accion', ICONO_ETIQUETA, 'Etiqueta y nota');
        accionEtiqueta.tabIndex = -1;
        accionEtiqueta.addEventListener('click', evento => {
            evento.stopPropagation();
            this.abrirEditor(grupo, accionEtiqueta);
        });

        const accionOcultar = boton('tc-pestana-accion', ICONO_OCULTAR, 'Ocultar esta pestaña');
        accionOcultar.tabIndex = -1;
        accionOcultar.addEventListener('click', evento => {
            evento.stopPropagation();
            this.ocultar(grupo.clave);
        });

        p.append(textos, el('span', 'tc-contador', grupo.conteos?.[this.turno] ?? 0), accionEtiqueta, accionOcultar);
        p.addEventListener('click', () => this.elegir(grupo.clave));

        // Reordenar arrastrando.
        p.addEventListener('dragstart', evento => {
            this.arrastrada = p;
            p.classList.add('arrastrando');
            evento.dataTransfer.effectAllowed = 'move';
        });
        p.addEventListener('dragend', () => {
            p.classList.remove('arrastrando');
            this.contenedor.querySelectorAll('.destino-arrastre').forEach(x => x.classList.remove('destino-arrastre'));
        });
        p.addEventListener('dragover', evento => {
            evento.preventDefault();
            p.classList.add('destino-arrastre');
        });
        p.addEventListener('dragleave', () => p.classList.remove('destino-arrastre'));
        p.addEventListener('drop', evento => {
            evento.preventDefault();
            p.classList.remove('destino-arrastre');
            if (this.arrastrada && this.arrastrada !== p) {
                this.contenedor.insertBefore(this.arrastrada, p);
                this.guardarOrden();
            }
        });

        return p;
    }

    guardarOrden() {
        guardar(sessionStorage, CLAVE_ORDEN, [...this.contenedor.children].map(p => p.dataset.clave));
    }

    elegir(clave) {
        this.activa = clave;
        this.contenedor.querySelectorAll('.tc-pestana').forEach(p => {
            const seleccionada = p.dataset.clave === clave;
            p.setAttribute('aria-selected', String(seleccionada));
            p.tabIndex = seleccionada ? 0 : -1;
        });
        this.contenedor.querySelector(`[data-clave="${CSS.escape(clave)}"]`)?.scrollIntoView({ block: 'nearest', inline: 'nearest', behavior: 'smooth' });
        this.alElegir(clave);
    }

    ocultar(clave) {
        const ocultas = this.ocultas;
        ocultas.add(clave);
        guardar(sessionStorage, CLAVE_OCULTAS, [...ocultas]);
        const activa = clave === this.activa ? null : this.activa;
        this.alElegir(this.pintar(this.grupos, activa, this.turno));
        window.tc.toast('Pestaña oculta. La recuperas en el botón de pestañas ocultas.');
    }

    mostrar(clave) {
        const ocultas = this.ocultas;
        ocultas.delete(clave);
        guardar(sessionStorage, CLAVE_OCULTAS, [...ocultas]);
        this.pintar(this.grupos, clave, this.turno);
        this.elegir(clave);
    }

    /** Botón "N ocultas" con la lista para volver a mostrarlas. */
    pintarOcultas(ocultas) {
        this.botonOcultas.hidden = ocultas.length === 0;
        this.botonOcultas.textContent = `${ocultas.length} oculta${ocultas.length === 1 ? '' : 's'}`;
        this.menuOcultas.replaceChildren(...ocultas.map(g => {
            const b = el('button', null, `Mostrar ${g.nombreCorto}${g.esMixto ? ' MIXTO' : ''}`);
            b.type = 'button';
            b.addEventListener('click', () => this.mostrar(g.clave));
            return b;
        }));
    }

    /** Editor de etiqueta: color y nota corta. */
    abrirEditor(grupo, ancla) {
        const etiquetas = this.etiquetas;
        const actual = etiquetas[grupo.clave] ?? {};
        let color = actual.color ?? null;

        const titulo = el('h3', null, `${grupo.nombreCorto}${grupo.esMixto ? ' MIXTO' : ''}`);
        const colores = el('div', 'tc-colores');
        const muestras = [null, ...COLORES].map(c => {
            const m = el('button', `tc-color${c ? '' : ' tc-color-ninguno'}`);
            m.type = 'button';
            m.title = c ? 'Color de etiqueta' : 'Sin color';
            m.setAttribute('aria-pressed', String(c === color));
            if (c) m.style.setProperty('--muestra', c);
            m.addEventListener('click', () => {
                color = c;
                muestras.forEach(x => x.setAttribute('aria-pressed', String(x === m)));
            });
            return m;
        });
        colores.append(...muestras);

        const nota = el('input');
        nota.type = 'text';
        nota.maxLength = 40;
        nota.placeholder = 'Nota (ej. inicia 05:30)';
        nota.value = actual.nota ?? '';

        const guardarBoton = el('button', 'tc-boton-principal', 'Guardar');
        guardarBoton.type = 'button';
        const quitarBoton = el('button', 'tc-boton-suave', 'Quitar');
        quitarBoton.type = 'button';
        const botones = el('div', 'tc-editor-botones');
        botones.append(quitarBoton, guardarBoton);

        const aplicar = valor => {
            const todas = this.etiquetas;
            if (valor) {
                todas[grupo.clave] = valor;
            } else {
                delete todas[grupo.clave];
            }
            guardar(sessionStorage, CLAVE_ETIQUETAS, todas);
            this.editor.hidden = true;
            this.pintar(this.grupos, this.activa, this.turno);
        };
        guardarBoton.addEventListener('click', () => aplicar(color || nota.value.trim() ? { color, nota: nota.value.trim() } : null));
        quitarBoton.addEventListener('click', () => aplicar(null));
        nota.addEventListener('keydown', evento => {
            if (evento.key === 'Enter') guardarBoton.click();
            if (evento.key === 'Escape') this.editor.hidden = true;
        });

        this.editor.replaceChildren(titulo, colores, nota, botones);
        this.editor.hidden = false;
        const r = ancla.getBoundingClientRect();
        this.editor.style.left = `${Math.max(8, Math.min(r.left - 120, window.innerWidth - this.editor.offsetWidth - 8))}px`;
        this.editor.style.top = `${r.bottom + 8}px`;
        nota.focus();
    }

    /** Flechas izquierda/derecha para cambiar de planta; Ctrl+flecha mueve la pestaña. */
    teclado(evento) {
        if (!['ArrowLeft', 'ArrowRight'].includes(evento.key) || !evento.target.classList.contains('tc-pestana')) {
            return;
        }
        const pestanas = [...this.contenedor.children];
        const actual = pestanas.findIndex(p => p.dataset.clave === this.activa);
        const paso = evento.key === 'ArrowRight' ? 1 : -1;
        const siguiente = pestanas[(actual + paso + pestanas.length) % pestanas.length];
        if (!siguiente) {
            return;
        }
        evento.preventDefault();

        if (evento.ctrlKey) {
            this.contenedor.insertBefore(pestanas[actual], paso > 0 ? siguiente.nextSibling : siguiente);
            this.guardarOrden();
            pestanas[actual].focus();
        } else {
            this.elegir(siguiente.dataset.clave);
            siguiente.focus();
        }
    }
}
