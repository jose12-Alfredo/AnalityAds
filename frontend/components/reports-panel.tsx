"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { ApiError, apiBlobRequest, apiRequest } from "@/lib/api";
import { canDeleteReports, canManageReportShares, readSession, type AuthSession } from "@/lib/auth-session";
import { AnalysisResults } from "@/components/analysis-engine-panel";
import { ReportShareLinks } from "@/components/report-share-links";
import { SnapshotChart } from "@/components/snapshot-chart";
import { type ReportData, type ReportListItem } from "@/lib/reports";

const pageSize = 20;

function reportError(error: unknown) {
  if (!(error instanceof ApiError)) return error instanceof Error ? error.message : "No pudimos completar la operación del reporte.";
  if (error.status === 400) return error.problem.detail || Object.values(error.problem.errors ?? {}).flat().join(" ") || "Revisa los datos del reporte.";
  if (error.status === 401) return "Tu sesión venció. Inicia sesión nuevamente.";
  if (error.status === 403) return "Tu rol no tiene permiso para esta operación de reportes.";
  if (error.status === 404) return "Este reporte o cliente ya no está disponible dentro de tus accesos autorizados.";
  if (error.status === 409) return error.problem.detail || "El reporte no pudo completarse por un conflicto.";
  if (error.status === 429) return "Hay demasiadas solicitudes. Espera un momento antes de reintentar.";
  if (error.status >= 500) return "El servidor no pudo completar la operación. Intenta nuevamente.";
  return error.problem.detail || "No pudimos completar la operación del reporte. Intenta nuevamente.";
}

function formatTimestamp(value: string) {
  return new Intl.DateTimeFormat("es-BO", { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
}

export function ReportsList({ clientId }: { clientId: string }) {
  const router = useRouter();
  const [session] = useState<AuthSession | null>(() => readSession());
  const [reports, setReports] = useState<ReportListItem[]>([]);
  const [page, setPage] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [isAccessRevoked, setIsAccessRevoked] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadReports = useCallback(async () => {
    if (!session) return;
    setIsLoading(true);
    setError(null);
    try {
      const nextReports = await apiRequest<ReportListItem[]>(`/api/v1/clients/${clientId}/reports?page=${page}&pageSize=${pageSize}`, session.accessToken);
      setReports(nextReports);
      setIsAccessRevoked(false);
    } catch (requestError) {
      setReports([]);
      setError(reportError(requestError));
      if (requestError instanceof ApiError && requestError.status === 404) setIsAccessRevoked(true);
    } finally {
      setIsLoading(false);
    }
  }, [clientId, page, session]);

  useEffect(() => {
    if (!session) {
      router.replace("/login");
      return;
    }
    const timer = window.setTimeout(() => void loadReports(), 0);
    return () => window.clearTimeout(timer);
  }, [loadReports, router, session]);

  if (!session) return <main className="private-loading">Validando sesión…</main>;

  return <main className="reports-page">
    <header className="reports-header">
      <div><p className="eyebrow">Snapshot histórico</p><h1>Reportes del cliente</h1><p>Los reportes guardados conservan la evidencia, conclusiones y recomendaciones evaluadas al momento de crearlos.</p></div>
      <Link className="secondary-button" href={`/app/clientes/${clientId}`}>Volver al cliente</Link>
    </header>
    {error && <div className="notice notice-error" role="alert">{error}</div>}
    {isAccessRevoked ? <section className="reports-empty"><h2>El acceso a este cliente cambió</h2><p>Quitamos la lista de reportes porque el cliente ya no forma parte de tus accesos autorizados.</p><Link className="primary-button" href="/app/clientes">Ver clientes autorizados</Link></section> : <section className="reports-list-card" aria-labelledby="reports-list-title">
      <div className="section-heading"><div><p className="eyebrow">Historial inmutable</p><h2 id="reports-list-title">Reportes guardados</h2></div><button className="text-button" type="button" onClick={() => void loadReports()} disabled={isLoading}>Actualizar</button></div>
      {isLoading ? <div className="structure-loading">Consultando reportes autorizados…</div> : reports.length ? <><ul>{reports.map((report) => <li key={report.id}><div><Link href={`/app/clientes/${clientId}/reportes/${report.id}`}>{report.title}</Link><p>{report.since} a {report.until} · creado {formatTimestamp(report.createdAtUtc)}</p><small>Esquema v{report.schemaVersion} · integridad {report.snapshotHash.slice(0, 12)}…</small></div><Link className="secondary-button" href={`/app/clientes/${clientId}/reportes/${report.id}`}>Abrir reporte</Link></li>)}</ul><div className="reports-pagination"><button className="text-button" type="button" onClick={() => setPage((current) => current - 1)} disabled={page === 1}>Anterior</button><span>Página {page}</span><button className="text-button" type="button" onClick={() => setPage((current) => current + 1)} disabled={reports.length < pageSize}>Siguiente</button></div></> : <div className="reports-empty"><h3>No hay reportes guardados</h3><p>Genera un análisis y guárdalo como reporte para conservar un snapshot de esta evidencia.</p></div>}
    </section>}
  </main>;
}

export function ReportDetail({ clientId, reportId }: { clientId: string; reportId: string }) {
  const router = useRouter();
  const [session] = useState<AuthSession | null>(() => readSession());
  const [report, setReport] = useState<ReportData | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isDownloading, setIsDownloading] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const clearRevokedReport = useCallback(() => {
    setReport(null);
    router.replace(`/app/clientes/${clientId}/reportes`);
  }, [clientId, router]);

  const loadReport = useCallback(async () => {
    if (!session) return;
    setIsLoading(true);
    setError(null);
    try {
      const nextReport = await apiRequest<ReportData>(`/api/v1/reports/${reportId}`, session.accessToken);
      if (nextReport.clientId !== clientId) {
        setReport(null);
        setError("Este reporte no corresponde al cliente seleccionado.");
        return;
      }
      setReport(nextReport);
    } catch (requestError) {
      setReport(null);
      setError(reportError(requestError));
      if (requestError instanceof ApiError && requestError.status === 404) clearRevokedReport();
    } finally {
      setIsLoading(false);
    }
  }, [clearRevokedReport, clientId, reportId, session]);

  useEffect(() => {
    if (!session) {
      router.replace("/login");
      return;
    }
    const timer = window.setTimeout(() => void loadReport(), 0);
    return () => window.clearTimeout(timer);
  }, [loadReport, router, session]);

  async function downloadPdf() {
    if (!session || !report || isDownloading) return;
    setIsDownloading(true);
    setError(null);
    let objectUrl: string | null = null;
    try {
      const { blob, filename } = await apiBlobRequest(`/api/v1/reports/${report.reportId}/pdf`, session.accessToken);
      objectUrl = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = objectUrl;
      link.download = filename || `analitiads-report-${report.reportId}.pdf`;
      document.body.appendChild(link);
      link.click();
      link.remove();
    } catch (requestError) {
      setError(reportError(requestError));
      if (requestError instanceof ApiError && requestError.status === 404) clearRevokedReport();
    } finally {
      if (objectUrl) URL.revokeObjectURL(objectUrl);
      setIsDownloading(false);
    }
  }

  async function deleteReport() {
    if (!session || !report || isDeleting || !canDeleteReports(session.role) || !window.confirm(`Eliminar el reporte “${report.title}”? Esta acción no se puede deshacer.`)) return;
    setIsDeleting(true);
    setError(null);
    try {
      await apiRequest<void>(`/api/v1/reports/${report.reportId}`, session.accessToken, { method: "DELETE" });
      setReport(null);
      router.replace(`/app/clientes/${clientId}/reportes`);
    } catch (requestError) {
      setError(reportError(requestError));
      if (requestError instanceof ApiError && requestError.status === 404) clearRevokedReport();
    } finally {
      setIsDeleting(false);
    }
  }

  if (!session) return <main className="private-loading">Validando sesión…</main>;
  return <main className="report-detail-page">
    <header className="reports-header"><div><p className="eyebrow">Reporte inmutable</p><h1>{report?.title ?? "Cargando reporte"}</h1><p>La vista y el PDF usan el mismo snapshot persistido; no se consultan métricas actuales para reconstruirlo.</p></div><Link className="secondary-button" href={`/app/clientes/${clientId}/reportes`}>Volver a reportes</Link></header>
    {error && <div className="notice notice-error" role="alert">{error}</div>}
    {isLoading ? <div className="structure-loading">Cargando snapshot del reporte…</div> : report && <>
      <section className="report-metadata" aria-label="Integridad del reporte"><div><span>Creado</span><strong>{formatTimestamp(report.createdAtUtc)}</strong></div><div><span>Esquema</span><strong>Versión {report.schemaVersion}</strong></div><div><span>Hash del snapshot</span><code>{report.snapshotHash}</code></div></section>
      <div className="report-actions"><button className="secondary-button" type="button" onClick={() => void downloadPdf()} disabled={isDownloading}>{isDownloading ? "Preparando PDF…" : "Descargar PDF"}</button>{canDeleteReports(session.role) && <button className="text-button dangerous-text" type="button" onClick={() => void deleteReport()} disabled={isDeleting}>{isDeleting ? "Eliminando…" : "Eliminar reporte"}</button>}</div>
      <SnapshotChart analysis={report.analysis} />
      {canManageReportShares(session.role) && <ReportShareLinks reportId={report.reportId} session={session} onReportUnavailable={clearRevokedReport} />}
      <section className="report-snapshot" aria-label="Snapshot persistido del análisis"><AnalysisResults result={report.analysis} /></section>
    </>}
  </main>;
}
