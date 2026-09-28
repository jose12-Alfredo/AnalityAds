import { apiRequest } from "@/lib/api";

export type GoogleProviderSlug = "google-ads" | "ga4" | "tiktok-ads";

function base(provider: GoogleProviderSlug) { return provider === "tiktok-ads" ? "/api/v1/integrations/tiktok-ads" : `/api/v1/integrations/google/${provider}`; }

export interface ProviderConnection {
  provider: string;
  status: "Pending" | "Connected" | "Error" | "Revoked";
  expiresAtUtc: string | null;
  lastSucceededAtUtc: string | null;
  lastErrorCode: string | null;
}

export interface ProviderSource {
  externalId: string;
  name: string;
  currency: string | null;
  timeZone: string;
  sourceType: string;
  isAssigned: boolean;
  clientId: string | null;
  isManager: boolean;
  isTest: boolean;
  dataSourceId: string | null;
}

export interface IntegrationClient { id: string; name: string; isActive: boolean }

export const providerApi = {
  status: (provider: GoogleProviderSlug, token: string) =>
    apiRequest<ProviderConnection>(`${base(provider)}/connection`, token),
  start: (provider: GoogleProviderSlug, token: string) =>
    apiRequest<{ authorizationUrl: string }>(`${base(provider)}/oauth/start`, token),
  sources: (provider: GoogleProviderSlug, token: string) =>
    apiRequest<ProviderSource[]>(`${base(provider)}/sources`, token),
  associate: (provider: GoogleProviderSlug, clientId: string, externalId: string, token: string) =>
    apiRequest<ProviderSource>(provider === "tiktok-ads" ? `/api/v1/clients/${clientId}/integrations/tiktok-ads/sources` : `/api/v1/clients/${clientId}/integrations/google/${provider}/sources`, token, {
      method: "POST", body: JSON.stringify({ externalId }),
    }),
  clients: (token: string) => apiRequest<IntegrationClient[]>("/api/v1/clients", token),
  sync: (sourceId: string, since: string, until: string, token: string) =>
    apiRequest<{ rowsReceived: number }>(`/api/v1/data-sources/${sourceId}/sync`, token, {
      method: "POST", body: JSON.stringify({ since, until }),
    }),
  schedule: (sourceId: string, enabled: boolean, token: string) =>
    apiRequest(`${`/api/v1/data-sources/${sourceId}/schedule`}`, token, {
      method: "PUT", body: JSON.stringify({ isEnabled: enabled, intervalMinutes: 1440, lookbackDays: 30 }),
    }),
};
