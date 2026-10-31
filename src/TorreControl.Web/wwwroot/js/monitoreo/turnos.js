import { el } from './util.js';

// Selector de turno: 1º · 2º · 3º · Día, cada uno con cuántos servicios tiene la pestaña elegida.
// El turno que corre ahora lleva un punto que late. Si no se elige ninguno, la tabla sigue sola
// al turno en curso (al dar las 16:00 pasa al 3º sin recargar).

const TURNOS = [
    { valor: 1, texto: '1º', titulo: '1er turno · 00:00 a 08:00' },
    { valor: 2, texto: '2º', titulo: '2º turno · 08:00 a 16:00' },
    { valor: 3, texto: '3º', titulo: '3er turno · 16:00 a 00:00' },
    { valor: 0, texto: 'Día', titulo: 'Día completo' }
];

export class SelectorTurno {
    constructor({ alElegir }) {
        this.contenedor = document.getElementById('tc-turnos');
        this.indicador = this.contenedor.querySelector('.tc-turnos-indicador');
        this.botones = TURNOS.map(turno => {
            const boton = el('button', 'tc-turno');
            boton.type = 'button';
            boton.setAttribute('role', 'radio');
            boton.title = turno.titulo;
            boton.dataset.turno = turno.valor;
            boton.append(el('span', null, turno.texto), el('span', 'tc-contador', '·'));
            boton.addEventListener('click', () => alElegir(turno.valor));
            this.contenedor.append(boton);
            return boton;
        });
        window.addEventListener('resize', () => this.moverIndicador());
    }

    /** conteos: { "1": n, "2": n, "3": n, "0": n } de la pestaña elegida. */
    pintar(conteos, turnoElegido, turnoActual) {
        this.botones.forEach(boton => {
            const valor = Number(boton.dataset.turno);
            boton.setAttribute('aria-checked', String(valor === turnoElegido));
            boton.classList.toggle('tc-turno-ahora', valor === turnoActual);
            boton.querySelector('.tc-contador').textContent = conteos?.[valor] ?? 0;
        });
        this.moverIndicador();
    }

    /** El fondo resaltado se desliza hasta el turno elegido. */
    moverIndicador() {
        const elegido = this.botones.find(b => b.getAttribute('aria-checked') === 'true');
        if (!elegido) {
            return;
        }
        this.indicador.style.width = `${elegido.offsetWidth}px`;
        this.indicador.style.transform = `translateX(${elegido.offsetLeft}px)`;
    }
}
