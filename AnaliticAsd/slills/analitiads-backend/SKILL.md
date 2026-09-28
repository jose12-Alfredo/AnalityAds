---
name: analitiads-backend
description: Construir AnalitiAds exclusivamente dentro de la solución AnaliticAsd.sln y su único proyecto AnaliticAsd/AnaliticAsd.csproj. Usar para planificación, implementación y revisión del backend; nunca crear una solución o proyecto paralelo.
---

# AnalitiAds Backend

> Entrada operativa para un agente backend nuevo: leer primero `../BACKEND_AGENT_HANDOFF.md`. Fases 0–12 están completas. `20260915181708_AddReportShareLinks` está aplicada en Neon y existen diez migraciones. La siguiente fase funcional debe acordarse con el usuario.

## Actualización final de Fase 11 — 15 de septiembre de 2026

Fase 11 fue autorizada y completada. `POST /api/v1/reports` congela el resultado de AnalysisEngine en ReportData JSON versionado y protegido con hash SHA-256. Las rutas permiten listar por cliente, consultar el snapshot, descargar un PDF creado únicamente desde ese snapshot y eliminar con rol Owner/Admin. El acceso se revalida y ClientViewer pierde lectura tras revocación. Compilación limpia, 79/79 pruebas, PDF renderizado e inspeccionado visualmente, modelo EF sin cambios pendientes y migración `20260915171245_AddImmutableReports` aplicada en Neon. Leer `../PHASE11_BACKEND_HANDOFF.md`.

## Actualización final de Fase 10 — 15 de septiembre de 2026

Fase 10 fue autorizada y completada. `POST /api/v1/analyses` conserva el request y los campos de Fase 9, y añade `insights` y `recommendations`. Las reglas son deterministas, usan únicamente evidencia estructurada de AnalysisEngine, exigen suficiencia y disponibilidad, incorporan evidencia numérica y nunca llaman a Meta o IA. Los umbrales están en `AnalysisRules` dentro de configuración. No incluye scoring, fatiga creativa sin series compatibles, breakdowns, atributos creativos, reportes ni PDF. Compilación limpia y 77/77 pruebas aprobadas; no hubo migración. Leer `../PHASE10_BACKEND_HANDOFF.md`.

## Fuente de verdad consolidada — 14 de septiembre de 2026

Actualización vigente: Fase 9 `AnalysisEngine` está completa e integrada con el frontend. Leer también `../ANALYSISENGINE_BACKEND_HANDOFF.md`. Su endpoint conserva el contrato `POST /api/v1/analyses`. La implementación agrupó las cargas de jerarquía y snapshots para eliminar consultas repetidas por entidad después de que la matriz frontend midiera aproximadamente 24 segundos con nueve campañas. La próxima fase es Fase 10 y requiere autorización expresa.

Antes de continuar, leer `../CODEX_HANDOFF.md` y `../ANALYSISENGINE_BACKEND_HANDOFF.md`. Allí está el estado verificable, rutas, seguridad, migraciones, pruebas, limitaciones y archivos clave. Fases 0–9 están completas; la próxima fase es Fase 10 —reglas, insights y recomendaciones— y requiere autorización expresa. Esta sección prevalece sobre toda referencia histórica inferior que describa Fase 7B, Fase 8 o Fase 9 como pendiente.

Actualización final de Fase 8 del 11 de septiembre de 2026: comparaciones temporales y benchmarks internos completados. Disponibles por REST para Account, Campaign, AdSet y Ad, y por las tools MCP `get_metrics_comparison` y `get_campaign_benchmarks`. 72/72 pruebas aprobadas; no hubo migración nueva y permanecen ocho aplicadas en Neon. La siguiente fase es Fase 9 `AnalysisEngine` y requiere autorización expresa. Esta actualización prevalece sobre referencias históricas inferiores.

Actualización final del 11 de septiembre de 2026: Fase 7B completada y verificada. MCP de lectura está en `/mcp`, con scope `analitiads:read`, credenciales revocables y aislamiento por usuario/agencia/cliente. Migración `20260911195705_AddReadOnlyMcpConnections` aplicada en Neon; ocho migraciones y 69/69 pruebas aprobadas. La siguiente fase es comparaciones y requiere autorización expresa. Esta actualización prevalece sobre las referencias históricas inferiores que describen Fase 7B como pendiente.

## Continuacion obligatoria para otro chat de Codex

El estado vigente esta en `../PLAN.md`, `../FRONTEND_HANDOFF.md` y `../traspaso.md`. Las fases 0 a 7 estan completas; la siguiente fase autorizada es unicamente Fase 7B. El texto historico de estado que aparezca mas abajo no prevalece sobre esos documentos.

Antes de modificar codigo, leer completamente y en este orden: `SKILL.md`, `../FRONTEND_HANDOFF.md`, `../PLAN.md`, `../instruccion.md`, `../traspaso.md` y `FUSION_DASHBOARD_MCP.md`.

Fase 7B consiste en implementar MCP de solo lectura dentro del mismo proyecto ASP.NET Core, con autorizacion delegada por usuario. Consultar primero la especificacion oficial vigente de MCP y el SDK oficial C#; no inventar transporte, endpoints, scopes ni contratos.

Trabajar solo en `AnaliticAsd.sln` y `AnaliticAsd/`. No modificar `frontend/`, el puerto 3000 ni soluciones paralelas. Reutilizar servicios, repositorios y `AuthorizedData`; resolver siempre usuario, membresia, agencia y asignaciones `ClientViewer`. Nunca aceptar `agencyId` libre, usar una clave global de administrador, hacer token passthrough de Meta o exponer secretos, tokens, JWT o cadenas de Neon.

La primera entrega MCP solo puede leer clientes, cuentas, Campaign, AdSet, Ad, series diarias y resumenes de Fase 7. MCP debe ser opcional para la web. Implementar consentimiento, audiencia, scopes, credenciales revocables y auditoria sin secretos. No implementar escritura en Meta, administracion de usuarios, borrado/regeneracion de datos, comparaciones, analisis, recomendaciones, reportes/PDF ni IA interpretativa.

Agregar pruebas reales sin credenciales Meta para inicializacion, descubrimiento, invocacion autorizada, anonimato, rol insuficiente, audiencia incorrecta, revocacion y cruces de agencia/cliente. Compilar la solucion y ejecutar todas las pruebas. Actualizar la documentacion solo si Fase 7B queda completamente verificada; si falta documentacion oficial, detenerse y explicar el bloqueo.

Construye AnalitiAds desde cero como una plataforma multi-tenant de análisis y reporting de Meta Ads que funcione sin IA externa.

El objetivo incluye un dashboard web independiente para clientes y conexión opcional de usuarios a Claude u otras IA compatibles. La capacidad de conexión mediante MCP es un requisito obligatorio del producto, no una dependencia del dashboard.

## Proyecto único y obligatorio

Trabaja exclusivamente en:

```text
C:\Users\jose1\RiderProjects\AnaliticAsd\AnaliticAsd.sln
└── AnaliticAsd\AnaliticAsd.csproj
```

- Todo código nuevo debe vivir dentro de `AnaliticAsd/`.
- No modifiques `frontend/`. El origen local autorizado es `http://localhost:3001`; el puerto 3000 pertenece a otro proyecto.
- Mantén una sola solución y un solo proyecto hasta que el usuario autorice expresamente otra estructura.
- La reconstrucción paralela `AnalitiAds.sln`, `src/AnalitiAds.*` y `tests/AnalitiAds.Tests` fue eliminada el 16 de septiembre de 2026; no debe recrearse sin autorización explícita.
- No interpretes esos archivos paralelos como estado completado del proyecto principal.
- No borres archivos o soluciones paralelas sin autorización explícita.

## Fuente de verdad

Antes de implementar una fase:

1. Lee completamente `../FRONTEND_HANDOFF.md`, `../PLAN.md` y `../instruccion.md`, en ese orden.
2. Lee `../traspaso.md` y `../instrucciondesistema.md` cuando la tarea afecte producto, métricas, Meta, reporting o seguridad.
3. Para la fusión del dashboard o conexión con IA, lee `FUSION_DASHBOARD_MCP.md`. El handoff de Claude y `assets/claude-dashboard/README.md` son referencias; no reemplazan el contrato de la API ni documentación oficial de Meta.
4. Inspecciona `AnaliticAsd.sln`, `AnaliticAsd/AnaliticAsd.csproj`, el código relacionado y Git. Limita el cambio a la fase activa.

Si un documento antiguo contradice esta ubicación o afirma que existen fases terminadas en otra solución, esta skill y `../PLAN.md` prevalecen.

## Estado vigente

Consulta `../PLAN.md` para el estado verificable. Existen fases 0 a 6 y la ampliación 6A de acceso externo por cliente. La siguiente etapa es consolidar métricas/consultas y el selector por actividad; luego MCP de lectura con autorización delegada. No hay servidor MCP ni análisis, reportes o PDF implementados. Las pruebas locales no certifican despliegue PostgreSQL ni conexión real a Meta.

## Reglas esenciales

- Construye incrementalmente dentro del proyecto único; organiza por carpetas o módulos internos sin crear proyectos adicionales.
- No conectes Meta antes de implementar autenticación y aislamiento multi-tenant.
- Resuelve el tenant desde la identidad autenticada, nunca desde un `AgencyId` libre enviado por el frontend.
- `Viewer` es interno de agencia; `ClientViewer` solo puede leer clientes activos asignados y sus cuentas/jerarquía/métricas. Aplica el mismo aislamiento a REST, futuros adaptadores MCP y reportes. Revalida permisos; no caches asignaciones dentro del JWT.
- Las invitaciones externas se crean por Owner/Admin, caducan, son revocables y de un solo uso; únicamente su hash se persiste. No conviertas usuarios internos en externos ni cambies su contraseña al aceptar otra invitación.
- Consulta documentación oficial vigente de Meta antes de definir endpoints, campos, permisos, niveles, breakdowns, atribución o versiones. Los formatos de una tool MCP de terceros no son el contrato de Graph API.
- Relaciona cuentas, campañas, conjuntos y anuncios mediante IDs de Meta, no mediante nombres.
- Usa `decimal` para dinero y métricas fraccionarias.
- Guarda moneda, zona horaria y timestamps técnicos en UTC.
- Separa datos observados de Meta de métricas calculadas.
- Distingue datos exactos, derivados y estimados.
- No sumes `reach` entre periodos como personas únicas.
- No inventes datos ni presentes correlación como causalidad.
- Usar IA es opcional; mantener conectabilidad MCP es obligatorio. El dashboard debe funcionar sin Claude Desktop. Ninguna IA puede alterar métricas oficiales, saltarse permisos ni recibir credenciales de Meta.
- El reporte web y el PDF deben consumir el mismo snapshot inmutable `ReportData`.
- No coloques secretos de servidor, tokens Meta o contraseñas en frontend, archivos versionados o logs. JWT de usuario y código de invitación son credenciales del flujo: transporte autorizado, mínimo tiempo en memoria, nunca URL, telemetría ni almacenamiento público.

## Flujo de trabajo

Para cada fase:

1. Revisa el estado real y explica el alcance.
2. Modifica solo `AnaliticAsd/` y los documentos locales de `AnaliticAsd/slills/`.
3. Conserva cambios del usuario y evita adelantar fases.
4. Compila `AnaliticAsd.sln` y ejecuta las pruebas disponibles.
5. Revisa el diff e informa archivos modificados, pruebas reales, limitaciones y siguiente fase.

No afirmes que PostgreSQL, autenticación, Meta, PDF o frontend funcionan sin una prueba real.
