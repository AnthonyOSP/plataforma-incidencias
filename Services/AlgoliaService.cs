using System.Text.Json.Serialization;
using Algolia.Search.Clients;
using Algolia.Search.Models.Search;
using Microsoft.Extensions.Options;
using PlataformaIncidencias.Models;

namespace PlataformaIncidencias.Services;

public class AlgoliaService : IAlgoliaService
{
    private static readonly List<string> AtributosBuscables = ["estacion", "descripcion"];

    private readonly AlgoliaOptions _options;
    private readonly SearchClient? _client;

    public AlgoliaService(IOptions<AlgoliaOptions> options, ILoggerFactory loggerFactory)
    {
        _options = options.Value;
        if (_options.EstaConfigurado)
        {
            _client = new SearchClient(new SearchConfig(_options.ApplicationId, _options.ApiKey), loggerFactory);
        }
    }

    public bool EstaConfigurado => _client is not null;

    public async Task<IReadOnlyList<int>> BuscarIdsAsync(string texto, CancellationToken cancellationToken = default)
    {
        var client = ObtenerCliente();

        var respuesta = await client.SearchSingleIndexAsync<Hit>(
            _options.IndexName,
            new SearchParams(new SearchParamsObject
            {
                Query = texto,
                AttributesToRetrieve = ["objectID"],
                HitsPerPage = 1000
            }),
            cancellationToken: cancellationToken);

        // El objectID de cada documento es el Id de la incidencia en SQLite.
        return respuesta.Hits
            .Select(h => int.TryParse(h.ObjectID, out var id) ? id : (int?)null)
            .OfType<int>()
            .ToList();
    }

    public async Task IndexarAsync(IEnumerable<Incidencia> incidencias, CancellationToken cancellationToken = default)
    {
        var client = ObtenerCliente();

        await client.SetSettingsAsync(
            _options.IndexName,
            new IndexSettings { SearchableAttributes = AtributosBuscables },
            cancellationToken: cancellationToken);

        var documentos = incidencias.Select(i => new IncidenciaDocumento
        {
            ObjectID = i.Id.ToString(),
            Estacion = i.Estacion,
            Descripcion = i.Descripcion,
            Prioridad = i.Prioridad.ToString()
        });

        await client.SaveObjectsAsync(_options.IndexName, documentos, cancellationToken: cancellationToken);
    }

    private SearchClient ObtenerCliente() =>
        _client ?? throw new InvalidOperationException("Algolia no está configurado.");

    // Documento que se guarda en Algolia. No incluye el Estado: la fuente de verdad es SQLite.
    private sealed class IncidenciaDocumento
    {
        [JsonPropertyName("objectID")]
        public string ObjectID { get; init; } = string.Empty;

        [JsonPropertyName("estacion")]
        public string Estacion { get; init; } = string.Empty;

        [JsonPropertyName("descripcion")]
        public string Descripcion { get; init; } = string.Empty;

        [JsonPropertyName("prioridad")]
        public string Prioridad { get; init; } = string.Empty;
    }
}
