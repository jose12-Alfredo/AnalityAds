"use client";

import { FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import { apiRequest } from "@/lib/api";
import { type AuthSession } from "@/lib/auth-session";
import {
  type CampaignBenchmarksResponse,
  type CampaignSelectorResponse,
  type ComparisonType,
  formatMetric,
  initialMetricRange,
  type InsightLevel,
  type InsightSnapshot,
  type MetricValue,
  type RangeMetricsComparison,
  type RangeMetricsSummary,
  metricsError,
} from "@/lib/metrics";

const metricPath: Record<InsightLevel, (id: string) => string> = {
  Account: (id) => `/api/v1/ad-accounts/${id}/metrics`,
  Campaign: (id) => `/api/v1/campaigns/${id}/metrics`,
  AdSet: (id) => `/api/v1/ad-sets/${id}/metrics`,
  Ad: (id) => `/api/v1/ads/${id}/metrics`,
};

const metricLabels = [
  ["spend", "Inversión", "money"], ["impressions", "Impresiones", "count"], ["linkClicks", "Clics", "count"],
  ["leads", "Leads", "count"], ["purchases", "Compras", "count"], ["cpm", "CPM", "money"],
  ["ctr", "CTR", "percent"], ["cpc", "CPC", "money"], ["cpl", "CPL", "money"], ["cpa", "CPA", "money"], ["roas", "ROAS", "decimal"],
] as const;

function validateRange(since: string, until: string) {
  if (since > until) return "La fecha inicial debe ser anterior o igual a la fecha final.";
  const days = Math.floor((Date.parse(`${until}T00:00:00Z`) - Date.parse(`${since}T00:00:00Z`)) / 86_400_000) + 1;
  return days > 90 ? "El rango máximo permitido es de 90 días inclusivos." : null;
}

function valueFor(summary: RangeMetricsSummary, key: string) {
  return key in summary.observed ? summary.observed[key as keyof typeof summary.observed] : summary.derived[key as keyof typeof summary.derived];
}

function displayRangeMetric(metric: MetricValue, kind: string) {
  if (metric.availability !== "CompleteForSnapshots") return "—";
  const options = kind === "money" ? { minimumFractionDigits: 2, maximumFractionDigits: 2 } : kind === "count" ? { maximumFractionDigits: 0 } : { maximumFractionDigits: 2 };
  const value = formatMetric(metric.value, options);
  return kind === "percent" && value !== "—" ? `${value}%` : value;
}

export function MetricsInsightsPanel({ level, resourceId, adAccountId, session, onCampaignSelect }: { level: InsightLevel; resourceId: string; adAccountId?: string; session: AuthSession; onCampaignSelect?: (campaignId: string, name: string) => void }) {
  const initialRange = useMemo(() => initialMetricRange(), []);
  const [since, setSince] = useState(initialRange.since);
  const [until, setUntil] = useState(initialRange.until);
  const [applied, setApplied] = useState(initialRange);
  const [snapshots, setSnapshots] = useState<InsightSnapshot[]>([]);
  const [summary, setSummary] = useState<RangeMetricsSummary | null>(null);
  const [selector, setSelector] = useState<CampaignSelectorResponse | null>(null);
  const [benchmarks, setBenchmarks] = useState<CampaignBenchmarksResponse | null>(null);
  const [comparison, setComparison] = useState<RangeMetricsComparison | null>(null);
  const [comparisonType, setComparisonType] = useState<ComparisonType>("PreviousPeriod");
  const [comparisonSince, setComparisonSince] = useState("");
  const [comparisonUntil, setComparisonUntil] = useState("");
  const [activity, setActivity] = useState<"WithActivity" | "WithSpend" | "All">("WithActivity");
  const [isLoading, setIsLoading] = useState(true);
  const [isComparing, setIsComparing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    const query = new URLSearchParams(applied).toString();
    try {
      const base = metricPath[level](resourceId);
      const requests: [Promise<InsightSnapshot[]>, Promise<RangeMetricsSummary>, Promise<CampaignSelectorResponse | null>, Promise<CampaignBenchmarksResponse | null>] = [
        apiRequest<InsightSnapshot[]>(`${base}?${query}`, session.accessToken),
        apiRequest<RangeMetricsSummary>(`${base}/summary?${query}`, session.accessToken),
        level === "Account" ? apiRequest<CampaignSelectorResponse>(`/api/v1/ad-accounts/${adAccountId ?? resourceId}/campaigns/selector?${query}&activity=${activity}`, session.accessToken) : Promise.resolve(null),
        level === "Account" ? apiRequest<CampaignBenchmarksResponse>(`/api/v1/ad-accounts/${adAccountId ?? resourceId}/campaigns/benchmarks?${query}`, session.accessToken) : Promise.resolve(null),
      ];
      const [nextSnapshots, nextSummary, nextSelector, nextBenchmarks] = await Promise.all(requests);
      setSnapshots(nextSnapshots); setSummary(nextSummary); setSelector(nextSelector); setBenchmarks(nextBenchmarks);
    } catch (requestError) {
      setSnapshots([]); setSummary(null); setSelector(null); setBenchmarks(null);
      setError(metricsError(requestError));
    } finally { setIsLoading(false); }
  }, [activity, adAccountId, applied, level, resourceId, session.accessToken]);

  useEffect(() => { const timer = window.setTimeout(() => void load(), 0); return () => window.clearTimeout(timer); }, [load]);

  function applyRange(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const issue = validateRange(since, until);
    if (issue) { setError(issue); return; }
    setApplied({ since, until });
  }

  async function loadComparison(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const issue = validateRange(since, until);
    if (issue) { setError(issue); return; }
    if (comparisonType === "Custom") {
      if (!comparisonSince || !comparisonUntil) { setError("Indica ambas fechas para el período personalizado."); return; }
      const comparisonIssue = validateRange(comparisonSince, comparisonUntil);
      if (comparisonIssue || comparisonUntil >= since) { setError(comparisonIssue || "El período de comparación debe terminar antes del rango actual."); return; }
    }
    setIsComparing(true); setError(null);
    try {
      const query = new URLSearchParams({ since: applied.since, until: applied.until, comparison: comparisonType });
      if (comparisonType === "Custom") { query.set("comparisonSince", comparisonSince); query.set("comparisonUntil", comparisonUntil); }
      const result = await apiRequest<RangeMetricsComparison>(`${metricPath[level](resourceId)}/comparison?${query}`, session.accessToken);
      setComparison(result);
    } catch (requestError) { setComparison(null); setError(metricsError(requestError)); }
    finally { setIsComparing(false); }
  }

  const hasLegacy = snapshots.some((snapshot) => snapshot.observedDataQuality === "LegacyZeroNormalized") || (summary?.coverage.legacyZeroNormalizedSnapshotDays ?? 0) > 0;

  return <section className="metric-insights" aria-labelledby={`metric-insights-${resourceId}`}>
    <div className="section-heading"><div><p className="eyebrow">Datos verificados por rango</p><h2 id={`metric-insights-${resourceId}`}>Métricas y comparaciones</h2></div>{summary && <span className="metrics-currency">{summary.currencyStatus === "Single" ? summary.currency : summary.currencyStatus === "Mixed" ? "Moneda mixta" : "Sin moneda"}</span>}</div>
    <p className="metrics-intro">Los totales y ratios proceden del resumen seguro del backend. Reach y frecuencia no se agregan para un rango.</p>
    <form className="metrics-range" onSubmit={applyRange}><label>Desde<input type="date" value={since} onChange={(event) => setSince(event.target.value)} required /></label><label>Hasta<input type="date" value={until} onChange={(event) => setUntil(event.target.value)} required /></label><button className="text-button" type="submit" disabled={isLoading}>Actualizar rango</button></form>
    {error && <div className="notice notice-error" role="alert">{error}</div>}
    {hasLegacy && <div className="notice notice-warning" role="status">Datos históricos por re-sincronizar: algunas filas no distinguen entre ausencia y cero explícito.</div>}
    {summary?.currencyStatus === "Mixed" && <div className="notice notice-warning" role="status">El rango contiene más de una moneda. Los importes y ratios dependientes de moneda no se presentan como un total.</div>}
    {isLoading ? <div className="structure-loading">Consultando series y resúmenes seguros…</div> : summary ? <>
      <div className="safe-metric-grid">{metricLabels.map(([key, label, kind]) => { const metric = valueFor(summary, key); return <article key={key} className="safe-metric"><p>{label}</p><strong>{displayRangeMetric(metric, kind)}</strong><small>{metric.availability}</small></article>; })}</div>
      <div className="coverage-strip"><span>{summary.coverage.snapshotDays} de {summary.coverage.requestedDays} días con snapshot</span><span>{summary.coverage.firstSnapshotDate ?? "Sin primer día"} — {summary.coverage.lastSnapshotDate ?? "Sin último día"}</span><span>Reach y frecuencia: no disponibles para rango</span></div>
      <DailyTable snapshots={snapshots} />
      <form className="comparison-controls" onSubmit={loadComparison}><div><p className="eyebrow">Comparar períodos</p><h3>Periodo base</h3></div><label>Comparación<select value={comparisonType} onChange={(event) => setComparisonType(event.target.value as ComparisonType)}><option value="PreviousPeriod">Período anterior</option><option value="PreviousMonth">Mes anterior</option><option value="PreviousYear">Año anterior</option><option value="Custom">Personalizado</option></select></label>{comparisonType === "Custom" && <><label>Desde comparación<input type="date" value={comparisonSince} onChange={(event) => setComparisonSince(event.target.value)} required /></label><label>Hasta comparación<input type="date" value={comparisonUntil} onChange={(event) => setComparisonUntil(event.target.value)} required /></label></>}<button className="secondary-button" type="submit" disabled={isComparing}>{isComparing ? "Comparando…" : "Ver comparación"}</button></form>
      {comparison && <ComparisonTable comparison={comparison} />}
      {level === "Account" && <><section className="campaign-selector-section"><div className="section-heading"><div><p className="eyebrow">Campañas del rango</p><h3>Selector autorizado</h3></div><label className="selector-filter">Actividad<select value={activity} onChange={(event) => setActivity(event.target.value as typeof activity)}><option value="WithActivity">Con actividad</option><option value="WithSpend">Con gasto</option><option value="All">Todas</option></select></label></div>{selector ? <div className="campaign-selector-list">{selector.campaigns.map((campaign) => <button key={campaign.id} type="button" onClick={() => onCampaignSelect?.(campaign.id, campaign.name)}><span><strong>{campaign.name}</strong><small>{campaign.objective} · {campaign.effectiveStatus}</small></span><span><b>{displayRangeMetric(campaign.spend, "money")}</b><small>{campaign.spend.availability}</small></span></button>)}</div> : null}</section>
      <BenchmarksTable benchmarks={benchmarks} /></>}</>
    : <div className="structure-empty"><h3>Sin resumen para este rango</h3><p>No hay totales verificables hasta que existan snapshots compatibles.</p></div>}
  </section>;
}

function DailyTable({ snapshots }: { snapshots: InsightSnapshot[] }) {
  if (!snapshots.length) return <div className="structure-empty"><h3>Sin observaciones para este rango</h3><p>Una fecha sin actividad no se convierte en una fila con ceros.</p></div>;
  return <div className="metrics-table-wrap"><table className="metrics-table"><thead><tr><th>Fecha</th><th>Gasto</th><th>Impresiones</th><th>Reach diario</th><th>Clics</th><th>Compras</th><th>Calidad</th></tr></thead><tbody>{snapshots.map((snapshot) => <tr key={snapshot.id}><td><strong>{snapshot.date}</strong><span>{snapshot.currency}</span></td><td>{formatMetric(snapshot.observed.spend, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</td><td>{formatMetric(snapshot.observed.impressions)}</td><td>{formatMetric(snapshot.observed.reach)}</td><td>{formatMetric(snapshot.observed.linkClicks)}</td><td>{formatMetric(snapshot.observed.purchases)}</td><td>{snapshot.observedDataQuality === "LegacyZeroNormalized" ? "Histórico por re-sincronizar" : "Campo preservado"}</td></tr>)}</tbody></table></div>;
}

function ComparisonTable({ comparison }: { comparison: RangeMetricsComparison }) {
  const rows = [["spend", "Inversión", comparison.observed.spend], ["impressions", "Impresiones", comparison.observed.impressions], ["linkClicks", "Clics", comparison.observed.linkClicks], ["purchases", "Compras", comparison.observed.purchases], ["cpm", "CPM", comparison.derived.cpm], ["ctr", "CTR", comparison.derived.ctr], ["cpa", "CPA", comparison.derived.cpa], ["roas", "ROAS", comparison.derived.roas]] as const;
  return <section className="comparison-table-wrap"><p className="comparison-caption">Comparación: {comparison.comparisonType}. Un porcentaje solo aparece si el backend confirma un baseline válido.</p><table className="metrics-table"><thead><tr><th>Métrica</th><th>Actual</th><th>Base</th><th>Cambio</th><th>Estado</th></tr></thead><tbody>{rows.map(([key, label, metric]) => <tr key={key}><td>{label}</td><td>{formatMetric(metric.current)}</td><td>{formatMetric(metric.baseline)}</td><td>{metric.availability === "Available" ? <>{formatMetric(metric.absoluteChange)} {metric.percentageChange === null ? "" : `(${formatMetric(metric.percentageChange)}%)`}</> : "—"}</td><td>{metric.availability}</td></tr>)}</tbody></table></section>;
}

function BenchmarksTable({ benchmarks }: { benchmarks: CampaignBenchmarksResponse | null }) {
  if (!benchmarks) return null;
  const names = ["cpm", "ctr", "cpc", "cpl", "cpa", "roas"] as const;
  return <section className="benchmark-section"><div><p className="eyebrow">Benchmark interno</p><h3>Campañas con el mismo objetivo</h3><p>La referencia es la mediana de otras campañas comparables; no es una regla universal.</p></div>{benchmarks.campaigns.length ? <div className="metrics-table-wrap"><table className="metrics-table"><thead><tr><th>Campaña</th>{names.map((name) => <th key={name}>{name.toUpperCase()}</th>)}</tr></thead><tbody>{benchmarks.campaigns.map((campaign) => <tr key={campaign.campaignId}><td><strong>{campaign.name}</strong><span>{campaign.objective}</span></td>{names.map((name) => { const metric = campaign[name]; return <td key={name}><strong>{metric.availability === "Available" ? formatMetric(metric.campaignValue) : "—"}</strong><span>Mediana: {metric.availability === "Available" ? formatMetric(metric.benchmarkValue) : "—"}</span><small>{metric.comparableCampaigns} comparables · {metric.availability}</small></td>; })}</tr>)}</tbody></table></div> : <div className="structure-empty"><h3>Sin campañas comparables</h3><p>Se requieren al menos dos campañas comparables además de la evaluada.</p></div>}</section>;
}
