import type { DashboardComponentType } from "@/lib/dashboards";

export const DATA_COMPONENT_TYPES: DashboardComponentType[] = [
  "kpiCard", "table", "pivotTable", "timeSeries", "horizontalBar", "column", "stackedBar",
  "stacked100Bar", "pie", "donut", "area", "combo", "funnel", "scatter", "bubble", "goalGauge",
  "map", "bullet", "treemap", "sankey", "waterfall", "boxPlot", "candlestick", "timeline",
];

export function chartMetricLimit(type: DashboardComponentType) {
  if (["table", "pivotTable", "timeSeries", "stackedBar", "stacked100Bar", "bubble"].includes(type)) return 3;
  if (["combo", "scatter", "sankey", "boxPlot", "candlestick"].includes(type)) return type === "candlestick" ? 3 : 2;
  return 1;
}

export function chartDimensions(type: DashboardComponentType): string[] {
  if (["table", "pivotTable"].includes(type)) return ["date", "campaign"];
  if (["timeSeries", "area", "combo", "waterfall", "candlestick", "timeline"].includes(type)) return ["date"];
  if (["kpiCard", "goalGauge", "bullet"].includes(type)) return ["none"];
  return ["campaign"];
}

export function chartRequiresSameUnit(type: DashboardComponentType) {
  return ["timeSeries", "stackedBar", "stacked100Bar"].includes(type);
}

export function buildPieStops(values: number[], colors: string[]) {
  const safe = values.map((value) => Math.max(0, value));
  const total = safe.reduce((sum, current) => sum + current, 0);
  if (!total || !colors.length) return "#e8ebed 0% 100%";
  return safe.map((current, index) => {
    const from = safe.slice(0, index).reduce((sum, previous) => sum + previous, 0) / total * 100;
    const until = from + current / total * 100;
    return `${colors[index % colors.length]} ${from}% ${until}%`;
  }).join(", ");
}
