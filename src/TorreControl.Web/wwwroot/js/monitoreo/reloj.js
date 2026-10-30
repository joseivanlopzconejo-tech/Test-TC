import { formatoHora } from './util.js';

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

    pintar() {
        this.elemento.textContent = formatoHora.format(this.ahora());
    }
}
