// JavaScript común de Torre de Control.

window.tc = window.tc || {};

/**
 * POST con JSON que incluye el token antiforgery (CSRF).
 * Todo fetch que modifique datos debe pasar por aquí: el servidor rechaza (400)
 * cualquier POST que no traiga el token.
 */
window.tc.post = function (url, datos) {
    const token = document.querySelector('meta[name="csrf-token"]')?.content ?? '';
    return fetch(url, {
        method: 'POST',
        credentials: 'same-origin',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        },
        body: JSON.stringify(datos ?? {})
    });
};

/** Notificación breve en la esquina. tipo: 'info' | 'exito' | 'error'. */
window.tc.toast = function (mensaje, tipo = 'info') {
    const contenedor = document.getElementById('tc-toasts');
    if (!contenedor) {
        return;
    }

    const toast = document.createElement('div');
    toast.className = `tc-toast tc-toast-${tipo}`;
    toast.setAttribute('role', tipo === 'error' ? 'alert' : 'status');
    toast.textContent = mensaje; // textContent: nunca se interpreta como HTML
    contenedor.appendChild(toast);
    setTimeout(() => toast.remove(), 4500);
};
