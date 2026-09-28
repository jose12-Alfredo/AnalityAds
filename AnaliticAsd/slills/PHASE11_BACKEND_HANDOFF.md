# Fase 11 — ReportData, reporte web y PDF

Fecha de cierre: 15 de septiembre de 2026.

## Resultado

Se implementó un reporte persistente e inmutable. Al crear un reporte, el backend ejecuta AnalysisEngine y congela toda su respuesta, incluidos métricas, cobertura, suficiencia, comparaciones, benchmarks, insights y recomendaciones. Consultar el reporte o descargar el PDF nunca vuelve a consultar Meta ni las métricas actuales.

El snapshot usa esquema versión 1 y un hash SHA-256 calculado sobre su forma canónica con `snapshotHash` vacío. Cada lectura verifica el hash y que los identificadores críticos coincidan con la fila persistida.

## Rutas

- `POST /api/v1/reports`: Owner, Admin y Analyst; responde 201 y ReportData.
- `GET /api/v1/clients/{clientId}/reports?page=1&pageSize=20`: todos los roles dentro de su alcance; tamaño máximo 100.
- `GET /api/v1/reports/{reportId}`: ReportData completo.
- `GET /api/v1/reports/{reportId}/pdf`: PDF adjunto creado desde el mismo snapshot.
- `DELETE /api/v1/reports/{reportId}`: Owner y Admin; responde 204.

No se acepta `agencyId` ni autoridad libre en los cuerpos. ClientViewer solo accede a reportes de clientes activos asignados y pierde acceso en la siguiente petición después de una revocación.

## Persistencia

La entidad `Report` guarda agencia, cliente, cuenta, creador, título, periodo, fecha, versión, JSON y hash. Las relaciones usan borrado restringido para evitar eliminar accidentalmente evidencia histórica al borrar usuario, cliente o cuenta.

Migración `20260915171245_AddImmutableReports` generada y aplicada en Neon después de la autorización explícita del usuario. `dotnet ef migrations has-pending-model-changes` confirmó que no hay diferencias y `dotnet ef migrations list` confirmó nueve migraciones aplicadas.

## PDF

El renderizador no consulta repositorios y recibe únicamente `ReportDataModel`. Genera páginas A4 con portada, resumen, calidad, insights, recomendaciones, campañas, metodología, hash y numeración. Soporta paginación automática y caracteres españoles mediante WinAnsi. El PDF de muestra fue validado estructuralmente, renderizado a PNG e inspeccionado sin texto cortado, superposición ni problemas de márgenes.

## Verificación

- `dotnet build AnaliticAsd.sln --no-restore`: 0 errores y 0 advertencias.
- `dotnet test AnaliticAsd.sln --no-restore`: 79/79 aprobadas.
- Modelo EF: sin cambios pendientes después de la migración.
- Integración: creación, Location, listado, PDF, snapshot estable después de resincronizar, Viewer, ClientViewer, revocación, cruce de agencia y eliminación.
- PDF extenso: prueba de árbol multipágina.
- Muestra: `output/pdf/analitiads-phase11-sample.pdf`.

## Archivos principales

- `Domain/Reports/Report.cs`
- `Application/Reports/ReportContracts.cs`
- `Application/Reports/ReportService.cs`
- `Contracts/Reports/ReportContracts.cs`
- `Controllers/ReportsController.cs`
- `Infrastructure/Persistence/Configurations/ReportConfiguration.cs`
- `Infrastructure/Persistence/Repositories/ReportRepository.cs`
- `Infrastructure/Reports/SimpleReportPdfRenderer.cs`
- `Infrastructure/Persistence/Migrations/*AddImmutableReports*`
- `Tests/Reports/ReportPdfRendererTests.cs`
- `Tests/Api/ClientAccessApiTests.cs`

## Próximos pasos

1. Integrar las rutas en frontend según `FRONTEND_HANDOFF.md`.
2. Ejecutar una prueba HTTP de reportes contra el entorno Neon desde el frontend autorizado.
3. Definir Fase 12: enlaces compartibles con expiración, revocación y acceso sin sesión cuidadosamente limitado.
