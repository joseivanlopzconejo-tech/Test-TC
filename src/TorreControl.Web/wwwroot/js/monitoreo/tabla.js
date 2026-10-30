import { el, leerGuardado, guardar } from './util.js';

const CLAVE_COLUMNAS = 'tc-columnas-ocultas';
const CLAVE_DENSIDAD = 'tc-densidad';

// ---------- Celdas ----------
// Cada "formateador" arma el contenido de una celda con elementos del DOM (nunca HTML en texto).

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

function celdaRuta(celda) {
    const fila = celda.getData();
    const caja = el('div', 'tc-ruta');
    const tipo = el('span', `tc-tipo tc-tipo-${fila.tipo}`, fila.tipo);
    tipo.title = fila.tipo === 'E' ? 'Entrada a planta' : 'Salida de planta';
    caja.append(tipo, el('span', 'tc-ruta-nombre', fila.ruta));

    if (fila.esMixto) {
        const mixto = el('span', 'tc-etiqueta tc-etiqueta-mixto', 'MIXTO');
        mixto.title = 'Cruza un cambio de turno: se ve y se edita en ambos turnos';
        caja.append(mixto);
    }
    if (fila.cruzaMedianoche) {
        const noche = el('span', 'tc-etiqueta tc-etiqueta-medianoche', '+1');
        noche.title = 'Llega al día siguiente';
        caja.append(noche);
    }
    return caja;
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

/** Diferencia en minutos con signo, coloreada según la escalera de tolerancia. */
function celdaDiferencia(reglas) {
    return celda => {
        const minutos = celda.getValue();
        if (minutos === null || minutos === undefined) {
            return el('span', 'tc-nulo', '—');
        }
        const clase = Math.abs(minutos) > reglas.sospechosaMin
            ? 'tc-dif-alerta'
            : minutos > reglas.toleranciaMin ? 'tc-dif-aviso' : 'tc-dif-ok';
        return el('span', `tc-dif ${clase}`, minutos > 0 ? `+${minutos}` : String(minutos));
    };
}

function celdaFiltro(celda) {
    const filtro = celda.getValue();
    if (!filtro) {
        // Los servicios de salida (S) no tienen filtros.
        const na = el('span', 'tc-nulo', '—');
        na.title = 'No aplica (servicio de salida)';
        return na;
    }
    // Compacto: la hora límite dentro de un chip del color del estatus. El nombre del estatus
    // y la observación aparecen al pasar el cursor.
    const caja = el('span', 'tc-filtro-celda');
    caja.append(chipEstatus(filtro.estatus, filtro.hora));
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

    // Se coloca junto al botón, sin salirse de la pantalla.
    const r = boton.getBoundingClientRect();
    const ancho = ficha.offsetWidth;
    const alto = ficha.offsetHeight;
    ficha.style.left = `${Math.max(8, Math.min(r.left, window.innerWidth - ancho - 8))}px`;
    ficha.style.top = `${r.bottom + alto + 12 > window.innerHeight ? r.top - alto - 6 : r.bottom + 6}px`;
}

function cerrarFicha() {
    ficha.hidden = true;
}

document.addEventListener('click', evento => {
    if (!ficha.hidden && !ficha.contains(evento.target) && !evento.target.closest('.tc-boton-info')) {
        cerrarFicha();
    }
});
document.addEventListener('keydown', evento => {
    if (evento.key === 'Escape') {
        cerrarFicha();
    }
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
    ruta: (_, f) => `${f.tipo} ${f.ruta}${f.esMixto ? ' (MIXTO)' : ''}`,
    unidad: (_, f) => [f.unidad, ...f.apoyos].filter(Boolean).join(' + '),
    filtro: valor => (valor ? `${valor.hora} ${valor.estatus?.nombre ?? 'Sin acción'}` : ''),
    texto: valor => valor ?? ''
};

// ---------- Datos ----------

/** Convierte un servicio de la API en una fila plana para la tabla. */
export function aFila(servicio) {
    const filtro = n => servicio.filtros.find(f => f.numero === n) ?? null;
    return {
        ...servicio,
        f1: filtro(1), f2: filtro(2), f3: filtro(3), f4: filtro(4),
        operadorNombre: servicio.operador?.nombre ?? '',
        estatusNombre: servicio.estatus?.nombre ?? '',
        // Para ordenar por hora real de salida (fecha + Hip), no solo por el texto "HH:mm".
        orden: `${servicio.fecha}T${servicio.hip}`
    };
}

// ---------- Filtros rápidos y búsqueda ----------

export const FILTROS_RAPIDOS = {
    todos: () => true,
    // Pendiente = todavía le falta la hora real de inicio o de llegada.
    pendientes: f => !f.hir || !f.htr,
    incidencia: f => f.tieneIncidencia,
    trayecto: f => f.estatusNombre === 'Trayecto'
};

// ---------- Tabla ----------

export class TablaServicios {
    constructor(reglas) {
        this.reglas = reglas;
        this.busqueda = '';
        this.filtroRapido = 'todos';
        this.filas = [];

        const ocultas = new Set(leerGuardado(localStorage, CLAVE_COLUMNAS, []));
        const visible = campo => !ocultas.has(campo);
        const hora = (titulo, campo, extra = {}) => ({
            title: titulo, field: campo, formatter: celdaTexto, hozAlign: 'center', width: 82,
            accessorClipboard: copiar.texto, visible: visible(campo), ...extra
        });
        const filtro = n => ({
            // El F4 es más ancho porque además muestra cuántas modificaciones lleva (×2).
            title: `F${n}`, field: `f${n}`, formatter: celdaFiltro, hozAlign: 'center', width: n === 4 ? 124 : 100,
            headerSort: false, accessorClipboard: copiar.filtro, visible: visible(`f${n}`)
        });
        const diferencia = campo => hora('Dif.', campo, { formatter: celdaDiferencia(this.reglas), sorter: 'number', width: 72 });
        const ordenPorSalida = (a, b, filaA, filaB) => filaA.getData().orden.localeCompare(filaB.getData().orden);

        this.tabla = new Tabulator('#tc-tabla', {
            index: 'id',
            layout: 'fitData',
            height: 400,
            placeholder: 'No hay servicios en esta ventana.',
            movableColumns: true,
            // Títulos de una sola fila alineados abajo, junto a los de los grupos.
            columnHeaderVertAlign: 'bottom',
            initialSort: [{ column: 'orden', dir: 'asc' }],

            // Como hoja de cálculo: números de fila, celda activa que se mueve con las flechas,
            // selección de rangos (Shift + flechas o arrastrando) y Ctrl+C para copiar.
            rowHeader: { formatter: 'rownum', headerSort: false, hozAlign: 'center', resizable: false, frozen: true, width: 44 },
            selectableRange: 1,
            selectableRangeColumns: true,
            selectableRangeRows: true,
            clipboard: 'copy',
            clipboardCopyRowRange: 'range',
            clipboardCopyConfig: { columnHeaders: true, columnGroups: false, rowHeaders: false, formatCells: false },
            // Ordenar con la flecha del encabezado; clic en el título selecciona la columna (como en Excel).
            headerSortClickElement: 'icon',

            rowFormatter: fila => {
                const datos = fila.getData();
                fila.getElement().classList.toggle('tc-fila-mixto', datos.esMixto);
                fila.getElement().classList.toggle('tc-fila-incidencia', datos.tieneIncidencia);
            },

            // Orden de columnas: identificación (congelada) → filtros → inicio → llegada →
            // aforo → observaciones (lo que pasó en el turno) → operador.
            columns: [
                { title: 'Ruta', field: 'ruta', frozen: true, formatter: celdaRuta, minWidth: 200, sorter: 'string', accessorClipboard: copiar.ruta },
                { title: 'Unidad', field: 'unidad', frozen: true, formatter: celdaUnidad, width: 92, accessorClipboard: copiar.unidad },
                { title: 'Estatus', field: 'estatusNombre', frozen: true, formatter: c => chipEstatus(c.getData().estatus), width: 122, accessorClipboard: v => v || 'Sin acción' },
                { title: 'orden', field: 'orden', visible: false, clipboard: false },
                { title: 'Filtros de revisión', columns: [filtro(1), filtro(2), filtro(3), filtro(4)] },
                { title: 'Hora de inicio', columns: [hora('Prog.', 'hip', { sorter: ordenPorSalida }), hora('Real', 'hir'), diferencia('difIni')] },
                { title: 'Hora de llegada', columns: [hora('Prog.', 'htp'), hora('Real', 'htr'), diferencia('difFin')] },
                { title: 'Aforo', field: 'aforo', formatter: celdaTexto, hozAlign: 'right', width: 96, sorter: 'number', accessorClipboard: copiar.texto, visible: visible('aforo') },
                { title: 'Observaciones', field: 'incidencias', formatter: celdaIncidencias, minWidth: 340, headerSort: false, accessorClipboard: copiar.texto, visible: visible('incidencias') },
                { title: 'Operador', field: 'operadorNombre', formatter: celdaOperador, minWidth: 180, accessorClipboard: copiar.texto, visible: visible('operadorNombre') }
            ]
        });

        this.lista = new Promise(resolve => this.tabla.on('tableBuilt', resolve));
        this.tabla.on('rangeChanged', () => {
            this.pintarSeleccion();
            this.mostrarCeldaActiva();
        });
        this.prepararHerramientas();
        window.addEventListener('resize', () => this.ajustarAltura());
    }

    async mostrar(servicios) {
        await this.lista;
        this.filas = servicios.map(aFila);
        // replaceData conserva el desplazamiento, el orden y los filtros (no "salta" al actualizar).
        await this.tabla.replaceData(this.filas);
        this.aplicarFiltros();
        this.pintarCuentas();
        this.ajustarAltura();
    }

    /** La tabla ocupa el alto que queda en la ventana (su encabezado se queda fijo arriba). */
    ajustarAltura() {
        const arriba = this.tabla.element.getBoundingClientRect().top;
        const barraEstado = document.querySelector('.tc-barra-estado')?.offsetHeight ?? 0;
        this.tabla.setHeight(Math.max(260, window.innerHeight - arriba - barraEstado - 4));
    }

    aplicarFiltros() {
        const texto = this.busqueda.trim().toLowerCase();
        const rapido = FILTROS_RAPIDOS[this.filtroRapido] ?? FILTROS_RAPIDOS.todos;
        this.tabla.setFilter(fila => {
            if (!rapido(fila)) {
                return false;
            }
            if (!texto) {
                return true;
            }
            return [fila.ruta, fila.unidad, fila.operadorNombre, ...fila.apoyos]
                .some(valor => (valor ?? '').toLowerCase().includes(texto));
        });
    }

    /** Cuántos servicios hay en cada filtro rápido y resumen en la barra de estado. */
    pintarCuentas() {
        Object.entries(FILTROS_RAPIDOS).forEach(([clave, condicion]) => {
            const destino = document.querySelector(`[data-cuenta="${clave}"]`);
            if (destino) {
                destino.textContent = this.filas.filter(condicion).length;
            }
        });

        const resumen = document.getElementById('tc-resumen-tabla');
        const mixtos = this.filas.filter(f => f.esMixto).length;
        const partes = [
            ['servicios', this.filas.length],
            ['pendientes', this.filas.filter(FILTROS_RAPIDOS.pendientes).length],
            ['mixtos', mixtos],
            ['con incidencia', this.filas.filter(FILTROS_RAPIDOS.incidencia).length]
        ];
        resumen.replaceChildren(...partes.flatMap(([texto, n], i) => [
            ...(i > 0 ? [document.createTextNode(' · ')] : []),
            el('strong', null, n),
            document.createTextNode(` ${texto}`)
        ]));
    }

    /** Al moverse con el teclado, desplaza la tabla para que la celda activa siempre se vea. */
    mostrarCeldaActiva() {
        const rangos = this.tabla.getRanges();
        // getBounds() entrega celdas internas de Tabulator; getComponent() da la versión pública.
        const fin = rangos[rangos.length - 1]?.getBounds()?.end?.getComponent?.();
        if (!fin) {
            return;
        }
        const columna = fin.getColumn();
        if (!columna.getDefinition().frozen) {
            columna.scrollTo('middle', false);
        }
        fin.getRow().scrollTo('nearest', false);
    }

    pintarSeleccion() {
        const destino = document.getElementById('tc-seleccion');
        const celdas = this.tabla.getRanges().reduce((total, rango) => total + rango.getCells().flat().length, 0);
        destino.textContent = celdas > 1 ? `Selección: ${celdas} celdas` : '';
    }

    prepararHerramientas() {
        document.getElementById('tc-buscar').addEventListener('input', evento => {
            this.busqueda = evento.target.value;
            this.aplicarFiltros();
        });

        document.querySelectorAll('.tc-filtro').forEach(boton => boton.addEventListener('click', () => {
            document.querySelectorAll('.tc-filtro').forEach(b => b.classList.toggle('activo', b === boton));
            this.filtroRapido = boton.dataset.filtro;
            this.aplicarFiltros();
        }));

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
}
