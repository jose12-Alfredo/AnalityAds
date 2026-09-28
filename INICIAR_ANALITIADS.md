# Iniciar AnalitiAds en Windows

AnalitiAds tiene un backend ASP.NET Core y un frontend Next.js. Deben mantenerse abiertas dos terminales.

## Terminal 1 — backend

```powershell
cd C:\Users\jose1\RiderProjects\AnaliticAsd\AnaliticAsd
dotnet run --launch-profile http
```

Debe mostrar `Now listening on: http://localhost:5019`.

## Terminal 2 — frontend

```powershell
cd C:\Users\jose1\RiderProjects\AnaliticAsd\frontend
npm run dev
```

Debe mostrar `Local: http://localhost:3001`. Abrir esa dirección en el navegador.

## Si aparece “file is being used by another process”

Ya existe un backend abierto. Detenerlo desde Rider o desde la terminal donde se ejecutó usando `Ctrl+C`. Después volver a ejecutar el comando.

## Estado de la base

Neon fue actualizada el 22 de septiembre de 2026 hasta `20260922182817_EnforceAgencyBrandUniqueness`. No quedan migraciones pendientes. El SQL de actualización usado como respaldo revisable se conserva en `output/UpgradeNeonToD6.sql`.
