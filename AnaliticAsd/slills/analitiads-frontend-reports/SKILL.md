---
name: analitiads-frontend-reports
description: Integrar o revisar en el frontend de AnalitiAds los reportes inmutables y sus enlaces compartibles de Fases 11 y 12. Usar cuando el trabajo afecte creación, listado, detalle, PDF, administración de enlaces o vista pública; no usar para modificar el cálculo backend de AnalysisEngine.
---

# AnalitiAds Frontend Reports

Implementa el frontend contra el contrato vigente de Fase 11. Antes de editar, lee [references/report-api.md](references/report-api.md) y revisa el cliente HTTP, autenticación, tipos de AnalysisEngine y patrones visuales existentes.

## Invariantes

- Un reporte guardado es un snapshot histórico. Renderiza `report.analysis`; nunca vuelvas a ejecutar `/analyses` para reconstruirlo.
- Descarga el PDF con JWT Bearer como `blob`, usa una URL temporal y revócala después.
- Conserva `null` como “Sin dato”. No recalcules métricas, comparaciones, benchmarks, insights ni recomendaciones.
- Nunca envíes `agencyId`, `clientId`, creador, versión o hash al crear.
- Usa el backend como autoridad de permisos. Oculta acciones incompatibles con el rol, pero maneja siempre `401`, `403` y `404`.
- Ante `404`, limpia el detalle y cualquier caché privada del reporte y refresca la lista autorizada.
- No almacenes PDFs, blobs o URLs temporales en `localStorage`.
- Para enlaces públicos, extrae el token del fragmento URL, limpia la barra inmediatamente y envíalo solo por body. Nunca lo pongas en path, query, logs o analytics.
- Los gráficos pueden ser interactivos sobre el snapshot; no pueden cambiar las cifras, el periodo ni consultar datos actuales.

## Flujo de implementación

1. Extiende el cliente API central y los tipos compartidos; evita llamadas `fetch` dispersas en componentes.
2. Añade creación desde la selección actual de AnalysisEngine y exige un título.
3. Añade lista paginada por cliente, ordenada según la respuesta del backend.
4. Crea una vista de detalle que consuma exclusivamente el snapshot persistido.
5. Añade descarga PDF autenticada y eliminación con confirmación solo para Owner/Admin.
6. Representa carga, lista vacía, ausencia de conclusiones, error, permiso insuficiente y acceso revocado.
7. Verifica roles, navegación, `null`, errores Problem Details y liberación de la URL del blob.
8. Cuando corresponda Fase 12, añade administración Owner/Admin y una página pública mínima que no exponga navegación privada.

## Cierre

Informa archivos modificados y pruebas ejecutadas. Si el contrato observado difiere de la referencia, comprueba primero `../FRONTEND_HANDOFF.md` y el backend actual; no inventes rutas o campos.
