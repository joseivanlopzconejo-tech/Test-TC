import { HORA_VALIDA } from './util.js';

const NOMBRE_TURNO = { 1: '1er turno', 2: '2º turno', 3: '3er turno' };

// Barra de ventana: Día / Hora inicio / Hora fin. Una vez fijada se minimiza y muestra
// solo el turno y la ventana, para liberar espacio para la tabla.
export class BarraVentana {
    constructor({ alAplicar, alTurnoActual }) {
        this.barra = document.getElementById('tc-barra-ventana');
        this.form = document.getElementById('tc-form-ventana');
        this.dia = document.getElementById('tc-dia');
        this.desde = document.getElementById('tc-desde');
        this.hasta = document.getElementById('tc-hasta');
        this.error = document.getElementById('tc-error-ventana');
        this.chip = document.getElementById('tc-chip-turno');
        this.resumen = document.getElementById('tc-resumen-ventana');
        this.botonCambiar = document.getElementById('tc-cambiar-ventana');

        [this.desde, this.hasta].forEach(campo => campo.addEventListener('input', () => this.mascara(campo)));

        this.form.addEventListener('submit', evento => {
            evento.preventDefault();
            const ventana = this.leer();
            if (ventana) {
                this.abrir(false);
                alAplicar(ventana);
            }
        });

        document.getElementById('tc-turno-actual').addEventListener('click', () => {
            this.abrir(false);
            alTurnoActual();
        });

        this.botonCambiar.addEventListener('click', () => this.abrir(true));
        this.barra.addEventListener('keydown', evento => {
            if (evento.key === 'Escape') {
                this.abrir(false);
            }
        });
    }

    /** Escribir "0830" se convierte en "08:30" mientras se teclea. */
    mascara(campo) {
        const digitos = campo.value.replace(/\D/g, '').slice(0, 4);
        campo.value = digitos.length > 2 ? `${digitos.slice(0, 2)}:${digitos.slice(2)}` : digitos;
    }

    leer() {
        const ventana = { dia: this.dia.value, desde: this.desde.value, hasta: this.hasta.value };
        if (!ventana.dia || !HORA_VALIDA.test(ventana.desde) || !HORA_VALIDA.test(ventana.hasta)) {
            this.error.textContent = 'Revisa el día y las horas: usa formato de 24 h, por ejemplo 08:00 o 16:30.';
            return null;
        }
        this.error.textContent = '';
        return ventana;
    }

    abrir(abierta) {
        this.barra.dataset.abierta = String(abierta);
        this.botonCambiar.setAttribute('aria-expanded', String(abierta));
        if (abierta) {
            this.desde.focus();
            this.desde.select();
        }
    }

    /** Refleja la ventana que el servidor usó (puede venir por defecto = turno actual). */
    mostrar(ventana) {
        this.dia.value = ventana.dia;
        this.desde.value = ventana.desde;
        this.hasta.value = ventana.hasta;
        this.chip.textContent = NOMBRE_TURNO[ventana.turno] ?? '—';
        const [anio, mes, dia] = ventana.dia.split('-');
        this.resumen.textContent = `${dia}/${mes}/${anio} · ${ventana.desde}–${ventana.hasta}`;
    }
}
