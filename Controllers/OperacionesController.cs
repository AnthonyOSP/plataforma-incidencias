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
    private readonly IAlgoliaService _algolia;
    private readonly ILogger<OperacionesController> _logger;

    public OperacionesController(ApplicationDbContext context, IAlgoliaService algolia, ILogger<OperacionesController> logger)
    {
        _context = context;
        _algolia = algolia;
        _logger = logger;
    }

    // GET: /Operaciones/Incidencias?q=texto
    public async Task<IActionResult> Incidencias(string? q, CancellationToken cancellationToken)
    {
        var abiertas = _context.Incidencias.Where(i => i.Estado == EstadoIncidencia.Abierta);

        q = q?.Trim();
        ViewData["Busqueda"] = q;

        if (!string.IsNullOrEmpty(q))
        {
            // Algolia solo identifica qué incidencias coinciden; el estado se toma de SQLite.
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

            abiertas = abiertas.Where(i => ids.Contains(i.Id));
        }

        var resultado = await abiertas
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
            TempData["Mensaje"] = $"La incidencia #{incidencia.Id} fue cerrada.";
        }

        return RedirectToAction(nameof(Incidencias), new { q = string.IsNullOrWhiteSpace(q) ? null : q });
    }
}
