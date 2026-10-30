// JavaScript propio de Torre de Control (los módulos de la tabla llegan en la Fase 2).

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
