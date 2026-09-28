"use client";

import { dashboardMetricLabel, formatDashboardMetric, type DashboardDataCatalog, type DashboardQueryResult } from "@/lib/dashboard-data";
import type { DashboardComponent } from "@/lib/dashboards";
import { buildPieStops } from "@/lib/dashboard-chart-model";

export type ComponentQueryState = { status: "loading" | "ready" | "empty" | "error"; result?: DashboardQueryResult; error?: string };
const fallbackColors = ["#8054d8", "#3c9ca0", "#e09755", "#d45f87", "#4f78d8", "#68a45b"];

function selectedMetrics(component: DashboardComponent) {
  const configured = component.data.configuration?.metrics;
  if (Array.isArray(configured)) return configured.filter((item): item is string => typeof item === "string");
  const single = component.data.configuration?.metric;
  return typeof single === "string" ? [single] : [];
}

function chartColors(component: DashboardComponent) {
  const configured = Array.isArray(component.style?.seriesColors) ? component.style.seriesColors.filter((item): item is string => typeof item === "string") : [];
  return configured.length ? configured : fallbackColors;
}

function EmptyState({ state }: { state?: ComponentQueryState }) {
  if (!state || state.status === "empty") return <div className="visual-empty"><b>Sin datos</b><span>Configura una fuente, fechas y métricas compatibles.</span></div>;
  if (state.status === "loading") return <div className="visual-empty visual-loading"><b>Cargando datos…</b><span>Consultando el período configurado.</span></div>;
  if (state.status === "error") return <div className="visual-empty visual-error"><b>No se pudo consultar</b><span>{state.error}</span></div>;
  return null;
}

function Legend({ metrics, colors, catalog }: { metrics: string[]; colors: string[]; catalog: DashboardDataCatalog | null }) {
  return <div className="chart-legend">{metrics.map((metric, index) => <span key={metric}><i style={{ background: colors[index % colors.length] }} />{dashboardMetricLabel(catalog, metric)}</span>)}</div>;
}

export function DataVisualization({ component, state, catalog }: { component: DashboardComponent; state?: ComponentQueryState; catalog: DashboardDataCatalog | null }) {
  if (state?.status !== "ready" || !state.result?.rows.length) return <EmptyState state={state} />;
  const result = state.result;
  const metrics = selectedMetrics(component);
  const configuredDecimals = Number(component.data.configuration?.decimals);
  const decimals = Number.isFinite(configuredDecimals) ? configuredDecimals : undefined;
  const colors = chartColors(component);
  const legend = component.style?.showLegend !== false;
  if (component.type === "table") return <DataTable result={result} metrics={metrics} catalog={catalog} decimals={decimals} />;
  if (component.type === "pivotTable") return <PivotTable result={result} metrics={metrics} catalog={catalog} decimals={decimals} />;
  if (component.type === "timeSeries") return <TimeSeries result={result} metrics={metrics} colors={colors} catalog={catalog} showLegend={legend} />;
  if (component.type === "horizontalBar" || component.type === "column") return <Bars result={result} metric={metrics[0]} color={colors[0]} vertical={component.type === "column"} decimals={decimals} />;
  if (component.type === "stackedBar" || component.type === "stacked100Bar") return <StackedBars result={result} metrics={metrics} colors={colors} catalog={catalog} normalized={component.type === "stacked100Bar"} showLegend={legend} />;
  if (component.type === "pie" || component.type === "donut") return <Pie result={result} metric={metrics[0]} colors={colors} donut={component.type === "donut"} decimals={decimals} />;
  if (component.type === "area") return <Area result={result} metric={metrics[0]} color={colors[0]} />;
  if (component.type === "combo") return <Combo result={result} metrics={metrics} colors={colors} catalog={catalog} showLegend={legend} />;
  if (component.type === "funnel") return <Funnel result={result} metric={metrics[0]} color={colors[0]} decimals={decimals} />;
  if (component.type === "scatter" || component.type === "bubble") return <Scatter result={result} metrics={metrics} colors={colors} bubble={component.type === "bubble"} />;
  if (component.type === "goalGauge") return <Gauge result={result} metric={metrics[0]} target={Number(component.data.configuration?.target ?? 100)} color={colors[0]} decimals={decimals} />;
  if (component.type === "map") return <MapChart result={result} metric={metrics[0]} colors={colors} decimals={decimals} />;
  if (component.type === "bullet") return <Bullet result={result} metric={metrics[0]} target={Number(component.data.configuration?.target ?? 100)} color={colors[0]} decimals={decimals} />;
  if (component.type === "treemap") return <Treemap result={result} metric={metrics[0]} colors={colors} decimals={decimals} />;
  if (component.type === "sankey") return <Sankey result={result} metrics={metrics} color={colors[0]} />;
  if (component.type === "waterfall") return <Waterfall result={result} metric={metrics[0]} colors={colors} />;
  if (component.type === "boxPlot") return <BoxPlot result={result} metrics={metrics} colors={colors} />;
  if (component.type === "candlestick") return <Candlestick result={result} metrics={metrics} colors={colors} />;
  if (component.type === "timeline") return <Timeline result={result} metric={metrics[0]} color={colors[0]} decimals={decimals} />;
  return <EmptyState state={{ status: "empty" }} />;
}

function DataTable({ result, metrics, catalog, decimals }: { result: DashboardQueryResult; metrics: string[]; catalog: DashboardDataCatalog | null; decimals?: number }) {
  return <div className="data-table-wrap"><table><thead><tr><th>{result.dimension === "date" ? "Fecha" : "Campaña"}</th>{metrics.map((metric) => <th key={metric}>{dashboardMetricLabel(catalog, metric)}</th>)}</tr></thead><tbody>{result.rows.map((row) => <tr key={row.key}><td>{row.label}</td>{metrics.map((metric) => <td key={metric}>{formatDashboardMetric(row.metrics[metric], result.currency, decimals)}</td>)}</tr>)}</tbody></table></div>;
}

function PivotTable({ result, metrics, catalog, decimals }: { result: DashboardQueryResult; metrics: string[]; catalog: DashboardDataCatalog | null; decimals?: number }) {
  return <div className="data-table-wrap pivot-table-wrap"><table><thead><tr><th>Métrica</th>{result.rows.map((row) => <th key={row.key}>{row.label}</th>)}</tr></thead><tbody>{metrics.map((metric) => <tr key={metric}><td>{dashboardMetricLabel(catalog, metric)}</td>{result.rows.map((row) => <td key={row.key}>{formatDashboardMetric(row.metrics[metric], result.currency, decimals)}</td>)}</tr>)}</tbody></table></div>;
}

function linePoints(result: DashboardQueryResult, metric: string, width = 600, height = 220, pad = 26) {
  const values = result.rows.map((row) => row.metrics[metric]?.value ?? 0);
  const max = Math.max(1, ...values);
  return values.map((raw, index) => `${pad + index * (width - pad * 2) / Math.max(1, values.length - 1)},${height - pad - raw / max * (height - pad * 2)}`).join(" ");
}

function TimeSeries({ result, metrics, colors, catalog, showLegend }: { result: DashboardQueryResult; metrics: string[]; colors: string[]; catalog: DashboardDataCatalog | null; showLegend: boolean }) {
  return <div className="time-series"><svg viewBox="0 0 600 220" preserveAspectRatio="none" role="img" aria-label="Serie temporal"><line x1="26" y1="194" x2="574" y2="194" className="chart-axis" />{metrics.map((metric, index) => <polyline key={metric} points={linePoints(result, metric)} fill="none" stroke={colors[index % colors.length]} strokeWidth="3" vectorEffect="non-scaling-stroke" />)}</svg>{showLegend && <Legend metrics={metrics} colors={colors} catalog={catalog} />}</div>;
}

function Bars({ result, metric, color, vertical, decimals }: { result: DashboardQueryResult; metric: string; color: string; vertical: boolean; decimals?: number }) {
  const max = Math.max(1, ...result.rows.map((row) => row.metrics[metric]?.value ?? 0));
  if (vertical) return <div className="column-chart">{result.rows.map((row) => <div key={row.key} className="column-item"><div><i title={formatDashboardMetric(row.metrics[metric], result.currency, decimals)} style={{ height: `${Math.max(2, (row.metrics[metric]?.value ?? 0) / max * 100)}%`, background: color }} /></div><span title={row.label}>{row.label}</span></div>)}</div>;
  return <div className="bar-chart">{result.rows.map((row) => <div key={row.key} className="bar-item"><span title={row.label}>{row.label}</span><div><i style={{ width: `${Math.max(1, (row.metrics[metric]?.value ?? 0) / max * 100)}%`, background: color }} /></div><b>{formatDashboardMetric(row.metrics[metric], result.currency, decimals)}</b></div>)}</div>;
}

function StackedBars({ result, metrics, colors, catalog, normalized, showLegend }: { result: DashboardQueryResult; metrics: string[]; colors: string[]; catalog: DashboardDataCatalog | null; normalized: boolean; showLegend: boolean }) {
  const totals = result.rows.map((row) => metrics.reduce((sum, metric) => sum + Math.max(0, row.metrics[metric]?.value ?? 0), 0));
  const globalMax = Math.max(1, ...totals);
  return <div className="stacked-chart"><div>{result.rows.map((row, rowIndex) => <div className="stacked-row" key={row.key}><span title={row.label}>{row.label}</span><div>{metrics.map((metric, index) => { const raw = Math.max(0, row.metrics[metric]?.value ?? 0); const denominator = normalized ? Math.max(1, totals[rowIndex]) : globalMax; return <i key={metric} title={`${dashboardMetricLabel(catalog, metric)}: ${raw}`} style={{ width: `${raw / denominator * 100}%`, background: colors[index % colors.length] }} />; })}</div></div>)}</div>{showLegend && <Legend metrics={metrics} colors={colors} catalog={catalog} />}</div>;
}

function Pie({ result, metric, colors, donut, decimals }: { result: DashboardQueryResult; metric: string; colors: string[]; donut: boolean; decimals?: number }) {
  const values = result.rows.map((row) => Math.max(0, row.metrics[metric]?.value ?? 0));
  const stops = buildPieStops(values, colors);
  return <div className="pie-layout"><div className={`pie-graphic ${donut ? "is-donut" : ""}`} style={{ background: `conic-gradient(${stops})` }}>{donut && <i />}</div><div className="pie-legend">{result.rows.map((row, index) => <span key={row.key}><i style={{ background: colors[index % colors.length] }} /><b title={row.label}>{row.label}</b><small>{formatDashboardMetric(row.metrics[metric], result.currency, decimals)}</small></span>)}</div></div>;
}

function Area({ result, metric, color }: { result: DashboardQueryResult; metric: string; color: string }) {
  const points = linePoints(result, metric);
  return <div className="area-chart"><svg viewBox="0 0 600 220" preserveAspectRatio="none" role="img" aria-label="Gráfico de área"><defs><linearGradient id={`area-${metric}`} x1="0" y1="0" x2="0" y2="1"><stop offset="0" stopColor={color} stopOpacity=".58" /><stop offset="1" stopColor={color} stopOpacity=".08" /></linearGradient></defs><polygon points={`26,194 ${points} 574,194`} fill={`url(#area-${metric})`} /><polyline points={points} fill="none" stroke={color} strokeWidth="3" vectorEffect="non-scaling-stroke" /><line x1="26" y1="194" x2="574" y2="194" className="chart-axis" /></svg></div>;
}

function Combo({ result, metrics, colors, catalog, showLegend }: { result: DashboardQueryResult; metrics: string[]; colors: string[]; catalog: DashboardDataCatalog | null; showLegend: boolean }) {
  const barMetric = metrics[0], lineMetric = metrics[1] ?? metrics[0];
  const max = Math.max(1, ...result.rows.map((row) => row.metrics[barMetric]?.value ?? 0));
  return <div className="time-series combo-chart"><svg viewBox="0 0 600 220" preserveAspectRatio="none" role="img" aria-label="Gráfico combinado">{result.rows.map((row, index) => { const slot = 548 / Math.max(1, result.rows.length); const barHeight = (row.metrics[barMetric]?.value ?? 0) / max * 168; return <rect key={row.key} x={26 + index * slot + slot * .18} y={194 - barHeight} width={slot * .64} height={barHeight} fill={colors[0]} opacity=".72" />; })}<polyline points={linePoints(result, lineMetric)} fill="none" stroke={colors[1 % colors.length]} strokeWidth="3" vectorEffect="non-scaling-stroke" /><line x1="26" y1="194" x2="574" y2="194" className="chart-axis" /></svg>{showLegend && <Legend metrics={[barMetric, lineMetric]} colors={colors} catalog={catalog} />}</div>;
}

function Funnel({ result, metric, color, decimals }: { result: DashboardQueryResult; metric: string; color: string; decimals?: number }) {
  const max = Math.max(1, ...result.rows.map((row) => row.metrics[metric]?.value ?? 0));
  return <div className="funnel-chart">{result.rows.map((row, index) => <div key={row.key} style={{ width: `${Math.max(18, (row.metrics[metric]?.value ?? 0) / max * 100)}%`, background: color, opacity: 1 - index * .055 }}><span>{row.label}</span><b>{formatDashboardMetric(row.metrics[metric], result.currency, decimals)}</b></div>)}</div>;
}

function Scatter({ result, metrics, colors, bubble }: { result: DashboardQueryResult; metrics: string[]; colors: string[]; bubble: boolean }) {
  const xMetric = metrics[0], yMetric = metrics[1] ?? metrics[0], sizeMetric = metrics[2] ?? yMetric;
  const maxX = Math.max(1, ...result.rows.map((row) => row.metrics[xMetric]?.value ?? 0));
  const maxY = Math.max(1, ...result.rows.map((row) => row.metrics[yMetric]?.value ?? 0));
  const maxSize = Math.max(1, ...result.rows.map((row) => row.metrics[sizeMetric]?.value ?? 0));
  return <div className="scatter-chart"><svg viewBox="0 0 600 220" preserveAspectRatio="none" role="img" aria-label={bubble ? "Gráfico de burbujas" : "Gráfico de dispersión"}><line x1="26" y1="194" x2="574" y2="194" className="chart-axis" /><line x1="26" y1="18" x2="26" y2="194" className="chart-axis" />{result.rows.map((row, index) => <circle key={row.key} cx={26 + (row.metrics[xMetric]?.value ?? 0) / maxX * 548} cy={194 - (row.metrics[yMetric]?.value ?? 0) / maxY * 176} r={bubble ? 5 + (row.metrics[sizeMetric]?.value ?? 0) / maxSize * 14 : 6} fill={colors[index % colors.length]} opacity=".76"><title>{row.label}</title></circle>)}</svg></div>;
}

function Gauge({ result, metric, target, color, decimals }: { result: DashboardQueryResult; metric: string; target: number; color: string; decimals?: number }) {
  const measured = result.rows[0]?.metrics[metric];
  const ratio = target > 0 ? Math.max(0, Math.min(1, (measured?.value ?? 0) / target)) : 0;
  return <div className="goal-gauge"><div style={{ background: `conic-gradient(from 270deg, ${color} 0 ${ratio * 50}%, #e8ebed ${ratio * 50}% 50%, transparent 50% 100%)` }}><i /><strong>{Math.round(ratio * 100)}%</strong></div><span>{formatDashboardMetric(measured, result.currency, decimals)} de {new Intl.NumberFormat("es-BO").format(target)}</span></div>;
}

function MapChart({ result, metric, colors, decimals }: { result: DashboardQueryResult; metric: string; colors: string[]; decimals?: number }) {
  const max=Math.max(1,...result.rows.map(row=>Math.max(0,row.metrics[metric]?.value??0)));
  return <div className="map-chart" role="img" aria-label="Mapa de intensidad por ubicación"><svg viewBox="0 0 600 260"><path d="M42 83l87-48 96 30 74-37 99 37 75-13 85 68-44 78-118 22-89-28-106 37-119-42z" className="map-outline"/>{result.rows.slice(0,12).map((row,index)=>{const value=Math.max(0,row.metrics[metric]?.value??0);const x=80+(index*113)%455,y=72+(index*67)%130;return <circle key={row.key} cx={x} cy={y} r={8+value/max*24} fill={colors[index%colors.length]} opacity={.35+value/max*.55}><title>{row.label}: {formatDashboardMetric(row.metrics[metric],result.currency,decimals)}</title></circle>})}</svg><p>Representación relativa; requiere una dimensión geográfica para interpretación territorial.</p></div>;
}
function Bullet({result,metric,target,color,decimals}:{result:DashboardQueryResult;metric:string;target:number;color:string;decimals?:number}){const value=result.rows[0]?.metrics[metric];const ratio=target>0?Math.max(0,(value?.value??0)/target):0;return <div className="bullet-chart" role="meter" aria-valuemin={0} aria-valuemax={target} aria-valuenow={value?.value??0}><div><i style={{width:`${Math.min(100,ratio*100)}%`,background:color}}/><b style={{left:`${Math.min(100,Math.max(0,100))}%`}}/></div><span>{formatDashboardMetric(value,result.currency,decimals)} / {target}</span></div>}
function Treemap({result,metric,colors,decimals}:{result:DashboardQueryResult;metric:string;colors:string[];decimals?:number}){const total=result.rows.reduce((s,r)=>s+Math.max(0,r.metrics[metric]?.value??0),0)||1;return <div className="treemap-chart" role="img" aria-label="Mapa de árbol">{result.rows.slice(0,16).map((row,index)=><div key={row.key} style={{flexGrow:Math.max(.08,(row.metrics[metric]?.value??0)/total),background:colors[index%colors.length]}}><b>{row.label}</b><span>{formatDashboardMetric(row.metrics[metric],result.currency,decimals)}</span></div>)}</div>}
function Sankey({result,metrics,color}:{result:DashboardQueryResult;metrics:string[];color:string}){const first=metrics[0],second=metrics[1]??first;const max=Math.max(1,...result.rows.map(r=>r.metrics[first]?.value??0));return <div className="sankey-chart" role="img" aria-label="Diagrama Sankey">{result.rows.slice(0,10).map(row=><div key={row.key}><span>{row.label}</span><i style={{height:`${4+(row.metrics[first]?.value??0)/max*18}px`,background:color}}/><b>{row.metrics[second]?.value??0}</b></div>)}</div>}
function Waterfall({result,metric,colors}:{result:DashboardQueryResult;metric:string;colors:string[]}){const values=result.rows.map((row,index)=>{const next=row.metrics[metric]?.value??0;const previous=index?result.rows[index-1].metrics[metric]?.value??0:0;return{row,delta:next-previous}});const max=Math.max(1,...values.map(x=>Math.abs(x.delta)));return <div className="waterfall-chart" role="img" aria-label="Gráfico de cascada">{values.map(({row,delta})=><div key={row.key}><i style={{height:`${Math.abs(delta)/max*85}%`,background:delta>=0?colors[0]:colors[1%colors.length]}} title={`${row.label}: ${delta}`}/><span>{row.label}</span></div>)}</div>}
function BoxPlot({result,metrics,colors}:{result:DashboardQueryResult;metrics:string[];colors:string[]}){return <div className="boxplot-chart" role="img" aria-label="Caja y bigotes">{metrics.map((metric,index)=>{const values=result.rows.map(r=>r.metrics[metric]?.value).filter((x):x is number=>typeof x==="number").sort((a,b)=>a-b);const min=values[0]??0,max=values.at(-1)??0,q1=values[Math.floor(values.length*.25)]??0,median=values[Math.floor(values.length*.5)]??0,q3=values[Math.floor(values.length*.75)]??0,range=max-min||1;return <div key={metric}><span>{metric}</span><section><i style={{left:"0",width:"100%"}}/><b style={{left:`${(q1-min)/range*100}%`,width:`${(q3-q1)/range*100}%`,borderColor:colors[index%colors.length]}}/><em style={{left:`${(median-min)/range*100}%`}}/></section></div>})}</div>}
function Candlestick({result,metrics,colors}:{result:DashboardQueryResult;metrics:string[];colors:string[]}){const keys=[metrics[0],metrics[1]??metrics[0],metrics[2]??metrics[0]];const max=Math.max(1,...result.rows.flatMap(r=>keys.map(k=>r.metrics[k]?.value??0)));return <div className="candlestick-chart" role="img" aria-label="Gráfico de velas">{result.rows.map((row,index)=>{const points=keys.map(k=>row.metrics[k]?.value??0).sort((a,b)=>a-b);return <div key={row.key}><i style={{bottom:`${points[0]/max*100}%`,height:`${(points[2]-points[0])/max*100}%`}}/><b style={{bottom:`${points[0]/max*100}%`,height:`${Math.max(3,(points[1]-points[0])/max*100)}%`,background:colors[index%colors.length]}}/><span>{row.label}</span></div>})}</div>}
function Timeline({result,metric,color,decimals}:{result:DashboardQueryResult;metric:string;color:string;decimals?:number}){return <ol className="timeline-chart" aria-label="Línea de tiempo">{result.rows.map(row=><li key={row.key}><i style={{background:color}}/><time>{row.label}</time><strong>{formatDashboardMetric(row.metrics[metric],result.currency,decimals)}</strong></li>)}</ol>}
