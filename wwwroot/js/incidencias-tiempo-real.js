// Cliente WebSocket de PieHost para /Operaciones/Incidencias.
// Escucha el evento IncidenciaActualizada y actualiza la tabla sin recargar la página.
// Al (re)conectar consulta al servidor el estado vigente para no depender de eventos perdidos.
(function () {
    const indicador = document.getElementById('estado-tiempo-real');
    if (!indicador) return;

    const wsUrl = indicador.dataset.wsUrl;
    const syncUrl = indicador.dataset.syncUrl;
    const RETARDO_INICIAL_MS = 1000;
    const RETARDO_MAXIMO_MS = 30000;
    let retardo = RETARDO_INICIAL_MS;

    function mostrarEstado(texto, clase) {
        indicador.textContent = 'Tiempo real: ' + texto;
        indicador.className = 'small ' + clase;
    }

    if (!wsUrl) {
        mostrarEstado('no configurado', 'text-muted');
        return;
    }

    function filaDe(id) {
        return document.querySelector('tr[data-incidencia-id="' + id + '"]');
    }

    function quitarFila(fila) {
        fila.remove();
        const tabla = document.getElementById('tabla-incidencias');
        if (tabla && !tabla.querySelector('tbody tr')) {
            const vacio = document.createElement('p');
            vacio.id = 'sin-incidencias';
            vacio.className = 'text-muted';
            vacio.textContent = 'No hay incidencias abiertas.';
            tabla.replaceWith(vacio);
        }
    }

    function aplicarEstado(id, estado) {
        const fila = filaDe(id);
        if (!fila) return;
        if (estado === 'Abierta') {
            fila.querySelector('[data-campo="estado"]').textContent = estado;
        } else {
            quitarFila(fila); // Esta pantalla solo lista incidencias abiertas.
        }
    }

    async function sincronizar() {
        try {
            const respuesta = await fetch(syncUrl, { headers: { 'Accept': 'application/json' }, credentials: 'same-origin' });
            if (!respuesta.ok) return;
            const abiertas = new Set((await respuesta.json()).map(function (i) { return String(i.id); }));
            document.querySelectorAll('tr[data-incidencia-id]').forEach(function (fila) {
                if (!abiertas.has(fila.dataset.incidenciaId)) quitarFila(fila);
            });
        } catch (e) {
            console.warn('No se pudo sincronizar el estado de las incidencias', e);
        }
    }

    function conectar() {
        mostrarEstado('conectando…', 'text-muted');
        const socket = new WebSocket(wsUrl);

        socket.addEventListener('open', function () {
            retardo = RETARDO_INICIAL_MS;
            mostrarEstado('conectado', 'text-success');
            sincronizar();
        });

        socket.addEventListener('message', function (evento) {
            let mensaje;
            try { mensaje = JSON.parse(evento.data); } catch { return; }
            if (mensaje && mensaje.event === 'IncidenciaActualizada' && mensaje.data) {
                aplicarEstado(mensaje.data.Id, mensaje.data.Estado);
            }
        });

        socket.addEventListener('close', function () {
            // Reconexión progresiva: 1 s, 2 s, 4 s… hasta 30 s, con algo de azar.
            const espera = retardo + Math.floor(Math.random() * 500);
            mostrarEstado('reconectando en ' + Math.round(espera / 1000) + ' s…', 'text-warning');
            setTimeout(conectar, espera);
            retardo = Math.min(retardo * 2, RETARDO_MAXIMO_MS);
        });
    }

    conectar();
})();
