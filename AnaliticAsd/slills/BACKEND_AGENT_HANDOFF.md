# Traspaso operativo para el próximo agente backend de AnalitiAds

> Actualización 16 de septiembre de 2026: `AnalitiAds.sln` y la reconstrucción paralela en `src/` y `tests/` fueron eliminadas. Trabajar solamente con `AnaliticAsd.sln` y `AnaliticAsd/`.

> Actualización 15 de septiembre de 2026: Fase 12 completada. Enlaces compartibles seguros, revocables, expirables y auditados; 81/81 pruebas y migración `20260915181708_AddReportShareLinks` aplicada en Neon. Existen diez migraciones. Ver `PHASE12_BACKEND_HANDOFF.md`.

> Actualización 15 de septiembre de 2026: Fase 11 completada. Existen ReportData inmutable, endpoints REST y PDF basado en el mismo snapshot. 79/79 pruebas, compilación limpia y migración `20260915171245_AddImmutableReports` aplicada en Neon. Existen nueve migraciones aplicadas. Ver `PHASE11_BACKEND_HANDOFF.md`.

> Actualización 15 de septiembre de 2026: Fase 10 completada y documentada en `PHASE10_BACKEND_HANDOFF.md`. El AnalysisEngine devuelve reglas, insights y recomendaciones deterministas; 77/77 pruebas aprobadas y sin migración nueva. La próxima fase funcional es Fase 11 y requiere acordar su contrato.

Fecha de auditoría: 15 de septiembre de 2026.

Este es el documento de entrada para el próximo agente que trabaje en el backend. Resume el código verificado, los contratos congelados, la evidencia disponible y el trabajo pendiente. Las secciones antiguas de otros documentos se conservan como historial; si contradicen este archivo, prevalecen este documento, `CODEX_HANDOFF.md` y las actualizaciones iniciales de `analitiads-backend/SKILL.md`.

## Lectura obligatoria

Antes de modificar código, leer completamente en este orden:

1. `slills/analitiads-backend/SKILL.md`
2. `slills/BACKEND_AGENT_HANDOFF.md`
3. `slills/CODEX_HANDOFF.md`
4. `slills/ANALYSISENGINE_BACKEND_HANDOFF.md`
5. `slills/FRONTEND_HANDOFF.md`
6. `slills/PLAN.md`
7. `slills/instruccion.md`
8. `slills/traspaso.md`
9. `slills/analitiads-backend/FUSION_DASHBOARD_MCP.md`

Después se debe inspeccionar Git y el código relacionado. El árbol ya contiene trabajo del usuario y de agentes anteriores; no limpiar, reemplazar ni revertir archivos ajenos.

## Proyecto autorizado

Trabajar únicamente en:

```text
C:\Users\jose1\RiderProjects\AnaliticAsd\AnaliticAsd.sln
C:\Users\jose1\RiderProjects\AnaliticAsd\AnaliticAsd\
```

Se puede actualizar la documentación de continuidad dentro de `AnaliticAsd/slills/`.

No modificar:

- `frontend/`;
- `AnalitiAds.sln`;
- `src/` de la raíz;
- `tests/` de la raíz;
- proyectos o soluciones paralelos;
- el puerto 3000.

El frontend autorizado usa `http://localhost:3001`. No dejar el backend ejecutándose al finalizar.

## Estado verificado

Están completas las Fases 0, 1, 2, 3, 4, 5, 6, 6A, 7, 7B, 8 y 9.

Estado técnico al comenzar este traspaso:

- ASP.NET Core y .NET 10;
- una solución y un único proyecto web;
- EF Core con PostgreSQL/Neon y SQLite para integración;
- JWT web y autenticación MCP separada;
- ocho migraciones aplicadas en Neon;
- compilación limpia;
- 73 pruebas automatizadas aprobadas;
- no se requiere una migración por Fase 8 o Fase 9.

Evidencia repetida durante la auditoría del 15 de septiembre de 2026:

- `dotnet build AnaliticAsd.sln --no-restore`: aprobado, 0 errores y 0 advertencias;
- `dotnet test AnaliticAsd.sln --no-restore`: 73 aprobadas, 0 fallidas y 0 omitidas;
- `dotnet ef migrations has-pending-model-changes`: no hay cambios de modelo posteriores a la última migración.

## Funcionalidad terminada

### Identidad y multi-tenancy

- Registro, login y consulta de identidad actual.
- Agencias, usuarios y membresías.
- Roles internos `Owner`, `Admin`, `Analyst` y `Viewer`.
- Rol externo `ClientViewer` restringido a clientes asignados.
- El tenant se resuelve desde la identidad autenticada.
- La membresía y las asignaciones vigentes se revalidan en cada operación protegida.
- Los cruces entre agencias o clientes se ocultan como `404` cuando corresponde.

### Clientes, cuentas y acceso externo

- CRUD tenant-safe de clientes y cuentas publicitarias.
- Invitaciones externas de 48 horas, revocables, de un solo uso y persistidas mediante hash.
- Contrato vigente de `ClientAccessController`:
  - `GET /api/v1/clients/{clientId}/users`
  - `DELETE /api/v1/clients/{clientId}/users/{userId}`
  - `GET /api/v1/clients/{clientId}/invitations`
  - `POST /api/v1/clients/{clientId}/invitations`
  - `DELETE /api/v1/clients/{clientId}/invitations/{invitationId}`
  - `POST /api/v1/auth/client-invitations/accept`
- Estas rutas no contienen el segmento `/access`.

### Meta y sincronización

- OAuth iniciado por `GET /api/v1/meta/oauth/start`.
- Callback, conexión protegida, listado paginado de cuentas y asociación a cliente.
- Token Meta cifrado y utilizado exclusivamente por el backend.
- Sincronización y persistencia de Campaign, AdSet y Ad por sus IDs de Meta.
- Sincronización de snapshots diarios por Account, Campaign, AdSet y Ad.
- Pruebas mediante HTTP simulado, sin credenciales Meta.
- Continúa pendiente una validación de extremo a extremo con una app Meta real configurada.

### Métricas seguras

- Observados anulables para distinguir ausencia de cero explícito.
- Estados `FieldPresencePreserved` y `LegacyZeroNormalized`.
- Validación de jerarquía, duplicados, paginación y rango.
- Rango máximo de 90 días y prohibición de fechas futuras.
- Series y resúmenes para Account, Campaign, AdSet y Ad.
- No se suma `reach` por rango ni se agrega `frequency` incorrectamente.
- Ratios derivados desde numeradores y denominadores seguros.
- Selector de campañas por actividad, gasto o todas.

### MCP de lectura

- `ModelContextProtocol.AspNetCore` 2.2.0.
- Streamable HTTP stateless en `/mcp`.
- Scope `analitiads:read` y audiencia separada del JWT web.
- Consentimiento explícito, credenciales por conexión, vencimiento, revocación y auditoría sin secretos.
- Revalidación de usuario, membresía, agencia y `ClientViewer` en cada invocación.
- Doce herramientas para clientes, cuentas, jerarquía, métricas, resúmenes, comparaciones y benchmarks.
- No existe clave global, token passthrough de Meta ni escritura por MCP.
- Limitación vigente: no hay OAuth Authorization Code automático para clientes MCP que solo admitan autorización mediante navegador.

### Comparaciones y benchmarks

- Comparaciones Account, Campaign, AdSet y Ad contra `PreviousPeriod`, `PreviousMonth`, `PreviousYear` o `Custom`.
- Baseline anterior al rango actual, máximo de 90 días y estados explícitos de disponibilidad.
- Benchmarks CPM, CTR, CPC, CPL, CPA y ROAS mediante mediana de otras campañas con el mismo objetivo.
- No se presenta benchmark con muestra insuficiente, moneda incompatible o baseline indefinido.

### Fase 9 — AnalysisEngine

- Endpoint único: `POST /api/v1/analyses`.
- Autenticación: `Authorization: Bearer {jwt}` web.
- JSON camelCase.
- No recibe `agencyId`, secretos Meta ni tokens MCP.
- Solicitud: `adAccountId`, `since`, `until`, comparación opcional, rango comparativo opcional, `campaignIds` y `selectedMetrics`.
- `campaignIds: null` incluye todas las campañas autorizadas.
- `campaignIds: []` analiza solo la cuenta.
- Una lista limita la respuesta a esas campañas; duplicados o campañas no disponibles producen `400`.
- Devuelve evidencia estructurada de Account, Campaign, AdSet y Ad: cobertura, suficiencia, métricas, comparaciones y benchmarks.
- `Audiences`, `Placements` y `Creatives` se declaran en `unavailableSections` mientras no existan datos observados.
- No persiste análisis y no genera frases, scoring ni recomendaciones.
- Reutiliza los cálculos seguros existentes; no consulta Meta durante el análisis.
- La carga fue optimizada para traer jerarquía y snapshots agrupados por cuenta/rango y evitar consultas por cada entidad.

La integración frontend validó por HTTP:

- Owner, Admin, Analyst, Viewer y ClientViewer con respuesta `200` dentro de su alcance;
- campañas inválidas con `400`;
- ausencia de token con `401`;
- recursos fuera del tenant o del cliente asignado con `404`;
- selección de campañas y cuenta sola;
- comparación personalizada;
- suficiencia `Sufficient`, `Partial` e `Insufficient`;
- snapshots ausentes, datos legacy y moneda mixta;
- benchmarks disponibles e indefinidos.

El contrato fue declarado compatible. No cambiar ruta, camelCase, JWT, CORS o aislamiento sin una nueva necesidad verificada y coordinación explícita con frontend.

## Rendimiento pendiente de cerrar

La ejecución inicial del análisis con nueve campañas demo tardó aproximadamente 24 segundos. Después de esa medición, `AnalysisService` fue cambiado para cargar la jerarquía por cuenta y snapshots por nivel/rango.

El próximo agente debe repetir la misma medición con los mismos datos demo y registrar:

- duración total del request;
- cantidad de campañas, AdSets y Ads;
- si incluye comparación;
- consultas SQL y operaciones dominantes, si el tiempo sigue siendo alto;
- comparación antes/después con la referencia de 24 segundos.

No registrar credenciales, JWT, cadenas de conexión ni tokens. Si el rendimiento continúa siendo deficiente, perfilar antes de introducir caché o modificar el contrato. Cualquier caché futura debe conservar revocación de membresía y alcance `ClientViewer`.

## Próxima fase funcional: Fase 10

Fase 10 todavía requiere autorización expresa. Su objetivo es construir reglas, hallazgos y recomendaciones deterministas sobre la evidencia de AnalysisEngine. No debe depender de Claude, OpenAI ni otro modelo.

El nuevo agente debe acordar primero el contrato REST y actualizar `FRONTEND_HANDOFF.md`. No inventar una ruta definitiva ni persistencia sin revisar el producto y el frontend.

Alcance esperado según la especificación del producto:

- resultado estructurado y auditable con identificador de regla, nivel, entidades involucradas, evidencia numérica, severidad, confianza/suficiencia y mensaje controlado;
- regla de concentración de presupuesto: gasto top N dividido por gasto total, sin declarar automáticamente que sea negativo;
- campaña destacada: varias señales comparables, nunca una única métrica;
- bajo rendimiento: eficiencia inferior al grupo comparable con muestra suficiente;
- posible fatiga creativa: evolución conjunta de frecuencia, CTR y CPA durante varios períodos, sin afirmar causalidad;
- problema después del clic: tráfico competitivo y conversión baja, presentado como área posible y no como causa confirmada;
- creativo débil: CPM comparable y CTR inferior al benchmark;
- entrega más cara: CTR relativamente estable junto con aumento de CPM y CPC;
- thresholds configurables para muestra mínima y fuerza de evidencia;
- scoring de 0 a 100 configurable por objetivo solo si queda incluido expresamente en la autorización de Fase 10;
- lenguaje prudente: “compatible con”, “se observa” o “podría”; nunca causalidad inventada.

Antes de implementar cada regla se debe comprobar que AnalysisEngine aporta toda la evidencia necesaria. En el estado actual no existen breakdowns de audiencia, placements ni atributos creativos; las reglas que dependan de esos datos deben devolverse como no disponibles o aplazarse. No inferirlos desde nombres de campañas o anuncios.

Pruebas mínimas esperadas para Fase 10:

- regla activada con evidencia suficiente;
- regla no activada por threshold;
- evidencia parcial, legacy o ausente;
- moneda mixta;
- benchmark no disponible;
- baseline cero o comparación no disponible;
- selección de cuenta sola y campañas concretas;
- roles Owner, Admin, Analyst, Viewer y ClientViewer;
- revocación y cruces entre agencias/clientes;
- respuesta determinista ante la misma entrada;
- ausencia de secretos y de llamadas a IA o Meta.

No declarar Fase 10 completa sin compilación, suite total, documentación de contrato y evidencia multi-tenant.

## Trabajo posterior a Fase 10

Todavía no está implementado:

- breakdowns de audiencias, placements y dispositivos;
- datos y análisis de creatividades;
- `ReportData` inmutable;
- reporte web y PDF desde el mismo snapshot;
- enlaces compartibles con expiración y revocación;
- IA interpretativa opcional;
- OAuth automático para clientes MCP;
- refresh/revocación completa de sesiones web;
- recuperación y cambio de contraseña;
- administración completa de miembros internos;
- envío real de invitaciones por correo;
- sincronización programada y jobs;
- persistencia de Data Protection para despliegue;
- observabilidad y despliegue productivo.

## Migraciones aplicadas

1. `20260903214558_InitialCreate`
2. `20260903215445_AddIdentityAndMultiTenancy`
3. `20260904182304_AddMetaOAuth`
4. `20260904184710_AddAdvertisingHierarchy`
5. `20260904192718_AddDailyInsightSnapshots`
6. `20260911144440_AddClientAccess`
7. `20260911182820_AddMetricRangeConsolidation`
8. `20260911195705_AddReadOnlyMcpConnections`

Fases 8 y 9 no añadieron tablas ni migraciones.

## Archivos de entrada para el código

- Composición: `Program.cs`.
- Autorización central: `Infrastructure/Persistence/AuthorizedData.cs`.
- Tenant actual: `Infrastructure/Identity/CurrentTenant.cs`.
- Acceso externo: `Controllers/ClientAccessController.cs` y `Application/Identity/ClientAccessService.cs`.
- AnalysisEngine: `Controllers/AnalysisController.cs`, `Contracts/Analysis/AnalysisContracts.cs` y `Application/Analysis/`.
- Métricas: `Application/Metrics/`, `Controllers/MetricsController.cs` y `Infrastructure/Persistence/Repositories/MetricsRepository.cs`.
- MCP: `Application/Mcp/AnalitiAdsMcpTools.cs`, `Application/Identity/McpConnectionService.cs` e `Infrastructure/Identity/McpJwtEvents.cs`.
- Pruebas de integración principal: `Tests/Api/ClientAccessApiTests.cs`.
- Pruebas MCP: `Tests/Mcp/McpIntegrationTests.cs`.

## Reglas de seguridad innegociables

- Nunca usar `agencyId` enviado por cliente como autoridad.
- Nunca entregar secretos, JWT, access tokens Meta, credenciales MCP o cadenas de conexión.
- Nunca usar una clave global para saltar autorización.
- Mantener `ClientViewer` restringido a clientes asignados y activos.
- Revalidar identidad y membresía; no congelar permisos revocables en caché.
- No convertir ausencia en cero ni datos legacy en evidencia fiable.
- No sumar reach ni promediar ratios diarios.
- No afirmar causalidad sin evidencia.
- La IA es opcional y no puede alterar métricas oficiales.

## Checklist de cierre para cualquier cambio

1. Mantener el cambio dentro del proyecto autorizado.
2. Preservar contratos frontend vigentes o documentar explícitamente su cambio.
3. Añadir pruebas útiles y tenant-safe.
4. Ejecutar `dotnet build AnaliticAsd.sln --no-restore`.
5. Ejecutar `dotnet test AnaliticAsd.sln --no-restore`.
6. Si cambia EF Core, ejecutar `dotnet ef migrations has-pending-model-changes` y gestionar la migración autorizada.
7. Revisar secretos, logs y datos sensibles.
8. Actualizar `BACKEND_AGENT_HANDOFF.md`, `CODEX_HANDOFF.md`, `PLAN.md`, `FRONTEND_HANDOFF.md` y `traspaso.md` cuando cambie el estado.
9. Confirmar que no quede un backend escuchando en 5019 o 7178.
