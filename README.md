# PlataformaIncidencias

Gestión de incidencias para una empresa de bicicletas compartidas (ASP.NET Core MVC, .NET 10, Identity, EF Core + SQLite).

## Configurar el usuario Supervisor (solo desarrollo)

El email del Supervisor está en `appsettings.json` (`SeedSupervisor:Email`).
La contraseña **no** está en el repositorio: se lee de la configuración local.

Con user-secrets (recomendado, se guarda fuera del proyecto):

```bash
dotnet user-secrets set "SeedSupervisor:Password" "TuClaveLocal#2026"
```

O con una variable de entorno:

```bash
export SeedSupervisor__Password="TuClaveLocal#2026"
```

La contraseña debe tener mayúscula, minúscula, número y un carácter especial (mínimo 6).
El usuario se crea solo si todavía no existe; para cambiar la contraseña después, borra
`incidencias.db` y vuelve a iniciar la aplicación.

## Ejecutar

```bash
dotnet ef database update   # opcional: la app también aplica las migraciones al iniciar
dotnet run --launch-profile http
```

Abre http://localhost:5248/Operaciones/Incidencias e inicia sesión con el Supervisor.

## Caché con Redis

El listado general de `/Operaciones/Incidencias` (sin texto de búsqueda) se guarda en Redis
con la clave `incidencias:abiertas` durante 60 segundos. Al cerrar una incidencia, primero se
guarda en SQLite y después se elimina esa clave. Las búsquedas por texto no usan la caché.

| Configuración | Variable de entorno |
|---|---|
| `ConnectionStrings:Redis` | `ConnectionStrings__Redis` |

Acepta `host:6379,password=...` o una URL `redis://usuario:clave@host:6379` (`rediss://` para TLS).

```bash
dotnet user-secrets set "ConnectionStrings:Redis" "localhost:6379"
```

Sin esa configuración, o si Redis se cae, la aplicación funciona igual leyendo de SQLite.
En los logs aparece `Cache HIT` o `Cache MISS` en cada consulta del listado.
