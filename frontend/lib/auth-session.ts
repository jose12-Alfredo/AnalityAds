export type AgencyRole = "Owner" | "Admin" | "Analyst" | "Viewer" | "ClientViewer";

export interface AuthSession {
  accessToken: string;
  expiresAtUtc: string;
  userId: string;
  email: string;
  agencyId: string;
  agencyName: string;
  role: AgencyRole;
}

const sessionKey = "analitiads.auth.session";

export function readSession(): AuthSession | null {
  if (typeof window === "undefined") return null;

  const rawSession = window.sessionStorage.getItem(sessionKey);
  if (!rawSession) return null;

  try {
    const session = JSON.parse(rawSession) as AuthSession;
    return session.accessToken && session.expiresAtUtc ? session : null;
  } catch {
    window.sessionStorage.removeItem(sessionKey);
    return null;
  }
}

export function saveSession(session: AuthSession) {
  window.sessionStorage.setItem(sessionKey, JSON.stringify(session));
}

export function clearSession() {
  window.sessionStorage.removeItem(sessionKey);
}

export function canStartMetaConnection(role: AgencyRole) {
  return role === "Owner" || role === "Admin";
}

export function canManageMetaAccounts(role: AgencyRole) {
  return role === "Owner" || role === "Admin" || role === "Analyst";
}

export function canManageClientAccess(role: AgencyRole) {
  return role === "Owner" || role === "Admin";
}

export function canEditClients(role: AgencyRole) {
  return role === "Owner" || role === "Admin" || role === "Analyst";
}

export function canDeleteClients(role: AgencyRole) {
  return role === "Owner" || role === "Admin";
}

export function canCreateReports(role: AgencyRole) {
  return role === "Owner" || role === "Admin" || role === "Analyst";
}

export function canDeleteReports(role: AgencyRole) {
  return role === "Owner" || role === "Admin";
}

export function canManageReportShares(role: AgencyRole) {
  return role === "Owner" || role === "Admin";
}
