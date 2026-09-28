import { apiRequest } from "@/lib/api";

export interface DashboardMetricCatalogItem {
  key: string;
  name: string;
  description: string;
  provider: string;
  unit: "currency" | "count" | "percent" | "ratio";
  aggregation: string;
  valueType: string;
  supportedDimensions: string[];
  limitation: string | null;
}
export interface DashboardDimensionCatalogItem {
  key: string;
  name: string;
  provider: string;
  granularity: string;
  compatibleMetrics: string[];
  limitation: string | null;
}
export interface DashboardDataCatalog {
  schemaVersion: number;
  providers: string[];
  metrics: DashboardMetricCatalogItem[];
  dimensions: DashboardDimensionCatalogItem[];
}
export interface DashboardDataSource {
  id: string;
  clientId: string;
  provider: string;
  sourceType: string;
  name: string;
  currency: string | null;
  timeZone: string;
  isActive: boolean;
  availableSince: string | null;
  availableUntil: string | null;
  lastSyncedAtUtc: string | null;
}
export interface DashboardQueryMetric { value: number | null; availability: string; unit: string }
export interface DashboardQueryResult {
  clientId: string;
  dataSourceId: string;
  provider: string;
  dimension: string;
  since: string;
  until: string;
  currency: string | null;
  timeZone: string;
  lastSyncedAtUtc: string | null;
  coverage: { requestedDays: number; snapshotDays: number; firstSnapshotDate: string | null; lastSnapshotDate: string | null };
  rows: Array<{ key: string; label: string; dimensionValue: string | null; metrics: Record<string, DashboardQueryMetric> }>;
}

export interface DashboardQueryRequest {
  clientId: string;
  dataSourceId: string;
  since: string;
  until: string;
  dimension: string;
  metrics: string[];
  limit?: number;
  sortMetric?: string;
  sortDirection?: "asc" | "desc";
  dimensionValues?: string[];
}

export const dashboardDataApi = {
  catalog: (token: string) => apiRequest<DashboardDataCatalog>("/api/v1/dashboard-data/catalog", token),
  sources: (token: string, clientId: string) => apiRequest<DashboardDataSource[]>(`/api/v1/dashboard-data/clients/${clientId}/sources`, token),
  query: (token: string, body: DashboardQueryRequest) =>
    apiRequest<DashboardQueryResult>("/api/v1/dashboard-data/query", token, { method: "POST", body: JSON.stringify(body) }),
  compare: (token: string, body: DashboardQueryRequest) =>
    apiRequest<{ current: DashboardQueryResult; previous: DashboardQueryResult }>("/api/v1/dashboard-data/compare-previous", token, { method: "POST", body: JSON.stringify(body) }),
  cross: (token: string, body: { clientId: string; dataSourceIds: string[]; since: string; until: string; dimension: string; metrics: string[] }) =>
    apiRequest<{ since: string; until: string; dimension: string; canCombineMonetaryValues: boolean; limitation: string | null; series: Array<{ dataSourceId: string; provider: string; currency: string | null; rows: DashboardQueryResult["rows"] }> }>("/api/v1/dashboard-data/cross-channel", token, { method: "POST", body: JSON.stringify(body) }),
};

export function formatDashboardMetric(metric: DashboardQueryMetric | undefined, currency: string | null, decimals?: number) {
  if (!metric || metric.value === null || metric.availability !== "CompleteForSnapshots") return "—";
  const digits = Math.max(0, Math.min(6, decimals ?? (metric.unit === "count" ? 0 : 2)));
  if (metric.unit === "currency") return new Intl.NumberFormat("es-BO", { style: "currency", currency: currency || "USD", minimumFractionDigits: digits, maximumFractionDigits: digits }).format(metric.value);
  if (metric.unit === "percent") return `${new Intl.NumberFormat("es-BO", { minimumFractionDigits: digits, maximumFractionDigits: digits }).format(metric.value)} %`;
  return new Intl.NumberFormat("es-BO", { minimumFractionDigits: digits, maximumFractionDigits: digits }).format(metric.value);
}

export function dashboardMetricLabel(catalog: DashboardDataCatalog | null, key: string) {
  return catalog?.metrics.find((metric) => metric.key === key)?.name ?? key;
}
