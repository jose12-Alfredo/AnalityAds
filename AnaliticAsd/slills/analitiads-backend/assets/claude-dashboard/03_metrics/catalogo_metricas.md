# Catálogo completo de métricas — Dashboard C&P

> Extraído directamente del código de `cp-dashboard-meta.html` (arrays `METRICS`, `CAMP_METRICS`, `AD_METRICS`, `ITEMS_METRICS`). Sin recortes.
> Columna **Tipo**: `OBSERVADA` = viene directa de un campo de Meta; `DERIVADA` = se calcula en el dashboard a partir de otros campos (spend/conteo, sumas, promedios); `ESTIMADA` = aproximación matemática que no es un dato real de Meta (cruces de audiencia).

## 1. `METRICS[]` — motor de métricas de "Resultados" (secciones 1-6, tarjetas KPI, comunidad y plataforma)

| Clave (`k`) | Etiqueta | Grupo (`g`) | Campo original de Meta | Fórmula | Formato (`fmt`) | Tipo |
|---|---|---|---|---|---|---|
| `budget` | Presupuesto / Inversión | GENERAL | `amount_spent` (o `spend`) | `SP(t)` = `pNum(t.amount_spent ?? t.spend)` | `money` | OBSERVADA |
| `impressions` | Impresiones | GENERAL | `impressions` | `pNum(t.impressions)` | `int` | OBSERVADA |
| `reach` | Alcance | GENERAL | `reach` | `pNum(t.reach)` | `int` | OBSERVADA (exacto a nivel cuenta; **aproximado si se suma por subconjunto de campañas**, ver handoff sección I) |
| `cpm` | CPM | GENERAL | `cpm` | `pNum(t.cpm)` | `money` | OBSERVADA |
| `frequency` | Frecuencia | GENERAL | `frequency` | `pNum(t.frequency)` | `dec2` | OBSERVADA |
| `interacciones` | Interacciones | ENGAGEMENT | `actions:page_engagement` | `pNum(t["actions:page_engagement"])` | `int` | OBSERVADA |
| `cpi` | CPI (costo por interacción) | ENGAGEMENT | `actions:page_engagement`, `cost_per_action_type:page_engagement` | si conteo>0: `SP(t)/conteo`; si no, `pNum(t["cost_per_action_type:page_engagement"])` | `money4` | DERIVADA |
| `msg` | Conversaciones con mensaje iniciadas | ENGAGEMENT | `cost_per_action_type:onsite_conversion.messaging_conversation_started_7d` | `SP(t) / costo_por_conversación` (conteo derivado del costo, no de un conteo directo) | `int` | DERIVADA |
| `cmsg` | Costo por conversación con mensaje iniciada | ENGAGEMENT | `cost_per_action_type:onsite_conversion.messaging_conversation_started_7d` | directo | `money` | OBSERVADA |
| `link_clicks` | Clics en el enlace | PERFORMANCE | `actions:link_click` | `pNum(t["actions:link_click"])` | `int` | OBSERVADA |
| `cpc` | CPC (costo por clic enlace) | PERFORMANCE | `cost_per_link_click` | directo | `money` | OBSERVADA |
| `lpv` | Visitas a la página de destino | PERFORMANCE | `cost_per_action_type:landing_page_view` | `SP(t) / costo_por_lpv` (conteo derivado) | `int` | DERIVADA |
| `cplpv` | Costo por visita a la página de destino | PERFORMANCE | `cost_per_action_type:landing_page_view` | directo | `money` | OBSERVADA |
| `atc` | Añadidos al carrito | PERFORMANCE | `cost_per_action_type:add_to_cart` | `SP(t) / costo_por_atc` (conteo derivado) | `int` | DERIVADA |
| `cpatc` | Costo por añadido al carrito | PERFORMANCE | `cost_per_action_type:add_to_cart` | directo | `money` | OBSERVADA |
| `compras` | Compras | PERFORMANCE | `actions:omni_purchase` | `pNum(t["actions:omni_purchase"])` | `int` | OBSERVADA |
| `cpcompra` | Costo por compra | PERFORMANCE | `actions:omni_purchase`, `cost_per_action_type:omni_purchase` | si conteo>0: `SP(t)/conteo`; si no, valor directo de `cost_per_action_type:omni_purchase` | `money` | DERIVADA (con fallback observado) |
| `leads` | Leads | PERFORMANCE | `lead` | `pNum(t.lead)` | `int` | OBSERVADA |
| `cpl` | CPL (costo por lead) | PERFORMANCE | `lead`, `cost_per_action_type:lead` | si conteo>0: `SP(t)/conteo`; si no, valor directo | `money` | DERIVADA (con fallback observado) |
| `pvp` | Visitas al perfil y a la página | PERFORMANCE | campo `results` de campañas de clics, parseado por texto (`"5.702 (Profile and Page visits)"`) | `computePvpTotals()` — suma del valor parseado de `results` en campañas seleccionadas | `int` | DERIVADA (parseo de texto, no un campo de acción estándar) |
| `cpvp` | Costo por visita al perfil y página | PERFORMANCE | igual que arriba | `spend_total / conteo_pvp` | `money` | DERIVADA |

**Nota sobre `pair`**: varias métricas tienen un campo `pair` que enlaza cada métrica de volumen con su métrica de costo asociada (ej. `impressions`↔`cpm`, `link_clicks`↔`cpc`, `compras`↔`cpcompra`) — se usa para mostrar el promedio de la métrica pareada como subtítulo de la tarjeta KPI.
**Nota sobre `brk`**: indica qué campo de Meta se usa para las secciones de Comunidad/Ubicación (breakdowns) cuando esa métrica está seleccionada.
**Nota sobre `chart`**: indica si la métrica dispara un gráfico de volumen+costo dedicado en la sección de Resultados (`spend`, `impr`, `reach`, o el nombre de la propia clave si tiene `pair`).

## 2. `CAMP_METRICS[]` — columnas de la tabla de Campañas

| Clave | Etiqueta | Visible por defecto (`def`) | Campo original | Fórmula/Formato | Tipo |
|---|---|---|---|---|---|
| `name` | Nombre de Campaña | — (siempre) | `name` (o `id`) | texto | OBSERVADA |
| `impressions` | Impresiones | ✓ | `impressions` | `fmtInt(pNum(r.impressions))` | OBSERVADA |
| `reach` | Alcance | ✓ | `reach` | `fmtInt(pNum(r.reach))` | OBSERVADA |
| `cpm` | CPM | ✓ | `cpm` | `fmtMoney(pNum(r.cpm))` | OBSERVADA |
| `spend` | Inversión | ✓ | `amount_spent` ?? `spend` | `fmtMoney(...)` | OBSERVADA |
| `frequency` | Frecuencia | ✗ | `frequency` | `fmtDec(...,2)` | OBSERVADA |
| `ctr` | CTR | ✗ | `ctr` | `fmtPct(...)` | OBSERVADA |
| `clicks` | Clicks | ✗ | `clicks` | `fmtInt(...)` | OBSERVADA |
| `interacciones` | Interacciones | ✗ | `actions:page_engagement` | `fmtInt(...)` | OBSERVADA |
| `link_clicks` | Clics enlace | ✗ | `actions:link_click` | `fmtInt(...)` | OBSERVADA |
| `cpc` | CPC enlace | ✗ | `cost_per_link_click` | `fmtMoney(...)` | OBSERVADA |
| `compras` | Compras | ✗ | `actions:omni_purchase` | `fmtInt(...)` | OBSERVADA |
| `leads` | Leads | ✗ | `lead` | `fmtInt(...)` | OBSERVADA |

## 3. `AD_METRICS[]` — columnas de la tabla de Anuncios/Creatividades

| Clave | Etiqueta | Visible por defecto | Campo original | Tipo |
|---|---|---|---|---|
| `thumb` | Imagen / Contenido | — (columna sin dato, tipo `none`) | — | — |
| `name` | Nombre del anuncio | — (siempre) | `name` (o `id`) | OBSERVADA |
| `impressions` | Impresiones | ✓ | `impressions` | OBSERVADA |
| `reach` | Alcance | ✓ | `reach` | OBSERVADA |
| `cpm` | CPM | ✓ | `cpm` | OBSERVADA |
| `spend` | Inversión | ✗ | `amount_spent` ?? `spend` | OBSERVADA |
| `ctr` | CTR | ✗ | `ctr` | OBSERVADA |
| `interacciones` | Interacciones | ✗ | `actions:page_engagement` | OBSERVADA |
| `link_clicks` | Clics enlace | ✗ | `actions:link_click` | OBSERVADA |
| `compras` | Compras | ✗ | `actions:omni_purchase` | OBSERVADA |
| `leads` | Leads | ✗ | `lead` | OBSERVADA |

## 4. `ITEMS_METRICS[]` — columnas de la tabla "Artículos más vendidos" (vista Global, fuente: Excel de Analytics/GA4)

| Clave | Etiqueta | Campo original | Tipo |
|---|---|---|---|
| `name` | Nombre del artículo | columna "Nombre del artículo" del Excel | OBSERVADA (de GA4, no de Meta) |
| `qty` | Artículos comprados | columna "Artículos comprados" | OBSERVADA (de GA4) |
| `revenue` | Ingresos del artículo | columna "Ingreso del artículo" | OBSERVADA (de GA4) |

## 5. Métricas de la vista Global (fuera de los catálogos anteriores, calculadas ad-hoc en `renderGlobalReport`)

| Métrica | Fuente | Fórmula | Tipo |
|---|---|---|---|
| Inversión combinada | Meta `amount_spent` + Google `cost` | suma simple | DERIVADA |
| Sesiones | Excel Analytics, hoja "Sesiones y Compras Diarias" | directo | OBSERVADA (GA4) |
| Añadir a los carritos | idem | directo | OBSERVADA (GA4) |
| Compras iniciadas (checkout) | idem | directo (solo si el Excel tiene esa columna; si no, `"—"`) | OBSERVADA (GA4) |
| Transacciones | idem | directo | OBSERVADA (GA4) |
| Total de ingresos | idem | directo | OBSERVADA (GA4) |
| Tabla "por plataforma" → Compras | Meta `actions:omni_purchase` + Google `conversions` | suma simple | DERIVADA |
| Compras por canal / campaña (secciones 5-6) | Excel Analytics, hojas "Compras por Canal"/"Compras por Campaña" | `aggregateByLabel()` — suma de sesiones/qty/revenue agrupada por canal o campaña | OBSERVADA (GA4), agregada en cliente |

## 6. Estimaciones explícitas (marcadas ESTIMADA, no observadas ni derivadas de una fórmula exacta)

- **Cruces edad×género** y **género×plataforma** en las secciones de Comunidad y Ubicación por plataforma: Meta no permite pedir dos breakdowns cruzados en una sola llamada de forma confiable, así que el dashboard **aplica la proporción global de género** (obtenida del breakdown `gender` a nivel cuenta/campaña) sobre las filas de `age` o `publisher_platform`. **Esto no es un dato real de Meta.**
- Decisión ya tomada para AnalitiAds (confirmada en la conversación anterior): **no se implementarán estas estimaciones**; solo se mostrarán breakdowns reales.
