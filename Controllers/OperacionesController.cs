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
    private readonly IPieHostPublisher _pieHost;
    private readonly ILogger<OperacionesController> _logger;

    public OperacionesController(ApplicationDbContext context, IPieHostPublisher pieHost, ILogger<OperacionesController> logger)
    {
        _context = context;
        _pieHost = pieHost;
        _logger = logger;
    }

    // GET: /Operaciones/Incidencias
    public async Task<IActionResult> Incidencias()
    {
        var abiertas = await _context.Incidencias
            .Where(i => i.Estado == EstadoIncidencia.Abierta)
            .OrderBy(i => i.FechaCreacion)
            .ToListAsync();

        // Solo la URL pública del canal (con la API Key pública); el Secret no sale del servidor.
        ViewData["PieHostWebSocketUrl"] = _pieHost.ObtenerUrlWebSocket();
        return View(abiertas);
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

            // Publicar solo después de persistir. Si PieHost falla, el cierre ya quedó guardado.
            await _pieHost.PublicarIncidenciaActualizadaAsync(incidencia.Id, incidencia.Estado);
            TempData["Mensaje"] = $"La incidencia #{incidencia.Id} fue cerrada.";
        }

        return RedirectToAction(nameof(Incidencias));
    }
}
