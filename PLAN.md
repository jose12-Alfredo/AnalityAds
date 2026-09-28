# PLAN OBSOLETO — NO USAR

> Actualización 16 de septiembre de 2026: la reconstrucción paralela `AnalitiAds.sln`, `src/AnalitiAds.*` y `tests/AnalitiAds.Tests` fue eliminada por decisión del usuario. El backend único es `AnaliticAsd/AnaliticAsd.csproj`.

El plan vigente está en `AnaliticAsd/slills/PLAN.md`. El usuario decidió reiniciar desde cero y trabajar exclusivamente en `AnaliticAsd.sln` y `AnaliticAsd/AnaliticAsd.csproj`.

# Plan de construcción de AnalitiAds

## 1. Propósito

AnalitiAds será una plataforma web multi-tenant para agencias que administran múltiples clientes y cuentas de Meta Ads. Permitirá sincronizar datos mediante Meta Marketing API, conservar históricos, calcular métricas, detectar problemas y oportunidades mediante reglas deterministas y publicar reportes web y PDF.

El sistema principal funcionará sin Claude, ChatGPT ni otra IA. Una IA podrá añadirse al final como adaptador opcional de redacción y consulta, sin autoridad para modificar métricas, hechos o recomendaciones oficiales.

## 2. Estado actual

El repositorio contiene actualmente:

- La solución backend `AnalitiAds.sln` con las cinco capas acordadas en .NET 10.
- Clientes y cuentas publicitarias con API CRUD, EF Core, PostgreSQL y migraciones.
- Autenticación por JWT Bearer, agencias, usuarios, membresías y roles.
- Aislamiento por tenant aplicado a clientes y cuentas publicitarias.
- Pruebas de autenticación, autorización y acceso cruzado entre agencias.

No existen todavía:

- Integración directa con Meta.
- Sincronización o históricos.
- Motor de métricas, análisis o insights.
- Reportes web, PDF o enlaces compartibles.
- Frontend Next.js.
- Pruebas automatizadas.

El ZIP de traspaso contiene un prototipo HTML monolítico dependiente de Claude Cowork/MCP. Se conservará únicamente como material funcional y visual de referencia.

### Decisión sobre la base actual

**Opción C: reconstruir la arquitectura desde cero**, reutilizando solo conceptos, reglas que puedan validarse y elementos visuales del prototipo.

Justificación:

- La solución actual es una plantilla sin lógica de producto.
- El prototipo mezcla interfaz, acceso a datos, cálculos, IA y PDF en un solo archivo.
- La integración MCP no sirve como base para una aplicación independiente.
- Los filtros y algunas relaciones del prototipo usan nombres en lugar de IDs.
- Las estimaciones no tienen trazabilidad suficiente.

No se eliminarán archivos existentes ni se modificará el ZIP hasta que la etapa correspondiente lo requiera y sea autorizada.

## 3. Principios obligatorios

1. Relacionar cuentas, campañas, conjuntos y anuncios mediante IDs de Meta, nunca mediante nombres.
2. Usar `decimal` para dinero, valores de conversión, costos y métricas que puedan ser fraccionarias.
3. Guardar moneda y zona horaria de cada cuenta.
4. Guardar timestamps técnicos en UTC y conservar la zona horaria de negocio de Meta.
5. Separar datos observados en Meta de métricas calculadas por AnalitiAds.
6. Clasificar cada valor como `Exact`, `Derived` o `Estimated`.
7. No sumar alcance como si representara personas únicas entre campañas o días.
8. No inventar valores ausentes ni convertir ausencia en cero sin conocer su semántica.
9. No afirmar causalidad cuando solo existe correlación.
10. No almacenar secretos ni tokens sin cifrar o dentro del repositorio.
11. Recorrer completamente la paginación de Meta.
12. Registrar rate limits, reintentos, errores parciales y estado de sincronización.
13. Usar el mismo modelo `ReportData` para el reporte web y el PDF.
14. Mantener reglas de negocio fuera de controladores e infraestructura.
15. Construir una sola etapa a la vez, manteniendo compilación y pruebas verdes.

## 4. Arquitectura final del backend

```text
AnalitiAds/
├── AnalitiAds.sln
├── src/
│   ├── AnalitiAds.Api/
│   ├── AnalitiAds.Application/
│   ├── AnalitiAds.Domain/
│   └── AnalitiAds.Infrastructure/
├── tests/
│   └── AnalitiAds.Tests/
├── reference/
│   └── antiguo-prototipo-claude/
└── .agents/
    └── skills/
        └── analitiads-backend/
            └── SKILL.md
```

Referencias permitidas:

```text
AnalitiAds.Api            -> AnalitiAds.Application, AnalitiAds.Infrastructure
AnalitiAds.Infrastructure -> AnalitiAds.Application, AnalitiAds.Domain
AnalitiAds.Application    -> AnalitiAds.Domain
AnalitiAds.Domain         -> ninguna capa
AnalitiAds.Tests          -> capas bajo prueba
```

### AnalitiAds.Api

Responsabilidades:

- Controladores HTTP.
- Contratos de entrada y salida de la API.
- Autenticación y autorización HTTP.
- OpenAPI.
- Middleware de errores, correlación y tenant.
- Composición de dependencias.
- Health checks.

Los controladores no calcularán métricas ni contendrán reglas de negocio.

### AnalitiAds.Application

Responsabilidades:

- Casos de uso y coordinación.
- Commands, queries y DTOs internos.
- Interfaces de persistencia, cifrado, reloj, Meta API, reportes e IA opcional.
- Validación de solicitudes de aplicación.
- Orquestación de sincronizaciones y análisis.

Módulos previstos:

- Agencies y Users.
- Clients.
- AdAccounts.
- MetaConnections.
- Synchronization.
- AdvertisingEntities.
- Metrics.
- Comparisons.
- Analysis.
- Insights.
- Reports.
- SharedReports.

### AnalitiAds.Domain

Responsabilidades:

- Entidades, value objects y enums.
- Invariantes y reglas puras.
- Fórmulas de métricas.
- Clasificación de precisión.
- Reglas deterministas de análisis.
- Evidencia, confianza y versiones de regla.

No tendrá referencias a ASP.NET Core, EF Core, Meta SDKs, PostgreSQL ni generadores PDF.

### AnalitiAds.Infrastructure

Responsabilidades:

- `DbContext`, configuraciones EF Core y migraciones.
- PostgreSQL.
- Implementación del cliente Meta Marketing API.
- OAuth de Meta y almacenamiento cifrado de tokens.
- Reintentos, rate limits y checkpoints.
- Renderizado de reportes y PDF.
- Adaptadores opcionales de IA.
- Implementaciones de repositorios e interfaces externas.

### AnalitiAds.Tests

Incluirá:

- Pruebas unitarias de dominio y aplicación.
- Pruebas de arquitectura para las referencias entre capas.
- Pruebas de integración con PostgreSQL aislado.
- Pruebas de contratos Meta con respuestas sanitizadas.
- Pruebas HTTP de controladores.
- Pruebas de consistencia entre `ReportData`, web y PDF.

## 5. Modelo multi-tenant y seguridad

La raíz de aislamiento será `Agency`.

```text
Agency
├── Membership -> User
├── Client
│   ├── AdAccount
│   │   ├── Campaign
│   │   │   ├── AdSet
│   │   │   │   └── Ad
│   │   ├── Metric observations
│   │   └── Sync runs
│   └── Reports
└── MetaConnection
```

Reglas:

- Toda entidad de negocio deberá poder resolverse hasta una `Agency`.
- Cada consulta y modificación deberá estar limitada por el tenant autenticado.
- No se aceptará un `AgencyId` del cliente HTTP como fuente de autoridad.
- Los identificadores internos no reemplazarán las verificaciones de pertenencia.
- Los enlaces públicos usarán tokens opacos, aleatorios y almacenados como hash.
- Los enlaces podrán expirar y revocarse.
- Los tokens de Meta se cifrarán en reposo y nunca se devolverán por la API.
- Logs, errores y telemetría no incluirán tokens completos ni payloads sensibles.

## 6. Entidades principales

### Identidad y tenancy

- `Agency`: tenant propietario de la información.
- `User`: identidad de acceso.
- `Membership`: relación de usuario, agencia y rol.
- `Client`: anunciante administrado por la agencia.

### Conexión con Meta

- `MetaConnection`: autorización OAuth perteneciente a una agencia.
- `AdAccount`: cuenta asociada a un cliente y a una conexión válida.
- `EncryptedCredential`: representación de infraestructura; nunca expuesta al dominio o API.

`AdAccount` guardará al menos:

- ID interno.
- `AgencyId` y `ClientId`.
- Meta Ad Account ID como string.
- Nombre.
- Moneda ISO.
- Zona horaria informada por Meta.
- Estado de conexión y sincronización.
- Fecha de última sincronización exitosa.

### Jerarquía publicitaria

- `Campaign` con `AdAccountId` y Meta Campaign ID.
- `AdSet` con `CampaignId` y Meta Ad Set ID.
- `Ad` con `AdSetId` y Meta Ad ID.

Los nombres serán atributos descriptivos y nunca claves relacionales.

### Sincronización

- `SyncRun`: cuenta, rango solicitado, inicio, fin, estado y checkpoint.
- `SyncIssue`: error parcial, entidad/página afectada, código externo y posibilidad de reintento.
- `SyncPage`: cursor o checkpoint sanitizado, cantidad recibida y estado de procesamiento cuando sea necesario para diagnóstico.

Estados previstos:

```text
Pending -> Running -> PartiallySucceeded | Succeeded | Failed
```

### Métricas e histórico

Se usará un modelo híbrido:

- Columnas tipadas para métricas centrales y consultadas frecuentemente.
- Filas normalizadas para acciones/conversiones variables de Meta.
- JSONB únicamente para conservar información adicional o payloads sanitizados, no como sustituto del modelo relacional.

Entidades conceptuales:

- `PerformanceObservation`: métricas observadas por nivel, entidad y periodo.
- `ActionObservation`: acciones por tipo y ventana de atribución.
- `BreakdownObservation`: métricas por dimensión y valor de dimensión.
- `CalculatedMetric`: resultado derivado con fórmula, versión y precisión.

Cada observación tendrá:

- Cuenta y entidad mediante IDs internos vinculados a IDs Meta.
- Nivel: account, campaign, ad set o ad.
- Inicio y fin del periodo observado.
- Zona horaria aplicable.
- Ventana/configuración de atribución cuando corresponda.
- Fuente y fecha de obtención.
- `SyncRunId`.
- Calidad/precisión.

### Métricas no aditivas

Spend, impresiones, clics y acciones compatibles pueden agregarse cuando compartan grano y atribución.

Reach y frequency no se sumarán entre campañas o días. Para un rango arbitrario se seguirá este orden:

1. Usar una observación exacta obtenida por Meta para ese nivel y rango.
2. Solicitarla a Meta si el reporte requiere exactitud y no existe.
3. Usar una estimación solo si el caso de uso lo permite, etiquetándola explícitamente.

## 7. Catálogo y motor de métricas

El catálogo inicial cubrirá:

- General: spend, impressions, reach, frequency y CPM.
- Engagement: interactions, CPI, conversations y costo por conversación.
- Traffic: link clicks, CTR, CPC, landing page views y su costo.
- Leads: leads, CPL y conversion rate.
- Sales: add to cart, purchases, CPA, purchase value y ROAS.
- Other: profile visits y costo por visita.

Fórmulas base:

```text
Frequency = Impressions / Reach
CPM       = Spend / Impressions * 1000
CTR       = LinkClicks / Impressions * 100
CPC       = Spend / LinkClicks
CPA       = Spend / Purchases
CPL       = Spend / Leads
ROAS      = PurchaseValue / Spend
```

Todas las fórmulas deberán:

- Manejar divisiones por cero.
- Distinguir valor ausente de cero real.
- Rechazar valores negativos imposibles.
- Mantener números puros sin formato de moneda.
- Registrar versión de fórmula.
- Registrar datos utilizados para poder auditar el resultado.

## 8. Integración con Meta Marketing API

No se utilizará MCP.

```text
AnalitiAds.Api
      -> caso de uso Application
      -> MetaMarketingApiClient de Infrastructure
      -> Meta Graph API
```

Componentes previstos:

- Flujo OAuth iniciado desde backend.
- Callback con `state` criptográficamente seguro y validado.
- Configuración de App ID y secretos mediante variables de entorno o secret store.
- Cifrado de tokens antes de persistirlos.
- Cliente HTTP tipado.
- Versión de Graph API configurable.
- Paginación hasta agotar cursores, sin topes arbitrarios.
- Reintentos con backoff y jitter solo cuando el error sea reintentable.
- Lectura de señales de uso/rate limit disponibles.
- Cancelación, timeouts y correlation IDs.
- Registro de errores parciales sin perder páginas ya procesadas.
- Upsert idempotente por cuenta, entidad Meta, periodo, breakdown y atribución.

Antes de implementar cada campo o breakdown se verificará en documentación oficial:

- Endpoint y versión.
- Permisos requeridos.
- Nivel permitido.
- Compatibilidad entre campo, breakdown y atribución.
- Semántica de ausencia y cero.
- Restricciones o deprecaciones.

Las pruebas automatizadas usarán fixtures sanitizados. Una integración no se declarará funcional hasta probarla contra una aplicación y cuenta Meta autorizadas.

## 9. Sincronización

Flujo previsto:

```text
Solicitar sincronización
-> crear SyncRun
-> validar conexión y cuenta
-> obtener identidad/configuración de cuenta
-> paginar campañas
-> paginar ad sets
-> paginar ads
-> obtener insights por ventanas controladas
-> normalizar
-> validar
-> persistir por transacciones pequeñas/idempotentes
-> registrar errores parciales
-> finalizar estado
```

La primera versión será bajo demanda. La ejecución programada se agregará únicamente cuando la sincronización manual sea estable.

## 10. Comparaciones y benchmarks

Comparaciones soportadas progresivamente:

- Sin comparación.
- Periodo anterior de igual duración.
- Mes anterior.
- Mismo periodo anterior.
- Año anterior.
- Rango personalizado.

Prioridad de benchmark:

1. Misma campaña en periodos anteriores comparables.
2. Campañas del mismo objetivo y suficiente volumen.
3. Histórico de la cuenta.
4. Mediana del grupo comparable.

No se aplicarán umbrales universales como “CTR menor a 1% es malo”. Los grupos comparables compartirán objetivo, nivel, atribución, moneda y contexto temporal relevante.

## 11. AnalysisEngine

El motor será independiente de controladores, EF Core, Meta e IA.

Entrada conceptual:

```text
AnalysisRequest
├── agency/client/account
├── current period
├── comparison period
├── campaign IDs
├── selected metrics
├── business objective
└── normalized observations
```

Salida conceptual:

```text
AnalysisResult
├── summary facts
├── account analysis
├── campaign analysis
├── ad set analysis
├── ad analysis
├── audience analysis
├── alerts
├── hypotheses
├── insights
└── recommendations
```

Cada hallazgo conservará:

- Tipo.
- Severidad.
- Hechos.
- Evidencia y valores utilizados.
- Benchmark y comparación.
- Entidades Meta involucradas por ID.
- Hipótesis prudentes.
- Recomendación.
- Confianza `High`, `Medium` o `Low` y motivo.
- Versión de regla.
- Fecha de evaluación.

Reglas iniciales:

1. Concentración de inversión, sin afirmar automáticamente que es negativa.
2. Distribución del presupuesto por objetivo y rendimiento comparable.
3. Variación significativa entre periodos.
4. Campaña destacada mediante múltiples métricas y muestra suficiente.
5. Rendimiento inferior frente a benchmark comparable.
6. Posible fatiga: frecuencia creciente junto con CTR decreciente y/o costo creciente.
7. Posible problema después del clic.
8. Respuesta creativa débil con entrega dentro del rango.
9. Entrega publicitaria más cara.
10. Anuncio o ad set destacado indicando siempre el criterio.
11. Audiencias y ubicaciones por volumen, costo y conversión, sin confundirlos.
12. Calidad y suficiencia insuficiente de datos.

Los thresholds y pesos de scoring serán configurables y versionados por objetivo. El score será una ayuda, no una verdad absoluta.

## 12. Reportes

El caso de uso generará un snapshot inmutable `ReportData`.

```text
AnalysisResult + configuración + métricas seleccionadas
                         -> ReportData
                            ├── Reporte web
                            └── PDF
```

`ReportData` contendrá:

- Cliente, cuenta, moneda y zona horaria.
- Periodos actual y comparativo.
- Campañas incluidas mediante IDs.
- Métricas, precisión y procedencia.
- Series temporales.
- Campañas, ad sets, anuncios y rankings.
- Audiencias y ubicaciones.
- Hechos, alertas, hipótesis y recomendaciones.
- Versión del motor y reglas.
- Fecha de generación.

El PDF no recalculará métricas. Renderizará exactamente el snapshot usado por el reporte web.

Estructura de cliente prevista:

1. Portada.
2. Resumen ejecutivo.
3. Objetivos y periodo.
4. KPIs y comparación.
5. Evolución temporal.
6. Campañas principales.
7. Ad sets y creatividades destacadas.
8. Audiencias y ubicaciones.
9. Insights con evidencia.
10. Recomendaciones y próximas acciones.

La tecnología definitiva de PDF se seleccionará mediante una prueba técnica en su fase, priorizando fidelidad entre HTML y PDF, tipografías, paginación, ejecución en servidor y pruebas visuales. No se portará el generador PDF artesanal del prototipo.

## 13. Reportes compartibles

Un reporte publicado será un snapshot, no una consulta mutable.

Características:

- Token opaco de alta entropía.
- Solo se guarda el hash del token.
- Expiración opcional.
- Revocación.
- Protección adicional configurable cuando sea necesaria.
- Acceso exclusivo al snapshot publicado.
- Registro mínimo y respetuoso de privacidad.
- Sin IDs internos o información de otros clientes en la URL.

## 14. API prevista

Los contratos se versionarán bajo `/api/v1`. La lista es una guía de diseño y se implementará por fases.

### Clients

```text
GET    /api/v1/clients
POST   /api/v1/clients
GET    /api/v1/clients/{clientId}
PUT    /api/v1/clients/{clientId}
DELETE /api/v1/clients/{clientId}
```

### Ad accounts

```text
GET    /api/v1/clients/{clientId}/ad-accounts
POST   /api/v1/clients/{clientId}/ad-accounts
GET    /api/v1/ad-accounts/{adAccountId}
PUT    /api/v1/ad-accounts/{adAccountId}
DELETE /api/v1/ad-accounts/{adAccountId}
```

### Meta connections

```text
POST   /api/v1/meta/connections
GET    /api/v1/meta/oauth/callback
GET    /api/v1/meta/connections
DELETE /api/v1/meta/connections/{connectionId}
GET    /api/v1/meta/connections/{connectionId}/available-ad-accounts
```

La forma exacta del inicio OAuth y callback se cerrará después de verificar la documentación oficial y el modelo de seguridad.

### Advertising data

```text
GET /api/v1/ad-accounts/{adAccountId}/campaigns
GET /api/v1/campaigns/{campaignId}/ad-sets
GET /api/v1/ad-sets/{adSetId}/ads
GET /api/v1/ad-accounts/{adAccountId}/metrics
```

Los listados soportarán paginación, búsqueda, estado, objetivo, actividad, gasto y rango de fechas.

### Synchronization

```text
POST /api/v1/ad-accounts/{adAccountId}/sync-runs
GET  /api/v1/ad-accounts/{adAccountId}/sync-runs
GET  /api/v1/sync-runs/{syncRunId}
```

### Analysis

```text
POST /api/v1/analyses
GET  /api/v1/analyses/{analysisId}
```

### Reports

```text
POST   /api/v1/reports
GET    /api/v1/reports
GET    /api/v1/reports/{reportId}
GET    /api/v1/reports/{reportId}/pdf
POST   /api/v1/reports/{reportId}/shares
DELETE /api/v1/reports/{reportId}/shares/{shareId}
GET    /shared/reports/{opaqueToken}
```

## 15. Frontend futuro

El frontend será una aplicación Next.js y TypeScript separada. No se construirá hasta estabilizar autenticación, clientes, cuentas y los primeros contratos de datos/OpenAPI.

Estructura conceptual:

```text
AnalitiAds.Web/
├── app/
│   ├── auth/
│   ├── clients/
│   ├── ad-accounts/
│   ├── campaigns/
│   ├── analyses/
│   ├── reports/
│   └── shared/
├── components/
├── features/
├── lib/api/
├── styles/
└── tests/
```

Pantallas previstas:

- Inicio de sesión.
- Clientes y cuentas publicitarias.
- Conexiones de Meta.
- Estado e historial de sincronizaciones.
- Configurador de análisis: periodo, comparación, campañas, objetivo y métricas.
- Dashboard planner con drill-down Campaign -> Ad Set -> Ad.
- Constructor y previsualización de reporte.
- Administración de enlaces compartidos.
- Vista ejecutiva del cliente.

El cliente API se generará o validará contra OpenAPI para reducir divergencias entre frontend y backend.

## 16. IA opcional

La IA se implementará mediante una interfaz de Application, después de que reportes y reglas funcionen sin ella.

Podrá recibir únicamente un contexto estructurado y controlado:

- Métricas ya calculadas.
- Hechos y comparaciones.
- Insights y recomendaciones existentes.
- Evidencia y confianza.

Podrá mejorar redacción o responder preguntas, pero:

- No escribirá métricas oficiales.
- No modificará observaciones históricas.
- No sustituirá reglas.
- Sus respuestas se marcarán como contenido generado por IA.
- Su ausencia o error no impedirá generar análisis, reporte web o PDF.

## 17. Fases de implementación

### Fase 0 — Auditoría y planificación

Estado: auditoría inicial completada y este plan creado.

Criterio de salida:

- Inventario del repositorio y ZIP conocido.
- Riesgos identificados.
- Arquitectura y etapas documentadas.

### Fase 1 — Solución de cinco proyectos

- Crear `AnalitiAds.sln` y los cinco proyectos acordados.
- Configurar referencias permitidas.
- Habilitar controladores y OpenAPI.
- Añadir pruebas de arquitectura mínimas.
- Compilar y ejecutar tests.

Criterio de salida: solución limpia, compilable y sin referencias inválidas.

### Fase 2 — Client y AdAccount

- Crear únicamente las entidades y value objects necesarios.
- Definir casos de uso iniciales.
- Añadir PostgreSQL y EF Core.
- Configurar mapeos.
- Crear la migración inicial.
- Implementar API CRUD.
- Probar dominio, persistencia y HTTP.

Criterio de salida: clientes y cuentas administrables sin Meta.

### Fase 3 — Autenticación y aislamiento multi-tenant

Estado: completada el 3 de septiembre de 2026.

- Incorporar Agency, User y Membership.
- Implementar autenticación y roles mínimos.
- Resolver tenant desde la identidad autenticada.
- Aplicar aislamiento en consultas y comandos.
- Añadir pruebas negativas de acceso cruzado.

Criterio de salida: ninguna operación puede cruzar agencias.

### Fase 4 — Meta OAuth y cuentas disponibles

- Verificar documentación oficial vigente.
- Configurar aplicación Meta fuera del repositorio.
- Implementar OAuth server-side.
- Cifrar tokens.
- Listar cuentas accesibles con paginación completa.
- Asociar una cuenta autorizada a un cliente.

Criterio de salida: conexión real probada o, si falta aprobación externa de Meta, implementación verificada con fixtures y bloqueo documentado sin afirmar funcionamiento real.

### Fase 5 — Entidades Meta y sincronización

- Sincronizar Campaign, AdSet y Ad mediante IDs.
- Implementar `SyncRun`, errores parciales y checkpoints.
- Añadir reintentos y rate-limit handling.
- Hacer el proceso idempotente.

Criterio de salida: jerarquía completa persistida y sincronización auditable.

### Fase 6 — Métricas e histórico

- Validar campos, niveles, breakdowns y atribución.
- Normalizar métricas centrales y acciones.
- Guardar observaciones históricas.
- Etiquetar exactas, derivadas y estimadas.
- Proteger métricas no aditivas.

Criterio de salida: histórico diario consultable sin pérdida de procedencia.

### Fase 7 — Consultas y selector de campañas

- Exponer campañas con actividad en el rango por defecto.
- Añadir búsqueda, estado, objetivo, gasto y paginación.
- Permitir todas, ninguna y campañas sin actividad de forma explícita.
- Incluir contexto de gasto y resultados.

Criterio de salida: selección inequívoca basada en IDs.

### Fase 8 — Comparaciones y benchmarks

- Implementar periodos comparativos.
- Calcular variaciones seguras.
- Crear grupos comparables por objetivo.
- Aplicar mediana e históricos internos.

Criterio de salida: comparaciones auditables con datos suficientes.

### Fase 9 — AnalysisEngine

- Implementar entrada y salida independientes.
- Analizar cuenta, campañas, ad sets, ads y breakdowns.
- Añadir evaluación de suficiencia.
- Probar fórmulas y casos límite.

Criterio de salida: análisis estructurado sin frases ni IA.

### Fase 10 — Reglas, insights y recomendaciones

- Implementar reglas iniciales versionadas.
- Separar hechos, alertas, hipótesis y recomendaciones.
- Adjuntar evidencia y confianza.
- Añadir scoring configurable por objetivo solo después de validar benchmarks.

Criterio de salida: cada recomendación puede explicar exactamente por qué existe.

### Fase 11 — ReportData, reporte web y PDF

- Crear snapshot inmutable.
- Construir plantilla profesional basada en el prototipo visual.
- Renderizar web y PDF desde los mismos datos.
- Añadir pruebas de consistencia y revisión visual.

Criterio de salida: web y PDF muestran las mismas cifras, filtros y precisión.

### Fase 12 — Enlaces compartibles

- Publicar snapshots mediante tokens seguros.
- Añadir expiración y revocación.
- Probar aislamiento y ausencia de enumeración.

Criterio de salida: un cliente solo accede al reporte explícitamente publicado.

### Fase 13 — Frontend Next.js

- Crear aplicación separada.
- Consumir contratos OpenAPI.
- Implementar primero clientes, cuentas, Meta y sincronización.
- Añadir configurador, dashboard, drill-down y reportes.

Criterio de salida: flujo completo de planner desde login hasta reporte compartido.

### Fase 14 — IA opcional

- Definir proveedor intercambiable.
- Redacción sobre resultados estructurados.
- Consultas controladas sobre un reporte.
- Manejar indisponibilidad sin afectar el sistema.

Criterio de salida: deshabilitar la IA no rompe ninguna función principal.

## 18. Verificación obligatoria por fase

Antes de modificar:

1. Revisar archivos y código relacionados.
2. Explicar brevemente el cambio de la fase.
3. Confirmar que no se adelantarán módulos posteriores.

Después de modificar:

1. Compilar la solución.
2. Ejecutar pruebas relevantes.
3. Corregir fallos introducidos.
4. Revisar `git diff` sin alterar cambios ajenos.
5. Informar exactamente los archivos creados o modificados.
6. Separar lo probado de lo pendiente o no verificable.
7. Indicar el siguiente paso y esperar autorización cuando corresponda.

## 19. Decisiones que se cerrarán en su fase

Estas decisiones no bloquean la Fase 1:

- Proveedor concreto de autenticación y estrategia de recuperación de cuenta.
- Mecanismo definitivo de cifrado y almacenamiento del key ring en producción.
- Hosting y ejecución de trabajos programados.
- Proveedor de almacenamiento para logos y artefactos PDF.
- Tecnología definitiva de renderizado PDF.
- Dominio y política de expiración de enlaces compartidos.
- Proveedor de IA opcional.

Cada decisión se documentará antes de introducir su dependencia y se elegirá usando requisitos de seguridad, operación, costos y mantenibilidad.

## 20. Próximo paso pendiente

La siguiente etapa del backend es la **Fase 4 — Meta OAuth y cuentas disponibles**. No debe iniciarse sin revisar la documentación oficial vigente y contar con una aplicación Meta y credenciales configuradas fuera del repositorio.

Con la Fase 3 terminada ya puede iniciarse, como trabajo separado, la base del frontend Next.js para login, sesión, clientes y cuentas. Las pantallas que dependen de campañas, métricas, análisis y reportes deben esperar sus respectivos contratos backend.
