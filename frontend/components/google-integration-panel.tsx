"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { ApiError } from "@/lib/api";
import { readSession, type AuthSession } from "@/lib/auth-session";
import { providerApi, type GoogleProviderSlug, type IntegrationClient, type ProviderConnection, type ProviderSource } from "@/lib/provider-integrations";

const details = {
  "google-ads": { name: "Google Ads", noun: "cuentas publicitarias", description: "Descubre las cuentas accesibles y asígnalas al cliente correcto." },
  ga4: { name: "Google Analytics 4", noun: "propiedades", description: "Descubre propiedades GA4 con acceso de lectura y asígnalas a un cliente." },
  "tiktok-ads": { name: "TikTok Ads", noun: "cuentas publicitarias", description: "Descubre anunciantes autorizados y asígnalos al cliente correcto." },
} as const;

function message(error: unknown) {
  if (error instanceof ApiError) return error.problem.detail || error.problem.title || "La solicitud no pudo completarse.";
  return error instanceof Error ? error.message : "La solicitud no pudo completarse.";
}

export function GoogleIntegrationPanel({ provider }: { provider: GoogleProviderSlug }) {
  const copy = details[provider];
  const [session, setSession] = useState<AuthSession | null>(null);
  const [connection, setConnection] = useState<ProviderConnection | null>(null);
  const [sources, setSources] = useState<ProviderSource[]>([]);
  const [clients, setClients] = useState<IntegrationClient[]>([]);
  const [clientId, setClientId] = useState("");
  const [busy, setBusy] = useState(false);
  const [discovering, setDiscovering] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);

  const load = useCallback(async () => {
    const active = readSession(); setSession(active); if (!active) return;
    setBusy(true);
    try {
      const [status, nextClients] = await Promise.all([providerApi.status(provider, active.accessToken), providerApi.clients(active.accessToken)]);
      setConnection(status); setClients(nextClients); setClientId((value) => value || nextClients.find((x) => x.isActive)?.id || "");
    } catch (error) { setNotice(message(error)); } finally { setBusy(false); }
  }, [provider]);

  useEffect(() => {
    const result = new URLSearchParams(window.location.search).get("result");
    if (result) window.history.replaceState(null, "", window.location.pathname);
    const timer = window.setTimeout(() => {
      if (result) setNotice(result === "success" ? `${copy.name} quedó conectado.` : "La autorización no se completó.");
      void load();
    }, 0); return () => window.clearTimeout(timer);
  }, [copy.name, load]);

  const canConnect = session?.role === "Owner" || session?.role === "Admin";
  const canManage = canConnect || session?.role === "Analyst";
  const assigned = useMemo(() => sources.filter((x) => x.isAssigned).length, [sources]);

  async function connect() {
    if (!session) return; setBusy(true); setNotice(null);
    try { const value = await providerApi.start(provider, session.accessToken); window.location.assign(value.authorizationUrl); }
    catch (error) { setNotice(message(error)); setBusy(false); }
  }
  async function discover() {
    if (!session) return; setDiscovering(true); setNotice(null);
    try { setSources(await providerApi.sources(provider, session.accessToken)); }
    catch (error) { setNotice(message(error)); } finally { setDiscovering(false); }
  }
  async function associate(source: ProviderSource) {
    if (!session || !clientId) return; setBusy(true); setNotice(null);
    try { await providerApi.associate(provider, clientId, source.externalId, session.accessToken); await discover(); setNotice(`${source.name} quedó asignada.`); }
    catch (error) { setNotice(message(error)); } finally { setBusy(false); }
  }
  async function sync(source: ProviderSource) {
    if (!session || !source.dataSourceId) return; setBusy(true); setNotice(null);
    const until = new Date(); const since = new Date(); since.setUTCDate(until.getUTCDate() - 29);
    try { const result = await providerApi.sync(source.dataSourceId, since.toISOString().slice(0, 10), until.toISOString().slice(0, 10), session.accessToken); setNotice(`Sincronización terminada: ${result.rowsReceived} filas recibidas.`); }
    catch (error) { setNotice(message(error)); } finally { setBusy(false); }
  }
  async function enableSchedule(source: ProviderSource) {
    if (!session || !source.dataSourceId) return; setBusy(true); setNotice(null);
    try { await providerApi.schedule(source.dataSourceId, true, session.accessToken); setNotice(`${source.name}: sincronización automática diaria activada.`); }
    catch (error) { setNotice(message(error)); } finally { setBusy(false); }
  }

  return <main className="provider-page">
    <header className="provider-hero"><div><p className="eyebrow">Integraciones / {copy.name}</p><h1>{copy.name}</h1><p>{copy.description}</p></div><span className={`status-pill status-${connection?.status === "Connected" ? "connected" : "disconnected"}`}>{connection?.status ?? "Sin conexión"}</span></header>
    {notice && <div className="notice notice-info" role="status">{notice}</div>}
    <section className="provider-card"><div><p className="eyebrow">Autorización de agencia</p><h2>Credenciales protegidas en el servidor</h2><p>El navegador recibe la URL de autorización, pero nunca almacena tokens del proveedor.</p></div>{canConnect ? <button className="primary-button" disabled={busy} onClick={() => void connect()}>{connection?.status === "Connected" ? "Reconectar" : "Conectar"}</button> : <small>Solo Owner o Admin pueden autorizar.</small>}</section>
    {canManage && <section className="provider-card provider-catalog"><div className="section-heading"><div><p className="eyebrow">Fuentes autorizadas</p><h2>{copy.noun}</h2><p>{assigned} asignadas de {sources.length} descubiertas.</p></div><button className="secondary-button" disabled={discovering || connection?.status !== "Connected"} onClick={() => void discover()}>{discovering ? "Consultando…" : "Descubrir fuentes"}</button></div>
      <label className="provider-client">Cliente<select value={clientId} onChange={(e) => setClientId(e.target.value)}><option value="">Selecciona un cliente</option>{clients.map((client) => <option value={client.id} key={client.id}>{client.name}</option>)}</select></label>
      <div className="provider-sources">{sources.map((source) => <article key={source.externalId}><div><h3>{source.name}</h3><p>{source.externalId} · {source.currency ?? "sin moneda"} · {source.timeZone}{source.isManager ? " · administradora" : ""}</p></div>{source.isAssigned ? <div className="provider-actions"><span className="status-pill status-connected">Asignada</span><button className="secondary-button" disabled={busy || !source.dataSourceId} onClick={() => void sync(source)}>Sincronizar 30 días</button><button className="secondary-button" disabled={busy || !source.dataSourceId} onClick={() => void enableSchedule(source)}>Automática diaria</button></div> : <button className="secondary-button" disabled={!clientId || busy || source.isManager} onClick={() => void associate(source)}>{source.isManager ? "No asignable" : "Asignar"}</button>}</article>)}</div>
      {!sources.length && <div className="empty-state"><h3>Aún no se consultaron fuentes</h3><p>Conecta el proveedor y usa “Descubrir fuentes”.</p></div>}
    </section>}
  </main>;
}
