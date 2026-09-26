namespace PlataformaIncidencias.Services;

// Se enlaza con la sección "Algolia" de la configuración (appsettings, user-secrets o
// variables de entorno como Algolia__ApiKey). La ApiKey solo se usa en el servidor.
public class AlgoliaOptions
{
    public const string Seccion = "Algolia";

    public string ApplicationId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string IndexName { get; set; } = string.Empty;

    // Si es true, al iniciar la aplicación se suben las incidencias de SQLite al índice.
    public bool IndexarAlIniciar { get; set; }

    public bool EstaConfigurado =>
        !string.IsNullOrWhiteSpace(ApplicationId) &&
        !string.IsNullOrWhiteSpace(ApiKey) &&
        !string.IsNullOrWhiteSpace(IndexName);
}
