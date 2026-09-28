"use client";

import Link from "next/link";
import { FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { ApiError, apiRequest } from "@/lib/api";
import { clearSession, readSession, canManageClientAccess, type AuthSession } from "@/lib/auth-session";
import { type Ad, type AdSet, type Campaign } from "@/lib/advertising";
import { initialMetricRange, type InsightLevel, type InsightSnapshot, formatMetric } from "@/lib/metrics";
import { MetricsInsightsPanel } from "@/components/metrics-insights-panel";
import { AnalysisEnginePanel } from "@/components/analysis-engine-panel";

interface Client {
  id: string;
  name: string;
  isActive: boolean;
}

interface AdAccount {
  id: string;
  clientId: string;
  name: string;
  currency: string;
  timeZone: string;
  connectionStatus: string;
  isActive: boolean;
}

type SelectedNode = { level: InsightLevel; id: string; name: string };

const nodeMetricPath: Record<InsightLevel, (id: string) => string> = {
  Account: (id) => `/api/v1/ad-accounts/${id}/metrics`,
  Campaign: (id) => `/api/v1/campaigns/${id}/metrics`,
  AdSet: (id) => `/api/v1/ad-sets/${id}/metrics`,
  Ad: (id) => `/api/v1/ads/${id}/metrics`,
};

function dashboardError(error: unknown) {
  if (!(error instanceof ApiError)) return error instanceof Error ? error.message : "No pudimos cargar tus datos autorizados.";
  if (error.status === 401) return "Tu sesión venció. Inicia sesión nuevamente.";
  if (error.status === 403 || error.status === 404) return "Este acceso ya no está disponible. Actualizamos tu espacio de trabajo para proteger los datos del cliente.";
  if (error.status === 400) return error.problem.detail || "Revisa el rango de fechas solicitado.";
  if (error.status === 409) return "La cuenta no tiene una conexión activa para esta consulta.";
  return error.problem.detail || "No pudimos cargar tus datos autorizados.";
}

function formatTimestamp(value: string) {
  return new Intl.DateTimeFormat("es-BO", { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
}

function statusLabel(status: string) {
  return status === "ACTIVE" ? "Activo" : status || "Sin estado";
}

export function ClientDashboard() {
  const router = useRouter();
  const defaultRange = useMemo(() => initialMetricRange(), []);
  const [session] = useState<AuthSession | null>(() => readSession());
  const [clients, setClients] = useState<Client[]>([]);
  const [clientId, setClientId] = useState("");
  const [accounts, setAccounts] = useState<AdAccount[]>([]);
  const [accountId, setAccountId] = useState("");
  const [campaigns, setCampaigns] = useState<Campaign[]>([]);
  const [adSets, setAdSets] = useState<Record<string, AdSet[]>>({});
  const [ads, setAds] = useState<Record<string, Ad[]>>({});
  const [openedCampaigns, setOpenedCampaigns] = useState<Record<string, boolean>>({});
  const [openedAdSets, setOpenedAdSets] = useState<Record<string, boolean>>({});
  const [selectedNode, setSelectedNode] = useState<SelectedNode | null>(null);
  const [since, setSince] = useState(defaultRange.since);
  const [until, setUntil] = useState(defaultRange.until);
  const [appliedRange, setAppliedRange] = useState(defaultRange);
  const [snapshots, setSnapshots] = useState<InsightSnapshot[]>([]);
  const [isLoadingClients, setIsLoadingClients] = useState(true);
  const [isLoadingAccounts, setIsLoadingAccounts] = useState(false);
  const [isLoadingCampaigns, setIsLoadingCampaigns] = useState(false);
  const [isLoadingSnapshots, setIsLoadingSnapshots] = useState(false);
  const [isAccessRevoked, setIsAccessRevoked] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const clearPrivateData = useCallback(() => {
    setClientId("");
    setAccounts([]);
    setAccountId("");
    setCampaigns([]);
    setAdSets({});
    setAds({});
    setOpenedCampaigns({});
    setOpenedAdSets({});
    setSelectedNode(null);
    setSnapshots([]);
  }, []);

  const handleError = useCallback((requestError: unknown) => {
    setError(dashboardError(requestError));
    if (requestError instanceof ApiError && (requestError.status === 403 || requestError.status === 404)) {
      clearPrivateData();
      setIsAccessRevoked(true);
    }
  }, [clearPrivateData]);

  const loadClients = useCallback(async (activeSession: AuthSession) => {
    setIsLoadingClients(true);
    setError(null);
    setIsAccessRevoked(false);
    try {
      const nextClients = await apiRequest<Client[]>("/api/v1/clients", activeSession.accessToken);
      const activeClients = nextClients.filter((client) => client.isActive);
      setClients(activeClients);
      setClientId((current) => activeClients.some((client) => client.id === current) ? current : activeClients[0]?.id ?? "");
      if (!activeClients.length) clearPrivateData();
    } catch (requestError) {
      setClients([]);
      setClientId("");
      clearPrivateData();
      handleError(requestError);
    } finally {
      setIsLoadingClients(false);
    }
  }, [clearPrivateData, handleError]);

  useEffect(() => {
    const activeSession = session;
    if (!activeSession) {
      router.replace("/login");
      return;
    }
    const timer = window.setTimeout(() => void loadClients(activeSession), 0);
    return () => window.clearTimeout(timer);
  }, [loadClients, router, session]);

  useEffect(() => {
    if (!session || !clientId || isAccessRevoked) return;
    const loadAccounts = async () => {
      setIsLoadingAccounts(true);
      setError(null);
      try {
        const nextAccounts = await apiRequest<AdAccount[]>(`/api/v1/clients/${clientId}/ad-accounts`, session.accessToken);
        const activeAccounts = nextAccounts.filter((account) => account.isActive);
        setAccounts(activeAccounts);
        setAccountId((current) => activeAccounts.some((account) => account.id === current) ? current : activeAccounts[0]?.id ?? "");
        setCampaigns([]);
        setAdSets({});
        setAds({});
      } catch (requestError) {
        setAccounts([]);
        handleError(requestError);
      } finally {
        setIsLoadingAccounts(false);
      }
    };
    void loadAccounts();
  }, [clientId, handleError, isAccessRevoked, session]);

  useEffect(() => {
    if (!session || !accountId || isAccessRevoked) return;
    const loadCampaigns = async () => {
      setIsLoadingCampaigns(true);
      setError(null);
      try {
        const nextCampaigns = await apiRequest<Campaign[]>(`/api/v1/ad-accounts/${accountId}/campaigns?includeMissing=false`, session.accessToken);
        setCampaigns(nextCampaigns);
        setAdSets({});
        setAds({});
        setOpenedCampaigns({});
        setOpenedAdSets({});
        const account = accounts.find((item) => item.id === accountId);
        if (account) setSelectedNode({ level: "Account", id: account.id, name: account.name });
      } catch (requestError) {
        setCampaigns([]);
        handleError(requestError);
      } finally {
        setIsLoadingCampaigns(false);
      }
    };
    void loadCampaigns();
  }, [accountId, accounts, handleError, isAccessRevoked, session]);

  useEffect(() => {
    if (!session || !selectedNode || isAccessRevoked) return;
    const loadSnapshots = async () => {
      setIsLoadingSnapshots(true);
      try {
        const query = new URLSearchParams(appliedRange);
        const nextSnapshots = await apiRequest<InsightSnapshot[]>(`${nodeMetricPath[selectedNode.level](selectedNode.id)}?${query}`, session.accessToken);
        setSnapshots(nextSnapshots);
      } catch (requestError) {
        setSnapshots([]);
        handleError(requestError);
      } finally {
        setIsLoadingSnapshots(false);
      }
    };
    void loadSnapshots();
  }, [appliedRange, handleError, isAccessRevoked, selectedNode, session]);

  async function toggleCampaign(campaign: Campaign) {
    if (!session || isAccessRevoked) return;
    const isOpen = openedCampaigns[campaign.id];
    setOpenedCampaigns((current) => ({ ...current, [campaign.id]: !isOpen }));
    setSelectedNode({ level: "Campaign", id: campaign.id, name: campaign.name });
    if (isOpen || adSets[campaign.id]) return;
    try {
      const nextAdSets = await apiRequest<AdSet[]>(`/api/v1/campaigns/${campaign.id}/ad-sets?includeMissing=false`, session.accessToken);
      setAdSets((current) => ({ ...current, [campaign.id]: nextAdSets }));
    } catch (requestError) {
      setOpenedCampaigns((current) => ({ ...current, [campaign.id]: false }));
      handleError(requestError);
    }
  }

  async function toggleAdSet(adSet: AdSet) {
    if (!session || isAccessRevoked) return;
    const isOpen = openedAdSets[adSet.id];
    setOpenedAdSets((current) => ({ ...current, [adSet.id]: !isOpen }));
    setSelectedNode({ level: "AdSet", id: adSet.id, name: adSet.name });
    if (isOpen || ads[adSet.id]) return;
    try {
      const nextAds = await apiRequest<Ad[]>(`/api/v1/ad-sets/${adSet.id}/ads?includeMissing=false`, session.accessToken);
      setAds((current) => ({ ...current, [adSet.id]: nextAds }));
    } catch (requestError) {
      setOpenedAdSets((current) => ({ ...current, [adSet.id]: false }));
      handleError(requestError);
    }
  }

  function submitRange(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (since > until) {
      setError("La fecha inicial debe ser anterior o igual a la fecha final.");
      return;
    }
    const days = Math.floor((Date.parse(`${until}T00:00:00Z`) - Date.parse(`${since}T00:00:00Z`)) / 86_400_000) + 1;
    if (days > 90) {
      setError("El rango máximo permitido es de 90 días inclusivos.");
      return;
    }
    setError(null);
    setAppliedRange({ since, until });
  }

  function signOut() {
    clearSession();
    router.replace("/login");
  }

  const selectedClient = clients.find((client) => client.id === clientId);
  const selectedAccount = accounts.find((account) => account.id === accountId);

  if (!session) return <main className="private-loading">Validando sesión…</main>;

  return (
    <main className="dashboard-page">
      <header className="dashboard-header">
        <div><Link className="brand-lockup" href="/app"><span className="brand-mark">A</span><span>Analiti<span>Ads</span></span></Link><p className="eyebrow">{session.role === "ClientViewer" ? "Tu espacio de cliente" : "Vista de datos autorizados"}</p><h1>Panel de campañas</h1></div>
        <div className="dashboard-header-actions"><span className="role-pill">{session.role} · {session.agencyName}</span>{selectedClient && <Link className="secondary-button" href={`/app/clientes/${selectedClient.id}/reportes`}>Reportes</Link>}{canManageClientAccess(session.role) && selectedClient && <Link className="secondary-button" href={`/app/clientes/${selectedClient.id}/accesos`}>Gestionar accesos</Link>}{session.role !== "ClientViewer" && <Link className="secondary-button" href="/app/configuracion/integraciones/meta">Integración Meta</Link>}<button className="text-button" type="button" onClick={signOut}>Cerrar sesión</button></div>
      </header>
      {error && <div className="notice notice-error" role="alert">{error}</div>}
      {isLoadingClients ? <DashboardLoading /> : isAccessRevoked ? <AccessRevoked onReload={() => void loadClients(session)} /> : !clients.length ? <NoAssignments /> : <>
        <section className="dashboard-selector-card" aria-label="Selección de datos autorizados">
          <label>Cliente<select value={clientId} onChange={(event) => { setClientId(event.target.value); setSelectedNode(null); setSnapshots([]); }}><option value="">Selecciona un cliente</option>{clients.map((client) => <option key={client.id} value={client.id}>{client.name}</option>)}</select></label>
          <label>Cuenta publicitaria<select value={accountId} onChange={(event) => { const nextAccount = accounts.find((item) => item.id === event.target.value); setAccountId(event.target.value); if (nextAccount) setSelectedNode({ level: "Account", id: nextAccount.id, name: nextAccount.name }); }} disabled={isLoadingAccounts || !accounts.length}><option value="">{isLoadingAccounts ? "Cargando cuentas…" : accounts.length ? "Selecciona una cuenta" : "No hay cuentas autorizadas"}</option>{accounts.map((account) => <option key={account.id} value={account.id}>{account.name}</option>)}</select></label>
          {selectedAccount && <div className="account-context"><span>{selectedAccount.currency} · {selectedAccount.timeZone}</span><span>{selectedAccount.connectionStatus}</span></div>}
        </section>
        {isLoadingAccounts ? <div className="structure-loading">Consultando cuentas autorizadas…</div> : !accounts.length ? <section className="structure-empty"><h2>Este cliente aún no tiene cuentas disponibles</h2><p>Cuando la agencia asocie una cuenta, aparecerá aquí automáticamente.</p></section> : <div className="dashboard-content-grid">
          <section className="hierarchy-card" aria-labelledby="hierarchy-title"><div className="section-heading"><div><p className="eyebrow">Estructura autorizada</p><h2 id="hierarchy-title">Campañas</h2></div><span>{campaigns.length}</span></div>{isLoadingCampaigns ? <div className="structure-loading">Consultando campañas…</div> : campaigns.length ? <ul className="campaign-tree">{campaigns.map((campaign) => <li key={campaign.id}><TreeButton isSelected={selectedNode?.id === campaign.id} isOpen={Boolean(openedCampaigns[campaign.id])} label={campaign.name} detail={statusLabel(campaign.effectiveStatus)} onClick={() => void toggleCampaign(campaign)} />{openedCampaigns[campaign.id] && <ul>{(adSets[campaign.id] ?? []).map((adSet) => <li key={adSet.id}><TreeButton isSelected={selectedNode?.id === adSet.id} isOpen={Boolean(openedAdSets[adSet.id])} label={adSet.name} detail={statusLabel(adSet.effectiveStatus)} onClick={() => void toggleAdSet(adSet)} />{openedAdSets[adSet.id] && <ul>{(ads[adSet.id] ?? []).map((ad) => <li key={ad.id}><button className={`tree-button tree-leaf ${selectedNode?.id === ad.id ? "is-selected" : ""}`} type="button" onClick={() => setSelectedNode({ level: "Ad", id: ad.id, name: ad.name })}><span>{ad.name}</span><small>{statusLabel(ad.effectiveStatus)}</small></button></li>)}</ul>}</li>)}</ul>}</li>)}</ul> : <div className="structure-empty"><h3>No hay campañas para mostrar</h3><p>No mostramos registros ausentes de Meta ni datos inventados.</p></div>}</section>
          <section className="daily-series-card" aria-labelledby="series-title"><div className="section-heading"><div><p className="eyebrow">Serie diaria autorizada</p><h2 id="series-title">{selectedNode ? selectedNode.name : "Selecciona una cuenta"}</h2></div>{selectedNode && <span className="series-level">{selectedNode.level}</span>}</div><p className="metrics-intro">Cada fila corresponde a un día observado. No convertimos reach en un total de personas para el rango.</p><form className="metrics-range" onSubmit={submitRange}><label>Desde<input type="date" value={since} onChange={(event) => setSince(event.target.value)} required /></label><label>Hasta<input type="date" value={until} onChange={(event) => setUntil(event.target.value)} required /></label><button className="text-button" type="submit" disabled={isLoadingSnapshots || !selectedNode}>Consultar</button></form>{isLoadingSnapshots ? <div className="structure-loading">Consultando serie diaria…</div> : selectedNode ? <DailySeries snapshots={snapshots} /> : <div className="structure-empty"><h3>Selecciona una cuenta</h3><p>Elige una cuenta publicitaria para ver sus campañas y series diarias.</p></div>}</section>
        </div>}
        {selectedNode && !isAccessRevoked && <MetricsInsightsPanel level={selectedNode.level} resourceId={selectedNode.id} adAccountId={accountId} session={session} onCampaignSelect={(id, name) => setSelectedNode({ level: "Campaign", id, name })} />}
        {selectedAccount && selectedClient && !isAccessRevoked && <AnalysisEnginePanel key={`${selectedAccount.id}:${appliedRange.since}:${appliedRange.until}`} clientId={selectedClient.id} adAccountId={selectedAccount.id} accountName={selectedAccount.name} campaigns={campaigns} session={session} initialRange={appliedRange} onNotFound={() => handleError(new ApiError(404, { detail: "Esta cuenta ya no está disponible dentro de tus accesos autorizados." }))} />}
      </>}
    </main>
  );
}

function TreeButton({ isSelected, isOpen, label, detail, onClick }: { isSelected: boolean; isOpen: boolean; label: string; detail: string; onClick: () => void }) {
  return <button className={`tree-button ${isSelected ? "is-selected" : ""}`} type="button" onClick={onClick}><span><b aria-hidden="true">{isOpen ? "−" : "+"}</b>{label}</span><small>{detail}</small></button>;
}

function DailySeries({ snapshots }: { snapshots: InsightSnapshot[] }) {
  if (!snapshots.length) return <div className="structure-empty"><h3>Sin observaciones para este rango</h3><p>Una fecha sin actividad puede no producir una fila. No la mostramos como cero.</p></div>;
  return <div className="daily-series-table-wrap"><table className="daily-series-table"><thead><tr><th>Fecha</th><th>Gasto</th><th>Impresiones</th><th>Reach diario</th><th>Clics</th><th>Leads</th><th>Compras</th><th>Actualizado</th></tr></thead><tbody>{snapshots.map((snapshot) => <tr key={snapshot.id}><td>{snapshot.date}<small>{snapshot.currency}</small></td><td>{formatMetric(snapshot.observed.spend, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</td><td>{formatMetric(snapshot.observed.impressions)}</td><td>{formatMetric(snapshot.observed.reach)}</td><td>{formatMetric(snapshot.observed.linkClicks)}</td><td>{formatMetric(snapshot.observed.leads)}</td><td>{formatMetric(snapshot.observed.purchases)}</td><td><small>{formatTimestamp(snapshot.observedAtUtc)}</small></td></tr>)}</tbody></table></div>;
}

function DashboardLoading() {
  return <main className="dashboard-page"><header className="dashboard-header"><div><p className="eyebrow">AnalitiAds</p><h1>Panel de campañas</h1></div></header><div className="dashboard-skeleton"><span /><span /><span /></div></main>;
}

function NoAssignments() {
  return <section className="dashboard-empty-state"><p className="eyebrow">Acceso de cliente</p><h2>Aún no tienes clientes asignados.</h2><p>La agencia puede darte acceso a un cliente desde una invitación. Cuando exista una asignación activa, aparecerá aquí sin necesidad de crear una nueva cuenta.</p></section>;
}

function AccessRevoked({ onReload }: { onReload: () => void }) {
  return <section className="dashboard-empty-state"><p className="eyebrow">Acceso actualizado</p><h2>Tu acceso a estos datos fue revocado o cambió.</h2><p>Quitamos la selección y los datos cargados anteriormente. Consulta nuevamente los clientes que todavía tengas autorizados.</p><button className="primary-button" type="button" onClick={onReload}>Actualizar clientes autorizados</button></section>;
}
