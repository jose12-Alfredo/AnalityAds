"use client";

import { useEffect, useState, type CSSProperties } from "react";
import { DataVisualization, type ComponentQueryState } from "@/components/dashboard-visualizations";
import { sharingApi, type SharedDashboard } from "@/lib/dashboard-sharing";
import type { DashboardQueryResult } from "@/lib/dashboard-data";

export function SharedDashboardView() {
  const [token, setToken] = useState("");
  const [data, setData] = useState<SharedDashboard | null>(null);
  const [results, setResults] = useState<Record<string, ComponentQueryState>>({});
  const [password, setPassword] = useState("");
  const [email, setEmail] = useState("");
  const [error, setError] = useState("");
  useEffect(() => { setTimeout(() => setToken(location.hash.slice(1)), 0); }, []);

  async function open() {
    try {
      const shared = await sharingApi.access(token, password, email, new URLSearchParams(location.search).get("embed") === "1");
      setData(shared); setError("");
      for (const page of shared.definition.pages) for (const item of page.components) {
        const source = item.data.sources[0]?.dataSourceId;
        const config = item.data.configuration;
        const metrics = Array.isArray(config?.metrics) ? config.metrics.filter((value): value is string => typeof value === "string") : [config?.metric].filter((value): value is string => typeof value === "string");
        if (!source || !metrics.length || !config?.since || !config?.until) continue;
        setResults((current) => ({ ...current, [item.id]: { status: "loading" } }));
        const dimension = item.type === "kpiCard" || item.type === "goalGauge" ? "none" : String(config.dimension || "none");
        void sharingApi.query({ accessToken: token, password: password || null, recipientEmail: email || null, dataSourceId: source, since: config.since, until: config.until, dimension, metrics })
          .then((result) => setResults((current) => ({ ...current, [item.id]: { status: result.rows.length ? "ready" : "empty", result: { ...result, clientId: "public", dataSourceId: source, dimension, since: String(config.since), until: String(config.until), lastSyncedAtUtc: null, coverage: { requestedDays: 0, snapshotDays: 0, firstSnapshotDate: null, lastSnapshotDate: null }, rows: result.rows.map((row) => ({ ...row, dimensionValue: row.key })) } as DashboardQueryResult } })))
          .catch(() => setResults((current) => ({ ...current, [item.id]: { status: "error", error: "No disponible" } })));
      }
    } catch (caught) { setError(caught instanceof Error ? caught.message : "Enlace inválido."); }
  }

  if (!data) return <main className="shared-report-page"><section className="shared-report-state"><span className="brand-mark">A</span><h1>Dashboard protegido</h1><input type="email" placeholder="Correo autorizado, si corresponde" value={email} onChange={(event) => setEmail(event.target.value)} /><input type="password" placeholder="Contraseña, si corresponde" value={password} onChange={(event) => setPassword(event.target.value)} /><button className="primary-button" disabled={!token} onClick={() => void open()}>Abrir dashboard</button>{error && <p>{error}</p>}</section></main>;
  return <main className="shared-dashboard" style={{ background: data.branding.backgroundColor, fontFamily: data.branding.fontFamily, "--shared-primary": data.branding.primaryColor } as CSSProperties}><header>{data.branding.logoUrl && <span role="img" aria-label="Logo" style={{ display: "block", width: 160, height: 48, background: `center / contain no-repeat url(${JSON.stringify(data.branding.logoUrl)})` }} />}<p className="eyebrow">Dashboard publicado · versión {data.publicationNumber}</p><h1>{data.title}</h1><p>{data.description}</p></header>{data.definition.pages.map((page) => <section className="shared-dashboard-page" key={page.id} style={{ aspectRatio: `${page.width}/${page.height}`, background: String(page.background || "#fff") }}><h2>{page.name}</h2>{page.components.map((item) => <div className={`shared-dashboard-component component-${item.type}`} key={item.id} style={{ left: `${item.x / page.width * 100}%`, top: `${item.y / page.height * 100}%`, width: `${item.width / page.width * 100}%`, height: `${item.height / page.height * 100}%`, zIndex: item.zIndex, background: String(item.style?.background || "#fff"), color: String(item.style?.color || "#17202a") }}><strong>{String(item.data.configuration?.title || item.data.configuration?.text || item.type)}</strong>{!(["text", "shape", "divider"] as string[]).includes(item.type) && <DataVisualization component={item} state={results[item.id]} catalog={null} />}</div>)}</section>)}</main>;
}
