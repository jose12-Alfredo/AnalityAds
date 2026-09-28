# D5 — sincronización operativa y consultas multicanal

Fecha: 22 de septiembre de 2026.

## Implementado

- Programación persistente por fuente con intervalo de 15 minutos a 7 días y ventana móvil de 1 a 90 días.
- `ProviderSyncWorker` ejecuta trabajos en el servidor aunque no haya un navegador abierto.
- Bloqueo recuperable de 15 minutos, reintento exponencial acotado, contador de fallos, último inicio, último éxito y código de error sanitizado.
- Sincronización idempotente sobre la clave fuente/fecha/dimensión y refresco configurable de días recientes.
- Catálogo y consultas para Meta Ads, Google Ads, TikTok Ads y GA4.
- Totales, fecha y campaña; filtros explícitos por valores de dimensión; orden y límite.
- CTR, CPC, CPM, CPA y ROAS calculados desde totales compatibles, con división por cero como ausencia.
- Comparación con el período inmediatamente anterior.
- Unión multicanal de dos a diez fuentes como series separadas por plataforma. No afirma deduplicación de personas o conversiones.
- Rechazo de suma monetaria cuando existen monedas diferentes.
- Controles del editor para todas las fuentes activas, filtro, período anterior y segunda fuente.
- Pantallas de integración con sincronización manual y activación de programación diaria.

## Persistencia

La migración `20260922162515_AddProviderSyncSchedules` crea `provider_sync_schedules`. SQL revisable:

- `output/AddProviderSyncSchedules.sql`;
- `output/RemoveProviderSyncSchedules.sql`.

No se aplicó a Neon. D1, D4 y D5 deben probarse en orden en una copia recuperable antes de tocar la base compartida.

## Verificación

```text
dotnet build: 0 advertencias, 0 errores
dotnet test: 138 aprobadas, 0 fallidas, 0 omitidas
dotnet ef migrations has-pending-model-changes: ninguno
npm run lint: correcto
npm run test:editor: 6 aprobadas
npm run build: correcto
```

Las pruebas nuevas comprueban cálculo de ratios Google desde totales y recuperación/reprogramación de trabajos fallidos. La verificación real programada contra Google Ads, TikTok Ads y GA4 requiere credenciales, aprobaciones y aplicar las migraciones en una base de prueba.

## Límites explícitos

Las fórmulas disponibles en este corte son las confirmadas CTR, CPC, CPM, CPA y ROAS. Un editor libre de expresiones arbitrarias requiere un lenguaje aislado y queda para una ampliación posterior; no se ejecuta código proporcionado por usuarios. TikTok usa páginas de hasta 1000 filas por consulta D4 y GA4 hasta 100000; la validación con cuentas que excedan esos límites sigue pendiente de credenciales reales.

## Próximo bloque

D6 implementará branding, temas y plantillas iniciales; publicación explícita de dashboards; enlaces seguros con expiración, contraseña/destinatarios y revocación inmediata; permisos de filtros/exportación y vista embebible equivalente.
