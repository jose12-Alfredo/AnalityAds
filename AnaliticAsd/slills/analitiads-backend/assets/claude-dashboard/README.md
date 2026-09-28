# Referencias visuales C&P para AnalitiAds

Preparación del 11 de septiembre de 2026. Fuente proporcionada por el usuario: `Downloads/CLAUDE_DASHBOARD_RESOURCES`. Los originales permanecen intactos. Este directorio NO es un frontend ni una aplicación ejecutable, y no agrega funcionalidades a la API.

## Archivos seleccionados

- `01_logos/`: 2 JPG originales C&P, revisados visualmente. Conservan la identidad de la agencia.
- `02_screenshots/`: 11 PNG revisados visualmente (el README de origen dice 10, pero hay 11). Sus datos son ejemplos sintéticos según el proveedor. Los nombres de campaña/anuncio se identifican como ejemplos; las capturas de configuración no muestran cuentas reales.
- `03_metrics/catalogo_metricas.md`: catálogo histórico, solo como checklist de necesidades. No son campos ni fórmulas autorizados automáticamente para Graph API o AnalitiAds.
- `04_pdf_example/ejemplo_reporte_anonimizado.pdf`: ejemplo original de 6 páginas, revisado mediante extracción de texto, metadatos y render de todas sus páginas. Sin adjuntos internos y con metadatos descriptivos vacíos. Solo referencia de composición; conserva defectos del generador antiguo, como títulos al pie y cortes de sección. No es un PDF generado por AnalitiAds ni un diseño final aprobado.
- `05_chart_configs/chartjs_configs.md`: configuraciones históricas, no listas para copiar sin adaptar.
- `06_responsive/responsive_breakpoints.md`: referencia de los dos breakpoints del original, no validación de usabilidad móvil.

17 archivos fuente importados (2 logos + 11 capturas + 3 documentos + 1 PDF), además de este README. Los archivos importados conservan sus bytes: se verificó SHA-256 frente al origen. El proceso de selección no convierte el ZIP entero en material anonimizado.

## Exclusiones deliberadas

No se copiaron `00_original/cp-dashboard-meta.html`, `00_original/CP-Dashboard-Meta_HANDOFF.md` ni `07_security_anonimizacion/datos_a_eliminar.md`: contienen identificadores y nombres de clientes y/o referencias de Drive. Tampoco se copió el README original, para no presentar esos originales como seguros. No se borraron ni modificaron en Descargas. Si fueran necesarios más adelante, preparar una copia anonimizada y volver a auditar antes de versionar o compartir.

No publicar este directorio completo como assets públicos ni importar el ZIP entero al frontend. Copiar al frontend únicamente recursos visuales concretos que se vayan a usar y que su agente haya revisado.

## Qué se puede reutilizar

Dirección visual: fondos oscuros `#1B232A`, superficies `#212C35`/`#2A3640`, lila `#9D5BF0`/`#640AE6`, tema claro, encabezados, tablas y gráficos diarios. Adaptar componentes a la aplicación ya existente, accesibilidad y móvil; no crear otra app ni portar globals/DOM/Chart.js automáticamente.

Autoridad funcional: `../../../FRONTEND_HANDOFF.md`, secciones 16, 17 y 19. Para decisiones de fusión/MCP: `../../FUSION_DASHBOARD_MCP.md`. Las capturas no cambian las rutas ni permisos de esos contratos.

Prohibiciones de reutilización:

- No llamar `window.claude.use`, instalar un MCP de terceros como dependencia del dashboard ni pasar secretos Meta al navegador.
- No copiar parsers que convierten ausencia en cero, sumas de reach, promedios de ratios, reconstrucción de conteos por costo, ni estimaciones edad/género/plataforma.
- No tomar formatos `actions:...`, afirmaciones de “un breakdown por llamada” o limitaciones del conector original como documentación oficial de Meta. Verificar con documentación oficial en la fase correspondiente.
- No copiar `pieChart` sin validar total cero ni el resto de funciones sin manejar null/no finitos. Los valores numéricos de las capturas son sintéticos, no pruebas de exactitud del motor.
- No habilitar objetivos, Comunidad, Google/GA4/TikTok, análisis/IA, PDF o enlaces compartibles porque aparezcan en la imagen. No existen todavía en el backend.

La web del cliente debe funcionar con su sesión y la API de AnalitiAds aun si Claude/MCP está desconectado. MCP será una entrada adicional protegida al mismo backend, no el motor obligatorio del dashboard.
