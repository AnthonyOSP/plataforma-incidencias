using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaIncidencias.Data;
using PlataformaIncidencias.Models;
using PlataformaIncidencias.Services;

namespace PlataformaIncidencias.Controllers;

[Authorize]
public class OperacionesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IIncidenciasCacheService _cache;
    private readonly IAlgoliaService _algolia;
    private readonly IPieHostPublisher _pieHost;
    private readonly ILogger<OperacionesController> _logger;

    public OperacionesController(
        ApplicationDbContext context,
        IIncidenciasCacheService cache,
        IAlgoliaService algolia,
        IPieHostPublisher pieHost,
        ILogger<OperacionesController> logger)
    {
        _context = context;
        _cache = cache;
        _algolia = algolia;
        _pieHost = pieHost;
        _logger = logger;
    }

    // GET: /Operaciones/Incidencias?q=texto
    public async Task<IActionResult> Incidencias(string? q, CancellationToken cancellationToken)
    {
        q = q?.Trim();
        ViewData["Busqueda"] = q;

        // Solo la URL pública del canal (con la API Key pública); el Secret no sale del servidor.
        ViewData["PieHostWebSocketUrl"] = _pieHost.ObtenerUrlWebSocket();

        // Listado GENERAL (sin texto de búsqueda): se sirve desde la caché de Redis
        // (con respaldo en SQLite si Redis no está disponible).
        if (string.IsNullOrEmpty(q))
        {
            var listado = await _cache.ObtenerAbiertasAsync(cancellationToken);
            ViewData["FuenteDatos"] = listado.DesdeRedis
                ? "Redis (caché)"
                : listado.GuardadoEnRedis ? "SQLite (guardado en Redis por 60 s)" : "SQLite (Redis no disponible)";
            return View(listado.Incidencias);
        }

        // Búsqueda por texto: NO usa Redis. Algolia solo identifica qué incidencias
        // coinciden; el estado se toma de SQLite.
        IReadOnlyList<int> ids;
        if (!_algolia.EstaConfigurado)
        {
            ViewData["ErrorBusqueda"] = "La búsqueda no está disponible: Algolia no está configurado.";
            ids = [];
        }
        else
        {
            try
            {
                ids = await _algolia.BuscarIdsAsync(q, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error al consultar Algolia con el texto {Texto}", q);
                ViewData["ErrorBusqueda"] = "No se pudo realizar la búsqueda. Inténtalo de nuevo más tarde.";
                ids = [];
            }
        }

        ViewData["FuenteDatos"] = "Algolia + SQLite";
        var resultado = await _context.Incidencias
            .Where(i => i.Estado == EstadoIncidencia.Abierta && ids.Contains(i.Id))
            .OrderBy(i => i.FechaCreacion)
            .ToListAsync(cancellationToken);

        return View(resultado);
    }

    // GET: /Operaciones/IncidenciasAbiertas
    // Estado vigente para que el navegador se sincronice tras (re)conectar el WebSocket.
    [HttpGet]
    public async Task<IActionResult> IncidenciasAbiertas()
    {
        var abiertas = await _context.Incidencias
            .Where(i => i.Estado == EstadoIncidencia.Abierta)
            .Select(i => new { i.Id, Estado = i.Estado.ToString() })
            .ToListAsync();

        return Json(abiertas);
    }

    // POST: /Operaciones/Cerrar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Supervisor)]
    public async Task<IActionResult> Cerrar(int id, string? q)
    {
        var incidencia = await _context.Incidencias.FindAsync(id);
        if (incidencia is null)
        {
            return NotFound();
        }

        if (incidencia.Estado == EstadoIncidencia.Abierta)
        {
            incidencia.Estado = EstadoIncidencia.Cerrada;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Incidencia {Id} cerrada y guardada en SQLite.", incidencia.Id);

            // Invalidar la caché solo después de confirmar la persistencia.
            await _cache.InvalidarAbiertasAsync();

            // Publicar al final. Si PieHost falla, el cierre ya quedó guardado y la caché invalidada.
            await _pieHost.PublicarIncidenciaActualizadaAsync(incidencia.Id, incidencia.Estado);
            TempData["Mensaje"] = $"La incidencia #{incidencia.Id} fue cerrada.";
        }

        return RedirectToAction(nameof(Incidencias), new { q = string.IsNullOrWhiteSpace(q) ? null : q });
    }
}
