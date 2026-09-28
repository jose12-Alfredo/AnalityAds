# D6.2 — datos públicos, plantillas y distribución

Fecha: 22 de septiembre de 2026.

## Alcance implementado

- Endpoint público de datos protegido por el mismo token, contraseña, destinatario, vencimiento y revocación del enlace.
- Autorización estructural: la fuente debe pertenecer al cliente del dashboard y estar referenciada en un componente de la versión publicada. Las métricas y la dimensión también deben estar configuradas en ese componente.
- Cuando `allowFilters` está desactivado, la consulta exige las fechas publicadas. Cuando está activado permite cambiar el intervalo hasta 90 días, sin ampliar fuentes, dimensiones ni métricas.
- Agregaciones que preservan valores ausentes y ratios CTR, CPC, CPM, CPA y ROAS calculados desde los totales del período.
- Lector público que conserva páginas, posiciones, tamaños, capas y estilos, aplica la marca de agencia o cliente y reutiliza las visualizaciones del editor.
- Controles al crear enlaces para vencimiento, contraseña, destinatario, filtros, exportación y embed.
- Configuración de marca general y por cliente. La marca del cliente prevalece en sus dashboards.
- Instalación idempotente de cinco plantillas editables: ventas, captación de leads, mensajes, reconocimiento de marca y resumen multicanal. Usan una ranura `primary` para evitar copiar por accidente una cuenta de otro cliente.

## Seguridad y límites

El token tiene 256 bits y en la base solo se almacena su SHA-256. Una revocación o vencimiento bloquea tanto la definición como sus datos. Las credenciales de los proveedores nunca forman parte de la respuesta pública.

El campo de destinatario es una barrera adicional que exige conocer el correo configurado, pero no demuestra propiedad del correo. La verificación real de identidad por correo requiere inicio de sesión o enlace mágico y queda para una fase posterior. `allowExport` ya se persiste y se entrega al lector; la generación de PDF/CSV corresponde a D7.

## Persistencia

D6.2 reutiliza las tablas y migraciones de D6.1. No añade cambios de esquema y no se aplicó ninguna migración a Neon.

## Verificación

- `dotnet build AnaliticAsd.sln --no-restore --verbosity:minimal`: correcto, 0 advertencias y 0 errores.
- `dotnet test AnaliticAsd.sln --no-restore`: 146 aprobadas, 0 fallidas. Cinco pruebas específicas verifican fuente publicada, métricas/dimensión permitidas y fechas con filtros habilitados o bloqueados.
- `npm run lint`: correcto.
- `npm run test:editor`: 6 aprobadas, 0 fallidas.
- `npm run build`: correcto; 19 páginas generadas.

Con autorización explícita del usuario, el 22 de septiembre de 2026 se aplicaron en Neon las siete migraciones pendientes, desde `20260917195257_AddGenericDataSources` hasta `20260922182817_EnforceAgencyBrandUniqueness`. Entity Framework confirmó que las 17 migraciones están registradas y que el modelo no tiene cambios pendientes. Se conserva `output/UpgradeNeonToD6.sql`, idempotente y de 30.760 bytes, como evidencia revisable.

El control posterior obtuvo `AdAccounts=5`, `MetaSources=5` y `MissingOrCrossClientLinks=0`. También consultó correctamente carpetas, dashboards, snapshots genéricos, agendas, enlaces y perfiles de marca. El worker inició contra Neon sin errores de esquema. Backend/OpenAPI y frontend respondieron HTTP 200.

## Correcciones de arranque

- En desarrollo, Data Protection guarda sus claves dentro de `.data-protection/`, excluida de Git, para no reutilizar claves DPAPI incompatibles de otro usuario o proceso de Rider.
- El registro de desarrollo usa consola y ya no depende de permisos del Event Log de Windows.
- El perfil HTTP de desarrollo no intenta redirigir a un puerto HTTPS inexistente.
- Si la tabla de agendas todavía no existe, el worker informa una sola vez que el esquema está atrasado y se deshabilita; el servidor permanece activo.
- Backend y frontend respondieron HTTP 200 en `http://localhost:5019/openapi/v1.json` y `http://localhost:3001` respectivamente.

## Próximo bloque

D7 debe implementar exportación PDF, CSV/Excel autorizado, generación en servidor y entregas programadas por correo. Después corresponde la validación integral en una base de prueba y con credenciales reales de Meta Ads, Google Ads, TikTok Ads y Google Analytics 4.
