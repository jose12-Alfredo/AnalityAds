import { ApiError } from "@/lib/api";

export type SyncStatus = "NeverSynced" | "Running" | "Succeeded" | "Failed";

export interface SyncResponse {
  adAccountId: string;
  status: SyncStatus;
  startedAtUtc: string | null;
  completedAtUtc: string | null;
  campaignsSynced: number;
  adSetsSynced: number;
  adsSynced: number;
  errorCode: string | null;
}

export interface Campaign {
  id: string;
  adAccountId: string;
  metaCampaignId: string;
  name: string;
  objective: string;
  configuredStatus: string;
  effectiveStatus: string;
  startsAtUtc: string | null;
  stopsAtUtc: string | null;
  metaCreatedAtUtc: string | null;
  metaUpdatedAtUtc: string | null;
  lastSyncedAtUtc: string;
  isPresentOnMeta: boolean;
}

export interface AdSet {
  id: string;
  campaignId: string;
  metaAdSetId: string;
  name: string;
  optimizationGoal: string;
  billingEvent: string;
  configuredStatus: string;
  effectiveStatus: string;
  startsAtUtc: string | null;
  endsAtUtc: string | null;
  metaCreatedAtUtc: string | null;
  metaUpdatedAtUtc: string | null;
  lastSyncedAtUtc: string;
  isPresentOnMeta: boolean;
}

export interface Ad {
  id: string;
  adSetId: string;
  metaAdId: string;
  name: string;
  configuredStatus: string;
  effectiveStatus: string;
  metaCreatedAtUtc: string | null;
  metaUpdatedAtUtc: string | null;
  lastSyncedAtUtc: string;
  isPresentOnMeta: boolean;
}

export function canSynchronize(role: string) {
  return role === "Owner" || role === "Admin" || role === "Analyst";
}

export function formatUtc(value: string | null) {
  if (!value) return "Sin fecha informada";

  return new Intl.DateTimeFormat("es-BO", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

export function describeAdvertisingError(error: unknown) {
  if (!(error instanceof ApiError)) {
    return error instanceof Error ? error.message : "No se pudo completar la solicitud.";
  }

  switch (error.status) {
    case 401:
      return "Tu sesión venció. Inicia sesión nuevamente para continuar.";
    case 403:
      return "Tu rol no tiene permiso para realizar esta acción.";
    case 404:
      return "No encontramos este objeto dentro de la agencia actual.";
    case 409:
      return "La cuenta no está conectada, la conexión venció o ya existe una sincronización en curso.";
    case 502:
      return "Meta no pudo devolver una estructura consistente. Conservamos los datos de la última sincronización.";
    case 503:
      return "La integración con Meta no está configurada en el servidor.";
    default:
      return error.problem.detail || "No se pudo completar la solicitud.";
  }
}
