using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PlataformaIncidencias.Services;

namespace PlataformaIncidencias.Data;

public static class SeedData
{
    // Aplica las migraciones pendientes y crea los roles y el usuario Supervisor de prueba.
    // La contraseña del Supervisor NUNCA se guarda en el repositorio: se lee de la
    // configuración local (user-secrets o variable de entorno SeedSupervisor__Password).
    public static async Task InicializarAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(SeedData));

        var context = provider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();

        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var rol in new[] { Roles.Supervisor, Roles.Usuario })
        {
            if (!await roleManager.RoleExistsAsync(rol))
            {
                await roleManager.CreateAsync(new IdentityRole(rol));
            }
        }

        var configuration = provider.GetRequiredService<IConfiguration>();
        var email = configuration["SeedSupervisor:Email"];
        var password = configuration["SeedSupervisor:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("No se creó el usuario Supervisor: configure SeedSupervisor:Email y SeedSupervisor:Password (ver README).");
            return;
        }

        var userManager = provider.GetRequiredService<UserManager<IdentityUser>>();
        var supervisor = await userManager.FindByEmailAsync(email);
        if (supervisor is null)
        {
            supervisor = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            var resultado = await userManager.CreateAsync(supervisor, password);
            if (!resultado.Succeeded)
            {
                logger.LogError("No se pudo crear el Supervisor: {Errores}",
                    string.Join("; ", resultado.Errors.Select(e => e.Description)));
                return;
            }
        }

        if (!await userManager.IsInRoleAsync(supervisor, Roles.Supervisor))
        {
            await userManager.AddToRoleAsync(supervisor, Roles.Supervisor);
        }
    }

    // Sube las incidencias de SQLite al índice de Algolia si Algolia:IndexarAlIniciar es true.
    // Requiere una ApiKey con permiso de escritura (addObject y editSettings).
    public static async Task IndexarEnAlgoliaAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var options = provider.GetRequiredService<IOptions<AlgoliaOptions>>().Value;
        var algolia = provider.GetRequiredService<IAlgoliaService>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(SeedData));

        if (!options.IndexarAlIniciar || !algolia.EstaConfigurado)
        {
            return;
        }

        try
        {
            var context = provider.GetRequiredService<ApplicationDbContext>();
            var incidencias = await context.Incidencias.AsNoTracking().ToListAsync();
            await algolia.IndexarAsync(incidencias);
            logger.LogInformation("Se indexaron {Cantidad} incidencias en Algolia ({Indice}).", incidencias.Count, options.IndexName);
        }
        catch (Exception ex)
        {
            // La aplicación debe poder iniciar aunque Algolia no responda.
            logger.LogError(ex, "No se pudieron indexar las incidencias en Algolia.");
        }
    }
}
