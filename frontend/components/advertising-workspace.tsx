"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import { ApiError, apiRequest } from "@/lib/api";
import { MetricsPanel } from "@/components/metrics-panel";
import { clearSession, type AuthSession, readSession, saveSession } from "@/lib/auth-session";
import {
  type Ad,
  type AdSet,
  type Campaign,
  canSynchronize,
  describeAdvertisingError,
  formatUtc,
  type SyncResponse,
} from "@/lib/advertising";

interface CurrentUser {
  userId: string;
  email: string;
  agencyId: string;
  agencyName: string;
  role: AuthSession["role"];
}

interface SessionState {
  session: AuthSession | null;
  isLoading: boolean;
  error: string | null;
}

interface EntitySnapshot {
  id: string;
  name: string;
  externalId: string;
  configuredStatus: string;
  effectiveStatus: string;
  lastSyncedAtUtc: string;
  isPresentOnMeta: boolean;
  attributes: Array<{ label: string; value: string }>;
}

type DetailKind = "campaign" | "ad-set" | "ad";

function useVerifiedSession() {
  const [state, setState] = useState<SessionState>({ session: null, isLoading: true, error: null });

  const loadSession = useCallback(async () => {
    const storedSession = readSession();
    if (!storedSession) {
      setState({ session: null, isLoading: false, error: null });
      return;
    }

    try {
      const currentUser = await apiRequest<CurrentUser>("/api/v1/auth/me", storedSession.accessToken);
      const verified = { ...storedSession, ...currentUser };
      saveSession(verified);
      setState({ session: verified, isLoading: false, error: null });
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) clearSession();
      setState({ session: null, isLoading: false, error: describeAdvertisingError(error) });
    }
  }, []);

  useEffect(() => {
    const timer = window.setTimeout(() => void loadSession(), 0);
    return () => window.clearTimeout(timer);
  }, [loadSession]);

  return state;
}

function SyncBadge({ status }: { status: SyncResponse["status"] }) {
  return <span className={`sync-badge sync-${status.toLowerCase()}`}>{status}</span>;
}

function PresenceFlag({ isPresent }: { isPresent: boolean }) {
  return isPresent ? null : <span className="missing-flag">Ausente en Meta</span>;
}

function SessionGate({ state }: { state: SessionState }) {
  if (state.isLoading) return <div className="structure-loading">Validando sesión…</div>;
  return <section className="session-gate"><p className="eyebrow">Sesión requerida</p><h2>Inicia sesión para consultar la estructura.</h2><p>{state.error ?? "Esta vista requiere una sesión válida de AnalitiAds."}</p></section>;
}

function StructureHeader({
  eyebrow,
  title,
  session,
  onBack,
}: {
  eyebrow: string;
  title: string;
  session: AuthSession;
  onBack?: () => void;
}) {
  return (
    <header className="structure-header">
      <div>
        <p className="eyebrow">{eyebrow}</p>
        <h1>{title}</h1>
        <p>Explora la jerarquía sincronizada desde Meta. Esta vista no muestra métricas de rendimiento.</p>
      </div>
      <div className="structure-header-actions">
        {onBack && <button className="text-button" type="button" onClick={onBack}>Volver</button>}
        <span className="role-pill">{session.role} · {session.agencyName}</span>
      </div>
    </header>
  );
}

function HierarchyRail({ active }: { active: "account" | "campaign" | "ad-set" | "ad" }) {
  const levels: Array<{ id: typeof active; label: string }> = [
    { id: "account", label: "Cuenta" },
    { id: "campaign", label: "Campaña" },
    { id: "ad-set", label: "Conjunto" },
    { id: "ad", label: "Anuncio" },
  ];
  const activeIndex = levels.findIndex((level) => level.id === active);

  return <ol className="hierarchy-rail" aria-label="Jerarquía publicitaria">{levels.map((level, index) => <li className={index <= activeIndex ? "is-reached" : ""} key={level.id}><span>{index + 1}</span>{level.label}</li>)}</ol>;
}

function EmptyState({ title, body }: { title: string; body: string }) {
  return <div className="structure-empty"><h3>{title}</h3><p>{body}</p></div>;
}

interface HierarchyFiltersState {
  query: string;
  configuredStatus: string;
  effectiveStatus: string;
}

const initialHierarchyFilters: HierarchyFiltersState = { query: "", configuredStatus: "", effectiveStatus: "" };

function filterHierarchy<T extends Pick<EntitySnapshot, "name" | "externalId" | "configuredStatus" | "effectiveStatus">>(items: T[], filters: HierarchyFiltersState) {
  const query = filters.query.trim().toLocaleLowerCase();
  return items.filter((item) => {
    const matchesQuery = !query || [item.name, item.externalId, item.configuredStatus, item.effectiveStatus].some((value) => value.toLocaleLowerCase().includes(query));
    return matchesQuery
      && (!filters.configuredStatus || item.configuredStatus === filters.configuredStatus)
      && (!filters.effectiveStatus || item.effectiveStatus === filters.effectiveStatus);
  });
}

function HierarchyFilters<T extends Pick<EntitySnapshot, "name" | "externalId" | "configuredStatus" | "effectiveStatus">>({ items, filters, onChange }: { items: T[]; filters: HierarchyFiltersState; onChange: (filters: HierarchyFiltersState) => void }) {
  const configuredStatuses = [...new Set(items.map((item) => item.configuredStatus))].sort();
  const effectiveStatuses = [...new Set(items.map((item) => item.effectiveStatus))].sort();
  const hasFilters = Boolean(filters.query || filters.configuredStatus || filters.effectiveStatus);
  return <div className="hierarchy-filters" aria-label="Filtros de la lista"><label>Buscar<input type="search" value={filters.query} onChange={(event) => onChange({ ...filters, query: event.target.value })} placeholder="Nombre o ID de Meta" /></label><label>Estado configurado<select value={filters.configuredStatus} onChange={(event) => onChange({ ...filters, configuredStatus: event.target.value })}><option value="">Todos</option>{configuredStatuses.map((status) => <option key={status} value={status}>{status}</option>)}</select></label><label>Estado efectivo<select value={filters.effectiveStatus} onChange={(event) => onChange({ ...filters, effectiveStatus: event.target.value })}><option value="">Todos</option>{effectiveStatuses.map((status) => <option key={status} value={status}>{status}</option>)}</select></label>{hasFilters && <button className="text-button" type="button" onClick={() => onChange(initialHierarchyFilters)}>Limpiar filtros</button>}</div>;
}

export function AccountHierarchyPanel({ clientId, adAccountId }: { clientId: string; adAccountId: string }) {
  const sessionState = useVerifiedSession();
  const [sync, setSync] = useState<SyncResponse | null>(null);
  const [campaigns, setCampaigns] = useState<Campaign[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isPosting, setIsPosting] = useState(false);
  const [showMissing, setShowMissing] = useState(false);
  const [campaignFilters, setCampaignFilters] = useState<HierarchyFiltersState>(initialHierarchyFilters);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const loadCampaigns = useCallback(async (accessToken: string, includeMissing: boolean) => {
    const suffix = includeMissing ? "?includeMissing=true" : "";
    const result = await apiRequest<Campaign[]>(`/api/v1/ad-accounts/${adAccountId}/campaigns${suffix}`, accessToken);
    setCampaigns(result);
  }, [adAccountId]);

  const refreshSync = useCallback(async (accessToken: string) => {
    const result = await apiRequest<SyncResponse>(`/api/v1/ad-accounts/${adAccountId}/sync`, accessToken);
    setSync(result);
    if (result.status === "Succeeded") await loadCampaigns(accessToken, showMissing);
    return result;
  }, [adAccountId, loadCampaigns, showMissing]);

  const loadAccount = useCallback(async () => {
    if (!sessionState.session) return;
    setIsLoading(true);
    setError(null);
    try {
      await Promise.all([
        refreshSync(sessionState.session.accessToken),
        loadCampaigns(sessionState.session.accessToken, showMissing),
      ]);
    } catch (loadError) {
      setError(describeAdvertisingError(loadError));
    } finally {
      setIsLoading(false);
    }
  }, [loadCampaigns, refreshSync, sessionState.session, showMissing]);

  useEffect(() => {
    if (!sessionState.session) return;
    const timer = window.setTimeout(() => void loadAccount(), 0);
    return () => window.clearTimeout(timer);
  }, [loadAccount, sessionState.session]);

  useEffect(() => {
    if (!sessionState.session || sync?.status !== "Running") return;
    const timer = window.setTimeout(() => {
      void refreshSync(sessionState.session!.accessToken).catch((pollError) => setError(describeAdvertisingError(pollError)));
    }, 2500);
    return () => window.clearTimeout(timer);
  }, [refreshSync, sessionState.session, sync?.status]);

  const visibleCampaigns = useMemo(() => filterHierarchy(campaigns.map((campaign) => ({ ...campaign, externalId: campaign.metaCampaignId })), campaignFilters), [campaignFilters, campaigns]);

  if (!sessionState.session) return <main className="structure-page"><SessionGate state={sessionState} /></main>;

  const maySynchronize = canSynchronize(sessionState.session.role);
  const isBusy = isPosting || sync?.status === "Running";

  async function synchronize() {
    if (!maySynchronize || isBusy) return;
    setIsPosting(true);
    setError(null);
    setNotice(null);
    try {
      const result = await apiRequest<SyncResponse>(`/api/v1/ad-accounts/${adAccountId}/sync`, sessionState.session!.accessToken, { method: "POST" });
      setSync(result);
      if (result.status === "Succeeded") {
        await Promise.all([
          refreshSync(sessionState.session!.accessToken),
          loadCampaigns(sessionState.session!.accessToken, showMissing),
        ]);
        setNotice("La estructura se sincronizó correctamente y la lista de campañas fue actualizada.");
      }
    } catch (syncError) {
      setError(describeAdvertisingError(syncError));
      try {
        await refreshSync(sessionState.session!.accessToken);
      } catch {
        // The original error is the most actionable message for this screen.
      }
    } finally {
      setIsPosting(false);
    }
  }

  return (
    <main className="structure-page">
      <StructureHeader eyebrow="Cliente / Cuenta publicitaria" title="Estructura de la cuenta" session={sessionState.session} onBack={() => window.history.back()} />
      <HierarchyRail active="account" />
      <section className="sync-panel" aria-busy={isBusy}>
        <div>
          <p className="eyebrow">Sincronización con Meta</p>
          <h2>Cuenta {adAccountId}</h2>
          <p>{sync?.status === "Running" ? "Meta está actualizando la jerarquía. Consultamos el estado cada 2,5 segundos." : "Sincroniza campañas, conjuntos y anuncios disponibles para esta cuenta."}</p>
        </div>
        <div className="sync-controls">
          {sync ? <SyncBadge status={sync.status} /> : <span className="sync-badge sync-neversynced">Consultando</span>}
          {maySynchronize ? <button className="primary-button" type="button" onClick={() => void synchronize()} disabled={isBusy}>{isBusy ? "Sincronizando…" : "Sincronizar con Meta"}</button> : <p className="permission-note">Viewer puede consultar, pero no iniciar una sincronización.</p>}
        </div>
        {sync && <div className="sync-summary"><span>Inicio: {formatUtc(sync.startedAtUtc)}</span><span>Fin: {formatUtc(sync.completedAtUtc)}</span>{sync.errorCode && <span>Código seguro: {sync.errorCode}</span>}</div>}
      </section>

      {notice && <div className="notice notice-success" role="status">{notice}</div>}
      {error && <div className="notice notice-error" role="alert">{error}</div>}

      <section className="structure-list-section" aria-labelledby="campaign-list-title">
        <div className="section-heading">
          <div><p className="eyebrow">Nivel 2</p><h2 id="campaign-list-title">Campañas</h2></div>
          {(sessionState.session.role === "Owner" || sessionState.session.role === "Admin") && <label className="missing-toggle"><input type="checkbox" checked={showMissing} onChange={(event) => setShowMissing(event.target.checked)} /> Mostrar ausentes de Meta</label>}
        </div>
        {!isLoading && campaigns.length > 0 && <HierarchyFilters items={campaigns.map((campaign) => ({ ...campaign, externalId: campaign.metaCampaignId }))} filters={campaignFilters} onChange={setCampaignFilters} />}
        {isLoading ? <div className="structure-loading">Consultando estado y campañas…</div> : campaigns.length === 0 ? <EmptyState title="No hay campañas para mostrar" body="Sincroniza esta cuenta o habilita la vista administrativa de registros ausentes." /> : visibleCampaigns.length ? <div className="structure-list">{visibleCampaigns.map((campaign) => <article className="structure-row" key={campaign.id}><div><p className="structure-kicker">Campaign · {campaign.metaCampaignId}</p><h3>{campaign.name}</h3><p>{campaign.objective}</p></div><div className="status-pair"><span>Configurado</span><strong>{campaign.configuredStatus}</strong><span>Efectivo</span><strong>{campaign.effectiveStatus}</strong></div><div><PresenceFlag isPresent={campaign.isPresentOnMeta} /><p className="last-sync">Última sync: {formatUtc(campaign.lastSyncedAtUtc)}</p></div><Link className="secondary-button" href={`/app/campanas/${campaign.id}`}>Ver conjuntos</Link></article>)}</div> : <EmptyState title="No hay coincidencias" body="Ajusta o limpia los filtros para volver a ver la jerarquía disponible." />}
      </section>
      <p className="structure-route-note">Cliente interno: {clientId}. La API resuelve la agencia desde tu sesión.</p>
      <MetricsPanel level="Account" resourceId={adAccountId} adAccountId={adAccountId} session={sessionState.session} />
    </main>
  );
}

function normalizeEntity(kind: DetailKind, value: Campaign | AdSet | Ad): EntitySnapshot {
  if (kind === "campaign") {
    const campaign = value as Campaign;
    return { id: campaign.id, name: campaign.name, externalId: campaign.metaCampaignId, configuredStatus: campaign.configuredStatus, effectiveStatus: campaign.effectiveStatus, lastSyncedAtUtc: campaign.lastSyncedAtUtc, isPresentOnMeta: campaign.isPresentOnMeta, attributes: [{ label: "Objetivo", value: campaign.objective }, { label: "Inicio", value: formatUtc(campaign.startsAtUtc) }, { label: "Fin", value: formatUtc(campaign.stopsAtUtc) }] };
  }
  if (kind === "ad-set") {
    const adSet = value as AdSet;
    return { id: adSet.id, name: adSet.name, externalId: adSet.metaAdSetId, configuredStatus: adSet.configuredStatus, effectiveStatus: adSet.effectiveStatus, lastSyncedAtUtc: adSet.lastSyncedAtUtc, isPresentOnMeta: adSet.isPresentOnMeta, attributes: [{ label: "Optimización", value: adSet.optimizationGoal }, { label: "Facturación", value: adSet.billingEvent }, { label: "Inicio", value: formatUtc(adSet.startsAtUtc) }, { label: "Fin", value: formatUtc(adSet.endsAtUtc) }] };
  }
  const ad = value as Ad;
  return { id: ad.id, name: ad.name, externalId: ad.metaAdId, configuredStatus: ad.configuredStatus, effectiveStatus: ad.effectiveStatus, lastSyncedAtUtc: ad.lastSyncedAtUtc, isPresentOnMeta: ad.isPresentOnMeta, attributes: [] };
}

function childConfig(kind: DetailKind, id: string, includeMissing: boolean) {
  const suffix = includeMissing ? "?includeMissing=true" : "";
  if (kind === "campaign") return { path: `/api/v1/campaigns/${id}/ad-sets${suffix}`, nextKind: "ad-set" as const, heading: "Conjuntos de anuncios" };
  if (kind === "ad-set") return { path: `/api/v1/ad-sets/${id}/ads${suffix}`, nextKind: "ad" as const, heading: "Anuncios" };
  return null;
}

function childSnapshot(kind: "ad-set" | "ad", value: AdSet | Ad) {
  return normalizeEntity(kind, value);
}

export function AdvertisingDetailPanel({ kind, id }: { kind: DetailKind; id: string }) {
  const sessionState = useVerifiedSession();
  const [entity, setEntity] = useState<EntitySnapshot | null>(null);
  const [children, setChildren] = useState<EntitySnapshot[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [showMissing, setShowMissing] = useState(false);
  const [childFilters, setChildFilters] = useState<HierarchyFiltersState>(initialHierarchyFilters);
  const [error, setError] = useState<string | null>(null);
  const config = useMemo(() => childConfig(kind, id, showMissing), [id, kind, showMissing]);
  const singular = kind === "campaign" ? "Campaña" : kind === "ad-set" ? "Conjunto de anuncios" : "Anuncio";
  const visibleChildren = useMemo(() => filterHierarchy(children, childFilters), [childFilters, children]);

  const loadDetail = useCallback(async () => {
    if (!sessionState.session) return;
    setIsLoading(true);
    setError(null);
    try {
      const detailPath = kind === "campaign" ? `/api/v1/campaigns/${id}` : kind === "ad-set" ? `/api/v1/ad-sets/${id}` : `/api/v1/ads/${id}`;
      const detail = await apiRequest<Campaign | AdSet | Ad>(detailPath, sessionState.session.accessToken);
      setEntity(normalizeEntity(kind, detail));
      if (config) {
        const childValues = await apiRequest<AdSet[] | Ad[]>(config.path, sessionState.session.accessToken);
        setChildren(childValues.map((value) => childSnapshot(config.nextKind, value)));
      } else {
        setChildren([]);
      }
    } catch (loadError) {
      setError(describeAdvertisingError(loadError));
    } finally {
      setIsLoading(false);
    }
  }, [config, id, kind, sessionState.session]);

  useEffect(() => {
    if (!sessionState.session) return;
    const timer = window.setTimeout(() => void loadDetail(), 0);
    return () => window.clearTimeout(timer);
  }, [loadDetail, sessionState.session]);

  if (!sessionState.session) return <main className="structure-page"><SessionGate state={sessionState} /></main>;

  return (
    <main className="structure-page">
      <StructureHeader eyebrow={`Estructura / ${singular}`} title={entity?.name ?? singular} session={sessionState.session} onBack={() => window.history.back()} />
      <HierarchyRail active={kind} />
      {error && <div className="notice notice-error" role="alert">{error}</div>}
      {isLoading ? <div className="structure-loading">Consultando {singular.toLowerCase()}…</div> : entity ? <>
        <section className="entity-card">
          <div><p className="structure-kicker">ID Meta · {entity.externalId}</p><h2>{entity.name}</h2><p>Última sincronización: {formatUtc(entity.lastSyncedAtUtc)}</p></div>
          <div className="entity-status"><PresenceFlag isPresent={entity.isPresentOnMeta} /><span>Configurado <strong>{entity.configuredStatus}</strong></span><span>Efectivo <strong>{entity.effectiveStatus}</strong></span></div>
          {entity.attributes.length > 0 && <dl>{entity.attributes.map((attribute) => <div key={attribute.label}><dt>{attribute.label}</dt><dd>{attribute.value}</dd></div>)}</dl>}
        </section>
        {config && <section className="structure-list-section" aria-labelledby="children-title"><div className="section-heading"><div><p className="eyebrow">Siguiente nivel</p><h2 id="children-title">{config.heading}</h2></div>{(sessionState.session.role === "Owner" || sessionState.session.role === "Admin") && <label className="missing-toggle"><input type="checkbox" checked={showMissing} onChange={(event) => setShowMissing(event.target.checked)} /> Mostrar ausentes de Meta</label>}</div>{children.length > 0 && <HierarchyFilters items={children} filters={childFilters} onChange={setChildFilters} />}{children.length === 0 ? <EmptyState title={`No hay ${config.heading.toLowerCase()} para mostrar`} body="El backend conserva objetos ausentes; habilita la vista administrativa si corresponde." /> : visibleChildren.length ? <div className="structure-list">{visibleChildren.map((child) => <article className="structure-row" key={child.id}><div><p className="structure-kicker">ID Meta · {child.externalId}</p><h3>{child.name}</h3></div><div className="status-pair"><span>Configurado</span><strong>{child.configuredStatus}</strong><span>Efectivo</span><strong>{child.effectiveStatus}</strong></div><div><PresenceFlag isPresent={child.isPresentOnMeta} /><p className="last-sync">Última sync: {formatUtc(child.lastSyncedAtUtc)}</p></div><Link className="secondary-button" href={config.nextKind === "ad-set" ? `/app/conjuntos/${child.id}` : `/app/anuncios/${child.id}`}>Ver detalle</Link></article>)}</div> : <EmptyState title="No hay coincidencias" body="Ajusta o limpia los filtros para volver a ver la jerarquía disponible." />}</section>}
        <MetricsPanel level={kind === "campaign" ? "Campaign" : kind === "ad-set" ? "AdSet" : "Ad"} resourceId={id} session={sessionState.session} />
      </> : <EmptyState title="No pudimos cargar este objeto" body="Vuelve atrás y verifica que pertenezca a la agencia actual." />}
    </main>
  );
}
