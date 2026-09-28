# Plan vigente de AnalitiAds

> Traspaso operativo para el próximo agente backend: `BACKEND_AGENT_HANDOFF.md`.

> Fuente de verdad consolidada: leer `CODEX_HANDOFF.md` antes de continuar. Fases 0–12 están completas. La migración `20260915181708_AddReportShareLinks` está aplicada en Neon y existen diez migraciones registradas.

Actualización de integración y rendimiento: el frontend validó Fase 9 contra los cinco roles y los datos demo, sin cambios de contrato requeridos. La primera ejecución de nueve campañas tardó aproximadamente 24 segundos. El backend eliminó consultas repetidas por entidad y ahora carga jerarquía y snapshots agrupados por cuenta y rango. Falta repetir la medición real con las credenciales del entorno. Ver `ANALYSISENGINE_BACKEND_HANDOFF.md`.

## Proyecto autorizado

Todo el producto se construirá desde cero en la solución existente:

```text
AnaliticAsd.sln
└── AnaliticAsd/
    └── AnaliticAsd.csproj
```

No se crearán soluciones ni proyectos paralelos sin autorización expresa.

## Estado real

Actualización final de Fase 8 (prevalece sobre referencias históricas posteriores): comparaciones temporales y benchmarks internos completados y verificados el 11 de septiembre de 2026. Existen comparaciones seguras para Account, Campaign, AdSet y Ad; benchmarks de campañas por mediana de otras campañas del mismo objetivo; y tools MCP de lectura equivalentes. Compilación: 0 errores y 0 advertencias. Pruebas: 72/72. No fue necesaria una migración; las ocho migraciones existentes siguen aplicadas. Próxima fase: Fase 9, `AnalysisEngine`, no autorizada todavía.

Actualización final de Fase 7B (prevalece sobre referencias históricas posteriores): MCP de lectura completado y verificado el 11 de septiembre de 2026. Usa `ModelContextProtocol.AspNetCore` 2.2.0, Streamable HTTP stateless en `/mcp`, scope `analitiads:read`, credenciales de audiencia MCP separada, consentimiento explícito, revocación y auditoría. La migración `20260911195705_AddReadOnlyMcpConnections` está aplicada en Neon; hay ocho migraciones aplicadas. Compilación: 0 errores y 0 advertencias. Pruebas: 69/69. La siguiente fase es comparaciones y no está autorizada todavía.

La verificación MCP cubre cliente oficial C#, descubrimiento/listado/invocación, anónimo, audiencia incorrecta, scope insuficiente, clave global, revocación de conexión, revocación de membresía y cruces de agencia/cliente. No existen herramientas de escritura, análisis, recomendaciones, reportes ni PDF.

- Fase 0: **completada el 3 de septiembre de 2026**.
- Fase 1: **completada el 3 de septiembre de 2026**.
- Fase 2: **completada el 3 de septiembre de 2026**.
- Fase 3: **completada el 3 de septiembre de 2026**.
- Fase 4: **completada el 4 de septiembre de 2026**.
- Fase 5: **completada el 4 de septiembre de 2026**.
- Fase 6: **completada el 4 de septiembre de 2026**.
- Fase 6A: **acceso externo por cliente implementado y verificado el 11 de septiembre de 2026**.
- Fase 7: **consolidación segura de métricas, consultas de rango y selector implementados y verificados el 11 de septiembre de 2026**.
- Próxima fase: **Fase 7B — MCP de lectura con autorización delegada**, requisito obligatorio de conectabilidad con IA compatible. Todavía no implementado.
- Código actual: Web API en .NET 10 con JWT, multi-tenancy, clientes, cuentas publicitarias, Meta OAuth, jerarquía publicitaria, sincronización, insights normalizados, snapshots diarios, EF Core/PostgreSQL, OpenAPI y pruebas.

Fase 7 corrige la limitación histórica de Fase 6: los observados ahora preservan la ausencia como `null` y distinguen el cero explícito. Las filas previas se marcan `LegacyZeroNormalized`, por lo que no se presentan como totales verificados hasta re-sincronizarlas. Los resúmenes de rango nunca suman `reach` ni calculan `frequency`; recomputan los demás ratios desde numeradores y denominadores seguros, sin promediar ratios diarios. Las siete migraciones, incluida `20260911182820_AddMetricRangeConsolidation`, se aplicaron y verificaron en la instancia Neon autorizada el 11 de septiembre de 2026. OAuth/Insights con una app Meta real siguen sin verificarse.

## Fases

1. **Fase 0 — Auditoría y planificación:** completada. Resultado en `AUDITORIA_FASE_0.md`.
2. **Fase 1 — Base interna del proyecto:** completada. Dependencias seguras, manejo de errores, OpenAPI, endpoint de estado y pruebas.
3. **Fase 2 — Clientes y cuentas publicitarias:** completada. Dominio, persistencia EF Core/PostgreSQL, migración inicial y CRUD.
4. **Fase 3 — Autenticación y multi-tenancy:** completada. `Agency`, `User`, `Membership`, JWT, roles y aislamiento.
5. **Fase 4 — Meta OAuth:** completada. OAuth backend, token protegido, cuentas disponibles y asociación con clientes.
6. **Fase 5 — Campaign, AdSet, Ad y sincronización:** completada. Jerarquía persistente, sincronización paginada, estados y consultas tenant-safe.
7. **Fase 6 — Métricas e histórico:** completada. Insights diarios para cuenta, Campaign, AdSet y Ad; observados separados de derivados; persistencia histórica y consultas tenant-safe.
8. **Fase 6A — Acceso externo por cliente:** completada. `ClientViewer`, asignaciones persistentes, invitaciones de 48 horas con hash/consumo único, revocación, aislamiento de toda la jerarquía y snapshots. No incluye envío de correo ni recuperación de contraseña.
9. **Fase 7 — Consolidación de métricas, consultas y selector de campañas:** completada. La ingesta preserva ausencia frente a cero, valida rango, jerarquía y paginación; snapshots antiguos quedan identificados como no verificables. Expone resúmenes seguros por cuenta, Campaign, AdSet y Ad, y selector de Campaign por actividad/gasto sin exponer ni aceptar `agencyId`.
10. **Fase 7B — MCP de lectura:** pendiente, obligatorio. Adaptador dentro del proyecto existente, servicios compartidos, identidad delegada por usuario/agencia/cliente, consentimiento, revocación, auditoría sin secretos y pruebas reales de aislamiento. No usar una clave global de administrador.
11. **Fase 8 — Comparaciones y benchmarks.**
12. **Fase 9 — AnalysisEngine:** completada. Análisis determinista bajo demanda para cuenta, Campaign, AdSet y Ad; selección por UUID, métricas seleccionadas, comparaciones, benchmarks, cobertura y suficiencia estructurada. Sin persistencia, frases, scoring ni recomendaciones.
13. **Fase 10 — Reglas, insights y recomendaciones:** completada. Añade `insights` y `recommendations` al AnalysisEngine, con evidencia, severidad, confianza, suficiencia y umbrales configurables. Sin IA, scoring ni llamadas nuevas a Meta.
14. **Fase 11 — ReportData, reporte web y PDF:** completada. Snapshot JSON versionado con hash, rutas tenant-safe, PDF generado desde el mismo snapshot y pruebas de inmutabilidad/revocación. Migración aplicada en Neon.
15. **Fase 12 — Enlaces compartibles:** completada. Tokens de 256 bits hasheados, expiración de 1–30 días, revocación, auditoría, acceso/PDF público por body y rate limiting. Migración aplicada en Neon.
16. **Fase 13 — Frontend Next.js:** integración incremental en paralelo por el agente frontend. El usuario informa integración hasta Fase 6; no se auditó ni modificó en esta etapa.
17. **Fase 14 — IA opcional:** interpretación asistida futura, separada de la conectabilidad MCP obligatoria de Fase 7B.

## Próxima tarea

Repetir la medición real de AnalysisEngine con nueve campañas y registrar el tiempo posterior a la carga agrupada. Después, acordar el alcance de Fase 11 —`ReportData`, reporte web y PDF— antes de implementarla.

## Evidencia y límites de la Fase 7

- `dotnet build AnaliticAsd.sln --no-restore`: aprobado, 0 advertencias y 0 errores.
- `dotnet test AnaliticAsd.sln --no-restore`: aprobado, 63/63, sin pruebas omitidas.
- Las pruebas cubren ausencia frente a cero explícito, valores derivados nulos, rango parcial, moneda mixta, snapshots antiguos, paginación repetida, re-sincronización, jerarquía Meta inconsistente, roles, revocación y alcance `ClientViewer` en las rutas nuevas.
- Migración `20260911182820_AddMetricRangeConsolidation`: hace anulables los siete observados, agrega `observed_data_quality` con valor histórico `LegacyZeroNormalized` y quedó aplicada en Neon. `dotnet ef migrations has-pending-model-changes` no informó diferencias y `dotnet ef migrations list` confirmó las siete migraciones.
- CORS y retorno frontend local siguen restringidos a `http://localhost:3001`. No se modificó `frontend/` ni se dejó un servidor backend persistente.
- Los tokens, App Secret y cadena de Neon permanecen fuera de código, logs y contrato público; la cadena está solo en User Secrets.
- Meta OAuth e Insights se probaron con HTTP simulado y contratos oficiales; una conexión contra credenciales Meta reales sigue pendiente. No existe MCP todavía.
