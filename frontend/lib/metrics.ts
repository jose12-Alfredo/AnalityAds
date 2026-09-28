import { ApiError } from "@/lib/api";

export type InsightLevel = "Account" | "Campaign" | "AdSet" | "Ad";
export type ObservedDataQuality = "FieldPresencePreserved" | "LegacyZeroNormalized";
export type RangeAvailability = "CompleteForSnapshots" | "NoData" | "Incomplete" | "LegacyZeroNormalized" | "MixedCurrency" | "NotAvailableForRange" | "Undefined";
export type ComparisonType = "PreviousPeriod" | "PreviousMonth" | "PreviousYear" | "Custom";
export type ComparisonAvailability = "Available" | "CurrentUnavailable" | "BaselineUnavailable" | "UndefinedBaseline" | "CurrencyMismatch";
export type BenchmarkAvailability = "Available" | "MetricUnavailable" | "InsufficientComparableCampaigns" | "CurrencyMismatch" | "UndefinedBenchmark";

export interface InsightSnapshot {
  id: string;
  adAccountId: string;
  campaignId: string | null;
  adSetId: string | null;
  adId: string | null;
  level: InsightLevel;
  date: string;
  currency: string;
  observedDataQuality: ObservedDataQuality;
  observed: {
    spend: number | null;
    impressions: number | null;
    reach: number | null;
    linkClicks: number | null;
    leads: number | null;
    purchases: number | null;
    purchaseValue: number | null;
  };
  derived: {
    frequency: number | null;
    cpm: number | null;
    ctr: number | null;
    cpc: number | null;
    cpl: number | null;
    cpa: number | null;
    roas: number | null;
  };
  observedAtUtc: string;
}

export interface MetricsSyncResponse {
  adAccountId: string;
  since: string;
  until: string;
  accountSnapshots: number;
  campaignSnapshots: number;
  adSetSnapshots: number;
  adSnapshots: number;
  completedAtUtc: string;
}

export interface MetricValue { value: number | null; availability: RangeAvailability; }
export interface MetricsCoverage { requestedDays: number; snapshotDays: number; legacyZeroNormalizedSnapshotDays: number; firstSnapshotDate: string | null; lastSnapshotDate: string | null; }
export interface RangeMetricsSummary {
  adAccountId: string; campaignId: string | null; adSetId: string | null; adId: string | null; level: InsightLevel; since: string; until: string;
  currency: string | null; currencyStatus: "NoData" | "Single" | "Mixed"; coverage: MetricsCoverage;
  observed: Record<"spend" | "impressions" | "reach" | "linkClicks" | "leads" | "purchases" | "purchaseValue", MetricValue>;
  derived: Record<"frequency" | "cpm" | "ctr" | "cpc" | "cpl" | "cpa" | "roas", MetricValue>;
}
export interface CampaignSelectorItem { id: string; name: string; objective: string; configuredStatus: string; effectiveStatus: string; isPresentOnMeta: boolean; hasActivity: boolean; hasObservedSpend: boolean; currency: string | null; currencyStatus: "NoData" | "Single" | "Mixed"; coverage: MetricsCoverage; spend: MetricValue; }
export interface CampaignSelectorResponse { adAccountId: string; since: string; until: string; activityFilter: "WithActivity" | "WithSpend" | "All"; campaigns: CampaignSelectorItem[]; }
export interface ComparedMetric { current: number | null; baseline: number | null; absoluteChange: number | null; percentageChange: number | null; availability: ComparisonAvailability; }
export interface RangeMetricsComparison { comparisonType: ComparisonType; current: RangeMetricsSummary; baseline: RangeMetricsSummary; observed: Record<"spend" | "impressions" | "reach" | "linkClicks" | "leads" | "purchases" | "purchaseValue", ComparedMetric>; derived: Record<"frequency" | "cpm" | "ctr" | "cpc" | "cpl" | "cpa" | "roas", ComparedMetric>; }
export interface BenchmarkMetric { campaignValue: number | null; benchmarkValue: number | null; absoluteDifference: number | null; percentageDifference: number | null; comparableCampaigns: number; availability: BenchmarkAvailability; }
export interface CampaignBenchmarkItem { campaignId: string; name: string; objective: string; currency: string | null; cpm: BenchmarkMetric; ctr: BenchmarkMetric; cpc: BenchmarkMetric; cpl: BenchmarkMetric; cpa: BenchmarkMetric; roas: BenchmarkMetric; }
export interface CampaignBenchmarksResponse { adAccountId: string; since: string; until: string; method: "MedianOfOtherCampaignsWithSameObjective"; campaigns: CampaignBenchmarkItem[]; }

export function initialMetricRange() {
  const until = new Date();
  const since = new Date(until);
  since.setUTCDate(since.getUTCDate() - 6);
  const toDate = (value: Date) => value.toISOString().slice(0, 10);
  return { since: toDate(since), until: toDate(until) };
}

export function formatMetric(value: number | null, options: Intl.NumberFormatOptions = {}) {
  if (value === null) return "—";
  return new Intl.NumberFormat("es-BO", { maximumFractionDigits: 4, ...options }).format(value);
}

export function metricsError(error: unknown) {
  if (!(error instanceof ApiError)) return error instanceof Error ? error.message : "No pudimos consultar los snapshots.";
  switch (error.status) {
    case 400: return "El rango debe usar fechas válidas, inclusivas y no superar 90 días.";
    case 401: return "Tu sesión venció. Inicia sesión nuevamente.";
    case 403: return "Tu rol puede consultar snapshots, pero no sincronizar métricas.";
    case 404: return "No encontramos este recurso dentro de la agencia actual.";
    case 409: return "La cuenta no tiene una conexión Meta activa.";
    case 502: return "Meta no pudo procesar los insights. Sincroniza primero la estructura si el ID es nuevo.";
    case 500: return "El servidor no pudo completar la consulta. Intenta nuevamente.";
    default: return error.problem.detail || "No pudimos completar los snapshots.";
  }
}
