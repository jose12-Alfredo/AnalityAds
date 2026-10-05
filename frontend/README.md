# Frontend de AnalitiAds

Aplicación web de AnalitiAds construida con Next.js 16, React 19 y TypeScript. Consume la API de AnalitiAds mediante una URL pública configurada en tiempo de compilación.

## Requisitos

- Node.js 20.9 o superior y menor que 25. La versión de desarrollo usada para esta preparación es `24.13.0`; `.nvmrc` permite seleccionarla con nvm.
- npm 10 o superior.
- La API de AnalitiAds disponible. En desarrollo local usa `http://localhost:5019`.

## Inicio local

Desde este directorio:

```powershell
Copy-Item .env.example .env.local
npm ci
npm run dev
```

Abre `http://localhost:3001`. La configuración local queda en `.env.local`, que Git ignora. No guardes tokens JWT, claves OAuth, secretos ni contraseñas en variables `NEXT_PUBLIC_*`: esas variables se incluyen en el JavaScript que recibe el navegador.

## Configuración

La única variable requerida por la interfaz es:

```text
NEXT_PUBLIC_API_URL=https://api.tudominio.com
```

Usa una URL base sin una barra final. Para desarrollo, `.env.example` contiene `http://localhost:5019`. Para producción, define `NEXT_PUBLIC_API_URL` en el entorno de compilación del proveedor antes de ejecutar `npm run build`; Next.js incorpora las variables `NEXT_PUBLIC_*` al artefacto generado.

El backend debe permitir el origen del frontend mediante CORS y conservar la autenticación Bearer descrita en [ANALYSISENGINE_BACKEND_HANDOFF.md](./ANALYSISENGINE_BACKEND_HANDOFF.md).

## Comandos

```powershell
npm run lint          # Reglas de ESLint
npm run typecheck     # Genera tipos de rutas de Next y valida TypeScript
npm run test:editor   # Pruebas unitarias de los modelos de editor
npm run build         # Compilación de producción
npm run verify        # Todas las comprobaciones anteriores
npm run test:e2e      # Pruebas de interfaz con Playwright
```

Para la primera ejecución de Playwright puede ser necesario instalar el navegador:

```powershell
npx playwright install chromium
```

## Antes de enviar cambios

1. Ejecuta `npm run verify`.
2. Revisa `git status --short`; no deben aparecer `.env.local`, resultados de pruebas, logs, cachés ni carpetas de IDE.
3. Confirma que `.env.example` contiene solo valores públicos de ejemplo y que la URL de producción se configura en el proveedor de despliegue.

El `.gitignore` excluye dependencias, compilados de Next, entornos locales, resultados de Playwright, logs y metadatos de IDE. El `package-lock.json` debe versionarse para instalaciones reproducibles.
