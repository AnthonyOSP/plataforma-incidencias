using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlataformaIncidencias.Data;
using PlataformaIncidencias.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.Configure<AlgoliaOptions>(builder.Configuration.GetSection(AlgoliaOptions.Seccion));
builder.Services.AddSingleton<IAlgoliaService, AlgoliaService>();

// Redis es opcional: sin ConnectionStrings:Redis el listado se lee siempre de SQLite.
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        ConnectionMultiplexer.Connect(RedisConfiguracion.Crear(redisConnectionString)));
}
builder.Services.AddScoped<IIncidenciasCacheService, IncidenciasCacheService>();

builder.Services.Configure<PieHostOptions>(builder.Configuration.GetSection(PieHostOptions.Seccion));
builder.Services.AddHttpClient<IPieHostPublisher, PieHostPublisher>(client =>
    client.Timeout = TimeSpan.FromSeconds(5));

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

await SeedData.InicializarAsync(app.Services);
await SeedData.IndexarEnAlgoliaAsync(app.Services);

// La base SQLite puede haberse recreado (p. ej. en cada despliegue de Render):
// se descarta el listado cacheado anterior para no mostrar datos de otra base.
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<IIncidenciasCacheService>().InvalidarAbiertasAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
app.MapRazorPages()
    .WithStaticAssets();


app.Run();
