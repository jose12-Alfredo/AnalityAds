"use client";

import { useEffect, useState } from "react";
import { apiRequest } from "@/lib/api";
import { readSession } from "@/lib/auth-session";
import type { WorkspaceClient } from "@/lib/dashboards";

type Brand = { logoUrl:string|null; primaryColor:string; secondaryColor:string; backgroundColor:string; fontFamily:string };
const defaultBrand: Brand = { logoUrl:null, primaryColor:"#8054D8", secondaryColor:"#3C9CA0", backgroundColor:"#F5F6F8", fontFamily:"Arial" };

export function BrandingPanel() {
  const [session] = useState(() => readSession());
  const [clients, setClients] = useState<WorkspaceClient[]>([]);
  const [clientId, setClientId] = useState("");
  const [brand, setBrand] = useState<Brand>(defaultBrand);
  const [notice, setNotice] = useState("");
  const endpoint = clientId ? `/api/v1/branding/clients/${clientId}` : "/api/v1/branding/agency";
  useEffect(() => { if (session) void apiRequest<WorkspaceClient[]>("/api/v1/clients", session.accessToken).then(setClients); }, [session]);
  useEffect(() => { if (session) void apiRequest<Brand>(endpoint, session.accessToken).then(setBrand).catch(() => setBrand(defaultBrand)); }, [endpoint, session]);
  async function save() { if (!session) return; setBrand(await apiRequest<Brand>(endpoint, session.accessToken, { method:"PUT", body:JSON.stringify(brand) })); setNotice("Identidad visual guardada."); }
  return <main className="provider-page"><header className="provider-hero"><div><p className="eyebrow">Personalización</p><h1>Marca de informes</h1><p>Define la identidad general de la agencia o una marca específica que prevalece para un cliente.</p></div></header>{notice && <div className="notice notice-success">{notice}</div>}<section className="provider-card provider-catalog"><div className="association-toolbar"><label>Aplicar a<select value={clientId} onChange={(event) => { setClientId(event.target.value); setNotice(""); }}><option value="">Toda la agencia</option>{clients.map((client) => <option key={client.id} value={client.id}>{client.name}</option>)}</select></label><label>Logo HTTPS<input value={brand.logoUrl ?? ""} onChange={(event) => setBrand({ ...brand, logoUrl:event.target.value || null })} /></label><label>Tipografía<input value={brand.fontFamily} onChange={(event) => setBrand({ ...brand, fontFamily:event.target.value })} /></label><label>Color principal<input type="color" value={brand.primaryColor} onChange={(event) => setBrand({ ...brand, primaryColor:event.target.value })} /></label><label>Color secundario<input type="color" value={brand.secondaryColor} onChange={(event) => setBrand({ ...brand, secondaryColor:event.target.value })} /></label><label>Fondo<input type="color" value={brand.backgroundColor} onChange={(event) => setBrand({ ...brand, backgroundColor:event.target.value })} /></label></div><button className="primary-button" onClick={() => void save()}>Guardar marca</button></section></main>;
}
