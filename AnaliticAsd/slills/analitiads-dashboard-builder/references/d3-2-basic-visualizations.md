# D3.2 — configuración visual y gráficos básicos

Fecha de cierre: 21 de septiembre de 2026.

## Resultado

El editor de AnalitiAds incorpora tabla, serie temporal, barras horizontales y columnas conectadas al contrato de consultas D3.1. Todos consultan snapshots Meta persistidos mediante el backend y guardan su configuración en el borrador del dashboard.

Los gráficos no contienen datos de demostración. Antes de configurar una fuente muestran un estado vacío; durante la consulta muestran carga; una respuesta sin filas se presenta como ausencia y un fallo conserva un mensaje recuperable.

## Componentes

### Tabla

- dimensión por fecha o campaña;
- hasta tres métricas compatibles;
- encabezados tomados del catálogo;
- formato de moneda, porcentaje, conteo o ratio;
- desplazamiento interno para resultados extensos.

### Serie temporal

- dimensión diaria obligatoria;
- hasta tres métricas de la misma unidad;
- líneas SVG escalables;
- paleta de series y leyenda configurable;
- orden cronológico ascendente o descendente.

La interfaz impide mezclar unidades en una misma serie para no dibujar valores incompatibles sobre una escala común.

### Barras y columnas

- dimensión por campaña;
- una métrica compatible;
- orden ascendente o descendente;
- límite entre 1 y 100 filas;
- color de serie configurable;
- etiquetas de campaña y valores formateados en barras.

## Configuración compartida

Los componentes de datos permiten guardar:

- título;
- fuente Meta activa del cliente;
- dimensión compatible;
- una o varias métricas según el gráfico;
- fecha inicial y final;
- cantidad de decimales entre 0 y 6;
- orden, métrica de orden y límite;
- color principal y visibilidad de leyenda.

Cambiar de dimensión elimina métricas incompatibles y selecciona una alternativa válida. Quitar la última métrica deja el componente en estado sin configurar y evita enviar una consulta inválida.

## Contrato de consulta ampliado

`POST /api/v1/dashboard-data/query` acepta ahora `sortMetric` y `sortDirection`. La métrica usada para ordenar debe formar parte de las métricas solicitadas y la dirección solo puede ser `asc` o `desc`.

Las filas por campaña se ordenan en el servidor antes de aplicar el límite. Las filas diarias se ordenan por fecha. El navegador se limita a representar la respuesta autorizada.

## Archivos principales

- `frontend/components/dashboard-visualizations.tsx`: tabla, líneas, barras, columnas y estados.
- `frontend/components/dashboard-editor.tsx`: inserción y panel de datos/presentación.
- `frontend/lib/dashboard-data.ts`: contratos tipados, consulta y formato.
- `AnaliticAsd/Application/DashboardQueries/DashboardQueryService.cs`: orden y límite autorizados.
- `AnaliticAsd/Tests/DashboardQueries/DashboardQueryServiceTests.cs`: reglas de consulta.

## Verificación

```text
npm run lint
Resultado: correcto.

npm run build
Resultado: correcto.

npm run test:editor
Resultado: 3 aprobadas, 0 fallidas.

dotnet test AnaliticAsd.sln --no-restore --logger "console;verbosity=minimal"
Resultado: 116 aprobadas, 0 fallidas, 0 omitidas.
```

D3.2 no modifica el modelo EF, no añade migraciones y no aplica cambios a Neon. Computer Use continúa sin exponer navegador, por lo que el recorrido visual con puntero y datos persistidos reales queda pendiente de un entorno con navegador y una base que tenga aplicadas las migraciones D1.

## Límites y siguiente incremento

D3.2 cubre los primeros gráficos. Siguen pendientes tabla dinámica, apiladas/100 %, circular/dona, área, combinado, embudo, dispersión/burbujas, medidor, imagen y separador. D3.3 implementará estos componentes reutilizando el catálogo y la consulta existentes; ampliará el contrato solo para las dimensiones necesarias que ya existan en los snapshots Meta.
