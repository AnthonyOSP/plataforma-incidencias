using PlataformaIncidencias.Models;

namespace PlataformaIncidencias.Services;

public interface IAlgoliaService
{
    bool EstaConfigurado { get; }

    // Devuelve los Ids de las incidencias cuya estación o descripción coinciden con el texto.
    Task<IReadOnlyList<int>> BuscarIdsAsync(string texto, CancellationToken cancellationToken = default);

    // Sube (o reemplaza) las incidencias en el índice de Algolia.
    Task IndexarAsync(IEnumerable<Incidencia> incidencias, CancellationToken cancellationToken = default);
}
