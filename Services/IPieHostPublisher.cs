using PlataformaIncidencias.Models;

namespace PlataformaIncidencias.Services;

public interface IPieHostPublisher
{
    // Publica el evento IncidenciaActualizada con { Id, Estado }. Nunca lanza excepción:
    // devuelve false y registra el error si PieHost no está disponible.
    Task<bool> PublicarIncidenciaActualizadaAsync(int id, EstadoIncidencia estado, CancellationToken cancellationToken = default);

    // URL wss:// del canal para el navegador (solo con la API Key pública), o null si no hay configuración.
    string? ObtenerUrlWebSocket();
}
