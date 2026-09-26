using System.ComponentModel.DataAnnotations;

namespace PlataformaIncidencias.Models;

public class Incidencia
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Estación")]
    public string Estacion { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    [Display(Name = "Descripción")]
    public string Descripcion { get; set; } = string.Empty;

    public Prioridad Prioridad { get; set; }

    public EstadoIncidencia Estado { get; set; } = EstadoIncidencia.Abierta;

    [Display(Name = "Fecha de creación")]
    public DateTime FechaCreacion { get; set; }
}
