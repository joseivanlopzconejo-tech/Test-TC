// Comunicación con el servidor. El navegador solo pide y muestra: todas las reglas de tiempo
// (qué se ve, qué está bloqueado) las decide el servidor.

export class SesionVencida extends Error {}

/**
 * Pide la tabla de una ventana de horas.
 * automatica = true en las actualizaciones periódicas: el servidor no las cuenta como
 * actividad, para que la sesión venza si nadie está frente a la pantalla.
 */
export async function obtenerTabla({ dia, desde, hasta }, automatica = false) {
    const parametros = new URLSearchParams();
    if (dia) parametros.set('dia', dia);
    if (desde) parametros.set('desde', desde);
    if (hasta) parametros.set('hasta', hasta);

    const respuesta = await fetch(`/api/tabla?${parametros}`, {
        credentials: 'same-origin',
        cache: 'no-store',
        headers: automatica ? { 'X-Actualizacion-Automatica': '1' } : {}
    });

    if (respuesta.status === 401) {
        throw new SesionVencida();
    }
    if (!respuesta.ok) {
        throw new Error(`Error ${respuesta.status}`);
    }

    return respuesta.json();
}
