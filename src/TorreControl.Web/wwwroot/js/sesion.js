// Aviso de sesión por vencer.
// La sesión dura 8 horas y se renueva sola mientras se trabaja. Si la persona deja la
// pestaña abierta sin actividad, unos minutos antes de vencer se le avisa y puede renovarla
// sin perder lo que tiene en pantalla.
(function () {
    const MINUTOS_DE_AVISO = 10;

    const meta = document.querySelector('meta[name="sesion-expira"]');
    const aviso = document.getElementById('tc-aviso-sesion');
    const texto = document.getElementById('tc-aviso-sesion-texto');
    const boton = document.getElementById('tc-aviso-sesion-boton');
    if (!meta || !aviso) {
        return;
    }

    let expira = new Date(meta.content);
    let temporizadores = [];

    const zona = document.querySelector('meta[name="zona-horaria"]')?.content;
    const horaCorta = (fecha) =>
        fecha.toLocaleTimeString('es-MX', { hour: '2-digit', minute: '2-digit', hourCycle: 'h23', timeZone: zona });

    function programar() {
        temporizadores.forEach(clearTimeout);
        aviso.hidden = true;

        const faltan = expira.getTime() - Date.now();
        temporizadores = [
            setTimeout(mostrarAviso, Math.max(0, faltan - MINUTOS_DE_AVISO * 60 * 1000)),
            setTimeout(mostrarVencida, Math.max(0, faltan))
        ];
    }

    function mostrarAviso() {
        texto.textContent = `Tu sesión vence a las ${horaCorta(expira)}.`;
        boton.textContent = 'Seguir trabajando';
        boton.onclick = renovar;
        aviso.hidden = false;
    }

    function mostrarVencida() {
        texto.textContent = 'Tu sesión terminó.';
        boton.textContent = 'Iniciar sesión';
        // Regresar exactamente a esta vista (planta, día y horas van en la dirección).
        boton.onclick = () => {
            const regreso = encodeURIComponent(location.pathname + location.search);
            location.href = `/Cuenta/Login?returnUrl=${regreso}`;
        };
        aviso.hidden = false;
    }

    async function renovar() {
        try {
            const respuesta = await window.tc.post('/Cuenta/MantenerSesion');
            // Si la sesión ya había vencido, el servidor responde con la página de login.
            if (!respuesta.ok || respuesta.redirected) {
                mostrarVencida();
                return;
            }
            const datos = await respuesta.json();
            if (datos.expiraUtc) {
                expira = new Date(datos.expiraUtc);
            }
            programar();
        } catch {
            texto.textContent = 'No se pudo renovar la sesión. Revisa tu conexión.';
        }
    }

    programar();
})();
