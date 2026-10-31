// Comunicación con el servidor. El navegador solo pide y muestra: todas las reglas de tiempo
// (qué se ve en cada turno, qué está bloqueado, el estatus) las decide el servidor.

export class SesionVencida extends Error {}

/**
 * Pide la tabla de un día y turno (null = hoy / turno en curso).
 * automatica = true en las actualizaciones periódicas: el servidor no las cuenta como
 * actividad, para que la sesión venza si nadie está frente a la pantalla.
 */
export async function obtenerTabla({ dia, turno }, automatica = false) {
    const parametros = new URLSearchParams();
    if (dia) parametros.set('dia', dia);
    if (turno !== null && turno !== undefined) parametros.set('turno', turno);

    const respuesta = await fetch(`/api/tabla?${parametros}`, {
        credentials: 'same-origin',
        cache: 'no-store',
        headers: automatica ? { 'X-Actualizacion-Automatica': '1' } : {}
    });
    return leer(respuesta);
}

/** Marca (o reactiva) servicios como "no operó hoy". */
export async function marcarNoOpero(ids, noOpero) {
    return leer(await window.tc.post('/api/servicios/no-opero', { ids, noOpero }));
}

async function leer(respuesta) {
    if (respuesta.status === 401) {
        throw new SesionVencida();
    }
    if (!respuesta.ok) {
        throw new Error(`Error ${respuesta.status}`);
    }
    return respuesta.json();
}
