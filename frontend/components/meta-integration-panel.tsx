"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import { ApiError, apiRequest } from "@/lib/api";
import {
  type AgencyRole,
  type AuthSession,
  canManageMetaAccounts,
  canStartMetaConnection,
  clearSession,
  readSession,
  saveSession,
} from "@/lib/auth-session";

interface CurrentUser {
  userId: string;
  email: string;
  agencyId: string;
  agencyName: string;
  role: AgencyRole;
}

interface Client {
  id: string;
  name: string;
  isActive: boolean;
}

interface AdAccount {
  id: string;
  metaAccountId: string;
  name: string;
  currency: string;
  timeZone: string;
  connectionStatus: "Disconnected" | "Connected" | "Error";
}

interface MetaConnection {
  isConnected: boolean;
  expiresAtUtc: string | null;
}

interface MetaAccount {
  metaAccountId: string;
  name: string;
  currency: string;
  timeZone: string;
  accountStatus: number;
  isLinked: boolean;
  clientId: string | null;
}

type NoticeTone = "success" | "warning" | "error" | "info";

interface Notice {
  tone: NoticeTone;
  text: string;
}

const callbackNotices: Record<string, Notice> = {
  success: {
    tone: "success",
    text: "Meta se conectó correctamente. Actualizamos el estado y las cuentas disponibles.",
  },
  denied: {
    tone: "warning",
    text: "No autorizaste la conexión con Meta. Puedes intentarlo nuevamente cuando quieras.",
  },
  error: {
    tone: "error",
    text: "Meta no pudo completar la conexión. Revisa la configuración o vuelve a intentarlo.",
  },
};

function apiMessage(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return error instanceof Error ? error.message : "No pudimos completar la solicitud. Intenta nuevamente.";
  }

  switch (error.status) {
    case 401:
      return "Tu sesión ya no es válida. Inicia sesión otra vez para continuar.";
    case 403:
      return "Tu rol no tiene permiso para realizar esta acción.";
    case 404:
      return "No encontramos el cliente o la cuenta solicitada dentro de tu agencia.";
    case 409:
      return "La cuenta ya está asociada, o la conexión con Meta no está disponible. Actualiza e intenta otra vez.";
    case 502:
      return "Meta no pudo completar la solicitud en este momento. Vuelve a intentarlo.";
    case 503:
      return "La integración Meta aún no está configurada en el servidor.";
    default:
      return error.problem.detail || "No pudimos completar la solicitud. Intenta nuevamente.";
  }
}

function formatExpiration(value: string | null) {
  if (!value) return "Meta no informó una fecha de vencimiento";

  return new Intl.DateTimeFormat("es-BO", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

function AccountStatus({ status }: { status: "Disconnected" | "Connected" | "Error" }) {
  return <span className={`status-pill status-${status.toLowerCase()}`}>{status}</span>;
}

export function MetaIntegrationPanel() {
  const [session, setSession] = useState<AuthSession | null>(null);
  const [connection, setConnection] = useState<MetaConnection | null>(null);
  const [clients, setClients] = useState<Client[]>([]);
  const [metaAccounts, setMetaAccounts] = useState<MetaAccount[]>([]);
  const [selectedClientId, setSelectedClientId] = useState("");
  const [selectedClientAccounts, setSelectedClientAccounts] = useState<AdAccount[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isConnecting, setIsConnecting] = useState(false);
  const [associatingAccountId, setAssociatingAccountId] = useState<string | null>(null);
  const [notice, setNotice] = useState<Notice | null>(null);
  const [catalogError, setCatalogError] = useState<string | null>(null);
  const [sessionExpired, setSessionExpired] = useState(false);

  const refreshClientAccounts = useCallback(async (clientId: string, accessToken: string) => {
    const accounts = await apiRequest<AdAccount[]>(`/api/v1/clients/${clientId}/ad-accounts`, accessToken);
    setSelectedClientAccounts(accounts);
  }, []);

  const handleFailure = useCallback((error: unknown, target: "notice" | "catalog" = "notice") => {
    if (error instanceof ApiError && error.status === 401) {
      clearSession();
      setSession(null);
      setSessionExpired(true);
    }

    const message = apiMessage(error);
    if (target === "catalog") {
      setCatalogError(message);
    } else {
      setNotice({ tone: "error", text: message });
    }
  }, []);

  const loadIntegration = useCallback(async () => {
    const storedSession = readSession();
    setSession(storedSession);
    setIsLoading(true);
    setCatalogError(null);

    if (!storedSession) {
      setIsLoading(false);
      return;
    }

    try {
      const currentUser = await apiRequest<CurrentUser>("/api/v1/auth/me", storedSession.accessToken);
      const verifiedSession: AuthSession = { ...storedSession, ...currentUser };
      saveSession(verifiedSession);
      setSession(verifiedSession);

      const currentConnection = await apiRequest<MetaConnection>("/api/v1/meta/connection", verifiedSession.accessToken);
      setConnection(currentConnection);

      if (canManageMetaAccounts(verifiedSession.role)) {
        try {
          const [nextClients, nextMetaAccounts] = await Promise.all([
            apiRequest<Client[]>("/api/v1/clients", verifiedSession.accessToken),
            apiRequest<MetaAccount[]>("/api/v1/meta/ad-accounts", verifiedSession.accessToken),
          ]);
          setClients(nextClients);
          setMetaAccounts(nextMetaAccounts);
          setSelectedClientId((current) => current || nextClients.find((client) => client.isActive)?.id || nextClients[0]?.id || "");
        } catch (error) {
          handleFailure(error, "catalog");
        }
      }
    } catch (error) {
      handleFailure(error);
    } finally {
      setIsLoading(false);
    }
  }, [handleFailure]);

  useEffect(() => {
    const callbackResult = new URLSearchParams(window.location.search).get("meta");
    if (callbackResult && callbackNotices[callbackResult]) {
      void Promise.resolve().then(() => setNotice(callbackNotices[callbackResult]));
      window.history.replaceState(null, "", window.location.pathname);
    }

    const loadTimer = window.setTimeout(() => {
      void loadIntegration();
    }, 0);

    return () => window.clearTimeout(loadTimer);
  }, [loadIntegration]);

  useEffect(() => {
    if (!selectedClientId || !session || !canManageMetaAccounts(session.role)) {
      return;
    }

    const refreshTimer = window.setTimeout(() => {
      void refreshClientAccounts(selectedClientId, session.accessToken).catch((error) => handleFailure(error, "catalog"));
    }, 0);

    return () => window.clearTimeout(refreshTimer);
  }, [handleFailure, refreshClientAccounts, selectedClientId, session]);

  const selectedClient = useMemo(
    () => clients.find((client) => client.id === selectedClientId) ?? null,
    [clients, selectedClientId],
  );

  const canConnect = session ? canStartMetaConnection(session.role) : false;
  const canAssociate = session ? canManageMetaAccounts(session.role) : false;
  const connectionStatus: "Disconnected" | "Connected" | "Error" = catalogError || notice?.tone === "error"
    ? "Error"
    : connection?.isConnected
      ? "Connected"
      : "Disconnected";

  async function startConnection() {
    if (!session || !canConnect) return;

    setIsConnecting(true);
    setNotice(null);
    try {
      const result = await apiRequest<{ authorizationUrl: string }>("/api/v1/meta/oauth/start", session.accessToken, { method: "GET" });
      window.location.assign(result.authorizationUrl);
    } catch (error) {
      handleFailure(error);
      setIsConnecting(false);
    }
  }

  async function associateAccount(account: MetaAccount) {
    if (!session || !selectedClientId) {
      setNotice({ tone: "warning", text: "Selecciona un cliente antes de asociar una cuenta." });
      return;
    }

    setAssociatingAccountId(account.metaAccountId);
    setNotice(null);
    try {
      await apiRequest<MetaAccount>(`/api/v1/clients/${selectedClientId}/meta-ad-accounts`, session.accessToken, {
        method: "POST",
        body: JSON.stringify({ metaAccountId: account.metaAccountId }),
      });

      const [nextMetaAccounts] = await Promise.all([
        apiRequest<MetaAccount[]>("/api/v1/meta/ad-accounts", session.accessToken),
        refreshClientAccounts(selectedClientId, session.accessToken),
      ]);
      setMetaAccounts(nextMetaAccounts);
      setNotice({ tone: "success", text: `La cuenta ${account.name} quedó asociada a ${selectedClient?.name ?? "el cliente"}.` });
    } catch (error) {
      handleFailure(error);
    } finally {
      setAssociatingAccountId(null);
    }
  }

  return (
    <main className="meta-page">
      <div className="meta-app-shell">
        <aside className="meta-sidebar" aria-label="Navegación de la aplicación">
          <div className="brand-lockup" aria-label="AnalitiAds">
            <span className="brand-mark">A</span>
            <span>Analiti<span>Ads</span></span>
          </div>
          <p className="workspace-label">Área de agencia</p>
          <nav className="meta-nav">
            <span className="meta-nav-item is-muted">Resumen <small>Próximamente</small></span>
            <span className="meta-nav-item is-muted">Clientes</span>
            <span className="meta-nav-item is-active">Integraciones</span>
          </nav>
          <div className="sidebar-footnote">La conexión con Meta se gestiona de forma segura desde el servidor.</div>
        </aside>

        <section className="meta-workspace" aria-busy={isLoading}>
          <header className="meta-header">
            <div>
              <p className="eyebrow">Integraciones / Meta Ads</p>
              <h1>Conecta la fuente, no los secretos.</h1>
              <p className="header-description">Autoriza Meta para descubrir cuentas publicitarias y asociarlas al cliente correcto.</p>
            </div>
            {session && <span className="role-pill">{session.role} · {session.agencyName}</span>}
          </header>

          {notice && <div className={`notice notice-${notice.tone}`} role="status">{notice.text}</div>}

          {!session && (
            <section className="session-gate" aria-labelledby="session-title">
              <p className="eyebrow">Sesión requerida</p>
              <h2 id="session-title">Inicia sesión para administrar la integración.</h2>
              <p>{sessionExpired ? "La sesión anterior venció o dejó de ser válida." : "Esta vista valida la sesión antes de consultar datos privados."}</p>
              <div className="session-actions"><Link className="primary-button" href="/login">Iniciar sesión</Link><Link className="detail-link" href="/registro">Crear una agencia</Link></div>
            </section>
          )}

          {session && (
            <>
              <section className="connection-panel" aria-labelledby="connection-title">
                <div className="connection-signal" aria-hidden="true">
                  <span className={`signal-dot signal-${connectionStatus.toLowerCase()}`} />
                  <span className="signal-line" />
                  <span className="signal-end" />
                </div>
                <div className="connection-copy">
                  <p className="eyebrow">Conexión de la agencia</p>
                  <h2 id="connection-title">Meta Ads</h2>
                  <p>{connection?.isConnected ? `Autorización vigente. Vence: ${formatExpiration(connection.expiresAtUtc)}.` : "Aún no hay una autorización Meta activa para esta agencia."}</p>
                </div>
                <div className="connection-action">
                  <AccountStatus status={connectionStatus} />
                  {canConnect ? (
                    <button className="primary-button" type="button" onClick={startConnection} disabled={isConnecting}>
                      {isConnecting ? "Preparando conexión…" : connection?.isConnected ? "Reconectar Meta" : "Conectar con Meta"}
                    </button>
                  ) : (
                    <p className="permission-note">Solo Owner y Admin pueden conectar o reconectar Meta.</p>
                  )}
                </div>
              </section>

              {canAssociate ? (
                <section className="accounts-section" aria-labelledby="accounts-title">
                  <div className="section-heading">
                    <div>
                      <p className="eyebrow">Asociación de cuentas</p>
                      <h2 id="accounts-title">Cuentas disponibles en Meta</h2>
                    </div>
                    <button className="text-button" type="button" onClick={() => void loadIntegration()} disabled={isLoading}>Actualizar</button>
                  </div>

                  <div className="association-toolbar">
                    <label htmlFor="client-selector">
                      <span>Asociar al cliente</span>
                      <select id="client-selector" value={selectedClientId} onChange={(event) => setSelectedClientId(event.target.value)} disabled={clients.length === 0}>
                        <option value="">Selecciona un cliente</option>
                        {clients.map((client) => <option value={client.id} key={client.id}>{client.name}{client.isActive ? "" : " · Inactivo"}</option>)}
                      </select>
                    </label>
                    <p>El backend confirma que el cliente pertenece a tu agencia antes de guardar la asociación.</p>
                  </div>

                  {catalogError && <div className="catalog-error" role="alert">{catalogError}</div>}

                  {isLoading ? (
                    <div className="account-skeletons" aria-label="Cargando cuentas Meta"><span /><span /><span /></div>
                  ) : metaAccounts.length > 0 ? (
                    <div className="accounts-list">
                      {metaAccounts.map((account) => {
                        const linkedClient = clients.find((client) => client.id === account.clientId);
                        const accountState: "Disconnected" | "Connected" = account.isLinked ? "Connected" : "Disconnected";
                        return (
                          <article className="account-row" key={account.metaAccountId}>
                            <div className="account-identifiers">
                              <h3>{account.name}</h3>
                              <p>act_{account.metaAccountId} <span aria-hidden="true">·</span> {account.currency} <span aria-hidden="true">·</span> {account.timeZone}</p>
                            </div>
                            <div className="account-meta"><span>Estado Meta</span><strong>{account.accountStatus}</strong></div>
                            <div className="account-link-state">
                              <AccountStatus status={accountState} />
                              <p>{account.isLinked ? `Asociada a ${linkedClient?.name ?? "un cliente"}` : "Disponible para asociar"}</p>
                            </div>
                            {account.isLinked ? (
                              <span className="linked-label">Asociada</span>
                            ) : (
                              <button className="secondary-button" type="button" onClick={() => void associateAccount(account)} disabled={!selectedClientId || associatingAccountId === account.metaAccountId}>
                                {associatingAccountId === account.metaAccountId ? "Asociando…" : "Asociar cuenta"}
                              </button>
                            )}
                          </article>
                        );
                      })}
                    </div>
                  ) : (
                    <div className="empty-state">
                      <h3>{connection?.isConnected ? "No hay cuentas disponibles" : "Conecta Meta para ver sus cuentas"}</h3>
                      <p>{connection?.isConnected ? "La conexión no devolvió cuentas publicitarias disponibles para este usuario Meta." : "Cuando la autorización se complete, las cuentas aparecerán aquí."}</p>
                    </div>
                  )}

                  {selectedClient && (
                    <section className="client-accounts" aria-labelledby="linked-accounts-title">
                      <div>
                        <p className="eyebrow">Cliente seleccionado</p>
                        <h3 id="linked-accounts-title">Cuentas de {selectedClient.name}</h3>
                      </div>
                      {selectedClientAccounts.length ? (
                        <ul>{selectedClientAccounts.map((account) => <li key={account.id}><span>{account.name}</span><span className="client-account-actions"><AccountStatus status={account.connectionStatus} /><Link className="detail-link" href={`/app/clientes/${selectedClient.id}/cuentas/${account.id}`}>Ver estructura</Link></span></li>)}</ul>
                      ) : <p className="muted-copy">Todavía no hay cuentas asociadas a este cliente.</p>}
                    </section>
                  )}
                </section>
              ) : (
                <section className="permission-panel">
                  <h2>Tu rol puede consultar el estado de conexión.</h2>
                  <p>La lista y asociación de cuentas Meta requieren rol Owner, Admin o Analyst.</p>
                </section>
              )}
            </>
          )}
        </section>
      </div>
    </main>
  );
}
