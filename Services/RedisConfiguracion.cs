using StackExchange.Redis;

namespace PlataformaIncidencias.Services;

public static class RedisConfiguracion
{
    // Acepta el formato de StackExchange.Redis ("host:6379,password=...") o una URL
    // "redis://usuario:clave@host:6379" / "rediss://..." como la que entrega Render.
    public static ConfigurationOptions Crear(string connectionString)
    {
        ConfigurationOptions opciones;
        if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) &&
            (uri.Scheme == "redis" || uri.Scheme == "rediss"))
        {
            opciones = new ConfigurationOptions { Ssl = uri.Scheme == "rediss" };
            opciones.EndPoints.Add(uri.Host, uri.IsDefaultPort ? 6379 : uri.Port);

            var credenciales = uri.UserInfo.Split(':', 2);
            if (credenciales.Length == 2)
            {
                opciones.User = string.IsNullOrEmpty(credenciales[0]) ? null : Uri.UnescapeDataString(credenciales[0]);
                opciones.Password = Uri.UnescapeDataString(credenciales[1]);
            }
            else if (credenciales[0].Length > 0)
            {
                opciones.Password = Uri.UnescapeDataString(credenciales[0]);
            }
        }
        else
        {
            opciones = ConfigurationOptions.Parse(connectionString);
        }

        // Si Redis no está disponible la aplicación arranca igual y reintenta en segundo plano.
        opciones.AbortOnConnectFail = false;
        opciones.ConnectTimeout = 3000;
        opciones.SyncTimeout = 3000;
        opciones.AsyncTimeout = 3000;
        return opciones;
    }
}
