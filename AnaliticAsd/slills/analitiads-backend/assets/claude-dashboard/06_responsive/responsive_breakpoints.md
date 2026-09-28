# Breakpoints responsive y limitaciones conocidas — Dashboard C&P

## Breakpoints reales encontrados en el CSS

```css
@media (max-width: 1000px) {
  .controls-grid { grid-template-columns: 1fr 1fr; }
  .cards3, .cards5, .cards6 { grid-template-columns: 1fr 1fr; }
  .camp-list { grid-template-columns: 1fr; }
}

@media (max-width: 640px) {
  .cards3, .cards5, .cards6 { grid-template-columns: 1fr; }
}
```

Eso es **todo** lo que existe de responsive "de pantalla" (no de impresión). Solo 2 breakpoints, y solo afectan:
- La grilla de controles superiores (selector de cuenta/fechas/campañas).
- Las grillas de tarjetas KPI (`cards3`/`cards5`/`cards6`), que pasan de 3/5/6 columnas → 2 columnas (≤1000px) → 1 columna (≤640px).
- La lista de checkboxes de campañas, que pasa de 2 columnas a 1 (≤1000px).

## Un tercer "breakpoint" — no responsive, es para impresión

```css
@media print {
  @page { size: A4 landscape; margin: 11mm; }
  :root { --bg:#fff; --surface:#fff; --surface2:#fff; --line:#cfd4d8; --text:#1B232A; --muted:#5a646d; --lila:#640AE6; }
  body { background:#fff; color:#1B232A; }
  header.topbar { display:none; }
  .controls, .export-btns, .table-tools .metric-picker, .linkbtn, .no-print, .search-inp, .filter-sel, .focus-bar .linkbtn { display:none !important; }
  .print-head { display:flex !important; align-items:center; justify-content:space-between; border-bottom:3px solid var(--lila); padding-bottom:10px; margin-bottom:14px; }
  .print-head .ph-logo { height:44px; }
  .print-head .ph-meta { text-align:right; font-size:12px; color:#1B232A; }
  .print-head .ph-meta b { color:#640AE6; font-size:15px; display:block; }
  .card, .table-wrap { break-inside:avoid; page-break-inside:avoid; border:1px solid #cfd4d8; }
  section.block { break-inside:avoid; page-break-inside:avoid; margin:14px 0; }
  .sec-title { break-after:avoid; }
  thead th { background:#1B232A !important; }
  .chart-box { height:150px; } .chart-box.tall { height:200px; }
}
```
Este bloque **no se usa realmente** para exportar (el PDF real se genera con el generador vanilla en JS, no con `window.print()`, según el propio handoff anterior: "PDF por generador propio, no print del navegador, que fallaba en el visor"). Es CSS muerto o un intento anterior abandonado — vale la pena confirmarlo antes de portarlo, no asumir que está en uso.

## Limitaciones conocidas (no es "responsive real", es una herramienta de escritorio)

1. **No hay diseño mobile-first ni un tercer breakpoint para pantallas muy angostas** (<480px, por ejemplo). Con solo 2 breakpoints y sin reflow de las tablas (que simplemente permiten scroll horizontal/vertical vía `.tbl-scroll`/`overflow:auto`), el dashboard es usable en tablet apaisada hacia arriba, pero **no está pensado para celular**.
2. **Las tablas no colapsan a tarjetas en móvil** — en pantallas angostas quedan con scroll horizontal dentro de `.tbl-scroll`, lo cual es aceptable en desktop/tablet pero incómodo en celular.
3. **El panel de Configuración (`.cfg-panel`) es un drawer de ancho fijo** (`width:330px; max-width:86vw`) — se adapta razonablemente bien incluso en pantallas angostas gracias al `max-width:86vw`, pero no fue diseñado ni probado explícitamente para celular.
4. **Los gráficos dependen de contenedores con altura fija en CSS** (`.chart-box{height:120px}`, `.tall{height:240px}`, `.pie{height:230px}`) en vez de una altura relativa/fluida — en un rediseño para Next.js esto debería resolverse con un sistema de layout más flexible (aspect-ratio o contenedores basados en el viewport), no copiando alturas fijas en píxeles.
5. **La vista de cliente en AnalitiAds probablemente sí necesitará ser mobile-first** (según lo discutido: "el cliente ve el rendimiento desde un navegador normal", sin especificar escritorio) — este es un punto donde el dashboard viejo **no aporta ningún patrón reutilizable**, hay que diseñarlo desde cero para esa vista.
