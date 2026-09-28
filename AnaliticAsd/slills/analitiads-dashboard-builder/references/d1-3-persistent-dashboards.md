# D1.3 — dashboards persistentes

Fecha de cierre: 21 de septiembre de 2026.

## Resultado

El backend único de AnalitiAds incorpora dashboards editables separados de los reportes históricos inmutables. Cada dashboard pertenece a una agencia y un cliente, puede estar dentro de una carpeta y mantiene un borrador editable separado de sus publicaciones inmutables.

La definición JSON versión 1 persiste páginas, tamaño del lienzo, componentes, geometría, capas, bloqueo, grupos, referencias de fuentes, configuración y estilo. El servidor limita el documento a 1 MiB, 25 páginas, 250 componentes por página y 1500 componentes totales. También rechaza propiedades estructurales desconocidas, tipos de componente no admitidos, geometría fuera del lienzo, secretos dentro de configuración libre y fuentes que no pertenezcan al cliente.

## Persistencia

La migración `20260917212324_AddPersistentDashboards` crea:

- `dashboards`, con cliente, carpeta, metadatos, archivado y control de concurrencia;
- `dashboard_drafts`, con una revisión editable por dashboard;
- `dashboard_versions`, con publicaciones inmutables numeradas y hash SHA-256;
- `dashboard_templates`, con alcance de agencia o cliente y ranuras de fuente.

Las claves foráneas compuestas impiden relacionar dashboards, carpetas, borradores o publicaciones de clientes o agencias diferentes. Las plantillas de agencia tienen además una referencia obligatoria a la agencia aunque no tengan cliente.

El SQL revisable está en `output/AddPersistentDashboards.sql` y la reversión en `output/RemovePersistentDashboards.sql`. Esta migración no fue aplicada a Neon.

## Comportamiento y API

Las rutas implementadas permiten listar, crear, consultar, renombrar, mover, duplicar, archivar y restaurar dashboards. También permiten consultar y guardar el borrador, publicar y leer la publicación vigente, listar plantillas y crear plantillas de cliente o agencia.

El autosave debe enviar `expectedRevision`. Una revisión antigua recibe `409` y no sobrescribe el contenido más reciente. Publicar exige la revisión vigente, crea una copia inmutable y actualiza el número de publicación. Los cambios posteriores del borrador no alteran la publicación anterior.

Duplicar dentro del mismo cliente conserva las fuentes. Duplicar hacia otro cliente elimina todos los `dataSourceId` y los reemplaza por ranuras `rebind-source-N`. Un dashboard con ranuras sin asignar no puede publicarse. Las plantillas usan siempre ranuras y deben recibir asignaciones que pertenezcan al cliente de destino.

## Permisos

| Operación | Owner/Admin | Analyst asignado | Viewer | ClientViewer |
|---|---:|---:|---:|---:|
| Listar metadatos y plantillas | Sí | Sí | Sí | No |
| Leer publicación interna | Sí | Sí | Sí | No |
| Crear/editar/mover/duplicar | Sí | Sí | No | No |
| Leer/guardar borrador y publicar | Sí | Sí | No | No |
| Crear plantilla de cliente | Sí | Sí | No | No |
| Crear plantilla de agencia | Sí | No | No | No |

`Analyst` solo opera sobre clientes con una asignación vigente en base de datos. `ClientViewer` y futuros visitantes recibirán publicaciones mediante el mecanismo específico de enlaces compartidos de una fase posterior; no entran al explorador interno.

## Verificación

La suite cubre dominio, validación del esquema, fuentes ajenas, secretos, geometría, plantillas, duplicación entre clientes, revisión antigua, publicación inmutable, permisos, archivado y restauración. El flujo HTTP usa EF Core con SQLite relacional en memoria.

Resultado al cierre del bloque: 106 pruebas aprobadas, 0 fallidas. El modelo EF coincide con la migración generada. PostgreSQL/Neon sigue pendiente de una prueba sobre una rama o copia recuperable antes de cualquier aplicación compartida.

## Recuperación

Ante un conflicto de autosave, el cliente debe conservar la edición local, volver a cargar el borrador del servidor y pedir al editor que decida cómo reconciliar los cambios. Nunca debe reintentar silenciosamente con una revisión nueva.

La reversión de la migración elimina las cuatro tablas nuevas y sus datos. Solo debe considerarse en una base de prueba o con respaldo verificado; no forma parte del arranque normal.
