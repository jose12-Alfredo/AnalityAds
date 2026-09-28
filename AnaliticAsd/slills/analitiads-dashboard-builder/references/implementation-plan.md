# Plan de ejecución: AnalitiAds Dashboard Builder

Fecha: 17 de septiembre de 2026. Este plan fija la dirección del producto. Se usa junto con `product-contract.md` y prevalece para las nuevas capacidades de dashboards sobre planes históricos de fases 0–12.

## Objetivo bloqueado

AnalitiAds tendrá paridad funcional con las capacidades de creación, exploración, colaboración y distribución de Looker Studio que son útiles para agencias publicitarias: fuentes de datos reutilizables, editor visual libre, páginas, componentes, controles, filtros, campos calculados, combinación segura de fuentes, temas, plantillas, publicación, enlaces de lectura, PDF, datos tabulares exportables, envíos programados y visualización embebible.

La implementación será propia. No se copia software, conectores ni visualizaciones de Google. La primera versión funcional se considera terminada únicamente con conexiones reales y verificadas de Meta Ads, Google Ads, TikTok Ads y Google Analytics 4.

## Estado inicial y compatibilidad

| Área | Estado actual | Decisión |
|---|---|---|
| Backend | `AnaliticAsd/AnaliticAsd.csproj` es el único backend | Evolucionar por módulos internos; no crear solución paralela. |
| Frontend | Next.js en `frontend/` | Extender la aplicación existente. |
| Meta | OAuth, cuentas, snapshots, análisis y reportes existentes | Conservarlos y adaptarlos al modelo de fuente genérica. |
| Google Ads / TikTok Ads / GA4 | No implementados | Construir adaptadores independientes. |
| Reportes Fases 11–12 | Snapshots inmutables, PDF y enlaces propios | Mantenerlos como reportes históricos; no convertirlos ni romper sus rutas. |
| Editor de dashboards | No existe | Crear entidades y rutas nuevas bajo `/api/v1/dashboards`. |

## Arquitectura acordada

Los nombres concretos pueden variar, pero se mantienen estas separaciones:

```text
Frontend Next.js
  ├─ explorador de clientes/carpetas
  ├─ editor de dashboards y lector publicado
  └─ cliente API sin secretos

ASP.NET Core / PostgreSQL
  ├─ identidad, agencia, clientes y permisos
  ├─ catálogo de fuentes y conectores por proveedor
  ├─ sincronización y datos observados
  ├─ motor de consultas, métricas y fórmulas
  ├─ definición de dashboards, versiones y plantillas
  ├─ publicación, enlaces, exportación y entrega
  └─ trabajos de fondo persistentes
```

Las cuentas publicitarias y propiedades analíticas dejan de ser Meta-específicas: cada fuente tendrá proveedor, tipo, identificador externo, cliente, moneda cuando aplique, zona horaria, conexión autorizada y estado de sincronización. Los adaptadores de Meta, Google Ads, TikTok y GA4 solo traducen su API al contrato interno; el editor nunca llama directamente a un proveedor.

Las definiciones de dashboard se guardan en base de datos: carpeta, dashboard, páginas, componentes, orden, posición, tamaño, estilo, consulta, filtros y versión. Borrador y publicación son registros/versiones separados. Los archivos de logo e imagen se guardarán mediante una abstracción de almacenamiento, no como datos de navegador.

## Fases y criterios de salida

### D1 — Fundamentos de plataforma y migración segura

Implementar fuente/conexión/cuenta genérica, carpetas y subcarpetas, dashboards, páginas, componentes, versiones borrador/publicada y plantillas. Migrar las cuentas Meta existentes sin pérdida. Añadir permisos de administrador/editor/lector y archivado recuperable.

Especificación ejecutable: `phase-d1-foundation.md`.

Salida: dos agencias y dos clientes aislados; carpetas y dashboards persistentes; las rutas históricas Meta/reportes siguen funcionando; migraciones reversibles y pruebas de aislamiento aprobadas.

### D2 — Editor visual persistente

Implementar lienzo vacío/plantilla, páginas, barra de inserción, propiedades, arrastre, redimensionamiento, cuadrícula, guías, selección múltiple, alinear/distribuir, grupos, bloqueo, capas, copiar/pegar, duplicar/eliminar, zoom, vista previa, deshacer/rehacer y autosave con recuperación de errores.

Estado: D2.1 y D2.2 implementados. El editor incluye páginas, texto/KPI/forma, selección individual y múltiple, arrastre, redimensionamiento, cuadrícula, guías, alineación/distribución, grupos, bloqueo, capas, copiar/pegar, duplicación/eliminación, zoom, vista previa, historial y autoguardado con concurrencia. La inspección visual y la prueba integral de reapertura contra PostgreSQL quedan pendientes hasta disponer de navegador y aplicar las migraciones en una copia segura.

Salida: un editor puede crear, cerrar y reabrir un dashboard conservando exactamente su composición; un visitante no puede ver el borrador.

### D3 — Runtime de datos y visualizaciones iniciales

Crear catálogo de dimensiones/métricas, validador de combinaciones y motor de consulta paginado. Conectar primero los snapshots Meta existentes. Implementar tarjetas, texto, imágenes, separadores, tablas, tablas dinámicas, líneas/series temporales, barras/columnas, apiladas/100 %, circular/dona, área, combinado, embudo, dispersión/burbujas y medidor.

Estado: D3.1, D3.2 y D3.3 terminados con catálogo Meta, validación de compatibilidad, consulta por total/fecha/campaña, fuentes autorizadas y el conjunto inicial de componentes visuales. La tabla dinámica actual usa una dimensión y métricas transpuestas; el segundo desglose depende de la ampliación de consultas de D5. D4 continúa con los conectores reales restantes.

Salida: cada gráfico configura datos y presentación por separado; una consulta inválida explica la métrica o dimensión incompatible; no hay cálculos engañosos de alcance, ratios o moneda.

### D4 — Conectores reales de las tres plataformas

Mantener Meta, añadir Google Ads, TikTok Ads y Google Analytics 4 como adaptadores separados: inicio/callback OAuth o mecanismo oficial aplicable, protección/renovación de credenciales, descubrimiento de cuentas o propiedades, asignación a cliente, intervalo disponible, estado, errores y reconexión. GA4 usa Admin API para descubrir propiedades y Data API para reportes con alcance `analytics.readonly`; no administra propiedades ni envía eventos. Implementar lectura de jerarquía y métricas autorizadas.

Salida: una cuenta real autorizada de cada proveedor publicitario y una propiedad GA4 se conectan, asignan, sincronizan y contrastan con sus interfaces para las mismas fechas y criterios. Una integración sin credenciales queda indicada como bloqueada, nunca simulada como terminada.

### D5 — Sincronización, filtros, fórmulas y combinación

Agregar importación histórica, actualización manual y planificada, colas/trabajos persistentes, reintentos, límites, reanudación, idempotencia, auditoría y refresco de períodos recientes. Añadir filtros de componente/página/dashboard, controles, comparaciones, drilldown y fórmulas con validación. Implementar combinación de canales solo por dimensiones compatibles.

Salida: la sincronización funciona sin navegador abierto, no duplica datos, expone frescura/error y respeta reglas de ausencia, moneda, atribución, alcance y conversiones.

### D6 — Personalización, publicación y distribución

Agregar temas, branding de agencia/cliente, tipografías, fondos, notas, portadas, plantillas iniciales editables, publicar/revertir versión, enlaces con vencimiento/revocación/regeneración, contraseña o destinatarios cuando se configure, permisos de filtros y exportación, y modo embebible con autorización equivalente.

Salida: un visitante autorizado lee solo una versión publicada, no descubre datos ajenos y un enlace revocado queda inutilizable de inmediato.

### D7 — Exportación y entregas

Crear PDF del dashboard publicado, exportación autorizada CSV/Excel, cola de correos programados y registro de generación. El PDF debe respetar páginas, filtros, permisos y visualizaciones compatibles.

Salida: exportaciones y agenda se ejecutan en el servidor, con auditoría y sin depender de un navegador.

### D8 — Paridad extendida, calidad y producción

Implementar mapas, bala, árbol, Sankey, cascada, caja/bigotes, velas, línea de tiempo y el mecanismo de visualizaciones de terceros con revisión de seguridad. Completar accesibilidad, escritorio/tablet/móvil, pruebas end-to-end, instalación limpia en Windows, guía de configuración, observabilidad y pruebas de carga.

Salida: se cumple la matriz completa de aceptación del contrato y se entrega una lista verificable de funciones terminadas y pendientes.

## Matriz de capacidades

| Familia | D1 | D2–D3 | D4–D5 | D6–D8 |
|---|---:|---:|---:|---:|
| Clientes, carpetas y permisos | Sí | — | — | — |
| Editor libre y páginas | Base | Completo | — | — |
| Gráficos iniciales | Modelo | Sí | Datos multicanal | — |
| Campos calculados y blends | Modelo | — | Sí | — |
| Mapas y gráficos avanzados | — | Pendiente explícito | — | Sí |
| Meta / Google Ads / TikTok Ads / GA4 | Fuente genérica | Meta existente | Cuatro reales | Operación continua |
| Plantillas y marca | Modelo | — | — | Sí |
| Publicación, links y embed | Modelo | — | — | Sí |
| PDF, CSV/Excel y correo | — | — | — | Sí |
| Visualizaciones de terceros | — | — | — | Sí, bajo revisión |

## Reglas de datos y seguridad

- El servidor decide acceso a agencias, clientes, carpetas, dashboard, versión, consulta, archivo y exportación.
- Los secretos, tokens OAuth, refresh tokens, contraseñas y tokens de enlace nunca se entregan al navegador ni entran en URL, logs, analítica o repositorio.
- `null` significa ausencia, no cero. Los ratios se recalculan desde totales compatibles; `reach` único no se suma entre días/campañas/canales; las conversiones multicanal no se presentan como personas deduplicadas.
- Toda combinación conserva proveedor, moneda, timezone, atribución y definición de métrica. No hay suma de monedas sin conversión documentada.
- La vista compartida consulta solamente la versión publicada y sus filtros temporales no alteran la definición guardada.

## Dependencias externas y decisiones pendientes

| Elemento | Responsable | Necesario en |
|---|---|---|
| Credenciales Meta y cuenta de prueba real | Agencia | D4 verificación |
| Proyecto OAuth y acceso Google Ads de producción | Agencia | D4 verificación |
| Aplicación aprobada TikTok for Business y anunciante de prueba | Agencia | D4 verificación |
| Proyecto OAuth, Data API/Admin API habilitadas y propiedad GA4 de prueba | Agencia | D4 verificación |
| Dominio HTTPS y URLs de callback | Agencia/infraestructura | D4 producción |
| Proveedor de almacenamiento de imágenes | Decisión técnica | D6 |
| Proveedor de correo | Decisión técnica | D7 |
| Servicio/infraestructura de trabajos persistentes | Decisión técnica | D5 |

Las decisiones técnicas se documentan antes de su implementación. El hecho de que falten credenciales no detiene las entidades, UI, contratos, pruebas simuladas ni migraciones, pero sí la validación real del proveedor.

## Control de alcance

Cada tarea nueva debe indicar: fase, requisito del contrato que cumple, cambios de datos/API/UI, pruebas necesarias, dependencia externa y criterio de cierre. No se declara una fase completada por tener una maqueta, botón o respuesta simulada. Las capacidades no implementadas se muestran como pendientes, nunca como disponibles.
