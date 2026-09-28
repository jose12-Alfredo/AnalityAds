# D1.2 — Carpetas y editores asignados

Fecha de cierre local: 17 de septiembre de 2026.

## Resultado

Cada cliente dispone de un árbol persistente e independiente de carpetas y subcarpetas. Owner y Admin administran los editores de la agencia; un Analyst solo puede consultar y modificar recursos de los clientes que tenga asignados. Las asignaciones se consultan en la base en cada operación y no se incorporan al JWT, por lo que una revocación bloquea inmediatamente un token ya emitido.

`ClientEditorAssignment` es independiente de `ClientAccess`. El primero concede trabajo interno a miembros `Analyst`; el segundo continúa siendo el acceso externo y limitado de `ClientViewer`.

## Permisos

| Rol | Ver carpetas | Modificar carpetas | Administrar editores | Administrar clientes |
|---|---:|---:|---:|---:|
| Owner | Sí, toda su agencia | Sí | Sí | Sí |
| Admin | Sí, toda su agencia | Sí | Sí | Sí |
| Analyst | Solo clientes asignados | Solo clientes asignados | No | No |
| Viewer | Sí, toda su agencia | No | No | No |
| ClientViewer | No | No | No | No |

Los cruces de agencia y los clientes no asignados se ocultan con `404`. Un rol insuficiente dentro de una ruta conocida recibe `403`. Un ciclo, una versión antigua, un padre archivado o un movimiento entre clientes recibe `409`.

## Contrato HTTP

### Carpetas

```text
GET    /api/v1/clients/{clientId}/folders?includeArchived=false&search=
POST   /api/v1/clients/{clientId}/folders
PATCH  /api/v1/folders/{folderId}
POST   /api/v1/folders/{folderId}/move
DELETE /api/v1/folders/{folderId}?expectedVersion={guid}
POST   /api/v1/folders/{folderId}/restore
```

Crear:

```json
{
  "name": "Campañas",
  "parentFolderId": null
}
```

Renombrar:

```json
{
  "name": "Campañas 2026",
  "expectedVersion": "11111111-1111-1111-1111-111111111111"
}
```

Mover u ordenar:

```json
{
  "parentFolderId": null,
  "sortOrder": 0,
  "expectedVersion": "11111111-1111-1111-1111-111111111111"
}
```

Restaurar:

```json
{
  "expectedVersion": "11111111-1111-1111-1111-111111111111"
}
```

Cada respuesta incluye `version`. El cliente debe enviar la última versión recibida al renombrar, mover, archivar o restaurar. El archivado y la restauración afectan recursivamente a los descendientes. Una carpeta hija no puede restaurarse mientras su padre continúe archivado.

### Editores internos

```text
GET    /api/v1/agency/editors
GET    /api/v1/clients/{clientId}/editors
POST   /api/v1/clients/{clientId}/editors
DELETE /api/v1/clients/{clientId}/editors/{userId}?expectedVersion={guid}
```

Asignar:

```json
{
  "userId": "22222222-2222-2222-2222-222222222222"
}
```

Solo se puede asignar un usuario activo que ya tenga membresía `Analyst` en la misma agencia. No se aceptan usuarios de otras agencias ni miembros Owner, Admin, Viewer o ClientViewer.

## Persistencia y migración

La migración `20260917202057_AddFoldersAndEditorAssignments` crea:

- `folders`, con claves compuestas que impiden padres de otro cliente o agencia;
- `client_editor_assignments`, con claves hacia cliente, membresía interna y usuario otorgante;
- índices para navegación, archivado y búsqueda por espacio de trabajo;
- tokens de concurrencia en carpetas y asignaciones.

Antes de esta migración, un Analyst podía consultar todos los clientes de su agencia. Para mantener compatibilidad, la migración materializa ese acceso como asignaciones explícitas a todos los clientes existentes. Los Analyst creados después de la migración comienzan sin clientes y deben ser asignados por Owner/Admin.

SQL generado y revisable:

```text
output/AddFoldersAndEditorAssignments.sql
output/RemoveFoldersAndEditorAssignments.sql
```

La reversión elimina carpetas y asignaciones nuevas. No debe ejecutarse después de comenzar a usar estas funciones sin exportar o respaldar sus datos.

## Verificación local

```text
dotnet build AnaliticAsd.sln --no-restore --verbosity=minimal
Resultado: 0 errores, 0 advertencias.

dotnet test AnaliticAsd.sln --no-restore --logger "console;verbosity=minimal"
Resultado: 97 aprobadas, 0 fallidas, 0 omitidas.

dotnet ef migrations has-pending-model-changes --project AnaliticAsd --startup-project AnaliticAsd --no-build
Resultado: el modelo coincide con la última migración.
```

Las pruebas cubren asignación y revocación con JWT existente, aislamiento de clientes y agencias, roles, búsqueda, jerarquía, ciclos, movimientos entre clientes, archivado/restauración recursivos, versiones antiguas y claves foráneas compuestas.

La migración no fue aplicada a Neon. Como este entorno no dispone de Docker ni `psql`, el SQL todavía debe ejecutarse primero en una rama o copia aislada de PostgreSQL.
