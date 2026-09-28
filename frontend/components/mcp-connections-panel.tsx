"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { ApiError, apiRequest } from "@/lib/api";
import { clearSession, readSession, type AuthSession } from "@/lib/auth-session";

interface McpConnection { id: string; name: string; scopes: string[]; createdAtUtc: string; expiresAtUtc: string; revokedAtUtc: string | null; }
interface CreatedConnection { connection: McpConnection; accessToken: string; }

function connectionState(connection: McpConnection) {
  if (connection.revokedAtUtc) return "Revocada";
  return Date.parse(connection.expiresAtUtc) <= Date.now() ? "Expirada" : "Activa";
}

function connectionError(error: unknown) {
  if (!(error instanceof ApiError)) return error instanceof Error ? error.message : "No pudimos administrar la conexión MCP.";
  if (error.status === 401) return "Tu sesión venció. Inicia sesión nuevamente.";
  if (error.status === 400) return error.problem.detail || "Revisa el nombre y confirma tu consentimiento.";
  if (error.status === 404) return "No encontramos esa conexión MCP.";
  return error.problem.detail || "No pudimos administrar la conexión MCP.";
}

export function McpConnectionsPanel() {
  const router = useRouter();
  const [session] = useState<AuthSession | null>(() => readSession());
  const [connections, setConnections] = useState<McpConnection[]>([]);
  const [name, setName] = useState("Codex");
  const [consentAccepted, setConsentAccepted] = useState(false);
  const [oneTimeToken, setOneTimeToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (!session) return;
    setIsLoading(true); setError(null);
    try { setConnections(await apiRequest<McpConnection[]>("/api/v1/mcp/connections", session.accessToken)); }
    catch (requestError) { setError(connectionError(requestError)); }
    finally { setIsLoading(false); }
  }, [session]);

  useEffect(() => { if (!session) { router.replace("/login"); return; } const timer = window.setTimeout(() => void load(), 0); return () => window.clearTimeout(timer); }, [load, router, session]);

  async function createConnection(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!session || isSubmitting || !consentAccepted) return;
    setIsSubmitting(true); setError(null); setNotice(null); setOneTimeToken(null);
    try {
      const created = await apiRequest<CreatedConnection>("/api/v1/mcp/connections", session.accessToken, { method: "POST", body: JSON.stringify({ name: name.trim(), consentAccepted: true }) });
      setOneTimeToken(created.accessToken);
      setConsentAccepted(false);
      setNotice("Conexión MCP creada. Copia el token ahora y guárdalo en un gestor de secretos; no se mostrará otra vez.");
      await load();
    } catch (requestError) { setError(connectionError(requestError)); }
    finally { setIsSubmitting(false); }
  }

  async function copyToken() {
    if (!oneTimeToken) return;
    try { await navigator.clipboard.writeText(oneTimeToken); setNotice("Token MCP copiado. Guárdalo en un lugar seguro antes de salir de esta pantalla."); }
    catch { setError("No pudimos copiar el token. Cópialo manualmente antes de cerrar esta pantalla."); }
  }

  async function revoke(connection: McpConnection) {
    if (!session || !window.confirm(`Revocar la conexión MCP “${connection.name}”? Su siguiente solicitud MCP será rechazada.`)) return;
    setError(null); setNotice(null);
    try { await apiRequest<void>(`/api/v1/mcp/connections/${connection.id}`, session.accessToken, { method: "DELETE" }); setNotice("Conexión MCP revocada."); await load(); }
    catch (requestError) { setError(connectionError(requestError)); }
  }

  if (!session) return <main className="private-loading">Validando sesión…</main>;
  return <main className="mcp-page"><header className="mcp-header"><div><p className="eyebrow">Conexiones opcionales</p><h1>Acceso MCP de solo lectura</h1><p>El dashboard sigue funcionando con la API web aunque no crees ninguna conexión MCP.</p></div><button className="text-button" type="button" onClick={() => { clearSession(); router.replace("/login"); }}>Cerrar sesión</button></header>
    {notice && <div className="notice notice-success" role="status">{notice}</div>}{error && <div className="notice notice-error" role="alert">{error}</div>}
    <section className="mcp-create-card"><div><p className="eyebrow">Nueva conexión</p><h2>Autoriza un cliente MCP</h2><p>La conexión usa el scope <code>analitiads:read</code> y no recibe credenciales Meta. Solo crea una si sabes dónde guardar su token.</p></div><form className="mcp-create-form" onSubmit={(event) => void createConnection(event)}><label>Nombre de la conexión<input value={name} onChange={(event) => setName(event.target.value)} required minLength={1} maxLength={120} /></label><label className="consent-check"><input type="checkbox" checked={consentAccepted} onChange={(event) => setConsentAccepted(event.target.checked)} /> Entiendo que el token debe conservarse fuera de este navegador.</label><button className="primary-button" type="submit" disabled={!consentAccepted || isSubmitting}>{isSubmitting ? "Creando…" : "Crear conexión"}</button></form>{oneTimeToken && <div className="mcp-one-time"><strong>Token de acceso MCP</strong><p>Este valor solo está disponible en esta respuesta. No lo guardamos en la aplicación.</p><div><input value={oneTimeToken} readOnly aria-label="Token MCP de una sola visualización" /><button className="secondary-button" type="button" onClick={() => void copyToken()}>Copiar token</button></div></div>}</section>
    <section className="mcp-list-card"><div className="section-heading"><div><p className="eyebrow">Conexiones de tu usuario</p><h2>Estado y revocación</h2></div><button className="text-button" type="button" onClick={() => void load()} disabled={isLoading}>Actualizar</button></div>{isLoading ? <div className="structure-loading">Consultando conexiones MCP…</div> : connections.length ? <ul>{connections.map((connection) => <li key={connection.id}><div><strong>{connection.name}</strong><p>{connection.scopes.join(", ")} · creada {new Intl.DateTimeFormat("es-BO", { dateStyle: "medium" }).format(new Date(connection.createdAtUtc))}</p><p>Vence {new Intl.DateTimeFormat("es-BO", { dateStyle: "medium", timeStyle: "short" }).format(new Date(connection.expiresAtUtc))}</p></div><div className="mcp-row-actions"><span className={`mcp-state state-${connectionState(connection).toLowerCase()}`}>{connectionState(connection)}</span>{connectionState(connection) === "Activa" && <button className="text-button dangerous-text" type="button" onClick={() => void revoke(connection)}>Revocar</button>}</div></li>)}</ul> : <div className="structure-empty"><h3>No hay conexiones MCP</h3><p>Crear una es opcional. La navegación web y las métricas REST no requieren MCP.</p></div>}</section>
  </main>;
}
