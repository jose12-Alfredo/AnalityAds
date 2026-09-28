# Contrato frontend de reportes - Fases 11 y 12

Estado: Fases 11 y 12 aplicadas en Neon. Backend con 81/81 pruebas; migración `20260915181708_AddReportShareLinks` aplicada y diez migraciones registradas.

## Enlaces compartibles

Owner/Admin administran enlaces con `GET|POST /api/v1/reports/{reportId}/share-links` y `DELETE /api/v1/reports/{reportId}/share-links/{shareLinkId}`. Crear recibe `{ expirationDays: 1..30 }` y devuelve el token solo esa vez, además de `sharePath: /reportes-compartidos#token`.

La página pública extrae el fragmento, limpia la URL y conserva el token solo en memoria. Resuelve por `POST /api/v1/shared-reports/access` con `{ accessToken }`; descarga por `POST /api/v1/shared-reports/pdf` con el mismo body y manejo blob. No usa JWT.

No colocar el token en rutas API, query, logs, analytics, localStorage o sessionStorage. Un `404` público siempre se presenta como enlace no disponible. El límite es 60 solicitudes por minuto por IP.

Los gráficos pueden tener tooltips, selección visual de series y zoom sobre el snapshot. No deben solicitar otro periodo, llamar `/analyses` ni recalcular métricas. El PDF es estático.

## Rutas y permisos

| Operación | Ruta | Roles |
|---|---|---|
| Crear | `POST /api/v1/reports` | Owner, Admin, Analyst |
| Listar | `GET /api/v1/clients/{clientId}/reports?page=1&pageSize=20` | Todos dentro de su alcance |
| Detalle | `GET /api/v1/reports/{reportId}` | Todos dentro de su alcance |
| PDF | `GET /api/v1/reports/{reportId}/pdf` | Todos dentro de su alcance |
| Eliminar | `DELETE /api/v1/reports/{reportId}` | Owner, Admin |

`ClientViewer` solo puede leer reportes de clientes activos que continúen asignados. El backend responde 404 si el recurso está fuera de su alcance.

## Crear

```ts
type CreateReportRequest = {
  title: string;
  adAccountId: string;
  since: string;
  until: string;
  comparison: "PreviousPeriod" | "PreviousMonth" | "PreviousYear" | "Custom" | null;
  comparisonSince: string | null;
  comparisonUntil: string | null;
  campaignIds: string[] | null;
  selectedMetrics: string[] | null;
};
```

`Custom` exige ambas fechas comparativas. `campaignIds: null` incluye todas las campañas autorizadas; `[]` analiza solo la cuenta. La respuesta es `201`, incluye `Location` y devuelve `ReportData`.

## Listado

`page` comienza en 1 y `pageSize` admite 1 a 100.

```ts
type ReportListItem = {
  id: string;
  title: string;
  clientId: string;
  adAccountId: string;
  since: string;
  until: string;
  createdAtUtc: string;
  schemaVersion: number;
  snapshotHash: string;
};
```

## Detalle

```ts
type ReportData = {
  reportId: string;
  schemaVersion: number;
  title: string;
  clientId: string;
  adAccountId: string;
  createdByUserId: string;
  createdAtUtc: string;
  snapshotHash: string;
  analysis: AnalysisResult;
};
```

`AnalysisResult` es el contrato vigente de Fases 9 y 10 e incluye cuenta, campañas, AdSets, Ads, métricas, cobertura, suficiencia, comparaciones, benchmarks, secciones no disponibles, insights y recomendaciones.

## Descarga PDF

Solicita `GET /api/v1/reports/{reportId}/pdf` con Bearer y valida `response.ok`. Convierte la respuesta a `blob`, crea `URL.createObjectURL`, descarga mediante un enlace temporal y ejecuta `URL.revokeObjectURL` en `finally`.

La respuesta usa `application/pdf` y `Content-Disposition: attachment`.

## Eliminación

Solicita confirmación visual y llama `DELETE /api/v1/reports/{reportId}`. Tras `204`, elimina el elemento de la lista y cierra el detalle si coincide. Solo Owner/Admin deben ver la acción.

## Estados y errores

- `400`: muestra el detalle o los errores por campo.
- `401`: limpia la sesión y navega al login.
- `403`: muestra permisos insuficientes.
- `404`: limpia detalle/caché privada y refresca la lista autorizada.
- `409`: muestra el detalle del conflicto.
- `500`: muestra mensaje genérico y reintento seguro.

Un array vacío de insights o recomendaciones significa que no hubo evidencia suficiente. No es un error. Muestra `null` como `—` o “Sin dato”. Presenta `snapshotHash` solo como información de integridad; no es una credencial.

## Criterios de aceptación

- Crear desde la misma selección de AnalysisEngine más `title`.
- Mostrar el reporte desde `GET /reports/{id}` sin llamar `/analyses`.
- Descargar un PDF autenticado y liberar la URL temporal.
- Paginar por cliente y actualizar la lista después de crear/eliminar.
- Respetar la matriz de roles y limpiar datos ante revocación.
- No convertir ausencia en cero ni recalcular resultados del backend.

Fuente completa: `../../FRONTEND_HANDOFF.md`.
