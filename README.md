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

## Búsqueda con Algolia

La búsqueda (`/Operaciones/Incidencias?q=texto`) se ejecuta en el servidor: el controlador
consulta Algolia, obtiene los `objectID` (= Id de la incidencia) y luego filtra en SQLite
solo las incidencias con `Estado = Abierta`. La ApiKey nunca se envía al navegador.

| Configuración | Variable de entorno | Descripción |
|---|---|---|
| `Algolia:ApplicationId` | `Algolia__ApplicationId` | Application ID de Algolia |
| `Algolia:ApiKey` | `Algolia__ApiKey` | Clave del servidor (secreta) |
| `Algolia:IndexName` | `Algolia__IndexName` | Nombre del índice (por defecto `incidencias`) |
| `Algolia:IndexarAlIniciar` | `Algolia__IndexarAlIniciar` | `true` para subir las incidencias de SQLite al índice al iniciar |

En desarrollo:

```bash
dotnet user-secrets set "Algolia:ApplicationId" "TU_APP_ID"
dotnet user-secrets set "Algolia:ApiKey" "TU_API_KEY"
dotnet user-secrets set "Algolia:IndexarAlIniciar" "true"   # solo para poblar el índice
```

Para indexar, la clave necesita permisos `addObject` y `editSettings` (p. ej. la Admin API Key).
Si solo se busca sobre un índice ya poblado, basta una clave con permiso `search`.
Los documentos del índice tienen `objectID`, `estacion`, `descripcion` y `prioridad`, con
`searchableAttributes = [estacion, descripcion]`.

Sin configuración de Algolia la aplicación funciona igual; solo la búsqueda muestra un aviso.

## Ejecutar

```bash
dotnet ef database update   # opcional: la app también aplica las migraciones al iniciar
dotnet run --launch-profile http
```

Abre http://localhost:5248/Operaciones/Incidencias e inicia sesión con el Supervisor.
