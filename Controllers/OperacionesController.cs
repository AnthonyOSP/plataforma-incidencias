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
    private readonly ILogger<OperacionesController> _logger;

    public OperacionesController(
        ApplicationDbContext context,
        IIncidenciasCacheService cache,
        IAlgoliaService algolia,
        ILogger<OperacionesController> logger)
    {
        _context = context;
        _cache = cache;
        _algolia = algolia;
        _logger = logger;
    }

    // GET: /Operaciones/Incidencias?q=texto
    public async Task<IActionResult> Incidencias(string? q, CancellationToken cancellationToken)
    {
        q = q?.Trim();
        ViewData["Busqueda"] = q;

        // Listado GENERAL (sin texto de búsqueda): se sirve desde la caché de Redis
        // (con respaldo en SQLite si Redis no está disponible).
        if (string.IsNullOrEmpty(q))
        {
            var abiertas = await _cache.ObtenerAbiertasAsync(cancellationToken);
            return View(abiertas);
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

        var resultado = await _context.Incidencias
            .Where(i => i.Estado == EstadoIncidencia.Abierta && ids.Contains(i.Id))
            .OrderBy(i => i.FechaCreacion)
            .ToListAsync(cancellationToken);

        return View(resultado);
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
            TempData["Mensaje"] = $"La incidencia #{incidencia.Id} fue cerrada.";
        }

        return RedirectToAction(nameof(Incidencias), new { q = string.IsNullOrWhiteSpace(q) ? null : q });
    }
}
