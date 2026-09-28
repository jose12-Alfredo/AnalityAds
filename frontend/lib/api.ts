import { clearSession } from "@/lib/auth-session";

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly problem: ProblemDetails,
    public readonly retryAfter: string | null = null,
  ) {
    super(problem.detail || problem.title || "The request could not be completed.");
    this.name = "ApiError";
  }
}

function getApiBaseUrl() {
  const apiUrl = process.env.NEXT_PUBLIC_API_URL;

  if (!apiUrl) {
    throw new Error("Falta configurar NEXT_PUBLIC_API_URL para conectar AnalitiAds con la API.");
  }

  return apiUrl.replace(/\/$/, "");
}

async function readResponseBody(response: Response): Promise<ProblemDetails> {
  const contentType = response.headers.get("content-type") ?? "";

  if (contentType.includes("application/json")) {
    return (await response.json()) as ProblemDetails;
  }

  const detail = await response.text();
  return { status: response.status, detail };
}

export async function apiRequest<T>(
  path: string,
  accessToken: string,
  init: RequestInit = {},
): Promise<T> {
  const headers = new Headers(init.headers);
  headers.set("Authorization", `Bearer ${accessToken}`);

  if (init.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const response = await fetch(`${getApiBaseUrl()}${path}`, {
    ...init,
    headers,
    cache: "no-store",
  });

  if (!response.ok) {
    const error = new ApiError(response.status, await readResponseBody(response), response.headers.get("Retry-After"));
    if (response.status === 401 && typeof window !== "undefined") {
      clearSession();
      window.dispatchEvent(new Event("analitiads:session-expired"));
    }
    throw error;
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

function reportSessionExpired(response: Response) {
  if (response.status === 401 && typeof window !== "undefined") {
    clearSession();
    window.dispatchEvent(new Event("analitiads:session-expired"));
  }
}

export async function apiBlobRequest(
  path: string,
  accessToken: string,
  init: RequestInit = {},
): Promise<{ blob: Blob; filename: string | null }> {
  const headers = new Headers(init.headers);
  headers.set("Authorization", `Bearer ${accessToken}`);

  const response = await fetch(`${getApiBaseUrl()}${path}`, {
    ...init,
    headers,
    cache: "no-store",
  });

  if (!response.ok) {
    reportSessionExpired(response);
    throw new ApiError(response.status, await readResponseBody(response), response.headers.get("Retry-After"));
  }

  const disposition = response.headers.get("content-disposition") ?? "";
  const filename = /filename\*?=(?:UTF-8''|\")?([^;\"]+)/i.exec(disposition)?.[1] ?? null;
  return { blob: await response.blob(), filename };
}

export async function publicApiRequest<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers);
  if (init.body && !headers.has("Content-Type")) headers.set("Content-Type", "application/json");

  const response = await fetch(`${getApiBaseUrl()}${path}`, { ...init, headers, cache: "no-store" });
  if (!response.ok) throw new ApiError(response.status, await readResponseBody(response), response.headers.get("Retry-After"));
  return (await response.json()) as T;
}

export async function publicApiBlobRequest(
  path: string,
  init: RequestInit = {},
): Promise<{ blob: Blob; filename: string | null }> {
  const headers = new Headers(init.headers);
  if (init.body && !headers.has("Content-Type")) headers.set("Content-Type", "application/json");

  const response = await fetch(`${getApiBaseUrl()}${path}`, { ...init, headers, cache: "no-store" });
  if (!response.ok) throw new ApiError(response.status, await readResponseBody(response), response.headers.get("Retry-After"));

  const disposition = response.headers.get("content-disposition") ?? "";
  const filename = /filename\*?=(?:UTF-8''|\")?([^;\"]+)/i.exec(disposition)?.[1] ?? null;
  return { blob: await response.blob(), filename };
}
