import { formatoHora } from './util.js';

const zona = document.querySelector('meta[name="zona-horaria"]')?.content || 'America/Mexico_City';
const partes = new Intl.DateTimeFormat('en-US', {
    timeZone: zona, year: 'numeric', month: '2-digit', day: '2-digit',
    hour: '2-digit', minute: '2-digit', second: '2-digit', hourCycle: 'h23'
});

// Reloj de 24 h con la hora del SERVIDOR (no la de la computadora, que puede estar desfasada).
// Cada respuesta de la API trae la hora del servidor; con eso se calcula el desfase.
export class Reloj {
    constructor(elemento) {
        this.elemento = elemento;
        this.desfaseMs = 0;
        setInterval(() => this.pintar(), 1000);
        this.pintar();
    }

    sincronizar(ahoraUtcServidor) {
        this.desfaseMs = new Date(ahoraUtcServidor).getTime() - Date.now();
        this.pintar();
    }

    ahora() {
        return new Date(Date.now() + this.desfaseMs);
    }

    /**
     * La hora de CDMX "de reloj de pared", como fecha local del navegador. Sirve para comparar con
     * las horas programadas ("2026-10-31T05:30"), que también son hora de CDMX.
     */
    ahoraNegocio() {
        const p = Object.fromEntries(partes.formatToParts(this.ahora()).map(x => [x.type, x.value]));
        return new Date(Number(p.year), Number(p.month) - 1, Number(p.day), Number(p.hour), Number(p.minute), Number(p.second));
    }

    pintar() {
        this.elemento.textContent = formatoHora.format(this.ahora());
    }
}
