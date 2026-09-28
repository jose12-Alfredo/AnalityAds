# CLAUDE_DASHBOARD_HANDOFF.md

> Traspaso técnico del Dashboard C&P (`cp-dashboard-meta.html`, ~1842 líneas) hacia AnalitiAds.
> Generado a partir del código real del Artifact y del documento `CP-Dashboard-Meta_HANDOFF.md` ya existente en el proyecto.
> **No se modificó ningún archivo del backend ni del frontend de AnalitiAds.**
> **Nota de acceso**: no tengo acceso al sistema de archivos de tu máquina Windows (`C:\Users\jose1\...`), así que este documento no se pudo guardar directamente en `AnaliticAsd\AnaliticAsd\slills\`. Te lo entrego completo aquí para que lo copies manualmente a esa ruta.
> Todos los IDs de cuentas Meta, Google Sheets y archivos de Drive reales fueron **anonimizados** con placeholders (`<META_ACCOUNT_ID>`, `<GOOGLE_SHEET_ID>`, etc.).

---

## A. Inventario del dashboard

### A.1 Archivos que lo componen
- **Un único archivo HTML autocontenido**: `cp-dashboard-meta.html` (~1842 líneas: HTML + `<style>` inline + `<script>` inline). No hay build step, no hay módulos separados.
- Dependencia externa única: **Chart.js 4.5.0** cargado desde CDN jsDelivr (`<script src="https://cdn.jsdelivr.net/npm/chart.js@4.5.0/...">`). Es la única librería externa; el generador de PDF es 100% código propio (no usa jsPDF ni ninguna librería).
- Documento de handoff previo: `CP-Dashboard-Meta_HANDOFF.md` (ya en el proyecto), que describe decisiones e iteraciones históricas.

### A.2 Secciones y pantallas existentes
1. **Objetivos Proyectados** — inputs editables por métrica (opcional, toggle en config).
2. **Resultados** — tarjetas de KPI (`cards5`/`cards6`) con valor, % vs objetivo, gráfico volumen+costo diario.
3. **Campañas** — tabla ordenable/buscable con columnas configurables; clic en fila = "enfocar" esa campaña (sub-filtro).
4. **Creatividades/Anuncios** — tabla + botón "Ver anuncio" (abre Meta Ads Manager en otra pestaña).
5. **Comunidad** — 3 gráficos (edad/género, ciudad/región, género) por cada métrica de volumen elegida.
6. **Ubicación por plataforma** — 3 gráficos (alcance+impresiones, interacciones, género estimado) por `publisher_platform`.
7. **Conclusiones y recomendaciones** — texto generado por IA (capability `sample`) o fallback analítico basado en reglas, editable a mano.

**Vista multiplataforma añadida después:**
- Selector **Global | Meta | Google | TikTok** sobre los controles.
- Vista Global: 6 tarjetas (Inversión combinada, Sesiones, Añadir a carritos, Compras iniciadas, Transacciones, Ingresos totales), tabla "por plataforma", gráfico de ingresos diarios, tabla "Artículos más vendidos", "Compras por canal" y "Compras por campaña de sesión" (con checkboxes de series y encabezados ordenables).
- Panel **"⚙ Administrar marcas"** (dentro del panel de Configuración): CRUD de marcas con sus cuentas Meta, Google Sheet, Excel de Analytics.

### A.3 Componentes visuales
- Header sticky con logo (chip blanco), título, botones de exportación.
- `controls` — selector de plataforma, cuenta (combobox con búsqueda tipo dropdown, `.acct-drop`), rango de fechas, selección de campañas (`.camp-list`, checkboxes en grid 2 columnas).
- Tarjetas `.card` con variantes `cards3/cards5/cards6`.
- `.table-wrap` — tabla con headers pegajosos (`sticky`), ordenamiento por clic en `<th>`, buscador (`.search-inp`), filtro por combo (`.filter-sel`), selector de métricas visibles vía menú desplegable (`.metric-menu`).
- `.focus-bar` — barra de "enfoque" cuando se hace clic en una campaña.
- `.acct-drop` — dropdown de cuentas con badge de "no habilitada" para cuentas sin acceso MCP.

### A.4 Gráficos utilizados (todos vía Chart.js)
- Líneas (`type:"line"`) para series diarias con doble eje Y (volumen + costo, `volumeCostChart`).
- Barras (`type:"bar"`) para plataforma/demografía.
- Doughnut/pie (`type:"doughnut"`) para distribuciones.
- Líneas múltiples para "Compras por canal"/"campaña" en la vista Global.
- Paleta fija: `PALETTE = ["#9D5BF0","#C9A8F5","#7d8893","#640AE6","#C9CD30","#566069"]`.

### A.5 Tablas y filtros
- Motor de tablas genérico compartido por Campañas, Anuncios y Artículos: `buildHead()`, `sortRows()`, objeto `SORT` con estado de orden por tabla (`{key, dir}`).
- Filtros: macro (checkboxes de campañas a incluir, rige todas las secciones), sub-filtro (clic en fila de Campañas, solo afecta Anuncios y Comunidad), selector de métricas visibles, buscador de texto por tabla.

### A.6 Navegación
- No hay rutas/URLs — todo es una sola página con estado en memoria (`STATE`) y renderizado condicional por `platform` (`meta|google|tiktok|global`).
- Cambios de estado disparan funciones `render*()` específicas (`renderResults`, `renderDemographics`, `renderPlatform`, `renderGlobalItemsTable`, etc.), no hay un framework de componentes (todo es DOM manipulado directamente + template strings).

### A.7 Estados de carga, vacío y error
- No hay un sistema de loading states estructurado (spinners genéricos); las llamadas a `callMeta`/`callDrive` son `await` directos dentro de handlers de botón, con `try/catch` que hace `console.error` + `alert()` en caso de fallo del PDF, o deja la tarjeta en `"—"` si falta un dato.
- Errores de conector: `metaHandle()` lanza `Error` explícito si no hay conector de Meta ("El conector de Meta Ads no está disponible en este visor de Claude."); `driveHandle()` retorna `null` silenciosamente y el código cae a fallback (Sheet público).
- Cuentas sin habilitar (permisos) se muestran en el dropdown con un badge `"no habilitada"` en vez de ocultarse.

### A.8 Funciones de exportación
- **PDF**: generador vanilla que construye el archivo PDF byte a byte (estructura `%PDF-1.4`, objetos, xref) sin ninguna librería — ver sección I para riesgos de portar esto.
- Guardado vía `window.claude.use("downloads")` → `downloads.save({filename, data})`, con fallback a `<a download>` + blob URL si la capability no existe.
- Botón "Consultar a Claude" para generar PPT — delega a `window.cowork.sendPrompt` o abre un modal para copiar/pegar el prompt (no genera el PPT en el cliente).

### A.9 Dependencias de Claude, MCP, Google Drive y capabilities (**bloqueantes para AnalitiAds**)
Todas viven en el bloque `<script>`, líneas ~445-478:
```js
let _mcpNS=null,_metaHandleP=null,_driveHandleP=null,_sampleNS=null;
async function getMcp(){
  if(_mcpNS!==null)return _mcpNS;
  try{_mcpNS=(window.claude&&window.claude.use)?(await window.claude.use("mcp")):null;}catch(e){_mcpNS=null;}
  return _mcpNS;
}
async function metaHandle(){
  if(_metaHandleP)return _metaHandleP;
  const mcp=await getMcp();
  if(!mcp)throw new Error("El conector de Meta Ads no está disponible en este visor de Claude.");
  _metaHandleP=await mcp.server("Meta");
  return _metaHandleP;
}
async function driveHandle(){ /* similar, con mcp.server("Google Drive"), null si falla */ }
async function getSample(){ /* window.claude.use("sample") para las conclusiones IA */ }
```
- **`window.claude.use("mcp")`**: única puerta de entrada a datos reales. Sin esto, el dashboard no puede traer ni un solo número.
- **`window.claude.use("sample")`**: genera el texto de "Conclusiones y recomendaciones"; tiene fallback analítico si falla.
- **`window.claude.use("downloads")`**: usado solo para guardar el PDF; tiene fallback a `<a download>`.
- **`window.cowork.sendPrompt`** (legado, mencionado en el HANDOFF anterior): para el botón "Consultar a Claude" (PPT), ya casi no se usa.

### A.10 Uso de localStorage
Todas las claves usadas (**ninguna es multi-tenant ni tiene aislamiento por agencia/usuario** — viven en el navegador de quien abre el Artifact):

| Clave | Contenido |
|---|---|
| `cp_metrics` | array de métricas seleccionadas (`STATE.selectedMetrics`) |
| `cp_showobj` | `"1"`/`"0"`, si se muestran Objetivos |
| `cp_obj_<accountId>_<mes>` (vía `objKey()`) | valores de objetivos por cuenta y mes |
| `cp_brands` | array de marcas configuradas (`DEFAULT_BRANDS` si no hay nada guardado) |
| `cp_theme` | `"dark"`/`"light"` |
| `cp_clientlogo_<accountId>` | logo del cliente en base64 (subido por el usuario, para el PDF) |
| `cp_pdftitle` | último título usado en el PDF |

### A.11 Recursos gráficos, logo, colores y tipografías
- Logo C&P incrustado dos veces en base64 dentro del `<script>`: `LOGO_JPG_B64` (a color, `LOGO_W=700,LOGO_H=301`) y `LOGO_WHITE_JPG_B64` (versión blanca recoloreada, `LWW=700,LWH=301`) — usada en el header del PDF sobre fondo oscuro.
- Tokens de color (ver bloque REUTILIZABLE en la sección F).
- Tipografía: `'Roboto','Segoe UI',system-ui,-apple-system,Arial,sans-serif`.

---

## B. Inventario de datos por sección/componente

| Sección | Datos que necesita | Fuente actual (tool MCP) | Parámetros | Transformaciones |
|---|---|---|---|---|
| Objetivos | valores manuales del usuario | ninguna (input manual) | — | ninguna, solo persistencia en localStorage |
| Resultados (tarjetas KPI) | spend, impressions, reach, cpm, frequency, actions:*, cost_per_action_type:* a nivel cuenta o subset de campañas, diario y total | `ads_get_ad_entities` (vía `callMeta`) | `level:"account"` o `"campaign"` + `filtering`, `fields`, `time_range`, a veces `time_increment:"1"` | `pNum()` (parseo es-BO), derivación de conteos `spend/cost_per_action_type`, agregación manual cuando se filtra por campañas (`aggTotals`) |
| Campañas (tabla) | name, impressions, reach, cpm, spend, frequency, ctr, clicks, actions:page_engagement, actions:link_click, actions:omni_purchase, lead, cost_per_link_click, results, cost_per_result | `ads_get_ad_entities` `level:"campaign"` | `sort:"impressions_descending"`, `limit:300` | orden client-side vía `sortRows`, filtro por búsqueda de texto |
| Anuncios (tabla) | name, campaign_id, impressions, reach, cpm, spend, ctr, actions:* | `ads_get_ad_entities` `level:"ad"` | `sort:"impressions_descending"`, `limit:200` | top 10 por impresiones para el PDF; tabla completa en pantalla |
| Comunidad (edad/género/región) | reach, impressions, actions:page_engagement, actions:link_click, actions:omni_purchase, lead | `ads_get_ad_entities` con `breakdowns:["age"]`, `["gender"]`, `["region"]` (3 llamadas separadas) | `level:"account"` o `"campaign"` + filtering | cruces edad×género y género×plataforma **estimados** aplicando proporción global (no vienen reales de Meta) |
| Ubicación por plataforma | reach, impressions, actions:page_engagement, actions:link_click por `publisher_platform` | `ads_get_ad_entities` `breakdowns:["publisher_platform"]` | — | género por plataforma estimado igual que arriba |
| PVP ("Visitas al perfil y a la página") | campo `results`/`cost_per_result` de campañas | `ads_get_ad_entities` `level:"campaign"`, `fields:["amount_spent","results","cost_per_result"]`, `time_increment:"1"` | — | parseo de `results:"5.702 (Profile and Page visits)"` vía regex (`parseResultVal`) |
| Cuentas disponibles (selector) | lista de cuentas publicitarias | `ads_get_ad_accounts` | `cursor` para paginación | acumulación manual de páginas (máx. 6 iteraciones, `guard<6`) |
| Google Ads (vista Google) | Day/Día, Campaign/Campaña, Cost, Impressions, Clicks, Conversions | Google Sheet vía conector Drive (`download_file_content`, `exportMimeType:"text/csv"`) o fallback CSV público | `fileId` | detección automática de formato inglés/español (`parseGoogleAdsSheet`), `pNum` (es-BO) vs `pNumUS` (formato punto) |
| Analytics/GA4 (vista Global: artículos, canal, campaña, resumen diario) | 4 hojas de un Excel: "Desglose por Artículo", "Compras por Canal", "Compras por Campaña", "Sesiones y Compras Diarias" | conector Drive (`read_file_content`, texto) | `fileId` | parseo por texto con separadores de sección por título exacto de hoja; `pNumUS` para montos |
| Artículos (formatos legado) | ítems comprados/ingresos por artículo | Google Sheet nativo (CSV), PDF de GA4, o xlsx diario | `fileId` | `fetchItemsRows` prueba 3 formatos en cascada; solo el formato "xlsx diario" permite filtrar por rango de fechas real |
| Conclusiones y recomendaciones | texto generado | capability `sample` (IA) | prompt armado con campañas enfocadas/marcadas + métricas elegidas | fallback analítico basado en reglas si `sample` no está disponible |

---

## C. Contrato de las tools utilizadas

### C.1 `ads_get_ad_accounts` (Meta, vía conector MCP "Meta")
- **Llamada real en el código** (`loadAccounts`, línea ~1139-1150):
```js
do{
  const o = await srv.ads_get_ad_accounts(cursor ? {cursor} : {});
  (o.ad_accounts || []).forEach(a => all.push(a));
  cursor = o.next_cursor;
  guard++;
}while(cursor && guard < 6);
```
- **Request**: `{}` o `{cursor: "<token>"}`.
- **Response (forma esperada por el código)**: `{ ad_accounts: [{id, name, ...}], next_cursor: string|null }`.
- **Campos usados**: `id`, `name`.
- **Campos ignorados**: cualquier otro campo que la cuenta traiga (status, currency, etc. — el dashboard no los usa de esta tool, la moneda se maneja como constante `STATE.currency`).
- **Caso especial documentado**: algunas cuentas reales **no aparecen** en esta respuesta aunque sí sean consultables (problema de permisos/visibilidad del lado de Meta) — se agregaban manualmente en el código como fallback hardcodeado (anonimizado aquí, pero existía un objeto `{id, name}` agregado a mano en `loadAccounts` para al menos una cuenta así).
- **Errores posibles**: sin permisos → probablemente la cuenta simplemente no aparece (fallo silencioso desde la perspectiva del dashboard, no un error explícito).

### C.2 `ads_get_ad_entities` (Meta, vía conector MCP "Meta")
- **Wrapper usado por todo el dashboard** (`callMeta`, línea ~594):
```js
async function callMeta(args){
  const srv = await metaHandle();
  const rows = entitiesOf(await srv.ads_get_ad_entities(args));
  const flds = (args && args.fields) || [];
  rows.forEach(function(r){
    flds.forEach(function(f){
      if(typeof f === "string" && f.indexOf(":") > -1 && r[f] === undefined){
        var suf = f.slice(f.indexOf(":") + 1);
        if(r[suf] !== undefined) r[f] = r[suf];
      }
    });
  });
  return rows;
}
```
- **Request — forma general**:
```json
{
  "ad_account_id": "act_<META_ACCOUNT_ID>",
  "level": "account | campaign | adset | ad",
  "fields": ["spend", "impressions", "actions:omni_purchase", "cost_per_action_type"],
  "time_range": "{\"since\":\"2026-08-01\",\"until\":\"2026-09-09\"}",
  "time_increment": "1",
  "breakdowns": ["age"],
  "filtering": [{"field":"campaign.id","operator":"IN","value":["<CAMPAIGN_ID_1>","<CAMPAIGN_ID_2>"]}],
  "sort": "impressions_descending",
  "limit": 300
}
```
- **Response real (anonimizada, un registro)**:
```json
{
  "ad_entities": "[{\"campaign_id\":\"<CAMPAIGN_ID>\",\"name\":\"Campaña Ejemplo\",\"spend\":\"1.234,56\",\"impressions\":\"45.678\",\"reach\":\"12.345\",\"cpm\":\"27,03\",\"frequency\":\"3,70\",\"actions\":[{\"action_type\":\"omni_purchase\",\"value\":\"87\"},{\"action_type\":\"page_engagement\",\"value\":\"512\"}],\"cost_per_action_type\":[{\"action_type\":\"omni_purchase\",\"value\":\"14,19\"},{\"action_type\":\"landing_page_view\",\"value\":\"0,85\"}]}]"
}
```
  - **`ad_entities` viene como string JSON anidado** — hay que parsearlo dos veces (ver `entitiesOf`, sección D).
  - Los valores numéricos (`spend`, `impressions`, `cpm`, `frequency`, valores dentro de `actions`/`cost_per_action_type`) llegan **como strings en formato es-BO** (miles con `.`, decimal con `,`).
- **Campos usados**: los listados en `fields` de cada llamada (ver tabla de la sección B) más `name`, `campaign_id`, `id` implícitos.
- **Campos ignorados**: cualquier campo de la respuesta no pedido explícitamente en `fields` no se usa (la API de Meta normalmente solo devuelve lo pedido, así que en la práctica no hay "sobrante" que ignorar).
- **JSON anidado / doblemente serializado**: sí — `ad_entities` es un string que contiene un array JSON.
- **Errores posibles**: rate limiting de Meta (no documentado explícitamente en el código con manejo de reintentos — el dashboard no implementa backoff; una llamada fallida simplemente propaga el error hacia el `try/catch` del handler que la llamó).

### C.3 Tools de Google Drive (conector MCP "Google Drive")
- **`download_file_content`** — usado para el Google Sheet de Google Ads:
```js
var o = await _drv.download_file_content({fileId: fileId, exportMimeType: "text/csv"});
```
  - Response esperada: objeto con `content` o `base64Content` (CSV en texto/base64) — el código intenta ambos (ver nota de `TextDecoder` en la sección D).
- **`read_file_content`** — usado para el Excel de Analytics de 4 hojas, y como fallback para el reporte de artículos en PDF/xlsx:
```js
var o = await drv.read_file_content({fileId: fileId});
```
  - Response esperada: objeto con `fileContent` (texto plano, incluso para xlsx — la tool ya extrae el texto internamente del lado del servidor MCP).
- **Campos usados**: `content`/`base64Content` (primera tool), `fileContent` (segunda).
- **Errores posibles**: si el archivo no es accesible o no matchea el formato esperado, el código cae al parser de PDF agregado como último fallback (`fetchItemsRows` prueba 3 formatos en cascada).

### C.4 Capability `sample`
```js
async function getSample(){
  if(_sampleNS!==null)return _sampleNS;
  try{_sampleNS=(window.claude&&window.claude.use)?(await window.claude.use("sample")):null;}catch(e){_sampleNS=null;}
  return _sampleNS;
}
```
- Se usa para generar el texto de "Conclusiones y recomendaciones" (`genInsights`) a partir de un prompt armado con las campañas enfocadas/marcadas y las métricas elegidas.
- Si no está disponible, el dashboard usa un **fallback analítico basado en reglas** (comparaciones simples contra objetivos, sin IA).

### C.5 Capability `downloads`
```js
const _dl = (window.claude && window.claude.use) ? (await window.claude.use("downloads")) : null;
if(_dl){ await _dl.save({filename: _pdfName, data: _pdfBlob}); _saved = true; }
```
- Si falla o no existe, fallback a `<a download>` con blob URL.

---

## D. Reglas aprendidas de Meta (comportamientos observados vs. soluciones del dashboard)

**Observado en respuestas reales de Meta** (documentado ya en el handoff anterior y confirmado en el código):
- Los valores numéricos llegan **siempre como strings**, con formato es-BO: miles con `.`, decimal con `,`, moneda como `"$X USD"`. — `pNum()` limpia esto: quita `USD`, `$`, `%`, luego intercambia separadores y hace `parseFloat`.
- `ad_entities` viene como **JSON string anidado**, no como array directo — confirmado por `entitiesOf()`:
  ```js
  function entitiesOf(p){
    if(!p) return [];
    let e = p.ad_entities !== undefined ? p.ad_entities : p;
    if(typeof e === "string"){ try{ e = JSON.parse(e); }catch(x){ return []; } }
    return Array.isArray(e) ? e : [];
  }
  ```
- `cost_per_action_type` **sin especificar subtipo** devuelve el **diccionario completo** de costos por tipo de acción (landing_page_view, add_to_cart, messaging_conversation_started_7d, omni_purchase, etc.), no un solo número.
- El nombre canónico de la acción de "compra" es **`omni_purchase`** (campo `actions:omni_purchase`, sin prefijo adicional). Esto fue un **bug real corregido**: antes de la corrección, buscar la clave con el prefijo incorrecto hacía que "Compras" de Meta mostrara 0 en la vista "por plataforma" del Global, con 313 compras reales verificadas en Ads Manager.
- Meta solo acepta **un `breakdowns` por llamada** — no hay cruces directos (age×gender en una sola llamada no está soportado de forma confiable por esta tool).
- **Filtrar por campaña a nivel `account` no funciona**: Meta ignora el filtro y devuelve la cuenta completa. Solución del dashboard (no un comportamiento de Meta, sino un workaround): consultar a **nivel `campaign`** con `filtering:[{field:"campaign.id",operator:"IN",value:[...]}]` y **agregar los resultados en el cliente** (`aggTotals`, `aggDaily`, `aggBreakdown`).
- El campo `results`/`cost_per_result` de una campaña de clics no es una acción normal — viene como string con formato `"5.702 (Profile and Page visits)"`, parseado con regex (`parseResultVal`) para obtener el número.

**Levels usados y confirmados funcionando**: `account`, `campaign`, `ad` (no se documentó uso de `adset` en el código revisado, aunque el rol existe conceptualmente en el modelo de datos).

**Breakdowns consultados**: `age`, `gender`, `region`, `publisher_platform` — cada uno en llamada separada.

**Combinaciones de breakdown que dieron problemas**: cruces como edad×género o género×plataforma **no se pidieron directamente a Meta** — se **estiman** en el cliente aplicando la proporción global de género sobre el breakdown de edad/plataforma. Esto es una **aproximación del dashboard, no un dato real de Meta** (ver riesgos en la sección I).

**Zona horaria de Bolivia**: el código usa fechas ISO locales (`iso()`, construida a mano con `getFullYear/getMonth/getDate`) en vez de `toISOString()`, específicamente para evitar que el día se corra por la diferencia con UTC (Bolivia es UTC-4). Las fechas que Meta devuelve en texto ("1 de mayo de 2026") se parsean con `parseEsDate()` usando un diccionario de meses en español (`MONTHS_ES`).

**Miles, decimales y moneda**:
- Formato es-BO (Meta, Google Sheet "formato antiguo/inglés"): `pNum()` — punto = miles, coma = decimal.
- Formato US (xlsx de artículos diario, Google Sheet "formato nuevo/español"): `pNumUS()` — coma = miles, punto = decimal. Estos dos formatos **coexisten en el mismo dashboard** según la fuente, y **no son intercambiables** — usar el parser equivocado corrompe silenciosamente los números (fue justamente la causa de un bug real: "Añadir a los carritos" en 0, por decodificar el CSV con `atob()` puro en vez de `TextDecoder("utf-8")`, lo que corrompía acentos y rompía el matching de encabezados en español).

**Paginación**: confirmada solo para `ads_get_ad_accounts` (`cursor`/`next_cursor`, tope de 6 iteraciones como salvaguarda contra loops infinitos). No se documentó paginación explícita para `ads_get_ad_entities` en el código revisado — las llamadas usan `limit` (300 para campañas, 200 para anuncios) sin manejo de "hay más páginas".

**Límites o errores reales encontrados** (del handoff anterior, no inventados aquí):
- Cuentas visibles pero no consultables por el MCP (marcadas "no habilitada" en el dropdown).
- Corrupción de un archivo Excel de ~150KB al intentar transcribir manualmente un blob base64 con `download_file_content` — la lección aprendida y ya aplicada es usar siempre `read_file_content` (texto) para archivos binarios grandes de Drive, nunca reconstruir el binario a mano.

---

## E. Mapeo hacia AnalitiAds

> Regla seguida estrictamente: cuando no hay certeza de que un endpoint de AnalitiAds ya exista con esa forma exacta, se marca **BACKEND PENDIENTE**. No se inventó ningún endpoint.

| Componente del Dashboard C&P | Tool/fuente anterior | Datos requeridos | Endpoint actual de AnalitiAds que podría reemplazarlo | Dato que falta en el backend | Transformación en backend | Transformación solo visual (frontend) |
|---|---|---|---|---|---|---|
| Selector de cuentas | `ads_get_ad_accounts` | lista de cuentas con nombre e id | Probablemente existe algo en el módulo de "descubrimiento y asociación de cuentas publicitarias" mencionado en tus decisiones — **no confirmado el nombre exacto de la ruta** | — | Filtrado por tenant/cliente autenticado (ya lo tiene AnalitiAds vía multi-tenant) | Render del dropdown, búsqueda de texto |
| Tarjetas KPI (Resultados) | `ads_get_ad_entities`, `level:"account"/"campaign"` | spend, impressions, reach, cpm, frequency, actions:*, cost_per_action_type:* | Probablemente el módulo de "Meta Insights" / "métricas en niveles Account, Campaign..." — **BACKEND PENDIENTE** confirmar forma exacta de respuesta | endpoint de **agregación segura de métricas para rangos** (mencionado como pendiente en tu lista) | Cálculo de CPM/CTR/CPC/CPL/CPA/ROAS (ya lo tiene tu backend, según tu descripción — reemplaza la lógica de `METRICS[].get()` del dashboard) | Solo formato (`fmtMoney`, `fmtInt`, `fmtPct`), % vs objetivo |
| Tabla de Campañas | `ads_get_ad_entities`, `level:"campaign"` | name, impressions, reach, cpm, spend, ctr, actions:*, results | Snapshots/métricas a nivel Campaign — **BACKEND PENDIENTE** confirmar si expone estos campos ya calculados | selector avanzado de campañas por actividad (pendiente en tu lista) | — | Orden/búsqueda client-side (`sortRows`) |
| Tabla de Anuncios/Creatividades | `ads_get_ad_entities`, `level:"ad"` | name, campaign_id, impressions, reach, cpm, spend, ctr, actions:* | Métricas a nivel Ad — **BACKEND PENDIENTE** confirmar | Creatividades completas (thumbnails, texto del anuncio) — explícitamente pendiente en tu lista | — | Top 10 por impresiones para PDF |
| Comunidad (edad/género/región) | `ads_get_ad_entities` con `breakdowns` | reach, impressions, actions:* por age/gender/region | — | **BACKEND PENDIENTE** — breakdowns de audiencia explícitamente listados como pendientes | Si se implementa, decidir si los cruces (edad×género) se calculan reales en backend (mejor que la estimación actual) o se documentan como aproximación | Gráficos |
| Ubicación por plataforma | `ads_get_ad_entities`, `breakdowns:["publisher_platform"]` | reach, impressions, engagement por plataforma | — | **BACKEND PENDIENTE** — breakdowns de dispositivo/ubicación listados como pendientes | igual que arriba | Gráficos |
| Comparaciones de periodos | (no existía en el dashboard C&P; el snapshot v2 solo movía un rango dentro de un período fijo) | — | — | **BACKEND PENDIENTE** — explícitamente listado como pendiente | Cálculo de deltas entre periodos | Presentación de deltas |
| Benchmarks | no existía | — | — | **BACKEND PENDIENTE** — explícitamente pendiente | — | — |
| Vista Google Ads | Google Sheet vía Drive | Day/Campaign, Cost, Impressions, Clicks, Conversions | — | **BACKEND PENDIENTE** — Google Ads/GA4 listados como pendientes | Integración real con Google Ads API (reemplaza el hack de leer un Sheet manual) | — |
| Vista Analytics/GA4 (Global) | Excel de 4 hojas vía Drive | sesiones, add to cart, checkout, transacciones, ingresos, por artículo/canal/campaña | — | **BACKEND PENDIENTE** — GA4 listado como pendiente | Integración real con GA4 API | — |
| Conclusiones y recomendaciones | capability `sample` | texto generado sobre las métricas ya calculadas | — | IA opcional — explícitamente mencionada como capacidad futura y opcional en tus decisiones | El motor de análisis/insights, si se construye, debería vivir en backend y nunca modificar las métricas oficiales (regla ya definida por vos) | Render del texto, edición manual |
| Exportación PDF | generador vanilla en frontend | todos los datos ya renderizados en pantalla | — | — | Ninguna — la generación de PDF puede seguir siendo responsabilidad del frontend, pero **no** con el generador byte-a-byte actual (ver sección I) | Recomendado: usar una librería de PDF del lado del frontend Next.js, o mover la generación a backend |
| Snapshot compartible con clientes | Artifact estático con datos "horneados" | — | El acceso de clientes con usuario/contraseña que ya tiene AnalitiAds **reemplaza por completo** este mecanismo | — | — | — |

---

## F. Código reutilizable

### F.1 Tokens de diseño — **REUTILIZABLE DIRECTAMENTE**
```css
:root{
  color-scheme: dark;
  --bg:#1B232A;
  --surface:#212C35;
  --surface2:#2A3640;
  --line:#39454F;
  --text:#ECEEF0;
  --muted:#98A4AE;
  --lila:#9D5BF0;
  --lila-deep:#640AE6;
  --lila-soft:rgba(157,91,240,.16);
  --logo:#8B30E8;
  --ok:#3ed27e;--warn:#f0b34a;--bad:#ff6b6b;
}
```
```js
const LILA="#9D5BF0", LILA_DEEP="#640AE6", NEGRO="#cbd3da", GRIS="#6b7681";
const PALETTE=["#9D5BF0","#C9A8F5","#7d8893","#640AE6","#C9CD30","#566069"];
```
Tipografía: `'Roboto','Segoe UI',system-ui,-apple-system,Roboto,Arial,sans-serif`. Convertible directo a Tailwind config o CSS variables de Next.js.

### F.2 Funciones de formato — **REUTILIZABLE DIRECTAMENTE** (son puras, sin dependencias de DOM ni de Meta)
```js
function fmtInt(n){return Math.round(n).toLocaleString("es-BO");}
function fmtMoney(n){return STATE.currency==="USD" ? ("$"+n.toLocaleString("es-BO",{minimumFractionDigits:2,maximumFractionDigits:2})) : (n.toLocaleString("es-BO",{minimumFractionDigits:2,maximumFractionDigits:2})+" "+STATE.currency);}
function fmtDec(n,d){return n.toLocaleString("es-BO",{minimumFractionDigits:d||2,maximumFractionDigits:d||2});}
function fmtPct(n){return fmtDec(n,2)+"%";}
```
(La única adaptación necesaria: `STATE.currency` debería venir de un contexto/prop en vez de una variable global.)

### F.3 Parsers de números — **REQUIERE ADAPTACIÓN** (útiles solo si AnalitiAds sigue recibiendo strings con estos formatos desde algún lado; si el backend .NET ya entrega `decimal`/`number`, esta lógica **no se necesita en el frontend** — debería vivir, si acaso, en el backend al ingerir datos crudos de Meta)
```js
function pNum(v){ // es-BO: punto=miles, coma=decimal
  if(typeof v==="number")return v;
  if(v==null)return 0;
  let s=String(v).replace(/ /g," ").replace(/USD/gi,"").replace(/\$/g,"").replace(/%/g,"").trim();
  if(s==="")return 0;
  s=s.replace(/\./g,"").replace(/,/g,".");
  const n=parseFloat(s);
  return isNaN(n)?0:n;
}
function pNumUS(v){ // US: coma=miles, punto=decimal
  if(typeof v==="number")return v;
  if(v==null)return 0;
  var s=String(v).replace(/,/g,"").trim();
  var n=parseFloat(s);
  return isNaN(n)?0:n;
}
```

### F.4 Lógica de tablas (orden/filtro genérico) — **REUTILIZABLE DIRECTAMENTE** (es agnóstica de la fuente de datos)
```js
function sortRows(which, metrics, rows){
  const s = SORT[which];
  const m = metrics.find(x => x.k === s.key);
  if(!m) return rows;
  const dir = s.dir === "desc" ? -1 : 1;
  return [...rows].sort((a,b) => m.type === "text"
    ? dir * String(m.val(a)).localeCompare(String(m.val(b)), "es", {sensitivity:"base"})
    : dir * (m.val(a) - m.val(b)));
}
```
Portar a React implicaría convertir `SORT` (objeto global mutable) en estado de componente (`useState`), pero el algoritmo de comparación es directamente reutilizable.

### F.5 Configuración de gráficos Chart.js — **REQUIERE ADAPTACIÓN** (la lógica de "qué mostrar" es reutilizable; hay que envolverla en componentes `<Chart>` de React, y decidir si se mantiene Chart.js o se usa Recharts/otra lib ya estándar en el stack Next.js)
```js
function lineChart(cid, labels, data, label, color, money){ /* ... new Chart(...) ... */ }
function volumeCostChart(cid, labels, vol, cost, volLabel, costLabel, costFmt){ /* doble eje Y */ }
```

### F.6 Motor de definición de métricas (`METRICS[]`) — **REQUIERE ADAPTACIÓN CRÍTICA**
Esto es la pieza más valiosa del dashboard conceptualmente (qué es cada métrica, cómo se calcula, de qué campo de Meta sale), pero **su implementación actual asume la forma cruda de la respuesta de Meta** (`t["actions:omni_purchase"]`, `t["cost_per_action_type:landing_page_view"]`, etc.). Ejemplo:
```js
{k:"compras", g:"PERFORMANCE", label:"Compras", fmt:"int", proj:"high",
  brk:"actions:omni_purchase", pair:"cpcompra",
  get:t=>pNum(t["actions:omni_purchase"])},
{k:"cpcompra", g:"PERFORMANCE", label:"Costo por compra", fmt:"money", proj:"low",
  get:t=>{var c=pNum(t["actions:omni_purchase"]); return c>0 ? SP(t)/c : pNum(t["cost_per_action_type:omni_purchase"]);}}
```
**Lo reutilizable es el catálogo de métricas** (nombre, etiqueta en español, agrupación, fórmula conceptual) — la fórmula de cálculo (spend/conteo) debería vivir en el backend de AnalitiAds (que ya declara tener CPM/CTR/CPC/CPL/CPA/ROAS calculados), y el frontend solo debería **consumir el número ya calculado**, no recalcularlo desde campos crudos de Meta.

### F.7 Lógica de selección de métricas visibles — **REUTILIZABLE DIRECTAMENTE** (es UI state puro)
`STATE.selectedMetrics` (un `Set`), persistido en `localStorage["cp_metrics"]`. En AnalitiAds esto se convierte en estado de React (o preferencia de usuario guardada en backend si se quiere que persista entre dispositivos, que es justamente lo que localStorage no puede dar).

### F.8 Generación de PDF (`generatePdf`, la función completa de ~250 líneas) — **NO REUTILIZAR**
Genera el PDF armando manualmente los objetos `%PDF-1.4`, el `xref`, y streams de contenido con comandos de dibujo tipo `q ... cm /ImNombre Do Q`. Es un hack específico para el sandbox de Artifacts (que bloquea CDNs de librerías de PDF). En un entorno Next.js normal, sin esa restricción, esto se reemplaza por una librería estándar (ver sección I para la recomendación).

### F.9 Manejo responsive — **REQUIERE ADAPTACIÓN**
El CSS usa `grid-template-columns:repeat(N,1fr)` fijo (`.cards5`, `.cards6`) sin media queries visibles en el código revisado — es decir, **no es responsive de forma robusta** (pensado para pantallas de escritorio/laptop, como corresponde a una herramienta interna de agencia). Para una vista de cliente que se abra en celular, esto necesita breakpoints nuevos, no solo portarse.

---

## G. Recursos necesarios

- **Código fuente completo del dashboard**: `cp-dashboard-meta.html`, ya disponible en este proyecto de Claude (`/mnt/project/cp-dashboard-meta.html`). Te lo puedo entregar como archivo descargable aparte si lo necesitas en tu máquina.
- **Logo y recursos gráficos**: incrustados en base64 dentro del propio HTML (`LOGO_JPG_B64`, `LOGO_WHITE_JPG_B64`). **Cómo extraerlos de forma segura**: copiar el string base64 completo (entre las comillas de `LOGO_JPG_B64="..."`) y decodificarlo con cualquier herramienta base64-a-archivo (`Buffer.from(str,'base64')` en Node, o un decoder online offline) guardando el resultado como `.jpg` — es JPEG plano, no un formato propietario, así que no hay riesgo de corrupción si se copia el string completo y exacto.
- **Capturas de cada pantalla y estado**: **no generadas en esta entrega** — no tengo forma de ejecutar el Artifact en un navegador real desde aquí para capturarlas. Si las necesitás, lo más simple es abrir el Artifact publicado (URL en el handoff anterior) y capturarlas manualmente, o pedírmelo en una sesión donde tenga herramienta de navegador disponible.
- **Ejemplo anonimizado de los datos que alimentan cada vista**: incluido en la sección C (requests/responses anonimizados).
- **Ejemplo de un reporte/PDF producido**: no incluido como archivo — el generador de PDF no debería reutilizarse (sección F.8), así que un ejemplo del PDF viejo tiene valor limitado; lo relevante es qué secciones incluye (sección A.8).
- **Marcas o cuentas anonimizadas**: la única marca precargada en el código (`DEFAULT_BRANDS`) fue anonimizada en este documento; el ID real de cuenta Meta y los IDs de Google Sheet/Drive de esa marca **no se reproducen aquí**.
- **Documento de handoff adicional**: `CP-Dashboard-Meta_HANDOFF.md`, ya en el proyecto, con el historial completo de iteraciones y fixes.

---

## H. Propuesta funcional para clientes

### H.1 Vista interna de agencia/planner
Corresponde a lo que el dashboard C&P ya hace hoy, más lo que tu backend ya tiene:
- Selector de **todas las cuentas autorizadas** (multi-cuenta, multi-cliente) — hoy existe como dropdown con búsqueda; en AnalitiAds debería ser un selector de cliente → cuenta, respetando roles (Owner/Admin/Analyst ven todo; ver H.3).
- **Sincronización**: botón explícito o automática — el dashboard C&P no tenía este concepto (todo era "on demand" al abrir el Artifact); esto es una capacidad nueva que ya existe en tu backend (sincronización estructural paginada) y que el dashboard viejo no necesitaba portar 1:1.
- **Drill-down**: el "focus" en campaña (clic en fila → filtra Anuncios y Comunidad) es directamente portable como patrón de interacción.
- **Diagnóstico futuro**: espacio para health-checks de conexión Meta, errores de sync, etc. — no existía en el dashboard viejo.
- **Configuración**: el panel "⚙ Administrar marcas" del dashboard viejo (CRUD de marcas/cuentas) es conceptualmente el antecesor directo de la administración de **clientes y cuentas publicitarias** que ya tiene tu backend — no hace falta portar ese panel, tu backend ya resuelve esa necesidad de otra forma (probablemente mejor, con persistencia real en Postgres en vez de localStorage).

### H.2 Vista de cliente
Debe ser un subconjunto deliberadamente reducido:
- **Solo sus clientes y cuentas asignadas** (esto ya lo resuelve tu multi-tenant + roles; el rol equivalente sería un nuevo rol "Cliente/Viewer externo" más restringido que el `Viewer` interno que ya tenés, si es que `Viewer` hoy todavía ve todas las cuentas de la agencia).
- **KPIs comprensibles**: tarjetas de Resultados (sección A.2, punto 2) — pero probablemente con menos métricas técnicas visibles por defecto (ocultar CPI, CPL crudos si el cliente no los entiende, dejar Inversión/Alcance/Compras/CPA como default).
- **Evolución diaria**: los gráficos de volumen+costo diario (`volumeCostChart`) — directamente reutilizables como concepto.
- **Campañas principales**: la tabla de Campañas, pero probablemente sin las columnas configurables (fijar un set curado de columnas para clientes, no dejarles administrar la vista).
- **Creatividades**: la tabla de Anuncios con thumbnails — hoy el dashboard viejo no tenía thumbnails reales (bloqueo de CDN de Facebook en el sandbox del Artifact); en un dashboard web normal (sin ese sandbox) esto **sí es viable** y sería una mejora real sobre el dashboard anterior.
- **Sin configuraciones técnicas**: nada de "Administrar marcas", nada de selección de métricas avanzada, nada de Objetivos editables (o, si se permite editar Objetivos, que sea una capacidad separada y explícita, no la misma pantalla que ve el planner).
- **Sin acceso a otros clientes**: se resuelve completamente en el backend (tenant/cliente sale de la sesión autenticada, como ya definiste).
- **Sin acceso a tokens o integración Meta**: automático, dado que el navegador del cliente nunca habla con Meta directamente — solo con tu API REST.

### H.3 Mapeo explícito de elementos
| Elemento del Dashboard C&P | Vista interna (planner) | Vista de cliente |
|---|---|---|
| Selector de cuenta/marca | Todas las autorizadas | Solo las suyas, sin selector de "marca" (ya es su marca) |
| Objetivos Proyectados (editable) | Sí | Opcional, probablemente solo lectura |
| Tarjetas KPI | Todas las métricas disponibles | Subconjunto curado |
| Tabla de Campañas | Completa, columnas configurables | Simplificada, columnas fijas |
| Tabla de Anuncios/Creatividades | Completa | Sí, con thumbnails reales (mejora posible) |
| Comunidad/Ubicación | Sí | Opcional según cuánto detalle quiera dar la agencia |
| Conclusiones/IA | Sí, editable | Solo lectura, si se habilita |
| "⚙ Administrar marcas" | Reemplazado por el admin de clientes/cuentas ya existente en AnalitiAds | No visible |
| Exportar PDF | Sí | Sí, versión simplificada |

---

## I. Riesgos y contradicciones (sin ocultar nada, aunque el dashboard "funcione")

1. **Dependencia obligatoria de Claude Artifacts para funcionar hoy**: sin `window.claude.use("mcp")` el dashboard no trae ni un dato — es la razón de ser de todo este traspaso, no algo que se pueda "arreglar parcialmente"; hay que reescribir por completo la capa de datos.
2. **Generador de PDF (F.8) es imposible de copiar directamente a Next.js con sentido** — funciona, pero es un hack específico para eludir el bloqueo de CDNs del sandbox de Artifacts. En un entorno normal sin esa restricción, mantenerlo sería reinventar la rueda peor que con una librería estándar (`pdf-lib`, `@react-pdf/renderer`, o generación en backend con una librería .NET de PDF). Recomendación: **no portar esta función**, reemplazarla.
3. **El motor de métricas (`METRICS[]`, sección F.6) asume campos crudos de Meta con nombres tipo `"actions:omni_purchase"`** — si se copia tal cual, el frontend de AnalitiAds terminaría re-implementando lógica de negocio que tu backend .NET ya declara tener (CPM/CTR/CPC/CPL/CPA/ROAS, "separación entre métricas observadas y derivadas"). Portar esto sin adaptación duplicaría lógica de negocio en dos capas y arriesga inconsistencias entre lo que muestra el dashboard y lo que el backend considera "oficial".
4. **Inseguro para un navegador público, tal cual está**: el código original nunca maneja secretos de Meta directamente (eso siempre vivió en la capa MCP de Claude, no en el HTML), así que en ese sentido específico no hay tokens expuestos en el `cp-dashboard-meta.html`. El riesgo real es otro: **el `localStorage` no tiene ningún control de acceso** — cualquiera con acceso al navegador ve `cp_brands`, objetivos, logo de cliente. En una vista de cliente externo esto es inaceptable tal cual; toda esa configuración debe migrar a backend con control de acceso por sesión.
5. **Cálculos que pueden producir métricas incorrectas si se copian sin revisión**:
   - **Suma de reach por subconjunto de campañas** (`aggTotals` cuando se filtra por campaña): es una **suma simple sin deduplicación**, documentado explícitamente en el código y en el handoff anterior como aproximado ("con todas las campañas es exacto a nivel cuenta; en subconjuntos, el alcance es aproximado"). Si AnalitiAds va a mostrar Alcance para un subconjunto de campañas, **no debe sumarse reach como si fueran personas únicas** — esto ya está en tu lista de "decisiones ya tomadas" ("No se sumará reach diario como personas únicas"), así que el backend probablemente ya lo resuelve correctamente donde el dashboard viejo no podía.
   - **Estimaciones de cruces de audiencia** (edad×género, género×plataforma): son una **aproximación matemática** (proporción global aplicada), **no un dato real de Meta**. Si se porta esta lógica sin dejarlo explícito en la UI, un cliente podría interpretar un número estimado como un dato medido.
   - **Conteos derivados vía `spend / cost_per_action_type`**: es una fórmula válida pero indirecta — puede dar resultados ligeramente distintos a un conteo directo de Meta si `cost_per_action_type` viene redondeado. No es "incorrecto", pero es una derivación, no una medición directa, y así debe etiquetarse si se muestra.
6. **Datos almacenados únicamente en `localStorage`**: marcas, objetivos, tema, logo de cliente, título de PDF — **nada de esto sobrevive** a cambiar de navegador o de dispositivo, y nada de esto tiene aislamiento multi-tenant. Es probablemente el riesgo más directo a resolver en la fusión: todo lo persistente debe migrar a Postgres vía el backend de AnalitiAds.
7. **Dependencia de archivos de Google Drive** (Sheets/Excel) para Google Ads y Analytics/GA4: es un mecanismo manual y frágil (depende de que alguien mantenga actualizado un Sheet/Excel compartido) — no es una integración real con las APIs de Google Ads/GA4. Confirma lo que ya está en tu lista de pendientes del backend.
8. **Uso de IA para conclusiones**: ya está correctamente delimitado en tus decisiones ("la IA será opcional y nunca modificará métricas oficiales") — el dashboard viejo ya seguía ese principio en la práctica (el texto de conclusiones es una capa de interpretación separada, nunca alimenta de vuelta a `STATE.data`).
9. **Diferencias entre snapshot estático y datos en vivo**: el snapshot (Artifact separado, sin capabilities) es una solución específica al problema de "Claude Artifacts no se puede compartir con quien no tiene los conectores" — ese problema **deja de existir por completo** en AnalitiAds, porque el cliente entra con usuario/contraseña a una app que ya trae los datos desde tu backend. No hay necesidad de portar el concepto de snapshot congelado salvo que la agencia quiera explícitamente una versión "congelada" de un reporte para un mes cerrado (eso sí sería una funcionalidad nueva legítima: "reporte cerrado del mes X", no relacionada con la limitación de Claude).

---

## INFORMACIÓN QUE EL BACKEND NECESITA DE CLAUDE

- Confirmación de qué endpoints de AnalitiAds ya devuelven, exactamente, los campos usados por el dashboard C&P (tabla de la sección E marcada "BACKEND PENDIENTE" en varios puntos) — esto no puedo saberlo yo sin ver el código de `AnaliticAsd/AnaliticAsd/`, que no me compartiste en esta ronda.
- Si el backend ya calcula CPM/CTR/CPC/CPL/CPA/ROAS, necesito confirmar los **nombres de campo exactos** en sus respuestas JSON para poder mapear 1:1 contra las claves usadas por `METRICS[].get()` en el dashboard viejo.
- Confirmación de si existe ya algún concepto de "cuenta no habilitada"/permisos parciales en el backend (equivalente al badge "no habilitada" del dropdown viejo), para saber si se reutiliza esa UX o se diseña una nueva.

## INFORMACIÓN QUE EL FRONTEND NECESITA DE CLAUDE

- Nada bloqueante de mi lado — el frontend nuevo no depende de Claude en ningún punto una vez completada esta migración (ese es justamente el objetivo del proyecto). Si en el futuro se agrega IA opcional para "Conclusiones y recomendaciones", en ese momento sí habría que definir si se llama a la API de Claude directamente desde el backend de AnalitiAds (recomendado, para no exponer ninguna key en el navegador) o se deja como una capacidad MCP añadida más adelante, como ya está en tu roadmap.

## DECISIONES QUE NECESITAN CONFIRMACIÓN DEL USUARIO

1. **¿Se porta el motor de métricas (`METRICS[]`) como catálogo de referencia solamente, o se descarta por completo en favor de lo que ya calcula el backend .NET?** (Recomendación de este documento: usarlo solo como catálogo/checklist de qué métricas debe exponer el backend, no como código a copiar.)
2. **¿Qué librería de PDF se usará en el frontend Next.js** (o se genera en el backend .NET)? El generador vanilla del dashboard viejo no debe reutilizarse (sección I.2).
3. **¿Las estimaciones de cruces de audiencia (edad×género, género×plataforma) se implementan igual (aproximadas) en AnalitiAds, o se descartan hasta poder pedir datos reales a Meta por otra vía?** Esto afecta directamente qué debe construir el backend para la vista de Comunidad.
4. **¿La vista de cliente ocultará por defecto las métricas más técnicas** (CPI, CPL crudos, etc.) **o se dejará todo visible y curado manualmente por la agencia por cliente?**
5. **¿Se conserva el concepto de "Objetivos Proyectados" editables para la vista de cliente**, o queda como una herramienta exclusiva del planner?
6. **Prioridad de los "BACKEND PENDIENTE" de la sección E** — ¿cuáles de esos endpoints faltantes se construyen primero para desbloquear el frontend nuevo (KPIs/Campañas primero, y Comunidad/Ubicación/Google Ads/GA4 después, según tu propia lista de "todavía no existen completamente")?
