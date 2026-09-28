# D8 — calidad y producción

Fecha: 23 de septiembre de 2026.

## Alcance terminado en código

- Ocho visualizaciones persistentes: mapa, bala, árbol, Sankey, cascada, caja y bigotes, velas y línea de tiempo.
- Editor con barra avanzada y representación equivalente en el lector publicado.
- El mapa declara su carácter relativo y no presenta campañas como ubicaciones geográficas reales.
- Movimiento por teclado con flechas y pasos de diez píxeles con Shift; los bloqueados no se mueven.
- Etiquetas accesibles, foco visible, alto contraste y respeto por `prefers-reduced-motion`.
- `GET /api/system/readiness` comprueba PostgreSQL y devuelve 503 cuando no está listo.
- Correlación y cabeceras `nosniff`, política de referencia y permisos restringidos.
- Prueba de carga Windows reproducible en `scripts/smoke-load.ps1`.

## Verificación

- Backend: build limpio; 151 pruebas aprobadas.
- Frontend: 6 pruebas, ESLint y build de producción de 19 rutas aprobados.
- Carga local: 200 solicitudes, concurrencia 10, promedio 8,44 ms, p95 87,52 ms y máximo 233,02 ms.
- Neon conserva 18 migraciones; D8 no cambia persistencia.

## Extensiones y límites externos

No se carga JavaScript remoto, `eval` ni HTML arbitrario. Las visualizaciones son tipos compilados y
validados por backend. Una futura extensión de terceros necesitará revisión, firma, aislamiento y un
contrato limitado; no se presenta como disponible.

El código D8 está completo. La aceptación de producción general depende de credenciales reales de
Google Ads, TikTok Ads y GA4, contraste de cifras, SMTP real y revisión visual manual en navegadores
y tamaños de pantalla. Estas dependencias no pueden sustituirse con simulaciones.
