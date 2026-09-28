# D3.1 — catálogo y contrato de consultas

Fecha de cierre: 21 de septiembre de 2026.

## Resultado

AnalitiAds incorpora una capa de consulta para componentes que se ejecuta en el backend y respeta el alcance autorizado de agencia, cliente y fuente. El primer adaptador utiliza los snapshots Meta persistidos. El editor nunca consulta Meta directamente ni recalcula ratios en el navegador.

Una tarjeta KPI puede seleccionar una fuente Meta activa del cliente, una métrica compatible y un intervalo. Su configuración queda guardada dentro de la definición del dashboard y el valor se obtiene del endpoint de consultas. Cuando el dato no es válido o no está disponible, muestra `—`.

## Rutas

| Método | Ruta | Función |
|---|---|---|
| `GET` | `/api/v1/dashboard-data/catalog?provider=MetaAds` | Catálogo de métricas y dimensiones. |
| `GET` | `/api/v1/dashboard-data/clients/{clientId}/sources` | Fuentes visibles del cliente autorizado. |
| `POST` | `/api/v1/dashboard-data/query` | Consulta validada para un componente. |

Las rutas aceptan Owner, Admin, Analyst y Viewer. `AuthorizedData` vuelve a comprobar en la base el acceso del usuario. Un ID de fuente de otra agencia, cliente no asignado o fuente inexistente responde como recurso no encontrado.

## Consulta v1

La petición contiene `clientId`, `dataSourceId`, `since`, `until`, `dimension`, entre una y diez `metrics`, filtros opcionales de campaña y `limit`. El intervalo Meta está limitado a 90 días en este incremento.

Dimensiones iniciales:

- `none`: total del período;
- `date`: una fila por día;
- `campaign`: una fila por campaña.

Métricas Meta iniciales:

- inversión, impresiones, alcance y frecuencia;
- clics en enlace;
- leads, compras y valor de compras;
- CPM, CTR, CPC, costo por lead, costo por compra y ROAS.

El catálogo devuelve nombre, definición, proveedor, unidad, agregación, tipo, dimensiones compatibles y limitaciones. Google Ads, TikTok Ads y GA4 figuran como proveedores planificados, pero sus catálogos y consultas permanecen vacíos hasta implementar sus adaptadores reales.

## Semántica protegida

- Los ratios se calculan con los totales compatibles del período.
- Un denominador cero produce `Undefined` y valor `null`.
- El alcance y la frecuencia solo se permiten con dimensión diaria; no se suman entre días ni campañas.
- Inversión y valor de compras no se agregan cuando los snapshots contienen monedas diferentes.
- Ausencia, dato incompleto, legado normalizado, moneda mixta y ratio indefinido conservan estados distintos.
- Los clics disponibles se llaman explícitamente `linkClicks`; no se presentan como todos los clics.
- Compras y conversiones atribuidas no se presentan como personas únicas.
- Un filtro que contiene una campaña ajena a la fuente se rechaza.

## Frescura

La sincronización Meta existente ahora llama `DataSource.RecordSynchronization`. De este modo amplía `AvailableSince`/`AvailableUntil` y actualiza `LastSyncedAtUtc` después de persistir métricas. El endpoint devuelve moneda, zona horaria y última sincronización para que la interfaz no presente datos antiguos como recientes.

## Frontend

El panel de propiedades de un KPI permite seleccionar:

- cuenta Meta activa asignada al cliente;
- métrica compatible con el total del período;
- fecha inicial y final.

La tarjeta muestra números reales devueltos por el backend con formato de moneda, porcentaje, conteo o ratio. Un resultado diferente de `CompleteForSnapshots` se muestra como `—`, acompañado del error cuando la consulta falla. El nombre de la métrica y la fecha de actualización aparecen en el editor.

## Pruebas y verificación

Se añadieron ocho pruebas backend: siete del catálogo/servicio y una del repositorio relacional. Cubren catálogo no aditivo, ratios de totales, denominador cero, moneda mixta, incompatibilidad alcance/período, fuente no autorizada, campaña ajena y aislamiento entre agencias/clientes.

```text
dotnet build AnaliticAsd.sln --no-restore --verbosity:minimal
Resultado: correcto, 0 advertencias, 0 errores.

dotnet test AnaliticAsd.sln --no-restore --logger "console;verbosity=minimal"
Resultado: 114 aprobadas, 0 fallidas, 0 omitidas.

npm run test:editor
Resultado: 3 aprobadas, 0 fallidas.

npm run lint
Resultado: correcto.

npm run build
Resultado: correcto.

dotnet ef migrations has-pending-model-changes --project AnaliticAsd --startup-project AnaliticAsd --no-build
Resultado: no hay cambios de modelo pendientes.
```

D3.1 no añade migraciones y no modifica Neon.

## Límites y siguiente incremento

El editor solo configura consultas de datos en tarjetas KPI. El backend ya acepta `date` y `campaign`, pero sus resultados todavía no se dibujan como tablas o gráficos. D3.2 añadirá configuración común de datos y presentación, tabla, serie temporal y barras/columnas sobre este mismo contrato. Comparaciones, filtros de alcance página/dashboard y fórmulas pertenecen a D5.
