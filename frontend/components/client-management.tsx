"use client";

import Link from "next/link";
import { FormEvent, useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { ApiError, apiRequest } from "@/lib/api";
import { canDeleteClients, canEditClients, readSession, type AuthSession } from "@/lib/auth-session";

interface Client { id: string; name: string; isActive: boolean; createdAtUtc: string; updatedAtUtc: string; }
interface AdAccount { id: string; clientId: string; metaAccountId: string; name: string; currency: string; timeZone: string; connectionStatus: "Disconnected" | "Connected" | "Error"; isActive: boolean; }

function managementError(error: unknown) {
  if (!(error instanceof ApiError)) return error instanceof Error ? error.message : "No pudimos completar la operación.";
  if (error.status === 401) return "Tu sesión venció. Inicia sesión nuevamente.";
  if (error.status === 403) return "Tu rol no tiene permiso para esta operación.";
  if (error.status === 404) return "No encontramos este recurso en la agencia actual.";
  return error.problem.detail || "No pudimos completar la operación.";
}

export function ClientManagement({ initialClientId }: { initialClientId?: string }) {
  const router = useRouter();
  const [session] = useState<AuthSession | null>(() => readSession());
  const [clients, setClients] = useState<Client[]>([]);
  const [selectedId, setSelectedId] = useState(initialClientId ?? "");
  const [accounts, setAccounts] = useState<AdAccount[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [newClientName, setNewClientName] = useState("");
  const [draftName, setDraftName] = useState("");
  const [draftActive, setDraftActive] = useState(true);
  const [accountDraft, setAccountDraft] = useState({ metaAccountId: "", name: "", currency: "USD", timeZone: "America/La_Paz" });
  const [editingAccountId, setEditingAccountId] = useState<string | null>(null);
  const [accountEditDraft, setAccountEditDraft] = useState({ name: "", currency: "USD", timeZone: "America/La_Paz", isActive: true });

  const selectedClient = clients.find((client) => client.id === selectedId) ?? null;
  const loadClients = useCallback(async () => {
    if (!session) return;
    setIsLoading(true); setError(null);
    try {
      const nextClients = await apiRequest<Client[]>("/api/v1/clients", session.accessToken);
      setClients(nextClients);
      const nextId = initialClientId && nextClients.some((client) => client.id === initialClientId) ? initialClientId : selectedId && nextClients.some((client) => client.id === selectedId) ? selectedId : nextClients[0]?.id ?? "";
      setSelectedId(nextId);
    } catch (requestError) { setError(managementError(requestError)); }
    finally { setIsLoading(false); }
  }, [initialClientId, selectedId, session]);

  useEffect(() => { if (!session) { router.replace("/login"); return; } const timer = window.setTimeout(() => void loadClients(), 0); return () => window.clearTimeout(timer); }, [loadClients, router, session]);
  useEffect(() => { if (!selectedClient) return; const timer = window.setTimeout(() => { setDraftName(selectedClient.name); setDraftActive(selectedClient.isActive); }, 0); return () => window.clearTimeout(timer); }, [selectedClient]);
  useEffect(() => { const timer = window.setTimeout(() => { if (!session || !selectedId) { setAccounts([]); return; } void apiRequest<AdAccount[]>(`/api/v1/clients/${selectedId}/ad-accounts`, session.accessToken).then(setAccounts).catch((requestError) => setError(managementError(requestError))); }, 0); return () => window.clearTimeout(timer); }, [selectedId, session]);

  async function createClient(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!session || !canEditClients(session.role) || isSaving) return;
    setIsSaving(true); setError(null); setNotice(null);
    try { const client = await apiRequest<Client>("/api/v1/clients", session.accessToken, { method: "POST", body: JSON.stringify({ name: newClientName.trim() }) }); setNewClientName(""); setSelectedId(client.id); setNotice("Cliente creado."); await loadClients(); }
    catch (requestError) { setError(managementError(requestError)); } finally { setIsSaving(false); }
  }

  async function saveClient(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!session || !selectedClient || !canEditClients(session.role) || isSaving) return;
    setIsSaving(true); setError(null); setNotice(null);
    try { await apiRequest<Client>(`/api/v1/clients/${selectedClient.id}`, session.accessToken, { method: "PUT", body: JSON.stringify({ name: draftName.trim(), isActive: draftActive }) }); setNotice("Cambios del cliente guardados."); await loadClients(); }
    catch (requestError) { setError(managementError(requestError)); } finally { setIsSaving(false); }
  }

  async function deleteClient() {
    if (!session || !selectedClient || !canDeleteClients(session.role) || !window.confirm(`Eliminar “${selectedClient.name}”? Debes desvincular sus cuentas antes de eliminarlo.`)) return;
    setError(null); setNotice(null);
    try { await apiRequest<void>(`/api/v1/clients/${selectedClient.id}`, session.accessToken, { method: "DELETE" }); setNotice("Cliente eliminado."); setSelectedId(""); await loadClients(); }
    catch (requestError) { setError(managementError(requestError)); }
  }

  async function createAccount(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!session || !selectedClient || !canEditClients(session.role) || isSaving) return;
    setIsSaving(true); setError(null); setNotice(null);
    try { await apiRequest<AdAccount>(`/api/v1/clients/${selectedClient.id}/ad-accounts`, session.accessToken, { method: "POST", body: JSON.stringify({ ...accountDraft, metaAccountId: accountDraft.metaAccountId.trim(), name: accountDraft.name.trim(), currency: accountDraft.currency.trim().toUpperCase(), timeZone: accountDraft.timeZone.trim() }) }); setAccountDraft({ metaAccountId: "", name: "", currency: "USD", timeZone: "America/La_Paz" }); setAccounts(await apiRequest<AdAccount[]>(`/api/v1/clients/${selectedClient.id}/ad-accounts`, session.accessToken)); setNotice("Cuenta publicitaria creada."); }
    catch (requestError) { setError(managementError(requestError)); } finally { setIsSaving(false); }
  }

  async function toggleAccount(account: AdAccount) {
    if (!session || !canEditClients(session.role)) return;
    try { await apiRequest<AdAccount>(`/api/v1/ad-accounts/${account.id}`, session.accessToken, { method: "PUT", body: JSON.stringify({ name: account.name, currency: account.currency, timeZone: account.timeZone, isActive: !account.isActive }) }); setAccounts(await apiRequest<AdAccount[]>(`/api/v1/clients/${selectedId}/ad-accounts`, session.accessToken)); }
    catch (requestError) { setError(managementError(requestError)); }
  }

  function startEditingAccount(account: AdAccount) {
    setEditingAccountId(account.id);
    setAccountEditDraft({ name: account.name, currency: account.currency, timeZone: account.timeZone, isActive: account.isActive });
    setError(null);
    setNotice(null);
  }

  async function saveAccount(event: FormEvent<HTMLFormElement>, account: AdAccount) {
    event.preventDefault(); if (!session || !canEditClients(session.role) || isSaving) return;
    setIsSaving(true); setError(null); setNotice(null);
    try {
      await apiRequest<AdAccount>(`/api/v1/ad-accounts/${account.id}`, session.accessToken, {
        method: "PUT",
        body: JSON.stringify({ name: accountEditDraft.name.trim(), currency: accountEditDraft.currency.trim().toUpperCase(), timeZone: accountEditDraft.timeZone.trim(), isActive: accountEditDraft.isActive }),
      });
      setAccounts(await apiRequest<AdAccount[]>(`/api/v1/clients/${selectedId}/ad-accounts`, session.accessToken));
      setEditingAccountId(null);
      setNotice("Cambios de la cuenta guardados.");
    } catch (requestError) { setError(managementError(requestError)); } finally { setIsSaving(false); }
  }

  async function deleteAccount(account: AdAccount) {
    if (!session || !canDeleteClients(session.role) || !window.confirm(`Eliminar la cuenta “${account.name}”?`)) return;
    try { await apiRequest<void>(`/api/v1/ad-accounts/${account.id}`, session.accessToken, { method: "DELETE" }); setAccounts(await apiRequest<AdAccount[]>(`/api/v1/clients/${selectedId}/ad-accounts`, session.accessToken)); setNotice("Cuenta eliminada."); }
    catch (requestError) { setError(managementError(requestError)); }
  }

  if (!session) return <main className="private-loading">Validando sesión…</main>;
  const editable = canEditClients(session.role); const deletable = canDeleteClients(session.role);
  return <main className="management-page"><header className="management-header"><div><p className="eyebrow">Administración de agencia</p><h1>Clientes y cuentas</h1><p>La agencia se resuelve desde tu sesión. No necesitas indicar un tenant para administrar estos recursos.</p></div><span className="role-pill">{session.role} · {session.agencyName}</span></header>{notice && <div className="notice notice-success" role="status">{notice}</div>}{error && <div className="notice notice-error" role="alert">{error}</div>}
    <div className="management-grid"><aside className="client-list-card"><div className="section-heading"><div><p className="eyebrow">Clientes</p><h2>Agencia</h2></div><span>{clients.length}</span></div>{isLoading ? <div className="structure-loading">Consultando clientes…</div> : <ul>{clients.map((client) => <li key={client.id}><button className={selectedId === client.id ? "is-selected" : ""} type="button" onClick={() => { setSelectedId(client.id); router.replace(`/app/clientes/${client.id}`); }}><span>{client.name}</span><small>{client.isActive ? "Activo" : "Inactivo"}</small></button></li>)}</ul>}{editable && <form className="inline-create-form" onSubmit={(event) => void createClient(event)}><input value={newClientName} onChange={(event) => setNewClientName(event.target.value)} placeholder="Nombre del cliente" required maxLength={200} /><button className="primary-button" type="submit" disabled={isSaving}>Crear cliente</button></form>}</aside>
      <section className="client-detail-card">{selectedClient ? <><div className="section-heading"><div><p className="eyebrow">Cliente seleccionado</p><h2>{selectedClient.name}</h2></div><div><Link className="detail-link" href={`/app/clientes/${selectedClient.id}/reportes`}>Reportes</Link>{canDeleteClients(session.role) && <button className="text-button dangerous-text" type="button" onClick={() => void deleteClient()}>Eliminar</button>}</div></div><form className="client-edit-form" onSubmit={(event) => void saveClient(event)}><label>Nombre<input value={draftName} onChange={(event) => setDraftName(event.target.value)} disabled={!editable} required maxLength={200} /></label><label className="consent-check"><input type="checkbox" checked={draftActive} onChange={(event) => setDraftActive(event.target.checked)} disabled={!editable} /> Cliente activo</label>{editable && <button className="secondary-button" type="submit" disabled={isSaving}>Guardar cambios</button>}</form><section className="account-admin-section"><div className="section-heading"><div><p className="eyebrow">Cuentas publicitarias</p><h3>Asociadas a este cliente</h3></div><Link className="detail-link" href={`/app/clientes/${selectedClient.id}/accesos`}>Accesos externos</Link></div>{accounts.length ? <ul className="account-admin-list">{accounts.map((account) => <li key={account.id}><div><strong>{account.name}</strong><p>Meta: {account.metaAccountId} · {account.currency} · {account.timeZone}</p></div><div><span className={`connection-pill state-${account.connectionStatus.toLowerCase()}`}>{account.connectionStatus}</span>{editable && <><button className="text-button" type="button" onClick={() => startEditingAccount(account)}>Editar</button><button className="text-button" type="button" onClick={() => void toggleAccount(account)}>{account.isActive ? "Desactivar" : "Activar"}</button></>}{deletable && <button className="text-button dangerous-text" type="button" onClick={() => void deleteAccount(account)}>Eliminar</button>}<Link className="text-button" href={`/app/clientes/${selectedClient.id}/cuentas/${account.id}`}>Estructura</Link></div>{editingAccountId === account.id && <form className="account-edit-form" onSubmit={(event) => void saveAccount(event, account)}><label>Nombre<input value={accountEditDraft.name} onChange={(event) => setAccountEditDraft({ ...accountEditDraft, name: event.target.value })} required maxLength={200} /></label><label>Moneda<input value={accountEditDraft.currency} onChange={(event) => setAccountEditDraft({ ...accountEditDraft, currency: event.target.value })} required minLength={3} maxLength={3} /></label><label>Zona horaria<input value={accountEditDraft.timeZone} onChange={(event) => setAccountEditDraft({ ...accountEditDraft, timeZone: event.target.value })} required maxLength={100} /></label><label className="consent-check"><input type="checkbox" checked={accountEditDraft.isActive} onChange={(event) => setAccountEditDraft({ ...accountEditDraft, isActive: event.target.checked })} /> Cuenta activa</label><div className="account-edit-actions"><button className="secondary-button" type="submit" disabled={isSaving}>Guardar cuenta</button><button className="text-button" type="button" onClick={() => setEditingAccountId(null)} disabled={isSaving}>Cancelar</button></div></form>}</li>)}</ul> : <div className="structure-empty"><h3>No hay cuentas asociadas</h3><p>Crea una cuenta manual o vincula una cuenta disponible desde la integración Meta.</p></div>}{editable && <form className="account-create-form" onSubmit={(event) => void createAccount(event)}><label>ID de cuenta Meta<input value={accountDraft.metaAccountId} onChange={(event) => setAccountDraft({ ...accountDraft, metaAccountId: event.target.value })} required maxLength={64} /></label><label>Nombre<input value={accountDraft.name} onChange={(event) => setAccountDraft({ ...accountDraft, name: event.target.value })} required maxLength={200} /></label><label>Moneda<input value={accountDraft.currency} onChange={(event) => setAccountDraft({ ...accountDraft, currency: event.target.value })} required minLength={3} maxLength={3} /></label><label>Zona horaria<input value={accountDraft.timeZone} onChange={(event) => setAccountDraft({ ...accountDraft, timeZone: event.target.value })} required maxLength={100} /></label><button className="primary-button" type="submit" disabled={isSaving}>Crear cuenta manual</button></form>}</section></> : <div className="structure-empty"><h2>Selecciona un cliente</h2><p>Elige un cliente de la lista para ver sus cuentas y configuración.</p></div>}</section></div>
  </main>;
}
