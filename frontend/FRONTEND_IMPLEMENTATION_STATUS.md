# Estado de implementación del frontend AnalitiAds

Fecha de revisión: 15 de septiembre de 2026.

## Actualización D1.4 — 21 de septiembre de 2026

Se añadió `/app/informes`, conectado a las rutas persistentes de carpetas, dashboards, borradores, publicaciones y plantillas. Owner/Admin/Analyst pueden organizar y publicar; Viewer tiene lectura interna; ClientViewer no entra al explorador. La vista limpia su selección completa al cambiar de cliente, muestra conflictos de concurrencia y avisa cuando una copia entre clientes requiere reasignar fuentes.

El lienzo visual libre continúa pendiente para D2. La validación actual aprobó `npm run lint` y `npm run build` con la ruta nueva generada correctamente.

Este documento describe el estado real del directorio `frontend/`. El contrato de la API usado por la interfaz proviene de `FRONTEND_HANDOFF.md`; la referencia de Claude se usó solamente para la composición visual. No se copiaron credenciales, IDs, llamadas a `window.claude.use` ni datos simulados del prototipo.

## Alcance realizado

### Plataforma y ejecución

- Aplicación Next.js 16.3.4 con React 19 y TypeScript.
- `npm run dev` ejecuta Next en `http://localhost:3001`.
- La URL de la API se obtiene únicamente desde `NEXT_PUBLIC_API_URL`. El archivo local está configurado para `http://localhost:5019`.
- El cliente HTTP está centralizado en `lib/api.ts`. Añade el encabezado Bearer, usa JSON camelCase, evita caché con `cache: "no-store"` y convierte respuestas fallidas a `ApiError` con Problem Details.
- Ninguna petición de negocio envía `agencyId`; la agencia se resuelve en el backend desde el token JWT.

### Autenticación y sesión

- Rutas públicas: `/login`, `/registro` y `/aceptar-invitacion`.
- `AuthScreen` usa `POST /api/v1/auth/login` o `POST /api/v1/auth/register`.
- La sesión se guarda en `sessionStorage`, no en `localStorage`, y se restaura con `GET /api/v1/auth/me` en el layout privado.
- Un 401 del cliente HTTP limpia la sesión y emite `analitiads:session-expired`; el layout redirige al login.
- El cierre de sesión es local y elimina la sesión de `sessionStorage`.
- Se centralizaron los roles `Owner`, `Admin`, `Analyst`, `Viewer` y `ClientViewer` y los permisos de Meta, clientes y accesos externos en `lib/auth-session.ts`.

### Shell de aplicación

- `/app` está protegido por el layout privado y muestra estado de validación de sesión mientras consulta `/api/v1/auth/me`.
- `PrivateShell` incorpora barra lateral, navegación, correo, agencia, rol, cambio de tema y salida local.
- El tema se guarda como preferencia visual en `sessionStorage`; no contiene un secreto ni un token MCP.
- El shell tiene una barra reducida para móvil y reglas CSS responsive en `app/globals.css`.
- La navegación se adapta al rol: ClientViewer no ve Meta ni MCP; clientes está disponible para roles internos autorizados a consultar o gestionar.

### Clientes y cuentas publicitarias

Rutas:

- `/app/clientes`
- `/app/clientes/[clientId]`
- `/app/clientes/[clientId]/cuentas/[adAccountId]`
- `/app/clientes/[clientId]/cuentas/[adAccountId]/campanas`

`ClientManagement` implementa:

- listado de clientes con estados de carga, vacío y errores;
- alta de clientes mediante `POST /api/v1/clients`;
- edición de nombre y activación mediante `PUT /api/v1/clients/{id}`;
- eliminación confirmada mediante `DELETE /api/v1/clients/{id}`;
- listado de cuentas por cliente con `GET /api/v1/clients/{id}/ad-accounts`;
- alta manual de cuenta con `POST /api/v1/clients/{id}/ad-accounts`;
- activación/desactivación mediante `PUT /api/v1/ad-accounts/{id}`;
- eliminación confirmada mediante `DELETE /api/v1/ad-accounts/{id}`;
- acceso a la jerarquía de cada cuenta.

Las acciones de creación y edición de cliente se muestran para Owner, Admin y Analyst. La eliminación de clientes y cuentas se limita a Owner y Admin. El backend conserva la autoridad final sobre permisos y pertenencia de agencia.

### Integración Meta

Ruta: `/app/configuracion/integraciones/meta`.

- Consulta de sesión, conexión y catálogo con `/api/v1/auth/me`, `/api/v1/meta/connection`, `/api/v1/clients` y `/api/v1/meta/ad-accounts`.
- El inicio de OAuth usa solamente la URL que responde `GET /api/v1/meta/oauth/start`; la interfaz no construye URLs OAuth ni llama a Graph API.
- Maneja los retornos `meta=success`, `meta=denied` y `meta=error`, muestra un aviso y borra el parámetro de la URL.
- Permite asociar una cuenta disponible mediante `POST /api/v1/clients/{clientId}/meta-ad-accounts`.
- Owner y Admin pueden iniciar o reconectar OAuth. Owner, Admin y Analyst pueden consultar y asociar cuentas, conforme a los permisos de interfaz.
- No se muestran ni persisten credenciales Meta.

### Jerarquía publicitaria

Rutas:

- `/app/campanas/[campaignId]`
- `/app/conjuntos/[adSetId]`
- `/app/anuncios/[adId]`

`AdvertisingWorkspace` consulta campañas, conjuntos y anuncios, permite avanzar Campaign -> AdSet -> Ad, muestra detalle y presenta estado configurado, estado efectivo, presencia en Meta y última sincronización. Los roles Owner, Admin y Analyst pueden solicitar sincronización mediante `POST /api/v1/ad-accounts/{id}/sync`. Owner y Admin pueden habilitar la visualización de objetos ausentes de Meta.

### Dashboard y métricas

El dashboard usa datos de la API y no genera métricas ficticias.

- `ClientDashboard` carga exclusivamente los clientes y cuentas devueltos para la sesión. Para ClientViewer, esto depende del filtro de asignaciones activo del backend.
- La selección de cliente, cuenta y recurso se limpia cuando la API informa una revocación o un recurso inaccesible.
- Se consulta la serie diaria por recurso con los endpoints de métricas de Account, Campaign, AdSet y Ad.
- `MetricsInsightsPanel` consulta además `.../metrics/summary` y muestra valores seguros solo cuando su disponibilidad lo permite.
- El rango se limita a 90 días en la interfaz. Las validaciones de fecha también se mantienen en el backend.
- La interfaz diferencia `null` de cero mostrado por la API, muestra cobertura y avisa cuando el backend informa `LegacyZeroNormalized`.
- No calcula reach agregado, frequency agregado ni promedios locales de ratios.

Para una cuenta publicitaria, también consume:

- `GET /api/v1/ad-accounts/{id}/campaigns/selector` con `WithActivity`, `WithSpend` o `All`;
- `GET /api/v1/ad-accounts/{id}/campaigns/benchmarks`.

Los benchmarks muestran CPM, CTR, CPC, CPL, CPA y ROAS contra la mediana de campañas comparables con el mismo objetivo. Se enseña el número de campañas comparables y que se trata de un benchmark interno. Los estados de métrica no disponibles se muestran sin inventar un porcentaje.

### Comparaciones temporales

El panel de métricas integra los cuatro endpoints de comparación:

- `/api/v1/ad-accounts/{id}/metrics/comparison`
- `/api/v1/campaigns/{id}/metrics/comparison`
- `/api/v1/ad-sets/{id}/metrics/comparison`
- `/api/v1/ads/{id}/metrics/comparison`

Permite `PreviousPeriod`, `PreviousMonth`, `PreviousYear` y `Custom`. En el modo personalizado solicita `comparisonSince` y `comparisonUntil`. La tabla expone el valor actual, baseline, cambio absoluto, cambio porcentual y disponibilidad. Para `UndefinedBaseline`, `CurrencyMismatch` u otro estado no disponible no muestra porcentajes calculados por el navegador.

### AnalysisEngine

El dashboard del cliente incorpora `AnalysisEnginePanel`, conectado a `POST /api/v1/analyses` mediante `lib/api.ts`.

- Usa la cuenta publicitaria autorizada seleccionada y permite definir `since` y `until` con el mismo límite inclusivo de 90 días.
- Permite `PreviousPeriod`, `PreviousMonth`, `PreviousYear`, `Custom` y sin comparación. El modo personalizado exige dos fechas válidas y anteriores al período actual.
- Permite analizar todas las campañas (`campaignIds: null`), solo la cuenta (`campaignIds: []`) o campañas concretas de la cuenta seleccionada. La interfaz descarta duplicados antes de enviar la solicitud y verifica que la selección siga perteneciendo a la cuenta actual.
- Permite elegir las 14 métricas soportadas. Sin una selección, solicita todas con `selectedMetrics: null`.
- Presenta el resultado general, campañas, conjuntos y anuncios con la cobertura, suficiencia `Sufficient`, `Partial` o `Insufficient`, y las razones textuales que devuelve el backend.
- Un valor se muestra solo con disponibilidad `CompleteForSnapshots`; `null`, `Incomplete`, `MixedCurrency`, `NotAvailableForRange` u otro estado se conservan como no disponibles, sin convertirlos en cero.
- Las comparaciones y benchmarks se muestran únicamente con los valores y disponibilidades devueltos por el backend. El navegador no calcula cambios, porcentajes, ratios ni benchmarks.
- La misma respuesta incorpora insights deterministas y recomendaciones de Fase 10. La interfaz presenta regla, alcance, severidad o prioridad, confianza, suficiencia, entidades afectadas, evidencia y acciones sin reinterpretar sus mensajes ni calcular sus diferencias.
- Los valores de evidencia se muestran solamente si el backend entrega `availability: Available`; los arrays vacíos explican que no se activaron reglas o no se propusieron acciones para el alcance.
- Audiencias, ubicaciones y creatividades se presentan como secciones no disponibles con la razón exacta proporcionada por `unavailableSections`.

### Reportes inmutables

Fase 11 integra reportes desde el mismo dashboard y rutas por cliente:

- Owner, Admin y Analyst pueden asignar un título y guardar como reporte la selección que generó el resultado de AnalysisEngine, mediante `POST /api/v1/reports`.
- `/app/clientes/[clientId]/reportes` lista los reportes autorizados con paginación; todos los roles pueden leer los que estén dentro de su alcance.
- `/app/clientes/[clientId]/reportes/[reportId]` consulta el `ReportData` persistido y representa exclusivamente `report.analysis`, sin reconstruirlo mediante llamadas a análisis, Meta o métricas actuales.
- El PDF se descarga con Bearer JWT como blob y revoca su URL temporal. No persiste PDF, blob ni URL en almacenamiento del navegador.
- Owner y Admin pueden eliminar un reporte tras confirmación. Los errores de autorización y los `404` limpian el detalle o la lista privada y refrescan el contexto autorizado.
- Ante `400` se muestra Problem Details; ante `401` el cliente central limpia la sesión; ante `403` se muestra falta de permiso; y ante `404` se limpia la selección del dashboard para bloquear datos que ya no pertenecen al usuario.

### Enlaces compartibles de reportes

Fase 12 añade administración autenticada y acceso público:

- Owner y Admin pueden crear, listar y revocar enlaces desde el detalle del reporte. La vigencia admite 1, 3, 7, 14 o 30 días dentro del contrato de 1 a 30.
- El token recién creado se muestra una sola vez como `/reportes-compartidos#token`; el listado posterior presenta únicamente metadatos y estado.
- `/reportes-compartidos` lee el fragmento en el cliente, limpia inmediatamente la barra con `history.replaceState` y conserva el token solo en memoria.
- El acceso y el PDF públicos envían `{ accessToken }` por body. No se usa JWT ni se coloca el token en path, query, logs, analytics, `localStorage` o `sessionStorage`.
- Los estados inválido, vencido, revocado o eliminado se presentan de forma uniforme como “Este enlace no está disponible”. El límite `429` tiene un estado propio.
- La vista privada y la pública incluyen un gráfico interactivo por campaña. La métrica y visibilidad de series cambian solo la presentación de `analysis`; no disparan llamadas nuevas ni recalculan cifras.

### Invitaciones y ClientViewer

Rutas:

- `/app/clientes/[clientId]/accesos`
- `/aceptar-invitacion`

Para Owner y Admin, `ClientAccessPanel` implementa:

- listado de invitaciones pendientes y accesos ClientViewer;
- creación con `POST /api/v1/clients/{id}/invitations`;
- visualización y copia puntual del código recibido al crear;
- revocación de invitaciones con `DELETE /api/v1/clients/{id}/invitations/{invitationId}`;
- revocación de accesos con `DELETE /api/v1/clients/{id}/users/{userId}`.

`InvitationAcceptance` acepta el código con `POST /api/v1/auth/client-invitations/accept`, contempla cuenta nueva o existente según la respuesta del backend y limpia el campo al finalizar. El código no se escribe en la URL, analytics ni almacenamiento persistente.

### Conexiones MCP

Ruta: `/app/configuracion/mcp`.

`McpConnectionsPanel` implementa el flujo de lectura de Fase 7B:

- lista conexiones con `GET /api/v1/mcp/connections`;
- exige confirmar consentimiento antes de crear con `POST /api/v1/mcp/connections` y `{ name, consentAccepted: true }`;
- mantiene el `accessToken` solo en el estado de la respuesta de creación, ofrece copiarlo y avisa que debe guardarse fuera del navegador;
- no lo coloca en URL, logs, analytics, `localStorage` ni `sessionStorage`;
- calcula y presenta los estados Activa, Revocada y Expirada;
- permite revocar con `DELETE /api/v1/mcp/connections/{id}`;
- explica que MCP es opcional y el dashboard REST funciona sin él.

## Estados y accesibilidad

Las pantallas incorporan estados de carga, sin datos, errores de Problem Details, sesión expirada, acceso denegado y recursos no encontrados cuando corresponde. Los controles son botones, enlaces, etiquetas e inputs nativos; se mantienen foco visible mediante los estilos globales y se usan `role="alert"` o `role="status"` para avisos relevantes.

## Áreas deliberadamente no funcionales

No se añadieron llamadas ni datos simulados para scoring, creatividades avanzadas, audiencias o breakdowns nuevos, escritura MCP, cambios de campañas en Meta ni IA interpretativa. AnalysisEngine y los reportes presentan únicamente la evidencia estructurada, los insights deterministas y las recomendaciones que devuelve el backend; las secciones que éste aún no implementa permanecen como no disponibles.

## Validaciones ejecutadas

Se utilizaron las dependencias existentes; no fue necesario modificar `package.json` ni instalar paquetes nuevos.

| Comprobación | Resultado |
| --- | --- |
| `npm.cmd run lint` | Correcto |
| `npx.cmd tsc --noEmit` | Correcto |
| `npm.cmd run build` | Correcto; Next generó 13 rutas, incluida `/reportes-compartidos` |
| Suite backend | Correcto; 81/81 pruebas, incluidos snapshot/PDF, roles, revocación de ClientViewer y enlaces compartibles |
| Historial de Neon | Correcto; diez migraciones registradas y `20260915181708_AddReportShareLinks` aplicada |
| `git diff --check` | Correcto para el contenido modificado |
| `GET /api/system/status` | Correcto; backend Healthy en `http://localhost:5019` |
| Preflight CORS desde `http://localhost:3001` | Correcto para `GET` con `Authorization` y `Content-Type` |
| Matriz HTTP de roles y aislamiento | Correcto; Owner, Admin, Analyst, Viewer, ClientViewer y segunda agencia autenticaron según el contrato |
| Servidor frontend | Activo en `http://localhost:3001`; `/login` responde `200` |
| AnalysisEngine — alcances | Correcto: `campaignIds: null` devolvió 9 campañas, 9 conjuntos y 9 anuncios; `[]` devolvió solo la cuenta; una campaña concreta devolvió exactamente 1 campaña, 1 conjunto y 1 anuncio. |
| AnalysisEngine — comparación y datos | Correcto: `Custom` con fechas válidas devolvió `200`; se verificaron `Sufficient`, `Partial`, `Insufficient`, `NoSnapshots`, `LegacyZeroNormalized`, moneda mixta, valores nulos/no disponibles, y benchmarks `Available` y `UndefinedBenchmark`. |
| AnalysisEngine — autorización | Correcto: Owner, Admin, Analyst, Viewer y ClientViewer recibieron `200` para sus cuentas; ClientViewer recibió un único cliente y `404` fuera de su asignación; la segunda agencia recibió `404` para la cuenta ajena. Solicitudes con campaña ajena/incorrecta devolvieron `400` y sin Bearer devolvieron `401`. |

El proyecto no contiene una suite de pruebas de frontend detectada. El aviso de npm sobre `min-release-age` procede de la configuración de npm y no produjo un error de lint, tipos o build.

## Pendiente para avanzar

### Integración verificada por HTTP

El backend responde en `http://localhost:5019`: el estado del sistema devuelve `200 Healthy`, CORS autoriza `http://localhost:3001` y OpenAPI confirma las rutas de accesos externos que consume la interfaz. OpenAPI publica `GET /api/v1/meta/oauth/start`; la llamada actual del frontend también es GET.

- Owner, Admin, Analyst, Viewer y ClientViewer completaron login, `/auth/me` y `/clients` con los roles esperados. ClientViewer recibió un solo cliente; Owner, Admin, Analyst y Viewer recibieron los tres de su agencia.
- Un cliente no asignado devolvió `404` a ClientViewer. El Owner de la segunda agencia recibió `404` al consultar un cliente de la primera. ClientViewer recibió `403` al intentar administrar accesos, Viewer recibió `403` al intentar sincronizar métricas y una llamada sin Bearer recibió `401`.
- Las rutas de accesos externos respondieron `200`; el cliente de demostración con acceso revocado expone una invitación con estado `Revoked`.
- Las series, resúmenes, comparaciones y benchmarks respondieron `200` para las cuentas disponibles. La campaña `Ventas principal` contiene `LegacyZeroNormalized`; los resúmenes muestran `Incomplete`, `MixedCurrency`, `NoData` y `NotAvailableForRange` sin cálculos locales.
- Los fixtures de benchmark completos A, B y C devuelven `Available` con dos campañas comparables; `Benchmark cero evaluado` devuelve `UndefinedBenchmark`. La comparación de `Cuenta demo USD` para el 1 de septiembre de 2026 contra `PreviousPeriod` devuelve `UndefinedBaseline` en los observados documentados.
- OAuth devuelve `503` en el dataset de desarrollo, como se espera mientras falten `Meta:AppId`, `Meta:AppSecret` y una cuenta Meta autorizada.

### Implementación pendiente o a profundizar

1. Crear pruebas automatizadas de frontend para el cliente HTTP, permisos, formularios y flujos críticos. No hay una suite configurada.
2. Ejecutar una revisión visual manual en escritorio, tablet y móvil contra la referencia Claude. La superficie de navegador no estuvo disponible en esta sesión.
3. Probar OAuth real cuando el backend tenga configuración Meta y probar los flujos mutables de invitación, acceso y MCP en un dataset aislado.
4. Repetir el recorrido visual completo de reportes con credenciales de un dataset demo aislado; esta sesión no dispone de contraseñas demo ni de una superficie de navegador operativa.
5. El contrato de análisis permite consultar a los cinco roles autorizados, por lo que el endpoint no produce un `403` válido con el dataset demo. La interfaz sí trata ese estado para futuras políticas de autorización.

## Archivos principales

- `lib/api.ts`: cliente HTTP y Problem Details.
- `lib/auth-session.ts`: sesión y permisos de roles.
- `components/private-shell.tsx`: shell, navegación y tema.
- `components/client-management.tsx`: clientes y cuentas manuales.
- `components/meta-integration-panel.tsx`: OAuth y asociación de Meta.
- `components/advertising-workspace.tsx`: jerarquía y sincronización.
- `components/client-dashboard.tsx`: dashboard y selección autorizada.
- `components/metrics-insights-panel.tsx`: resumen, selector, comparaciones y benchmarks.
- `components/analysis-engine-panel.tsx` y `lib/analysis.ts`: solicitud, contrato y evidencia estructurada de AnalysisEngine.
- `components/reports-panel.tsx`, `components/report-share-links.tsx`, `components/shared-report-view.tsx`, `components/snapshot-chart.tsx` y `lib/reports.ts`: reportes privados, enlaces y vista pública.
- `components/client-access-panel.tsx` y `components/invitation-acceptance.tsx`: accesos ClientViewer.
- `components/mcp-connections-panel.tsx`: conexiones MCP.
- `app/globals.css`: tema, diseño responsive, tarjetas y tablas.
