using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PlataformaIncidencias.Data;
using PlataformaIncidencias.Models;
using StackExchange.Redis;

namespace PlataformaIncidencias.Services;

public class IncidenciasCacheService : IIncidenciasCacheService
{
    public const string ClaveAbiertas = "incidencias:abiertas";
    public static readonly TimeSpan Duracion = TimeSpan.FromSeconds(60);

    private readonly ApplicationDbContext _context;
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<IncidenciasCacheService> _logger;

    // redis es null cuando no hay ConnectionStrings:Redis configurado: se usa solo SQLite.
    public IncidenciasCacheService(
        ApplicationDbContext context,
        ILogger<IncidenciasCacheService> logger,
        IConnectionMultiplexer? redis = null)
    {
        _context = context;
        _logger = logger;
        _redis = redis;
    }

    public async Task<List<Incidencia>> ObtenerAbiertasAsync(CancellationToken cancellationToken = default)
    {
        var redis = ObtenerRedis();
        if (redis is not null)
        {
            try
            {
                var valor = await redis.StringGetAsync(ClaveAbiertas);
                if (valor.HasValue)
                {
                    _logger.LogInformation("Cache HIT: incidencias abiertas obtenidas desde Redis (clave {Clave}).", ClaveAbiertas);
                    return JsonSerializer.Deserialize<List<Incidencia>>(valor.ToString()) ?? [];
                }
            }
            catch (Exception ex) when (ex is RedisException or JsonException)
            {
                _logger.LogWarning(ex, "Redis no disponible al leer la clave {Clave}. Se consulta la base de datos.", ClaveAbiertas);
            }
        }

        _logger.LogInformation("Cache MISS: consultando incidencias abiertas en la base de datos (clave {Clave}).", ClaveAbiertas);
        var abiertas = await _context.Incidencias
            .AsNoTracking()
            .Where(i => i.Estado == EstadoIncidencia.Abierta)
            .OrderBy(i => i.FechaCreacion)
            .ToListAsync(cancellationToken);

        if (redis is not null)
        {
            try
            {
                await redis.StringSetAsync(ClaveAbiertas, JsonSerializer.Serialize(abiertas), Duracion);
                _logger.LogInformation("Incidencias abiertas guardadas en Redis (clave {Clave}, TTL {Segundos} s).", ClaveAbiertas, Duracion.TotalSeconds);
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex, "No se pudo guardar la clave {Clave} en Redis.", ClaveAbiertas);
            }
        }

        return abiertas;
    }

    public async Task InvalidarAbiertasAsync()
    {
        var redis = ObtenerRedis();
        if (redis is null)
        {
            return;
        }

        try
        {
            var eliminada = await redis.KeyDeleteAsync(ClaveAbiertas);
            _logger.LogInformation("Caché invalidada: clave {Clave} eliminada de Redis (existía: {Existia}).", ClaveAbiertas, eliminada);
        }
        catch (RedisException ex)
        {
            // La entrada expirará sola como máximo en 60 segundos.
            _logger.LogError(ex, "No se pudo invalidar la clave {Clave} en Redis.", ClaveAbiertas);
        }
    }

    // Devuelve null si Redis no está configurado o está desconectado, para no esperar
    // timeouts en cada petición: en ese caso se trabaja directamente con SQLite.
    private IDatabase? ObtenerRedis()
    {
        if (_redis is null)
        {
            return null;
        }

        if (!_redis.IsConnected)
        {
            _logger.LogWarning("Redis no está conectado. Se usa la base de datos sin caché (clave {Clave}).", ClaveAbiertas);
            return null;
        }

        return _redis.GetDatabase();
    }
}
