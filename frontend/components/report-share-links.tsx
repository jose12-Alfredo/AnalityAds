"use client";

import { useCallback, useEffect, useState } from "react";
import { ApiError, apiRequest } from "@/lib/api";
import type { AuthSession } from "@/lib/auth-session";
import type { CreatedReportShareLink, ReportShareLink } from "@/lib/reports";

function shareError(error: unknown) {
  if (!(error instanceof ApiError)) return "No pudimos completar la operación del enlace.";
  if (error.status === 400) return error.problem.detail || "Elige una vigencia entre 1 y 30 días.";
  if (error.status === 401) return "Tu sesión venció. Inicia sesión nuevamente.";
  if (error.status === 403) return "Tu rol no permite administrar enlaces compartibles.";
  if (error.status === 404) return "El reporte o enlace ya no está disponible.";
  if (error.status === 409) return error.problem.detail || "El enlace no pudo completarse por un conflicto.";
  if (error.status === 429) return "Hay demasiadas solicitudes. Espera un momento antes de reintentar.";
  if (error.status >= 500) return "El servidor no pudo completar la operación. Intenta nuevamente.";
  return error.problem.detail || "No pudimos completar la operación del enlace.";
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat("es-BO", { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
}

export function ReportShareLinks({ reportId, session, onReportUnavailable }: { reportId: string; session: AuthSession; onReportUnavailable: () => void }) {
  const [links, setLinks] = useState<ReportShareLink[]>([]);
  const [expirationDays, setExpirationDays] = useState(7);
  const [created, setCreated] = useState<CreatedReportShareLink | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadLinks = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      setLinks(await apiRequest<ReportShareLink[]>(`/api/v1/reports/${reportId}/share-links`, session.accessToken));
    } catch (requestError) {
      setLinks([]);
      setError(shareError(requestError));
      if (requestError instanceof ApiError && requestError.status === 404) onReportUnavailable();
    } finally {
      setIsLoading(false);
    }
  }, [onReportUnavailable, reportId, session.accessToken]);

  useEffect(() => {
    const timer = window.setTimeout(() => void loadLinks(), 0);
    return () => window.clearTimeout(timer);
  }, [loadLinks]);

  async function createLink() {
    if (isSaving) return;
    setIsSaving(true);
    setError(null);
    setCreated(null);
    try {
      const next = await apiRequest<CreatedReportShareLink>(`/api/v1/reports/${reportId}/share-links`, session.accessToken, { method: "POST", body: JSON.stringify({ expirationDays }) });
      setCreated(next);
      setLinks((current) => [next.shareLink, ...current]);
    } catch (requestError) {
      setError(shareError(requestError));
      if (requestError instanceof ApiError && requestError.status === 404) onReportUnavailable();
    } finally {
      setIsSaving(false);
    }
  }

  async function revokeLink(link: ReportShareLink) {
    if (isSaving || !window.confirm("¿Revocar este enlace? Quien lo tenga perderá acceso inmediatamente.")) return;
    setIsSaving(true);
    setError(null);
    try {
      await apiRequest<void>(`/api/v1/reports/${reportId}/share-links/${link.id}`, session.accessToken, { method: "DELETE" });
      setLinks((current) => current.map((item) => item.id === link.id ? { ...item, status: "Revoked", revokedAtUtc: new Date().toISOString() } : item));
      if (created?.shareLink.id === link.id) setCreated(null);
    } catch (requestError) {
      setError(shareError(requestError));
      if (requestError instanceof ApiError && requestError.status === 404) await loadLinks();
    } finally {
      setIsSaving(false);
    }
  }

  const shareUrl = created ? (typeof window === "undefined" ? created.sharePath : new URL(created.sharePath, window.location.origin).toString()) : "";
  return <section className="share-links-card" aria-labelledby="share-links-title">
    <div className="section-heading"><div><p className="eyebrow">Acceso público controlado</p><h2 id="share-links-title">Enlaces compartibles</h2><p>El token se muestra una sola vez. Los enlaces pueden durar entre 1 y 30 días.</p></div><button className="text-button" type="button" onClick={() => void loadLinks()} disabled={isLoading || isSaving}>Actualizar</button></div>
    <div className="share-create-row"><label>Vigencia<select value={expirationDays} onChange={(event) => setExpirationDays(Number(event.target.value))}>{[1, 3, 7, 14, 30].map((days) => <option key={days} value={days}>{days} {days === 1 ? "día" : "días"}</option>)}</select></label><button className="secondary-button" type="button" onClick={() => void createLink()} disabled={isSaving}>{isSaving ? "Procesando…" : "Crear enlace"}</button></div>
    {created && <div className="share-token-once" role="status"><div><strong>Guarda este enlace ahora</strong><span>No volverá a aparecer en el listado.</span></div><input readOnly value={shareUrl} aria-label="Enlace público recién creado" /><button className="secondary-button" type="button" onClick={() => void navigator.clipboard.writeText(shareUrl)}>Copiar</button></div>}
    {error && <div className="notice notice-error" role="alert">{error}</div>}
    {isLoading ? <div className="structure-loading">Consultando enlaces…</div> : links.length ? <ul className="share-links-list">{links.map((link) => <li key={link.id}><div><strong className={`share-status status-${link.status.toLowerCase()}`}>{link.status}</strong><p>Creado {formatDate(link.createdAtUtc)} · vence {formatDate(link.expiresAtUtc)}</p></div>{link.status === "Active" && <button className="text-button dangerous-text" type="button" onClick={() => void revokeLink(link)} disabled={isSaving}>Revocar</button>}</li>)}</ul> : <div className="reports-empty"><h3>Aún no hay enlaces</h3><p>Crea uno para compartir este snapshot sin exigir una sesión.</p></div>}
  </section>;
}
