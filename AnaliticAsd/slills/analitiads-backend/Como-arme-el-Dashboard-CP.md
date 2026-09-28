# Cómo armé el Dashboard C&P · Meta/Google/Analytics

> Guía basada en el documento de handoff del proyecto (`CP-Dashboard-Meta_HANDOFF.md`) y el código (`cp-dashboard-meta.html`).

## 1. Qué es y dónde vive

Es un **dashboard HTML interactivo**, publicado como **Artifact de Claude** (no una app externa), que genera informes mensuales multiplataforma (Meta Ads, Google Ads, Analytics/GA4, TikTok como placeholder) por marca/cuenta para **C&P – Consorcio Publicitario** (agencia en Bolivia).

- Un solo "template general" en un único archivo HTML (`cp-dashboard-meta.html`) que se adapta según las métricas que el usuario elige.
- Trae la data **en vivo** usando conectores MCP (Meta Ads y Google Drive) desde el propio navegador del visor del Artifact.
- Exporta a **PDF** generado 100% en el navegador (sin librerías externas).
- El texto de "Conclusiones y recomendaciones" se genera con IA usando la capability `sample`.

## 2. La pieza clave: conexión a datos vía capabilities de Artifact

Esto es lo primero que hay que entender para poder replicarlo o retomarlo:

1. El código llama `await window.claude.use("mcp")` para pedir el handle de conectores.
2. Con ese handle se obtiene un server por nombre: `mcp.server("Meta")`, `mcp.server("Google Drive")`.
3. Se invocan los métodos por nombre de tool directamente, ej. `srv.ads_get_ad_accounts(args)`, `srv.ads_get_ad_entities(args)`. Cada método ya devuelve el `payload` resuelto, sin tener que "desenvolver" la respuesta a mano.
4. Al publicar/republicar el Artifact hay que declarar las capabilities que usa:

```json
capabilities: {
  "mcp": {
    "servers": [
      {"server": "Meta", "tools": ["ads_get_ad_accounts", "ads_get_ad_entities"]},
      {"server": "Google Drive", "tools": ["download_file_content", "read_file_content"]}
    ]
  },
  "sample": {},
  "downloads": true
}
```

`"Meta"` y `"Google Drive"` son los **nombres de conector** tal como aparecen en Ajustes → Conectores de claude.ai — no un UUID de cuenta. Por eso el mismo código funciona en cualquier cuenta que tenga esos dos conectores conectados, sin tocar nada.

- Si falta el conector de Meta → error legible en pantalla.
- Si falta el de Google Drive → cae automáticamente a un plan B (leer el Google Sheet publicado como "cualquiera con el enlace puede ver").
- El export a PDF usa `window.claude.use("downloads")` → `downloads.save({filename, data})`, con confirmación de descarga (un `<a download>` con blob URL no funciona en la mayoría de visores de Artifacts).

## 3. Identidad de marca aplicada al dashboard

- Lila digital `#640AE6` (títulos/acento), negro `#1B232A` (fondo oscuro/textos), gris `#D1D3D2` (apoyo).
- Lila claro para gráficos sobre fondo oscuro: `#9D5BF0`, `#C9A8F5`; amarillo-verde de acento `#C9CD30`.
- Logo C&P incrustado como **PNG en base64** (no hay internet dentro del sandbox del Artifact), con una versión en blanco recoloreada para el header del PDF sobre fondo oscuro.
- Tipografía Roboto (fallback Segoe UI/Arial).
- Tema oscuro por defecto, con opción de tema claro que mantiene los colores de marca.

## 4. Estructura de datos y vistas

- Selector de plataforma: **Global | Meta | Google | TikTok**.
- Panel "⚙ Administrar marcas" (dentro del panel de Configuración) donde se define, por marca: cuentas de Meta, Google Sheet de Google Ads, y **un solo Excel de Analytics de 4 hojas** que reemplaza a los campos viejos (`analyticsSheet`, `itemsFile`).
- El Excel de Analytics tiene 4 hojas con títulos exactos que el parser usa como separadores:
  1. **Desglose por Artículo**
  2. **Compras por Canal**
  3. **Compras por Campaña**
  4. **Sesiones y Compras Diarias**
- Motor de Google Ads con **detección automática de formato**: encabezados en inglés (decimales es-BO) vs. español (decimales en punto).

### Particularidades del conector de Meta (aprendidas a la fuerza, ya resueltas en el código)
- Los valores llegan como strings es-BO (miles con `.`, decimal con `,`, moneda `"$X USD"`) → parser `pNum()`.
- `ad_entities` viene como JSON string anidado → hay que parsear dos veces.
- Fechas: usar ISO local, no `toISOString()` (corría el día por la zona horaria de Bolivia, UTC-4).
- Filtrar por campaña **a nivel `account` no funciona** — hay que consultar a nivel `campaign` con `filtering` y agregar los resultados en el cliente.
- `cost_per_action_type` sin subtipo devuelve el diccionario completo; los conteos se derivan como inversión ÷ costo.
- Solo se puede pedir **un breakdown por llamada** — cruces como edad×género se estiman aplicando la proporción global.
- Nombre canónico de la acción de compra: `omni_purchase` (sin prefijo `actions:`) — un bug real vino de buscar la clave con el prefijo equivocado.

## 5. Secciones del reporte

1. Objetivos Proyectados (editable, opcional).
2. Resultados — tarjetas por métrica elegida.
3. Campañas — tabla ordenable/buscable, clic en fila = enfocar esa campaña.
4. Creatividades/Anuncios.
5. Comunidad — gráficos de edad/género, ciudad/región.
6. Ubicación por plataforma.
7. Conclusiones y recomendaciones — generadas por IA (capability `sample`) con fallback analítico si no está disponible.

En la vista **Global** se agregan además: tabla de "Artículos más vendidos", "Compras por canal" y "Compras por campaña de sesión" (con checkboxes para elegir qué series graficar y encabezados ordenables).

## 6. Decisiones de diseño que marcaron la arquitectura

- PDF generado con código propio en el navegador (el `print()` nativo fallaba en el visor, y no se pueden usar librerías externas por el bloqueo de CDNs del sandbox).
- Logos incrustados en base64 porque el sandbox no tiene acceso a internet.
- Arquitectura orientada a métricas: lo que el usuario marca en el selector rige qué se muestra en el dashboard.
- Persistencia con `localStorage` (objetivos, logo del cliente, marcas configuradas, métricas, tema) — esto **no viaja entre navegadores o usuarios**, cada quien lo configura en su propia sesión.
- Texto de conectores siempre decodificado con `TextDecoder("utf-8")`, nunca `atob()` a secas (corrompía acentos y rompía el matching de encabezados en español).

## 7. Snapshot estático para compartir con clientes sin conectores

El dashboard vivo requiere que quien lo abre tenga los conectores de Meta y Google Drive conectados en su propia cuenta — así que no se puede mandar directo a un cliente. La solución fue publicar un **segundo Artifact independiente**, estático:

- Sin `capabilities` de `mcp`/`sample`/`downloads` — ninguna llamada en vivo.
- Los datos van "horneados" en una constante `SNAPSHOT_DATA` con series **diarias** (no totales pre-agregados), lo que permite poner dos `<input type="date">` (Desde/Hasta) y que el cliente mueva el rango dentro del período embebido — todo se recalcula en el navegador reusando las mismas funciones puras del dashboard en vivo.
- Para regenerarlo: pedir datos diarios reales con los conectores activos, parsearlos con las funciones reales del código (cargando el `<script>` del HTML en un sandbox de Node para no reimplementar la lógica a mano), armar el JSON final, y republicar el Artifact sin capabilities.

## 8. Cómo retomar el trabajo en otra sesión o cuenta

1. Conectar los conectores de **Meta Ads** y **Google Drive** en Ajustes → Conectores, con acceso a las cuentas publicitarias y la carpeta de Drive correspondientes.
2. Publicar `cp-dashboard-meta.html` con la herramienta Artifact, declarando las capabilities de la sección 2. No hay que tocar ningún UUID en el código.
3. Recordar que la configuración por usuario (localStorage) se rehace en cada sesión/navegador — si cambió el archivo de una marca, hay que actualizarlo a mano en "⚙ Administrar marcas".

## 9. Pendientes conocidos

- Probar a fondo la vista Google/Global con el conector de Drive conectado en un visor real.
- Tabla de campañas unificada Meta+Google en Global.
- Exportar a PDF las vistas Google/Global (hoy el PDF solo cubre Meta).
- TikTok real cuando se defina la fuente de datos.
- Generación de informe en PowerPoint desde el lado de Claude.
- Histórico de marca orgánico (requiere conector de Página de Facebook/Instagram, de pago).
- Tarea programada para generar el reporte automáticamente cada mes.
