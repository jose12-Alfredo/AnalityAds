"use client";

import { useMemo, useState } from "react";
import { analysisMetrics, type AnalysisMetricKey, type AnalysisResult } from "@/lib/analysis";

const chartMetrics = analysisMetrics.filter(([key]) => key !== "frequency");

export function SnapshotChart({ analysis }: { analysis: AnalysisResult }) {
  const initialMetric = analysis.selectedMetrics.find((key) => key !== "frequency") ?? "spend";
  const [metricKey, setMetricKey] = useState<AnalysisMetricKey>(initialMetric);
  const [hiddenIds, setHiddenIds] = useState<Set<string>>(() => new Set());
  const metricDefinition = analysisMetrics.find(([key]) => key === metricKey) ?? analysisMetrics[0];
  const rows = useMemo(() => analysis.campaigns
    .map((campaign) => ({ campaign, metric: campaign.metrics[metricKey] }))
    .filter((row) => row.metric?.availability === "CompleteForSnapshots" && row.metric.value !== null), [analysis.campaigns, metricKey]);
  const visibleRows = rows.filter(({ campaign }) => !hiddenIds.has(campaign.id));
  const maxValue = Math.max(...visibleRows.map(({ metric }) => Math.abs(metric?.value ?? 0)), 0);

  function toggle(id: string) {
    setHiddenIds((current) => {
      const next = new Set(current);
      if (next.has(id)) next.delete(id); else next.add(id);
      return next;
    });
  }

  return <section className="snapshot-chart" aria-labelledby="snapshot-chart-title">
    <div className="snapshot-chart-heading">
      <div><p className="eyebrow">Explorar el snapshot</p><h3 id="snapshot-chart-title">Comparación visual por campaña</h3><p>El gráfico usa únicamente las cifras guardadas en este reporte.</p></div>
      <label>Métrica<select value={metricKey} onChange={(event) => { setMetricKey(event.target.value as AnalysisMetricKey); setHiddenIds(new Set()); }}>{chartMetrics.map(([key, label]) => <option key={key} value={key}>{label}</option>)}</select></label>
    </div>
    {rows.length ? <>
      <div className="snapshot-bars" role="img" aria-label={`${metricDefinition[1]} por campaña`}>
        {rows.map(({ campaign, metric }) => {
          const hidden = hiddenIds.has(campaign.id);
          const width = maxValue > 0 ? Math.max(2, Math.abs(metric?.value ?? 0) / maxValue * 100) : 2;
          return <button type="button" className={hidden ? "is-hidden" : ""} key={campaign.id} onClick={() => toggle(campaign.id)} aria-pressed={!hidden} title={`${campaign.name}: ${metric?.value ?? "Sin dato"} · ${metric?.availability}`}>
            <span className="snapshot-bar-label">{campaign.name}</span><span className="snapshot-bar-track"><span style={{ width: `${hidden ? 0 : width}%` }} /></span><strong>{hidden ? "Oculta" : new Intl.NumberFormat("es-BO", { maximumFractionDigits: 2 }).format(metric?.value ?? 0)}</strong>
          </button>;
        })}
      </div>
      <p className="snapshot-chart-note">Selecciona una campaña para ocultarla o volver a mostrarla. La interacción no modifica el reporte.</p>
    </> : <p className="analysis-findings-empty">El snapshot no contiene valores disponibles de {metricDefinition[1].toLowerCase()} por campaña.</p>}
  </section>;
}
