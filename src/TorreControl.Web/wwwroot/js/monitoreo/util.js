// Utilidades pequeñas para armar la interfaz sin innerHTML.
// Se construyen elementos con textContent: así un dato nunca se interpreta como HTML (evita XSS),
// y los colores se ponen con element.style, que la política CSP sí permite.

/** Crea un elemento con clase y texto opcionales. */
export function el(etiqueta, clase, texto) {
    const nodo = document.createElement(etiqueta);
    if (clase) {
        nodo.className = clase;
    }
    if (texto !== undefined && texto !== null) {
        nodo.textContent = String(texto);
    }
    return nodo;
}

const zona = document.querySelector('meta[name="zona-horaria"]')?.content || 'America/Mexico_City';

/** Formateador de horas en 24 h y en la zona del negocio. */
export const formatoHora = new Intl.DateTimeFormat('es-MX', {
    hour: '2-digit', minute: '2-digit', second: '2-digit', hourCycle: 'h23', timeZone: zona
});

export const HORA_VALIDA = /^([01]\d|2[0-3]):[0-5]\d$/;

/** Color de texto legible (oscuro o claro) sobre un fondo #RRGGBB. */
export function textoSobre(colorHex) {
    const m = /^#?([0-9a-f]{6})$/i.exec(colorHex || '');
    if (!m) {
        return '#ffffff';
    }
    const n = parseInt(m[1], 16);
    const [r, g, b] = [(n >> 16) & 255, (n >> 8) & 255, n & 255].map(c => {
        const v = c / 255;
        return v <= 0.03928 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4;
    });
    const luminancia = 0.2126 * r + 0.7152 * g + 0.0722 * b;
    return luminancia > 0.4 ? '#14181d' : '#ffffff';
}

/** Lee/escribe en sessionStorage o localStorage sin romper si el navegador lo bloquea. */
export function leerGuardado(almacen, clave, porDefecto) {
    try {
        const valor = almacen.getItem(clave);
        return valor ? JSON.parse(valor) : porDefecto;
    } catch {
        return porDefecto;
    }
}

export function guardar(almacen, clave, valor) {
    try {
        almacen.setItem(clave, JSON.stringify(valor));
    } catch {
        // Sin almacenamiento: la preferencia solo dura mientras la página esté abierta.
    }
}
