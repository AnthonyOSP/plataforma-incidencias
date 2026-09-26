using PlataformaIncidencias.Models;

namespace PlataformaIncidencias.Services;

// Caché del listado GENERAL de incidencias abiertas (sin texto de búsqueda).
// Las búsquedas por texto no deben usar esta caché: consultan SQLite directamente.
public interface IIncidenciasCacheService
{
    // Devuelve las incidencias abiertas desde Redis (HIT) o desde SQLite (MISS),
    // guardando en Redis el resultado durante 60 segundos.
    Task<List<Incidencia>> ObtenerAbiertasAsync(CancellationToken cancellationToken = default);

    // Elimina el listado cacheado. Llamar SOLO después de persistir el cambio en SQLite.
    Task InvalidarAbiertasAsync();
}
