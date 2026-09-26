using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PlataformaIncidencias.Models;

namespace PlataformaIncidencias.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Incidencia> Incidencias => Set<Incidencia>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Incidencia>(entity =>
        {
            // Guardar los enums como texto para que la base de datos sea legible.
            entity.Property(i => i.Prioridad).HasConversion<string>().HasMaxLength(20);
            entity.Property(i => i.Estado).HasConversion<string>().HasMaxLength(20);

            entity.HasData(
                new Incidencia { Id = 1, Estacion = "Estación Parque Kennedy", Descripcion = "Anclaje 4 no libera la bicicleta.", Prioridad = Prioridad.Alta, Estado = EstadoIncidencia.Abierta, FechaCreacion = new DateTime(2026, 9, 20, 8, 15, 0) },
                new Incidencia { Id = 2, Estacion = "Estación Plaza San Martín", Descripcion = "Pantalla del tótem apagada.", Prioridad = Prioridad.Media, Estado = EstadoIncidencia.Abierta, FechaCreacion = new DateTime(2026, 9, 21, 10, 30, 0) },
                new Incidencia { Id = 3, Estacion = "Estación Larcomar", Descripcion = "Bicicleta 1023 con frenos dañados.", Prioridad = Prioridad.Critica, Estado = EstadoIncidencia.Abierta, FechaCreacion = new DateTime(2026, 9, 22, 7, 45, 0) },
                new Incidencia { Id = 4, Estacion = "Estación Campo de Marte", Descripcion = "Lector de tarjetas intermitente.", Prioridad = Prioridad.Baja, Estado = EstadoIncidencia.Abierta, FechaCreacion = new DateTime(2026, 9, 23, 16, 5, 0) },
                new Incidencia { Id = 5, Estacion = "Estación Parque de la Reserva", Descripcion = "Estación sin conexión a internet.", Prioridad = Prioridad.Alta, Estado = EstadoIncidencia.Cerrada, FechaCreacion = new DateTime(2026, 9, 18, 9, 0, 0) },
                new Incidencia { Id = 6, Estacion = "Estación Parque Kennedy", Descripcion = "Neumático desinflado en bicicleta 0877.", Prioridad = Prioridad.Baja, Estado = EstadoIncidencia.Cerrada, FechaCreacion = new DateTime(2026, 9, 19, 12, 20, 0) }
            );
        });
    }
}
