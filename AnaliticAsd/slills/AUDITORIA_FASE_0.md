# Auditoría de Fase 0

Fecha: 3 de septiembre de 2026.

## Alcance

Se auditó únicamente la solución autorizada `AnaliticAsd.sln` y su proyecto `AnaliticAsd/AnaliticAsd.csproj`. La solución paralela de la raíz quedó fuera del análisis funcional.

## Inventario

```text
AnaliticAsd.sln
└── AnaliticAsd/
    ├── AnaliticAsd.csproj
    ├── Program.cs
    ├── WeatherForecast.cs
    ├── Controllers/
    │   └── WeatherForecastController.cs
    ├── Properties/
    │   └── launchSettings.json
    ├── appsettings.json
    ├── appsettings.Development.json
    ├── AnaliticAsd.http
    └── slills/
```

## Estado técnico

- Stack: C#, ASP.NET Core Web API y .NET 10.
- Proyectos en la solución: uno.
- Referencias entre proyectos: ninguna.
- Backend: plantilla mínima con controladores y OpenAPI en desarrollo.
- Endpoint existente: `GET /weatherforecast` con datos aleatorios de ejemplo.
- Frontend: no existe.
- Base de datos y persistencia: no existen.
- Entity Framework Core y PostgreSQL: no instalados.
- Autenticación y autorización real: no existen. `UseAuthorization` está presente, pero no hay autenticación, políticas ni endpoints protegidos.
- Multi-tenancy: no existe.
- Meta Marketing API: no existe.
- Integración con Claude/OpenAI: no existe.
- Métricas, análisis, reportes y PDF: no existen.
- Pruebas automatizadas: no existe un proyecto o suite de pruebas.
- Secretos: no se encontraron secretos en código o configuración; las coincidencias detectadas pertenecen solamente a documentos de instrucciones.

## Dependencias

El proyecto declara `Microsoft.AspNetCore.OpenApi` 10.0.9. La restauración resuelve transitivamente `Microsoft.OpenApi` 2.0.0, que NuGet reporta con una vulnerabilidad conocida de severidad alta (`GHSA-v5pm-xwqc-g5wc`). Debe corregirse al iniciar la Fase 1 y volver a auditarse.

## Configuración

- Entornos de lanzamiento: HTTP en `localhost:5019` y HTTPS en `localhost:7178`.
- `AllowedHosts` está configurado como `*`.
- No hay cadenas de conexión ni variables de integración.
- La configuración actual es únicamente la plantilla estándar de logging.

## Git y archivos del usuario

El repositorio no presenta una base limpia: los archivos del proyecto aparecen agregados o modificados y existen cambios previos de formato en `WeatherForecastController.cs`, `Program.cs` y `WeatherForecast.cs`. Deben conservarse hasta que su reemplazo sea parte explícita de una fase.

## Riesgos

1. Dependencia OpenAPI transitiva con vulnerabilidad alta.
2. Ausencia total de pruebas automatizadas.
3. Endpoint de ejemplo sin valor de producto.
4. No existe manejo global de errores ni contratos versionados.
5. No existe persistencia, autenticación o aislamiento de datos.
6. Los artefactos de la solución paralela pueden causar confusión mientras permanezcan en la raíz.

## Decisión

Continuar sobre la plantilla actual y construir AnalitiAds dentro del único proyecto autorizado. No crear una solución adicional. La separación se realizará mediante carpetas y módulos internos con límites verificables.

## Criterio de salida

La Fase 0 queda completada porque se identificaron el stack, la estructura, las dependencias, la configuración, las capacidades existentes, los riesgos y el trabajo pendiente.

El siguiente paso es la Fase 1: establecer la base interna, corregir la dependencia vulnerable, retirar el ejemplo `WeatherForecast` cuando su reemplazo esté listo, agregar manejo de errores, endpoint de estado, OpenAPI y pruebas automatizadas.
