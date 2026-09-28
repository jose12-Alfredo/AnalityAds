# D1.4 — explorador frontend persistente

Fecha de cierre: 21 de septiembre de 2026.

## Resultado

El frontend incorpora `/app/informes`, un espacio interno en español para trabajar con los dashboards persistentes de D1.3. La navegación principal muestra “Informes” a Owner, Admin, Analyst y Viewer. `ClientViewer` no entra al explorador interno.

La pantalla usa tres áreas relacionadas: árbol de carpetas, informes de la carpeta e inspector del informe. Al cambiar de cliente se limpian carpeta, informe, borrador y publicación seleccionados para evitar que quede estado visual de otro cliente.

## Funciones implementadas

- selección de cliente dentro del alcance devuelto por el servidor;
- navegación por carpetas y subcarpetas;
- creación, cambio de nombre, archivado y restauración de carpetas;
- búsqueda y visualización opcional de dashboards archivados;
- creación de un dashboard con página vacía persistente;
- actualización de título y descripción;
- movimiento entre carpetas del mismo cliente;
- duplicación dentro del cliente o hacia otro cliente;
- archivado y restauración;
- consulta de revisión, páginas, componentes y esquema del borrador;
- publicación explícita de la revisión vigente;
- presentación del número, fecha y hash de la publicación inmutable;
- mensajes específicos para falta de permiso, recurso ausente y conflicto `409`;
- comportamiento adaptable para escritorio, tablet y móvil.

La copia entre clientes consume la protección del backend: las fuentes originales se sustituyen por ranuras pendientes de reasignación. La interfaz informa este resultado y no presenta la copia como lista para publicar.

## Archivos principales

- `frontend/lib/dashboards.ts`: contratos tipados, permisos, fechas y mensajes de error.
- `frontend/components/dashboard-workspace.tsx`: estado y operaciones del explorador.
- `frontend/app/app/informes/page.tsx`: ruta privada.
- `frontend/components/private-shell.tsx`: entrada de navegación.
- `frontend/app/globals.css`: diseño responsive del espacio de trabajo.

## Verificación

```text
npm run lint
Resultado: correcto.

npm run build
Resultado: correcto; TypeScript y generación de /app/informes aprobados.

dotnet build AnaliticAsd.sln --no-restore -p:BaseOutputPath=.../tmp/verify-bin/
Resultado: correcto, 0 advertencias, 0 errores.

dotnet test AnaliticAsd.sln --no-build --no-restore
Resultado: 106 aprobadas, 0 fallidas, 0 omitidas.
```

La inspección visual automatizada no pudo iniciarse porque el entorno de Computer Use no expuso ningún navegador. El build de producción verifica el render y los tipos, pero la revisión visual interactiva queda como una comprobación local adicional.

## Límite del bloque

Este explorador administra estructura, borrador y publicación, pero todavía no es el editor visual. El lienzo, arrastre, redimensionamiento, selección múltiple, capas, deshacer/rehacer y autosave visual pertenecen a D2. La gestión visual avanzada de plantillas y la reasignación guiada de ranuras también se completarán junto al editor.
