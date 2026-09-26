namespace PlataformaIncidencias.Services;

// Sección "PieHost" de la configuración (appsettings, user-secrets o variables de
// entorno como PieHost__Secret). El Secret solo se usa en el servidor.
public class PieHostOptions
{
    public const string Seccion = "PieHost";

    // URL del cluster, p. ej. https://free.nyc1.piesocket.com
    public string ClusterUrl { get; set; } = string.Empty;

    // API Key pública: el navegador la necesita para suscribirse al canal.
    public string ApiKey { get; set; } = string.Empty;

    // Secret privado: solo para publicar desde el servidor. Nunca se envía al navegador.
    public string Secret { get; set; } = string.Empty;

    // Canal (roomId) donde se publican los cambios de incidencias.
    public string Canal { get; set; } = "incidencias";

    public bool EstaConfigurado =>
        !string.IsNullOrWhiteSpace(ClusterUrl) &&
        !string.IsNullOrWhiteSpace(ApiKey) &&
        !string.IsNullOrWhiteSpace(Secret) &&
        !string.IsNullOrWhiteSpace(Canal);
}
