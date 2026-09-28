# Estado actual de implementación

Última actualización: 23 de septiembre de 2026.

Este documento es el punto de reanudación del proyecto. Debe leerse junto con `product-contract.md`, `implementation-plan.md` y `phase-d1-foundation.md`. No reemplaza esos contratos.

## Estado actual de PostgreSQL/Neon

Con autorización explícita se aplicaron las migraciones D1–D7. La base está actualmente en `20260923134536_AddDashboardExportsAndDeliveries`; las 18 migraciones aparecen registradas y EF no detecta cambios de modelo pendientes. Este estado reemplaza las notas históricas de cada bloque.

El control posterior confirmó cinco cuentas Meta y cinco fuentes Meta, sin cuentas huérfanas ni cruces de cliente. Las tablas de carpetas, dashboards, métricas genéricas, agendas, enlaces y marca respondieron correctamente. Backend, OpenAPI y frontend devolvieron HTTP 200.

## Bloque terminado: D1.1 — conexiones y fuentes genéricas

Se implementó la primera base persistente para que AnalitiAds pueda representar las cuatro fuentes confirmadas sin crear otro backend:

- proveedores `MetaAds`, `GoogleAds`, `TikTokAds` y `GoogleAnalytics4`;
- tipos de fuente `AdvertisingAccount` y `AnalyticsProperty`;
- entidad `ProviderConnection` para autorizaciones protegidas por agencia;
- entidad `DataSource` para cuentas o propiedades asignadas a un cliente;
- estado de conexión, vencimiento, último uso correcto, error sanitizado, intervalo conocido de datos, última sincronización, archivado y versión de concurrencia;
- restricciones relacionales que impiden asociar una fuente con un cliente o una conexión de otra agencia;
- unicidad de fuente por agencia, proveedor, tipo e identificador externo.

Google Analytics 4 solo acepta `AnalyticsProperty`. Meta Ads, Google Ads y TikTok Ads solo aceptan `AdvertisingAccount`. En este bloque todavía no se implementaron sus OAuth ni sus consultas reales.

## Compatibilidad con Meta existente

La integración Meta anterior continúa disponible. `MetaConnection`, `AdAccount`, campañas, conjuntos, anuncios, métricas, informes y enlaces históricos no fueron eliminados.

El callback OAuth de Meta ahora guarda la misma credencial protegida en la conexión histórica y en `ProviderConnection`. Al asociar una cuenta Meta, crea también su `DataSource` y la vincula con esa autorización genérica. La cuenta histórica y la fuente usan el mismo identificador interno para conservar relaciones.

La migración `20260917195257_AddGenericDataSources` hace el cambio de manera aditiva:

1. añade temporalmente `ad_accounts.data_source_id` como nullable;
2. crea `provider_connections` y `data_sources`;
3. copia las conexiones Meta existentes;
4. crea una fuente Meta por cada cuenta existente, conservando ID, cliente, nombre, moneda, zona horaria y estado;
5. recupera inicio, fin y última observación desde snapshots/sincronizaciones cuando existen;
6. enlaza cada `ad_account` con su fuente;
7. recién entonces exige `NOT NULL`, unicidad y claves foráneas.

El SQL revisable se generó en `output/AddGenericDataSources.sql` y su reversión en `output/RemoveGenericDataSources.sql`. La reversión conserva las tablas históricas de Meta, pero elimina las tablas genéricas; solo debe usarse antes de almacenar fuentes nuevas exclusivamente allí. La migración **no fue aplicada a Neon**.

## Verificación ejecutada

```text
dotnet build AnaliticAsd.sln --no-restore --verbosity:minimal
Resultado: correcto, 0 advertencias, 0 errores.

dotnet test AnaliticAsd.sln --no-restore --logger "console;verbosity=minimal"
Resultado: 97 aprobadas, 0 fallidas, 0 omitidas.

dotnet ef migrations has-pending-model-changes --project AnaliticAsd --startup-project AnaliticAsd --no-build
Resultado: el modelo coincide con la última migración.
```

Las pruebas nuevas cubren compatibilidad proveedor/tipo, normalización de moneda, ampliación del intervalo sincronizado, reautorización, persistencia y eliminación coordinada de la fuente Meta, conexión genérica durante OAuth y rechazo de referencias entre agencias.

No había Docker ni `psql` instalados en el entorno de desarrollo. Por eso el SQL se generó y revisó, pero aún debe ejecutarse primero en una rama o copia aislada de PostgreSQL antes de considerar su aplicación a Neon.

## Control previo a una base compartida

Antes de aplicar la migración a Neon:

1. crear una rama/copia recuperable de la base;
2. ejecutar `output/AddGenericDataSources.sql` en esa copia;
3. comprobar que la cantidad de `data_sources` Meta coincide con `ad_accounts`;
4. comprobar que ninguna cuenta quedó sin `data_source_id`;
5. comprobar que IDs, clientes, campañas, snapshots, informes y enlaces siguen presentes;
6. ejecutar una conexión y sincronización Meta real contra la copia;
7. autorizar por separado la aplicación en la base compartida.

Consultas mínimas de control:

```sql
SELECT COUNT(*) FROM ad_accounts;
SELECT COUNT(*) FROM data_sources WHERE provider = 'MetaAds';
SELECT COUNT(*) FROM ad_accounts WHERE data_source_id IS NULL;
SELECT COUNT(*)
FROM ad_accounts AS account
LEFT JOIN data_sources AS source ON source.id = account.data_source_id
WHERE source.id IS NULL OR source.client_id <> account.client_id;
```

Los dos últimos resultados deben ser cero y los dos primeros deben coincidir mientras solo existan fuentes Meta.

## Bloque terminado: D1.2 — carpetas y editores asignados

Se implementaron carpetas/subcarpetas persistentes por cliente, búsqueda, movimiento, orden, archivado y restauración recursivos, prevención de ciclos, control de concurrencia y rechazo de cruces entre clientes o agencias.

Se añadió `ClientEditorAssignment`, separado del acceso externo `ClientAccess`. Owner/Admin administran asignaciones; Analyst solo accede a los clientes asignados; Viewer mantiene lectura interna; ClientViewer no puede explorar carpetas. Las asignaciones se revalidan en la base y una revocación invalida el alcance de un JWT ya emitido.

La migración `20260917202057_AddFoldersAndEditorAssignments` conserva el acceso anterior de Analysts existentes convirtiéndolo en asignaciones explícitas. Su SQL de aplicación y reversión está en `output/`. No fue aplicada a Neon.

Contrato, matriz de permisos y comportamiento completo: `d1-2-folders-and-editors.md`.

## Bloque terminado: D1.3 — dashboards persistentes

Se implementaron `Dashboard`, `DashboardDraft`, `DashboardVersion` y `DashboardTemplate`, definición JSON versión 1 validada, control de revisión para autosave, publicaciones inmutables con hash, archivado, restauración, movimiento, búsqueda, duplicación segura y plantillas con ranuras de fuente.

La migración `20260917212324_AddPersistentDashboards` y sus SQL de aplicación/reversión están en `output/`. No fue aplicada a Neon. Contrato, rutas, permisos, límites y recuperación: `d1-3-persistent-dashboards.md`.

La verificación del backend aprobó 106 pruebas. La compilación limpia anterior tuvo 0 advertencias y 0 errores; si Rider o el servidor mantiene la DLL abierta, debe detenerse ese proceso antes de repetir el build final.

## Bloque terminado: D1.4 — explorador frontend persistente

Se añadió `/app/informes` con cliente HTTP tipado y una vista de tres áreas para seleccionar cliente, navegar carpetas, buscar dashboards, crear, renombrar, mover, duplicar, archivar, restaurar y publicar. Muestra revisión del borrador, resumen de páginas/componentes, publicación vigente y conflictos. Cambiar de cliente limpia todo el contexto seleccionado.

El frontend aprobó lint y build de producción. El backend aprobó compilación con 0 advertencias/errores y 106 pruebas. La inspección visual automatizada quedó bloqueada porque Computer Use no tenía un navegador disponible; consultar `d1-4-frontend-explorer.md`.

## Bloque terminado: D2.1 — núcleo del editor visual

Se añadió `/app/informes/{dashboardId}/editar` con un lienzo persistente de una página, inserción de texto/KPI/forma, selección, movimiento, redimensionamiento, cuadrícula, propiedades, capas, bloqueo, duplicación, eliminación y deshacer/rehacer. El autoguardado usa la revisión del servidor y conserva los cambios locales ante un conflicto `409`.

El frontend aprobó lint y build de producción; el backend conserva 106 pruebas aprobadas. D2.1 no añade migraciones ni modifica Neon. Contrato y límites: `d2-1-visual-editor-core.md`.

## Bloque terminado en código: D2.2 — herramientas avanzadas del editor

El editor ahora administra hasta 25 páginas, duplicación y orden, tamaños y fondos, selección múltiple con Shift o rectángulo, movimiento conjunto, alineación, distribución, grupos, capas, bloqueo, copiar/cortar/pegar, zoom, guías y vista previa. Al salir con cambios pendientes muestra una advertencia y el autoguardado conserva el control de revisión.

Las operaciones geométricas se extrajeron a `frontend/lib/dashboard-editor-model.ts` y tienen tres pruebas nativas. Frontend lint/build, esas tres pruebas y las 106 pruebas del backend aprobaron. Computer Use no expuso un navegador, por lo que la inspección visual y la prueba integral de reapertura contra PostgreSQL siguen pendientes. Las migraciones D1.1–D1.3 aún no se aplicaron a Neon. Detalle: `d2-2-advanced-editor.md`.

## Bloque terminado: D3.1 — catálogo y contrato de consultas

Se implementó un catálogo Meta normalizado, fuentes por cliente y consultas de total, fecha o campaña sobre snapshots persistidos. El servidor valida fuente, dimensión, métricas, fechas, filtros, monedas y alcance. Los ratios usan totales y los estados de ausencia permanecen diferenciados.

Las tarjetas KPI del editor ya seleccionan una cuenta Meta, métrica e intervalo y muestran exclusivamente valores devueltos por el backend. La sincronización Meta actualiza la frescura de la fuente genérica. La validación aprobó frontend lint/build, 3 pruebas del editor, build backend sin advertencias, 114 pruebas backend y modelo EF sin cambios pendientes. D3.1 no añade migraciones ni modifica Neon. Detalle: `d3-1-data-catalog-query-contract.md`.

## Bloque terminado: D3.2 — configuración visual y gráficos básicos

Se añadieron tabla, serie temporal, barras y columnas conectadas a consultas Meta persistidas. Comparten selección de fuente, dimensión, métricas, fechas, decimales, orden, límite, título, color y leyenda. Hay estados explícitos de carga, ausencia y error; barras/columnas admiten una métrica, tablas hasta tres y series hasta tres de la misma unidad.

El backend valida `sortMetric`/`sortDirection` y ordena antes de limitar. Frontend lint/build, 3 pruebas del editor y 116 pruebas backend aprobaron. No hay cambios de modelo, migraciones ni aplicación a Neon. Detalle: `d3-2-basic-visualizations.md`.

## Bloque terminado: D3.3 — visualizaciones iniciales restantes

Se añadieron tabla dinámica transpuesta, barras apiladas y al 100 %, circular, dona, área, combinado, embudo, dispersión, burbujas, medidor de objetivo, imagen HTTPS y separador. Cada componente reutiliza el panel de datos y restringe dimensión, cantidad de métricas y unidad según lo que el runtime Meta puede representar.

También se alinearon los identificadores del frontend con el enum persistido del backend (`horizontalBar`, `column`, `divider` y restantes tipos D3.3). La validación aprobó 6 pruebas frontend, lint, build de producción y 131 pruebas backend. No hubo cambios de modelo, migraciones ni aplicación a Neon. La tabla dinámica de dos dimensiones y la carga propia de imágenes siguen pendientes de D5/D6. Detalle: `d3-3-initial-visualizations.md`.

## Bloque terminado en código: D4 — conectores restantes

Se añadieron Google Ads, TikTok Ads y Google Analytics 4 como adaptadores separados con autorización, credenciales protegidas, renovación Google, descubrimiento, asignación por cliente y sincronización manual normalizada e idempotente. El frontend incluye pantallas para conectar, descubrir, asignar y sincronizar. La migración `20260922151443_AddProviderMetricSnapshots` y sus SQL revisables no fueron aplicados a Neon.

Backend: build con 0 advertencias/errores y 135 pruebas aprobadas. Frontend: lint, 6 pruebas y build aprobados. EF confirma que el modelo coincide con la migración. La verificación con proveedores reales continúa bloqueada por credenciales, aprobación de aplicaciones y cuentas de prueba; D4 no se considera verificado en producción hasta contrastar cifras reales. Detalle: `d4-provider-connectors.md`.

## Bloque terminado en código: D5 — sincronización operativa y consultas multicanal

Se implementaron agendas persistentes por fuente, worker servidor, bloqueo recuperable, reintento exponencial, estado de fallos/éxitos y refresco móvil reciente. El editor consulta las cuatro plataformas, filtra valores de dimensión, compara con el período anterior y puede unir dos fuentes como series identificadas. El backend ofrece unión de hasta diez fuentes, rechaza monedas incompatibles y calcula CTR, CPC, CPM, CPA y ROAS desde totales.

La migración `20260922162515_AddProviderSyncSchedules` y sus SQL revisables no fueron aplicados a Neon. Backend: 138 pruebas, build sin advertencias y modelo EF alineado. Frontend: lint, 6 pruebas y build aprobados. La operación contra proveedores reales continúa bloqueada por credenciales/aprobaciones y por aplicar D1/D4/D5 en una base de prueba. Detalle: `d5-operational-sync-and-multichannel.md`.

## Bloque terminado: D6.1 — branding y enlaces de dashboards

Se añadieron perfiles de marca de agencia/cliente, enlaces propios de dashboard sobre la publicación vigente, token almacenado solo como hash, contraseña y destinatario opcionales, vencimiento, revocación, permisos de filtros/exportación/embed y registro de accesos. El frontend incorpora configuración de marca, gestión de enlaces y lector público responsive.

Las migraciones `20260922182726_AddDashboardSharingAndBranding` y `20260922182817_EnforceAgencyBrandUniqueness` no fueron aplicadas a Neon. Backend: 141 pruebas; frontend: lint y build aprobados; EF sin cambios pendientes. Detalle: `d6-1-sharing-branding.md`.

## Bloque terminado en código: D6.2 — datos públicos, plantillas y distribución

Se añadió la consulta pública restringida a las fuentes, dimensiones y métricas de la versión publicada; cuando los filtros están deshabilitados también se fijan las fechas. El lector reutiliza las visualizaciones reales, aplica la marca de agencia o cliente y preserva los valores ausentes. También se añadieron controles completos de enlace y la instalación idempotente de cinco plantillas editables con ranuras de fuente.

Backend: build sin advertencias y 146 pruebas aprobadas, incluidas cinco de la política pública. Frontend: lint, 6 pruebas y build de 19 páginas aprobados. Se corrigió el arranque en Windows aislando las claves de desarrollo, evitando Event Log y deshabilitando con diagnóstico el worker cuando falta su tabla. D6.2 no agrega migraciones. Las migraciones D1–D6 ya fueron aplicadas a Neon y verificadas; el SQL idempotente revisable está en `output/UpgradeNeonToD6.sql`. El destinatario funciona como dato secreto adicional, no como verificación de propiedad del correo. Detalle: `d6-2-public-data-templates.md`.

## Bloque terminado en código: D7 — exportaciones y entregas programadas

Se implementaron trabajos persistentes para PDF, CSV y Excel, historial y descargas autenticadas,
exportaciones públicas condicionadas por `allowExport`, agendas diarias/semanales/mensuales y un
worker de generación y correo SMTP que no depende del navegador. La interfaz interna permite crear
y descargar archivos y administrar entregas. Backend: build sin advertencias y 149 pruebas; frontend:
lint, 6 pruebas y build de 19 páginas aprobados.

La migración `20260923134536_AddDashboardExportsAndDeliveries` fue aplicada a Neon; las 18
migraciones están registradas y EF no detecta cambios pendientes. El envío real requiere
configuración SMTP y una dirección de prueba. Detalle:
`d7-exports-deliveries.md`.

## Bloque terminado en código: D8 — calidad y producción

Se añadieron mapa, bala, árbol, Sankey, cascada, caja y bigotes, velas y línea de tiempo al contrato,
editor y lector. El editor admite movimiento por teclado; la interfaz respeta reducción de movimiento,
foco y alto contraste. El backend expone readiness de PostgreSQL, correlación y cabeceras de seguridad.
La carga local completó 200 solicitudes con concurrencia 10, promedio 8,44 ms y p95 87,52 ms.

Backend: build sin advertencias y 151 pruebas. Frontend: lint, 6 pruebas y build de 19 rutas. D8 no
agrega migraciones. Detalle: `d8-quality-production.md`.

## Cierre externo pendiente

La implementación D1–D8 está terminada en código. La aceptación de producción sigue condicionada a
cuentas reales de Google Ads, TikTok Ads y GA4, contraste de cifras, una entrega SMTP real y revisión
visual manual en navegadores y tamaños de pantalla. El contrato impide sustituirlas con simulaciones.
