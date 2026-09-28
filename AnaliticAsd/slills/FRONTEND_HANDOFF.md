# Traspaso e instrucciones para el frontend de AnalitiAds

> Actualización Fase 12 — 15 de septiembre de 2026: enlaces compartibles implementados y desplegados en Neon. Owner/Admin pueden crear, listar y revocar enlaces de reportes. El acceso público recibe el token por body y nunca por ruta/query; la URL compartible usa fragmento `#token`. La migración `20260915181708_AddReportShareLinks` está aplicada y existen diez migraciones.

## Actualización Fase 12 — enlaces compartibles

### Administración autenticada

Roles: `Owner` y `Admin`.

```http
GET    /api/v1/reports/{reportId}/share-links
POST   /api/v1/reports/{reportId}/share-links
DELETE /api/v1/reports/{reportId}/share-links/{shareLinkId}
Authorization: Bearer <accessToken>
```

Crear recibe:

```json
{ "expirationDays": 7 }
```

`expirationDays` admite 1 a 30. Responde `201`:

```json
{
  "shareLink": {
    "id": "uuid",
    "reportId": "uuid",
    "status": "Active",
    "createdAtUtc": "2026-09-15T12:00:00Z",
    "expiresAtUtc": "2026-09-22T12:00:00Z",
    "revokedAtUtc": null
  },
  "accessToken": "token-base64url-de-43-caracteres",
  "sharePath": "/reportes-compartidos#token-base64url-de-43-caracteres"
}
```

El token se muestra una sola vez. El listado nunca lo devuelve. Estados: `Active`, `Expired`, `Revoked`. Revocar responde `204` y el siguiente acceso público responde `404`.

### Acceso público

No usa JWT:

```http
POST /api/v1/shared-reports/access
POST /api/v1/shared-reports/pdf
Content-Type: application/json
```

Body:

```json
{ "accessToken": "token-extraido-del-fragmento" }
```

`access` devuelve título, fecha de creación y el `analysis` inmutable. `pdf` devuelve `application/pdf` como adjunto. Ambos tienen rate limit conjunto de 60 solicitudes por minuto por IP y responden `429` con `Retry-After` cuando se supera.

### Página pública

La ruta frontend debe ser `/reportes-compartidos#token`. Al cargar:

1. Leer `window.location.hash` únicamente en el cliente.
2. Retirar `#` y validar que exista un token.
3. Limpiar inmediatamente la barra con `history.replaceState(null, "", "/reportes-compartidos")`.
4. Mantener el token solo en memoria durante esa vista.
5. Enviar el token por body a `/shared-reports/access`.
6. Para el PDF, enviarlo por body a `/shared-reports/pdf`, descargar como blob y revocar la URL temporal.

Nunca enviar el token en path, query string, analytics, logs, errores, localStorage o sessionStorage. No añadir navegación privada, administración, otros reportes ni llamadas a `/analyses` en la página pública.

Los gráficos web pueden ser interactivos —tooltips, ocultar/mostrar series, zoom visual y navegación dentro de los datos incluidos— pero deben operar únicamente sobre `analysis` del snapshot. No deben cambiar el periodo, pedir métricas actuales ni recalcular cifras. El PDF es estático.

Token inválido, vencido, revocado, cliente inactivo, agencia inactiva o reporte eliminado responden igual: `404`. La interfaz debe mostrar “Este enlace no está disponible” sin distinguir la causa.

> Actualización Fase 11 — 15 de septiembre de 2026: backend de reportes implementado y verificado. El frontend puede integrar creación, listado, detalle inmutable y descarga PDF con las rutas descritas abajo. La migración `20260915171245_AddImmutableReports` está aplicada en Neon.

## Actualización Fase 11 — ReportData, reporte web y PDF

El reporte es un snapshot inmutable del resultado completo de AnalysisEngine, incluidos insights y recomendaciones de Fase 10. Abrir el reporte web y descargar el PDF no vuelve a consultar métricas ni Meta.

### Crear reporte

Roles: `Owner`, `Admin`, `Analyst`.

```http
POST /api/v1/reports
Authorization: Bearer <accessToken>
Content-Type: application/json
```

```json
{
  "title": "Reporte septiembre 2026",
  "adAccountId": "uuid",
  "since": "2026-09-01",
  "until": "2026-09-30",
  "comparison": "PreviousPeriod",
  "comparisonSince": null,
  "comparisonUntil": null,
  "campaignIds": ["uuid"],
  "selectedMetrics": ["spend", "impressions", "ctr", "cpa", "roas"]
}
```

La selección y validación es igual a `POST /api/v1/analyses`. Responde `201 Created`, incluye `Location: /api/v1/reports/{reportId}` y devuelve el `ReportData` completo.

### Listar reportes de un cliente

Roles: todos. `ClientViewer` solo ve reportes de clientes activos que continúen asignados.

```http
GET /api/v1/clients/{clientId}/reports?page=1&pageSize=20
```

`page` empieza en 1. `pageSize` admite 1 a 100. Devuelve un array ordenado por creación descendente:

```json
[
  {
    "id": "uuid",
    "title": "Reporte septiembre 2026",
    "clientId": "uuid",
    "adAccountId": "uuid",
    "since": "2026-09-01",
    "until": "2026-09-30",
    "createdAtUtc": "2026-09-15T12:00:00Z",
    "schemaVersion": 1,
    "snapshotHash": "SHA256_HEX"
  }
]
```

### Consultar snapshot

```http
GET /api/v1/reports/{reportId}
```

Devuelve:

```json
{
  "reportId": "uuid",
  "schemaVersion": 1,
  "title": "Reporte septiembre 2026",
  "clientId": "uuid",
  "adAccountId": "uuid",
  "createdByUserId": "uuid",
  "createdAtUtc": "2026-09-15T12:00:00Z",
  "snapshotHash": "SHA256_HEX",
  "analysis": {
    "...": "Contrato completo de Fases 9 y 10"
  }
}
```

La vista web debe renderizar exclusivamente `analysis` dentro de este objeto. No debe volver a llamar `/analyses` para reconstruir un reporte guardado.

### Descargar PDF

```http
GET /api/v1/reports/{reportId}/pdf
```

Devuelve `application/pdf` como archivo adjunto. Para descargarlo desde el navegador autenticado, el cliente HTTP debe solicitar `blob`, conservar el encabezado Bearer, crear una URL temporal, iniciar la descarga y revocarla después.

### Eliminar reporte

Roles: `Owner`, `Admin`.

```http
DELETE /api/v1/reports/{reportId}
```

Responde `204`. `Analyst`, `Viewer` y `ClientViewer` reciben `403`.

### Reglas de integración

- No enviar `agencyId`, `clientId`, `createdByUserId`, `schemaVersion` ni hashes al crear.
- Después de crear, insertar el reporte devuelto en la lista o refrescar la primera página.
- `404` significa que el reporte/cliente no existe o dejó de estar autorizado; limpiar el detalle y caché relacionados.
- `ClientViewer` pierde acceso inmediatamente cuando se revoca su asignación, aunque el reporte siga almacenado.
- Mostrar valores `null` como “Sin dato”; no reconstruir ceros, métricas, reglas o recomendaciones.
- Tratar `snapshotHash` como identificador de integridad, no como contraseña ni token.
- El PDF y la vista web representan el mismo snapshot; no combinarlo con métricas actuales.

> Actualización Fase 10 — 15 de septiembre de 2026: `POST /api/v1/analyses` conserva su request y todos los campos anteriores, y ahora añade `insights` y `recommendations`. Los resultados son deterministas, auditables y calculados por el backend. El frontend no debe recalcular reglas, severidad, confianza ni evidencia.

## Actualización Fase 10 — reglas, insights y recomendaciones

No hay una ruta nueva. Se mantiene:

```http
POST /api/v1/analyses
Authorization: Bearer <accessToken>
Content-Type: application/json
```

La respuesta añade dos arrays en el nivel raíz:

```json
{
  "insights": [
    {
      "ruleId": "underperforming_campaign",
      "level": "Campaign",
      "entityIds": ["uuid"],
      "severity": "Warning",
      "confidence": "High",
      "sufficiency": "Sufficient",
      "message": "The campaign shows lower efficiency than its comparable group across multiple signals.",
      "evidence": [
        {
          "metric": "cpa",
          "value": 30,
          "referenceValue": 20,
          "percentageDifference": 50,
          "availability": "Available"
        }
      ]
    }
  ],
  "recommendations": [
    {
      "ruleId": "underperforming_campaign",
      "level": "Campaign",
      "entityIds": ["uuid"],
      "priority": "High",
      "message": "Review the campaign before increasing its budget.",
      "actions": ["Identify the ad sets and ads contributing most to the gap."]
    }
  ]
}
```

Reglas que pueden aparecer:

- `budget_concentration`: porcentaje de gasto de las campañas top respecto del gasto total. Es informativa y no declara que la concentración sea negativa.
- `standout_campaign`: exige al menos dos señales favorables relevantes para el objetivo.
- `underperforming_campaign`: exige al menos dos señales desfavorables relevantes para el objetivo.
- `possible_post_click_issue`: respuesta de tráfico competitiva y costo de resultado inferior al grupo comparable; no atribuye una causa.
- `weak_ad_response`: costo de entrega comparable y CTR inferior al benchmark.
- `more_expensive_delivery`: CPM y CPC aumentan con CTR relativamente estable frente al periodo comparado.

Las reglas solo se emiten con suficiencia `Sufficient`, métricas completas y, cuando corresponde, al menos dos campañas comparables. Un array vacío significa que no existe evidencia suficiente o que ninguna regla superó sus umbrales; no es un error. No se implementó scoring porque no fue autorizado de manera específica. Fatiga creativa, audiencias, placements y atributos creativos siguen sin conclusión hasta disponer de observaciones reales.

Integración requerida en frontend:

1. Ampliar el tipo de respuesta de análisis con `insights` y `recommendations`.
2. Renderizar `message`, `severity`, `confidence` y `sufficiency` tal como llegan.
3. Mostrar la evidencia como valor, referencia y diferencia porcentual; conservar `null` como “sin dato”.
4. Vincular entidades usando `level` y `entityIds`; no asumir que siempre existe una sola entidad.
5. Relacionar una recomendación con su hallazgo mediante `ruleId` y sus entidades.
6. Mostrar un estado vacío cuando ambos arrays estén vacíos.
7. No traducir una observación en causalidad ni calcular reglas o scores en el navegador.
8. Mantener el manejo actual de JWT, `400`, `401`, `403` y `404`; no cambió.

> Continuidad general del backend: `CODEX_HANDOFF.md`. Fases 0–9 están completas. Este archivo conserva el contrato detallado que debe consumir el frontend.

> Integración confirmada el 14 de septiembre de 2026: el frontend completó y validó AnalysisEngine sin incompatibilidades de contrato. La matriz y el tratamiento de errores están consolidados en `ANALYSISENGINE_BACKEND_HANDOFF.md`. El backend optimizó internamente la carga de datos; la solicitud y la respuesta no cambiaron.

## Actualización Fase 9 — AnalysisEngine

Fase 9 está disponible mediante `POST /api/v1/analyses`. Es un cálculo síncrono y no crea un recurso persistente.

```json
{
  "adAccountId": "uuid",
  "since": "2026-08-30",
  "until": "2026-09-03",
  "comparison": "PreviousPeriod",
  "comparisonSince": null,
  "comparisonUntil": null,
  "campaignIds": ["uuid"],
  "selectedMetrics": ["spend", "impressions", "ctr", "cpa", "roas"]
}
```

`comparison` puede ser `null`, `PreviousPeriod`, `PreviousMonth`, `PreviousYear` o `Custom`; este último exige ambas fechas comparativas. `campaignIds: null` analiza todas las campañas actuales autorizadas y `campaignIds: []` analiza solo la cuenta. Los UUID deben ser únicos y pertenecer a la cuenta autorizada.

Métricas admitidas: `spend`, `impressions`, `reach`, `linkClicks`, `leads`, `purchases`, `purchaseValue`, `frequency`, `cpm`, `ctr`, `cpc`, `cpl`, `cpa` y `roas`. Si `selectedMetrics` es `null` o vacío se incluyen todas.

La respuesta contiene `account`, `campaigns`, `adSets`, `ads` y `unavailableSections`. Cada entidad incluye `metrics`, `comparison` opcional, `benchmarks` solo para Campaign, `coverage` y `sufficiency`. La suficiencia usa `Sufficient`, `Partial` o `Insufficient`; sus motivos posibles incluyen `NoSnapshots`, `PartialDateCoverage`, `LegacyZeroNormalized`, `MixedCurrency` y `UnavailableSelectedMetrics`.

El frontend debe mostrar estos objetos como diagnóstico estructurado. Todavía no debe convertirlos en conclusiones causales, scoring o recomendaciones. `Audiences`, `Placements` y `Creatives` aparecen como no disponibles hasta contar con observaciones reales. La ruta usa JWT Bearer, Problem Details y los mismos filtros de agencia y `ClientViewer`.

## Contrato REST congelado para integración

Las siguientes rutas reflejan el código vigente y no deben incorporar el segmento `/access`:

```text
GET    /api/v1/meta/oauth/start
GET    /api/v1/clients/{clientId}/users
DELETE /api/v1/clients/{clientId}/users/{userId}
GET    /api/v1/clients/{clientId}/invitations
POST   /api/v1/clients/{clientId}/invitations
DELETE /api/v1/clients/{clientId}/invitations/{invitationId}
```

El inicio OAuth es `GET`, no recibe body y devuelve `authorizationUrl`. Todas las rutas administrativas anteriores, excepto el callback OAuth y la aceptación pública de invitaciones documentados más adelante, usan JWT Bearer. El frontend no envía `agencyId`: el backend resuelve y revalida usuario, agencia, membresía, rol y asignaciones `ClientViewer` en cada solicitud. Los errores conservan el formato Problem Details.

## Actualización Fase 8 (prevalece sobre referencias históricas a comparaciones)

Comparaciones temporales disponibles:

```text
GET /api/v1/ad-accounts/{id}/metrics/comparison
GET /api/v1/campaigns/{id}/metrics/comparison
GET /api/v1/ad-sets/{id}/metrics/comparison
GET /api/v1/ads/{id}/metrics/comparison
```

Parámetros comunes: `since`, `until` y `comparison`. Valores de `comparison`: `PreviousPeriod` (rango inmediatamente anterior de igual duración), `PreviousMonth` (mes calendario anterior al inicio), `PreviousYear` (mismas fechas un año antes) y `Custom`. Para `Custom` son obligatorios `comparisonSince` y `comparisonUntil`. Todo rango admite máximo 90 días, no puede terminar en el futuro y la comparación debe terminar antes del rango actual.

La respuesta contiene `comparisonType`, `current`, `baseline`, `observed` y `derived`. Cada métrica comparada contiene `current`, `baseline`, `absoluteChange`, `percentageChange` y `availability`. Estados: `Available`, `CurrentUnavailable`, `BaselineUnavailable`, `UndefinedBaseline` y `CurrencyMismatch`. Un baseline cero conserva el cambio absoluto, pero no inventa porcentaje. Reach y frequency de rango continúan sin agregarse y normalmente aparecen como no disponibles.

Benchmarks de campañas:

```text
GET /api/v1/ad-accounts/{id}/campaigns/benchmarks?since=YYYY-MM-DD&until=YYYY-MM-DD
```

Método: `MedianOfOtherCampaignsWithSameObjective`. Compara CPM, CTR, CPC, CPL, CPA y ROAS de cada campaña con la mediana de otras campañas del mismo objetivo. Exige al menos dos campañas comparables además de la evaluada. Cada métrica informa `campaignValue`, `benchmarkValue`, `absoluteDifference`, `percentageDifference`, `comparableCampaigns` y `availability`. Estados: `Available`, `MetricUnavailable`, `InsufficientComparableCampaigns`, `CurrencyMismatch` y `UndefinedBenchmark`.

Las rutas usan los mismos filtros de autorización que métricas. `ClientViewer` solo puede consultar cuentas y jerarquía de clientes activos asignados. Datos `LegacyZeroNormalized`, incompletos o de moneda incompatible no producen deltas ni benchmarks aparentemente válidos. MCP añade `get_metrics_comparison` y `get_campaign_benchmarks`. Todavía no existen análisis, scoring, insights, recomendaciones, reportes ni PDF.

## Actualización Fase 7B (prevalece sobre referencias históricas a MCP)

MCP de lectura está disponible y es opcional. La web sigue usando las rutas REST existentes y funciona sin conexión MCP.

- Endpoint MCP: `POST /mcp` mediante Streamable HTTP stateless.
- Metadata: `GET /.well-known/oauth-protected-resource/mcp`.
- Scope único: `analitiads:read`.
- Listar conexiones del usuario: `GET /api/v1/mcp/connections`.
- Crear conexión con consentimiento: `POST /api/v1/mcp/connections` con `{ "name": "Codex", "consentAccepted": true }`. Responde `201` con `connection` y `accessToken`; el token se muestra solo una vez y vence a los 30 días.
- Revocar: `DELETE /api/v1/mcp/connections/{id}`; responde `204` y la siguiente solicitud MCP queda rechazada.
- Estados de conexión: activa cuando `revokedAtUtc` es `null` y `expiresAtUtc` es futuro; revocada cuando `revokedAtUtc` tiene valor; expirada cuando vence la fecha.
- Errores: `400` sin consentimiento o nombre inválido, `401` sin sesión/JWT válido, `404` para conexión ajena o inexistente. `/mcp` devuelve `401` por credencial ausente, revocada, expirada, membresía inactiva o audiencia incorrecta; `403` por scope insuficiente.
- Nunca guardar el token MCP en telemetría, URL o almacenamiento público. No es el JWT del frontend y no contiene credenciales Meta.
- Si MCP está desconectado o revocado, clientes, dashboard, sincronización e histórico REST continúan funcionando normalmente.

Tools publicadas: `list_authorized_clients`, `list_authorized_ad_accounts`, `list_campaigns`, `get_campaign`, `list_ad_sets`, `get_ad_set`, `list_ads`, `get_ad`, `get_daily_metrics` y `get_metrics_summary`. No hay tools de escritura, usuarios, borrado, regeneración, comparaciones, análisis, recomendaciones, reportes o PDF.

Fecha de corte: 11 de septiembre de 2026, Fases 6, 6A y 7 verificadas. La sección 20 prevalece sobre las partes de métricas de las secciones 17 y 19 cuando haya diferencia.

## 1. Objetivo de este documento

Este documento es el contrato vigente entre el backend implementado y el frontend que se construirá. Debe entregarse junto con la referencia visual elegida por el usuario.

- La imagen de referencia determina dirección visual, composición, jerarquía, tipografía, color, espaciado y estilo de componentes.
- Este documento determina rutas, datos, permisos, estados, seguridad y comportamiento de integración.
- Si la imagen muestra datos o funciones que todavía no existen en la API, deben representarse como estados vacíos o prototipos claramente desacoplados. No se deben inventar endpoints ni métricas.

## 2. Estado real del producto

Implementación disponible de fases 0 a 7, verificada con pruebas automatizadas y migraciones aplicadas (no certifica una conexión Meta real ni despliegue):

- Base ASP.NET Core Web API sobre .NET 10.
- PostgreSQL mediante EF Core y migraciones.
- Gestión CRUD de clientes.
- Gestión CRUD de cuentas publicitarias.
- Autenticación JWT.
- Agencias, usuarios, membresías y roles.
- Aislamiento multi-tenant por agencia.
- Manejo de errores con Problem Details.
- OpenAPI en desarrollo.
- Meta OAuth gestionado por backend, token protegido, descubrimiento y asociación de cuentas.
- Sincronización y consulta de Campaign, AdSet y Ad mediante IDs de Meta.
- Insights normalizados y snapshots históricos diarios en niveles Account, Campaign, AdSet y Ad.
- Observados anulables con marca de calidad, resúmenes seguros de rango y selector de Campaign por actividad o gasto.
- Invitaciones y permisos `ClientViewer` por cliente; revocación comprobada en consultas posteriores.
- 63 pruebas automatizadas aprobadas.

Todavía no existen:

- Análisis, benchmarks, insights interpretativos o recomendaciones.
- Reportes, PDF o enlaces compartibles.
- Refresh tokens, cierre de sesión en servidor o lista de revocación individual de JWT. Sí se comprueba que la membresía/rol/usuario/agencia sigan vigentes.
- Recuperación o cambio de contraseña.
- Administración de miembros internos/cambio de rol y envío automático de correo. Las invitaciones a clientes externos sí existen (sección 19).
- MCP y generación con IA: son capacidades futuras; la conectabilidad MCP es obligatoria en el plan, pero aún no existe un endpoint.

## 3. Backend disponible

Durante desarrollo local:

```text
HTTP:  http://localhost:5019
HTTPS: https://localhost:7178
```

Base path de negocio:

```text
/api/v1
```

Estado del servicio:

```text
GET /api/system/status
```

OpenAPI solo en ambiente Development:

```text
GET /openapi/v1.json
```

En desarrollo, CORS autoriza exactamente `http://localhost:3001`. No se permiten orígenes comodín ni credenciales cross-origin. No usar ni detener el puerto 3000: pertenece a otro proyecto. El origen se puede sobrescribir mediante `Cors__AllowedOrigins__0`; no agregar variantes sin autorización.

## 4. Convenciones del contrato

- El JSON utiliza nombres `camelCase`.
- Los identificadores internos son UUID representados como strings.
- Los timestamps son ISO 8601 con zona UTC.
- El frontend puede formatear fechas, moneda y zona horaria para mostrar, pero no debe alterar sus valores de transporte.
- Los endpoints protegidos requieren `Authorization: Bearer <accessToken>`.
- Nunca se debe enviar `agencyId` al crear, listar o editar clientes o cuentas. El backend lo obtiene del JWT.
- Nunca almacenar ni mostrar contraseñas, claves de firma, secretos de Meta o tokens de Meta.
- El `metaAccountId` es un identificador de Meta, no un nombre. El backend normaliza valores como `act_123456789` a `123456789`.

## 5. Autenticación

### Registrar agencia y propietario

```http
POST /api/v1/auth/register
Content-Type: application/json
```

```json
{
  "agencyName": "Agencia Demo",
  "email": "owner@example.com",
  "password": "SecurePassword123"
}
```

La contraseña debe tener como mínimo 12 caracteres e incluir mayúscula, minúscula y número.

Respuesta `200 OK`:

```json
{
  "accessToken": "eyJ...",
  "expiresAtUtc": "2026-09-04T20:00:00Z",
  "userId": "00000000-0000-0000-0000-000000000000",
  "email": "owner@example.com",
  "agencyId": "00000000-0000-0000-0000-000000000000",
  "agencyName": "Agencia Demo",
  "role": "Owner"
}
```

El registro crea en una sola operación:

- La agencia.
- El usuario.
- La membresía con rol `Owner`.
- El token de acceso.

### Iniciar sesión

```http
POST /api/v1/auth/login
Content-Type: application/json
```

```json
{
  "email": "owner@example.com",
  "password": "SecurePassword123",
  "agencyId": null
}
```

`agencyId` es opcional. Hoy cada usuario creado por registro tiene una agencia. El campo existe para soportar usuarios con varias membresías en el futuro.

Respuesta: el mismo modelo `AuthResponse` del registro.

### Obtener la sesión actual

```http
GET /api/v1/auth/me
Authorization: Bearer <accessToken>
```

Respuesta `200 OK`:

```json
{
  "userId": "00000000-0000-0000-0000-000000000000",
  "email": "owner@example.com",
  "agencyId": "00000000-0000-0000-0000-000000000000",
  "agencyName": "Agencia Demo",
  "role": "Owner"
}
```

### Manejo recomendado de sesión

- Mantener `accessToken`, `expiresAtUtc` y los datos mínimos de sesión en una capa central de autenticación.
- Añadir el encabezado Bearer desde un único cliente HTTP o interceptor.
- Al arrancar la aplicación con una sesión existente, llamar a `/auth/me` antes de cargar datos privados.
- Ante `401`, limpiar la sesión local y enviar al usuario al login.
- Ocultar acciones no permitidas según el rol, pero considerar al backend como autoridad definitiva.
- El JWT dura actualmente 8 horas.
- No existe refresh token. No intentar renovar llamando a un endpoint inexistente.
- Para cerrar sesión, borrar sesión y cachés privadas locales. No existe endpoint de logout ni revocación individual del JWT; eliminar la membresía, cambiar su rol o inactivar usuario/agencia sí invalida ese JWT en la siguiente petición.

Para producción es preferible evolucionar a cookies seguras `HttpOnly` o a un flujo de access/refresh tokens antes de manejar sesiones persistentes de larga duración.

## 6. Roles y permisos actuales

| Operación | Owner | Admin | Analyst | Viewer (interno) | ClientViewer (externo) |
|---|---:|---:|---:|---:|---|
| Leer clientes/cuentas/jerarquía/métricas | Agencia | Agencia | Agencia | Agencia | Solo clientes activos asignados |
| Crear y editar clientes/cuentas | Sí | Sí | Sí | No | No |
| Eliminar clientes y cuentas | Sí | Sí | No | No | No |
| Invitar, listar y revocar accesos externos | Sí | Sí | No | No | No |
| Sincronizar estructura o métricas | Sí | Sí | Sí | No | No |
| Consultar conexión Meta de agencia | Sí | Sí | Sí | Sí | No |

Los nombres de rol se reciben exactamente como:

```text
Owner
Admin
Analyst
Viewer
ClientViewer
```

No existe API para modificar roles o invitar miembros internos. Las invitaciones de sección 19 crean exclusivamente `ClientViewer`; nunca promover a Owner/Admin desde la interfaz.

## 7. Clientes

Todos los endpoints requieren autenticación.

### Listar clientes

```http
GET /api/v1/clients
```

Respuesta `200 OK`:

```json
[
  {
    "id": "00000000-0000-0000-0000-000000000000",
    "name": "Cliente Demo",
    "isActive": true,
    "createdAtUtc": "2026-09-04T12:00:00Z",
    "updatedAtUtc": "2026-09-04T12:00:00Z"
  }
]
```

Solo devuelve clientes de la agencia incluida en el token.

### Obtener cliente

```http
GET /api/v1/clients/{clientId}
```

### Crear cliente

```http
POST /api/v1/clients
Content-Type: application/json
```

```json
{
  "name": "Cliente Demo"
}
```

Respuesta `201 Created` con el modelo del cliente.

### Editar cliente

```http
PUT /api/v1/clients/{clientId}
Content-Type: application/json
```

```json
{
  "name": "Cliente actualizado",
  "isActive": true
}
```

### Eliminar cliente

```http
DELETE /api/v1/clients/{clientId}
```

Respuesta `204 No Content`. Si tiene cuentas asociadas, devuelve `409 Conflict`; la UI debe pedir que se eliminen o desvinculen primero.

## 8. Cuentas publicitarias

Todos los endpoints requieren autenticación.

### Listar cuentas de un cliente

```http
GET /api/v1/clients/{clientId}/ad-accounts
```

### Obtener cuenta

```http
GET /api/v1/ad-accounts/{adAccountId}
```

### Crear cuenta manualmente

```http
POST /api/v1/clients/{clientId}/ad-accounts
Content-Type: application/json
```

```json
{
  "metaAccountId": "act_123456789",
  "name": "Cuenta publicitaria principal",
  "currency": "USD",
  "timeZone": "America/La_Paz"
}
```

Respuesta `201 Created`:

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "clientId": "00000000-0000-0000-0000-000000000000",
  "metaAccountId": "123456789",
  "name": "Cuenta publicitaria principal",
  "currency": "USD",
  "timeZone": "America/La_Paz",
  "connectionStatus": "Disconnected",
  "isActive": true,
  "createdAtUtc": "2026-09-04T12:00:00Z",
  "updatedAtUtc": "2026-09-04T12:00:00Z"
}
```

### Editar cuenta

```http
PUT /api/v1/ad-accounts/{adAccountId}
Content-Type: application/json
```

```json
{
  "name": "Cuenta actualizada",
  "currency": "BOB",
  "timeZone": "America/La_Paz",
  "isActive": true
}
```

### Eliminar cuenta

```http
DELETE /api/v1/ad-accounts/{adAccountId}
```

Respuesta `204 No Content`.

### Valores y validaciones

- `metaAccountId`: obligatorio, máximo 64 caracteres y único dentro de la agencia.
- `name`: obligatorio, máximo 200 caracteres.
- `currency`: exactamente tres letras; se normaliza a mayúsculas.
- `timeZone`: identificador IANA, por ejemplo `America/La_Paz`, máximo 100 caracteres.
- `connectionStatus`: comienza en `Disconnected` para altas manuales y queda en `Connected` cuando la cuenta se asocia mediante Meta OAuth.

## 9. Errores HTTP

El frontend debe manejar como mínimo:

| Código | Significado | Comportamiento sugerido |
|---:|---|---|
| 400 | Datos inválidos | Mostrar errores en el formulario |
| 401 | Sesión ausente, inválida o login incorrecto | Volver al login; no revelar si falló email o contraseña |
| 403 | Rol insuficiente | Mostrar acceso denegado y ocultar la acción |
| 404 | Recurso inexistente o perteneciente a otra agencia | Mostrar no encontrado; nunca inferir datos de otro tenant |
| 409 | Conflicto o duplicado | Mostrar el detalle entregado por la API |
| 500 | Error inesperado | Mensaje genérico y opción de reintentar |

Errores controlados usan una forma compatible con Problem Details:

```json
{
  "type": "about:blank",
  "title": "Request conflict",
  "status": 409,
  "detail": "The client cannot be deleted while it has associated ad accounts.",
  "instance": "/api/v1/clients/..."
}
```

Los errores automáticos de validación pueden incluir además un objeto `errors` por campo. La UI debe soportar ambos formatos.

## 10. Pantallas que ya pueden construirse

1. Login.
2. Registro de agencia y propietario.
3. Shell autenticado: navegación, usuario, agencia y cierre de sesión local.
4. Lista, creación, edición, activación/desactivación y eliminación de clientes.
5. Detalle de cliente con sus cuentas publicitarias.
6. Alta manual, edición, activación/desactivación y eliminación de cuentas.
7. Estados de carga, vacío, error, sin permisos y sesión expirada.
8. Flujo funcional para conectar Meta, consultar el estado, listar cuentas disponibles y asociarlas a un cliente.
9. Sincronización, listado y drill-down estructural de Campaign → AdSet → Ad.

No construir como funcional todavía:

- Dashboard de rendimiento.
- Gráficos o KPIs reales.
- Reportes y exportación PDF.
- Administración de miembros.

Esas vistas pueden formar parte del mapa visual, pero deben estar deshabilitadas o marcadas como próximas funciones.

## 11. Rutas frontend sugeridas

```text
/login
/registro
/app
/app/clientes
/app/clientes/nuevo
/app/clientes/{clientId}
/app/clientes/{clientId}/editar
/app/clientes/{clientId}/cuentas/nueva
/app/cuentas/{adAccountId}/editar
```

Las rutas son una recomendación de frontend, no endpoints del backend.

## 12. Arquitectura frontend sugerida

- Framework recomendado: Next.js con TypeScript.
- Separar componentes visuales, funcionalidades, cliente HTTP y modelos de API.
- Validar formularios también en frontend, sin reemplazar la validación del servidor.
- Centralizar sesión y permisos.
- Centralizar el tratamiento de Problem Details.
- Usar estados de servidor/cache para clientes y cuentas; invalidar listas después de mutaciones.
- Confirmar eliminaciones en una ventana clara e indicar consecuencias.
- No codificar URLs del backend directamente en componentes; usar configuración de entorno.
- No introducir secretos en variables públicas del navegador.

Tipos TypeScript mínimos:

```ts
type AgencyRole = "Owner" | "Admin" | "Analyst" | "Viewer" | "ClientViewer";

interface AuthResponse {
  accessToken: string;
  expiresAtUtc: string;
  userId: string;
  email: string;
  agencyId: string;
  agencyName: string;
  role: AgencyRole;
}

interface Client {
  id: string;
  name: string;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

interface AdAccount {
  id: string;
  clientId: string;
  metaAccountId: string;
  name: string;
  currency: string;
  timeZone: string;
  connectionStatus: string;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}
```

## 13. Cómo usar la referencia visual

Al recibir la imagen del usuario:

1. Identificar layout, densidad, navegación, escalas tipográficas, colores, bordes, sombras y espaciado.
2. Traducirla a tokens reutilizables; no copiar valores aislados en cada componente.
3. Adaptar el contenido visible a las funciones realmente disponibles descritas aquí.
4. Mantener accesibilidad: contraste suficiente, foco visible, navegación por teclado, labels y estados no dependientes solo del color.
5. Diseñar responsive desde escritorio hasta móvil.
6. No copiar marcas, logos o recursos protegidos que aparezcan en la referencia salvo autorización del usuario.
7. No rellenar dashboards con métricas ficticias que parezcan datos reales. Los placeholders deben estar identificados como tales.

## 14. Condiciones para iniciar la integración

Antes de conectar frontend y backend en navegador se debe acordar:

- Carpeta y proyecto autorizados para el frontend. Actualmente solo existe el proyecto backend.
- Puerto/origen local del frontend.
- Mantener el frontend local en `http://localhost:3001`; no tocar el proyecto del puerto 3000.
- Estrategia final de almacenamiento del JWT.
- Variables de entorno para la URL base de la API.
- Si registro público seguirá habilitado en producción.

## 15. Meta OAuth: contrato definitivo de la fase 4

### Configuración y seguridad

El frontend solo necesita:

```env
NEXT_PUBLIC_API_URL=http://localhost:5019
```

Nunca necesita ni debe contener `Meta:AppId`, `Meta:AppSecret`, tokens de Meta o claves de Data Protection.

El backend requiere, fuera del repositorio:

```text
Meta__AppId
Meta__AppSecret
```

En desarrollo pueden configurarse con User Secrets:

```powershell
dotnet user-secrets set "Meta:AppId" "APP_ID" --project AnaliticAsd/AnaliticAsd.csproj
dotnet user-secrets set "Meta:AppSecret" "APP_SECRET" --project AnaliticAsd/AnaliticAsd.csproj
```

Configuración no secreta vigente:

```text
Graph API: v25.0, configurable mediante Meta:GraphVersion
Callback backend: http://localhost:5019/api/v1/meta/oauth/callback
Retorno frontend: http://localhost:3001/app/configuracion/integraciones/meta
Scope solicitado: ads_read
```

La URL de callback debe registrarse exactamente en la configuración de Facebook Login de la app Meta. Para cuentas de terceros, la app deberá obtener Advanced Access para `ads_read` y completar los requisitos de revisión de Meta. Los tokens se almacenan protegidos en backend mediante ASP.NET Core Data Protection. En producción, las claves de Data Protection deben persistirse fuera de una instancia efímera y compartirse entre réplicas.

### Consultar conexión

```http
GET /api/v1/meta/connection
Authorization: Bearer <accessToken>
```

Roles: `Owner`, `Admin`, `Analyst`, `Viewer`.

Respuesta `200 OK`:

```json
{
  "isConnected": true,
  "expiresAtUtc": "2026-11-03T12:00:00Z"
}
```

`expiresAtUtc` puede ser `null` si Meta no informa expiración. `isConnected` es falso si no existe conexión o si el token ya venció.

### Iniciar conexión Meta

```http
GET /api/v1/meta/oauth/start
Authorization: Bearer <accessToken>
```

Contrato congelado: este inicio OAuth usa `GET`, no recibe body y devuelve la URL de autorización. El frontend debe navegar después a `authorizationUrl`.

Para integración local hay usuarios demo para `Owner`, `Admin`, `Analyst`, `Viewer` y `ClientViewer`, más un `Owner` de otra agencia. Sus correos y el rango de datos se documentan en `CODEX_HANDOFF.md`; la contraseña se entrega por el operador del backend y no se versiona.

Roles: `Owner`, `Admin`.

Respuesta `200 OK`:

```json
{
  "authorizationUrl": "https://www.facebook.com/v25.0/dialog/oauth?..."
}
```

Flujo exacto del frontend:

1. Solicitar este endpoint con el JWT de AnalitiAds.
2. Recibir `authorizationUrl`.
3. Navegar la ventana completa a esa URL; no abrir Meta dentro de un iframe.
4. Meta regresa exclusivamente al callback del backend.
5. El backend valida un `state` cifrado y limitado a 10 minutos, intercambia el código y protege el token.
6. El backend redirige nuevamente al frontend.

Errores posibles:

- `401`: JWT ausente o inválido.
- `403`: rol diferente de Owner/Admin.
- `503`: faltan credenciales/configuración Meta en backend.

### Resultado del callback

El frontend recibe una navegación a:

```text
http://localhost:3001/app/configuracion/integraciones/meta?meta=success
http://localhost:3001/app/configuracion/integraciones/meta?meta=denied
http://localhost:3001/app/configuracion/integraciones/meta?meta=error
```

- `success`: mostrar confirmación y volver a consultar `/meta/connection` y `/meta/ad-accounts`.
- `denied`: el usuario canceló o negó autorización; mostrar un mensaje neutral y permitir reintentar.
- `error`: falló el intercambio con Meta; mostrar error genérico y permitir reintentar.

El frontend nunca recibe `code`, `state`, access token ni App Secret. Un callback sin `code/state` devuelve `400`. Un `state` manipulado o vencido devuelve `400` mediante Problem Details.

### Listar cuentas disponibles en Meta

```http
GET /api/v1/meta/ad-accounts
Authorization: Bearer <accessToken>
```

Roles: `Owner`, `Admin`, `Analyst`.

Respuesta `200 OK`:

```json
[
  {
    "metaAccountId": "123456789",
    "name": "Cuenta principal",
    "currency": "USD",
    "timeZone": "America/La_Paz",
    "accountStatus": 1,
    "isLinked": false,
    "clientId": null
  }
]
```

El backend pagina automáticamente `/me/adaccounts` y solicita únicamente `account_id`, `name`, `currency`, `timezone_name` y `account_status`. El frontend no debe llamar directamente a Graph API.

Errores posibles:

- `401` o `403`: autenticación/permisos de AnalitiAds.
- `409`: agencia sin conexión o conexión expirada.
- `502`: Meta rechazó o no pudo completar la consulta.

### Asociar una cuenta Meta con un cliente

```http
POST /api/v1/clients/{clientId}/meta-ad-accounts
Authorization: Bearer <accessToken>
Content-Type: application/json
```

```json
{
  "metaAccountId": "123456789"
}
```

Roles: `Owner`, `Admin`, `Analyst`.

Respuesta `200 OK`:

```json
{
  "metaAccountId": "123456789",
  "name": "Cuenta principal",
  "currency": "USD",
  "timeZone": "America/La_Paz",
  "accountStatus": 1,
  "isLinked": true,
  "clientId": "00000000-0000-0000-0000-000000000000"
}
```

El backend vuelve a verificar que la cuenta pertenezca al usuario Meta conectado y que el cliente pertenezca a la agencia del JWT.

Errores posibles:

- `404`: cliente fuera del tenant, inexistente o cuenta no disponible para el usuario Meta.
- `409`: cuenta ya asociada, conexión ausente o conexión vencida.
- `502`: error de Meta.

Después de asociar, refrescar tanto `/meta/ad-accounts` como `/clients/{clientId}/ad-accounts`.

### Estados de conexión de cuentas

El campo `connectionStatus` del CRUD de cuentas utiliza exactamente:

```text
Disconnected
Connected
Error
```

- `Disconnected`: cuenta creada manualmente, sin asociación OAuth verificada.
- `Connected`: cuenta descubierta y asociada mediante Meta OAuth.
- `Error`: reservado para una falla de conexión detectada por sincronizaciones futuras.

No inferir el estado operativo de Meta únicamente desde `accountStatus`; es un código numérico entregado por Meta y debe mostrarse de forma neutral hasta definir su catálogo de UX.

### CORS exacto

En desarrollo se autoriza:

```text
Origin: http://localhost:3001
Methods: cualquier método HTTP
Headers: cualquier encabezado, incluido Authorization y Content-Type
Credentials: no habilitadas
```

No usar `http://127.0.0.1:3001`, otro puerto o HTTPS sin agregar exactamente ese origen a `Cors:AllowedOrigins`. En producción debe configurarse mediante variables, por ejemplo `Cors__AllowedOrigins__0=https://app.ejemplo.com`; nunca usar `AllowAnyOrigin`. Los preflight permiten `Authorization`/`Content-Type` y los métodos solicitados; no `Access-Control-Allow-Credentials`. Usar Bearer, no `credentials: include` como sustituto.

## 16. Campaign, AdSet, Ad y sincronización: contrato de fase 5

Todos los endpoints siguientes requieren JWT. El tenant se resuelve desde `agency_id`; ningún request acepta `agencyId`.

### Iniciar una sincronización

```http
POST /api/v1/ad-accounts/{adAccountId}/sync
Authorization: Bearer <accessToken>
```

No lleva body. Roles permitidos: `Owner`, `Admin`, `Analyst`. `Viewer` recibe `403`.

La operación es sincrónica: la petición permanece cargando mientras el backend pagina campañas, conjuntos y anuncios, realiza upsert y confirma la transacción lógica. No enviar varias solicitudes simultáneas.

Respuesta `200 OK`:

```json
{
  "adAccountId": "00000000-0000-0000-0000-000000000000",
  "status": "Succeeded",
  "startedAtUtc": "2026-09-04T18:00:00Z",
  "completedAtUtc": "2026-09-04T18:00:08Z",
  "campaignsSynced": 12,
  "adSetsSynced": 30,
  "adsSynced": 75,
  "errorCode": null
}
```

Errores posibles:

- `401`: sesión inválida.
- `403`: rol sin permiso.
- `404`: cuenta inexistente o perteneciente a otra agencia.
- `409`: cuenta no conectada mediante OAuth, conexión vencida o sincronización ya en curso.
- `502`: Meta falló o devolvió una jerarquía inconsistente.
- `500`: error inesperado.

### Consultar estado de sincronización

```http
GET /api/v1/ad-accounts/{adAccountId}/sync
Authorization: Bearer <accessToken>
```

Roles permitidos: todos.

Respuesta: el mismo `SyncResponse`. Los valores exactos de `status` son:

```text
NeverSynced
Running
Succeeded
Failed
```

- `NeverSynced`: nunca se ejecutó sincronización.
- `Running`: hay una sincronización en curso.
- `Succeeded`: última sincronización completada.
- `Failed`: última sincronización fallida; `errorCode` contiene un código seguro, nunca datos sensibles.

Durante `Running`, mostrar carga y deshabilitar el botón. Si la llamada POST pierde conexión, consultar periódicamente este GET hasta dejar `Running`. Evitar polling más frecuente que cada 2–3 segundos.

### Listar campañas de una cuenta

```http
GET /api/v1/ad-accounts/{adAccountId}/campaigns?includeMissing=false
Authorization: Bearer <accessToken>
```

Roles permitidos: todos. `includeMissing` es opcional y por defecto `false`.

Respuesta `200 OK`:

```json
[
  {
    "id": "00000000-0000-0000-0000-000000000000",
    "adAccountId": "00000000-0000-0000-0000-000000000000",
    "metaCampaignId": "120000000000001",
    "name": "Campaña tráfico",
    "objective": "OUTCOME_TRAFFIC",
    "configuredStatus": "ACTIVE",
    "effectiveStatus": "ACTIVE",
    "startsAtUtc": "2026-09-01T04:00:00Z",
    "stopsAtUtc": null,
    "metaCreatedAtUtc": "2026-08-20T15:30:00Z",
    "metaUpdatedAtUtc": "2026-09-04T17:50:00Z",
    "lastSyncedAtUtc": "2026-09-04T18:00:08Z",
    "isPresentOnMeta": true
  }
]
```

Obtener una campaña:

```http
GET /api/v1/campaigns/{campaignId}
```

### Listar Ad Sets de una campaña

```http
GET /api/v1/campaigns/{campaignId}/ad-sets?includeMissing=false
Authorization: Bearer <accessToken>
```

Roles permitidos: todos.

Respuesta `200 OK`:

```json
[
  {
    "id": "00000000-0000-0000-0000-000000000000",
    "campaignId": "00000000-0000-0000-0000-000000000000",
    "metaAdSetId": "120000000000002",
    "name": "Prospecting Bolivia",
    "optimizationGoal": "LINK_CLICKS",
    "billingEvent": "IMPRESSIONS",
    "configuredStatus": "ACTIVE",
    "effectiveStatus": "ACTIVE",
    "startsAtUtc": "2026-09-01T04:00:00Z",
    "endsAtUtc": null,
    "metaCreatedAtUtc": "2026-08-20T15:35:00Z",
    "metaUpdatedAtUtc": "2026-09-04T17:51:00Z",
    "lastSyncedAtUtc": "2026-09-04T18:00:08Z",
    "isPresentOnMeta": true
  }
]
```

Obtener un Ad Set:

```http
GET /api/v1/ad-sets/{adSetId}
```

### Listar Ads de un Ad Set

```http
GET /api/v1/ad-sets/{adSetId}/ads?includeMissing=false
Authorization: Bearer <accessToken>
```

Roles permitidos: todos.

Respuesta `200 OK`:

```json
[
  {
    "id": "00000000-0000-0000-0000-000000000000",
    "adSetId": "00000000-0000-0000-0000-000000000000",
    "metaAdId": "120000000000003",
    "name": "Video producto A",
    "configuredStatus": "ACTIVE",
    "effectiveStatus": "ACTIVE",
    "metaCreatedAtUtc": "2026-08-20T15:40:00Z",
    "metaUpdatedAtUtc": "2026-09-04T17:52:00Z",
    "lastSyncedAtUtc": "2026-09-04T18:00:08Z",
    "isPresentOnMeta": true
  }
]
```

Obtener un Ad:

```http
GET /api/v1/ads/{adId}
```

### Semántica de IDs y registros ausentes

- `id`, `adAccountId`, `campaignId` y `adSetId` son UUID internos usados en rutas del backend.
- `metaCampaignId`, `metaAdSetId` y `metaAdId` son IDs externos de Meta y no deben usarse como rutas internas.
- Las relaciones se resuelven durante sincronización usando IDs de Meta; nunca mediante nombres.
- Si un objeto deja de aparecer en Meta, no se elimina: queda con `isPresentOnMeta=false` para preservar referencias futuras.
- Las listas normales excluyen ausentes. Usar `includeMissing=true` solo en vistas administrativas o diagnósticas.
- `configuredStatus` representa `status` configurado en Meta; `effectiveStatus` incorpora el estado efectivo reportado por Meta. El frontend debe mostrarlos como valores informativos y no inventar transiciones.

### Comportamiento del frontend después de sincronizar

Cuando el POST termine con `Succeeded`, invalidar y refrescar:

1. `GET /api/v1/ad-accounts/{adAccountId}/sync`.
2. `GET /api/v1/ad-accounts/{adAccountId}/campaigns`.
3. Las listas de Ad Sets abiertas de campañas pertenecientes a esa cuenta.
4. Las listas de Ads abiertas de esos Ad Sets.

No es necesario refrescar clientes, sesión ni cuentas disponibles de OAuth.

Si termina en error HTTP, consultar una vez el estado de sincronización para mostrar `Failed` y conservar los datos de la última sincronización exitosa.

### TypeScript mínimo de fase 5

```ts
type SyncStatus = "NeverSynced" | "Running" | "Succeeded" | "Failed";

interface SyncResponse {
  adAccountId: string;
  status: SyncStatus;
  startedAtUtc: string | null;
  completedAtUtc: string | null;
  campaignsSynced: number;
  adSetsSynced: number;
  adsSynced: number;
  errorCode: string | null;
}

interface Campaign {
  id: string;
  adAccountId: string;
  metaCampaignId: string;
  name: string;
  objective: string;
  configuredStatus: string;
  effectiveStatus: string;
  startsAtUtc: string | null;
  stopsAtUtc: string | null;
  metaCreatedAtUtc: string | null;
  metaUpdatedAtUtc: string | null;
  lastSyncedAtUtc: string;
  isPresentOnMeta: boolean;
}
```

Crear tipos equivalentes para AdSet y Ad usando exactamente los JSON anteriores.

### Funciones todavía prohibidas

La fase 5 no publica métricas ni acciones de escritura sobre Meta. El frontend no debe implementar todavía:

- Gasto, impresiones, reach, clicks, resultados, CTR, CPC, CPM, CPA, ROAS u otras métricas.
- Rangos de fecha o históricos de rendimiento.
- Creación, edición, activación o pausa de Campaign, AdSet o Ad.
- Sincronización automática programada.
- Selector analítico de campañas basado en actividad o gasto.
- Gráficos, benchmarks, análisis, insights o recomendaciones.
- Reportes, PDF o links compartibles.

## 17. Métricas, normalización e histórico diario: base de fases 6 y 7

Todos estos endpoints requieren JWT y resuelven la agencia desde `agency_id`. Nunca aceptan `agencyId`.

### Sincronizar métricas desde Meta

```http
POST /api/v1/ad-accounts/{adAccountId}/metrics/sync
Authorization: Bearer <accessToken>
Content-Type: application/json
```

Roles: `Owner`, `Admin`, `Analyst`. `Viewer` recibe `403`.

```json
{ "since": "2026-08-01", "until": "2026-08-31" }
```

Respuesta `200 OK`:

```json
{
  "adAccountId": "00000000-0000-0000-0000-000000000000",
  "since": "2026-08-01",
  "until": "2026-08-31",
  "accountSnapshots": 31,
  "campaignSnapshots": 120,
  "adSetSnapshots": 250,
  "adSnapshots": 500,
  "completedAtUtc": "2026-09-04T19:30:00Z"
}
```

La llamada es sincrónica. Consulta Insights en `level=account|campaign|adset|ad`, con `time_range` explícito, `time_increment=1` y paginación. No usa breakdowns ni fuerza ventanas de atribución: conserva la atribución predeterminada entregada por Meta. Primero debe existir la jerarquía de fase 5; un ID de Meta desconocido devuelve `502` y exige sincronizar la jerarquía.

### Consultar snapshots

```http
GET /api/v1/ad-accounts/{adAccountId}/metrics?since=2026-08-01&until=2026-08-31
GET /api/v1/campaigns/{campaignId}/metrics?since=2026-08-01&until=2026-08-31
GET /api/v1/ad-sets/{adSetId}/metrics?since=2026-08-01&until=2026-08-31
GET /api/v1/ads/{adId}/metrics?since=2026-08-01&until=2026-08-31
Authorization: Bearer <accessToken>
```

Roles: todos. Cada ruta devuelve exclusivamente su nivel, ordenado por fecha ascendente. Una fecha sin actividad puede no producir fila; no inventar ceros.

Respuesta `200 OK`:

```json
[
  {
    "id": "00000000-0000-0000-0000-000000000000",
    "adAccountId": "00000000-0000-0000-0000-000000000000",
    "campaignId": null,
    "adSetId": null,
    "adId": null,
    "level": "Account",
    "date": "2026-08-01",
    "currency": "USD",
    "observedDataQuality": "FieldPresencePreserved",
    "observed": {
      "spend": 100.25,
      "impressions": 10000,
      "reach": 4000,
      "linkClicks": 200,
      "leads": 10,
      "purchases": 4,
      "purchaseValue": 500
    },
    "derived": {
      "frequency": 2.5,
      "cpm": 10.025,
      "ctr": 2,
      "cpc": 0.50125,
      "cpl": 10.025,
      "cpa": 25.0625,
      "roas": 4.9875311721
    },
    "observedAtUtc": "2026-09-04T19:30:00Z"
  }
]
```

En `Campaign`, `AdSet` y `Ad`, el UUID del nivel deja de ser `null`. Son IDs internos enlazados con IDs de Meta, nunca por nombre.

### Rangos, validaciones y errores

- `since` y `until`: obligatorios, inclusivos y en `YYYY-MM-DD`.
- `since` debe ser menor o igual que `until`.
- Máximo: 90 días inclusivos.
- `until` no puede superar la fecha UTC del backend.
- `400`: formato/rango inválido, futuro, más de 90 días o valores Meta negativos/no numéricos.
- `401`: JWT ausente o inválido.
- `403`: rol insuficiente para sincronizar.
- `404`: recurso inexistente o de otra agencia.
- `409`: cuenta no conectada o conexión Meta ausente/vencida.
- `502`: rechazo de Meta, respuesta inválida o ID estructural no sincronizado.
- `500`: error inesperado.

Los errores controlados usan Problem Details.

### Definición de métricas

Observadas: `spend`, `impressions`, `reach`, `inline_link_clicks` normalizado como `linkClicks`, leads, purchases y `purchaseValue` desde `action_values`. Para leads y compras se prioriza un alias compatible (`lead`/pixel/onsite y `omni_purchase`/purchase/pixel), sin sumarlos, porque Meta puede incluir la misma conversión en categorías superpuestas. Dinero, acciones y fracciones usan `decimal`; conteos enteros usan `long`. La moneda queda congelada en cada snapshot. Desde Fase 7 todos los observados pueden ser `null`: significa que Meta no entregó ese campo, no cero.

Derivadas diariamente:

- `frequency = impressions / reach`.
- `cpm = spend / impressions * 1000`.
- `ctr = linkClicks / impressions * 100` (porcentaje).
- `cpc = spend / linkClicks`.
- `cpl = spend / leads`.
- `cpa = spend / purchases`.
- `roas = purchaseValue / spend`.

Si el denominador normalizado es cero, el resultado es `null`. Mostrar `null` como `—` o “Sin datos”, no como cero. `reach` es diario y no debe sumarse entre días como personas únicas. Un alcance único de rango requerirá una consulta agregada futura.

`observedDataQuality` vale `FieldPresencePreserved` en snapshots creados o actualizados por Fase 7: un cero explícito de Meta sigue siendo `0`, y una ausencia es `null`. Vale `LegacyZeroNormalized` en filas previas a esta corrección: sus ceros podrían haber sido ausencias; el frontend debe mostrar el aviso de re-sincronización y no tratarlos como KPI verificado. La sección 20 define el contrato de rango que consume esta marca.

```ts
type InsightLevel = "Account" | "Campaign" | "AdSet" | "Ad";
interface InsightSnapshot {
  id: string; adAccountId: string; campaignId: string | null;
  adSetId: string | null; adId: string | null; level: InsightLevel;
  date: string; currency: string; observedDataQuality: "FieldPresencePreserved" | "LegacyZeroNormalized";
  observed: { spend: number | null; impressions: number | null; reach: number | null; linkClicks: number | null; leads: number | null; purchases: number | null; purchaseValue: number | null };
  derived: { frequency: number | null; cpm: number | null; ctr: number | null; cpc: number | null; cpl: number | null; cpa: number | null; roas: number | null };
  observedAtUtc: string;
}
```

Tras sincronizar, invalidar las consultas de métricas abiertas de esa cuenta y rango. No refrescar sesión, clientes u OAuth. Si aparece `502` por IDs desconocidos, ejecutar primero la sincronización estructural.

### Funciones que siguen prohibidas

Fase 7 habilita solamente los resúmenes seguros y selector definidos en la sección 20. Siguen sin existir comparaciones, benchmarks, breakdowns, análisis, scoring, insights interpretativos, recomendaciones, reportes, PDF, links compartibles, IA, MCP, sincronización programada ni escritura en Meta.

## 18. Criterios de aceptación del primer frontend

- Registro y login consumen la API real.
- Una sesión válida accede al área privada.
- Una sesión ausente o vencida vuelve al login.
- Clientes y cuentas se consultan y modifican mediante la API real.
- El flujo Meta usa `/meta/oauth/start`, deja el callback al backend y procesa únicamente `meta=success|denied|error` al regresar.
- Las cuentas disponibles se leen del backend y se asocian a clientes sin exponer tokens Meta.
- Una cuenta conectada puede sincronizarse y navegarse mediante Campaign → AdSet → Ad sin métricas ficticias.
- La interfaz respeta los permisos del rol.
- Ninguna petición de clientes o cuentas envía `agencyId`.
- Se muestran correctamente cargas, listas vacías, validaciones, 401, 403, 404, 409 y 500.
- La referencia visual se implementa de forma consistente y responsive.
- No hay secretos ni datos reales sensibles en el repositorio o navegador.
- Las funciones futuras aparecen deshabilitadas o identificadas claramente, nunca simuladas como terminadas.

## 19. Acceso externo por cliente (Fase 6A, contrato vigente)

### Alcance y sesiones

`Viewer` sigue siendo un usuario INTERNO que lee toda su agencia. No asignarlo a clientes externos. `ClientViewer` solo lee clientes activos que tengan una asignación persistida para su usuario y agencia. La asignación incluye todas las cuentas de ese cliente, actuales y futuras, su jerarquía y snapshots; no hay permisos por cuenta individual.

Los permisos se resuelven en backend en cada petición. Pasar otro ID por URL, query o body no concede acceso. Un recurso inexistente o fuera del alcance responde 404, sin confirmar su existencia. Sin asignaciones, `GET /api/v1/clients` devuelve `[]`; `GET /api/v1/auth/me` puede seguir siendo 200. Las invitaciones y la API de administración nunca aceptan `agencyId`, `role` ni `userId` libre en el body; los cuerpos con campos desconocidos se rechazan con 400.

Todas las respuestas `/api/v1` llevan `Cache-Control: no-store`. Después de revocar, las nuevas peticiones se bloquean aunque el JWT siga sin expirar. Esto no borra los datos ya descargados por el navegador; el frontend debe vaciar las cachés privadas y la selección actual al recibir 401/403/404, cambiar de sesión o cerrar sesión. No usar caché compartida/estática de Next.js para datos autenticados.

### Endpoints administrativos (Bearer; solo Owner y Admin)

| Método | Ruta exacta | Body | Éxito |
|---|---|---|---|
| POST | `/api/v1/clients/{clientId}/invitations` | `{ "email": "cliente@example.com" }` | 201, invitación y código de un solo uso |
| GET | `/api/v1/clients/{clientId}/invitations` | Sin body | 200, array de invitaciones sin código ni hash |
| DELETE | `/api/v1/clients/{clientId}/invitations/{invitationId}` | Sin body | 204, sin body |
| GET | `/api/v1/clients/{clientId}/users` | Sin body | 200, array de accesos externos |
| DELETE | `/api/v1/clients/{clientId}/users/{userId}` | Sin body | 204, sin body |

Los UUID de ruta son internos de AnalitiAds. El tenant proviene del JWT, no del UUID ni del navegador. Los listados no están paginados en esta versión. No existe GET individual de invitación ni endpoint para añadir usuarios/roles arbitrarios.

Respuesta de crear invitación, `201` (valores de ejemplo, no credenciales reales):

```json
{
  "invitation": {
    "id": "11111111-1111-4111-8111-111111111111",
    "clientId": "22222222-2222-4222-8222-222222222222",
    "email": "cliente@example.com",
    "status": "Pending",
    "createdAtUtc": "2026-09-11T14:00:00+00:00",
    "expiresAtUtc": "2026-09-13T14:00:00+00:00"
  },
  "invitationToken": "<codigo-aleatorio-base64url-de-43-caracteres>"
}
```

El código se devuelve UNA sola vez, al creador autorizado. Solo su hash se persiste. Mostrarlo temporalmente con una acción explícita de copiar y advertir que se comparte por un canal privado con el destinatario. No ponerlo en query strings, rutas, logs, errores, analytics, localStorage ni repositorio. Si se pierde, revocar la invitación y crear otra. La interfaz debe decir “Invitación creada”, no “Correo enviado”: NO hay envío de email.

`GET .../invitations` devuelve objetos iguales a `invitation` dentro de un array, ordenados por creación descendente. Estados exactos:

- `Pending`: no consumida ni revocada, dentro de las 48 horas desde creación UTC.
- `Accepted`: ya consumida. Borrar la invitación no quita el acceso: DELETE de una invitación aceptada devuelve 409; usar DELETE de `users/{userId}`.
- `Revoked`: ya no sirve. Repetir su DELETE devuelve 204.
- `Expired`: llegaron las 48 horas, límite exclusivo para aceptar. Crear otra para reintentar; no existe extensión de vigencia.

`GET .../users` muestra solo asignaciones externas, no propietarios ni miembros internos:

```json
[
  {
    "userId": "33333333-3333-4333-8333-333333333333",
    "email": "cliente@example.com",
    "role": "ClientViewer",
    "grantedAtUtc": "2026-09-11T14:10:00+00:00"
  }
]
```

DELETE de acceso elimina solo la asignación de ese usuario a ese cliente y revoca sus invitaciones aún no consumidas para el mismo cliente. Conserva cuenta de usuario, contraseña, membresía y otras asignaciones. Un segundo DELETE devuelve 404. Nunca borrar al cliente ni a sus campañas para quitarle acceso a alguien.

### Aceptar invitación (público, sin Bearer requerido)

```http
POST /api/v1/auth/client-invitations/accept
Content-Type: application/json
```

```json
{
  "invitationToken": "<codigo-recibido-de-43-caracteres>",
  "email": "cliente@example.com",
  "password": "<contrasena-del-usuario>"
}
```

El email debe coincidir con el de la invitación; comparación normalizada sin diferencia de mayúsculas. Para un usuario nuevo, la contraseña debe tener entre 12 y 128 caracteres, mayúscula, minúscula y número. Si la cuenta ya existe, debe proporcionar su contraseña ACTUAL; este endpoint nunca la restablece. No hay recuperación de contraseña implementada.

La agencia/cliente se obtienen de la invitación persistida. Aceptar crea o reutiliza usuario, añade una membresía `ClientViewer` si no existe y concede el acceso a ese cliente. No se admite usar este flujo para una membresía interna de la misma agencia (409). La invitación se consume en la misma transacción; un reintento posterior de una invitación aceptada devuelve 400. Si se perdió la respuesta de éxito, usar login; no intentar reaceptar continuamente.

Respuesta `200`, mismo contrato de login:

```json
{
  "accessToken": "<jwt-de-usuario>",
  "expiresAtUtc": "2026-09-11T22:10:00+00:00",
  "userId": "33333333-3333-4333-8333-333333333333",
  "email": "cliente@example.com",
  "agencyId": "44444444-4444-4444-8444-444444444444",
  "agencyName": "Agencia de ejemplo",
  "role": "ClientViewer"
}
```

No hay redirección HTTP ni callback externo: frontend guarda sesión según el mecanismo actual, limpia código/contraseña del formulario y navega al área privada de cliente. Login posterior conserva `POST /api/v1/auth/login`. El `agencyId` opcional de login únicamente selecciona una membresía ya existente y comprobada (importante si un usuario pertenece a varias agencias); no da permiso para elegir tenants arbitrarios. No enviar `agencyId` en peticiones de negocio ni en aceptación de invitación.

### Errores y carga

| Código | Cuándo | Comportamiento frontend |
|---|---|---|
| 400 | Email/body inválido, campo extra, contraseña nueva débil, código inválido/expirado/revocado/usado, cliente/agencia inactivos al aceptar | Mostrar Problem Details; corregir formulario o solicitar nueva invitación, sin bucle de reintento |
| 401 | Bearer inválido/vencido, membresía/rol ya no vigentes; contraseña incorrecta de usuario existente al aceptar | En zona privada volver al login; en aceptación conservar formulario sin contraseña y mostrar error |
| 403 | Analyst/Viewer/ClientViewer intenta administrar accesos, o ClientViewer intenta Meta/escritura/sync | Ocultar acción y mostrar acceso no permitido; no cambiar rol en cliente |
| 404 | Cliente, invitación o acceso inexistente/fuera de agencia; lectura de jerarquía sin asignación | Limpiar selección/caché afectada y refrescar lista autorizada; no revelar datos anteriores |
| 409 | Cliente inactivo al invitar; usuario interno/inactivo; acceso ya concedido; borrar invitación aceptada; conflicto concurrente | Refrescar usuarios/invitaciones antes de decidir nueva acción |
| 429 | Aceptación supera 10 peticiones por minuto por IP y proceso | Esperar `Retry-After: 60`; sin reintento automático inmediato |
| 500 | Error inesperado | Mensaje genérico y reintento manual; no mostrar stack ni credenciales |

Los errores usan `application/problem+json`, `status`, `title` y, cuando corresponde, `detail`/`instance`. La validación automática añade `errors`; no depender del texto literal ni asumir un campo `errorCode` en estos endpoints. Ejemplo:

```json
{
  "title": "Resource not found",
  "status": 404,
  "detail": "Client was not found.",
  "instance": "/api/v1/clients/22222222-2222-4222-8222-222222222222/users"
}
```

Operaciones síncronas, sin jobs ni polling de invitaciones: bloquear el botón mientras carga y evitar doble submit. Tras crear/revocar invitación, refrescar su listado. Tras quitar acceso, refrescar usuarios e invitaciones. Tras aceptar, consultar `/auth/me`, `/clients` y cuentas del cliente seleccionado. Nunca disparar sincronización Meta desde una sesión ClientViewer.

El rate limit actual es local al proceso y usa la IP de conexión, no cabeceras reenviadas arbitrarias. Para despliegue detrás de proxy o varias instancias hay que configurar proxies confiables y limitación distribuida/perimetral; no declarar esta protección suficiente para publicación abierta.

### Rutas de lectura que puede usar ClientViewer

Las respuestas JSON de secciones 5, 7, 8, 16 y 17 se conservan. Todos estos GET requieren Bearer y filtran por asignación:

```text
GET /api/v1/auth/me
GET /api/v1/clients
GET /api/v1/clients/{clientId}
GET /api/v1/clients/{clientId}/ad-accounts
GET /api/v1/ad-accounts/{adAccountId}
GET /api/v1/ad-accounts/{adAccountId}/sync
GET /api/v1/ad-accounts/{adAccountId}/campaigns?includeMissing=false
GET /api/v1/campaigns/{campaignId}
GET /api/v1/campaigns/{campaignId}/ad-sets?includeMissing=false
GET /api/v1/ad-sets/{adSetId}
GET /api/v1/ad-sets/{adSetId}/ads?includeMissing=false
GET /api/v1/ads/{adId}
GET /api/v1/ad-accounts/{adAccountId}/metrics?since=YYYY-MM-DD&until=YYYY-MM-DD
GET /api/v1/campaigns/{campaignId}/metrics?since=YYYY-MM-DD&until=YYYY-MM-DD
GET /api/v1/ad-sets/{adSetId}/metrics?since=YYYY-MM-DD&until=YYYY-MM-DD
GET /api/v1/ads/{adId}/metrics?since=YYYY-MM-DD&until=YYYY-MM-DD
GET /api/v1/ad-accounts/{adAccountId}/metrics/summary?since=YYYY-MM-DD&until=YYYY-MM-DD
GET /api/v1/campaigns/{campaignId}/metrics/summary?since=YYYY-MM-DD&until=YYYY-MM-DD
GET /api/v1/ad-sets/{adSetId}/metrics/summary?since=YYYY-MM-DD&until=YYYY-MM-DD
GET /api/v1/ads/{adId}/metrics/summary?since=YYYY-MM-DD&until=YYYY-MM-DD
GET /api/v1/ad-accounts/{adAccountId}/campaigns/selector?since=YYYY-MM-DD&until=YYYY-MM-DD&activity=WithActivity
```

Métricas: enviar siempre ambas fechas, rango inclusivo máximo 90 días, inicio no posterior al fin, fin no futuro UTC. Las series diarias preservan `null` y calidad de observación. Los resúmenes y selector están definidos de forma precisa en sección 20; ClientViewer puede leerlos únicamente para clientes activos asignados. Un array vacío no equivale a gasto cero. `GET .../sync` permite conocer estado, no lanzar sincronización. Los estados de conexión existentes siguen siendo los de sección 15, no inferir conexión por tener campañas almacenadas.

### Instrucciones concretas para implementar frontend

Leer completos este documento, `PLAN.md`, `analitiads-backend/FUSION_DASHBOARD_MCP.md` y el README de `analitiads-backend/assets/claude-dashboard/`. `CLAUDE_DASHBOARD_HANDOFF.md` aporta contexto histórico; no contratos nuevos. Respetar además la skill/AGENTS propios del frontend; la skill backend no autoriza editarlo.

Rutas UI propuestas (NO endpoints API; adaptar al router existente sin duplicar aplicación):

- `/aceptar-invitacion`: formulario de código, email y contraseña; ayuda distinta para cuenta nueva/existente, validaciones, carga, 400/401/409/429, y navegación privada tras 200. No incluir código en la URL.
- `/app/clientes/[clientId]/accesos`: solo Owner/Admin. Listas de invitados/usuarios, creación con email, código mostrado una vez y confirmación de revocación.
- Área privada existente de dashboard: variante `ClientViewer` con selector cliente → cuenta de la API, Campaign → AdSet → Ad, fechas y series diarias; estado “Aún no tienes clientes asignados” para lista vacía. Si se elige `/app/dashboard` como URL, es una decisión de UI, no una nueva ruta de backend.

Componentes: formulario de invitación, panel de accesos, guardas por rol, selector de clientes/cuentas autorizados, tabla de jerarquía, gráficos diarios y estados vacíos/error/sin permiso. Ocultar a ClientViewer CRUD, sincronización, integración Meta y configuración técnica. Ningún permiso depende solo de ocultar botones.

Mantener `NEXT_PUBLIC_API_URL=http://localhost:5019` como se documenta en la sección 14; añadir `/api/v1` en el cliente HTTP según las rutas anteriores, sin duplicarlo. No se requiere nueva variable pública para invitaciones ni IA. Arrancar Next.js exclusivamente en `3001`, sin buscar automáticamente `3000` como alternativa. El único origen CORS local permitido es `http://localhost:3001`; callback OAuth de Meta sigue en el backend `http://localhost:5019/api/v1/meta/oauth/callback`.

Prohibido por ahora: totales de reach o frequency para rangos, promedios de ratios diarios como KPIs globales, métricas nuevas copiadas del catálogo de Claude, objetivos empresariales editables, Comunidad/breakdowns, thumbnails reales, comparaciones, benchmarks, recomendaciones, Google Ads/GA4/TikTok, reportes/PDF, enlaces públicos y botones “Conectar Claude/IA” funcionales. No existe `/mcp`; no implementar `window.claude`, keys de proveedor ni dependencia de Claude Desktop en la web.

Las siete migraciones, incluida `20260911182820_AddMetricRangeConsolidation`, ya fueron aplicadas y verificadas en la instancia Neon autorizada el 11 de septiembre de 2026. El esquema conserva los datos existentes; snapshots previos a Fase 7 están marcados como `LegacyZeroNormalized` hasta una re-sincronización. No quedó un servidor backend encendido. La cadena de Neon vive solo en User Secrets y nunca debe copiarse al frontend.

## 20. Fase 7: resúmenes seguros y selector de Campaign

Esta sección reemplaza cualquier indicación anterior que diga que no hay agregación segura de rango o selector por actividad. No cambia el flujo OAuth, las rutas de jerarquía ni los permisos de invitación.

### Reglas comunes

- Todas las rutas de esta sección son `GET`, requieren `Authorization: Bearer <accessToken>` y no tienen body.
- `since` y `until` son obligatorios, inclusivos, `YYYY-MM-DD`, máximo 90 días, `since <= until` y `until` no puede ser futuro UTC. El backend no acepta `agencyId`.
- `Owner`, `Admin`, `Analyst` y `Viewer` leen toda su agencia. `ClientViewer` solo puede leer recursos de clientes activos asignados; fuera de ese alcance recibe `404`.
- Las consultas solo usan snapshots persistidos y autorizados: no lanzan una llamada a Meta ni inventan ceros para días sin fila.
- `POST /ad-accounts/{adAccountId}/metrics/sync` sigue reservado a `Owner`, `Admin` y `Analyst`. `Viewer` y `ClientViewer` reciben `403`.

### Series diarias corregidas

Las cuatro rutas diarias de la sección 17 se conservan. Su cambio obligatorio es:

```ts
type ObservedDataQuality = "FieldPresencePreserved" | "LegacyZeroNormalized";

interface InsightSnapshot {
  id: string;
  adAccountId: string;
  campaignId: string | null;
  adSetId: string | null;
  adId: string | null;
  level: "Account" | "Campaign" | "AdSet" | "Ad";
  date: string;
  currency: string;
  observedDataQuality: ObservedDataQuality;
  observed: {
    spend: number | null;
    impressions: number | null;
    reach: number | null;
    linkClicks: number | null;
    leads: number | null;
    purchases: number | null;
    purchaseValue: number | null;
  };
  derived: {
    frequency: number | null; cpm: number | null; ctr: number | null;
    cpc: number | null; cpl: number | null; cpa: number | null; roas: number | null;
  };
  observedAtUtc: string;
}
```

`FieldPresencePreserved` significa que `null` es ausencia y `0` es cero explícito de Meta. `LegacyZeroNormalized` significa que la fila histórica no permite distinguir todos los ceros de una ausencia: mostrar “Datos históricos por re-sincronizar”, no sustituirlos por un KPI confiable. Un `null` diario se muestra como `—` o “Sin dato”; nunca como `0`.

### Endpoints de resumen de rango

```http
GET /api/v1/ad-accounts/{adAccountId}/metrics/summary?since=2026-08-01&until=2026-08-31
GET /api/v1/campaigns/{campaignId}/metrics/summary?since=2026-08-01&until=2026-08-31
GET /api/v1/ad-sets/{adSetId}/metrics/summary?since=2026-08-01&until=2026-08-31
GET /api/v1/ads/{adId}/metrics/summary?since=2026-08-01&until=2026-08-31
```

Respuesta `200 OK` — el mismo modelo se usa en los cuatro niveles; los UUID de niveles no aplicables son `null`:

```json
{
  "adAccountId": "00000000-0000-0000-0000-000000000000",
  "campaignId": null,
  "adSetId": null,
  "adId": null,
  "level": "Account",
  "since": "2026-08-01",
  "until": "2026-08-31",
  "currency": "USD",
  "currencyStatus": "Single",
  "coverage": {
    "requestedDays": 31,
    "snapshotDays": 28,
    "legacyZeroNormalizedSnapshotDays": 0,
    "firstSnapshotDate": "2026-08-01",
    "lastSnapshotDate": "2026-08-31"
  },
  "observed": {
    "spend": { "value": 100.25, "availability": "CompleteForSnapshots" },
    "impressions": { "value": 10000, "availability": "CompleteForSnapshots" },
    "reach": { "value": null, "availability": "NotAvailableForRange" },
    "linkClicks": { "value": 200, "availability": "CompleteForSnapshots" },
    "leads": { "value": 10, "availability": "CompleteForSnapshots" },
    "purchases": { "value": 4, "availability": "CompleteForSnapshots" },
    "purchaseValue": { "value": 500, "availability": "CompleteForSnapshots" }
  },
  "derived": {
    "frequency": { "value": null, "availability": "NotAvailableForRange" },
    "cpm": { "value": 10.025, "availability": "CompleteForSnapshots" },
    "ctr": { "value": 2, "availability": "CompleteForSnapshots" },
    "cpc": { "value": 0.50125, "availability": "CompleteForSnapshots" },
    "cpl": { "value": 10.025, "availability": "CompleteForSnapshots" },
    "cpa": { "value": 25.0625, "availability": "CompleteForSnapshots" },
    "roas": { "value": 4.9875311721, "availability": "CompleteForSnapshots" }
  }
}
```

`currencyStatus` puede ser `NoData`, `Single` o `Mixed`. Con `Single`, `currency` trae el código; con `NoData` o `Mixed`, es `null`.

Cada métrica de rango tiene `{ value, availability }`. El frontend debe decidir si muestra el número exclusivamente por `availability`, no por si JavaScript considera el valor truthy:

- `CompleteForSnapshots`: cada snapshot persistido del rango tiene ese campo y no es histórico ambiguo. No implica que haya snapshot para todos los días solicitados: mostrar siempre `coverage`.
- `NoData`: no hay snapshots para esa entidad/rango.
- `Incomplete`: algún snapshot tiene el campo ausente (`null`); no se publica una suma parcial.
- `LegacyZeroNormalized`: al menos un snapshot es anterior a la corrección; no se publica el total.
- `MixedCurrency`: un valor monetario o ratio que depende de dinero no se suma entre monedas.
- `NotAvailableForRange`: cálculo intencionalmente prohibido para ese rango.
- `Undefined`: los datos requeridos existen, pero el denominador conocido es cero.

Para un rango, el backend suma solo observados completos y compatibles; vuelve a calcular `CPM`, `CTR`, `CPC`, `CPL`, `CPA` y `ROAS` desde esos totales. Nunca promedia ratios diarios. `reach` no se suma y `frequency` tampoco se calcula para rangos: ambos siempre usan `NotAvailableForRange`. No implementar cálculos alternativos en el navegador.

### Selector de Campaign

```http
GET /api/v1/ad-accounts/{adAccountId}/campaigns/selector?since=2026-08-01&until=2026-08-31&activity=WithActivity
```

`activity` es opcional y no distingue mayúsculas/minúsculas; los valores canónicos son:

- `WithActivity` (predeterminado): solo Campaign actuales con algún observado positivo en el rango.
- `WithSpend`: solo Campaign actuales con gasto observado positivo.
- `All`: todas las Campaign actuales autorizadas, incluso sin snapshots.

Otro valor devuelve `400` Problem Details. La respuesta `200 OK` es:

```json
{
  "adAccountId": "00000000-0000-0000-0000-000000000000",
  "since": "2026-08-01",
  "until": "2026-08-31",
  "activityFilter": "WithActivity",
  "campaigns": [
    {
      "id": "00000000-0000-0000-0000-000000000000",
      "name": "Prospección",
      "objective": "SALES",
      "configuredStatus": "ACTIVE",
      "effectiveStatus": "ACTIVE",
      "isPresentOnMeta": true,
      "hasActivity": true,
      "hasObservedSpend": true,
      "currency": "USD",
      "currencyStatus": "Single",
      "coverage": {
        "requestedDays": 31,
        "snapshotDays": 28,
        "legacyZeroNormalizedSnapshotDays": 0,
        "firstSnapshotDate": "2026-08-01",
        "lastSnapshotDate": "2026-08-31"
      },
      "spend": { "value": 100.25, "availability": "CompleteForSnapshots" }
    }
  ]
}
```

`hasActivity` y `hasObservedSpend` sirven para filtrar y etiquetar campañas; no reemplazan el estado de disponibilidad del objeto `spend`. En particular, si `spend.availability` no es `CompleteForSnapshots`, no mostrar una cifra como total verificado.

### Errores y refresco

- `400` Problem Details: fechas faltantes, formato inválido, rango futuro, rango de más de 90 días o `activity` inválido.
- `401`: JWT ausente, vencido o inválido.
- `403`: solo al intentar sincronizar sin rol `Owner`, `Admin` o `Analyst`.
- `404`: entidad inexistente o fuera del tenant/asignación del `ClientViewer`.
- `409` y `502`: posibles en la sincronización de Meta ya documentada (conexión no válida/expirada, o respuesta/jerarquía Meta inválida); las rutas GET de Fase 7 no sincronizan.
- `500`: error inesperado. No exponer el detalle técnico al usuario.

Después de un `POST /metrics/sync` exitoso, invalidar/refrescar: la serie diaria abierta de esa cuenta o entidad, su resumen de rango abierto y el selector de Campaign de esa cuenta/rango. No refrescar JWT, lista de clientes u OAuth. Si el POST devuelve `502`, sincronizar primero la jerarquía de Fase 5 y luego reintentar.

### INSTRUCCIONES PARA EL AGENTE FRONTEND

Antes de cambiar UI, leer completos este archivo (especialmente secciones 17, 19 y 20), `PLAN.md` y `analitiads-backend/FUSION_DASHBOARD_MCP.md`. Implementar únicamente:

- Adaptación de tipos y gráficos diarios para observados `null` y `observedDataQuality`.
- Tarjetas de resumen que consuman las cuatro rutas `/metrics/summary`, muestren `coverage` y el estado de disponibilidad, sin cálculos locales.
- Selector que consuma `/campaigns/selector`, permita `WithActivity`, `WithSpend` y `All`, y conserve los límites de fecha.
- Estados de carga, rango vacío, error, acceso revocado, datos incompletos, datos históricos por re-sincronizar y moneda mixta.
- Refresco de las rutas indicadas tras una sincronización autorizada. Mantener ClientViewer como solo lectura y limitado a sus clientes asignados.

No implementar todavía MCP/Claude/otra IA, comparaciones, benchmarks, análisis, recomendaciones, gráficos de reach o frequency de rango, breakdowns, reportes/PDF, enlaces compartibles, jobs programados, escritura en Meta, ni variables/secretos nuevos. Usar exclusivamente `NEXT_PUBLIC_API_URL=http://localhost:5019` durante desarrollo, ejecutar Next.js en el puerto `3001`, y no tocar el puerto `3000`.
