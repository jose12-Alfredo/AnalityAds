# Traspaso vigente de AnalitiAds

> Para un agente backend nuevo, comenzar por `BACKEND_AGENT_HANDOFF.md`.

> Traspaso consolidado para un nuevo chat: `CODEX_HANDOFF.md` y `ANALYSISENGINE_BACKEND_HANDOFF.md`. Fases 0–9 están completas y verificadas; Fase 10 —reglas, insights y recomendaciones— es la próxima fase y no está autorizada. Estos documentos prevalecen sobre referencias históricas inferiores.

Actualización de integración del 15 de septiembre de 2026: el frontend consume exclusivamente `POST /api/v1/analyses` desde su cliente API central, con JWT Bearer y JSON camelCase. No envía `agencyId`, secretos Meta o tokens MCP y no recalcula evidencia. Validó por HTTP los cinco roles autorizados, selección de campañas, comparación personalizada, suficiencia, ausencia de snapshots, datos legacy, moneda mixta, benchmarks y aislamiento entre agencias y clientes. El contrato actual es compatible y no requiere rutas nuevas ni cambios de serialización, JWT o CORS. La medición inicial de todas las campañas fue de aproximadamente 24 segundos; el backend ya agrupó las cargas y queda pendiente repetir esa medición.

Actualización final de Fase 8 del 11 de septiembre de 2026 (prevalece sobre texto histórico inferior): comparaciones temporales y benchmarks internos implementados. Cuatro rutas `/metrics/comparison` aceptan `PreviousPeriod`, `PreviousMonth`, `PreviousYear` o `Custom`; campañas exponen `/ad-accounts/{id}/campaigns/benchmarks`. MCP agrega `get_metrics_comparison` y `get_campaign_benchmarks`. Los cálculos bloquean ausencia, legacy, moneda incompatible y divisiones contra baseline cero. 72/72 pruebas aprobadas, sin migración nueva. Próxima fase no autorizada: Fase 9 `AnalysisEngine`.

Actualización final del 11 de septiembre de 2026 (prevalece sobre el texto histórico inferior): Fase 7B completada. MCP de solo lectura está disponible en `/mcp` con Streamable HTTP stateless, scope `analitiads:read`, consentimiento, audiencia separada, credenciales revocables y auditoría sin argumentos, secretos ni tokens. Revalida usuario, membresía, agencia, rol y acceso `ClientViewer` en cada solicitud. `20260911195705_AddReadOnlyMcpConnections` está aplicada en Neon; ocho migraciones aplicadas y 69/69 pruebas aprobadas. Próxima fase no autorizada: comparaciones.

Fecha de reinicio: 3 de septiembre de 2026.

Actualización vigente del 11 de septiembre de 2026: Fases 6A y 7 implementadas. La Fase 7 preserva ausencia frente a cero en Insights, identifica snapshots antiguos como `LegacyZeroNormalized`, expone resúmenes seguros de rango y selector de Campaign. 63 pruebas aprobadas; CORS local y retorno frontend en `http://localhost:3001`. Las siete migraciones, incluida `AddMetricRangeConsolidation`, se aplicaron y verificaron en la base Neon autorizada. La conexión permanece solo en User Secrets; el backend admite la URI estándar de Neon sin registrar sus credenciales. Requisito de conectividad MCP incorporado a `analitiads-backend/FUSION_DASHBOARD_MCP.md`, todavía sin servidor MCP. Para estado y contratos completos prevalecen `PLAN.md` y `FRONTEND_HANDOFF.md`, especialmente sección 20. Las secciones de resultados anteriores que siguen abajo son históricas.

## Decisión del usuario

AnalitiAds se reinicia desde cero y se desarrollará exclusivamente en:

```text
C:\Users\jose1\RiderProjects\AnaliticAsd\AnaliticAsd.sln
└── AnaliticAsd\AnaliticAsd.csproj
```

La solución `AnalitiAds.sln`, las carpetas raíz `src/` y `tests/` de la reconstrucción paralela fueron eliminadas el 16 de septiembre de 2026 por autorización explícita del usuario. No representan el estado del proyecto principal.

## Estado real del proyecto principal

- Fase 0: completada y documentada en `AUDITORIA_FASE_0.md`.
- Fase 1: completada y verificada.
- Fase 2: completada y verificada.
- Fase 3: completada y verificada.
- Fase 4: completada y verificada sin credenciales reales de Meta.
- Fase 5: completada y verificada con simulación HTTP de Meta.
- Fase 6: completada y verificada con simulación HTTP de Meta y persistencia relacional de prueba.
- Fase 6A: acceso externo por cliente implementado y verificado con JWT real y SQLite.
- Fase 7: consolidación segura de métricas, consultas y selector implementados y verificados.
- Próxima fase pendiente: Fase 7B, MCP de lectura con autorización delegada.
- Stack detectado: ASP.NET Core Web API, C# y .NET 10.
- Solución abierta en Rider: `AnaliticAsd.sln`.
- Proyecto único: `AnaliticAsd/AnaliticAsd.csproj`.
- Código actual: base Web API con `Program.cs`, manejo global de errores, endpoint de estado y OpenAPI.
- Endpoint disponible: `GET /api/system/status`.
- El ejemplo `WeatherForecast` fue retirado.
- PostgreSQL y EF Core: implementados; las siete migraciones fueron aplicadas y comprobadas contra la instancia Neon autorizada. Las notas más abajo sobre una base local no aplicada son históricas.
- Autenticación y multi-tenancy: implementados con JWT, password hashing, agencias, usuarios, membresías, roles y consultas aisladas por agencia.
- Meta Marketing API: OAuth, intercambio por token extendido, descubrimiento paginado de cuentas y asociación implementados. Falta validación de extremo a extremo con una app Meta real configurada.
- Frontend: el usuario informa integración hasta Fase 6 en el proyecto existente. No revisado ni modificado en esta entrega. Mantener puerto 3001 y no tocar 3000.
- Métricas normalizadas y snapshots diarios: implementados. Análisis, benchmarks, reportes y PDF: no implementados.

## Reglas para continuar

1. Leer `AnaliticAsd/slills/analitiads-backend/SKILL.md` y `AnaliticAsd/slills/PLAN.md`.
2. Trabajar solo dentro de `AnaliticAsd/` salvo documentación local en `AnaliticAsd/slills/`.
3. Mantener un solo proyecto hasta nueva autorización.
4. No asumir como completadas fases ejecutadas en otra solución.
5. Conservar cambios del usuario y no eliminar la solución paralela sin permiso.

## Resultado de la auditoría

- La solución compila con 0 errores y 1 advertencia.
- `Microsoft.OpenApi` 2.0.0, dependencia transitiva, tiene una vulnerabilidad conocida de severidad alta.
- No existen pruebas automatizadas.
- No se encontraron secretos en el código o configuración.
- Se decidió continuar sobre la plantilla y organizar el único proyecto mediante módulos internos.

## Fase 1 completada

- `Microsoft.AspNetCore.OpenApi` actualizado a 10.0.11.
- `Microsoft.OpenApi` fijado en 2.7.5; NuGet no reporta vulnerabilidades conocidas.
- Manejo global de excepciones con respuestas Problem Details.
- Endpoint `GET /api/system/status` con nombre, estado, versión y timestamp UTC.
- OpenAPI disponible en desarrollo.
- Cinco pruebas automatizadas dentro de `AnaliticAsd/Tests`.
- Compilación: 0 advertencias y 0 errores.
- Pruebas: 5 aprobadas, 0 fallidas.

Por decisión del usuario se conserva un único `.csproj`; por eso las pruebas están temporalmente dentro del proyecto web. Separarlas en otro proyecto requiere autorización futura.

## Fase 2 completada

- Entidades de dominio `Client` y `AdAccount`, con validación de ID de Meta, moneda y zona horaria.
- Persistencia con EF Core y proveedor PostgreSQL.
- Migración `InitialCreate` con tablas, relación, índices y restricción única para el ID de cuenta de Meta.
- API CRUD versionada para clientes y cuentas publicitarias.
- Errores HTTP 400, 404 y 409 mediante Problem Details.
- Diecinueve pruebas aprobadas en total, incluyendo dominio, persistencia SQLite en memoria y API.
- Compilación: 0 advertencias y 0 errores.
- NuGet no reporta paquetes vulnerables conocidos.

La migración no se aplicó porque no se verificó una instancia PostgreSQL local disponible. No se implementaron autenticación, Meta, frontend, métricas ni reportes.

## Fase 3 completada

- Entidades `Agency`, `User` y `Membership`, con roles `Owner`, `Admin`, `Analyst` y `Viewer`.
- Registro inicial de agencia y propietario, login JWT y endpoint de identidad actual.
- Contraseñas almacenadas únicamente mediante hash de ASP.NET Core Identity.
- El tenant se obtiene del claim `agency_id`; no se acepta desde los contratos de clientes o cuentas.
- Repositorios de clientes y cuentas publicitarias filtrados por la agencia autenticada.
- Endpoints protegidos y operaciones de escritura restringidas por rol.
- Migración `AddIdentityAndMultiTenancy`, compatible con clientes existentes mediante una agencia de transición.
- Veinticuatro pruebas aprobadas, incluyendo autenticación real, rechazo anónimo y aislamiento entre agencias.
- En desarrollo se usa una clave JWT efímera si no hay secreto configurado. Producción exige `Jwt:SigningKey` de al menos 32 bytes.

## Fase 4 completada

- OAuth iniciado por backend con `state` cifrado y expiración de 10 minutos.
- Callback exclusivo del backend, intercambio por token extendido y retorno controlado al frontend.
- Token Meta protegido en persistencia mediante ASP.NET Core Data Protection y aislado por agencia.
- Descubrimiento paginado de `/me/adaccounts` con `ads_read` y campos explícitos.
- Asociación verificada de cuentas Meta a clientes del tenant autenticado.
- CORS limitado en desarrollo a `http://localhost:3000`.
- Migración `AddMetaOAuth` generada y SQL validado.
- Veintisiete pruebas aprobadas; 0 errores y 0 advertencias.
- No se realizó prueba contra Meta real por falta de credenciales y configuración externa de una app Meta.

## Fase 5 completada

- Entidades persistentes `Campaign`, `AdSet` y `Ad`, relacionadas por IDs internos resueltos desde IDs de Meta.
- Sincronización paginada de `/campaigns`, `/adsets` y `/ads` con scope `ads_read` y Bearer exclusivo de backend.
- Upsert de la jerarquía y conservación de objetos ausentes mediante `is_present_on_meta=false`.
- Estado por cuenta: `NeverSynced`, `Running`, `Succeeded` y `Failed`.
- Consultas tenant-safe para listado y detalle en los tres niveles.
- Migración `AddAdvertisingHierarchy` generada.
- Veintinueve pruebas aprobadas; 0 errores y 0 advertencias.
- No se realizó sincronización contra Meta real por falta de credenciales/configuración externa.

## Próximo paso vigente

Fase 7B: implementar MCP de solo lectura con autorización delegada, aislamiento por usuario/agencia/cliente, revocación y pruebas reales. El frontend debe integrar primero la sección 20 de `FRONTEND_HANDOFF.md`; no adelantar IA, análisis, benchmarks, reportes ni PDF.
