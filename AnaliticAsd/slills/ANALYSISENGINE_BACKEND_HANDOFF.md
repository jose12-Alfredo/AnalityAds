# Handoff de integración — Fase 9 AnalysisEngine

Fecha inicial: 14 de septiembre de 2026. Confirmación de continuidad: 15 de septiembre de 2026.

## Estado verificado por el frontend

La interfaz de AnalysisEngine está integrada y validada contra el backend en Development mediante `POST /api/v1/analyses`. No se detectaron incompatibilidades de contrato y no se solicitan rutas ni cambios de nombres, serialización, JWT, aislamiento multi-tenant o CORS.

El frontend usa el cliente central `lib/api.ts`, JSON camelCase y JWT Bearer. Nunca envía `agencyId`, secretos Meta o tokens MCP, ni recalcula métricas, comparaciones, porcentajes o benchmarks.

## Contrato consumido

La solicitud contiene `adAccountId`, `since`, `until`, `comparison`, `comparisonSince`, `comparisonUntil`, `campaignIds` y `selectedMetrics`.

- `campaignIds: null`: todas las campañas autorizadas.
- `campaignIds: []`: únicamente la cuenta.
- `campaignIds: [uuid, ...]`: campañas autorizadas concretas.
- `selectedMetrics: null`: todas las métricas soportadas.
- `Custom` exige ambos límites comparativos anteriores al periodo actual.

La interfaz consume `account`, `campaigns`, `adSets`, `ads`, `unavailableSections`, cobertura, suficiencia, métricas, comparaciones y benchmarks. Solo muestra métricas con `availability: CompleteForSnapshots` y comparaciones o benchmarks con `availability: Available`; nunca transforma ausencia en cero.

## Matriz real completada

Para `2026-08-30` a `2026-09-03` se verificaron:

- nueve campañas, nueve conjuntos y nueve anuncios con `campaignIds: null`;
- análisis solo de cuenta y selección de una campaña;
- comparación `Custom`;
- suficiencia `Sufficient`, `Partial` e `Insufficient`;
- `NoSnapshots`, `PartialDateCoverage`, `UnavailableSelectedMetrics`, `MixedCurrency` y `LegacyZeroNormalized`;
- benchmark `Available` con dos comparables y `UndefinedBenchmark`;
- `400` por campaña inválida, `401` sin Bearer y `404` por cruce de cliente o agencia;
- acceso autorizado para Owner, Admin, Analyst, Viewer y ClientViewer;
- aislamiento de ClientViewer y de una segunda agencia.

El frontend maneja Problem Details: muestra `detail` en `400`, elimina sesión en `401`, informa permisos en `403` y limpia selecciones privadas en `404`.

## Rendimiento

La primera medición del frontend para la jerarquía completa fue de aproximadamente 24 segundos. El backend se optimizó después para cargar jerarquía y snapshots por cuenta y rango, eliminando consultas repetidas por cada entidad. El contrato público no cambió.

La medición real posterior debe repetirse con las mismas credenciales y datos demo. Las credenciales no se documentan ni se incorporan al repositorio.
