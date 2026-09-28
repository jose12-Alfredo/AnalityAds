# Fase 12 — enlaces compartibles

Fecha de cierre: 15 de septiembre de 2026.

## Resultado

Owner y Admin pueden crear, listar y revocar enlaces de un reporte autorizado. Cada enlace usa un token aleatorio de 256 bits, guarda solo SHA-256, expira entre 1 y 30 días y utiliza concurrencia optimista para revocación.

El acceso público es de solo lectura y entrega exclusivamente el snapshot inmutable de Fase 11 o su PDF. El token no forma parte de una ruta API ni query: la URL frontend usa `#token` y lo intercambia mediante body. Los accesos válidos se auditan como `View` o `Pdf` sin argumentos ni token.

## Rutas

- `GET /api/v1/reports/{reportId}/share-links`: Owner/Admin.
- `POST /api/v1/reports/{reportId}/share-links`: Owner/Admin, `{ expirationDays: 1..30 }`.
- `DELETE /api/v1/reports/{reportId}/share-links/{shareLinkId}`: Owner/Admin.
- `POST /api/v1/shared-reports/access`: anónimo, `{ accessToken }`.
- `POST /api/v1/shared-reports/pdf`: anónimo, `{ accessToken }`.

Los endpoints públicos comparten límite de 60 solicitudes por minuto por IP. Token inválido, expirado o revocado, reporte ausente, cliente inactivo y agencia inactiva devuelven el mismo 404.

## Persistencia

- `report_share_links`: reporte, agencia, cliente, creador, hash, fechas, revocación y versión de concurrencia.
- `report_share_audit_events`: enlace, operación y timestamp; nunca token o argumentos.
- Eliminando un reporte se eliminan sus enlaces y auditoría.

Migración `20260915181708_AddReportShareLinks` aplicada en Neon después de la autorización explícita del usuario. El modelo no tiene cambios pendientes y el historial confirma diez migraciones aplicadas.

## Verificación

- Compilación: 0 errores y 0 advertencias.
- Pruebas: 81/81.
- Cobertura nueva: campos extra, expiración inválida, hash, exposición única, listado sin token, acceso, PDF, auditoría, roles, cruce de agencia, revocación, expiración, token desconocido y 429.
- La comprobación de integridad SHA-256 del ReportData se reutiliza también en el acceso público.

## Frontend

El contrato completo está al inicio de `FRONTEND_HANDOFF.md` y en `analitiads-frontend-reports/references/report-api.md`. Los gráficos web pueden ser interactivos únicamente sobre el snapshot; el PDF permanece estático.
