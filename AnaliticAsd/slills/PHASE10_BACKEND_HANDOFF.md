# Fase 10 — reglas, insights y recomendaciones

Fecha de cierre: 15 de septiembre de 2026.

## Resultado

Fase 10 amplía `POST /api/v1/analyses` sin cambiar su solicitud ni retirar campos. La respuesta añade `insights` y `recommendations`. El cálculo ocurre bajo demanda, no persiste resultados, no consulta Meta y no usa IA.

Cada insight contiene `ruleId`, `level`, `entityIds`, `severity`, `confidence`, `sufficiency`, `message` y evidencia numérica. Cada recomendación contiene la misma regla y entidades, prioridad, mensaje y acciones controladas. La salida es determinista para la misma evidencia y configuración.

## Reglas disponibles

- `budget_concentration`: gasto de las N campañas principales dividido por el gasto total.
- `standout_campaign`: al menos dos señales favorables contra campañas comparables del mismo objetivo.
- `underperforming_campaign`: al menos dos señales desfavorables contra campañas comparables del mismo objetivo.
- `possible_post_click_issue`: tráfico competitivo junto con costo de resultado desfavorable.
- `weak_ad_response`: CPM comparable y CTR inferior al benchmark.
- `more_expensive_delivery`: CPM y CPC aumentan mientras CTR permanece relativamente estable.

Las reglas de campaña requieren suficiencia `Sufficient`. Las basadas en benchmarks exigen métricas `Available` y el mínimo de comparables configurado. Las basadas en comparación exigen cambios porcentuales disponibles. Si falta evidencia, la regla no se emite.

No se añadió scoring porque la autorización de Fase 10 no lo incluyó específicamente. Tampoco se emite fatiga creativa: la frecuencia de rango no está disponible de forma segura y el producto exige varios periodos. Audiencias, placements y atributos creativos permanecen en `unavailableSections`.

## Configuración

Sección `AnalysisRules`:

- `BudgetConcentrationTopCampaigns`: 2.
- `BudgetConcentrationPercentage`: 40.
- `MaterialDifferencePercentage`: 20.
- `StableCtrPercentage`: 10.
- `MinimumComparableCampaigns`: 2.

Los valores se validan antes de evaluar reglas. Cambiarlos modifica conclusiones, por lo que deben tratarse como configuración de producto versionada y revisada.

## Seguridad y compatibilidad

La ruta mantiene JWT Bearer y reutiliza todo el aislamiento de AnalysisEngine. Owner, Admin, Analyst y Viewer operan dentro de su agencia; ClientViewer conserva acceso exclusivo a clientes activos asignados. No se aceptan `agencyId`, reglas ni thresholds desde el request.

No se agregaron secretos, tablas ni migraciones. El frontend debe consumir los resultados y no recalcularlos.

## Archivos principales

- `Application/Analysis/AnalysisRulesEngine.cs`
- `Application/Analysis/AnalysisContracts.cs`
- `Application/Analysis/AnalysisService.cs`
- `Program.cs`
- `appsettings.json`
- `Tests/Analysis/AnalysisRulesEngineTests.cs`
- `slills/FRONTEND_HANDOFF.md`

## Verificación

- `dotnet build AnaliticAsd.sln --no-restore`: aprobado, 0 errores y 0 advertencias.
- `dotnet test AnaliticAsd.sln --no-restore`: aprobado, 77/77.
- Pruebas nuevas: determinismo, concentración, señales múltiples por objetivo, supresión por evidencia parcial/no disponible y costo de entrega con CTR estable.
- Sin migración EF Core.

## Pendientes

- Repetir la medición real de AnalysisEngine con nueve campañas después de la optimización de cargas.
- Fase 11: definir `ReportData` inmutable y el contrato común para reporte web y PDF.
- Obtener breakdowns y atributos creativos reales antes de habilitar reglas que dependan de ellos.
- Endurecimiento posterior de identidad: correo real, recuperación/cambio de contraseña, sesiones revocables y miembros internos.
