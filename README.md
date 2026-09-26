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

## Despliegue en Render

El repositorio incluye un `Dockerfile` (Render no tiene entorno nativo de .NET).

1. En Render: **New → Web Service**, conecta el repositorio de GitHub.
2. **Runtime / Language:** `Docker` (detecta el `Dockerfile` de la raíz).
3. **Instance type:** Free.
4. En **Environment** agrega estas variables (los valores secretos no están en el repositorio):

| Variable | Valor | ¿Secreta? |
|---|---|---|
| `SeedSupervisor__Password` | Contraseña del Supervisor para las pruebas | Sí |
| `Algolia__ApplicationId` | Application ID de Algolia | No |
| `Algolia__ApiKey` | API Key con permiso de escritura (para indexar) | Sí |
| `Algolia__IndexarAlIniciar` | `true` | No |
| `ConnectionStrings__Redis` | `redis://default:CLAVE@HOST:PUERTO` | Sí |
| `PieHost__ClusterUrl` | `https://free.blr2.piesocket.com` | No |
| `PieHost__ApiKey` | API Key de PieHost | No (pública) |
| `PieHost__Secret` | Secret de PieHost | Sí |

El `Dockerfile` ya define `ASPNETCORE_ENVIRONMENT=Production`, la ruta de SQLite
(`/app/data/incidencias.db`) y el puerto (usa la variable `PORT` que asigna Render).

Si en PieHost la API Key tiene dominios permitidos, añade el dominio `*.onrender.com` de tu servicio.

**Importante:** en el plan gratuito el disco es temporal. SQLite se recrea en cada despliegue o
cuando el servicio se reinicia tras 15 minutos sin uso: las incidencias vuelven a su estado inicial.
Al iniciar, la aplicación reindexa Algolia y descarta la caché de Redis anterior. La primera carga
tras estar inactivo puede tardar ~30–50 s.

## Cómo probar la aplicación publicada

Inicia sesión con `supervisor@bicis.local` y la contraseña configurada en `SeedSupervisor__Password`,
y abre **Incidencias**. Bajo el título, la etiqueta **Datos** indica de dónde salió la lista.

1. **Algolia (búsqueda):** busca `kennedy` → solo la #1 (la #6 coincide pero está cerrada).
   Busca `frenos` → la #3. Etiqueta: *Algolia + SQLite*.
2. **Redis (caché 60 s):** sin búsqueda, la primera carga muestra *SQLite (guardado en Redis por 60 s)*;
   al recargar antes de 60 s muestra *Redis (caché)*. Al cerrar una incidencia, la siguiente carga
   vuelve a *SQLite* porque la caché se invalidó.
3. **PieHost (tiempo real):** abre la lista en dos navegadores (uno en incógnito, ambos con sesión).
   El indicador debe decir *Tiempo real: conectado*. Cierra una incidencia en uno: desaparece sola
   en el otro, sin recargar.

En la pestaña **Logs** de Render se ve la secuencia de cada cierre:
`Incidencia N cerrada y guardada en SQLite` → `Caché invalidada` → `Evento IncidenciaActualizada publicado en PieHost`.
