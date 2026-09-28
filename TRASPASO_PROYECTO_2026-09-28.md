# Traspaso vigente de AnalitiAds

Fecha de revisión: 28 de septiembre de 2026.

Este documento resume el estado real del proyecto para continuar en otro chat. Sustituye como punto de entrada a `TRASPASO_ANALITIADS.md`, que está marcado como obsoleto, y a las notas históricas de `AnaliticAsd/slills/PLAN.md`.

## 1. Resumen ejecutivo

AnalitiAds es una plataforma web multi-tenant para agencias de marketing. Permite administrar clientes y fuentes de Meta Ads, Google Ads, TikTok Ads y Google Analytics 4; sincronizar métricas; crear dashboards editables; compartirlos; exportarlos; y programar entregas.

La implementación funcional histórica (fases 0–12) y el plan Dashboard Builder D1–D8 están terminados en código. D8 es la última fase definida en el plan actual. El trabajo siguiente no es una fase D9 documentada: corresponde al cierre de aceptación con servicios reales, revisión visual, endurecimiento operativo y preparación de despliegue.

Estado técnico verificado el 28 de septiembre de 2026:

- backend ASP.NET Core sobre .NET 10;
- frontend Next.js 16.3.4 y React 19.2.8;
- PostgreSQL/Neon mediante EF Core y Npgsql;
- compilación backend: 0 advertencias y 0 errores;
- pruebas backend: 151 aprobadas, 0 fallidas, 0 omitidas;
- pruebas del editor frontend: 6 aprobadas;
- ESLint frontend: aprobado;
- build frontend: aprobado, 19 rutas generadas;
- última migración documentada y aplicada a Neon: `20260923134536_AddDashboardExportsAndDeliveries`;
- 18 migraciones registradas según la última comprobación documentada;
- D8 no agregó una migración.

La aceptación de producción todavía no está cerrada. Faltan pruebas con credenciales y cuentas reales de Google Ads, TikTok Ads y GA4, contraste de cifras, un envío SMTP real y revisión visual manual en navegadores y tamaños de pantalla.

## 2. Documentos que debe leer el siguiente chat

Leer en este orden:

1. Este archivo.
2. `AnaliticAsd/slills/analitiads-dashboard-builder/references/current-progress.md`.
3. `AnaliticAsd/slills/analitiads-dashboard-builder/references/product-contract.md`.
4. `AnaliticAsd/slills/analitiads-dashboard-builder/references/implementation-plan.md`.
5. `AnaliticAsd/slills/analitiads-dashboard-builder/references/d7-exports-deliveries.md`.
6. `AnaliticAsd/slills/analitiads-dashboard-builder/references/d8-quality-production.md`.
7. `AnaliticAsd/slills/instrucciondesistema.md` y `AnaliticAsd/slills/instruccion.md` para reglas históricas del producto.

Notas sobre documentación antigua:

- `TRASPASO_ANALITIADS.md` declara expresamente que es obsoleto y describe una arquitectura eliminada.
- El `PLAN.md` de la raíz está marcado como obsoleto.
- `AnaliticAsd/slills/PLAN.md` conserva estados antiguos, por ejemplo MCP pendiente.
- Algunas secciones históricas de `current-progress.md` dicen que determinadas migraciones aún no estaban aplicadas. La sección inicial "Estado actual de PostgreSQL/Neon" y las secciones D6.2/D7 son posteriores y prevalecen: Neon llegó a D7.
- `INICIAR_ANALITIADS.md` todavía menciona D6 en su nota de base. Los comandos de arranque son válidos, pero ese dato de migración quedó desactualizado.
- `frontend/FRONTEND_IMPLEMENTATION_STATUS.md` refleja una etapa anterior y no debe usarse para decidir el avance actual.

## 3. Arquitectura actual

El proyecto válido es una única solución y un único proyecto .NET:

```text
AnaliticAsd.sln
├── AnaliticAsd/                 ASP.NET Core, dominio, aplicación, infraestructura y pruebas
│   ├── Application/
│   ├── Contracts/
│   ├── Controllers/
│   ├── Domain/
│   ├── Infrastructure/
│   └── Tests/
├── frontend/                    Next.js/TypeScript
├── scripts/                     utilidades, incluido smoke-load.ps1
└── output/                      SQL y otros artefactos revisables
```

Las pruebas xUnit viven dentro de `AnaliticAsd/Tests` y se compilan desde `AnaliticAsd/AnaliticAsd.csproj`; no existe un proyecto `AnaliticAsd.Tests.csproj` separado.

La aplicación aplica JWT, roles, aislamiento por agencia y cliente, rate limiting en accesos públicos, Problem Details, correlación, cabeceras de seguridad y readiness de base de datos. Los secretos y cadenas reales deben permanecer fuera del repositorio.

## 4. Trabajo completado antes del Dashboard Builder

Las fases históricas 0–12 cubrieron:

- auditoría y arquitectura base;
- clientes, cuentas publicitarias, autenticación y multi-tenancy;
- OAuth de Meta, jerarquía Campaign/AdSet/Ad y snapshots diarios;
- métricas, consolidación, análisis, reglas, insights y recomendaciones;
- acceso a clientes y MCP de solo lectura;
- `ReportData` inmutable, reportes web/PDF y enlaces compartibles;
- frontend de administración y consulta.

Estas capacidades se conservaron al construir el Dashboard Builder.

## 5. Dashboard Builder D1–D8

### D1 — fundamento persistente

- conexiones y fuentes genéricas para Meta Ads, Google Ads, TikTok Ads y GA4;
- compatibilidad con las entidades Meta existentes;
- carpetas, subcarpetas y asignación de editores;
- dashboards, borradores, versiones publicadas y plantillas persistentes;
- explorador frontend de informes.

### D2 — editor visual

- lienzo, componentes, propiedades, capas, bloqueo, duplicación y eliminación;
- autoguardado con revisión y manejo de conflictos;
- hasta 25 páginas, selección múltiple, grupos, alineación, distribución, copiar/pegar, zoom, guías y vista previa;
- movimiento por teclado agregado en D8.

### D3 — datos y visualizaciones

- catálogo de métricas y contrato de consultas;
- KPI, tabla, serie temporal, barras y columnas;
- tabla dinámica, barras apiladas, circular, dona, área, combinado, embudo, dispersión, burbujas, medidor, imagen y separador;
- validación de dimensiones, métricas, unidades, fechas, fuentes y monedas en servidor.

### D4 — conectores

- adaptadores separados para Google Ads, TikTok Ads y GA4;
- autorización, credenciales protegidas, descubrimiento, asignación y sincronización;
- pantallas frontend para conectar y operar cada proveedor.

El código está terminado, pero las integraciones no se consideran aceptadas en producción sin cuentas reales, aprobación de aplicaciones y contraste de resultados.

### D5 — operación multicanal

- agendas de sincronización, worker, bloqueo recuperable y reintento exponencial;
- consultas de las cuatro plataformas;
- período anterior, unión de fuentes, filtros de dimensiones y ratios calculados desde totales;
- rechazo de monedas incompatibles.

### D6 — compartir, marca y plantillas

- marca por agencia o cliente;
- enlaces públicos con token hasheado, contraseña, destinatario, vencimiento, revocación y permisos;
- consulta pública limitada a la versión publicada;
- lector responsive y cinco plantillas editables;
- migraciones D1–D6 aplicadas y verificadas en Neon según la documentación vigente.

### D7 — exportaciones y entregas

- trabajos persistentes PDF, CSV y Excel;
- historial y descargas autenticadas;
- exportación pública condicionada por `allowExport`;
- entregas diarias, semanales y mensuales;
- worker de generación y remitente SMTP;
- interfaz interna para exportar y administrar entregas;
- migración `20260923134536_AddDashboardExportsAndDeliveries` aplicada a Neon.

El envío real de correo todavía requiere configuración SMTP y un destinatario de prueba.

### D8 — calidad y producción

- visualizaciones mapa, bala, treemap, Sankey, cascada, caja y bigotes, velas y línea de tiempo;
- soporte en contrato backend, editor y lector público;
- foco visible, reducción de movimiento y alto contraste;
- endpoint `GET /api/system/readiness` que responde 200 cuando PostgreSQL está disponible y 503 cuando no lo está;
- `X-Correlation-ID`, `X-Content-Type-Options`, `Referrer-Policy`, `Permissions-Policy` y `Cache-Control` para API;
- prueba local de humo de 200 solicitudes con concurrencia 10: promedio 8,44 ms, p95 87,52 ms y máximo 233,02 ms.

La prueba de carga midió `/api/system/status`. Es una comprobación local de disponibilidad, no una prueba completa de consultas, autenticación, base de datos o dashboards bajo carga.

Las visualizaciones avanzadas son renderizados ligeros propios en DOM/SVG. El mapa es una representación relativa y no un mapa geográfico completo. La extensión con runtimes de terceros requiere un diseño de seguridad futuro.

## 6. Diferencia entre reportes y dashboards

`AnaliticAsd/Controllers/ReportsController.cs`, el archivo señalado durante el trabajo, pertenece al sistema histórico de reportes inmutables de la fase 11. Expone:

- `POST /api/v1/reports` para Owner/Admin/Analyst;
- `GET /api/v1/clients/{clientId}/reports`;
- `GET /api/v1/reports/{reportId}`;
- `GET /api/v1/reports/{reportId}/pdf`;
- `DELETE /api/v1/reports/{reportId}` para Owner/Admin.

No es el controlador del editor de dashboards ni de las exportaciones D7. Esas operaciones están principalmente en `DashboardsController`, `DashboardDataController`, `DashboardShareController` y `DashboardExportsController`.

`DashboardExportsController` ofrece exportaciones internas, descargas, agendas de entrega y los endpoints públicos de solicitud/estado/archivo. No se debe fusionar ambos conceptos sin una decisión explícita de producto: un reporte histórico es un snapshot inmutable; un dashboard es un documento editable con versiones publicadas.

## 7. Migraciones y base de datos

Existen 18 migraciones, desde `20260903214558_InitialCreate` hasta `20260923134536_AddDashboardExportsAndDeliveries`.

La documentación vigente afirma:

- las 18 están registradas en Neon;
- no había cambios pendientes en el modelo EF al cerrar D7;
- cinco cuentas Meta y cinco fuentes Meta estaban presentes;
- no se encontraron cuentas huérfanas ni cruces de cliente.

En esta revisión no se repitió `has-pending-model-changes` porque la herramienta local `dotnet-ef` no estaba restaurada. Debe ejecutarse `dotnet tool restore` antes de repetir esa comprobación. No aplicar una migración nueva a Neon sin revisar el SQL y contar con autorización explícita.

## 8. Verificación del 28 de septiembre de 2026

Desde la raíz del repositorio:

```powershell
dotnet build AnaliticAsd.sln --no-restore
# Correcto: 0 advertencias, 0 errores.

dotnet test AnaliticAsd.sln --no-restore --logger "console;verbosity=minimal"
# Correcto: 151 aprobadas, 0 fallidas, 0 omitidas.

cd frontend
npm run test:editor
# Correcto: 6 aprobadas.

npm run lint
# Correcto.

npm run build
# Correcto: 19 rutas generadas.
```

Advertencias no bloqueantes observadas:

- npm avisa que `min-release-age` será desconocido en una versión mayor futura;
- Node avisa que los tests TypeScript se reinterpretan como módulos ES porque `package.json` no declara `type: module`.

## 9. Riesgos y pendientes reales

1. **No hay historial Git.** La rama `master` no tiene ningún commit y casi todo el proyecto figura agregado, modificado o sin seguimiento. Este es el mayor riesgo inmediato de continuidad. Antes de cambios amplios, revisar archivos locales que no deban versionarse y crear un commit base seguro sin secretos, logs, `.idea`, `bin`, `obj` ni artefactos temporales.
2. **Integraciones externas sin aceptación real.** Google Ads, TikTok Ads y GA4 necesitan credenciales, cuentas de prueba y comparación de cifras. Reconfirmar también el recorrido Meta completo antes de producción.
3. **SMTP sin prueba real.** Configurar un entorno seguro y verificar generación, envío, recepción, adjunto/enlace, reintento y errores.
4. **QA visual manual pendiente.** Revisar editor, lector público, exportaciones y responsive en Chrome/Edge/Firefox y tamaños móvil/tablet/escritorio.
5. **Cobertura frontend limitada.** Solo hay seis pruebas unitarias del modelo del editor; no existe una suite E2E amplia para los flujos críticos.
6. **Carga parcial.** La medición D8 solo cubre el endpoint de estado. Hace falta medir login, consultas, apertura/guardado de dashboards, exportaciones y workers con una base representativa.
7. **Despliegue no cerrado.** Faltan definir o validar entorno productivo, variables, observabilidad, backups, restauración, rotación de secretos, límites operativos y procedimiento de rollback.
8. **Documentación contradictoria.** No actualizar el proyecto basándose únicamente en archivos de traspaso o planes anteriores; verificar siempre código, migraciones, pruebas y `current-progress.md`.

## 10. Orden recomendado para continuar

1. Proteger el trabajo actual con higiene de `.gitignore`, revisión de secretos y un commit base.
2. Ejecutar `dotnet tool restore` y confirmar que EF no tiene cambios pendientes.
3. Hacer QA manual de los flujos D1–D8 y registrar defectos reproducibles.
4. Validar Google Ads, TikTok Ads, GA4 y Meta con cuentas reales y contrastar totales, fechas, moneda, zona horaria y paginación.
5. Configurar SMTP en un entorno de prueba y cerrar una entrega programada real.
6. Añadir E2E para autenticación, aislamiento multi-tenant, edición/publicación, enlace público y exportación.
7. Ejecutar una prueba de carga representativa y observar base, workers, colas, errores y latencias.
8. Preparar despliegue, monitoreo, backups, rollback y checklist de aceptación.

## 11. Arranque local

Backend:

```powershell
cd C:\Users\jose1\RiderProjects\AnaliticAsd\AnaliticAsd
dotnet run --launch-profile http
# http://localhost:5019
```

Frontend:

```powershell
cd C:\Users\jose1\RiderProjects\AnaliticAsd\frontend
npm run dev
# http://localhost:3001
```

Se necesita una cadena `ConnectionStrings:AnalitiAds` válida y secretos de JWT/proveedores configurados fuera del control de versiones.

## 12. Texto listo para otro chat

> Continúa AnalitiAds en `C:\Users\jose1\RiderProjects\AnaliticAsd`. Lee primero `TRASPASO_PROYECTO_2026-09-28.md` y luego los documentos vigentes que enumera. El proyecto histórico 0–12 y Dashboard Builder D1–D8 están terminados en código; D8 es la última fase definida. Verifica el estado antes de editar. La validación del 28/09/2026 dio backend build limpio, 151/151 pruebas, frontend 6/6 pruebas, lint y build de 19 rutas. Neon está documentada hasta la migración D7 `20260923134536_AddDashboardExportsAndDeliveries`, con 18 migraciones. No confundas los reportes inmutables de `ReportsController` con los dashboards editables y sus exportaciones. Los pendientes son aceptación real de proveedores, SMTP, QA visual, E2E, carga representativa y despliegue. La rama `master` todavía no tiene commits: protege primero el estado actual, excluye secretos/logs/artefactos y crea un baseline revisable. Conserva el aislamiento multi-tenant, no inventes datos ni afirmes que una integración externa funciona sin una prueba real.
