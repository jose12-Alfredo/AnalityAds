# Publicación del backend de AnalitiAds

El backend está en `AnaliticAsd/AnaliticAsd.csproj`, usa .NET 10 y PostgreSQL. Los secretos no se guardan en Git. `appsettings.Development.json`, los archivos de producción y los logs están excluidos por `.gitignore`.

## Verificación local

```powershell
dotnet restore AnaliticAsd.sln
dotnet build AnaliticAsd.sln --configuration Release --no-restore
dotnet test AnaliticAsd.sln --configuration Release --no-build
dotnet publish AnaliticAsd/AnaliticAsd.csproj --configuration Release --output artifacts/backend
```

GitHub Actions ejecuta estos pasos mediante `.github/workflows/backend-ci.yml`.

## Imagen Docker

Construir desde la raíz del repositorio:

```powershell
docker build -f AnaliticAsd/Dockerfile -t analitiads-backend .
```

El contenedor escucha en el puerto `8080` y ejecuta con `ASPNETCORE_ENVIRONMENT=Production`.

## Variables mínimas de producción

Configurar estas variables en el proveedor de nube. En ASP.NET Core, `__` representa `:`.

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__AnalitiAds=<cadena PostgreSQL/Neon>
Jwt__SigningKey=<clave aleatoria de al menos 32 bytes>
Jwt__Issuer=AnalitiAds
Jwt__Audience=AnalitiAds.Frontend
Cors__AllowedOrigins__0=https://app.tudominio.com
DataProtection__KeysPath=/var/analitiads/keys

Google__ClientId=<id OAuth>
Google__ClientSecret=<secreto OAuth>
GoogleAds__RedirectUri=https://api.tudominio.com/api/v1/integrations/google/google-ads/oauth/callback
GoogleAds__FrontendCallbackUrl=https://app.tudominio.com/app/configuracion/integraciones/google-ads
GoogleAnalytics4__RedirectUri=https://api.tudominio.com/api/v1/integrations/google/ga4/oauth/callback
GoogleAnalytics4__FrontendCallbackUrl=https://app.tudominio.com/app/configuracion/integraciones/ga4
```

La ruta de `DataProtection__KeysPath` debe apuntar a un volumen persistente y escribible por el contenedor. Estas claves protegen los estados y credenciales OAuth almacenados; si se pierden durante un reinicio, las integraciones existentes tendrán que autorizarse nuevamente. En despliegues con varias instancias, todas deben compartir el mismo almacén de claves.

`GoogleAds__DeveloperToken` puede quedar vacío. `GoogleAds__LoginCustomerId` solo se configura cuando la estructura de la cuenta administradora lo requiera y se escribe sin guiones.

Meta, TikTok, SMTP y otros proveedores requieren sus propios secretos cuando se habiliten. El archivo `AnaliticAsd/appsettings.Production.example.json` enumera la estructura sin contener valores reales.

## Base de datos

La aplicación no ejecuta migraciones automáticamente en producción. Antes de iniciar una versión nueva, aplicar las migraciones desde un entorno protegido que tenga acceso a la base de datos:

```powershell
dotnet ef database update --project AnaliticAsd/AnaliticAsd.csproj
```

Realizar un respaldo antes de cada migración de producción.

## OAuth en producción

Registrar en cada proveedor la URL HTTPS exacta del callback. Para Google Ads:

```text
https://api.tudominio.com/api/v1/integrations/google/google-ads/oauth/callback
```

Al cambiar de cliente OAuth, las conexiones creadas con el cliente anterior deben autorizarse nuevamente.

## Comprobación después del despliegue

La ruta de preparación comprueba la conexión con PostgreSQL:

```text
GET https://api.tudominio.com/api/v1/readiness
```

Una respuesta `200` confirma que el proceso está activo y puede conectarse a la base de datos. Una respuesta `503` indica que la aplicación está activa pero PostgreSQL no está disponible.
