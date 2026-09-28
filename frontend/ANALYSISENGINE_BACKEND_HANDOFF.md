# Handoff de integración — Fases 9, 10 y 11 AnalysisEngine y ReportData

Fecha: 15 de septiembre de 2026.

## Estado

La interfaz de AnalysisEngine quedó integrada y validada contra el backend en Development, disponible durante la prueba en `http://localhost:5019`. La ampliación de Fase 10 incorpora la representación de insights y recomendaciones devueltos en la misma respuesta. Fase 11 añade la creación y consulta de snapshots inmutables de ese análisis.

No hay incompatibilidades de contrato detectadas. El frontend consume exclusivamente `POST /api/v1/analyses` mediante el cliente central `lib/api.ts`, con `Authorization: Bearer {jwt}` y JSON camelCase.

## Contrato consumido por el frontend

Solicitud:

```json
{
  "adAccountId": "uuid",
  "since": "2026-08-30",
  "until": "2026-09-03",
  "comparison": "PreviousPeriod | PreviousMonth | PreviousYear | Custom | null",
  "comparisonSince": "yyyy-MM-dd | null",
  "comparisonUntil": "yyyy-MM-dd | null",
  "campaignIds": "null | [] | [uuid]",
  "selectedMetrics": "null | [metric]"
}
```

Semántica aplicada:

- `campaignIds: null`: todas las campañas autorizadas de la cuenta.
- `campaignIds: []`: solo resultado de la cuenta, sin campañas, conjuntos ni anuncios.
- `campaignIds: [uuid, ...]`: únicamente campañas autorizadas de esa cuenta.
- `selectedMetrics: null`: todas las métricas soportadas.
- Para `Custom`, el frontend exige `comparisonSince` y `comparisonUntil`, ambos válidos y anteriores al período actual.

El frontend no envía `agencyId`, secretos de Meta ni tokens MCP. Tampoco calcula métricas, comparaciones, porcentajes o benchmarks.

## Campos de respuesta utilizados

La interfaz consume los siguientes campos, respetando exactamente sus valores:

- Resultado: `adAccountId`, `clientId`, `since`, `until`, `comparisonType`, `selectedMetrics`, `evaluatedAtUtc`.
- Entidades: `account`, `campaigns`, `adSets`, `ads`.
- Por entidad: `level`, `id`, `parentId`, `name`, `objective`, `currency`, `coverage`, `sufficiency`, `metrics`, `comparison`, `benchmarks`.
- Cobertura: `requestedDays`, `snapshotDays`, `legacyZeroNormalizedSnapshotDays`, `firstSnapshotDate`, `lastSnapshotDate`.
- Suficiencia: `Sufficient`, `Partial`, `Insufficient`, y las razones devueltas por el servidor.
- Métricas: `value`, `availability`, `source`.
- Secciones pendientes: `unavailableSections[].section` y `unavailableSections[].reason`.
- Insights: `insights[].ruleId`, `level`, `entityIds`, `severity`, `confidence`, `sufficiency`, `message` y `evidence`.
- Evidencia: `metric`, `value`, `referenceValue`, `percentageDifference` y `availability`.
- Recomendaciones: `recommendations[].ruleId`, `level`, `entityIds`, `priority`, `message` y `actions`.

Solo se presenta un valor de métrica cuando `availability` es `CompleteForSnapshots` y `value` no es `null`. Estados como `Incomplete`, `MixedCurrency` y `NotAvailableForRange` permanecen visualmente no disponibles; nunca se transforman en cero.

Para comparación y benchmark, el frontend muestra los valores solamente cuando el backend entrega `availability: Available`.

## Fase 10 — insights y recomendaciones

No se incorporó una ruta ni un request nuevo. El frontend conserva `POST /api/v1/analyses`, el Bearer JWT, el JSON camelCase y el manejo de errores de Fase 9.

- Los insights se muestran como hallazgos trazables: regla, nivel, entidades afectadas, severidad, confianza, suficiencia, mensaje y tabla de evidencia.
- La evidencia conserva los números y el estado de disponibilidad del backend. Sus valores y diferencias porcentuales se muestran solo cuando `availability` es `Available`; el navegador no infiere valores ni porcentajes.
- Las recomendaciones se muestran por separado con regla, nivel, prioridad, entidades afectadas, mensaje y acciones ordenadas.
- Los nombres de las entidades se resuelven desde la jerarquía incluida en la misma respuesta; si una entidad no forma parte de ella, se conserva su identificador recibido.
- Cuando un array llega vacío, la interfaz informa que no se activaron reglas o que no se propusieron acciones para el alcance consultado.

## Fase 11 — ReportData, reporte web y PDF

El frontend consume las rutas de reportes con JWT Bearer y el cliente HTTP central:

- `POST /api/v1/reports`: Owner, Admin y Analyst pueden guardar un reporte con título desde la misma selección que generó el resultado de AnalysisEngine.
- `GET /api/v1/clients/{clientId}/reports?page=1&pageSize=20`: lista paginada y ordenada por el backend, disponible para todos los roles autorizados al cliente.
- `GET /api/v1/reports/{reportId}`: muestra el snapshot persistido. La vista reutiliza únicamente `report.analysis`; nunca llama de nuevo a `/api/v1/analyses`, Meta ni métricas actuales.
- `GET /api/v1/reports/{reportId}/pdf`: descarga autenticada como `blob`. La URL temporal se revoca al finalizar y no se almacena ningún PDF, blob o URL en el navegador.
- `DELETE /api/v1/reports/{reportId}`: se muestra con confirmación solo a Owner y Admin. Tras `204`, se elimina el detalle y se vuelve al listado.

La interfaz expone título, fecha, versión de esquema y `snapshotHash` como información de integridad. No envía agencia, cliente, creador, versión ni hash al crear. Los valores nulos, las métricas, las comparaciones, los benchmarks, los insights y las recomendaciones siguen siendo los del snapshot entregado por el backend.

Un `404` limpia el detalle y la lista privada en memoria; desde el detalle devuelve al listado autorizado del cliente. `401`, `403`, `400`, `409` y errores inesperados muestran el estado correspondiente sin conservar información privada obsoleta.

## Matriz ejecutada contra datos demo

Rango principal: `2026-08-30` a `2026-09-03`.

| Caso | Resultado |
| --- | --- |
| Todas las campañas (`campaignIds: null`) | `200`; 9 campañas, 9 conjuntos y 9 anuncios. |
| Solo cuenta (`campaignIds: []`) | `200`; sin entidades hijas. |
| Una campaña concreta | `200`; 1 campaña, 1 conjunto y 1 anuncio. |
| Comparación `Custom` | `200`; `comparisonType: Custom`. |
| Suficiencia | Se observaron `Sufficient`, `Partial` e `Insufficient`. |
| Sin snapshots | `Insufficient` con `NoSnapshots`, `PartialDateCoverage` y `UnavailableSelectedMetrics`. |
| Moneda mixta | `Partial` con `MixedCurrency`; las métricas monetarias se devolvieron no disponibles. |
| Datos legacy | `Ventas principal` devolvió `LegacyZeroNormalized` y `legacyZeroNormalizedSnapshotDays: 1`. |
| Benchmark disponible | `Benchmark completo A`: `Available`, con 2 comparables. |
| Benchmark indefinido | `Benchmark cero evaluado`: `UndefinedBenchmark`, con 2 comparables. |
| Validación | Campaña ajena/inexistente: `400`; solicitud sin Bearer: `401`. |
| Roles | Owner, Admin, Analyst, Viewer y ClientViewer: `200` para sus cuentas autorizadas. |
| Aislamiento ClientViewer | Un cliente asignado; consulta a cuenta fuera de su asignación: `404`. |
| Segunda agencia | `owner.isolation@analitiads.local` recibió `404` al consultar una cuenta de la primera agencia. |

## Tratamiento del frontend ante errores

- `400`: muestra `ProblemDetails.detail`.
- `401`: `lib/api.ts` limpia `sessionStorage` y el shell vuelve a `/login`.
- `403`: la vista informa falta de permisos.
- `404`: limpia la selección y los datos privados del dashboard para bloquear recursos que ya no pertenecen al usuario.

El endpoint de análisis permite consultar a los cinco roles definidos, por lo que no existe un escenario `403` válido de AnalysisEngine en el dataset demo. El manejo visual quedó preparado para cambios futuros de política.

## Observación para backend

La consulta de todas las campañas del rango principal terminó correctamente en aproximadamente 24 segundos. No bloquea la integración actual —el frontend muestra estado de carga y deshabilita el botón—, pero conviene medir este caso si se espera que una cuenta tenga más campañas, conjuntos o anuncios. La respuesta actual recorre jerarquía completa y podría necesitar optimización antes de ampliar el volumen de datos.

## Sin cambios solicitados al backend

El contrato actual de las Fases 9, 10 y 11 es compatible con el frontend. No se requieren nuevas rutas ni cambios de nombres, serialización, JWT, aislamiento multi-tenant o CORS para esta integración.

## Confirmación de cierre del backend

El backend confirma que las Fases 9, 10 y 11 quedan cerradas técnicamente. No pudo repetir la medición de las nueve campañas porque la contraseña de los usuarios demo no está disponible en User Secrets ni en documentación del backend.

El frontend conserva el caso de prueba y puede repetirlo cuando el backend proporcione un mecanismo documentado y seguro para autenticar usuarios demo. La medición anterior de aproximadamente 24 segundos queda registrada como observación de la integración frontend, no como benchmark reproducido por backend.

La integración visual y de tipos de Fases 10 y 11 está completa en el frontend. Queda pendiente ejecutar la matriz HTTP de reportes con un usuario demo autorizado para registrar creación, listado, detalle, PDF, eliminación y revocación de acceso de extremo a extremo.
