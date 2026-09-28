# D2.2 — herramientas avanzadas del editor

Fecha de cierre de implementación: 21 de septiembre de 2026.

## Resultado

El editor persistente de AnalitiAds permite construir informes con varias páginas y organizar conjuntamente los componentes de cada lienzo. La definición continúa usando el esquema v1 validado por el backend y el autoguardado con revisión de D2.1.

## Funciones implementadas

### Páginas

- crear hasta 25 páginas;
- renombrar, duplicar, reordenar y eliminar;
- impedir que el dashboard quede sin páginas;
- confirmar la eliminación de una página que contiene componentes;
- conservar tamaños, fondo, componentes y grupos al duplicar;
- editar ancho entre 320 y 4096 px y alto entre 320 y 20 000 px;
- impedir que una reducción de lienzo deje componentes fuera de sus límites;
- cambiar el fondo de cada página.

### Selección y organización

- selección acumulativa con `Shift`;
- selección mediante rectángulo sobre el lienzo;
- movimiento conjunto;
- alineación horizontal y vertical en seis direcciones;
- distribución horizontal y vertical;
- agrupación y desagrupación mediante `groupId`;
- selección automática de un grupo al elegir uno de sus miembros;
- bloqueo de elementos y selección;
- traer al frente y enviar al fondo;
- panel de capas con estado de selección y bloqueo.

### Productividad y presentación

- copiar, cortar, pegar y duplicar dentro de la sesión del editor;
- `Ctrl+Z`, `Ctrl+Shift+Z`, `Ctrl+Y`, `Ctrl+C`, `Ctrl+X`, `Ctrl+V`, `Ctrl+D`, `Delete` y `Escape`;
- zoom de 25 % a 150 % sin modificar las coordenadas guardadas;
- guías visuales durante el movimiento;
- vista previa limpia del borrador y navegación entre sus páginas;
- advertencia al abandonar el editor con cambios pendientes;
- recuperación explícita ante conflicto de revisión.

El portapapeles es interno al editor y no copia secretos ni datos al portapapeles del sistema. Los componentes pegados reciben IDs nuevos y los grupos reciben un nuevo `groupId`.

## Organización técnica

Las transformaciones puras quedaron en `frontend/lib/dashboard-editor-model.ts`. Allí se concentran normalización de páginas, selección de componentes, alineación, distribución, agrupación, eliminación y duplicación. El componente visual conserva carga, interacción, autoguardado y render.

Esta separación permite que D3 añada consultas y gráficos sin duplicar reglas geométricas ni de historial.

## Verificación ejecutada

```text
npm run test:editor
Resultado: 3 aprobadas, 0 fallidas.

npm run lint
Resultado: correcto.

npm run build
Resultado: correcto; TypeScript y la ruta dinámica del editor aprobados.

dotnet test AnaliticAsd.sln --no-restore --logger "console;verbosity=minimal"
Resultado: 106 aprobadas, 0 fallidas, 0 omitidas.
```

Las pruebas del modelo cubren alineación, distribución, protección de elementos bloqueados al eliminar, duplicación con IDs nuevos y conservación segura de grupos al duplicar una página.

Computer Use devolvió `apps: []` y `browsers: []`; por eso no se realizó inspección visual automatizada. El build verifica render estático y tipos, pero la interacción real con puntero y la persistencia contra PostgreSQL todavía deben recorrerse en un navegador cuando haya uno disponible.

## Datos y despliegue

D2.2 no añade migraciones. Las migraciones D1.1, D1.2 y D1.3 continúan sin aplicarse a Neon. Para una prueba integral de cerrar y reabrir el editor, primero deben aplicarse en una rama o copia recuperable y ejecutar el control descrito en `current-progress.md`.

## Siguiente incremento

D3.1 creará el catálogo normalizado de dimensiones y métricas, las reglas de compatibilidad y el contrato de consulta de componentes. Su primer adaptador consultará datos Meta persistidos; una tarjeta KPI dejará de mostrar `—` solo cuando tenga una fuente, métrica y resultado reales.
