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
    private readonly ILogger<OperacionesController> _logger;

    public OperacionesController(ApplicationDbContext context, IIncidenciasCacheService cache, ILogger<OperacionesController> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    // GET: /Operaciones/Incidencias
    // El listado GENERAL (sin texto de búsqueda) se sirve desde la caché de Redis.
    // Una búsqueda por texto NO debe usar esta caché: debe consultar SQLite directamente.
    public async Task<IActionResult> Incidencias(CancellationToken cancellationToken)
    {
        var abiertas = await _cache.ObtenerAbiertasAsync(cancellationToken);

        return View(abiertas);
    }

    // POST: /Operaciones/Cerrar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Supervisor)]
    public async Task<IActionResult> Cerrar(int id)
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

        return RedirectToAction(nameof(Incidencias));
    }
}
