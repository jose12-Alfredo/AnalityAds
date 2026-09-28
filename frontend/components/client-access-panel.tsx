"use client";

import Link from "next/link";
import { FormEvent, useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { ApiError, apiRequest } from "@/lib/api";
import { canManageClientAccess, clearSession, readSession, type AuthSession } from "@/lib/auth-session";

interface Client {
  id: string;
  name: string;
  isActive: boolean;
}

interface Invitation {
  id: string;
  clientId: string;
  email: string;
  status: "Pending" | "Accepted" | "Revoked" | "Expired";
  createdAtUtc: string;
  expiresAtUtc: string;
}

interface ExternalAccess {
  userId: string;
  email: string;
  role: "ClientViewer";
  grantedAtUtc: string;
}

interface CreatedInvitation {
  invitation: Invitation;
  invitationToken: string;
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat("es-BO", { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
}

function accessError(error: unknown) {
  if (!(error instanceof ApiError)) return error instanceof Error ? error.message : "No pudimos completar la operación.";
  if (error.status === 401) return "Tu sesión venció. Inicia sesión nuevamente.";
  if (error.status === 403) return "Solo Owner y Admin pueden administrar accesos externos.";
  if (error.status === 404) return "No encontramos este cliente o ya no tienes acceso a él.";
  if (error.status === 409) return error.problem.detail || "La operación entra en conflicto con el estado actual. Actualizamos las listas para que puedas revisarlas.";
  return error.problem.detail || "No pudimos completar la operación.";
}

export function ClientAccessPanel({ clientId }: { clientId: string }) {
  const router = useRouter();
  const [session] = useState<AuthSession | null>(() => readSession());
  const [client, setClient] = useState<Client | null>(null);
  const [invitations, setInvitations] = useState<Invitation[]>([]);
  const [users, setUsers] = useState<ExternalAccess[]>([]);
  const [email, setEmail] = useState("");
  const [oneTimeToken, setOneTimeToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isCopying, setIsCopying] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const load = useCallback(async (activeSession: AuthSession) => {
    setIsLoading(true);
    setError(null);
    try {
      const [nextClient, nextInvitations, nextUsers] = await Promise.all([
        apiRequest<Client>(`/api/v1/clients/${clientId}`, activeSession.accessToken),
        apiRequest<Invitation[]>(`/api/v1/clients/${clientId}/invitations`, activeSession.accessToken),
        apiRequest<ExternalAccess[]>(`/api/v1/clients/${clientId}/users`, activeSession.accessToken),
      ]);
      setClient(nextClient);
      setInvitations(nextInvitations);
      setUsers(nextUsers);
    } catch (loadError) {
      setClient(null);
      setInvitations([]);
      setUsers([]);
      setError(accessError(loadError));
    } finally {
      setIsLoading(false);
    }
  }, [clientId]);

  useEffect(() => {
    const activeSession = session;
    if (!activeSession) {
      router.replace("/login");
      return;
    }
    if (!canManageClientAccess(activeSession.role)) {
      return;
    }
    const timer = window.setTimeout(() => void load(activeSession), 0);
    return () => window.clearTimeout(timer);
  }, [load, router, session]);

  async function createInvitation(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!session || !canManageClientAccess(session.role) || isSubmitting) return;
    setError(null);
    setNotice(null);
    setOneTimeToken(null);
    setIsSubmitting(true);
    try {
      const created = await apiRequest<CreatedInvitation>(`/api/v1/clients/${clientId}/invitations`, session.accessToken, {
        method: "POST",
        body: JSON.stringify({ email: email.trim() }),
      });
      setEmail("");
      setOneTimeToken(created.invitationToken);
      setNotice("Invitación creada. Comparte el código por un canal privado; no se envió ningún correo.");
      await load(session);
    } catch (requestError) {
      setError(accessError(requestError));
      await load(session).catch(() => undefined);
    } finally {
      setIsSubmitting(false);
    }
  }

  async function copyToken() {
    if (!oneTimeToken || isCopying) return;
    setIsCopying(true);
    try {
      await navigator.clipboard.writeText(oneTimeToken);
      setNotice("Código copiado. Solo se muestra durante esta sesión de creación.");
    } catch {
      setError("No pudimos copiar el código. Selecciónalo y cópialo manualmente antes de cerrar esta pantalla.");
    } finally {
      setIsCopying(false);
    }
  }

  async function revokeInvitation(invitation: Invitation) {
    if (!session || !window.confirm(`Revocar la invitación pendiente de ${invitation.email}?`)) return;
    setError(null);
    setNotice(null);
    try {
      await apiRequest<void>(`/api/v1/clients/${clientId}/invitations/${invitation.id}`, session.accessToken, { method: "DELETE" });
      setNotice("Invitación revocada.");
      await load(session);
    } catch (requestError) {
      setError(accessError(requestError));
      await load(session).catch(() => undefined);
    }
  }

  async function revokeAccess(access: ExternalAccess) {
    if (!session || !window.confirm(`Quitar el acceso de ${access.email} a ${client?.name ?? "este cliente"}?`)) return;
    setError(null);
    setNotice(null);
    try {
      await apiRequest<void>(`/api/v1/clients/${clientId}/users/${access.userId}`, session.accessToken, { method: "DELETE" });
      setNotice("Acceso revocado. Las próximas consultas de esa persona quedarán bloqueadas.");
      await load(session);
    } catch (requestError) {
      setError(accessError(requestError));
      await load(session).catch(() => undefined);
    }
  }

  function signOut() {
    clearSession();
    router.replace("/login");
  }

  if (!session) return <main className="private-loading">Validando sesión…</main>;
  if (!canManageClientAccess(session.role)) return <main className="access-page"><section className="access-denied"><p className="eyebrow">Acceso restringido</p><h1>No puedes administrar accesos externos.</h1><p>Esta sección está disponible solo para Owner y Admin.</p><Link className="secondary-button" href="/app">Volver al dashboard</Link></section></main>;

  return (
    <main className="access-page">
      <header className="access-header">
        <div><Link className="brand-lockup" href="/app"><span className="brand-mark">A</span><span>Analiti<span>Ads</span></span></Link><p className="eyebrow">Cliente / accesos externos</p><h1>{client?.name ?? "Administrar accesos"}</h1></div>
        <div className="dashboard-header-actions"><span className="role-pill">{session.role} · {session.agencyName}</span><Link className="secondary-button" href="/app">Dashboard</Link><button className="text-button" type="button" onClick={signOut}>Cerrar sesión</button></div>
      </header>
      {notice && <div className="notice notice-success" role="status">{notice}</div>}
      {error && <div className="notice notice-error" role="alert">{error}</div>}
      {isLoading ? <div className="structure-loading">Consultando invitaciones y accesos…</div> : client ? <>
        <section className="access-create-card">
          <div><p className="eyebrow">Nueva invitación</p><h2>Da acceso a {client.name}</h2><p>La persona verá todas las cuentas presentes y futuras de este cliente. El acceso siempre será de solo lectura.</p></div>
          <form className="access-create-form" onSubmit={(event) => void createInvitation(event)}><label>Correo electrónico<input type="email" value={email} onChange={(event) => setEmail(event.target.value)} required autoComplete="email" /></label><button className="primary-button" type="submit" disabled={isSubmitting || !client.isActive}>{isSubmitting ? "Creando…" : "Crear invitación"}</button>{!client.isActive && <p className="field-note">No se pueden crear invitaciones para un cliente inactivo.</p>}</form>
          {oneTimeToken && <div className="one-time-token"><p><strong>Código de un solo uso</strong><span>Muéstralo o cópialo ahora. No se volverá a mostrar.</span></p><div><input value={oneTimeToken} readOnly aria-label="Código de invitación de un solo uso" /><button className="secondary-button" type="button" onClick={() => void copyToken()} disabled={isCopying}>{isCopying ? "Copiando…" : "Copiar código"}</button></div></div>}
        </section>
        <section className="access-grid">
          <section className="access-list-card" aria-labelledby="invitations-title"><div className="section-heading"><div><p className="eyebrow">Invitaciones</p><h2 id="invitations-title">Pendientes y anteriores</h2></div><span>{invitations.length}</span></div>{invitations.length ? <ul>{invitations.map((invitation) => <li key={invitation.id}><div><strong>{invitation.email}</strong><p>Creada: {formatDate(invitation.createdAtUtc)} · vence: {formatDate(invitation.expiresAtUtc)}</p></div><div className="access-row-actions"><span className={`invitation-status status-${invitation.status.toLowerCase()}`}>{invitation.status}</span>{invitation.status === "Pending" && <button className="text-button dangerous-text" type="button" onClick={() => void revokeInvitation(invitation)}>Revocar</button>}</div></li>)}</ul> : <div className="structure-empty"><h3>No hay invitaciones</h3><p>Crea una invitación para dar acceso de lectura a este cliente.</p></div>}</section>
          <section className="access-list-card" aria-labelledby="external-users-title"><div className="section-heading"><div><p className="eyebrow">Accesos activos</p><h2 id="external-users-title">ClientViewer</h2></div><span>{users.length}</span></div>{users.length ? <ul>{users.map((access) => <li key={access.userId}><div><strong>{access.email}</strong><p>{access.role} · concedido: {formatDate(access.grantedAtUtc)}</p></div><button className="text-button dangerous-text" type="button" onClick={() => void revokeAccess(access)}>Quitar acceso</button></li>)}</ul> : <div className="structure-empty"><h3>No hay accesos activos</h3><p>Cuando alguien acepte una invitación, aparecerá aquí.</p></div>}</section>
        </section>
      </> : <div className="structure-empty"><h3>No pudimos cargar el cliente</h3><p>Vuelve al dashboard y verifica que el cliente siga disponible.</p></div>}
    </main>
  );
}
