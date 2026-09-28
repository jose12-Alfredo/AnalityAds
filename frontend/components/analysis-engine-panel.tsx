"use client";

import { FormEvent, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { ApiError, apiRequest } from "@/lib/api";
import { type Campaign } from "@/lib/advertising";
import { canCreateReports, type AuthSession } from "@/lib/auth-session";
import {
  analysisMetrics,
  type AnalysisComparisonType,
  type AnalysisEntity,
  type AnalysisEvidence,
  type AnalysisInsight,
  type AnalysisMetric,
  type AnalysisMetricKey,
  type AnalysisRecommendation,
  type AnalysisResult,
  type CreateAnalysisRequest,
} from "@/lib/analysis";
import { type CreateReportRequest, type ReportData } from "@/lib/reports";
import { formatMetric } from "@/lib/metrics";

type CampaignScope = "all" | "account" | "selected";

function validateRange(since: string, until: string) {
  if (!since || !until) return "Indica la fecha inicial y final del análisis.";
  if (since > until) return "La fecha inicial debe ser anterior o igual a la fecha final.";
  const days = Math.floor((Date.parse(`${until}T00:00:00Z`) - Date.parse(`${since}T00:00:00Z`)) / 86_400_000) + 1;
  return days > 90 ? "El rango máximo permitido es de 90 días inclusivos." : null;
}

function analysisError(error: unknown) {
  if (!(error instanceof ApiError)) return error instanceof Error ? error.message : "No pudimos generar el análisis.";
  if (error.status === 400) return error.problem.detail || "Revisa las fechas, campañas y métricas seleccionadas.";
  if (error.status === 401) return "Tu sesión venció. Inicia sesión nuevamente.";
  if (error.status === 403) return "Tu rol no tiene permiso para consultar este análisis.";
  if (error.status === 404) return "Esta cuenta ya no está disponible dentro de tus accesos autorizados.";
  if (error.status === 409) return error.problem.detail || "No se pudo completar el análisis por un conflicto.";
  if (error.status === 429) return "Hay demasiadas solicitudes. Espera un momento antes de reintentar.";
  if (error.status >= 500) return "El servidor no pudo completar el análisis. Intenta nuevamente.";
  return error.problem.detail || "No pudimos generar el análisis.";
}

function metricValue(metric: AnalysisMetric | undefined, kind: string) {
  if (!metric || metric.availability !== "CompleteForSnapshots" || metric.value === null) return "—";
  const options = kind === "money"
    ? { minimumFractionDigits: 2, maximumFractionDigits: 2 }
    : kind === "count"
      ? { maximumFractionDigits: 0 }
      : { maximumFractionDigits: 2 };
  const value = formatMetric(metric.value, options);
  return kind === "percent" ? `${value}%` : value;
}

function compactMetricValue(value: number | null, kind: string) {
  if (value === null) return "—";
  const formatted = formatMetric(value, kind === "count" ? { maximumFractionDigits: 0 } : { maximumFractionDigits: 2 });
  return kind === "percent" ? `${formatted}%` : formatted;
}

function statusLabel(status: string) {
  return status === "Sufficient" ? "Suficiente" : status === "Partial" ? "Parcial" : "Insuficiente";
}

function evidenceMetric(metric: string) {
  if (metric === "topCampaignSpend") return { label: "Inversión de campañas principales", kind: "money" };
  if (metric === "campaignCount") return { label: "Cantidad de campañas", kind: "count" };
  const match = analysisMetrics.find(([key]) => key === metric);
  return match ? { label: match[1], kind: match[2] } : { label: metric, kind: "decimal" };
}

function evidenceValue(value: number | null, evidence: AnalysisEvidence, kind: string) {
  return evidence.availability === "Available" ? compactMetricValue(value, kind) : "—";
}

function entityNames(result: AnalysisResult, entityIds: string[]) {
  const names = new Map<string, string>([
    [result.account.id, result.account.name],
    ...[...result.campaigns, ...result.adSets, ...result.ads].map((entity) => [entity.id, entity.name] as const),
  ]);
  return entityIds.map((id) => names.get(id) ?? id);
}

export function AnalysisEnginePanel({
  clientId,
  adAccountId,
  accountName,
  campaigns,
  session,
  initialRange,
  onNotFound,
}: {
  clientId: string;
  adAccountId: string;
  accountName: string;
  campaigns: Campaign[];
  session: AuthSession;
  initialRange: { since: string; until: string };
  onNotFound: () => void;
}) {
  const router = useRouter();
  const [since, setSince] = useState(initialRange.since);
  const [until, setUntil] = useState(initialRange.until);
  const [comparison, setComparison] = useState<AnalysisComparisonType | "">("PreviousPeriod");
  const [comparisonSince, setComparisonSince] = useState("");
  const [comparisonUntil, setComparisonUntil] = useState("");
  const [campaignScope, setCampaignScope] = useState<CampaignScope>("all");
  const [campaignIds, setCampaignIds] = useState<string[]>([]);
  const [selectedMetrics, setSelectedMetrics] = useState<AnalysisMetricKey[]>([]);
  const [result, setResult] = useState<AnalysisResult | null>(null);
  const [reportRequest, setReportRequest] = useState<CreateAnalysisRequest | null>(null);
  const [reportTitle, setReportTitle] = useState("");
  const [isSavingReport, setIsSavingReport] = useState(false);
  const [reportNotice, setReportNotice] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  const availableCampaignIds = useMemo(() => new Set(campaigns.map((campaign) => campaign.id)), [campaigns]);

  function toggleCampaign(id: string) {
    setCampaignIds((current) => current.includes(id) ? current.filter((item) => item !== id) : [...current, id]);
  }

  function toggleMetric(metric: AnalysisMetricKey) {
    setSelectedMetrics((current) => current.includes(metric) ? current.filter((item) => item !== metric) : [...current, metric]);
  }

  async function generateAnalysis(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const rangeIssue = validateRange(since, until);
    if (rangeIssue) { setError(rangeIssue); return; }
    if (comparison === "Custom") {
      const comparisonIssue = validateRange(comparisonSince, comparisonUntil);
      if (comparisonIssue || comparisonUntil >= since) {
        setError(comparisonIssue || "El período personalizado debe terminar antes del rango actual.");
        return;
      }
    }
    if (campaignScope === "selected" && !campaignIds.length) {
      setError("Selecciona al menos una campaña o elige otro alcance.");
      return;
    }
    if (campaignIds.some((id) => !availableCampaignIds.has(id))) {
      setError("La selección contiene una campaña que ya no pertenece a la cuenta actual.");
      return;
    }

    const request: CreateAnalysisRequest = {
      adAccountId,
      since,
      until,
      comparison: comparison || null,
      comparisonSince: comparison === "Custom" ? comparisonSince : null,
      comparisonUntil: comparison === "Custom" ? comparisonUntil : null,
      campaignIds: campaignScope === "all" ? null : campaignScope === "account" ? [] : [...new Set(campaignIds)],
      selectedMetrics: selectedMetrics.length ? selectedMetrics : null,
    };

    setIsLoading(true);
    setError(null);
    try {
      const nextResult = await apiRequest<AnalysisResult>("/api/v1/analyses", session.accessToken, {
        method: "POST",
        body: JSON.stringify(request),
      });
      setResult(nextResult);
      setReportRequest(request);
      setReportNotice(null);
    } catch (requestError) {
      setResult(null);
      setError(analysisError(requestError));
      if (requestError instanceof ApiError && requestError.status === 404) onNotFound();
    } finally {
      setIsLoading(false);
    }
  }

  async function createReport(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!reportRequest || !reportTitle.trim() || !canCreateReports(session.role) || isSavingReport) return;
    setIsSavingReport(true);
    setReportNotice(null);
    try {
      const request: CreateReportRequest = { ...reportRequest, title: reportTitle.trim() };
      const report = await apiRequest<ReportData>("/api/v1/reports", session.accessToken, {
        method: "POST",
        body: JSON.stringify(request),
      });
      router.push(`/app/clientes/${clientId}/reportes/${report.reportId}`);
    } catch (requestError) {
      setReportNotice(analysisError(requestError));
      if (requestError instanceof ApiError && requestError.status === 404) onNotFound();
    } finally {
      setIsSavingReport(false);
    }
  }

  return <section className="analysis-engine" aria-labelledby={`analysis-engine-${adAccountId}`}>
    <div className="analysis-engine-heading">
      <div><p className="eyebrow">AnalysisEngine · evidencia del backend</p><h2 id={`analysis-engine-${adAccountId}`}>Generar análisis</h2><p>Consulta evidencia, insights y recomendaciones del servidor para <strong>{accountName}</strong>. Esta vista no realiza cálculos propios.</p></div>
      <span className="analysis-account-tag">Cuenta seleccionada</span>
    </div>
    <form className="analysis-form" onSubmit={generateAnalysis}>
      <div className="analysis-form-grid">
        <label>Desde<input type="date" value={since} onChange={(event) => setSince(event.target.value)} required /></label>
        <label>Hasta<input type="date" value={until} onChange={(event) => setUntil(event.target.value)} required /></label>
        <label>Comparación<select value={comparison} onChange={(event) => setComparison(event.target.value as AnalysisComparisonType | "")}><option value="">Sin comparación</option><option value="PreviousPeriod">Período anterior</option><option value="PreviousMonth">Mes anterior</option><option value="PreviousYear">Año anterior</option><option value="Custom">Personalizada</option></select></label>
        {comparison === "Custom" && <><label>Desde comparación<input type="date" value={comparisonSince} onChange={(event) => setComparisonSince(event.target.value)} required /></label><label>Hasta comparación<input type="date" value={comparisonUntil} onChange={(event) => setComparisonUntil(event.target.value)} required /></label></>}
      </div>
      <fieldset className="analysis-selection"><legend>Alcance de campañas</legend><div className="analysis-options"><label><input type="radio" name={`campaign-scope-${adAccountId}`} checked={campaignScope === "all"} onChange={() => setCampaignScope("all")} />Todas las campañas autorizadas</label><label><input type="radio" name={`campaign-scope-${adAccountId}`} checked={campaignScope === "account"} onChange={() => setCampaignScope("account")} />Solo la cuenta</label><label><input type="radio" name={`campaign-scope-${adAccountId}`} checked={campaignScope === "selected"} onChange={() => setCampaignScope("selected")} />Campañas concretas</label></div>{campaignScope === "selected" && <div className="analysis-checkbox-grid">{campaigns.length ? campaigns.map((campaign) => <label key={campaign.id}><input type="checkbox" checked={campaignIds.includes(campaign.id)} onChange={() => toggleCampaign(campaign.id)} />{campaign.name}</label>) : <p className="field-note">No hay campañas autorizadas disponibles para seleccionar.</p>}</div>}</fieldset>
      <fieldset className="analysis-selection"><legend>Métricas</legend><div className="analysis-selection-title"><button className="text-button" type="button" onClick={() => setSelectedMetrics([])}>Todas las métricas</button></div><p className="field-note">Sin selección, el backend devuelve las 14 métricas disponibles.</p><div className="analysis-checkbox-grid metrics-checkbox-grid">{analysisMetrics.map(([key, label]) => <label key={key}><input type="checkbox" checked={selectedMetrics.includes(key)} onChange={() => toggleMetric(key)} />{label}</label>)}</div></fieldset>
      <div className="analysis-form-actions"><button className="primary-button" type="submit" disabled={isLoading}>{isLoading ? "Generando análisis…" : "Generar análisis"}</button><p>El servidor valida acceso, aislamiento, pertenencia de campañas y disponibilidad de datos.</p></div>
    </form>
    {error && <div className="notice notice-error" role="alert">{error}</div>}
    {result && <AnalysisResults result={result} />}
    {result && reportRequest && canCreateReports(session.role) && <section className="analysis-report-create" aria-labelledby={`report-create-${adAccountId}`}>
      <div><p className="eyebrow">Snapshot inmutable</p><h3 id={`report-create-${adAccountId}`}>Guardar como reporte</h3><p>El servidor vuelve a evaluar esta misma selección y guarda su resultado histórico. El reporte no se reconstruye con métricas futuras.</p></div>
      <form onSubmit={createReport}><label>Título del reporte<input value={reportTitle} onChange={(event) => setReportTitle(event.target.value)} required minLength={1} maxLength={200} placeholder="Ej. Reporte septiembre 2026" /></label><button className="secondary-button" type="submit" disabled={isSavingReport}>{isSavingReport ? "Guardando reporte…" : "Guardar reporte"}</button></form>
      {reportNotice && <div className="notice notice-error" role="alert">{reportNotice}</div>}
    </section>}
  </section>;
}

export function AnalysisResults({ result }: { result: AnalysisResult }) {
  return <div className="analysis-results" aria-live="polite">
    <div className="analysis-result-header"><div><p className="eyebrow">Resultado evaluado</p><h3>{result.since} a {result.until}</h3></div><span>{result.comparisonType ?? "Sin comparación"}</span></div>
    <AnalysisEntityCard entity={result.account} title="Resultado general de la cuenta" />
    <AnalysisEntityGroup title="Campañas" entities={result.campaigns} />
    <AnalysisEntityGroup title="Conjuntos de anuncios" entities={result.adSets} />
    <AnalysisEntityGroup title="Anuncios" entities={result.ads} />
    <AnalysisInsights insights={result.insights} result={result} />
    <AnalysisRecommendations recommendations={result.recommendations} result={result} />
    <section className="analysis-unavailable" aria-labelledby="analysis-unavailable-title"><p className="eyebrow">Desglose pendiente</p><h3 id="analysis-unavailable-title">Secciones no disponibles</h3><div>{result.unavailableSections.map((section) => <article key={section.section}><strong>{section.section}</strong><p>{section.reason}</p></article>)}</div></section>
  </div>;
}

function AnalysisInsights({ insights, result }: { insights: AnalysisInsight[]; result: AnalysisResult }) {
  return <section className="analysis-findings" aria-labelledby="analysis-insights-title">
    <div className="analysis-findings-heading"><div><p className="eyebrow">Reglas evaluadas por el backend</p><h3 id="analysis-insights-title">Insights</h3></div><span>{insights.length}</span></div>
    {insights.length ? <div className="analysis-findings-list">{insights.map((insight, index) => <article className="analysis-insight" key={`${insight.ruleId}-${index}`}>
      <div className="analysis-finding-topline"><div><span className={`analysis-severity analysis-severity-${insight.severity.toLowerCase()}`}>{insight.severity}</span><span className="analysis-finding-level">{insight.level}</span></div><code>{insight.ruleId}</code></div>
      <p className="analysis-finding-message">{insight.message}</p>
      <dl className="analysis-finding-meta"><div><dt>Confianza</dt><dd>{insight.confidence}</dd></div><div><dt>Evidencia</dt><dd>{statusLabel(insight.sufficiency)}</dd></div><div><dt>Entidades</dt><dd>{entityNames(result, insight.entityIds).join(", ") || "Cuenta"}</dd></div></dl>
      <AnalysisEvidenceTable evidence={insight.evidence} />
    </article>)}</div> : <p className="analysis-findings-empty">No se activaron reglas con evidencia suficiente para este alcance.</p>}
  </section>;
}

function AnalysisEvidenceTable({ evidence }: { evidence: AnalysisEvidence[] }) {
  if (!evidence.length) return null;
  return <div className="analysis-table-wrap"><table className="analysis-table analysis-evidence-table"><caption>Evidencia entregada por el backend</caption><thead><tr><th>Métrica</th><th>Valor</th><th>Referencia</th><th>Diferencia</th><th>Estado</th></tr></thead><tbody>{evidence.map((item, index) => {
    const metric = evidenceMetric(item.metric);
    return <tr key={`${item.metric}-${index}`}><td>{metric.label}</td><td>{evidenceValue(item.value, item, metric.kind)}</td><td>{evidenceValue(item.referenceValue, item, metric.kind)}</td><td>{item.availability === "Available" && item.percentageDifference !== null ? `${formatMetric(item.percentageDifference, { maximumFractionDigits: 2 })}%` : "—"}</td><td>{item.availability}</td></tr>;
  })}</tbody></table></div>;
}

function AnalysisRecommendations({ recommendations, result }: { recommendations: AnalysisRecommendation[]; result: AnalysisResult }) {
  return <section className="analysis-findings analysis-recommendations" aria-labelledby="analysis-recommendations-title">
    <div className="analysis-findings-heading"><div><p className="eyebrow">Próximos pasos del backend</p><h3 id="analysis-recommendations-title">Recomendaciones</h3></div><span>{recommendations.length}</span></div>
    {recommendations.length ? <div className="analysis-findings-list">{recommendations.map((recommendation, index) => <article className="analysis-recommendation" key={`${recommendation.ruleId}-${index}`}>
      <div className="analysis-finding-topline"><div><span className={`analysis-priority analysis-priority-${recommendation.priority.toLowerCase()}`}>Prioridad {recommendation.priority}</span><span className="analysis-finding-level">{recommendation.level}</span></div><code>{recommendation.ruleId}</code></div>
      <p className="analysis-finding-message">{recommendation.message}</p>
      <p className="analysis-affected-entities"><strong>Entidades:</strong> {entityNames(result, recommendation.entityIds).join(", ") || "Cuenta"}</p>
      {recommendation.actions.length > 0 && <ol className="analysis-actions">{recommendation.actions.map((action) => <li key={action}>{action}</li>)}</ol>}
    </article>)}</div> : <p className="analysis-findings-empty">El backend no propuso acciones para este alcance.</p>}
  </section>;
}

function AnalysisEntityGroup({ title, entities }: { title: string; entities: AnalysisEntity[] }) {
  return <details className="analysis-entity-group" open={entities.length > 0}><summary><span>{title}</span><small>{entities.length} resultados</small></summary>{entities.length ? <div>{entities.map((entity) => <AnalysisEntityCard key={entity.id} entity={entity} />)}</div> : <p className="analysis-empty">No hay resultados para este alcance.</p>}</details>;
}

function AnalysisEntityCard({ entity, title }: { entity: AnalysisEntity; title?: string }) {
  return <article className="analysis-entity-card">
    <div className="analysis-entity-topline"><div><p className="eyebrow">{title ?? entity.level}</p><h4>{entity.name}</h4>{entity.objective && <small>{entity.objective}</small>}</div><span className={`analysis-sufficiency analysis-sufficiency-${entity.sufficiency.status.toLowerCase()}`}>{statusLabel(entity.sufficiency.status)}</span></div>
    <div className="analysis-evidence-strip"><span>{entity.coverage.snapshotDays} de {entity.coverage.requestedDays} días con snapshot</span><span>{entity.currency ?? "Sin moneda consolidable"}</span><span>{entity.coverage.firstSnapshotDate ?? "Sin primer snapshot"} — {entity.coverage.lastSnapshotDate ?? "Sin último snapshot"}</span></div>
    {entity.sufficiency.reasons.length > 0 && <p className="analysis-reasons"><strong>Razones del backend:</strong>{entity.sufficiency.reasons.map((reason) => <span key={reason}>{reason}</span>)}</p>}
    <div className="analysis-metric-grid">{analysisMetrics.filter(([key]) => entity.metrics[key]).map(([key, label, kind]) => { const metric = entity.metrics[key]; return <article key={key}><p>{label}</p><strong>{metricValue(metric, kind)}</strong><small>{metric?.availability} · {metric?.source}</small></article>; })}</div>
    {entity.comparison && <AnalysisComparison values={entity.comparison} />}
    {entity.benchmarks && <AnalysisBenchmarks values={entity.benchmarks} />}
  </article>;
}

function AnalysisComparison({ values }: { values: NonNullable<AnalysisEntity["comparison"]> }) {
  const entries = analysisMetrics.filter(([key]) => values[key]);
  if (!entries.length) return null;
  return <div className="analysis-table-wrap"><table className="analysis-table"><caption>Comparación entregada por el backend</caption><thead><tr><th>Métrica</th><th>Actual</th><th>Base</th><th>Cambio</th><th>Estado</th></tr></thead><tbody>{entries.map(([key, label, kind]) => { const value = values[key]; return <tr key={key}><td>{label}</td><td>{compactMetricValue(value?.current ?? null, kind)}</td><td>{compactMetricValue(value?.baseline ?? null, kind)}</td><td>{value?.availability === "Available" ? <>{compactMetricValue(value.absoluteChange, kind)} {value.percentageChange === null ? "" : `(${formatMetric(value.percentageChange, { maximumFractionDigits: 2 })}%)`}</> : "—"}</td><td>{value?.availability}</td></tr>; })}</tbody></table></div>;
}

function AnalysisBenchmarks({ values }: { values: NonNullable<AnalysisEntity["benchmarks"]> }) {
  const entries = analysisMetrics.filter(([key]) => values[key]);
  if (!entries.length) return null;
  return <div className="analysis-table-wrap"><table className="analysis-table"><caption>Benchmark interno entregado por el backend</caption><thead><tr><th>Métrica</th><th>Campaña</th><th>Benchmark</th><th>Comparables</th><th>Estado</th></tr></thead><tbody>{entries.map(([key, label, kind]) => { const value = values[key]; return <tr key={key}><td>{label}</td><td>{value?.availability === "Available" ? compactMetricValue(value.entityValue, kind) : "—"}</td><td>{value?.availability === "Available" ? compactMetricValue(value.benchmarkValue, kind) : "—"}</td><td>{value?.comparableEntities ?? "—"}</td><td>{value?.availability}</td></tr>; })}</tbody></table></div>;
}
