// Tema claro/oscuro. Oscuro por defecto (se trabaja de noche). La elección se recuerda solo en
// este navegador (preferencia personal, no dato del negocio).
(function () {
    const CLAVE = 'tc-tema';
    const raiz = document.documentElement;

    function leer() {
        try { return localStorage.getItem(CLAVE); } catch { return null; }
    }

    function aplicar(tema) {
        raiz.setAttribute('data-bs-theme', tema === 'light' ? 'light' : 'dark');
    }

    // 1) Antes de dibujar la página.
    aplicar(leer());

    // 2) Cuando ya existe el botón.
    document.addEventListener('DOMContentLoaded', () => {
        const boton = document.getElementById('tc-cambiar-tema');
        if (!boton) {
            return;
        }

        const pintarBoton = () => {
            const oscuro = raiz.getAttribute('data-bs-theme') === 'dark';
            boton.textContent = oscuro ? '☀' : '☾';
            boton.setAttribute('aria-label', oscuro ? 'Cambiar a tema claro' : 'Cambiar a tema oscuro');
        };

        boton.addEventListener('click', () => {
            const nuevo = raiz.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark';
            aplicar(nuevo);
            try { localStorage.setItem(CLAVE, nuevo); } catch { /* sin almacenamiento: solo dura esta página */ }
            pintarBoton();
        });

        pintarBoton();
    });
})();
