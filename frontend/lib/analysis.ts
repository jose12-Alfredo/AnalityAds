export type AnalysisComparisonType = "PreviousPeriod" | "PreviousMonth" | "PreviousYear" | "Custom";
export type AnalysisSufficiencyStatus = "Sufficient" | "Partial" | "Insufficient";

export const analysisMetrics = [
  ["spend", "Inversión", "money"],
  ["impressions", "Impresiones", "count"],
  ["reach", "Alcance", "count"],
  ["linkClicks", "Clics en enlace", "count"],
  ["leads", "Leads", "count"],
  ["purchases", "Compras", "count"],
  ["purchaseValue", "Valor de compras", "money"],
  ["frequency", "Frecuencia", "decimal"],
  ["cpm", "CPM", "money"],
  ["ctr", "CTR", "percent"],
  ["cpc", "CPC", "money"],
  ["cpl", "CPL", "money"],
  ["cpa", "CPA", "money"],
  ["roas", "ROAS", "decimal"],
] as const;

export type AnalysisMetricKey = (typeof analysisMetrics)[number][0];

export interface CreateAnalysisRequest {
  adAccountId: string;
  since: string;
  until: string;
  comparison: AnalysisComparisonType | null;
  comparisonSince: string | null;
  comparisonUntil: string | null;
  campaignIds: string[] | null;
  selectedMetrics: AnalysisMetricKey[] | null;
}

export interface AnalysisMetric {
  value: number | null;
  availability: string;
  source: string;
}

export interface AnalysisChange {
  current: number | null;
  baseline: number | null;
  absoluteChange: number | null;
  percentageChange: number | null;
  availability: string;
}

export interface AnalysisBenchmark {
  entityValue: number | null;
  benchmarkValue: number | null;
  absoluteDifference: number | null;
  percentageDifference: number | null;
  comparableEntities: number;
  availability: string;
}

export interface AnalysisCoverage {
  requestedDays: number;
  snapshotDays: number;
  legacyZeroNormalizedSnapshotDays: number;
  firstSnapshotDate: string | null;
  lastSnapshotDate: string | null;
}

export interface AnalysisSufficiency {
  status: AnalysisSufficiencyStatus;
  reasons: string[];
}

export interface AnalysisEntity {
  level: "Account" | "Campaign" | "AdSet" | "Ad";
  id: string;
  parentId: string | null;
  name: string;
  objective: string | null;
  currency: string | null;
  coverage: AnalysisCoverage;
  sufficiency: AnalysisSufficiency;
  metrics: Partial<Record<AnalysisMetricKey, AnalysisMetric>>;
  comparison: Partial<Record<AnalysisMetricKey, AnalysisChange>> | null;
  benchmarks: Partial<Record<AnalysisMetricKey, AnalysisBenchmark>> | null;
}

export interface AnalysisUnavailableSection {
  section: string;
  reason: string;
}

export interface AnalysisEvidence {
  metric: string;
  value: number | null;
  referenceValue: number | null;
  percentageDifference: number | null;
  availability: string;
}

export interface AnalysisInsight {
  ruleId: string;
  level: "Account" | "Campaign" | "AdSet" | "Ad";
  entityIds: string[];
  severity: "Info" | "Positive" | "Warning";
  confidence: "High" | "Medium" | "Low";
  sufficiency: AnalysisSufficiencyStatus;
  message: string;
  evidence: AnalysisEvidence[];
}

export interface AnalysisRecommendation {
  ruleId: string;
  level: "Account" | "Campaign" | "AdSet" | "Ad";
  entityIds: string[];
  priority: "High" | "Medium" | "Low";
  message: string;
  actions: string[];
}

export interface AnalysisResult {
  adAccountId: string;
  clientId: string;
  since: string;
  until: string;
  comparisonType: AnalysisComparisonType | null;
  selectedMetrics: AnalysisMetricKey[];
  evaluatedAtUtc: string;
  account: AnalysisEntity;
  campaigns: AnalysisEntity[];
  adSets: AnalysisEntity[];
  ads: AnalysisEntity[];
  unavailableSections: AnalysisUnavailableSection[];
  insights: AnalysisInsight[];
  recommendations: AnalysisRecommendation[];
}
