import type { AnalysisComparisonType, AnalysisMetricKey, AnalysisResult } from "@/lib/analysis";

export interface CreateReportRequest {
  title: string;
  adAccountId: string;
  since: string;
  until: string;
  comparison: AnalysisComparisonType | null;
  comparisonSince: string | null;
  comparisonUntil: string | null;
  campaignIds: string[] | null;
  selectedMetrics: AnalysisMetricKey[] | null;
}

export interface ReportListItem {
  id: string;
  title: string;
  clientId: string;
  adAccountId: string;
  since: string;
  until: string;
  createdAtUtc: string;
  schemaVersion: number;
  snapshotHash: string;
}

export interface ReportData {
  reportId: string;
  schemaVersion: number;
  title: string;
  clientId: string;
  adAccountId: string;
  createdByUserId: string;
  createdAtUtc: string;
  snapshotHash: string;
  analysis: AnalysisResult;
}

export type ReportShareStatus = "Active" | "Expired" | "Revoked";

export interface ReportShareLink {
  id: string;
  reportId: string;
  status: ReportShareStatus;
  createdAtUtc: string;
  expiresAtUtc: string;
  revokedAtUtc: string | null;
}

export interface CreatedReportShareLink {
  shareLink: ReportShareLink;
  accessToken: string;
  sharePath: string;
}

export interface SharedReportData {
  title: string;
  createdAtUtc: string;
  analysis: AnalysisResult;
}
