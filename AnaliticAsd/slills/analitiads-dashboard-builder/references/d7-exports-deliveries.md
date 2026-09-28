# D7 — exportaciones y entregas programadas

Fecha: 23 de septiembre de 2026.

## Alcance implementado

- Trabajos persistentes de exportación ligados a una publicación inmutable del dashboard.
- Formatos PDF, CSV UTF-8 y Excel `.xlsx` generado con Open XML.
- Datos obtenidos en servidor desde las fuentes autorizadas del cliente; ausencia permanece vacía y los ratios se recalculan desde totales compatibles.
- Historial interno, estado `Pending/Processing/Completed/Failed` y descarga autenticada.
- Solicitud y descarga pública únicamente para enlaces activos con `allowExport`.
- Agendas diarias, semanales y mensuales con hasta veinte destinatarios.
- Worker independiente del navegador que crea archivos y envía correo mediante SMTP configurable.
- Historial de última ejecución, último éxito y error sanitizado de cada agenda.
- Interfaz de gestión para crear PDF/CSV/Excel, descargar resultados y crear/eliminar entregas semanales.

## Persistencia

La migración `20260923134536_AddDashboardExportsAndDeliveries` crea `dashboard_exports` y
`dashboard_delivery_schedules`. Conserva el número de publicación que originó cada archivo y la
identidad interna, enlace público o agenda que lo solicitó.

La migración está generada, el snapshot EF fue actualizado y se aplicó correctamente a Neon. Las
18 migraciones aparecen registradas y EF confirmó que no existen cambios de modelo pendientes.

## Configuración de correo

Configurar fuera del repositorio: `Email:From`, `Email:Smtp:Host`, `Email:Smtp:Port`,
`Email:Smtp:UseSsl`, `Email:Smtp:Username` y `Email:Smtp:Password`. Si SMTP no está configurado,
la generación manual sigue funcionando y una entrega programada registra un fallo sanitizado.

## Verificación

- Backend: compilación limpia, 0 advertencias y 0 errores.
- Backend: 149 pruebas aprobadas, incluidas reglas de trabajos y cálculo de agendas.
- Frontend: 6 pruebas, lint y build de producción de 19 páginas aprobados.
- EF confirmó que el modelo coincide con la migración y Neon registra
  `20260923134536_AddDashboardExportsAndDeliveries` como la migración número 18.

## Límites de validación externa

No se envió correo real porque no hay un servidor SMTP configurado. Debe ejecutarse una entrega a
una dirección de prueba cuando exista esa configuración. La validación con fuentes reales de Google Ads, TikTok Ads y GA4 sigue dependiendo de sus
credenciales y aprobaciones.

## Próximo bloque

D8: visualizaciones avanzadas, accesibilidad, pruebas end-to-end, observabilidad, carga y cierre de
producción con los cuatro proveedores reales.
