# Autenticación y tenant de AnalitiAds

La Fase 3 usa autenticación propia por email y contraseña y entrega un access token JWT Bearer de corta duración.

## Configuración

La clave de firma no se versiona. Debe configurarse con una variable de entorno de al menos 32 caracteres:

```text
Authentication__Jwt__SigningKey
```

Issuer, audience y expiración se configuran bajo `Authentication:Jwt`. La expiración predeterminada es de 60 minutos.

## Flujo

1. `POST /api/v1/auth/register-agency` crea una agencia, su usuario administrador y la membresía inicial.
2. `POST /api/v1/auth/login` valida `agencySlug`, email y contraseña y entrega el token.
3. El cliente envía `Authorization: Bearer <token>`.
4. `GET /api/v1/auth/me` devuelve los IDs y el rol contenidos en la identidad autenticada.

El token contiene `userId`, `agencyId` y rol. La API nunca acepta un `AgencyId` enviado por el frontend para decidir el tenant.

## Roles

- `Admin`: lectura y administración de clientes y cuentas.
- `Planner`: lectura y administración de clientes y cuentas.
- `Viewer`: solo lectura.

La administración e invitación de miembros se añadirá en una tarea posterior de identidad. La creación inicial siempre asigna `Admin` dentro de la nueva agencia.

## Contraseñas

Las contraseñas requieren entre 12 y 128 caracteres. Se almacenan usando PBKDF2-SHA256 con salt aleatorio y formato versionado; nunca se devuelven por la API ni se escriben en logs.

## Frontend

El frontend debe guardar el token únicamente en una sesión segura. Para producción se recomienda que Next.js actúe como Backend for Frontend y conserve el token en una cookie `HttpOnly`, `Secure` y `SameSite`, en lugar de `localStorage`.
