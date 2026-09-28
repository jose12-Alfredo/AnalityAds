# D2.1 — núcleo del editor visual

Fecha de cierre: 21 de septiembre de 2026.

## Resultado

AnalitiAds incorpora un editor visual persistente en `/app/informes/{dashboardId}/editar`. Owner, Admin y Analyst pueden abrir el borrador de un dashboard autorizado; Viewer y ClientViewer no pueden editarlo. El servidor sigue siendo la autoridad para agencia, cliente, rol y revisión.

El editor trabaja sobre la primera página del esquema v1 y guarda la definición completa en PostgreSQL mediante el endpoint de borrador de D1.3. Los elementos se posicionan en coordenadas lógicas del documento, por lo que su composición se conserva aunque el lienzo se muestre a otro tamaño.

## Funciones implementadas

- inserción de texto, tarjeta KPI sin datos inventados y forma;
- selección individual desde el lienzo;
- movimiento y redimensionamiento con puntero;
- cuadrícula de 12 px activable y ajuste a la cuadrícula;
- edición numérica de posición y tamaño;
- edición de texto o título y color de fondo;
- traer al frente y enviar al fondo;
- bloqueo y desbloqueo;
- duplicación y eliminación segura de elementos;
- deshacer y rehacer, con atajos `Ctrl+Z`, `Ctrl+Y` y `Ctrl+Shift+Z`;
- autoguardado con espera breve, indicador de estado y revisión del servidor;
- conservación local de los cambios ante un conflicto `409`;
- recarga explícita de la versión del servidor para resolver un conflicto;
- acceso directo desde el inspector de `/app/informes`.

Una tarjeta KPI nueva muestra `—` y “Selecciona una métrica”. D2.1 no simula resultados publicitarios porque el catálogo y el motor de consultas pertenecen a D3.

## Persistencia y concurrencia

Cada cambio genera una nueva definición válida y deja el borrador pendiente. El autoguardado envía `expectedRevision`; una respuesta correcta actualiza la revisión local. Si hubo cambios mientras la petición estaba en curso, el editor vuelve a guardar sobre la nueva revisión.

Ante `409`, el editor no reintenta con sobrescritura ni descarga automáticamente el borrador remoto. Conserva la composición local en pantalla, informa el conflicto y exige una decisión explícita antes de descartarla y recargar el servidor.

El historial de edición se limita a 50 estados en memoria. El servidor almacena el borrador vigente; el historial completo de deshacer/rehacer no persiste al cerrar el navegador.

## Archivos principales

- `frontend/components/dashboard-editor.tsx`: interacción, historial, propiedades y autoguardado.
- `frontend/app/app/informes/[dashboardId]/editar/page.tsx`: ruta privada del editor.
- `frontend/lib/dashboards.ts`: tipos v1 y operaciones de lectura/guardado.
- `frontend/components/dashboard-workspace.tsx`: entrada al editor.
- `frontend/app/globals.css`: workbench, lienzo y panel de propiedades.

## Verificación

```text
npm run lint
Resultado: correcto.

npm run build
Resultado: correcto; TypeScript y /app/informes/[dashboardId]/editar aprobados.

dotnet test AnaliticAsd.sln --no-restore --logger "console;verbosity=minimal"
Resultado: 106 aprobadas, 0 fallidas, 0 omitidas.
```

La revisión visual automatizada sigue pendiente porque el entorno no expuso un navegador en la comprobación previa. No se aplicó ninguna migración a Neon y D2.1 no añade migraciones.

## Límite y siguiente bloque

D2.1 es el núcleo de una sola página y selección individual. D2.2 añadirá administración de varias páginas, selección múltiple, agrupación, alineación y distribución, copiar/pegar, zoom, vista previa y guías de alineación. Después D3 conectará los componentes a fuentes, dimensiones, métricas y visualizaciones reales.
