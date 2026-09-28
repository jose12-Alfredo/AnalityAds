# D4 — conectores reales de Google Ads, TikTok Ads y GA4

Fecha: 22 de septiembre de 2026.

## Resultado implementado

D4 mantiene Meta Ads y añade adaptadores separados para Google Ads, TikTok Ads y Google Analytics 4. Cada integración ofrece autorización, callback protegido, credenciales cifradas mediante ASP.NET Data Protection, estado, descubrimiento remoto y asignación de una cuenta o propiedad a un cliente.

Owner/Admin autorizan o reconectan; Owner/Admin/Analyst descubren, asignan y sincronizan; Viewer consulta el estado. Las fuentes se validan por agencia y cliente. Los tokens no se devuelven al navegador.

La sincronización manual acepta hasta 367 días y guarda datos diarios en `provider_metric_snapshots`. La clave única `(data_source_id, date, dimension_key)` permite repetir el intervalo sin duplicarlo. Google Ads y TikTok guardan campaña, inversión, impresiones, clics, conversiones y valor; GA4 guarda usuarios activos, sesiones y vistas. Los valores ausentes permanecen `NULL`.

El frontend añadió las rutas `/app/configuracion/integraciones/google-ads`, `/app/configuracion/integraciones/tiktok-ads` y `/app/configuracion/integraciones/ga4`, con conexión, descubrimiento, asignación y sincronización de 30 días. Meta conserva su flujo existente.

## Configuración externa

Configurar con user-secrets o variables de entorno:

```text
Google:ClientId
Google:ClientSecret
GoogleAds:DeveloperToken
GoogleAds:LoginCustomerId              # opcional según la estructura
TikTokAds:AppId
TikTokAds:AppSecret
```

Google requiere cliente OAuth web, redirects exactos, Google Ads API, Analytics Admin API y Analytics Data API habilitadas, acceso aprobado y usuarios autorizados. TikTok requiere una aplicación TikTok for Business aprobada, redirect registrado y anunciantes autorizados para informes. Producción debe usar HTTPS.

## Persistencia y verificación

La migración `20260922151443_AddProviderMetricSnapshots` crea la tabla normalizada. Sus SQL están en `output/AddProviderMetricSnapshots.sql` y `output/RemoveProviderMetricSnapshots.sql`. No se aplicó a Neon.

```text
Backend build: 0 advertencias, 0 errores.
Backend tests: 135 aprobadas, 0 fallidas, 0 omitidas.
EF pending model changes: ninguno.
Frontend: lint, 6 pruebas y build aprobados.
```

No se ejecutó OAuth ni sincronización contra cuentas reales porque el entorno no contiene credenciales ni aprobaciones de Google/TikTok. D4 está terminado en código y verificación local, pero su aceptación externa sigue bloqueada. No se presenta como verificado en producción.

## Siguiente fase

D5 añadirá trabajos persistentes, programación sin navegador, reintentos, recuperación, paginación completa, auditoría y refresco reciente. También conectará `provider_metric_snapshots` al catálogo multicanal del editor con filtros, fórmulas y reglas de moneda, atribución y ausencia.
