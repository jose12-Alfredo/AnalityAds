import { ApiError, apiRequest } from "@/lib/api";
import type { AgencyRole } from "@/lib/auth-session";

export interface WorkspaceClient {
  id: string;
  name: string;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface Folder {
  id: string;
  clientId: string;
  parentFolderId: string | null;
  name: string;
  sortOrder: number;
  isArchived: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
  version: string;
}

export interface DashboardListItem {
  id: string;
  clientId: string;
  folderId: string | null;
  title: string;
  description: string;
  isArchived: boolean;
  draftRevision: number;
  currentPublicationNumber: number | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  version: string;
}

export type Dashboard = DashboardListItem;

export interface DashboardDraft {
  dashboardId: string;
  schemaVersion: number;
  revision: number;
  definition: DashboardDefinition;
  updatedByUserId: string;
  updatedAtUtc: string;
}

export type DashboardComponentType =
  | "kpiCard"
  | "text"
  | "shape"
  | "image"
  | "table"
  | "pivotTable"
  | "timeSeries"
  | "horizontalBar"
  | "column"
  | "stackedBar"
  | "stacked100Bar"
  | "pie"
  | "donut"
  | "area"
  | "combo"
  | "funnel"
  | "scatter"
  | "bubble"
  | "goalGauge"
  | "map"
  | "bullet"
  | "treemap"
  | "sankey"
  | "waterfall"
  | "boxPlot"
  | "candlestick"
  | "timeline"
  | "logo"
  | "divider"
  | "filterControl"
  | "dateRangeControl";

export interface DashboardSourceReference {
  dataSourceId?: string;
  sourceSlot?: string;
}

export interface DashboardComponent {
  id: string;
  type: DashboardComponentType;
  x: number;
  y: number;
  width: number;
  height: number;
  zIndex: number;
  isLocked: boolean;
  groupId?: string;
  data: {
    sources: DashboardSourceReference[];
    configuration?: Record<string, unknown>;
  };
  style?: Record<string, unknown>;
}

export interface DashboardPage {
  id: string;
  name: string;
  order: number;
  width: number;
  height: number;
  background?: string;
  components: DashboardComponent[];
}

export interface DashboardDefinition {
  schemaVersion: number;
  pages: DashboardPage[];
  theme?: Record<string, unknown>;
}

export interface PublishedDashboard {
  versionId: string;
  dashboardId: string;
  publicationNumber: number;
  draftRevision: number;
  schemaVersion: number;
  definition: DashboardDefinition;
  definitionHash: string;
  publishedByUserId: string;
  publishedAtUtc: string;
}

export interface DashboardTemplate {
  id: string;
  clientId: string | null;
  name: string;
  description: string;
  schemaVersion: number;
  definition: DashboardDefinition;
  createdAtUtc: string;
  version: string;
}

export function canEditDashboards(role: AgencyRole) {
  return role === "Owner" || role === "Admin" || role === "Analyst";
}

export function formatWorkspaceDate(value: string | null) {
  if (!value) return "Sin fecha";
  return new Intl.DateTimeFormat("es-BO", { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
}

export function describeWorkspaceError(error: unknown) {
  if (!(error instanceof ApiError)) return error instanceof Error ? error.message : "No se pudo completar la acción.";
  if (error.status === 403) return "Tu rol no tiene permiso para realizar esta acción.";
  if (error.status === 404) return "El recurso ya no está disponible dentro de tu espacio autorizado.";
  if (error.status === 409) return "Otra edición cambió este recurso. Recargamos los datos para evitar sobrescribirla.";
  return error.problem.detail || "No se pudo completar la acción.";
}

export const dashboardApi = {
  clients: (token: string) => apiRequest<WorkspaceClient[]>("/api/v1/clients", token),
  folders: (token: string, clientId: string, includeArchived = false) =>
    apiRequest<Folder[]>(`/api/v1/clients/${clientId}/folders?includeArchived=${includeArchived}`, token),
  dashboards: (token: string, clientId: string, includeArchived = false, search = "") =>
    apiRequest<DashboardListItem[]>(`/api/v1/clients/${clientId}/dashboards?includeArchived=${includeArchived}&search=${encodeURIComponent(search)}`, token),
  templates: (token: string, clientId: string) =>
    apiRequest<DashboardTemplate[]>(`/api/v1/clients/${clientId}/dashboard-templates`, token),
  installDefaultTemplates: (token: string) =>
    apiRequest<{ installed: number }>("/api/v1/dashboard-templates/defaults", token, { method: "POST" }),
  draft: (token: string, dashboardId: string) =>
    apiRequest<DashboardDraft>(`/api/v1/dashboards/${dashboardId}/draft`, token),
  dashboard: (token: string, dashboardId: string) =>
    apiRequest<Dashboard>(`/api/v1/dashboards/${dashboardId}`, token),
  saveDraft: (token: string, dashboardId: string, expectedRevision: number, definition: DashboardDefinition) =>
    apiRequest<DashboardDraft>(`/api/v1/dashboards/${dashboardId}/draft`, token, {
      method: "PUT",
      body: JSON.stringify({ expectedRevision, definition }),
    }),
  published: (token: string, dashboardId: string) =>
    apiRequest<PublishedDashboard>(`/api/v1/dashboards/${dashboardId}/published`, token),
};
