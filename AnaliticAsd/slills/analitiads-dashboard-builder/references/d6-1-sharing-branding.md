# D6.1 — branding y enlaces de dashboards

Fecha: 22 de septiembre de 2026.

## Implementado

- Identidad visual persistente de agencia y cliente: logo HTTPS, colores principal/secundario/fondo y tipografía.
- Publicación existente preservada como versión inmutable separada del borrador.
- Enlaces propios de dashboard que solo permiten acceder a la última publicación.
- Token aleatorio de 256 bits; solo su SHA-256 queda almacenado.
- Contraseña opcional con `PasswordHasher`, destinatario opcional, vencimiento opcional y revocación inmediata.
- Permisos independientes para filtros, exportación y uso embebido.
- Conteo y fecha del último acceso.
- Token ubicado en el fragmento `#` del navegador para evitar enviarlo en la URL solicitada al servidor.
- Gestión de enlaces desde `/app/informes/{dashboardId}/compartir`.
- Lector responsive en `/dashboards-compartidos` y configuración de marca en `/app/configuracion/marca`.

## Persistencia

Las migraciones `20260922182726_AddDashboardSharingAndBranding` y `20260922182817_EnforceAgencyBrandUniqueness` crean los enlaces, perfiles de marca e índices de unicidad. SQL revisable:

- `output/AddDashboardSharingAndBranding.sql`;
- `output/RemoveDashboardSharingAndBranding.sql`.

No se aplicaron a Neon.

## Verificación

Backend: 141 pruebas aprobadas y build sin errores ni advertencias. Frontend: lint sin errores y build con las rutas nuevas. EF confirmó que el modelo coincide con las migraciones.

## Pendiente para cerrar D6

El lector conserva el diseño de la publicación, pero todavía necesita un endpoint público de consultas limitado a las fuentes incluidas en esa publicación para mostrar cifras actuales sin revelar credenciales. También faltan las cinco plantillas iniciales instalables, aplicar el perfil de marca al lector y controles completos para vencimiento/permisos al crear el enlace. Estos puntos forman D6.2; D6 no se declara terminada todavía.
