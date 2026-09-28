# Fusión AnalitiAds + dashboard C&P + IA compatible

> Estado consolidado para otro chat: `../CODEX_HANDOFF.md`. Fases 0–8 están completas; Fase 9 requiere autorización expresa.

## Estado final de Fase 8

Comparaciones y benchmarks internos están disponibles por REST y MCP. MCP incorpora `get_metrics_comparison` y `get_campaign_benchmarks`; conserva el scope de lectura `analitiads:read`, la revalidación multi-tenant y la auditoría. No incorpora análisis, interpretación ni recomendaciones. 72/72 pruebas aprobadas; no fue necesaria una migración nueva.

## Estado final de Fase 7B

Esta sección prevalece sobre la planificación histórica inferior. Fase 7B quedó completada el 11 de septiembre de 2026 con el SDK oficial C# `ModelContextProtocol.AspNetCore` 2.2.0 y la especificación MCP 2026-07-28.

- Endpoint: `POST /mcp`, Streamable HTTP stateless.
- Metadata: `GET /.well-known/oauth-protected-resource/mcp`.
- Scope: `analitiads:read`.
- Conexiones: `GET|POST /api/v1/mcp/connections` y `DELETE /api/v1/mcp/connections/{id}`.
- El `POST` requiere `name` y `consentAccepted: true`; entrega el token MCP solo en esa respuesta. La credencial vence a los 30 días y puede revocarse inmediatamente.
- Tools: `list_authorized_clients`, `list_authorized_ad_accounts`, `list_campaigns`, `get_campaign`, `list_ad_sets`, `get_ad_set`, `list_ads`, `get_ad`, `get_daily_metrics`, `get_metrics_summary`.
- No se acepta `agencyId`; se reutilizan servicios y `AuthorizedData`. El JWT frontend, una clave global y tokens de otra audiencia no funcionan en `/mcp`.
- La auditoría guarda conexión, usuario, agencia, operación, resultado y fecha. No guarda tokens, secretos ni argumentos.
- Verificación: cliente MCP oficial, catálogo e invocación, anonimato, scope insuficiente, audiencia incorrecta, clave global, revocación, membresía y cruces multi-tenant. 69/69 pruebas aprobadas.
- Migración `20260911195705_AddReadOnlyMcpConnections` aplicada en Neon; ocho migraciones aplicadas.

Decisión del usuario, incorporada el 11 de septiembre de 2026. Este documento distingue lo implementado de lo planificado; no habilita nuevas rutas por sí mismo.

## Objetivo y decisiones

- Los clientes entran con su cuenta al dashboard web, sin Claude Desktop, y solo ven clientes que les asignó la agencia.
- El diseño C&P aporta colores, composición, tarjetas, tablas, selector y evolución diaria. AnalitiAds conserva autenticación, permisos, datos persistentes y cálculos en backend.
- La conexión de Claude y otras IA compatibles con MCP es un requisito obligatorio. Usarla será opcional: desconectar una IA no inutiliza la web, la sincronización ni el histórico.
- “Cualquier IA” significa clientes que admitan el protocolo y la autorización implementados, o un adaptador REST autenticado. No se promete compatibilidad universal ni automática.
- No portar `window.claude.use`, un MCP de terceros como fuente obligatoria de la web, IDs/credenciales incrustados ni configuración empresarial en localStorage.
- No portar el generador PDF manual. Cuando corresponda, web y PDF consumirán el mismo `ReportData` inmutable.
- No sumar reach de días/campañas como personas únicas; no reconstruir conteos por `spend / costo` como observados; no estimar cruces de audiencia.

## Implementado en fases 6A y 7

`ClientViewer` es distinto de `Viewer`. Owner/Admin crean y revocan invitaciones y accesos por cliente. Invitaciones: 48 horas, token aleatorio de 256 bits, solo hash SHA-256 en base de datos, un uso y consumo transaccional junto al alta/asignación. No se envía correo automáticamente. Se verifica la contraseña existente al incorporar una cuenta existente; nunca se sustituye su contraseña.

Todas las consultas de clientes, cuentas, Campaign, AdSet, Ad y snapshots pasan por `Infrastructure/Persistence/AuthorizedData.cs`. Un acceso revocado se comprueba en la siguiente consulta, no queda incrustado en un JWT de ocho horas. JWT válidos criptográficamente también requieren membresía, rol, usuario y agencia vigentes.

El alcance es el cliente completo: incluye todas sus cuentas actuales y futuras. No hay aún asignaciones por cuenta individual ni equipo interno configurable. Las reglas SQL se prueban también contra otra agencia y otro cliente de la misma agencia. Contrato REST definitivo: sección 19 de `../FRONTEND_HANDOFF.md`.

Fase 7 también preserva ausencia frente a cero explícito en Insights, identifica snapshots históricos ambiguos como `LegacyZeroNormalized`, valida jerarquía/paginación y expone resúmenes de rango seguros y selector de Campaign. El contrato definitivo está en sección 20 de `../FRONTEND_HANDOFF.md`: no suma reach, no calcula frequency de rango y no promedia ratios diarios.

## Secuencia de implementación vigente

1. Fase 7: completada y verificada el 11 de septiembre de 2026. Incluye migración `AddMetricRangeConsolidation` aplicada en Neon y pruebas de seguridad/normalización/rango.
2. Próxima, Fase 7B: adaptador MCP de lectura dentro del mismo proyecto ASP.NET Core. Reutilizar servicios, validaciones y filtros de acceso de la API; no crear un proyecto Node paralelo ni conectarse directamente a PostgreSQL desde una IA.
3. Más adelante: comparaciones, breakdowns reales, análisis/benchmarks, ReportData/web/PDF, enlaces compartibles e interpretación opcional con IA según PLAN. Google Ads, GA4, TikTok y creatividades completas no tienen contratos activos.

## MCP: requisitos de diseño, NO funcionalidades disponibles

El transporte previsto es Streamable HTTP. Antes de implementarlo, revisar versión vigente del protocolo y SDK oficial C#. Aún no hay endpoint `/mcp`, catálogo de tools, scopes publicados ni URL que entregar a Claude.

Primera versión: listar únicamente clientes/cuentas autorizados, consultar jerarquía y métricas existentes. Sin herramientas para modificar campañas en Meta, administrar usuarios, borrar datos o regenerar métricas oficiales. Las operaciones de escritura futuras exigirán permisos explícitos y controles en la capa de aplicación, no solo atributos HTTP.

Para MCP remoto, usar autorización delegada por usuario con consentimiento y credenciales revocables destinadas a ese recurso. El cliente MCP no recibe Meta App Secret ni access tokens Meta. La especificación HTTP describe OAuth, descubrimiento de metadatos, validación de audiencia y protección del código; una clave global compartida no satisface el aislamiento requerido. [Especificación oficial de autorización MCP](https://modelcontextprotocol.io/specification/2025-11-25/basic/authorization).

La autorización debe aplicar usuario + membresía vigente + agencia + clientes asignados + permisos del conector en cada llamada. No aceptar un `agencyId` de los argumentos de la IA como autoridad, ni permitir token passthrough de Meta. El JWT actual de audiencia frontend no debe reutilizarse automáticamente como credencial universal MCP.

Condición de cierre de 7B: pruebas con un cliente MCP real de inicialización, listado/invocación de tools, rechazo anónimo, permisos insuficientes, audiencia incorrecta, revocación y cruces agencia/cliente; frontend usable con MCP apagado. Solo entonces publicar rutas, scopes, instrucciones de conexión y compatibilidad comprobada.

## Recursos de Claude

Lee `assets/claude-dashboard/README.md` antes de reutilizar las referencias. No sustituye el contrato REST. Los nombres con `actions:...`, números localizados y limitaciones observadas por la tool del dashboard no son especificación de Graph API.

El handoff original de Claude afirma en algunos puntos que el acceso externo ya existía: antes de esta ampliación no era cierto. El `Viewer` original leía toda la agencia. También confunde algunas limitaciones del conector con reglas de Meta y propone agregaciones/estimaciones que aquí no se adoptan.

## Verificación de autorización

La comprobación por recurso complementa los roles; `[Authorize]` por sí solo no resuelve permisos de un cliente concreto. [Documentación oficial ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/resource-based?view=aspnetcore-10.0).

La invitación usa un token de concurrencia EF y una única transacción de guardado, para que un consumo/revocación concurrente no deje un acceso concedido parcialmente. [Documentación oficial EF Core](https://learn.microsoft.com/en-us/ef/core/saving/concurrency).
