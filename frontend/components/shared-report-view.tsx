"use client";

import { useEffect, useRef, useState } from "react";
import { ApiError, publicApiBlobRequest, publicApiRequest } from "@/lib/api";
import type { SharedReportData } from "@/lib/reports";
import { AnalysisResults } from "@/components/analysis-engine-panel";
import { SnapshotChart } from "@/components/snapshot-chart";

type PublicState = "loading" | "ready" | "unavailable" | "rate-limited" | "error";

function publicFailure(error: unknown): PublicState {
  if (error instanceof ApiError && (error.status === 400 || error.status === 404)) return "unavailable";
  if (error instanceof ApiError && error.status === 429) return "rate-limited";
  return "error";
}

export function SharedReportView() {
  const token = useRef<string | null>(null);
  const [report, setReport] = useState<SharedReportData | null>(null);
  const [state, setState] = useState<PublicState>("loading");
  const [isDownloading, setIsDownloading] = useState(false);

  useEffect(() => {
    let cancelled = false;
    const accessToken = window.location.hash.startsWith("#") ? window.location.hash.slice(1) : "";
    window.history.replaceState(null, "", "/reportes-compartidos");
    if (!accessToken) {
      queueMicrotask(() => { if (!cancelled) setState("unavailable"); });
      return () => { cancelled = true; };
    }
    token.current = accessToken;
    void publicApiRequest<SharedReportData>("/api/v1/shared-reports/access", { method: "POST", body: JSON.stringify({ accessToken }) })
      .then((data) => { if (!cancelled) { setReport(data); setState("ready"); } })
      .catch((error: unknown) => { if (!cancelled) { setReport(null); setState(publicFailure(error)); } });
    return () => { cancelled = true; token.current = null; };
  }, []);

  async function downloadPdf() {
    if (!token.current || isDownloading) return;
    setIsDownloading(true);
    let objectUrl: string | null = null;
    try {
      const result = await publicApiBlobRequest("/api/v1/shared-reports/pdf", { method: "POST", body: JSON.stringify({ accessToken: token.current }) });
      objectUrl = URL.createObjectURL(result.blob);
      const link = document.createElement("a");
      link.href = objectUrl;
      link.download = result.filename || "analitiads-reporte-compartido.pdf";
      document.body.appendChild(link);
      link.click();
      link.remove();
    } catch (error) {
      const nextState = publicFailure(error);
      if (nextState === "unavailable") { token.current = null; setReport(null); }
      setState(nextState);
    } finally {
      if (objectUrl) URL.revokeObjectURL(objectUrl);
      setIsDownloading(false);
    }
  }

  if (state === "loading") return <main className="shared-report-page"><div className="shared-report-state"><span className="brand-mark">A</span><h1>Abriendo reporte compartido</h1><p>Validando el enlace de acceso…</p></div></main>;
  if (state !== "ready" || !report) return <main className="shared-report-page"><div className="shared-report-state"><span className="brand-mark">A</span><h1>{state === "unavailable" ? "Este enlace no está disponible" : state === "rate-limited" ? "Demasiados intentos" : "No pudimos abrir el reporte"}</h1><p>{state === "unavailable" ? "Puede haber vencido, sido revocado o dejado de existir." : state === "rate-limited" ? "Espera un momento y vuelve a abrir el enlace." : "Intenta nuevamente más tarde."}</p></div></main>;

  return <main className="shared-report-page"><header className="shared-report-header"><div><div className="shared-brand"><span className="brand-mark">A</span><strong>AnalitiAds</strong></div><p className="eyebrow">Reporte compartido</p><h1>{report.title}</h1><p>Snapshot creado {new Intl.DateTimeFormat("es-BO", { dateStyle: "long", timeStyle: "short" }).format(new Date(report.createdAtUtc))}</p></div><button className="primary-button" type="button" onClick={() => void downloadPdf()} disabled={isDownloading}>{isDownloading ? "Preparando PDF…" : "Descargar PDF"}</button></header><div className="shared-report-content"><SnapshotChart analysis={report.analysis} /><section className="report-snapshot" aria-label="Snapshot compartido"><AnalysisResults result={report.analysis} /></section></div></main>;
}
