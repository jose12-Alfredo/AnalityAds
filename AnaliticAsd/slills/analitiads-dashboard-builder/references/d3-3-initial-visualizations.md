# D3.3 — visualizaciones iniciales restantes

Fecha de cierre: 21 de septiembre de 2026.

## Resultado

El editor persistente incorporó los componentes iniciales que faltaban y los conecta al contrato de consultas Meta de D3.1. Todos conservan estados explícitos de carga, ausencia y error y utilizan únicamente fuentes autorizadas del cliente.

Se añadieron:

- tabla dinámica transpuesta por métrica;
- barras apiladas y apiladas al 100 %;
- circular y dona;
- área;
- combinado de columnas y línea;
- embudo;
- dispersión y burbujas;
- medidor de cumplimiento con objetivo configurable;
- imagen mediante dirección HTTPS y descripción accesible;
- separador con color y grosor configurables.

También se corrigieron los nombres persistidos de barras, columnas y separador para que coincidan exactamente con `DashboardComponentType` del backend: `horizontalBar`, `column` y `divider`. El backend tiene cobertura automática para todos los nombres incorporados en D3.3.

## Compatibilidad de datos

La configuración limita cada componente a datos que el runtime actual puede representar:

- área, serie y combinado requieren fecha;
- circular, dona, embudo, dispersión, burbujas y gráficos apilados requieren campaña;
- medidor e indicador consultan el total del período;
- tabla y tabla dinámica permiten fecha o campaña;
- serie y apilados solo permiten combinar métricas con la misma unidad;
- combinado admite dos unidades porque cada serie se escala de forma independiente;
- dispersión usa dos métricas y burbujas usa hasta tres;
- los valores ausentes continúan mostrándose como ausencia y no como cero textual.

La tabla dinámica actual transpone una dimensión y hasta tres métricas. Una tabla dinámica con dos dimensiones reales requiere ampliar el motor de consultas con una dimensión de desglose; queda para D5 junto con filtros y combinación de datos.

Las imágenes se cargan solo desde HTTPS. D3.3 no incorpora todavía carga de archivos ni almacenamiento propio; esa decisión corresponde a D6. Una URL de imagen queda persistida en la definición del dashboard.

## Archivos principales

- `frontend/components/dashboard-editor.tsx`
- `frontend/components/dashboard-visualizations.tsx`
- `frontend/lib/dashboard-chart-model.ts`
- `frontend/lib/dashboard-chart-model.test.ts`
- `frontend/lib/dashboards.ts`
- `frontend/app/globals.css`
- `AnaliticAsd/Tests/Dashboards/DashboardDefinitionValidatorTests.cs`

## Verificación ejecutada

```text
npm run test:editor
Resultado: 6 aprobadas, 0 fallidas.

npm run lint
Resultado: correcto.

npm run build
Resultado: compilación de producción correcta.

dotnet test AnaliticAsd.sln --no-restore --logger "console;verbosity=minimal"
Resultado: 131 aprobadas, 0 fallidas, 0 omitidas.
```

La restauración de paquetes se repitió desde NuGet porque el sandbox no tenía todos los paquetes en caché; no se añadieron dependencias ni se cambiaron versiones. Backend y frontend se comprobaron después en `http://localhost:5019/api/system/status` y `http://localhost:3001/login`, ambos con respuesta 200.

D3.3 no cambia el modelo EF, no crea migraciones y no modifica Neon. No se realizó inspección visual automatizada porque Computer Use no expuso un navegador.

## Siguiente bloque

D4 — conectores reales de Google Ads, TikTok Ads y Google Analytics 4. Antes de programar cada adaptador se debe verificar la documentación oficial vigente, requisitos de producción, OAuth, permisos, renovación, límites y campos disponibles. Meta se conserva y se valida dentro de la abstracción genérica.
