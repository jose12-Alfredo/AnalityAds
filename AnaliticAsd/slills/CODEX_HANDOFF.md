# Traspaso completo de AnalitiAds para otro chat de Codex

> Actualización 16 de septiembre de 2026: `AnalitiAds.sln` y la reconstrucción paralela en `src/` y `tests/` fueron eliminadas. Trabajar solamente con `AnaliticAsd.sln` y `AnaliticAsd/`.

> Actualización 15 de septiembre de 2026: Fase 12 completada. Enlaces compartibles de solo lectura usan token hasheado, expiración, revocación, auditoría y acceso público por body para evitar secretos en rutas del servidor. Compilación limpia, 81/81 pruebas, modelo EF sin cambios pendientes y `20260915181708_AddReportShareLinks` aplicada en Neon. Existen diez migraciones. Ver `PHASE12_BACKEND_HANDOFF.md`.

> Actualización 15 de septiembre de 2026: Fase 11 completada. ReportData se persiste como snapshot inmutable, la vista JSON y el PDF usan ese mismo contenido y las rutas revalidan acceso. Compilación limpia, 79/79 pruebas, modelo EF sin cambios pendientes y migración `20260915171245_AddImmutableReports` aplicada en Neon. Existen nueve migraciones aplicadas. Ver `PHASE11_BACKEND_HANDOFF.md`.

> Actualización 15 de septiembre de 2026: Fase 10 completada. `POST /api/v1/analyses` añade insights y recomendaciones deterministas con evidencia auditable. 77/77 pruebas aprobadas, compilación sin errores ni advertencias y sin migración nueva. Ver `PHASE10_BACKEND_HANDOFF.md`.

> Auditoría operativa más reciente: `BACKEND_AGENT_HANDOFF.md`, 15 de septiembre de 2026. Usar ese documento para iniciar un nuevo agente backend.

Fecha de consolidación: 14 de septiembre de 2026.

Este documento es la fuente de verdad resumida para retomar el backend. Si una sección histórica de `PLAN.md`, `traspaso.md`, `FRONTEND_HANDOFF.md` o `SKILL.md` contradice este estado, prevalece este archivo junto con las actualizaciones más recientes situadas al comienzo de esos documentos.

## Instrucción inicial para el próximo Codex

Antes de modificar código, leer completamente y en este orden:

1. `slills/analitiads-backend/SKILL.md`
2. `slills/CODEX_HANDOFF.md`
3. `slills/FRONTEND_HANDOFF.md`
4. `slills/ANALYSISENGINE_BACKEND_HANDOFF.md`
5. `slills/PLAN.md`
6. `slills/instruccion.md`
7. `slills/traspaso.md`
8. `slills/analitiads-backend/FUSION_DASHBOARD_MCP.md`

Después, inspeccionar la solución, el código relacionado y el estado de Git. No asumir que una nota histórica representa el estado vigente.

## Límites autorizados

Trabajar exclusivamente en:

```text
C:\Users\jose1\RiderProjects\AnaliticAsd\AnaliticAsd.sln
C:\Users\jose1\RiderProjects\AnaliticAsd\AnaliticAsd\
```

La documentación del backend vive en `AnaliticAsd/slills/`.

No modificar ni usar como implementación principal:

- `frontend/`
- `AnalitiAds.sln`
- `src/`
- `tests/` de la raíz
- otros proyectos o soluciones paralelos

No borrar esos elementos. El frontend se desarrolla por separado y usa `http://localhost:3001`. El puerto 3000 pertenece a otro proyecto y no debe tocarse. No dejar el backend ejecutándose al terminar.

## Estado vigente

Las Fases 0, 1, 2, 3, 4, 5, 6, 6A, 7, 7B, 8 y 9 están implementadas. La última validación completa registrada fue:

- compilación de `AnaliticAsd.sln`: 0 errores y 0 advertencias;
- pruebas: 73/73 aprobadas;
- modelo EF Core sin cambios pendientes;
- ocho migraciones aplicadas en Neon;
- ningún backend quedó escuchando en los puertos locales 5019 o 7178.

La próxima fase es **Fase 10 — reglas, insights y recomendaciones**. No debe iniciarse sin autorización expresa del usuario. La autorización de Fase 9 fue consumida al completarla.

### AnalysisEngine — Fase 9

- `POST /api/v1/analyses` ejecuta un análisis determinista bajo demanda y no persiste resultados.
- Recibe cuenta, rango, comparación opcional, UUID de campañas y métricas seleccionadas.
- Reutiliza resúmenes, comparaciones y benchmarks existentes; no consulta Meta ni recalcula cifras con fórmulas alternativas.
- Devuelve cuenta, Campaign, AdSet y Ad con métricas, comparación, benchmark cuando aplica, cobertura y suficiencia `Sufficient`, `Partial` o `Insufficient` con motivos estructurados.
- Una lista de campañas `null` analiza todas las actuales; una lista vacía produce análisis solo de cuenta; UUID repetidos o no disponibles devuelven `400`.
- Audiencias, placements y atributos creativos se declaran en `unavailableSections`; no se inventan datos.
- La autorización reutiliza los servicios tenant-safe. `ClientViewer` continúa limitado a clientes activos asignados y los cruces devuelven `404`.
- No incluye frases, alertas interpretativas, scoring, recomendaciones, persistencia, MCP nuevo, reportes ni PDF.
- El frontend completó la integración y su matriz real de cinco roles, aislamiento, selección, comparaciones, benchmarks y estados de datos; ver `ANALYSISENGINE_BACKEND_HANDOFF.md`.
- La implementación posterior eliminó el patrón de consultas por entidad: jerarquía y snapshots se cargan por cuenta y rango. El contrato público permanece congelado.

## Arquitectura actual

- ASP.NET Core Web API en .NET 10.
- Una solución y un proyecto: `AnaliticAsd.sln` y `AnaliticAsd/AnaliticAsd.csproj`.
- EF Core con PostgreSQL/Neon; SQLite para pruebas de integración.
- JWT web y esquema Bearer separado para MCP.
- Roles internos: `Owner`, `Admin`, `Analyst` y `Viewer`.
- Rol externo: `ClientViewer`, limitado a clientes asignados.
- OpenAPI, Problem Details y endpoint de estado.
- Servicios y autorización compartidos entre REST y MCP.

## Funcionalidad implementada

### Base, clientes y autenticación

- `GET /api/system/status`.
- `POST /api/v1/auth/register`.
- `POST /api/v1/auth/login`.
- `GET /api/v1/auth/me`.
- Agencias, usuarios, membresías, roles y autorización multi-tenant.
- CRUD tenant-safe de clientes y cuentas publicitarias.
- Contraseñas mediante hash; secretos fuera del repositorio.

### Acceso externo por cliente — Fase 6A

`ClientAccessController` implementa:

- `GET /api/v1/clients/{clientId}/users`
- `DELETE /api/v1/clients/{clientId}/users/{userId}`
- `GET /api/v1/clients/{clientId}/invitations`
- `POST /api/v1/clients/{clientId}/invitations`
- `DELETE /api/v1/clients/{clientId}/invitations/{invitationId}`
- `POST /api/v1/auth/client-invitations/accept`

Estas rutas quedan congeladas sin el segmento `/access`, de acuerdo con el controlador y el frontend. El inicio OAuth de Meta queda congelado como `GET /api/v1/meta/oauth/start`; devuelve la URL de autorización y no recibe body.

Las invitaciones duran 48 horas, se persisten mediante hash, son revocables y de un solo uso. `Owner` y `Admin` administran el acceso. Un `ClientViewer` solo puede leer los clientes asignados y su asignación se comprueba nuevamente en cada consulta.

### Meta y jerarquía publicitaria

- OAuth de Meta resuelto desde backend.
- Token Meta protegido en servidor; nunca se entrega al frontend o MCP.
- Descubrimiento paginado y asociación de cuentas publicitarias.
- Persistencia y consulta de Campaign, AdSet y Ad mediante IDs de Meta.
- Sincronización de jerarquía con estado consultable.
- Pruebas con HTTP simulado, sin credenciales Meta reales.
- La validación completa contra una app Meta real sigue pendiente como tarea de entorno.

### Métricas — Fases 6 y 7

- Snapshots diarios para Account, Campaign, AdSet y Ad.
- Dinero y métricas fraccionarias usan `decimal`.
- Los observados anulables distinguen ausencia y cero explícito.
- `FieldPresencePreserved` identifica presencia conservada durante la ingesta.
- `LegacyZeroNormalized` identifica filas históricas cuyo cero no puede verificarse.
- La ingesta rechaza duplicados, filas fuera del rango y jerarquías inconsistentes.
- Rangos máximos de 90 días, sin fechas futuras.
- Resúmenes seguros para Account, Campaign, AdSet y Ad.
- `reach` no se suma entre días y `frequency` no se agrega por rango.
- Los ratios se recomputan desde componentes seguros; no se promedian ratios diarios.
- Selector de campañas por actividad, gasto o todas.

Los contratos exactos están en `FRONTEND_HANDOFF.md`.

### MCP de solo lectura — Fase 7B

- Paquete oficial `ModelContextProtocol.AspNetCore` 2.2.0.
- Streamable HTTP stateless en `/mcp`.
- Metadatos: `/.well-known/oauth-protected-resource/mcp`.
- Scope obligatorio: `analitiads:read`.
- Audiencia MCP separada; el JWT web no sirve como credencial MCP.
- Gestión de conexiones:
  - `GET /api/v1/mcp/connections`
  - `POST /api/v1/mcp/connections`
  - `DELETE /api/v1/mcp/connections/{id}`
- La creación exige nombre y `consentAccepted: true`.
- La credencial se muestra una sola vez, vence a los 30 días y es revocable.
- Cada llamada revalida usuario, membresía, agencia, rol, conexión y alcance `ClientViewer`.
- Auditoría sin argumentos, tokens, secretos ni cadenas de conexión.
- Sin clave global de administrador ni token passthrough de Meta.
- MCP es opcional; la web funciona desconectada de MCP.

Herramientas disponibles:

1. `list_authorized_clients`
2. `list_authorized_ad_accounts`
3. `list_campaigns`
4. `get_campaign`
5. `list_ad_sets`
6. `get_ad_set`
7. `list_ads`
8. `get_ad`
9. `get_daily_metrics`
10. `get_metrics_summary`
11. `get_metrics_comparison`
12. `get_campaign_benchmarks`

Las pruebas cubren cliente oficial C#, inicialización, descubrimiento, listado, invocación, anonimato, audiencia y scope incorrectos, rol insuficiente, revocación de conexión y membresía, intento de clave global y cruces de agencia y cliente.

Limitación conocida: la conexión se crea mediante la API autenticada de AnalitiAds y el Bearer se configura en el cliente MCP. Todavía no existe un flujo OAuth Authorization Code automático para clientes que solo admitan autorización mediante navegador.

### Comparaciones y benchmarks — Fase 8

Comparaciones temporales:

- `GET /api/v1/ad-accounts/{id}/metrics/comparison`
- `GET /api/v1/campaigns/{id}/metrics/comparison`
- `GET /api/v1/ad-sets/{id}/metrics/comparison`
- `GET /api/v1/ads/{id}/metrics/comparison`

Usan `since`, `until` y `comparison`; `Custom` también exige `comparisonSince` y `comparisonUntil`. Tipos: `PreviousPeriod`, `PreviousMonth`, `PreviousYear` y `Custom`.

Ambos rangos tienen máximo de 90 días, no admiten futuro y el baseline debe terminar antes del período actual. Cada métrica informa `current`, `baseline`, `absoluteChange`, `percentageChange` y `availability`. Estados: `Available`, `CurrentUnavailable`, `BaselineUnavailable`, `UndefinedBaseline` y `CurrencyMismatch`.

Benchmarks de campañas:

- `GET /api/v1/ad-accounts/{id}/campaigns/benchmarks?since=YYYY-MM-DD&until=YYYY-MM-DD`
- Método `MedianOfOtherCampaignsWithSameObjective`.
- Métricas CPM, CTR, CPC, CPL, CPA y ROAS.
- Exige al menos dos campañas comparables distintas de la evaluada.
- Estados: `Available`, `MetricUnavailable`, `InsufficientComparableCampaigns`, `CurrencyMismatch` y `UndefinedBenchmark`.

Fase 8 no requirió migración.

## Migraciones aplicadas

1. `20260903214558_InitialCreate`
2. `20260903215445_AddIdentityAndMultiTenancy`
3. `20260904182304_AddMetaOAuth`
4. `20260904184710_AddAdvertisingHierarchy`
5. `20260904192718_AddDailyInsightSnapshots`
6. `20260911144440_AddClientAccess`
7. `20260911182820_AddMetricRangeConsolidation`
8. `20260911195705_AddReadOnlyMcpConnections`

La cadena de Neon está solo en User Secrets. Nunca copiarla a documentación, código, configuración versionada, comandos visibles o logs.

## Archivos clave

- Arranque y DI: `Program.cs`
- Acceso autorizado compartido: `Application/Authorization/AuthorizedData.cs`
- Autenticación: `Controllers/AuthController.cs` y `Application/Identity/`
- Acceso externo: `Controllers/ClientAccessController.cs`
- Meta: `Controllers/MetaController.cs` y `Application/Meta/`
- Jerarquía: `Controllers/AdvertisingController.cs` y `Application/Advertising/`
- Métricas: `Controllers/MetricsController.cs` y `Application/Metrics/`
- MCP: `Application/Mcp/AnalitiAdsMcpTools.cs`, `Application/Identity/McpConnectionService.cs`, `Controllers/McpConnectionsController.cs` e `Infrastructure/Identity/McpJwtEvents.cs`
- Persistencia y migraciones: `Infrastructure/Persistence/`
- Pruebas: `Tests/`
- Contrato frontend: `slills/FRONTEND_HANDOFF.md`

## Reglas de seguridad permanentes

- Resolver usuario, membresía y agencia desde la identidad autenticada.
- Nunca aceptar `agencyId` como autoridad desde REST, frontend o MCP.
- Validar membresía y asignaciones en cada llamada sensible.
- Aplicar `ClientViewer` a clientes, cuentas, jerarquía, métricas, comparaciones, benchmarks y funciones futuras.
- No exponer Meta App Secret, Client Secret, access tokens, JWT, contraseñas o cadenas de conexión.
- No usar una clave global de administrador para MCP.
- No permitir escritura en Meta o mutaciones por IA sin una fase futura expresamente autorizada.
- No presentar ausencia como cero, datos legacy como verificados, reach agregado o correlación como causalidad.

## Trabajo pendiente

Fase 11 y posteriores:

- breakdowns de audiencias y creatividades;
- `ReportData`, reporte web y PDF;
- enlaces compartibles;
- IA interpretativa opcional.

Quedan además tareas de endurecimiento que deben planificarse según su fase: refresh/revocación de sesiones web, recuperación y cambio de contraseña, administración de miembros internos, correo real para invitaciones, jobs programados, Data Protection persistente, observabilidad/despliegue y OAuth automático para clientes MCP.

## Coordinación con frontend

Otro agente está construyendo el frontend según las indicaciones del backend. Ese trabajo no fue auditado aquí. Debe consumir `FRONTEND_HANDOFF.md`, manejar valores anulables y estados de disponibilidad, respetar roles y no recalcular métricas que entrega el backend.

El backend debe conservar las rutas documentadas. Cualquier cambio futuro de contrato exige actualizar `FRONTEND_HANDOFF.md` y explicar la adaptación requerida.

### Entorno de integración frontend

- OAuth Meta queda congelado como `GET /api/v1/meta/oauth/start`.
- Los accesos externos quedan congelados en `/api/v1/clients/{clientId}/users` y `/api/v1/clients/{clientId}/invitations`, sin el segmento `/access`.
- Existe un seeder idempotente y exclusivo de Development, activado mediante `DevelopmentSeed:Enabled=true`; la contraseña debe recibirse desde `DevelopmentSeed:Password` y nunca se versiona.
- Usuarios demo: `owner@analitiads.local`, `admin@analitiads.local`, `analyst@analitiads.local`, `viewer@analitiads.local`, `clientviewer@analitiads.local` y `owner.isolation@analitiads.local`.
- El dataset incluye dos agencias, clientes activos e inactivos, un cliente sin acceso vigente, invitación revocada, cuentas, AdSets, Ads, observados nulos, `LegacyZeroNormalized`, moneda mixta y baseline cero. Fechas demo: 2026-08-30 a 2026-09-03.
- Para benchmarks hay tres campañas `LEADS` con métricas completas durante todo el rango; cada una tiene dos pares y produce métricas `Available`. Otro grupo de tres campañas `AWARENESS` deja el CPM de dos pares exactamente en cero, por lo que `Benchmark cero evaluado` produce `UndefinedBenchmark` en CPM.
- Para `UndefinedBaseline`, consultar la comparación de la cuenta demo con `since=2026-09-01`, `until=2026-09-01` y `comparison=PreviousPeriod`; el baseline del 2026-08-31 contiene ceros explícitos.
- La validación OAuth real requiere configurar `Meta:AppId` y `Meta:AppSecret` fuera del repositorio y disponer de una app/usuario/cuenta Meta autorizados.

## Checklist para continuar

1. Confirmar autorización expresa de la fase.
2. Leer los documentos en el orden indicado.
3. Inspeccionar código y Git antes de editar.
4. Reutilizar autorización, servicios y contratos existentes.
5. Añadir pruebas sin depender de credenciales Meta.
6. Ejecutar `dotnet build AnaliticAsd.sln --no-restore`.
7. Ejecutar `dotnet test AnaliticAsd.sln --no-restore`.
8. Si cambia EF Core, comprobar el modelo y gestionar la migración dentro del proyecto autorizado.
9. Actualizar los documentos de continuidad cuando corresponda.
10. Comprobar que no queden secretos ni un backend en ejecución.
