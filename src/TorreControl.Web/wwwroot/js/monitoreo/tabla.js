import { el, leerGuardado, guardar } from './util.js';
import { FILTROS } from './filtros.js';

const CLAVE_COLUMNAS = 'tc-columnas-ocultas';
const CLAVE_DENSIDAD = 'tc-densidad';
const CLAVE_BLOQUES = 'tc-bloques-plegados';

const ICONO_OCULTAR = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M9.9 4.2A10 10 0 0 1 12 4c6 0 10 8 10 8a17 17 0 0 1-2.2 3.2M6.6 6.6A17 17 0 0 0 2 12s4 8 10 8a10 10 0 0 0 5.4-1.6"/><path d="M3 3l18 18M9.9 9.9a3 3 0 0 0 4.2 4.2"/></svg>';
const ICONO_REACTIVAR = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M3 12a9 9 0 1 0 3-6.7L3 8"/><path d="M3 3v5h5"/></svg>';

// ---------- Celdas ----------
// Cada "formateador" arma el contenido con elementos del DOM (nunca HTML hecho con datos).

/** Chip de estatus. El color va en la variable --chip y el CSS lo adapta al tema. */
function chipEstatus(estatus, texto) {
    if (!estatus) {
        return el('span', 'tc-chip tc-chip-vacio', texto ?? 'Sin acción');
    }
    const chip = el('span', 'tc-chip', texto ?? estatus.nombre);
    if (/^#[0-9a-f]{6}$/i.test(estatus.color)) {
        chip.style.setProperty('--chip', estatus.color);
    }
    return chip;
}

/** Minutos que faltan para la salida programada (negativo = ya debió salir). Solo informativo. */
function minutosParaSalir(fila, ahora) {
    const salida = new Date(`${fila.fecha}T${fila.hip}:00`);
    return Math.round((salida.getTime() - ahora.getTime()) / 60000);
}

function celdaRuta(ahoraNegocio) {
    return celda => {
        const fila = celda.getData();
        const caja = el('div', 'tc-ruta');
        const tipo = el('span', `tc-tipo tc-tipo-${fila.tipo}`, fila.tipo);
        tipo.title = fila.tipo === 'E' ? 'Entrada a planta' : 'Salida de planta';
        caja.append(tipo, el('span', 'tc-ruta-nombre', fila.ruta));

        if (fila.esEspecial) {
            caja.append(el('span', 'tc-etiqueta tc-etiqueta-especial', 'ESPECIAL'));
        }
        if (fila.cruzaMedianoche) {
            const noche = el('span', 'tc-etiqueta tc-etiqueta-medianoche', '+1');
            noche.title = 'Llega al día siguiente';
            caja.append(noche);
        }

        // Aviso de lo que está por salir (o debió salir y aún no tiene hora de inicio).
        if (!fila.hir && !fila.noOpero) {
            const faltan = minutosParaSalir(fila, ahoraNegocio());
            if (faltan >= 0 && faltan <= 30) {
                caja.append(el('span', 'tc-pronto', faltan === 0 ? 'sale ya' : `sale en ${faltan} min`));
            } else if (faltan < 0 && faltan >= -60) {
                const atrasado = el('span', 'tc-pronto atrasado', `sin inicio · ${-faltan} min`);
                atrasado.title = 'Ya pasó su hora de salida y no tiene hora de inicio capturada';
                caja.append(atrasado);
            }
        }
        return caja;
    };
}

function celdaUnidad(celda) {
    const fila = celda.getData();
    if (!fila.unidad) {
        return el('span', 'tc-nulo', '—');
    }
    const caja = el('span');
    caja.append(el('span', 'tc-unidad', fila.unidad));
    if (fila.apoyos.length > 0) {
        const apoyos = el('span', 'tc-apoyos', `+${fila.apoyos.length}`);
        apoyos.title = `Apoyo: ${fila.apoyos.join(', ')}`;
        caja.append(apoyos);
    }
    return caja;
}

function celdaTexto(celda) {
    const valor = celda.getValue();
    return valor === null || valor === undefined || valor === '' ? el('span', 'tc-nulo', '—') : el('span', null, valor);
}

/** Diferencia en minutos: a tiempo o antes = verde; hasta la tolerancia = naranja; más = rojo. */
function celdaDiferencia(reglas) {
    return celda => {
        const minutos = celda.getValue();
        if (minutos === null || minutos === undefined) {
            return el('span', 'tc-nulo', '—');
        }
        const clase = minutos <= 0 ? 'tc-dif-ok' : minutos <= reglas.toleranciaMin ? 'tc-dif-aviso' : 'tc-dif-alerta';
        return el('span', `tc-dif ${clase}`, minutos > 0 ? `+${minutos}` : String(minutos));
    };
}

function celdaFiltro(celda) {
    const filtro = celda.getValue();
    if (!filtro) {
        // Los servicios de salida no tienen filtros: la celda queda en blanco.
        return '';
    }
    const caja = el('span', 'tc-filtro-celda');
    const chip = chipEstatus(filtro.estatus, filtro.hora);
    chip.classList.add('tc-chip-hora');
    caja.append(chip);
    if (filtro.modificaciones > 0) {
        caja.append(el('span', 'tc-mods', `×${filtro.modificaciones}`));
    }
    caja.title = [filtro.estatus?.nombre ?? 'Sin acción', filtro.observacion].filter(Boolean).join(' · ');
    return caja;
}

function celdaIncidencias(celda) {
    const texto = celda.getValue();
    if (!texto) {
        return el('span', 'tc-nulo', '—');
    }
    const span = el('span', 'tc-incidencias', texto);
    span.title = texto;
    return span;
}

// ---------- Ficha del operador (ícono "i") ----------

const ficha = document.getElementById('tc-ficha-operador');

function abrirFicha(boton, operador) {
    const lista = el('dl');
    [['Clave', operador.clave], ['Nombre', operador.nombre], ['Supervisor', operador.supervisor], ['Coordinador', operador.coordinador]]
        .forEach(([etiqueta, valor]) => lista.append(el('dt', null, etiqueta), el('dd', null, valor || '—')));
    ficha.replaceChildren(lista);
    ficha.hidden = false;

    const r = boton.getBoundingClientRect();
    const ancho = ficha.offsetWidth;
    const alto = ficha.offsetHeight;
    ficha.style.left = `${Math.max(8, Math.min(r.left, window.innerWidth - ancho - 8))}px`;
    ficha.style.top = `${r.bottom + alto + 12 > window.innerHeight ? r.top - alto - 6 : r.bottom + 6}px`;
}

document.addEventListener('click', evento => {
    if (!ficha.hidden && !ficha.contains(evento.target) && !evento.target.closest('.tc-boton-info')) {
        ficha.hidden = true;
    }
});
document.addEventListener('keydown', evento => {
    if (evento.key === 'Escape') ficha.hidden = true;
});

function celdaOperador(celda) {
    const operador = celda.getData().operador;
    if (!operador) {
        return el('span', 'tc-nulo', '—');
    }
    const caja = el('span', 'tc-operador');
    const boton = el('button', 'tc-boton-info', 'i');
    boton.type = 'button';
    boton.setAttribute('aria-label', `Datos de ${operador.nombre}`);
    boton.addEventListener('click', evento => {
        evento.stopPropagation();
        abrirFicha(boton, operador);
    });
    caja.append(boton, el('span', 'tc-operador-nombre', operador.nombre));
    return caja;
}

// ---------- Copiar al portapapeles (Ctrl+C) ----------
// Al pegar en Excel u otra hoja deben llegar textos legibles, no objetos.

const copiar = {
    ruta: (_, f) => `${f.tipo} ${f.ruta}`,
    unidad: (_, f) => [f.unidad, ...f.apoyos].filter(Boolean).join(' + '),
    filtro: valor => (valor ? `${valor.hora} ${valor.estatus?.nombre ?? 'Sin acción'}` : ''),
    texto: valor => valor ?? ''
};

// ---------- Datos ----------

/** Convierte un servicio de la API en una fila plana para la tabla. */
function aFila(servicio) {
    const filtro = n => servicio.filtros.find(f => f.numero === n) ?? null;
    return {
        ...servicio,
        f1: filtro(1), f2: filtro(2), f3: filtro(3), f4: filtro(4),
        operadorNombre: servicio.operador?.nombre ?? '',
        estatusNombre: servicio.estatus?.nombre ?? '',
        // Orden fijo por hora real de salida (fecha + Hip): la lista siempre baja en orden.
        orden: `${servicio.fecha}T${servicio.hip}`
    };
}

/** "Huella" de una fila para saber si cambió entre una actualización y otra. */
const huella = fila => JSON.stringify([fila.hir, fila.htr, fila.estatus, fila.aforo, fila.unidad, fila.apoyos, fila.filtros, fila.incidencias, fila.noOpero]);

// ---------- Tabla ----------

export class TablaServicios {
    /**
     * reglas: { toleranciaMin } · ahoraNegocio: () => Date con la hora del servidor.
     * alOcultar(ids, noOpero): pide marcar o reactivar filas como "no operó".
     * alSeleccionar(ids): filas completas elegidas desde la numeración.
     */
    constructor({ reglas, ahoraNegocio, alOcultar, alSeleccionar }) {
        this.reglas = reglas;
        this.ahoraNegocio = ahoraNegocio;
        this.alOcultar = alOcultar;
        this.alSeleccionar = alSeleccionar;
        this.busqueda = '';
        this.filtroRapido = 'todos';
        this.verOcultas = false;
        this.filas = [];
        this.huellas = null;
        this.plegados = new Set(leerGuardado(sessionStorage, CLAVE_BLOQUES, []));

        const ocultas = new Set(leerGuardado(localStorage, CLAVE_COLUMNAS, []));
        const visible = campo => !ocultas.has(campo);
        const hora = (titulo, campo, extra = {}) => ({
            title: titulo, field: campo, formatter: celdaTexto, hozAlign: 'center', width: 78,
            accessorClipboard: copiar.texto, visible: visible(campo), ...extra
        });
        const filtro = n => ({
            title: `F${n}`, field: `f${n}`, formatter: celdaFiltro, hozAlign: 'center', width: n === 4 ? 112 : 96,
            accessorClipboard: copiar.filtro, visible: visible(`f${n}`)
        });
        const diferencia = campo => hora('Dif.', campo, { formatter: celdaDiferencia(this.reglas), width: 74 });

        this.tabla = new Tabulator('#tc-tabla', {
            index: 'id',
            layout: 'fitData',
            // Sin "height": la tabla crece con todos los servicios y la página baja como en Excel.
            placeholder: 'No hay servicios en este turno.',
            movableColumns: true,
            // Sin flechas de orden: siempre por hora de salida (y en bloques Entradas → Salidas).
            columnDefaults: { headerSort: false },
            initialSort: [{ column: 'orden', dir: 'asc' }],
            groupBy: 'tipo',
            groupValues: [['E', 'S']],
            groupStartOpen: valor => !this.plegados.has(valor),
            groupToggleElement: 'header',
            groupHeader: (valor, cantidad) => {
                const nombre = valor === 'E' ? 'Entradas' : 'Salidas';
                const ayuda = valor === 'E' ? 'llevan al personal a la planta' : 'regresan al personal';
                // HTML fijo + un número: no lleva datos escritos por usuarios.
                return `<span class="tc-bloque">${nombre}<span class="tc-bloque-cuenta">${Number(cantidad)} ${Number(cantidad) === 1 ? 'servicio' : 'servicios'}</span><span class="tc-bloque-ayuda">${ayuda}</span></span>`;
            },

            // Como hoja de cálculo: números de fila, celda activa con flechas, rangos con Shift,
            // y Ctrl+C para copiar. Clic en el número elige la fila completa.
            rowHeader: {
                formatter: celda => this.celdaNumero(celda), hozAlign: 'center', resizable: false,
                frozen: true, width: 62, headerSort: false
            },
            selectableRange: 1,
            selectableRangeColumns: true,
            selectableRangeRows: true,
            clipboard: 'copy',
            clipboardCopyRowRange: 'range',
            clipboardCopyConfig: { columnHeaders: true, columnGroups: false, rowHeaders: false, formatCells: false },

            rowFormatter: fila => {
                const datos = fila.getData();
                const elemento = fila.getElement();
                elemento.classList.toggle('tc-fila-no-opero', datos.noOpero);
                // Franja del color del estatus junto al número.
                elemento.style.setProperty('--franja', datos.estatus?.color ?? 'transparent');
            },

            // Orden de columnas acordado: identificación (congelada) → filtros → inicio → llegada →
            // aforo → observaciones → operador.
            columns: [
                { title: 'Ruta', field: 'ruta', frozen: true, formatter: celdaRuta(this.ahoraNegocio), minWidth: 230, accessorClipboard: copiar.ruta },
                { title: 'Unidad', field: 'unidad', frozen: true, formatter: celdaUnidad, width: 100, accessorClipboard: copiar.unidad },
                { title: 'Estatus', field: 'estatusNombre', frozen: true, formatter: c => chipEstatus(c.getData().estatus), width: 150, accessorClipboard: v => v || 'Sin acción' },
                { title: 'orden', field: 'orden', visible: false, clipboard: false },
                { title: 'Filtros de revisión', columns: [filtro(1), filtro(2), filtro(3), filtro(4)] },
                { title: 'Hora de inicio', columns: [hora('Prog.', 'hip'), hora('Real', 'hir'), diferencia('difIni')] },
                { title: 'Hora de llegada', columns: [hora('Prog.', 'htp'), hora('Real', 'htr'), diferencia('difFin')] },
                { title: 'Aforo', field: 'aforo', formatter: celdaTexto, hozAlign: 'center', width: 86, accessorClipboard: copiar.texto, visible: visible('aforo') },
                { title: 'Observaciones', field: 'incidencias', formatter: celdaIncidencias, minWidth: 320, accessorClipboard: copiar.texto, visible: visible('incidencias') },
                { title: 'Operador', field: 'operadorNombre', formatter: celdaOperador, minWidth: 190, accessorClipboard: copiar.texto, visible: visible('operadorNombre') }
            ]
        });

        this.lista = new Promise(resolver => this.tabla.on('tableBuilt', resolver));
        this.tabla.on('renderComplete', () => this.renumerar());
        this.tabla.on('rangeChanged', () => this.cambioDeSeleccion());
        this.tabla.on('groupVisibilityChanged', (grupo, visible) => {
            visible ? this.plegados.delete(grupo.getKey()) : this.plegados.add(grupo.getKey());
            guardar(sessionStorage, CLAVE_BLOQUES, [...this.plegados]);
        });
        this.prepararHerramientas();
        this.prepararScrollHorizontal();
    }

    /** Número de fila + botón para ocultarla ("no operó") o reactivarla. */
    celdaNumero(celda) {
        const fila = celda.getRow();
        const datos = fila.getData();
        const caja = el('span', 'tc-numero');
        caja.append(el('span', 'tc-numero-texto', fila.getPosition(true)));

        const boton = el('button', 'tc-numero-boton');
        boton.type = 'button';
        boton.innerHTML = datos.noOpero ? ICONO_REACTIVAR : ICONO_OCULTAR; // íconos fijos
        const titulo = datos.noOpero ? 'Reactivar (sí operó)' : 'Ocultar: no operó hoy';
        boton.title = titulo;
        boton.setAttribute('aria-label', `${titulo} · ${datos.ruta}`);
        // Que el clic no seleccione la fila (eso lo hace el número).
        boton.addEventListener('mousedown', evento => evento.stopPropagation());
        boton.addEventListener('click', evento => {
            evento.stopPropagation();
            // Si la fila es parte de una selección de varias filas, se aplica a todas.
            const elegidas = this.idsSeleccionados();
            const ids = elegidas.includes(datos.id) ? elegidas : [datos.id];
            this.alOcultar(ids, !datos.noOpero);
        });
        caja.append(boton);
        return caja;
    }

    async mostrar(servicios) {
        await this.lista;
        const anteriores = this.huellas;
        this.filas = servicios.map(aFila);
        this.huellas = new Map(this.filas.map(f => [f.id, huella(f)]));

        // replaceData conserva los filtros y bloques plegados. La posición de la PÁGINA se guarda y
        // se restaura: al redibujar, Tabulator enfoca la celda activa y el navegador saltaría a ella.
        const posicion = window.scrollY;
        await this.tabla.replaceData(this.filas);
        this.aplicarFiltros();
        window.scrollTo({ top: posicion, behavior: 'instant' });
        this.actualizarScrollHorizontal();

        // Movimiento con sentido: se iluminan solo las filas cuyos datos cambiaron.
        if (anteriores) {
            this.filas
                .filter(f => anteriores.has(f.id) && anteriores.get(f.id) !== this.huellas.get(f.id))
                .forEach(f => {
                    const elemento = this.tabla.getRow(f.id)?.getElement();
                    if (elemento) {
                        elemento.classList.remove('tc-cambio');
                        void elemento.offsetWidth;
                        elemento.classList.add('tc-cambio');
                    }
                });
        }
    }

    /** Al cambiar de pestaña no hay que "iluminar" nada: es otra lista. */
    olvidarCambios() {
        this.huellas = null;
    }

    filtrar(filtroRapido) {
        this.filtroRapido = filtroRapido;
        this.aplicarFiltros();
    }

    mostrarOcultas(ver) {
        this.verOcultas = ver;
        this.aplicarFiltros();
    }

    aplicarFiltros() {
        const texto = this.busqueda.trim().toLowerCase();
        const rapido = FILTROS[this.filtroRapido] ?? FILTROS.todos;
        this.tabla.setFilter(fila => {
            if (fila.noOpero) {
                return this.verOcultas;
            }
            if (!rapido(fila)) {
                return false;
            }
            return !texto || [fila.ruta, fila.unidad, fila.operadorNombre, ...fila.apoyos]
                .some(valor => (valor ?? '').toLowerCase().includes(texto));
        });
        requestAnimationFrame(() => this.renumerar());
    }

    /**
     * Numeración corrida (1, 2, 3…) en el orden en que se VEN las filas, como en Excel: sin
     * huecos por las filas ocultas o filtradas, y siguiendo de Entradas a Salidas.
     */
    renumerar() {
        this.tabla.element
            .querySelectorAll('.tabulator-row:not(.tabulator-group) .tc-numero-texto')
            .forEach((numero, i) => { numero.textContent = i + 1; });
    }

    /** Ids de las filas COMPLETAS seleccionadas (clic en la numeración, Shift/Ctrl para varias). */
    idsSeleccionados() {
        // Columnas de datos visibles (sin la de números de fila, que no tiene campo).
        const columnasVisibles = this.tabla.getColumns().filter(c => c.isVisible() && c.getField() && c.getField() !== 'orden').length;
        return [...new Set(this.tabla.getRanges()
            .filter(rango => rango.getColumns().length >= columnasVisibles)
            .flatMap(rango => rango.getRows().map(fila => fila.getData().id)))];
    }

    cambioDeSeleccion() {
        const celdas = this.tabla.getRanges().reduce((total, rango) => total + rango.getCells().flat().length, 0);
        document.getElementById('tc-seleccion').textContent = celdas > 1 ? `Selección: ${celdas} celdas` : '';
        this.alSeleccionar(this.idsSeleccionados());
    }

    prepararHerramientas() {
        const buscar = document.getElementById('tc-buscar');
        buscar.addEventListener('input', () => {
            this.busqueda = buscar.value;
            this.aplicarFiltros();
        });
        // "/" lleva a la búsqueda desde cualquier parte (como en muchos sistemas).
        document.addEventListener('keydown', evento => {
            if (evento.key === '/' && !['INPUT', 'TEXTAREA'].includes(document.activeElement?.tagName)) {
                evento.preventDefault();
                buscar.focus();
            }
        });

        // Densidad: cómoda (por defecto) o compacta para ver más filas.
        const botonDensidad = document.getElementById('tc-densidad');
        const aplicarDensidad = compacta => {
            this.tabla.element.classList.toggle('tc-compacta', compacta);
            botonDensidad.setAttribute('aria-pressed', String(compacta));
            botonDensidad.textContent = compacta ? 'Cómodo' : 'Compacto';
            this.tabla.redraw(true);
        };
        let compacta = leerGuardado(localStorage, CLAVE_DENSIDAD, false);
        this.lista.then(() => aplicarDensidad(compacta));
        botonDensidad.addEventListener('click', () => {
            compacta = !compacta;
            guardar(localStorage, CLAVE_DENSIDAD, compacta);
            aplicarDensidad(compacta);
        });

        // Selector de columnas visibles (las congeladas siempre se ven).
        this.lista.then(() => {
            const menu = document.getElementById('tc-menu-columnas');
            const columnas = this.tabla.getColumns().filter(c => !c.getDefinition().frozen && c.getField() && c.getField() !== 'orden');
            menu.replaceChildren(...columnas.map(columna => {
                // getParentColumn() devuelve false (no null) si la columna no está en un grupo.
                const padre = columna.getParentColumn();
                const grupo = padre ? padre.getDefinition().title : null;
                const titulo = grupo ? `${grupo} · ${columna.getDefinition().title}` : columna.getDefinition().title;
                const etiqueta = el('label');
                const casilla = el('input', 'form-check-input m-0');
                casilla.type = 'checkbox';
                casilla.checked = columna.isVisible();
                casilla.addEventListener('change', () => {
                    casilla.checked ? columna.show() : columna.hide();
                    this.guardarColumnas();
                    this.actualizarScrollHorizontal();
                });
                etiqueta.append(casilla, el('span', null, titulo));
                return etiqueta;
            }));
        });
    }

    guardarColumnas() {
        const ocultas = this.tabla.getColumns()
            .filter(c => c.getField() && !c.isVisible() && c.getField() !== 'orden')
            .map(c => c.getField());
        guardar(localStorage, CLAVE_COLUMNAS, ocultas);
    }

    /**
     * Barra horizontal siempre visible abajo (en la barra de estado): en una tabla larga no hay
     * que bajar hasta el final para moverse a la derecha. Se sincroniza en ambos sentidos.
     */
    prepararScrollHorizontal() {
        this.barra = document.getElementById('tc-scroll-horizontal');
        this.lista.then(() => {
            this.contenido = this.tabla.element.querySelector('.tabulator-tableholder');
            let moviendo = false;
            const sincronizar = (origen, destino) => () => {
                if (moviendo) return;
                moviendo = true;
                destino.scrollLeft = origen.scrollLeft;
                requestAnimationFrame(() => { moviendo = false; });
            };
            this.barra.addEventListener('scroll', sincronizar(this.barra, this.contenido));
            this.contenido.addEventListener('scroll', sincronizar(this.contenido, this.barra));
            new ResizeObserver(() => this.actualizarScrollHorizontal()).observe(this.tabla.element);
            this.actualizarScrollHorizontal();
        });
    }

    actualizarScrollHorizontal() {
        if (!this.contenido) return;
        const ancho = this.contenido.scrollWidth;
        this.barra.firstElementChild.style.width = `${ancho}px`;
        this.barra.style.visibility = ancho > this.contenido.clientWidth + 2 ? 'visible' : 'hidden';
    }
}
