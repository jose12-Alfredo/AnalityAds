"use client";

import Link from "next/link";
import { PointerEvent as ReactPointerEvent, useCallback, useEffect, useMemo, useRef, useState } from "react";
import { ApiError } from "@/lib/api";
import { readSession } from "@/lib/auth-session";
import { canEditDashboards, dashboardApi, describeWorkspaceError, type Dashboard, type DashboardComponent, type DashboardComponentType, type DashboardDefinition, type DashboardPage } from "@/lib/dashboards";
import { dashboardDataApi, dashboardMetricLabel, formatDashboardMetric, type DashboardDataCatalog, type DashboardDataSource, type DashboardQueryMetric, type DashboardQueryResult } from "@/lib/dashboard-data";
import { DataVisualization, type ComponentQueryState } from "@/components/dashboard-visualizations";
import { chartDimensions, chartMetricLimit, chartRequiresSameUnit, DATA_COMPONENT_TYPES } from "@/lib/dashboard-chart-model";
import { EDITOR_GRID_SIZE, EDITOR_HISTORY_LIMIT, alignComponents, cloneDefinition, createPage, deleteComponents, distributeComponents, duplicateComponents, expandGroupSelection, groupComponents, normalizePageOrders, selectedComponents, updateComponents } from "@/lib/dashboard-editor-model";

type SaveState = "saved" | "pending" | "saving" | "error" | "conflict";
type Point = { x: number; y: number };
type Interaction = { kind: "move" | "resize" | "marquee"; pointerId: number; start: Point; before: DashboardDefinition; ids: Set<string>; originals: Map<string, DashboardComponent> };
const labels: Record<string, string> = { kpiCard: "Indicador", text: "Texto", shape: "Forma", image: "Imagen", divider: "Separador", table: "Tabla", pivotTable: "Tabla dinámica", timeSeries: "Serie temporal", horizontalBar: "Barras", column: "Columnas", stackedBar: "Barras apiladas", stacked100Bar: "Barras 100 %", pie: "Circular", donut: "Dona", area: "Área", combo: "Combinado", funnel: "Embudo", scatter: "Dispersión", bubble: "Burbujas", goalGauge: "Medidor", map: "Mapa", bullet: "Bala", treemap: "Árbol", sankey: "Sankey", waterfall: "Cascada", boxPlot: "Caja y bigotes", candlestick: "Velas", timeline: "Línea de tiempo" };
const value = (item: DashboardComponent, bucket: "style" | "data", key: string, fallback: string) => {
  const found = bucket === "style" ? item.style?.[key] : item.data.configuration?.[key];
  return typeof found === "string" ? found : fallback;
};
const snap = (number: number, grid: boolean) => grid ? Math.round(number / EDITOR_GRID_SIZE) * EDITOR_GRID_SIZE : Math.round(number);

function makeComponent(type: DashboardComponentType, page: DashboardPage): DashboardComponent {
  const common = { id: crypto.randomUUID(), type, x: 48 + page.components.length % 6 * 24, y: 48 + page.components.length % 6 * 24, zIndex: Math.max(0, ...page.components.map((item) => item.zIndex)) + 1, isLocked: false, data: { sources: [] } };
  if (type === "text") return { ...common, width: 420, height: 96, data: { ...common.data, configuration: { text: "Texto explicativo" } }, style: { color: "#17202a", fontSize: 30, background: "transparent" } };
  if (type === "kpiCard") return { ...common, width: 300, height: 180, data: { ...common.data, configuration: { title: "Indicador", metricLabel: "Selecciona una m?trica" } }, style: { color: "#17202a", background: "#ffffff" } };
  if (type === "image") return { ...common, width: 420, height: 260, data: { ...common.data, configuration: { title: "Imagen", imageUrl: "", alt: "Imagen del informe" } }, style: { background: "#f4f5f7", objectFit: "contain" } };
  if (type === "divider") return { ...common, width: 620, height: 24, data: { ...common.data, configuration: {} }, style: { background: "transparent", color: "#8054d8", thickness: 2 } };
  if (type === "table") return { ...common, width: 720, height: 360, data: { ...common.data, configuration: { title: "Tabla", dimension: "campaign", metrics: ["spend", "impressions"], limit: 10, sortMetric: "spend", sortDirection: "desc" } }, style: { background: "#ffffff", seriesColors: ["#8054d8", "#3c9ca0", "#e09755"], showLegend: true } };
  if (type === "pivotTable") return { ...common, width: 760, height: 380, data: { ...common.data, configuration: { title: "Tabla dinámica", dimension: "campaign", metrics: ["spend", "impressions", "linkClicks"], limit: 8, sortMetric: "spend", sortDirection: "desc" } }, style: { background: "#ffffff", seriesColors: ["#8054d8", "#3c9ca0", "#e09755"], showLegend: false } };
  if (type === "timeSeries") return { ...common, width: 760, height: 360, data: { ...common.data, configuration: { title: "Evoluci?n", dimension: "date", metrics: ["spend"], limit: 90, sortDirection: "asc" } }, style: { background: "#ffffff", seriesColors: ["#8054d8", "#3c9ca0", "#e09755"], showLegend: true } };
  if (["horizontalBar", "column", "pie", "donut", "funnel"].includes(type)) return { ...common, width: 680, height: 380, data: { ...common.data, configuration: { title: labels[type], dimension: "campaign", metrics: ["spend"], limit: 10, sortMetric: "spend", sortDirection: "desc" } }, style: { background: "#ffffff", seriesColors: ["#8054d8", "#3c9ca0", "#e09755", "#d45f87", "#4f78d8"], showLegend: type === "pie" || type === "donut" } };
  if (["stackedBar", "stacked100Bar"].includes(type)) return { ...common, width: 720, height: 400, data: { ...common.data, configuration: { title: labels[type], dimension: "campaign", metrics: ["impressions", "linkClicks"], limit: 10, sortMetric: "impressions", sortDirection: "desc" } }, style: { background: "#ffffff", seriesColors: ["#8054d8", "#3c9ca0", "#e09755"], showLegend: true } };
  if (type === "area") return { ...common, width: 760, height: 360, data: { ...common.data, configuration: { title: "Área", dimension: "date", metrics: ["spend"], limit: 90, sortDirection: "asc" } }, style: { background: "#ffffff", seriesColors: ["#8054d8"], showLegend: false } };
  if (type === "combo") return { ...common, width: 760, height: 380, data: { ...common.data, configuration: { title: "Combinado", dimension: "date", metrics: ["spend", "impressions"], limit: 90, sortDirection: "asc" } }, style: { background: "#ffffff", seriesColors: ["#8054d8", "#3c9ca0"], showLegend: true } };
  if (type === "scatter" || type === "bubble") return { ...common, width: 680, height: 380, data: { ...common.data, configuration: { title: labels[type], dimension: "campaign", metrics: type === "bubble" ? ["spend", "impressions", "linkClicks"] : ["spend", "impressions"], limit: 30, sortMetric: "spend", sortDirection: "desc" } }, style: { background: "#ffffff", seriesColors: ["#8054d8", "#3c9ca0", "#e09755"] } };
  if (type === "goalGauge") return { ...common, width: 380, height: 300, data: { ...common.data, configuration: { title: "Cumplimiento", dimension: "none", metrics: ["spend"], target: 1000 } }, style: { background: "#ffffff", seriesColors: ["#8054d8"], showLegend: false } };
  if (type === "bullet") return { ...common, width: 520, height: 190, data: { ...common.data, configuration: { title: labels[type], dimension: "none", metrics: ["spend"], target: 1000 } }, style: { background: "#ffffff", seriesColors: ["#8054d8"] } };
  if (["waterfall", "candlestick", "timeline"].includes(type)) return { ...common, width: 760, height: 380, data: { ...common.data, configuration: { title: labels[type], dimension: "date", metrics: type === "candlestick" ? ["spend", "impressions", "linkClicks"] : ["spend"], limit: 90, sortDirection: "asc" } }, style: { background: "#ffffff", seriesColors: ["#8054d8", "#3c9ca0", "#e09755"] } };
  if (["map", "treemap", "sankey", "boxPlot"].includes(type)) return { ...common, width: 720, height: 400, data: { ...common.data, configuration: { title: labels[type], dimension: "campaign", metrics: type === "sankey" || type === "boxPlot" ? ["spend", "impressions"] : ["spend"], limit: 20, sortMetric: "spend", sortDirection: "desc" } }, style: { background: "#ffffff", seriesColors: ["#8054d8", "#3c9ca0", "#e09755", "#d45f87"] } };
  return { ...common, width: 280, height: 160, data: { ...common.data, configuration: { shape: "rectangle" } }, style: { background: "#d9c9fb", borderColor: "#7b4bd4" } };
}

function ItemContent({ item, metric, currency }: { item: DashboardComponent; metric?: DashboardQueryMetric; currency?: string | null }) {
  if (item.type === "text") return <p style={{ fontSize: `${Number(item.style?.fontSize) || 30}px` }}>{value(item, "data", "text", "Texto")}</p>;
  if (item.type === "kpiCard") return <div className="kpi-preview"><span>{value(item, "data", "title", "Indicador")}</span><strong>{formatDashboardMetric(metric, currency ?? null, Number(item.data.configuration?.decimals ?? 2))}</strong><small>{metric ? value(item, "data", "metricLabel", "Métrica conectada") : value(item, "data", "metricLabel", "Selecciona una métrica")}</small></div>;
  if (item.type === "image") { const url = value(item, "data", "imageUrl", ""); return /^https:\/\//i.test(url) ? <div className="report-image" role="img" aria-label={value(item, "data", "alt", "Imagen del informe")} style={{ backgroundImage: `url(${JSON.stringify(url)})`, backgroundSize: value(item, "style", "objectFit", "contain") }} /> : <div className="visual-empty"><b>Imagen sin configurar</b><span>Agrega una dirección HTTPS en propiedades.</span></div>; }
  if (item.type === "divider") return <div className="report-divider" style={{ borderColor: value(item, "style", "color", "#8054d8"), borderTopWidth: `${Number(item.style?.thickness) || 2}px` }} />;
  return <span className="shape-label">Forma</span>;
}

function DataProperties({ item, sources, catalog, state, onSource, onConfig, onMetric, onStyle }: {
  item: DashboardComponent; sources: DashboardDataSource[]; catalog: DashboardDataCatalog | null; state?: ComponentQueryState;
  onSource: (id: string) => void; onConfig: (key: string, value: string | number | boolean) => void;
  onMetric: (metric: string) => void; onStyle: (key: string, value: unknown) => void;
}) {
  const isTotal = item.type === "kpiCard" || item.type === "goalGauge";
  const dimension = isTotal ? "none" : String(item.data.configuration?.dimension ?? (["timeSeries", "area", "combo"].includes(item.type) ? "date" : "campaign"));
  const selectedMetrics = Array.isArray(item.data.configuration?.metrics) ? item.data.configuration.metrics.filter((entry): entry is string => typeof entry === "string") : [String(item.data.configuration?.metric ?? "spend")];
  const maxMetrics = chartMetricLimit(item.type);
  const firstUnit = catalog?.metrics.find((metric) => metric.key === selectedMetrics[0])?.unit;
  const dimensions = chartDimensions(item.type);
  const metrics = catalog?.metrics.filter((metric) => metric.supportedDimensions.includes(dimension)) ?? [];
  const result = state?.result;
  return <div className="kpi-data-properties"><span>Datos</span><label>Fuente<select value={item.data.sources[0]?.dataSourceId ?? ""} onChange={(event) => onSource(event.target.value)}><option value="">Seleccionar cuenta</option>{sources.filter((source) => source.isActive).map((source) => <option key={source.id} value={source.id}>{source.provider} · {source.name}</option>)}</select></label>
    {!isTotal && <label>Dimensión<select value={dimension} onChange={(event) => onConfig("dimension", event.target.value)}>{dimensions.map((key) => <option key={key} value={key}>{catalog?.dimensions.find((entry) => entry.key === key)?.name ?? key}</option>)}</select></label>}
    {item.type === "kpiCard" ? <label>Métrica<select value={String(item.data.configuration?.metric ?? "spend")} onChange={(event) => onConfig("metric", event.target.value)}>{metrics.map((metric) => <option key={metric.key} value={metric.key}>{metric.name}</option>)}</select></label> : <fieldset><legend>Métricas · máximo {maxMetrics}</legend>{metrics.map((metric) => { const incompatibleScale = chartRequiresSameUnit(item.type) && !!firstUnit && metric.unit !== firstUnit; return <label key={metric.key} title={incompatibleScale ? "Este gráfico requiere métricas con la misma unidad." : metric.description}><input type="checkbox" checked={selectedMetrics.includes(metric.key)} disabled={!selectedMetrics.includes(metric.key) && (selectedMetrics.length >= maxMetrics || incompatibleScale)} onChange={() => onMetric(metric.key)} />{metric.name}</label>; })}</fieldset>}
    <div><label>Desde<input type="date" value={String(item.data.configuration?.since ?? "")} onChange={(event) => onConfig("since", event.target.value)} /></label><label>Hasta<input type="date" value={String(item.data.configuration?.until ?? "")} onChange={(event) => onConfig("until", event.target.value)} /></label></div>
    <label>Decimales<input type="number" min="0" max="6" value={Number(item.data.configuration?.decimals ?? (isTotal ? 2 : 0))} onChange={(event) => onConfig("decimals", Number(event.target.value))} /></label>
    {item.type === "goalGauge" && <label>Objetivo<input type="number" min="0.01" step="0.01" value={Number(item.data.configuration?.target ?? 100)} onChange={(event) => onConfig("target", Number(event.target.value))} /></label>}
    {!isTotal && <><div><label>Límite<input type="number" min="1" max="100" value={Number(item.data.configuration?.limit ?? 10)} onChange={(event) => onConfig("limit", Number(event.target.value))} /></label><label>Orden<select value={String(item.data.configuration?.sortDirection ?? (dimension === "date" ? "asc" : "desc"))} onChange={(event) => onConfig("sortDirection", event.target.value)}><option value="desc">Descendente</option><option value="asc">Ascendente</option></select></label></div><label>Ordenar por<select value={String(item.data.configuration?.sortMetric ?? selectedMetrics[0] ?? "")} onChange={(event) => onConfig("sortMetric", event.target.value)}>{selectedMetrics.map((metric) => <option key={metric} value={metric}>{dashboardMetricLabel(catalog, metric)}</option>)}</select></label></>}
    {!isTotal && <label>Filtro de dimensión<input type="text" placeholder="IDs separados por coma" value={String(item.data.configuration?.dimensionFilter ?? "")} onChange={(event) => onConfig("dimensionFilter", event.target.value)} /></label>}
    <label>Segunda fuente<select value={String(item.data.configuration?.secondSourceId ?? "")} onChange={(event) => onConfig("secondSourceId", event.target.value)}><option value="">Sin combinación</option>{sources.filter((source) => source.isActive && source.id !== item.data.sources[0]?.dataSourceId).map((source) => <option key={source.id} value={source.id}>{source.provider} · {source.name}</option>)}</select></label>
    <label className="inline-check"><input type="checkbox" checked={item.data.configuration?.comparePrevious === true} onChange={(event) => onConfig("comparePrevious", event.target.checked)} /> Comparar con período anterior</label>
    {item.type !== "kpiCard" && <div><label>Color<input type="color" value={Array.isArray(item.style?.seriesColors) && typeof item.style.seriesColors[0] === "string" ? item.style.seriesColors[0] : "#8054d8"} onChange={(event) => onStyle("seriesColors", [event.target.value, "#3c9ca0", "#e09755", "#d45f87", "#4f78d8"])} /></label><label className="inline-check"><input type="checkbox" checked={item.style?.showLegend !== false} onChange={(event) => onStyle("showLegend", event.target.checked)} /> Leyenda</label></div>}
    {state?.status === "error" && <p>{state.error}</p>}{result?.lastSyncedAtUtc && <small>Fuente actualizada {new Intl.DateTimeFormat("es-BO", { dateStyle: "medium", timeStyle: "short" }).format(new Date(result.lastSyncedAtUtc))} · {result.coverage.snapshotDays}/{result.coverage.requestedDays} días observados</small>}
  </div>;
}

function RenderedComponent({ item, state, catalog }: { item: DashboardComponent; state?: ComponentQueryState; catalog: DashboardDataCatalog | null }) {
  if (DATA_COMPONENT_TYPES.includes(item.type) && item.type !== "kpiCard") return <div className="data-component-body"><strong className="data-component-title">{value(item, "data", "title", labels[item.type] || "Gráfico")}</strong><DataVisualization component={item} state={state} catalog={catalog} /></div>;
  const metricKey = String(item.data.configuration?.metric ?? "spend");
  return <ItemContent item={item} metric={state?.result?.rows[0]?.metrics[metricKey]} currency={state?.result?.currency} />;
}

export function DashboardEditor({ dashboardId }: { dashboardId: string }) {
  const session = readSession();
  const editable = !!session && canEditDashboards(session.role);
  const [dashboard, setDashboard] = useState<Dashboard | null>(null);
  const [definition, setDefinition] = useState<DashboardDefinition | null>(null);
  const [pageId, setPageId] = useState("");
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [revision, setRevision] = useState(0);
  const [history, setHistory] = useState<DashboardDefinition[]>([]);
  const [future, setFuture] = useState<DashboardDefinition[]>([]);
  const [grid, setGrid] = useState(true);
  const [zoom, setZoom] = useState(75);
  const [preview, setPreview] = useState(false);
  const [saveState, setSaveState] = useState<SaveState>("saved");
  const [error, setError] = useState("");
  const [catalog, setCatalog] = useState<DashboardDataCatalog | null>(null);
  const [sources, setSources] = useState<DashboardDataSource[]>([]);
  const [componentQueries, setComponentQueries] = useState<Record<string, ComponentQueryState>>({});
  const [loading, setLoading] = useState(true);
  const [interaction, setInteraction] = useState<Interaction | null>(null);
  const [marquee, setMarquee] = useState<{ start: Point; end: Point } | null>(null);
  const [guides, setGuides] = useState<{ x?: number; y?: number }>({});
  const canvasRef = useRef<HTMLDivElement>(null);
  const definitionRef = useRef<DashboardDefinition | null>(null);
  const revisionRef = useRef(0);
  const changeRef = useRef(0);
  const savingRef = useRef(false);
  const clipboardRef = useRef<DashboardComponent[]>([]);
  const page = definition?.pages.find((item) => item.id === pageId) ?? definition?.pages[0] ?? null;
  const selected = page ? selectedComponents(page, selectedIds) : [];
  const primary = selected.at(-1) ?? null;
  const sorted = useMemo(() => [...(page?.components ?? [])].sort((a, b) => a.zIndex - b.zIndex), [page]);

  useEffect(() => { definitionRef.current = definition; }, [definition]);
  useEffect(() => { revisionRef.current = revision; }, [revision]);
  const load = useCallback(async () => {
    if (!session) return;
    setLoading(true); setError("");
    try {
      const [record, draft, nextCatalog] = await Promise.all([dashboardApi.dashboard(session.accessToken, dashboardId), dashboardApi.draft(session.accessToken, dashboardId), dashboardDataApi.catalog(session.accessToken)]);
      const nextSources = await dashboardDataApi.sources(session.accessToken, record.clientId);
      setDashboard(record); setDefinition(draft.definition); setPageId(draft.definition.pages[0]?.id ?? ""); setRevision(draft.revision);
      setCatalog(nextCatalog); setSources(nextSources); setComponentQueries({});
      definitionRef.current = draft.definition; revisionRef.current = draft.revision; changeRef.current = 0;
      setHistory([]); setFuture([]); setSelectedIds(new Set()); setSaveState("saved");
    } catch (caught) { setError(describeWorkspaceError(caught)); }
    finally { setLoading(false); }
  }, [dashboardId, session]);
  useEffect(() => { const timer = window.setTimeout(() => void load(), 0); return () => window.clearTimeout(timer); }, [load]);

  const commit = useCallback((next: DashboardDefinition, before?: DashboardDefinition) => {
    const previous = before ?? definitionRef.current;
    if (previous) setHistory((items) => [...items.slice(-(EDITOR_HISTORY_LIMIT - 1)), cloneDefinition(previous)]);
    setFuture([]); setDefinition(next); definitionRef.current = next; changeRef.current += 1; setSaveState("pending");
  }, []);
  const save = useCallback(async () => {
    if (!session || !definitionRef.current || savingRef.current || saveState === "conflict") return;
    const snapshot = cloneDefinition(definitionRef.current); const change = changeRef.current;
    savingRef.current = true; setSaveState("saving"); setError("");
    try {
      const draft = await dashboardApi.saveDraft(session.accessToken, dashboardId, revisionRef.current, snapshot);
      revisionRef.current = draft.revision; setRevision(draft.revision);
      if (changeRef.current === change) setSaveState("saved"); else { setSaveState("saving"); window.setTimeout(() => setSaveState("pending"), 0); }
    } catch (caught) {
      if (caught instanceof ApiError && caught.status === 409) setSaveState("conflict");
      else { setSaveState("error"); setError(describeWorkspaceError(caught)); }
    } finally { savingRef.current = false; }
  }, [dashboardId, saveState, session]);
  useEffect(() => { if (saveState !== "pending") return; const timer = window.setTimeout(() => void save(), 900); return () => window.clearTimeout(timer); }, [definition, save, saveState]);
  useEffect(() => { const handler = (event: BeforeUnloadEvent) => { if (saveState !== "saved") { event.preventDefault(); event.returnValue = ""; } }; window.addEventListener("beforeunload", handler); return () => window.removeEventListener("beforeunload", handler); }, [saveState]);

  const querySignature = useMemo(() => JSON.stringify((page?.components ?? []).filter((item) => DATA_COMPONENT_TYPES.includes(item.type)).map((item) => ({ id: item.id, source: item.data.sources[0]?.dataSourceId, secondSource: item.data.configuration?.secondSourceId, comparePrevious: item.data.configuration?.comparePrevious === true, metrics: Array.isArray(item.data.configuration?.metrics) ? item.data.configuration.metrics : [item.data.configuration?.metric].filter(Boolean), dimension: item.type === "kpiCard" || item.type === "goalGauge" ? "none" : item.data.configuration?.dimension, since: item.data.configuration?.since, until: item.data.configuration?.until, limit: item.data.configuration?.limit, sortMetric: item.data.configuration?.sortMetric, sortDirection: item.data.configuration?.sortDirection, dimensionValues: String(item.data.configuration?.dimensionFilter ?? "").split(",").map((value) => value.trim()).filter(Boolean) }))), [page]);
  useEffect(() => {
    if (!session || !dashboard) return;
    const timer = window.setTimeout(() => {
      const definitions = JSON.parse(querySignature) as Array<{ id: string; source?: string; secondSource?: string; comparePrevious?: boolean; metrics?: string[]; dimension?: string; since?: string; until?: string; limit?: number; sortMetric?: string; sortDirection?: "asc" | "desc"; dimensionValues?: string[] }>;
      for (const item of definitions) {
        if (!item.source || !item.metrics?.length || !item.since || !item.until || !item.dimension) { setComponentQueries((current) => ({ ...current, [item.id]: { status: "empty" } })); continue; }
        setComponentQueries((current) => ({ ...current, [item.id]: { status: "loading" } }));
        const request = { clientId: dashboard.clientId, dataSourceId: item.source, since: item.since, until: item.until, dimension: item.dimension, metrics: item.metrics, limit: item.limit, sortMetric: item.sortMetric, sortDirection: item.sortDirection, dimensionValues: item.dimensionValues };
        const operation = item.secondSource ? dashboardDataApi.cross(session.accessToken, { clientId: dashboard.clientId, dataSourceIds: [item.source, item.secondSource], since: item.since, until: item.until, dimension: item.dimension, metrics: item.metrics }).then((cross) => ({ ...request, provider: "Multicanal", currency: cross.canCombineMonetaryValues ? cross.series[0]?.currency ?? null : null, timeZone: "Múltiple", lastSyncedAtUtc: null, coverage: { requestedDays: 0, snapshotDays: 0, firstSnapshotDate: null, lastSnapshotDate: null }, rows: cross.series.flatMap((series) => series.rows.map((row) => ({ ...row, key: `${series.provider}:${row.key}`, label: `${series.provider} · ${row.label}` }))) } as DashboardQueryResult)) : item.comparePrevious ? dashboardDataApi.compare(session.accessToken, request).then((comparison) => ({ ...comparison.current, rows: [...comparison.current.rows, ...comparison.previous.rows.map((row) => ({ ...row, key: `previous:${row.key}`, label: `${row.label} · anterior` }))] })) : dashboardDataApi.query(session.accessToken, request);
        void operation
          .then((result) => setComponentQueries((current) => ({ ...current, [item.id]: { status: result.rows.length ? "ready" : "empty", result } })))
          .catch((caught) => setComponentQueries((current) => ({ ...current, [item.id]: { status: "error", error: describeWorkspaceError(caught) } })));
      }
    }, 500);
    return () => window.clearTimeout(timer);
  }, [dashboard, pageId, querySignature, session]);

  const undo = useCallback(() => {
    if (!history.length || !definitionRef.current) return; const previous = history.at(-1)!;
    setHistory((items) => items.slice(0, -1)); setFuture((items) => [cloneDefinition(definitionRef.current!), ...items].slice(0, EDITOR_HISTORY_LIMIT));
    setDefinition(previous); definitionRef.current = previous; setSelectedIds(new Set()); changeRef.current += 1; setSaveState("pending");
  }, [history]);
  const redo = useCallback(() => {
    if (!future.length || !definitionRef.current) return; const next = future[0];
    setFuture((items) => items.slice(1)); setHistory((items) => [...items, cloneDefinition(definitionRef.current!)].slice(-EDITOR_HISTORY_LIMIT));
    setDefinition(next); definitionRef.current = next; setSelectedIds(new Set()); changeRef.current += 1; setSaveState("pending");
  }, [future]);
  const updateSelection = useCallback((update: (item: DashboardComponent) => DashboardComponent) => { if (definitionRef.current && page) commit(updateComponents(definitionRef.current, page.id, selectedIds, update)); }, [commit, page, selectedIds]);
  const removeSelection = useCallback(() => {
    if (!definitionRef.current || !page || !selectedIds.size) return; const next = deleteComponents(definitionRef.current, page.id, selectedIds);
    if (JSON.stringify(next) !== JSON.stringify(definitionRef.current)) { commit(next); setSelectedIds(new Set()); }
  }, [commit, page, selectedIds]);
  const duplicateSelection = useCallback(() => {
    if (!definitionRef.current || !page || !selectedIds.size) return; const result = duplicateComponents(definitionRef.current, page.id, selectedIds); commit(result.definition); setSelectedIds(result.ids);
  }, [commit, page, selectedIds]);
  const copySelection = useCallback((cut = false) => { if (!page || !selectedIds.size) return; clipboardRef.current = structuredClone(selectedComponents(page, selectedIds)); if (cut) removeSelection(); }, [page, removeSelection, selectedIds]);
  const paste = useCallback(() => {
    if (!definitionRef.current || !page || !clipboardRef.current.length) return;
    const next = cloneDefinition(definitionRef.current); const nextPage = next.pages.find((item) => item.id === page.id)!;
    const maxZ = Math.max(0, ...nextPage.components.map((item) => item.zIndex)); const groups = new Map<string, string>();
    const copies = clipboardRef.current.map((item, index) => { const copy = structuredClone(item); copy.id = crypto.randomUUID(); copy.x = Math.min(page.width - copy.width, copy.x + 24); copy.y = Math.min(page.height - copy.height, copy.y + 24); copy.zIndex = maxZ + index + 1; copy.isLocked = false; if (copy.groupId) { if (!groups.has(copy.groupId)) groups.set(copy.groupId, crypto.randomUUID()); copy.groupId = groups.get(copy.groupId); } return copy; });
    nextPage.components.push(...copies); clipboardRef.current = structuredClone(copies); commit(next); setSelectedIds(new Set(copies.map((item) => item.id)));
  }, [commit, page]);
  useEffect(() => {
    function handler(event: KeyboardEvent) {
      if ((event.target as HTMLElement | null)?.matches("input, textarea, select")) return; const command = event.ctrlKey || event.metaKey; const key = event.key.toLowerCase();
      if (command && key === "z") { event.preventDefault(); if (event.shiftKey) redo(); else undo(); }
      else if (command && key === "y") { event.preventDefault(); redo(); }
      else if (command && key === "c") { event.preventDefault(); copySelection(); }
      else if (command && key === "x") { event.preventDefault(); copySelection(true); }
      else if (command && key === "v") { event.preventDefault(); paste(); }
      else if (command && key === "d") { event.preventDefault(); duplicateSelection(); }
      else if (["arrowleft","arrowright","arrowup","arrowdown"].includes(key) && definitionRef.current && page && selectedIds.size) {
        event.preventDefault(); const step=event.shiftKey?10:1; const dx=key==="arrowleft"?-step:key==="arrowright"?step:0; const dy=key==="arrowup"?-step:key==="arrowdown"?step:0;
        const next=updateComponents(definitionRef.current,page.id,selectedIds,(item)=>item.isLocked?item:{...item,x:Math.max(0,Math.min(page.width-item.width,item.x+dx)),y:Math.max(0,Math.min(page.height-item.height,item.y+dy))});commit(next);
      }
      else if (key === "delete" || key === "backspace") removeSelection();
      else if (key === "escape") { setSelectedIds(new Set()); setPreview(false); }
    }
    window.addEventListener("keydown", handler); return () => window.removeEventListener("keydown", handler);
  }, [commit, copySelection, duplicateSelection, page, paste, redo, removeSelection, selectedIds, undo]);

  const canvasPoint = useCallback((clientX: number, clientY: number): Point => { const rect = canvasRef.current!.getBoundingClientRect(); return { x: (clientX - rect.left) * page!.width / rect.width, y: (clientY - rect.top) * page!.height / rect.height }; }, [page]);
  useEffect(() => {
    if (!interaction || !page) return;
    function move(event: PointerEvent) {
      if (event.pointerId !== interaction!.pointerId || !canvasRef.current) return; const point = canvasPoint(event.clientX, event.clientY);
      if (interaction!.kind === "marquee") { setMarquee({ start: interaction!.start, end: point }); return; }
      const dx = point.x - interaction!.start.x, dy = point.y - interaction!.start.y; const next = cloneDefinition(interaction!.before); const nextPage = next.pages.find((item) => item.id === page!.id)!;
      let guideX: number | undefined, guideY: number | undefined;
      for (const item of nextPage.components) {
        const original = interaction!.originals.get(item.id); if (!original || item.isLocked) continue;
        if (interaction!.kind === "move") {
          item.x = Math.max(0, Math.min(page!.width - item.width, snap(original.x + dx, grid))); item.y = Math.max(0, Math.min(page!.height - item.height, snap(original.y + dy, grid)));
          const others = nextPage.components.filter((candidate) => !interaction!.ids.has(candidate.id));
          guideX = others.flatMap((other) => [other.x, other.x + other.width / 2, other.x + other.width]).find((position) => Math.abs(position - item.x) < 5 || Math.abs(position - item.x - item.width / 2) < 5);
          guideY = others.flatMap((other) => [other.y, other.y + other.height / 2, other.y + other.height]).find((position) => Math.abs(position - item.y) < 5 || Math.abs(position - item.y - item.height / 2) < 5);
        } else if (interaction!.ids.size === 1) { item.width = Math.max(80, Math.min(page!.width - item.x, snap(original.width + dx, grid))); item.height = Math.max(48, Math.min(page!.height - item.y, snap(original.height + dy, grid))); }
      }
      setGuides({ x: guideX, y: guideY }); setDefinition(next); definitionRef.current = next;
    }
    function up(event: PointerEvent) {
      if (event.pointerId !== interaction!.pointerId) return;
      if (interaction!.kind === "marquee" && marquee) { const left = Math.min(marquee.start.x, marquee.end.x), right = Math.max(marquee.start.x, marquee.end.x), top = Math.min(marquee.start.y, marquee.end.y), bottom = Math.max(marquee.start.y, marquee.end.y); setSelectedIds(new Set(page!.components.filter((item) => item.x < right && item.x + item.width > left && item.y < bottom && item.y + item.height > top).map((item) => item.id))); }
      else if (definitionRef.current && JSON.stringify(definitionRef.current) !== JSON.stringify(interaction!.before)) commit(definitionRef.current, interaction!.before);
      setInteraction(null); setMarquee(null); setGuides({});
    }
    window.addEventListener("pointermove", move); window.addEventListener("pointerup", up); return () => { window.removeEventListener("pointermove", move); window.removeEventListener("pointerup", up); };
  }, [canvasPoint, commit, grid, interaction, marquee, page]);

  function selectAndStart(event: ReactPointerEvent, item: DashboardComponent, kind: "move" | "resize") {
    event.stopPropagation(); if (!definitionRef.current || !page) return; let ids = new Set(selectedIds);
    if (event.shiftKey) { if (ids.has(item.id)) ids.delete(item.id); else ids.add(item.id); } else if (!ids.has(item.id)) ids = new Set([item.id]);
    ids = expandGroupSelection(page, ids); setSelectedIds(ids); if (item.isLocked || (kind === "resize" && ids.size > 1)) return;
    setInteraction({ kind, pointerId: event.pointerId, start: canvasPoint(event.clientX, event.clientY), before: cloneDefinition(definitionRef.current), ids, originals: new Map(page.components.filter((candidate) => ids.has(candidate.id)).map((candidate) => [candidate.id, structuredClone(candidate)])) });
  }
  function startMarquee(event: ReactPointerEvent) { if (!definitionRef.current || !page || event.target !== event.currentTarget) return; const point = canvasPoint(event.clientX, event.clientY); if (!event.shiftKey) setSelectedIds(new Set()); setMarquee({ start: point, end: point }); setInteraction({ kind: "marquee", pointerId: event.pointerId, start: point, before: cloneDefinition(definitionRef.current), ids: new Set(), originals: new Map() }); }
  function add(type: DashboardComponentType) { if (!definitionRef.current || !page) return; const next = cloneDefinition(definitionRef.current); const nextPage = next.pages.find((item) => item.id === page.id)!; const component = makeComponent(type, nextPage); nextPage.components.push(component); commit(next); setSelectedIds(new Set([component.id])); }
  function addPage(copy = false) { if (!definitionRef.current || !page || definitionRef.current.pages.length >= 25) return; const next = cloneDefinition(definitionRef.current); const created = createPage(next.pages.length, copy ? page : undefined); next.pages.push(created); normalizePageOrders(next); commit(next); setPageId(created.id); setSelectedIds(new Set()); }
  function renamePage() { if (!definitionRef.current || !page) return; const name = window.prompt("Nombre de la pÃ¡gina", page.name)?.trim(); if (!name || name === page.name) return; const next = cloneDefinition(definitionRef.current); next.pages.find((item) => item.id === page.id)!.name = name.slice(0, 120); commit(next); }
  function removePage() { if (!definitionRef.current || !page || definitionRef.current.pages.length === 1) return; if (page.components.length && !window.confirm(`Eliminar â€œ${page.name}â€ y sus ${page.components.length} elementos?`)) return; const next = cloneDefinition(definitionRef.current); next.pages = next.pages.filter((item) => item.id !== page.id); normalizePageOrders(next); commit(next); setPageId(next.pages[0].id); setSelectedIds(new Set()); }
  function reorderPage(direction: -1 | 1) { if (!definitionRef.current || !page) return; const next = cloneDefinition(definitionRef.current); const index = next.pages.findIndex((item) => item.id === page.id), target = index + direction; if (target < 0 || target >= next.pages.length) return; [next.pages[index], next.pages[target]] = [next.pages[target], next.pages[index]]; normalizePageOrders(next); commit(next); }
  function layout(next: DashboardDefinition) { if (definitionRef.current && JSON.stringify(next) !== JSON.stringify(definitionRef.current)) commit(next); }
  function layer(direction: 1 | -1) { if (!definitionRef.current || !page || !selectedIds.size) return; const values = page.components.map((item) => item.zIndex); let index = 0; const base = direction > 0 ? Math.max(...values) + 1 : Math.max(0, Math.min(...values) - selectedIds.size); commit(updateComponents(definitionRef.current, page.id, selectedIds, (item) => ({ ...item, zIndex: base + index++ }))); }
  function updateGeometry(key: "x" | "y" | "width" | "height", raw: number) {
    if (!page || !primary || !Number.isFinite(raw)) return;
    updateSelection((item) => {
      if (key === "x") return { ...item, x: Math.max(0, Math.min(page.width - item.width, raw)) };
      if (key === "y") return { ...item, y: Math.max(0, Math.min(page.height - item.height, raw)) };
      if (key === "width") return { ...item, width: Math.max(80, Math.min(page.width - item.x, raw)) };
      return { ...item, height: Math.max(48, Math.min(page.height - item.y, raw)) };
    });
  }
  function updatePageAppearance(key: "width" | "height" | "background", raw: string) {
    if (!definitionRef.current || !page) return; const next = cloneDefinition(definitionRef.current); const target = next.pages.find((item) => item.id === page.id)!;
    if (key === "background") target.background = raw;
    else {
      const minimum = key === "width" ? Math.max(320, ...target.components.map((item) => item.x + item.width)) : Math.max(320, ...target.components.map((item) => item.y + item.height));
      const maximum = key === "width" ? 4096 : 20000; target[key] = Math.max(minimum, Math.min(maximum, Number(raw) || minimum));
    }
    commit(next);
  }
  function connectData(sourceId: string) {
    const source = sources.find((item) => item.id === sourceId); if (!primary || !source) return;
    const until = source.availableUntil ?? new Date().toISOString().slice(0, 10);
    const earliest = source.availableSince ?? until;
    const candidate = new Date(`${until}T00:00:00Z`); candidate.setUTCDate(candidate.getUTCDate() - 29);
    const since = candidate.toISOString().slice(0, 10) < earliest ? earliest : candidate.toISOString().slice(0, 10);
    updateSelection((item) => {
      const metric = typeof item.data.configuration?.metric === "string" ? item.data.configuration.metric : "spend";
      const metrics = Array.isArray(item.data.configuration?.metrics) ? item.data.configuration.metrics : [metric];
      return { ...item, data: { sources: [{ dataSourceId: source.id }], configuration: { ...item.data.configuration, since, until, metric, metrics, metricLabel: dashboardMetricLabel(catalog, metric) } } };
    });
  }
  function configureData(key: string, nextValue: string | number | boolean) {
    updateSelection((item) => {
      const configuration: Record<string, unknown> = { ...item.data.configuration, [key]: nextValue, ...(key === "metric" ? { metrics: [nextValue], metricLabel: dashboardMetricLabel(catalog, String(nextValue)) } : {}) };
      if (key === "dimension") {
        const current = Array.isArray(configuration.metrics) ? configuration.metrics.filter((entry): entry is string => typeof entry === "string") : [];
        const compatible = current.filter((metric) => catalog?.metrics.find((entry) => entry.key === metric)?.supportedDimensions.includes(String(nextValue))).slice(0, 3);
        const nextMetrics = compatible.length ? compatible : [catalog?.metrics.find((metric) => metric.supportedDimensions.includes(String(nextValue)))?.key ?? "spend"];
        configuration.metrics = nextMetrics;
        configuration.sortMetric = nextMetrics[0];
      }
      return { ...item, data: { ...item.data, configuration } };
    });
  }
  function toggleMetric(metric: string) {
    updateSelection((item) => {
      const current = Array.isArray(item.data.configuration?.metrics) ? item.data.configuration.metrics.filter((entry): entry is string => typeof entry === "string") : [];
      const maximum = chartMetricLimit(item.type);
      const next = current.includes(metric) ? current.filter((entry) => entry !== metric) : maximum === 1 ? [metric] : [...current, metric].slice(0, maximum);
      return { ...item, data: { ...item.data, configuration: { ...item.data.configuration, metrics: next, sortMetric: next.includes(String(item.data.configuration?.sortMetric)) ? item.data.configuration?.sortMetric : next[0] } } };
    });
  }

  if (!session) return <main className="editor-state"><h1>Inicia sesiÃ³n</h1><Link href="/login" className="primary-button">Ir al acceso</Link></main>;
  if (!editable) return <main className="editor-state"><h1>Sin permiso de ediciÃ³n</h1><Link href="/app/informes" className="secondary-button">Volver</Link></main>;
  if (loading) return <main className="editor-state"><p>Cargando el lienzo persistenteâ€¦</p></main>;
  if (!definition || !page || !dashboard) return <main className="editor-state"><h1>No se pudo abrir el editor</h1><p>{error || "El borrador no estÃ¡ disponible."}</p><button onClick={() => void load()}>Reintentar</button></main>;
  const saveLabel = saveState === "saved" ? `Guardado Â· r${revision}` : saveState === "pending" ? "Cambios pendientes" : saveState === "saving" ? "Guardandoâ€¦" : saveState === "conflict" ? "Conflicto" : "Error al guardar";
  const marqueeStyle = marquee ? { left: `${Math.min(marquee.start.x, marquee.end.x) / page.width * 100}%`, top: `${Math.min(marquee.start.y, marquee.end.y) / page.height * 100}%`, width: `${Math.abs(marquee.end.x - marquee.start.x) / page.width * 100}%`, height: `${Math.abs(marquee.end.y - marquee.start.y) / page.height * 100}%` } : undefined;

  const advancedTypes: DashboardComponentType[] = ["map","bullet","treemap","sankey","waterfall","boxPlot","candlestick","timeline"];
  return <main className="visual-editor">
    <nav className="advanced-chart-toolbar" aria-label="Visualizaciones avanzadas"><span>Avanzadas</span>{advancedTypes.map((type)=><button key={type} onClick={()=>add(type)}>{labels[type]}</button>)}</nav>
    <header className="editor-topbar"><div className="editor-identity"><Link href="/app/informes" onClick={(event) => { if (saveState !== "saved" && !window.confirm("Hay cambios pendientes. Â¿Salir del editor?")) event.preventDefault(); }}>â†</Link><div><span>Borrador</span><strong>{dashboard.title}</strong></div></div><div className="editor-history"><button onClick={undo} disabled={!history.length}>â†¶</button><button onClick={redo} disabled={!future.length}>â†·</button><button className={grid ? "is-active" : ""} onClick={() => setGrid(!grid)}>CuadrÃ­cula</button><button onClick={() => setPreview(true)}>Vista previa</button><Link className="secondary-button" href={`/app/informes/${dashboardId}/compartir`}>Compartir</Link></div><div className={`editor-save save-${saveState}`}><i />{saveLabel}</div></header>
    {(saveState === "conflict" || saveState === "error") && <div className="editor-alert"><div><strong>{saveState === "conflict" ? "Otro editor guardÃ³ una versiÃ³n mÃ¡s reciente." : "No se pudo guardar."}</strong><span>{saveState === "conflict" ? "Tus cambios siguen visibles." : error}</span></div><button onClick={() => saveState === "conflict" ? (window.confirm("Â¿Descartar cambios locales y recargar?") && void load()) : setSaveState("pending")}>{saveState === "conflict" ? "Recargar servidor" : "Reintentar"}</button></div>}
    <nav className="editor-pages">{definition.pages.map((item) => <button key={item.id} className={item.id === page.id ? "is-active" : ""} onClick={() => { setPageId(item.id); setSelectedIds(new Set()); }}>{item.order + 1}<span>{item.name}</span></button>)}<button className="page-add" onClick={() => addPage()} disabled={definition.pages.length >= 25}>+ PÃ¡gina</button><div className="page-menu"><button onClick={renamePage}>Renombrar</button><button onClick={() => addPage(true)}>Duplicar</button><button onClick={() => reorderPage(-1)} disabled={page.order === 0}>â†</button><button onClick={() => reorderPage(1)} disabled={page.order === definition.pages.length - 1}>â†’</button><button onClick={removePage} disabled={definition.pages.length === 1}>Eliminar</button></div></nav>
    <div className="editor-shell"><aside className="editor-tools"><span>Insertar</span><button onClick={() => add("text")}><b>T</b>Texto</button><button onClick={() => add("kpiCard")}><b>01</b>Indicador</button><button onClick={() => add("table")}><b>TB</b>Tabla</button><button onClick={() => add("pivotTable")}><b>PV</b>Dinámica</button><button onClick={() => add("timeSeries")}><b>LN</b>Serie</button><button onClick={() => add("horizontalBar")}><b>BR</b>Barras</button><button onClick={() => add("column")}><b>CL</b>Columnas</button><button onClick={() => add("stackedBar")}><b>AP</b>Apiladas</button><button onClick={() => add("stacked100Bar")}><b>100</b>100 %</button><button onClick={() => add("pie")}><b>PI</b>Circular</button><button onClick={() => add("donut")}><b>DO</b>Dona</button><button onClick={() => add("area")}><b>AR</b>Área</button><button onClick={() => add("combo")}><b>CO</b>Combinado</button><button onClick={() => add("funnel")}><b>EM</b>Embudo</button><button onClick={() => add("scatter")}><b>DI</b>Dispersión</button><button onClick={() => add("bubble")}><b>BU</b>Burbujas</button><button onClick={() => add("goalGauge")}><b>ME</b>Medidor</button><button onClick={() => add("image")}><b>IM</b>Imagen</button><button onClick={() => add("divider")}><b>--</b>Separador</button><button onClick={() => add("shape")}><b>FO</b>Forma</button><small>Cada gráfico consulta exclusivamente fuentes asignadas al cliente.</small></aside>
      <section className="editor-stage"><div className="editor-page-label"><span>{page.name}</span><small>{page.width} Ã— {page.height} px Â· {page.components.length} elementos</small></div><div className="editor-zoom"><button onClick={() => setZoom(Math.max(25, zoom - 10))}>âˆ’</button><input aria-label="Zoom" type="range" min="25" max="150" value={zoom} onChange={(event) => setZoom(Number(event.target.value))} /><button onClick={() => setZoom(Math.min(150, zoom + 10))}>+</button><span>{zoom}%</span><button onClick={() => setZoom(75)}>Ajustar</button></div><div className="editor-canvas-viewport"><div className="editor-canvas-scale" style={{ width: `${zoom / 75 * 100}%` }}><div ref={canvasRef} className={`editor-canvas ${grid ? "show-grid" : ""}`} style={{ aspectRatio: `${page.width}/${page.height}`, background: page.background || "#eef0f3" }} onPointerDown={startMarquee}>
        {sorted.map((item) => <div key={item.id} className={`canvas-component component-${item.type} ${selectedIds.has(item.id) ? "is-selected" : ""} ${item.isLocked ? "is-locked" : ""}`} style={{ left: `${item.x / page.width * 100}%`, top: `${item.y / page.height * 100}%`, width: `${item.width / page.width * 100}%`, height: `${item.height / page.height * 100}%`, zIndex: item.zIndex, color: value(item, "style", "color", "#17202a"), background: value(item, "style", "background", item.type === "shape" ? "#d9c9fb" : "#ffffff"), borderColor: value(item, "style", "borderColor", "transparent") }} onPointerDown={(event) => selectAndStart(event, item, "move")}><RenderedComponent item={item} state={componentQueries[item.id]} catalog={catalog} />{item.isLocked && <span className="lock-mark">●</span>}{selectedIds.has(item.id) && selectedIds.size === 1 && !item.isLocked && <button className="resize-handle" aria-label="Redimensionar" onPointerDown={(event) => selectAndStart(event, item, "resize")} />}</div>)}
        {marqueeStyle && <div className="selection-marquee" style={marqueeStyle} />}{guides.x !== undefined && <i className="alignment-guide guide-x" style={{ left: `${guides.x / page.width * 100}%` }} />}{guides.y !== undefined && <i className="alignment-guide guide-y" style={{ top: `${guides.y / page.height * 100}%` }} />}{!page.components.length && <div className="canvas-empty"><b>Tu lienzo estÃ¡ listo</b><span>Inserta un elemento desde la barra izquierda.</span></div>}
      </div></div></div></section>
      <aside className="editor-properties">{!selected.length ? <><div className="properties-empty"><span>PÃ¡gina</span><strong>{page.name}</strong><p>Selecciona elementos o configura el lienzo actual.</p></div><div className="property-grid"><label>Ancho<input type="number" min="320" max="4096" value={page.width} onChange={(event) => updatePageAppearance("width", event.target.value)} /></label><label>Alto<input type="number" min="320" max="20000" value={page.height} onChange={(event) => updatePageAppearance("height", event.target.value)} /></label></div><label className="property-field">Fondo<input type="color" value={page.background || "#eef0f3"} onChange={(event) => updatePageAppearance("background", event.target.value)} /></label><p className="property-hint">Usa Shift o dibuja un Ã¡rea para seleccionar varios elementos.</p></> : <><div className="properties-heading"><div><span>{selected.length > 1 ? "SelecciÃ³n" : "Elemento"}</span><strong>{selected.length > 1 ? `${selected.length} elementos` : labels[primary!.type]}</strong></div><button onClick={() => updateSelection((item) => ({ ...item, isLocked: !selected.every((candidate) => candidate.isLocked) }))}>{selected.every((item) => item.isLocked) ? "Desbloquear" : "Bloquear"}</button></div>
        {selected.length === 1 && <><div className="property-grid">{(["x", "y", "width", "height"] as const).map((key) => <label key={key}>{key === "width" ? "Ancho" : key === "height" ? "Alto" : key.toUpperCase()}<input type="number" value={Math.round(primary![key])} disabled={primary!.isLocked} onChange={(event) => updateGeometry(key, Number(event.target.value))} /></label>)}</div>
          {(primary!.type === "text" || DATA_COMPONENT_TYPES.includes(primary!.type)) && <label className="property-field">{primary!.type === "text" ? "Texto" : "Título"}<textarea value={value(primary!, "data", primary!.type === "text" ? "text" : "title", "")} onChange={(event) => { const key = primary!.type === "text" ? "text" : "title"; updateSelection((item) => ({ ...item, data: { ...item.data, configuration: { ...item.data.configuration, [key]: event.target.value } } })); }} /></label>}
          {primary!.type === "image" && <><label className="property-field">Dirección HTTPS<input type="url" placeholder="https://…" value={value(primary!, "data", "imageUrl", "")} onChange={(event) => configureData("imageUrl", event.target.value)} /></label><label className="property-field">Descripción accesible<input value={value(primary!, "data", "alt", "Imagen del informe")} onChange={(event) => configureData("alt", event.target.value)} /></label></>}
          {primary!.type === "divider" && <div className="property-grid"><label>Color<input type="color" value={value(primary!, "style", "color", "#8054d8")} onChange={(event) => updateSelection((item) => ({ ...item, style: { ...item.style, color: event.target.value } }))} /></label><label>Grosor<input type="number" min="1" max="12" value={Number(primary!.style?.thickness ?? 2)} onChange={(event) => updateSelection((item) => ({ ...item, style: { ...item.style, thickness: Number(event.target.value) } }))} /></label></div>}
          {DATA_COMPONENT_TYPES.includes(primary!.type) && <DataProperties item={primary!} sources={sources} catalog={catalog} state={componentQueries[primary!.id]} onSource={connectData} onConfig={configureData} onMetric={toggleMetric} onStyle={(key, nextValue) => updateSelection((item) => ({ ...item, style: { ...item.style, [key]: nextValue } }))} />}
        </>}        <div className="alignment-actions"><span>Alinear</span>{(["left", "center", "right", "top", "middle", "bottom"] as const).map((mode) => <button key={mode} disabled={selected.length < 2} onClick={() => layout(alignComponents(definitionRef.current!, page.id, selectedIds, mode))}>{mode}</button>)}<button disabled={selected.length < 3} onClick={() => layout(distributeComponents(definitionRef.current!, page.id, selectedIds, "horizontal"))}>Distribuir H</button><button disabled={selected.length < 3} onClick={() => layout(distributeComponents(definitionRef.current!, page.id, selectedIds, "vertical"))}>Distribuir V</button></div>
        <div className="property-actions"><button onClick={() => layer(1)}>Al frente</button><button onClick={() => layer(-1)}>Al fondo</button><button disabled={selected.length < 2} onClick={() => commit(groupComponents(definitionRef.current!, page.id, selectedIds, true))}>Agrupar</button><button disabled={!selected.some((item) => item.groupId)} onClick={() => commit(groupComponents(definitionRef.current!, page.id, selectedIds, false))}>Desagrupar</button><button onClick={duplicateSelection}>Duplicar</button><button className="dangerous-text" onClick={removeSelection}>Eliminar</button></div>
        <div className="layer-list"><span>Capas</span>{[...page.components].sort((a, b) => b.zIndex - a.zIndex).map((item) => <button key={item.id} className={selectedIds.has(item.id) ? "is-active" : ""} onClick={(event) => setSelectedIds(event.shiftKey ? new Set([...selectedIds, item.id]) : expandGroupSelection(page, new Set([item.id])))}><i className={`layer-kind kind-${item.type}`} /><b>{labels[item.type] || item.type}</b><small>{item.isLocked ? "Bloqueado" : `z${item.zIndex}`}</small></button>)}</div></>}</aside>
    </div>
    {preview && <div className="editor-preview" role="dialog" aria-modal="true"><header><div><span>Vista previa del borrador</span><strong>{dashboard.title}</strong></div><button onClick={() => setPreview(false)}>Cerrar</button></header><nav>{definition.pages.map((item) => <button className={item.id === page.id ? "is-active" : ""} key={item.id} onClick={() => { setPageId(item.id); setSelectedIds(new Set()); }}>{item.name}</button>)}</nav><div className="preview-page" style={{ aspectRatio: `${page.width}/${page.height}`, background: page.background || "#eef0f3" }}>{sorted.map((item) => <div key={item.id} className={`preview-component component-${item.type}`} style={{ left: `${item.x / page.width * 100}%`, top: `${item.y / page.height * 100}%`, width: `${item.width / page.width * 100}%`, height: `${item.height / page.height * 100}%`, zIndex: item.zIndex, color: value(item, "style", "color", "#17202a"), background: value(item, "style", "background", item.type === "shape" ? "#d9c9fb" : "#ffffff") }}><RenderedComponent item={item} state={componentQueries[item.id]} catalog={catalog} /></div>)}</div></div>}
  </main>;
}

