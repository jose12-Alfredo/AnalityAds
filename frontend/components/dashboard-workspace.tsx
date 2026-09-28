"use client";

import Link from "next/link";
import { FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import { ApiError, apiRequest } from "@/lib/api";
import { readSession } from "@/lib/auth-session";
import {
  canEditDashboards,
  dashboardApi,
  describeWorkspaceError,
  formatWorkspaceDate,
  type Dashboard,
  type DashboardDraft,
  type DashboardListItem,
  type DashboardTemplate,
  type Folder,
  type PublishedDashboard,
  type WorkspaceClient,
} from "@/lib/dashboards";

const rootFolder = "__root__";

export function DashboardWorkspace() {
  const session = readSession();
  const editable = !!session && canEditDashboards(session.role);
  const [clients, setClients] = useState<WorkspaceClient[]>([]);
  const [clientId, setClientId] = useState("");
  const [folders, setFolders] = useState<Folder[]>([]);
  const [dashboards, setDashboards] = useState<DashboardListItem[]>([]);
  const [templates, setTemplates] = useState<DashboardTemplate[]>([]);
  const [folderId, setFolderId] = useState<string>(rootFolder);
  const [dashboardId, setDashboardId] = useState("");
  const [dashboard, setDashboard] = useState<Dashboard | null>(null);
  const [draft, setDraft] = useState<DashboardDraft | null>(null);
  const [published, setPublished] = useState<PublishedDashboard | null>(null);
  const [search, setSearch] = useState("");
  const [includeArchived, setIncludeArchived] = useState(false);
  const [folderName, setFolderName] = useState("");
  const [dashboardTitle, setDashboardTitle] = useState("");
  const [dashboardDescription, setDashboardDescription] = useState("");
  const [moveFolderId, setMoveFolderId] = useState(rootFolder);
  const [duplicateClientId, setDuplicateClientId] = useState("");
  const [notice, setNotice] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  const activeClient = clients.find((client) => client.id === clientId) ?? null;
  const selectedFolder = folders.find((folder) => folder.id === folderId) ?? null;
  const visibleDashboards = useMemo(
    () => dashboards.filter((item) => folderId === rootFolder ? item.folderId === null : item.folderId === folderId),
    [dashboards, folderId],
  );

  const loadWorkspace = useCallback(async (nextClientId: string, archived: boolean, term: string) => {
    if (!session || !nextClientId) return;
    const [nextFolders, nextDashboards, nextTemplates] = await Promise.all([
      dashboardApi.folders(session.accessToken, nextClientId, archived),
      dashboardApi.dashboards(session.accessToken, nextClientId, archived, term),
      dashboardApi.templates(session.accessToken, nextClientId),
    ]);
    setFolders(nextFolders);
    setDashboards(nextDashboards);
    setTemplates(nextTemplates);
  }, [session]);

  useEffect(() => {
    if (!session) return;
    let active = true;
    void dashboardApi.clients(session.accessToken).then((items) => {
      if (!active) return;
      setClients(items);
      setClientId((current) => current || items[0]?.id || "");
      setDuplicateClientId((current) => current || items[0]?.id || "");
    }).catch((caught) => active && setError(describeWorkspaceError(caught))).finally(() => active && setLoading(false));
    return () => { active = false; };
  }, [session]);

  useEffect(() => {
    if (!clientId) return;
    const timer = window.setTimeout(() => {
      setError("");
      setLoading(true);
      void loadWorkspace(clientId, includeArchived, search)
        .catch((caught) => setError(describeWorkspaceError(caught)))
        .finally(() => setLoading(false));
    }, 0);
    return () => window.clearTimeout(timer);
  }, [clientId, includeArchived, loadWorkspace, search]);

  function changeClient(nextClientId: string) {
    setClientId(nextClientId);
    setFolderId(rootFolder);
    setDashboardId("");
    setDashboard(null);
    setDraft(null);
    setPublished(null);
    setDuplicateClientId(nextClientId);
  }

  async function refresh(message?: string) {
    if (!clientId) return;
    await loadWorkspace(clientId, includeArchived, search);
    if (message) setNotice(message);
  }

  async function installDefaultTemplates() {
    if (!session) return;
    await run(async () => {
      const result = await dashboardApi.installDefaultTemplates(session.accessToken);
      await refresh(result.installed ? `${result.installed} plantillas iniciales instaladas.` : "Las plantillas iniciales ya estaban instaladas.");
    });
  }

  async function run(action: () => Promise<void>) {
    setSaving(true);
    setError("");
    setNotice("");
    try { await action(); }
    catch (caught) {
      setError(describeWorkspaceError(caught));
      if (caught instanceof ApiError && caught.status === 409) await refresh();
    } finally { setSaving(false); }
  }

  async function selectDashboard(id: string, selectedItem?: DashboardListItem) {
    if (!session) return;
    setDashboardId(id);
    setError("");
    const item = selectedItem ?? dashboards.find((candidate) => candidate.id === id) ?? null;
    setDashboard(item);
    setDashboardTitle(item?.title ?? "");
    setDashboardDescription(item?.description ?? "");
    setMoveFolderId(item?.folderId ?? rootFolder);
    setDraft(null);
    setPublished(null);
    try {
      const tasks: Promise<unknown>[] = [];
      if (editable) tasks.push(dashboardApi.draft(session.accessToken, id).then(setDraft));
      if (item?.currentPublicationNumber) tasks.push(dashboardApi.published(session.accessToken, id).then(setPublished));
      await Promise.all(tasks);
    } catch (caught) { setError(describeWorkspaceError(caught)); }
  }

  function createFolder(event: FormEvent) {
    event.preventDefault();
    if (!session || !clientId || !folderName.trim()) return;
    void run(async () => {
      await apiRequest<Folder>(`/api/v1/clients/${clientId}/folders`, session.accessToken, {
        method: "POST",
        body: JSON.stringify({ name: folderName, parentFolderId: folderId === rootFolder ? null : folderId }),
      });
      setFolderName("");
      await refresh("Carpeta creada.");
    });
  }

  function renameFolder(folder: Folder) {
    const name = window.prompt("Nuevo nombre de la carpeta", folder.name)?.trim();
    if (!session || !name || name === folder.name) return;
    void run(async () => {
      await apiRequest(`/api/v1/folders/${folder.id}`, session.accessToken, {
        method: "PATCH", body: JSON.stringify({ name, expectedVersion: folder.version }),
      });
      await refresh("Carpeta renombrada.");
    });
  }

  function archiveFolder(folder: Folder) {
    if (!session || !window.confirm(`Archivar “${folder.name}” y sus subcarpetas?`)) return;
    void run(async () => {
      await apiRequest(`/api/v1/folders/${folder.id}?expectedVersion=${folder.version}`, session.accessToken,
        { method: "DELETE" });
      setFolderId(rootFolder);
      await refresh("Carpeta archivada.");
    });
  }

  function restoreFolder(folder: Folder) {
    if (!session) return;
    void run(async () => {
      await apiRequest(`/api/v1/folders/${folder.id}/restore`, session.accessToken, {
        method: "POST", body: JSON.stringify({ expectedVersion: folder.version }),
      });
      await refresh("Carpeta restaurada.");
    });
  }

  function createDashboard(event: FormEvent) {
    event.preventDefault();
    if (!session || !clientId || !dashboardTitle.trim()) return;
    void run(async () => {
      const created = await apiRequest<Dashboard>(`/api/v1/clients/${clientId}/dashboards`, session.accessToken, {
        method: "POST",
        body: JSON.stringify({ title: dashboardTitle, description: dashboardDescription,
          folderId: folderId === rootFolder ? null : folderId }),
      });
      setDashboardTitle("");
      setDashboardDescription("");
      await refresh("Informe creado con una página vacía persistente.");
      await selectDashboard(created.id, created);
    });
  }

  function updateDashboard(event: FormEvent) {
    event.preventDefault();
    if (!session || !dashboard || !dashboardTitle.trim()) return;
    void run(async () => {
      const updated = await apiRequest<Dashboard>(`/api/v1/dashboards/${dashboard.id}`, session.accessToken, {
        method: "PATCH", body: JSON.stringify({ title: dashboardTitle, description: dashboardDescription,
          expectedVersion: dashboard.version }),
      });
      setDashboard(updated);
      await refresh("Datos del informe guardados.");
    });
  }

  function moveDashboard() {
    if (!session || !dashboard) return;
    void run(async () => {
      const updated = await apiRequest<Dashboard>(`/api/v1/dashboards/${dashboard.id}/move`, session.accessToken, {
        method: "POST", body: JSON.stringify({ folderId: moveFolderId === rootFolder ? null : moveFolderId,
          expectedVersion: dashboard.version }),
      });
      setDashboard(updated);
      await refresh("Informe movido.");
    });
  }

  function duplicateDashboard() {
    if (!session || !dashboard || !duplicateClientId) return;
    void run(async () => {
      const copy = await apiRequest<Dashboard>(`/api/v1/dashboards/${dashboard.id}/duplicate`, session.accessToken, {
        method: "POST", body: JSON.stringify({ destinationClientId: duplicateClientId, folderId: null }),
      });
      if (duplicateClientId === clientId) {
        await refresh("Informe duplicado. Conservó las fuentes de este cliente.");
        await selectDashboard(copy.id, copy);
      } else {
        setNotice("Informe copiado al otro cliente. Sus fuentes quedaron pendientes de reasignación.");
      }
    });
  }

  function archiveDashboard() {
    if (!session || !dashboard || !window.confirm(`Archivar “${dashboard.title}”?`)) return;
    void run(async () => {
      await apiRequest(`/api/v1/dashboards/${dashboard.id}?expectedVersion=${dashboard.version}`,
        session.accessToken, { method: "DELETE" });
      setDashboardId(""); setDashboard(null); setDraft(null); setPublished(null);
      await refresh("Informe archivado.");
    });
  }

  function restoreDashboard() {
    if (!session || !dashboard) return;
    void run(async () => {
      const restored = await apiRequest<Dashboard>(`/api/v1/dashboards/${dashboard.id}/restore`, session.accessToken, {
        method: "POST", body: JSON.stringify({ expectedVersion: dashboard.version }),
      });
      setDashboard(restored);
      await refresh("Informe restaurado.");
    });
  }

  function publishDashboard() {
    if (!session || !dashboard || !draft) return;
    void run(async () => {
      const result = await apiRequest<PublishedDashboard>(`/api/v1/dashboards/${dashboard.id}/publish`,
        session.accessToken, { method: "POST", body: JSON.stringify({ expectedRevision: draft.revision }) });
      setPublished(result);
      await refresh(`Publicación ${result.publicationNumber} creada.`);
    });
  }

  if (!session) return <main className="dashboard-workspace"><div className="structure-empty"><h1>Inicia sesión</h1><p>Necesitas una sesión activa para trabajar con informes.</p></div></main>;

  const childFolders = folders.filter((folder) => folder.parentFolderId === (folderId === rootFolder ? null : folderId));
  const currentPath = selectedFolder ? selectedFolder.name : "Raíz del cliente";

  return <main className="dashboard-workspace">
    <header className="dashboard-workspace-header">
      <div><p className="eyebrow">Constructor de informes</p><h1>Espacios de cliente</h1><p>Organiza borradores y publicaciones sin mezclar cuentas, carpetas ni datos entre clientes.</p></div>
      <span className="role-pill">{session.role}</span>
    </header>

    {notice && <div className="notice notice-success" role="status">{notice}</div>}
    {error && <div className="notice notice-error" role="alert">{error}</div>}

    <section className="workspace-context" aria-label="Contexto del espacio de trabajo">
      <label>Cliente<select value={clientId} onChange={(event) => changeClient(event.target.value)} disabled={saving}>
        {clients.map((client) => <option key={client.id} value={client.id}>{client.name}{client.isActive ? "" : " · inactivo"}</option>)}
      </select></label>
      <label>Buscar informes<input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Nombre del informe" /></label>
      <label className="workspace-check"><input type="checkbox" checked={includeArchived} onChange={(event) => setIncludeArchived(event.target.checked)} /> Mostrar archivados</label>
    </section>

    {!loading && clients.length === 0 && <section className="structure-empty"><h2>No hay clientes disponibles</h2><p>Crea un cliente o solicita una asignación antes de construir informes.</p></section>}
    {loading ? <div className="structure-loading">Cargando el espacio de trabajo…</div> : activeClient && <div className="dashboard-explorer">
      <section className="folder-pane">
        <div className="pane-heading"><div><span>Carpetas</span><strong>{currentPath}</strong></div>{folderId !== rootFolder && <button className="text-button" type="button" onClick={() => setFolderId(selectedFolder?.parentFolderId ?? rootFolder)}>Subir</button>}</div>
        <button className={`folder-entry ${folderId === rootFolder ? "is-selected" : ""}`} type="button" onClick={() => setFolderId(rootFolder)}><span className="folder-glyph">⌂</span><span>Raíz del cliente</span></button>
        <div className="folder-list">{childFolders.map((folder) => <div className={`folder-line ${folder.isArchived ? "is-archived" : ""}`} key={folder.id}>
          <button className="folder-entry" type="button" onClick={() => setFolderId(folder.id)}><span className="folder-glyph">▰</span><span>{folder.name}</span></button>
          {editable && <div className="row-actions">{folder.isArchived ? <button type="button" onClick={() => restoreFolder(folder)}>Restaurar</button> : <><button type="button" onClick={() => renameFolder(folder)}>Renombrar</button><button type="button" onClick={() => archiveFolder(folder)}>Archivar</button></>}</div>}
        </div>)}</div>
        {editable && <form className="compact-create" onSubmit={createFolder}><input aria-label="Nombre de carpeta" value={folderName} onChange={(event) => setFolderName(event.target.value)} placeholder={folderId === rootFolder ? "Nueva carpeta" : "Nueva subcarpeta"} maxLength={160} required /><button className="secondary-button" type="submit" disabled={saving}>Crear</button></form>}
      </section>

      <section className="dashboard-pane">
        <div className="pane-heading"><div><span>Informes</span><strong>{visibleDashboards.length} en esta carpeta</strong></div><div><small>{templates.length} plantillas disponibles</small>{session && (session.role === "Owner" || session.role === "Admin") && <button className="text-button" type="button" disabled={saving} onClick={() => void installDefaultTemplates()}>Instalar plantillas iniciales</button>}</div></div>
        <div className="dashboard-list">{visibleDashboards.map((item) => <button className={`dashboard-entry ${dashboardId === item.id ? "is-selected" : ""} ${item.isArchived ? "is-archived" : ""}`} type="button" key={item.id} onClick={() => void selectDashboard(item.id)}>
          <span className="dashboard-entry-title">{item.title}</span><span>{item.description || "Sin descripción"}</span><span className="dashboard-entry-meta"><b>Borrador r{item.draftRevision}</b><b>{item.currentPublicationNumber ? `Publicado v${item.currentPublicationNumber}` : "Sin publicar"}</b></span>
        </button>)}</div>
        {visibleDashboards.length === 0 && <div className="pane-empty"><strong>Esta carpeta está lista</strong><span>Crea aquí el primer informe personalizado.</span></div>}
        {editable && <form className="dashboard-create" onSubmit={createDashboard}><h3>Nuevo informe</h3><input value={dashboardTitle} onChange={(event) => setDashboardTitle(event.target.value)} placeholder="Nombre del informe" maxLength={200} required /><textarea value={dashboardDescription} onChange={(event) => setDashboardDescription(event.target.value)} placeholder="Objetivo o destinatario" maxLength={2000} rows={2} /><button className="primary-button" type="submit" disabled={saving || !activeClient.isActive}>Crear lienzo vacío</button></form>}
      </section>

      <aside className="dashboard-inspector">
        {!dashboard ? <div className="inspector-empty"><span>Selecciona un informe</span><p>Aquí verás su borrador, publicación y acciones seguras.</p></div> : <>
          <div className="inspector-status"><span className={dashboard.isArchived ? "archive-chip" : published ? "published-chip" : "draft-chip"}>{dashboard.isArchived ? "Archivado" : published ? `Publicado v${published.publicationNumber}` : "Solo borrador"}</span><small>Actualizado {formatWorkspaceDate(dashboard.updatedAtUtc)}</small></div>
          <form className="inspector-form" onSubmit={updateDashboard}><label>Título<input value={dashboardTitle} onChange={(event) => setDashboardTitle(event.target.value)} disabled={!editable || dashboard.isArchived} /></label><label>Descripción<textarea value={dashboardDescription} onChange={(event) => setDashboardDescription(event.target.value)} rows={3} disabled={!editable || dashboard.isArchived} /></label>{editable && !dashboard.isArchived && <button className="secondary-button" type="submit" disabled={saving}>Guardar datos</button>}</form>
          <dl className="definition-summary"><div><dt>Revisión</dt><dd>{draft?.revision ?? dashboard.draftRevision}</dd></div><div><dt>Páginas</dt><dd>{draft?.definition.pages.length ?? "—"}</dd></div><div><dt>Componentes</dt><dd>{draft?.definition.pages.reduce((total, page) => total + page.components.length, 0) ?? "—"}</dd></div><div><dt>Esquema</dt><dd>{draft?.schemaVersion ?? "—"}</dd></div></dl>
          {editable && !dashboard.isArchived && <Link className="primary-button editor-open-button" href={`/app/informes/${dashboard.id}/editar`}>Abrir editor visual</Link>}
          {editable && !dashboard.isArchived && <div className="inspector-actions"><label>Mover a<select value={moveFolderId} onChange={(event) => setMoveFolderId(event.target.value)}><option value={rootFolder}>Raíz del cliente</option>{folders.filter((folder) => !folder.isArchived).map((folder) => <option value={folder.id} key={folder.id}>{folder.name}</option>)}</select></label><button className="text-button" type="button" onClick={moveDashboard} disabled={saving}>Mover informe</button><label>Duplicar para<select value={duplicateClientId} onChange={(event) => setDuplicateClientId(event.target.value)}>{clients.filter((client) => client.isActive).map((client) => <option value={client.id} key={client.id}>{client.name}</option>)}</select></label><button className="text-button" type="button" onClick={duplicateDashboard} disabled={saving}>Crear copia</button>{draft && <button className="primary-button publish-button" type="button" onClick={publishDashboard} disabled={saving}>Publicar revisión {draft.revision}</button>}<button className="danger-button" type="button" onClick={archiveDashboard} disabled={saving}>Archivar informe</button></div>}
          {editable && dashboard.isArchived && <button className="secondary-button" type="button" onClick={restoreDashboard} disabled={saving}>Restaurar informe</button>}
          {published && <div className="publication-proof"><span>Publicación inmutable</span><code>{published.definitionHash.slice(0, 16)}…</code><small>{formatWorkspaceDate(published.publishedAtUtc)}</small></div>}
        </>}
      </aside>
    </div>}
  </main>;
}
