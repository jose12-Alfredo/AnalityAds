# Fase D1: fundamentos y migración segura

Fecha: 17 de septiembre de 2026. Esta es la próxima fase de implementación. Su propósito es preparar el sistema para Meta Ads, Google Ads, TikTok Ads y Google Analytics 4, y crear la base persistente del constructor de dashboards sin interrumpir las funciones actuales.

## Resultado esperado

Al cerrar D1, la aplicación podrá organizar clientes en carpetas, crear dashboards con páginas y componentes persistentes, guardar un borrador y publicar una versión. Las cuentas Meta existentes seguirán funcionando, pero el dominio ya aceptará fuentes de los cuatro proveedores.

Una misma agencia podrá mantener varios clientes y varios dashboards personalizados por cliente al mismo tiempo. La selección del cliente definirá el espacio de trabajo y todas las operaciones se validarán también en el servidor.

D1 no implementa todavía OAuth de Google/TikTok/GA4, sincronización multicanal ni el lienzo visual completo. Esas funciones dependen de esta base.

## Decisiones confirmadas

- Backend único: `AnaliticAsd.sln` y `AnaliticAsd/`.
- Frontend único: `frontend/`.
- Persistencia: PostgreSQL; Neon puede continuar como alojamiento de producción.
- Proveedores iniciales: `MetaAds`, `GoogleAds`, `TikTokAds`, `GoogleAnalytics4`.
- GA4 se modela como propiedad analítica; no se finge que es una cuenta publicitaria.
- Los reportes inmutables de Fases 11–12 y sus enlaces permanecen sin cambios.
- Las definiciones editables usan rutas `/api/v1/dashboards`; no reutilizan `/api/v1/reports`.
- Borrador y publicación son versiones distintas. El lector nunca accede al borrador.
- Toda escritura usa control de concurrencia optimista para impedir que un autosave silencie cambios de otro editor.

## Modelo de datos objetivo

### Conexiones y fuentes

`ProviderConnection` representa una autorización de una agencia con un proveedor. Debe admitir más de una conexión por agencia y proveedor.

Campos mínimos:

- `Id`, `AgencyId`, `Provider`;
- nombre visible y sujeto externo autorizado;
- credencial protegida en servidor y vencimiento cuando exista;
- estado `Pending`, `Connected`, `Expired`, `Revoked`, `Error`;
- última autorización, último uso correcto y último error sanitizado;
- timestamps y versión de concurrencia.

`DataSource` representa una cuenta publicitaria o propiedad GA4 asignada a un cliente.

- `Id`, `AgencyId`, `ClientId`, `ProviderConnectionId`;
- `Provider`, `SourceType`, `ExternalId`, nombre;
- moneda opcional, zona horaria y estado;
- inicio/fin conocido de datos y última sincronización correcta;
- timestamps, archivado y versión de concurrencia.

La unicidad se comprueba por agencia, proveedor, tipo e identificador externo. Una fuente no puede cambiar de agencia y no puede asignarse a un cliente fuera de su agencia.

### Organización

`Folder` pertenece a una agencia y un cliente, admite `ParentFolderId`, nombre, orden y archivado recuperable. Se bloquean ciclos y cruces entre clientes.

`Dashboard` pertenece a agencia/cliente y opcionalmente a una carpeta. Guarda título, descripción, estado, branding, creador, borrador vigente, publicación vigente, archivado y concurrencia.

No existe un dashboard sin cliente. Las consultas, componentes, filtros, enlaces y exportaciones heredan el `ClientId` del dashboard; no aceptan un `ClientId` alternativo enviado libremente por el navegador.

`DashboardVersion` es inmutable después de publicarse. Guarda número de esquema, revisión, fecha, autor y `DefinitionJson` validado. La definición contiene:

- páginas con ID, nombre, orden, tamaño y fondo;
- componentes con ID estable, tipo, posición, tamaño, capa, bloqueo y grupo;
- configuración de datos, estilo, filtros y controles;
- referencias a `DataSourceId`, nunca tokens ni credenciales.

El borrador puede reemplazarse mediante autosave condicionado por su revisión. Publicar crea una copia inmutable del borrador y actualiza el puntero publicado dentro de una transacción.

`DashboardTemplate` reutiliza el mismo esquema de definición, pero sus fuentes son ranuras que deben reasignarse. Duplicar a otro cliente nunca copia `DataSourceId` del cliente original.

### Permisos

Mapeo inicial con los roles existentes:

| Rol actual | Capacidad D1 |
|---|---|
| Owner/Admin | Administrar clientes, fuentes, carpetas, dashboards, plantillas y publicación. |
| Analyst | Editar únicamente clientes asignados. |
| Viewer | Lectura interna de dashboards publicados dentro de su alcance. |
| ClientViewer | Lectura de clientes asignados y versiones publicadas. |
| Visitante por enlace | Lectura exclusiva de la publicación autorizada. |

Crear una asignación persistente para editores internos si el modelo actual no limita `Analyst` por cliente. No reutilizar una asignación externa si eso mezcla reglas de invitación o revocación.

La interfaz debe permitir cambiar entre clientes sin mezclar estado: al cambiar de cliente se limpian carpeta, dashboard, página, fuentes y filtros seleccionados. Es válido editar dashboards de clientes diferentes en sesiones o pestañas simultáneas porque cada guardado incluye dashboard, cliente, agencia y revisión esperada.

## Migración de Meta sin pérdida

La transición será aditiva:

1. Añadir proveedor e identificador externo genérico a las cuentas existentes como columnas inicialmente compatibles con datos históricos.
2. Rellenar todas las cuentas actuales con `Provider = MetaAds` y `ExternalId = meta_account_id` dentro de la migración.
3. Crear los nuevos índices después del relleno y validar duplicados antes de exigir restricciones.
4. Adaptar servicios y contratos internos para leer la identidad genérica, manteniendo `MetaAccountId` en las rutas Meta existentes.
5. Mantener `meta_account_id` durante D1. Su eliminación solo puede ocurrir en una fase posterior, con despliegue verificado y migración específica.
6. No cambiar ni borrar `MetaConnection`, campañas, snapshots, reportes o enlaces históricos durante esta fase.

La migración debe poder probarse sobre una copia o base aislada antes de aplicarse a Neon. Aplicarla a la base compartida requiere revisar SQL generado, respaldo/recuperación disponible y autorización explícita.

## API inicial

Las rutas concretas se congelan al implementar, pero D1 debe cubrir:

```text
GET    /api/v1/clients/{clientId}/folders
POST   /api/v1/clients/{clientId}/folders
PATCH  /api/v1/folders/{folderId}
POST   /api/v1/folders/{folderId}/move
DELETE /api/v1/folders/{folderId}              # archivado
POST   /api/v1/folders/{folderId}/restore

GET    /api/v1/clients/{clientId}/dashboards
POST   /api/v1/clients/{clientId}/dashboards
GET    /api/v1/dashboards/{dashboardId}
PATCH  /api/v1/dashboards/{dashboardId}
POST   /api/v1/dashboards/{dashboardId}/move
POST   /api/v1/dashboards/{dashboardId}/duplicate
DELETE /api/v1/dashboards/{dashboardId}         # archivado
POST   /api/v1/dashboards/{dashboardId}/restore

PUT    /api/v1/dashboards/{dashboardId}/draft
POST   /api/v1/dashboards/{dashboardId}/publish
GET    /api/v1/dashboards/{dashboardId}/published
```

Crear y duplicar devuelve la ubicación del recurso. Autosave requiere la revisión esperada; un conflicto devuelve `409` con información suficiente para recargar sin sobrescribir datos.

## Orden de implementación

1. Congelar contratos y esquema JSON versión 1 con validadores y límites de tamaño/cantidad.
2. Añadir tipos de proveedor/fuente y migración aditiva de cuentas Meta.
3. Añadir conexiones/fuentes genéricas sin conectar proveedores nuevos todavía.
4. Añadir carpetas y asignación de editores.
5. Añadir dashboard, versiones, draft, publicación, archivado y duplicación segura.
6. Exponer API tenant-safe y documentarla en OpenAPI.
7. Añadir el explorador básico de carpetas/dashboards y persistencia de una definición mínima en frontend.
8. Ejecutar pruebas, build backend, lint/typecheck/build frontend y prueba manual de persistencia.
9. Revisar la migración SQL; solo después decidir su aplicación en Neon.

## Pruebas obligatorias

- Backfill Meta conserva IDs y todas las relaciones actuales.
- Un usuario nunca lee, mueve, duplica o publica recursos de otra agencia o cliente.
- Dos dashboards de clientes diferentes pueden guardarse y publicarse simultáneamente sin colisiones ni estado compartido.
- Listar carpetas, dashboards, fuentes, plantillas de cliente, enlaces o entregas nunca devuelve elementos de otro cliente.
- Un Analyst sin asignación recibe recurso no encontrado o permiso insuficiente según el contrato.
- No se pueden crear ciclos de carpetas ni mover entre clientes.
- Autosave con revisión vieja devuelve conflicto y conserva el último contenido.
- Publicar crea una versión inmutable y el borrador posterior no la modifica.
- Duplicar dentro del cliente copia diseño; duplicar a otro cliente elimina/reemplaza referencias de fuentes.
- Archivar y restaurar conservan estructura; los recursos archivados no aparecen por defecto.
- JSON inválido, tipo de componente desconocido, coordenadas fuera de límites y referencias de fuente ajena se rechazan.
- Todas las pruebas existentes de Meta, métricas, AnalysisEngine, reportes, PDF y enlaces siguen aprobando.

## Documentación que se entrega con D1

- migración y modelo de datos final;
- contrato HTTP y ejemplos de definición versión 1;
- matriz de roles y asignaciones;
- guía de configuración local sin secretos;
- pruebas ejecutadas y resultados;
- lista de funciones terminadas, pendientes y bloqueos externos;
- instrucciones de recuperación si falla el autosave o una migración.

## Criterio de cierre

D1 termina cuando una instalación limpia puede crear al menos dos clientes aislados, organizar carpetas y varios dashboards personalizados en cada uno, trabajar con ambos sin mezclar estado, persistir cambios, publicar versiones independientes y demostrar que un borrador posterior no altera lo publicado. También debe probarse que un usuario o fuente de un cliente no accede al otro. Meta y los reportes anteriores deben continuar funcionando. No se declara terminada si solo existen tablas, endpoints sin interfaz, mocks o una migración no probada.
