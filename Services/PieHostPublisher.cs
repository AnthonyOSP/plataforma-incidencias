using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PlataformaIncidencias.Models;

namespace PlataformaIncidencias.Services;

public class PieHostPublisher : IPieHostPublisher
{
    public const string EventoIncidenciaActualizada = "IncidenciaActualizada";

    // Serialización sin camelCase para conservar los nombres Id y Estado del evento.
    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.General);

    private readonly HttpClient _http;
    private readonly PieHostOptions _options;
    private readonly ILogger<PieHostPublisher> _logger;

    public PieHostPublisher(HttpClient http, IOptions<PieHostOptions> options, ILogger<PieHostPublisher> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> PublicarIncidenciaActualizadaAsync(int id, EstadoIncidencia estado, CancellationToken cancellationToken = default)
    {
        if (!_options.EstaConfigurado)
        {
            _logger.LogWarning("PieHost no está configurado: no se publicó {Evento} para la incidencia {Id}.", EventoIncidenciaActualizada, id);
            return false;
        }

        // Formato de publicación de PieHost (protocolo V3): POST {cluster}/api/publish
        var cuerpo = new
        {
            key = _options.ApiKey,
            secret = _options.Secret,
            roomId = _options.Canal,
            message = new
            {
                @event = EventoIncidenciaActualizada,
                data = new { Id = id, Estado = estado.ToString() }
            }
        };

        try
        {
            var url = $"{_options.ClusterUrl.TrimEnd('/')}/api/publish";
            using var respuesta = await _http.PostAsJsonAsync(url, cuerpo, OpcionesJson, cancellationToken);
            if (!respuesta.IsSuccessStatusCode)
            {
                _logger.LogError("PieHost respondió {Codigo} al publicar {Evento} para la incidencia {Id}.",
                    (int)respuesta.StatusCode, EventoIncidenciaActualizada, id);
                return false;
            }

            _logger.LogInformation("Evento {Evento} publicado en PieHost (canal {Canal}): Id={Id}, Estado={Estado}.",
                EventoIncidenciaActualizada, _options.Canal, id, estado);
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "No se pudo publicar {Evento} en PieHost para la incidencia {Id}.", EventoIncidenciaActualizada, id);
            return false;
        }
    }

    public string? ObtenerUrlWebSocket()
    {
        if (!_options.EstaConfigurado ||
            !Uri.TryCreate(_options.ClusterUrl, UriKind.Absolute, out var cluster))
        {
            return null;
        }

        var builder = new UriBuilder(cluster)
        {
            Scheme = cluster.Scheme == Uri.UriSchemeHttp ? "ws" : "wss",
            Port = cluster.IsDefaultPort ? -1 : cluster.Port,
            Path = $"/v3/{Uri.EscapeDataString(_options.Canal)}",
            Query = $"api_key={Uri.EscapeDataString(_options.ApiKey)}"
        };
        return builder.Uri.ToString();
    }
}
