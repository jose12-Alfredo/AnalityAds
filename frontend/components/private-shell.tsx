"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { canEditClients, canManageClientAccess, clearSession, readSession } from "@/lib/auth-session";

const themeKey = "analitiads.ui.theme";

export function PrivateShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const [session] = useState(() => readSession());
  const [theme, setTheme] = useState<"dark" | "light">(() => typeof window !== "undefined" && window.sessionStorage.getItem(themeKey) === "light" ? "light" : "dark");

  useEffect(() => {
    document.documentElement.dataset.theme = theme;
  }, [theme]);

  if (!session) return <>{children}</>;
  const items = [
    ["/app", "Resumen"], ["/app/clientes", "Clientes"], ["/app/informes", "Informes"], ["/app/configuracion/marca", "Marca"], ["/app/configuracion/integraciones/meta", "Meta"], ["/app/configuracion/integraciones/google-ads", "Google Ads"], ["/app/configuracion/integraciones/tiktok-ads", "TikTok Ads"], ["/app/configuracion/integraciones/ga4", "Analytics 4"], ["/app/configuracion/mcp", "MCP"],
  ].filter(([href]) => href !== "/app/clientes" || canEditClients(session.role) || session.role === "Viewer")
    .filter(([href]) => href !== "/app/informes" || session.role !== "ClientViewer")
    .filter(([href]) => href !== "/app/configuracion/integraciones/meta" || session.role !== "ClientViewer")
    .filter(([href]) => !href.startsWith("/app/configuracion/integraciones/google") || session.role !== "ClientViewer")
    .filter(([href]) => href !== "/app/configuracion/integraciones/ga4" || session.role !== "ClientViewer")
    .filter(([href]) => href !== "/app/configuracion/integraciones/tiktok-ads" || session.role !== "ClientViewer")
    .filter(([href]) => href !== "/app/configuracion/mcp" || session.role !== "ClientViewer");

  function toggleTheme() {
    const nextTheme = theme === "dark" ? "light" : "dark";
    setTheme(nextTheme);
    window.sessionStorage.setItem(themeKey, nextTheme);
    document.documentElement.dataset.theme = nextTheme;
  }

  return <div className="private-shell"><aside className="app-sidebar"><Link className="shell-brand" href="/app"><span className="brand-mark">A</span><span>Analiti<span>Ads</span></span></Link><nav aria-label="Navegación principal">{items.map(([href, label]) => <Link className={pathname === href || (href !== "/app" && pathname.startsWith(`${href}/`)) ? "is-active" : ""} href={href} key={href}>{label}</Link>)}</nav><div className="sidebar-footer"><span>{session.email}</span><small>{session.agencyName} · {session.role}</small>{canManageClientAccess(session.role) && <Link href="/app/clientes">Accesos de clientes</Link>}<button type="button" onClick={toggleTheme}>Tema {theme === "dark" ? "claro" : "oscuro"}</button><button type="button" onClick={() => { clearSession(); router.replace("/login"); }}>Cerrar sesión</button></div></aside><div className="shell-mobile-bar"><Link className="shell-brand" href="/app"><span className="brand-mark">A</span><span>Analiti<span>Ads</span></span></Link><button type="button" onClick={toggleTheme}>Tema</button></div><div className="private-shell-content">{children}</div></div>;
}
