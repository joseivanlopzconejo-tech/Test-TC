import { el, textoSobre, leerGuardado, guardar } from './util.js';

const CLAVE_COLUMNAS = 'tc-columnas-ocultas';

// ---------- Celdas ----------
// Cada "formateador" arma el contenido de una celda con elementos del DOM (nunca HTML en texto).

function chipEstatus(estatus) {
    if (!estatus) {
        return el('span', 'tc-chip tc-chip-vacio', 'Sin acción');
    }
    const chip = el('span', 'tc-chip', estatus.nombre);
    chip.style.background = estatus.color;
    chip.style.color = textoSobre(estatus.color);
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

function celdaHora(celda) {
    const valor = celda.getValue();
    return valor ? el('span', null, valor) : el('span', 'tc-nulo', '—');
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

function celdaAforo(celda) {
    const valor = celda.getValue();
    return valor === null || valor === undefined ? el('span', 'tc-nulo', '—') : el('span', null, valor);
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
    const chip = chipEstatus(filtro.estatus);
    chip.textContent = filtro.hora;
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

const FILTROS_RAPIDOS = {
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
        const ocultas = new Set(leerGuardado(localStorage, CLAVE_COLUMNAS, []));
        const visible = campo => !ocultas.has(campo);
        const filtro = (n) => ({
            title: `F${n}`, field: `f${n}`, formatter: celdaFiltro, width: 92, hozAlign: 'center', headerSort: false, visible: visible(`f${n}`)
        });

        this.tabla = new Tabulator('#tc-tabla', {
            index: 'id',
            layout: 'fitData',
            height: 400,
            placeholder: 'No hay servicios en esta ventana.',
            movableColumns: true,
            initialSort: [{ column: 'orden', dir: 'asc' }],
            rowFormatter: fila => {
                const datos = fila.getData();
                fila.getElement().classList.toggle('tc-fila-mixto', datos.esMixto);
                fila.getElement().classList.toggle('tc-fila-incidencia', datos.tieneIncidencia);
            },
            columns: [
                // Congeladas: siempre visibles aunque se deslice a la derecha.
                { title: 'Ruta', field: 'ruta', frozen: true, formatter: celdaRuta, minWidth: 190 },
                { title: 'Unidad', field: 'unidad', frozen: true, formatter: celdaUnidad, width: 96 },
                { title: 'Estatus', field: 'estatusNombre', frozen: true, formatter: c => chipEstatus(c.getData().estatus), width: 118 },
                { title: 'orden', field: 'orden', visible: false },
                { title: 'Hip', field: 'hip', formatter: celdaHora, hozAlign: 'center', width: 70, sorter: (a, b, fa, fb) => fa.getData().orden.localeCompare(fb.getData().orden), visible: visible('hip') },
                { title: 'Hir', field: 'hir', formatter: celdaHora, hozAlign: 'center', width: 70, visible: visible('hir') },
                { title: 'Dif. ini', field: 'difIni', formatter: celdaDiferencia(this.reglas), hozAlign: 'center', width: 78, sorter: 'number', visible: visible('difIni') },
                { title: 'Htp', field: 'htp', formatter: celdaHora, hozAlign: 'center', width: 70, visible: visible('htp') },
                { title: 'Htr', field: 'htr', formatter: celdaHora, hozAlign: 'center', width: 70, visible: visible('htr') },
                { title: 'Dif. fin', field: 'difFin', formatter: celdaDiferencia(this.reglas), hozAlign: 'center', width: 78, sorter: 'number', visible: visible('difFin') },
                { title: 'Aforo', field: 'aforo', formatter: celdaAforo, hozAlign: 'right', width: 72, sorter: 'number', visible: visible('aforo') },
                filtro(1), filtro(2), filtro(3), filtro(4),
                { title: 'Operador', field: 'operadorNombre', formatter: celdaOperador, minWidth: 170, visible: visible('operadorNombre') },
                { title: 'Incidencias', field: 'incidencias', formatter: celdaIncidencias, minWidth: 260, headerSort: false, visible: visible('incidencias') }
            ]
        });

        this.lista = new Promise(resolve => this.tabla.on('tableBuilt', resolve));
        this.prepararHerramientas();
        window.addEventListener('resize', () => this.ajustarAltura());
    }

    async mostrar(servicios) {
        await this.lista;
        // replaceData conserva el desplazamiento, el orden y los filtros (no "salta" al actualizar).
        await this.tabla.replaceData(servicios.map(aFila));
        this.aplicarFiltros();
        this.ajustarAltura();
    }

    /** La tabla ocupa el alto que queda en la ventana (su encabezado se queda fijo arriba). */
    ajustarAltura() {
        const arriba = this.tabla.element.getBoundingClientRect().top;
        this.tabla.setHeight(Math.max(260, window.innerHeight - arriba - 12));
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

        // Selector de columnas visibles (las congeladas siempre se ven).
        this.lista.then(() => {
            const menu = document.getElementById('tc-menu-columnas');
            const columnas = this.tabla.getColumns().filter(c => !c.getDefinition().frozen && c.getField() !== 'orden');
            menu.replaceChildren(...columnas.map(columna => {
                const etiqueta = el('label');
                const casilla = el('input', 'form-check-input');
                casilla.type = 'checkbox';
                casilla.checked = columna.isVisible();
                casilla.addEventListener('change', () => {
                    casilla.checked ? columna.show() : columna.hide();
                    this.guardarColumnas();
                });
                etiqueta.append(casilla, el('span', null, columna.getDefinition().title));
                return etiqueta;
            }));
        });
    }

    guardarColumnas() {
        const ocultas = this.tabla.getColumns().filter(c => !c.isVisible() && c.getField() !== 'orden').map(c => c.getField());
        guardar(localStorage, CLAVE_COLUMNAS, ocultas);
    }
}
