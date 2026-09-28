# Guía técnica: Servidor MCP de Meta Ads + Backend + Frontend

> Documento pensado para pasarle directamente a Codex (o cualquier agente de código) como especificación del proyecto. Incluye estructura de carpetas, contratos de cada endpoint/tool, variables de entorno y pasos de despliegue.

---

## 0. Objetivo del proyecto

Construir **un backend único** que:
1. Habla con la **Graph API de Meta Marketing** y le da forma a los datos (cuentas, campañas, insights, creativos).
2. Expone esos datos como **servidor MCP remoto** (protocolo Model Context Protocol, transporte Streamable HTTP) para que Claude Code, claude.ai (conector personalizado) o cualquier otro cliente MCP lo use como herramienta.
3. Expone los mismos datos como **API REST** para que un **frontend propio** (dashboard) los consuma sin pasar por ninguna IA.

Un solo repo, un solo deploy, dos "puertas de entrada" (`/mcp` y `/api`) sobre la misma lógica de negocio.

---

## 1. Prerrequisitos (hacer esto ANTES de programar)

1. **Meta for Developers**: crear una app en https://developers.facebook.com/apps → tipo "Business". Agregarle el producto **Marketing API**.
2. **Business Manager**: en business.facebook.com, ir a *Configuración del negocio → Usuarios del sistema* y crear un **System User** con rol Admin (o el mínimo necesario). Asignarle acceso a las **cuentas publicitarias** que va a reportar.
3. **Token de acceso**: generar un token para ese System User con los permisos `ads_read` (y `ads_management` solo si en el futuro se va a escribir, no leer). Los tokens de System User no expiran por tiempo, así que evita el problema de tokens de 60 días de un usuario normal.
4. **IDs a mano**: guardar el/los `act_<id>` de cada cuenta publicitaria que se va a consultar.
5. **Cuenta en Railway o Render** (Render tiene "Web Service" con free tier, Railway también) para el deploy.
6. Definir una **API key propia** (un string random largo, ej. generado con `openssl rand -hex 32`) — esto NO es de Meta, es la clave que vos vas a exigir para que alguien pueda usar tu servidor MCP/API.

Guardar todo esto en un `.env` local (nunca commitear):
```
META_ACCESS_TOKEN=EAAG...
META_API_VERSION=v21.0
MCP_API_KEY=tu-clave-random-larga
PORT=3000
NODE_ENV=development
```

---

## 2. Estructura de carpetas

```
meta-ads-mcp/
├── src/
│   ├── meta/
│   │   ├── client.ts          # wrapper de fetch a la Graph API
│   │   ├── parsers.ts         # normaliza formatos es-BO, paginación, cost_per_action_type
│   │   └── types.ts           # tipos TS de las respuestas que nos interesan
│   ├── mcp/
│   │   ├── server.ts          # crea el McpServer y registra las tools
│   │   └── tools/
│   │       ├── getAdAccounts.ts
│   │       ├── getCampaigns.ts
│   │       ├── getInsights.ts
│   │       └── getCreatives.ts
│   ├── api/
│   │   └── routes.ts          # rutas REST /api/... que llaman a src/meta/client.ts
│   ├── auth/
│   │   └── bearerAuth.ts      # middleware que valida MCP_API_KEY
│   ├── app.ts                 # instancia de Express, monta /mcp y /api
│   └── index.ts               # entry point, arranca el server
├── frontend/                  # app separada (Vite+React), o repo aparte
│   └── ...
├── .env.example
├── package.json
├── tsconfig.json
├── Dockerfile
└── README.md
```

---

## 3. Backend — capa Meta (`src/meta/`)

### 3.1 `client.ts` — contrato mínimo

Funciones que debe exponer (todas async, todas devuelven JSON ya limpio, no la respuesta cruda de Meta):

```ts
getAdAccounts(): Promise<AdAccount[]>

getCampaigns(accountId: string, opts?: { status?: string[] }): Promise<Campaign[]>

getInsights(params: {
  accountId: string;
  level: "account" | "campaign" | "adset" | "ad";
  since: string;        // YYYY-MM-DD
  until: string;        // YYYY-MM-DD
  timeIncrement?: "1" | "all_days";
  campaignIds?: string[]; // para filtrar a nivel campaign, ver nota abajo
  fields?: string[];
}): Promise<InsightRow[]>

getCreatives(campaignId: string): Promise<Creative[]>
```

### 3.2 Reglas de negocio a respetar (aprendidas ya en el dashboard anterior — no repetir los mismos bugs)

- **Filtrar por campaña a nivel `account` no funciona en la Graph API** — Meta ignora el filtro y devuelve toda la cuenta. Hay que pedir con `level=campaign` y `filtering=[{field:"campaign.id", operator:"IN", value:[...ids]}]`, y si se necesita el total, **agregar en el backend** sumando las filas devueltas.
- Los valores numéricos de Meta llegan como **strings**, nunca asumir `number` directo — parsear siempre con `parseFloat` después de limpiar el string.
- `cost_per_action_type` sin especificar subtipo devuelve el diccionario completo de costos por tipo de acción (`landing_page_view`, `add_to_cart`, `omni_purchase`, etc.). Si se necesita el **conteo** de una acción, derivarlo como `spend / cost_per_action_type[esa_accion]`, no asumir que viene un campo `actions` con esa clave.
- El nombre canónico de la acción de "compra" en la API es `omni_purchase` (sin prefijo `actions:`) — buscarlo así en el array `actions`.
- Solo se puede pedir **un breakdown por llamada** (`breakdowns=age,gender` sí, pero cruces con `publisher_platform` en la misma llamada no siempre vienen bien poblados) — si se necesitan cruces, hacer llamadas separadas y combinar en el backend.
- Fechas: siempre trabajar en el rango `since`/`until` que pide el cliente, nunca usar `new Date().toISOString()` para calcular "hoy" sin fijar zona horaria explícita (usar UTC-4 si el negocio es de Bolivia, o mejor, dejar que el rango de fechas siempre lo pase el que llama).
- Manejar **paginación** (`paging.next`) en cualquier endpoint que pueda devolver más de una página (cuentas con muchas campañas).
- Manejar **rate limits** de Meta (HTTP 17 / código de error `4`, `17`, `32`, `613`): reintentar con backoff exponencial (2-3 intentos) antes de fallar.

### 3.3 `parsers.ts`

Centralizar acá cualquier normalización de formato (moneda, miles/decimales, fechas en español) para no repetirla en cada tool/endpoint.

---

## 4. Backend — servidor MCP (`src/mcp/`)

### 4.1 Dependencias

```bash
npm install @modelcontextprotocol/sdk zod express
npm install -D typescript @types/express @types/node tsx
```

### 4.2 Transporte

Usar **Streamable HTTP** (no stdio) — es el transporte para servidores MCP *remotos*, accesibles por URL, que es lo que necesitás para que Claude Code o claude.ai se conecten sin correr nada localmente.

### 4.3 `server.ts` — esqueleto

```ts
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { getAdAccounts, getCampaigns, getInsights, getCreatives } from "../meta/client.js";

export function createMcpServer() {
  const server = new McpServer({ name: "meta-ads-mcp", version: "1.0.0" });

  server.registerTool(
    "get_ad_accounts",
    {
      title: "Listar cuentas publicitarias",
      description: "Devuelve las cuentas publicitarias de Meta accesibles con el token configurado.",
      inputSchema: {},
    },
    async () => {
      const accounts = await getAdAccounts();
      return { content: [{ type: "text", text: JSON.stringify(accounts) }] };
    }
  );

  server.registerTool(
    "get_campaign_insights",
    {
      title: "Métricas de campañas",
      description: "Trae insights (gasto, impresiones, clics, compras) de campañas de una cuenta en un rango de fechas.",
      inputSchema: {
        accountId: z.string().describe("ID de cuenta, formato act_XXXXXXXXXX"),
        since: z.string().describe("YYYY-MM-DD"),
        until: z.string().describe("YYYY-MM-DD"),
        campaignIds: z.array(z.string()).optional(),
      },
    },
    async ({ accountId, since, until, campaignIds }) => {
      const rows = await getInsights({ accountId, since, until, level: "campaign", campaignIds });
      return { content: [{ type: "text", text: JSON.stringify(rows) }] };
    }
  );

  // repetir el patrón para get_campaigns y get_creatives

  return server;
}
```

Cada tool: nombre corto en snake_case, `description` clara (es lo que la IA lee para decidir cuándo usarla), `inputSchema` con Zod, y el resultado siempre como `content: [{ type: "text", text: JSON.stringify(...) }]`.

### 4.4 Montaje HTTP (`app.ts`)

```ts
import express from "express";
import { StreamableHTTPServerTransport } from "@modelcontextprotocol/sdk/server/streamableHttp.js";
import { createMcpServer } from "./mcp/server.js";
import { bearerAuth } from "./auth/bearerAuth.js";
import { apiRouter } from "./api/routes.js";

const app = express();
app.use(express.json());

app.post("/mcp", bearerAuth, async (req, res) => {
  const server = createMcpServer();
  const transport = new StreamableHTTPServerTransport({ sessionIdGenerator: undefined });
  await server.connect(transport);
  await transport.handleRequest(req, res, req.body);
});

app.use("/api", bearerAuth, apiRouter);

export default app;
```

### 4.5 `bearerAuth.ts`

Middleware simple: valida `Authorization: Bearer <MCP_API_KEY>` contra la variable de entorno. Sin esto, cualquiera con la URL podría gastar tu cuota de llamadas a Meta.

---

## 5. Backend — API REST (`src/api/routes.ts`)

Espejo de las tools MCP, para que el frontend no necesite entender MCP:

| Método | Ruta                              | Descripción                          |
|--------|-----------------------------------|---------------------------------------|
| GET    | `/api/accounts`                   | Lista de cuentas                      |
| GET    | `/api/accounts/:id/campaigns`     | Campañas de una cuenta                |
| GET    | `/api/accounts/:id/insights`      | Insights (query params: since, until, campaignIds) |
| GET    | `/api/campaigns/:id/creatives`    | Creativos de una campaña              |

Todas devuelven JSON ya limpio (mismos tipos que usan las tools MCP) — de hecho ambas capas llaman a las mismas funciones de `src/meta/client.ts`.

---

## 6. Frontend

- **Vite + React + TypeScript**, en carpeta separada (`frontend/`) o repo aparte — no necesita saber nada de MCP, solo pega contra `/api/...` con un fetch normal y el mismo `MCP_API_KEY` como header (o, mejor, un proxy/reverse-env en el propio backend si el frontend se sirve desde el mismo dominio, para no exponer la key en el navegador).
- Reusar la identidad de marca C&P (colores, logo) ya definida en el dashboard anterior.
- Estructura mínima: página de selección de cuenta → rango de fechas → tarjetas de KPIs + tabla de campañas + gráfico de insights diarios (se puede reusar Recharts o Chart.js).

> Nota de seguridad: si el frontend es público (para clientes), **no** pongas el `MCP_API_KEY` en el código del navegador. Poné un endpoint intermedio en tu propio backend que ya tenga la key server-side, y que el frontend llame a ese endpoint sin credenciales expuestas, o metele autenticación de usuarios (login) delante.

---

## 7. Variables de entorno (resumen)

```
META_ACCESS_TOKEN=       # token del System User
META_API_VERSION=v21.0
MCP_API_KEY=             # clave propia para proteger /mcp y /api
PORT=3000
NODE_ENV=production
```

---

## 8. Probar localmente antes de desplegar

1. `npm run dev` (con `tsx watch src/index.ts`).
2. Probar la API REST con `curl`:
   ```bash
   curl -H "Authorization: Bearer $MCP_API_KEY" http://localhost:3000/api/accounts
   ```
3. Probar el servidor MCP con el **MCP Inspector** (`npx @modelcontextprotocol/inspector`), apuntando a `http://localhost:3000/mcp` con el bearer token — permite ver las tools registradas y probarlas manualmente antes de conectar Claude.

---

## 9. Deploy (Render o Railway)

1. `Dockerfile` simple (Node 20-alpine, `npm ci`, `npm run build`, `CMD ["node", "dist/index.js"]`).
2. Subir el repo a GitHub.
3. En Render/Railway: crear un **Web Service** apuntando al repo, setear las variables de entorno de la sección 7, puerto expuesto = el mismo que usa Express.
4. Confirmar que queda con **HTTPS** (ambos lo dan gratis) — MCP remoto necesita HTTPS para que Claude lo acepte.
5. Anotar la URL final, ej. `https://meta-ads-mcp.onrender.com`.

---

## 10. Conectarlo a Claude Code / claude.ai

- **Claude Code**: agregar el servidor MCP remoto por configuración (URL + header `Authorization: Bearer <tu key>`), tipo Streamable HTTP.
- **claude.ai**: Ajustes → Conectores → Agregar conector personalizado, pegar la URL `https://tu-dominio/mcp` y la key.
- Una vez conectado, cualquier tool registrada (`get_ad_accounts`, `get_campaign_insights`, etc.) queda disponible como si fuera un conector nativo.

---

## 11. Orden sugerido de implementación (para dárselo a Codex en tandas)

1. Scaffold del proyecto (`package.json`, `tsconfig.json`, estructura de carpetas).
2. `src/meta/client.ts` + `parsers.ts` + `types.ts` — probarlo standalone con un script de prueba (sin MCP ni Express todavía) contra la cuenta real.
3. `src/api/routes.ts` + `app.ts` básico (solo REST) — probar con curl/Postman.
4. Agregar la capa MCP (`src/mcp/`) reusando las mismas funciones de `meta/client.ts`.
5. Probar con MCP Inspector.
6. Dockerfile + deploy a Render/Railway.
7. Conectar a Claude Code y validar en un caso real.
8. Recién ahí, frontend.

---

## 12. Roadmap posterior (no bloquea el MVP)

- Cache corto (ej. 5 min) de insights para no pegarle a Meta en cada tool call repetida.
- Logs/telemetría de qué tools se usan y con qué parámetros.
- Soporte multi-cuenta con distintos tokens por cliente (si más adelante se vende a otras agencias).
- Migrar `MCP_API_KEY` fija a OAuth si se va a compartir con terceros.
