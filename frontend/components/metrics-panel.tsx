"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import { apiRequest } from "@/lib/api";
import { type AuthSession } from "@/lib/auth-session";
import { initialMetricRange, type InsightLevel, type InsightSnapshot, type MetricsSyncResponse, formatMetric, metricsError } from "@/lib/metrics";

const pathForLevel: Record<InsightLevel, (id: string) => string> = {
  Account: (id) => `/api/v1/ad-accounts/${id}/metrics`,
  Campaign: (id) => `/api/v1/campaigns/${id}/metrics`,
  AdSet: (id) => `/api/v1/ad-sets/${id}/metrics`,
  Ad: (id) => `/api/v1/ads/${id}/metrics`,
};

function canSync(role: AuthSession["role"]) {
  return role === "Owner" || role === "Admin" || role === "Analyst";
}

export function MetricsPanel({ level, resourceId, adAccountId, session }: { level: InsightLevel; resourceId: string; adAccountId?: string; session: AuthSession }) {
  const initialRange = initialMetricRange();
  const [since, setSince] = useState(initialRange.since);
  const [until, setUntil] = useState(initialRange.until);
  const [snapshots, setSnapshots] = useState<InsightSnapshot[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSyncing, setIsSyncing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [syncResult, setSyncResult] = useState<MetricsSyncResponse | null>(null);

  const loadSnapshots = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const query = new URLSearchParams({ since, until });
      const result = await apiRequest<InsightSnapshot[]>(`${pathForLevel[level](resourceId)}?${query}`, session.accessToken);
      setSnapshots(result);
    } catch (requestError) {
      setError(metricsError(requestError));
    } finally {
      setIsLoading(false);
    }
  }, [level, resourceId, session.accessToken, since, until]);

  useEffect(() => {
    const timer = window.setTimeout(() => void loadSnapshots(), 0);
    return () => window.clearTimeout(timer);
  }, [loadSnapshots]);

  async function queryMetrics(event: FormEvent<HTMLFormElement>) {
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
    await loadSnapshots();
  }

  async function syncMetrics() {
    if (!adAccountId || !canSync(session.role) || isSyncing) return;
    setIsSyncing(true);
    setError(null);
    try {
      const result = await apiRequest<MetricsSyncResponse>(`/api/v1/ad-accounts/${adAccountId}/metrics/sync`, session.accessToken, {
        method: "POST",
        body: JSON.stringify({ since, until }),
      });
      setSyncResult(result);
      await loadSnapshots();
    } catch (requestError) {
      setError(metricsError(requestError));
    } finally {
      setIsSyncing(false);
    }
  }

  return (
    <section className="metrics-panel" aria-labelledby={`metrics-${resourceId}`}>
      <div className="section-heading">
        <div><p className="eyebrow">Snapshots diarios</p><h2 id={`metrics-${resourceId}`}>Métricas observadas y derivadas</h2></div>
        {level === "Account" && (canSync(session.role) ? <button className="secondary-button" type="button" onClick={() => void syncMetrics()} disabled={isSyncing}>{isSyncing ? "Sincronizando métricas…" : "Sincronizar métricas"}</button> : <span className="metrics-role-note">Viewer solo consulta</span>)}
      </div>
      <p className="metrics-intro">Cada fila es un snapshot diario entregado por el backend. No agregamos ni recalculamos métricas, incluido reach.</p>
      <form className="metrics-range" onSubmit={(event) => void queryMetrics(event)}>
        <label>Desde<input type="date" value={since} onChange={(event) => setSince(event.target.value)} required /></label>
        <label>Hasta<input type="date" value={until} onChange={(event) => setUntil(event.target.value)} required /></label>
        <button className="text-button" type="submit" disabled={isLoading}>Consultar</button>
      </form>
      {syncResult && <div className="notice notice-success" role="status">Métricas sincronizadas: {syncResult.accountSnapshots} de cuenta, {syncResult.campaignSnapshots} de campañas, {syncResult.adSetSnapshots} de conjuntos y {syncResult.adSnapshots} de anuncios.</div>}
      {error && <div className="notice notice-error" role="alert">{error}</div>}
      {isLoading ? <div className="structure-loading">Consultando snapshots diarios…</div> : snapshots.length ? <div className="metrics-table-wrap"><table className="metrics-table"><thead><tr><th>Fecha</th><th>Observado</th><th>Derivado</th><th>Registro</th></tr></thead><tbody>{snapshots.map((snapshot) => <tr key={snapshot.id}><td><strong>{snapshot.date}</strong><span>{snapshot.currency}</span></td><td><span>Spend: {formatMetric(snapshot.observed.spend, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span><span>Imp.: {formatMetric(snapshot.observed.impressions)}</span><span>Reach: {formatMetric(snapshot.observed.reach)}</span><span>Clics: {formatMetric(snapshot.observed.linkClicks)}</span><span>Leads: {formatMetric(snapshot.observed.leads)}</span><span>Compras: {formatMetric(snapshot.observed.purchases)}</span></td><td><span>Frecuencia: {formatMetric(snapshot.derived.frequency)}</span><span>CPM: {formatMetric(snapshot.derived.cpm)}</span><span>CTR: {formatMetric(snapshot.derived.ctr)}{snapshot.derived.ctr === null ? "" : "%"}</span><span>CPC: {formatMetric(snapshot.derived.cpc)}</span><span>CPL: {formatMetric(snapshot.derived.cpl)}</span><span>CPA: {formatMetric(snapshot.derived.cpa)}</span><span>ROAS: {formatMetric(snapshot.derived.roas)}</span></td><td><span>{snapshot.level}</span><span>{new Intl.DateTimeFormat("es-BO", { dateStyle: "medium", timeStyle: "short" }).format(new Date(snapshot.observedAtUtc))}</span></td></tr>)}</tbody></table></div> : <div className="structure-empty"><h3>Sin snapshots para este rango</h3><p>Una fecha sin actividad puede no producir filas. No mostramos ceros inventados.</p></div>}
    </section>
  );
}
