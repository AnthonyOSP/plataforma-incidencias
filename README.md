# PlataformaIncidencias

Gestión de incidencias para una empresa de bicicletas compartidas (ASP.NET Core MVC, .NET 10, Identity, EF Core + SQLite).

## Tiempo real con PieHost (WebSocket)

Al cerrar una incidencia, el servidor guarda el cambio en SQLite y **después** publica en
PieHost el evento `IncidenciaActualizada` con `{ "Id": 3, "Estado": "Cerrada" }`
(`POST {ClusterUrl}/api/publish`, protocolo V3). La pantalla `/Operaciones/Incidencias` se
suscribe a `wss://.../v3/{Canal}?api_key=...` y quita la fila sin recargar. Al (re)conectar
consulta `GET /Operaciones/IncidenciasAbiertas` para sincronizarse.

| Configuración | Variable de entorno | ¿Secreta? |
|---|---|---|
| `PieHost:ClusterUrl` | `PieHost__ClusterUrl` | No (p. ej. `https://free.nyc1.piesocket.com`) |
| `PieHost:ApiKey` | `PieHost__ApiKey` | No: el navegador la usa para suscribirse |
| `PieHost:Secret` | `PieHost__Secret` | **Sí**: solo en el servidor |
| `PieHost:Canal` | `PieHost__Canal` | No (por defecto `incidencias`) |

```bash
dotnet user-secrets set "PieHost:ClusterUrl" "https://TU_CLUSTER.piesocket.com"
dotnet user-secrets set "PieHost:ApiKey" "TU_API_KEY"
dotnet user-secrets set "PieHost:Secret" "TU_SECRET"
```

Si PieHost no está configurado o no responde, el cierre se guarda igual y el error queda en el log.

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
