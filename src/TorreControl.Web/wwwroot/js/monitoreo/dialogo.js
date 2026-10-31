// Ventana de confirmación propia (en lugar de window.confirm): se ve igual que el resto de la
// app, funciona con teclado (Enter acepta, Esc cancela) y regresa el foco a donde estaba.

const fondo = document.getElementById('tc-dialogo');
const titulo = document.getElementById('tc-dialogo-titulo');
const texto = document.getElementById('tc-dialogo-texto');
const aceptar = document.getElementById('tc-dialogo-aceptar');
const cancelar = document.getElementById('tc-dialogo-cancelar');

export function confirmar({ titulo: t, texto: x, aceptar: etiqueta = 'Aceptar' }) {
    const foco = document.activeElement;
    titulo.textContent = t;
    texto.textContent = x;
    aceptar.textContent = etiqueta;
    fondo.hidden = false;
    aceptar.focus();

    return new Promise(resolver => {
        const cerrar = valor => {
            fondo.hidden = true;
            fondo.removeEventListener('keydown', teclas);
            aceptar.onclick = cancelar.onclick = fondo.onclick = null;
            foco?.focus?.();
            resolver(valor);
        };
        const teclas = evento => {
            if (evento.key === 'Escape') cerrar(false);
        };
        aceptar.onclick = () => cerrar(true);
        cancelar.onclick = () => cerrar(false);
        fondo.onclick = evento => { if (evento.target === fondo) cerrar(false); };
        fondo.addEventListener('keydown', teclas);
    });
}
