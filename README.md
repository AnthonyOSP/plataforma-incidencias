## Demo publicada

- **URL:** https://plataforma-incidencias-7tto.onrender.com/
- **Pantalla principal:** `/Operaciones/Incidencias`

| Usuario de prueba | Contraseña | Rol |
|---|---|---|
| `supervisor@bicis.local` | `Profe#Examen2026` | Supervisor (puede cerrar incidencias) |

> Cuenta solo de prueba. Render (plan gratuito) se "duerme" tras 15 min sin uso: la primera carga
> puede tardar ~30–50 s. Al reiniciarse, la base SQLite vuelve a los datos iniciales.

---

## Base del proyecto

- **Identity:** login con usuario y contraseña; roles `Supervisor` y `Usuario`. El usuario Supervisor
  se crea al iniciar la aplicación; su contraseña se lee de la configuración, nunca del código.
- **EF Core + SQLite:** entidad `Incidencia` (`Id`, `Estacion`, `Descripcion`, `Prioridad`, `Estado`,
  `FechaCreacion`) con enums `Prioridad` (Baja, Media, Alta, Critica) y `EstadoIncidencia`
  (Abierta, Cerrada). Migraciones aplicadas automáticamente al iniciar.
- **Datos iniciales:** 6 incidencias (4 abiertas, 2 cerradas).
- **Cerrar incidencia:** `POST /Operaciones/Cerrar/{id}`, protegido con antifalsificación y
  `[Authorize(Roles = "Supervisor")]`. Un usuario sin ese rol no ve el botón y recibe "Acceso denegado".

---


## Cómo probar

Inicia sesión con el usuario de prueba y abre **Incidencias**.

1. **Algolia:** busca `kennedy` → solo la #1 (la #6 coincide pero está cerrada). Busca `frenos` → la #3.
   Etiqueta: *Algolia + SQLite*.
2. **Redis:** sin búsqueda, la primera carga muestra *SQLite (guardado en Redis por 60 s)*; al recargar
   antes de 60 s muestra *Redis (caché)*. Tras cerrar una incidencia vuelve a *SQLite* (caché invalidada).
3. **PieHost:** abre la lista en dos navegadores (uno en incógnito, ambos con sesión). El indicador dice
   *Tiempo real: conectado*. Cierra una incidencia en uno: desaparece sola en el otro, sin F5.

En los logs de Render, cada cierre muestra:
`Incidencia N cerrada y guardada en SQLite` → `Caché invalidada` → `Evento IncidenciaActualizada publicado en PieHost`.


