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
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

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

  function signOut() {
    clearSession();
    router.replace("/login");
  }

  const navigation = items.map(([href, label]) => (
    <Link
      className={pathname === href || (href !== "/app" && pathname.startsWith(`${href}/`)) ? "is-active" : ""}
      href={href}
      key={href}
      onClick={() => setIsMobileMenuOpen(false)}
    >
      {label}
    </Link>
  ));

  return <div className="private-shell">
    <aside className="app-sidebar">
      <Link className="shell-brand" href="/app"><span className="brand-mark">A</span><span>Analiti<span>Ads</span></span></Link>
      <nav aria-label="Navegación principal">{navigation}</nav>
      <div className="sidebar-footer">
        <span>{session.email}</span><small>{session.agencyName} · {session.role}</small>
        {canManageClientAccess(session.role) && <Link href="/app/clientes">Accesos de clientes</Link>}
        <button type="button" onClick={toggleTheme}>Tema {theme === "dark" ? "claro" : "oscuro"}</button>
        <button type="button" onClick={signOut}>Cerrar sesión</button>
      </div>
    </aside>
    <header className="shell-mobile-bar">
      <Link className="shell-brand" href="/app"><span className="brand-mark">A</span><span>Analiti<span>Ads</span></span></Link>
      <button type="button" aria-expanded={isMobileMenuOpen} aria-controls="mobile-navigation" onClick={() => setIsMobileMenuOpen((current) => !current)}>Menú</button>
    </header>
    {isMobileMenuOpen && <div className="shell-mobile-menu" id="mobile-navigation">
      <nav aria-label="Navegación principal">{navigation}</nav>
      <div className="shell-mobile-session"><span>{session.email}</span><small>{session.agencyName} · {session.role}</small></div>
      <div className="shell-mobile-actions">
        <button type="button" onClick={toggleTheme}>Tema {theme === "dark" ? "claro" : "oscuro"}</button>
        <button type="button" onClick={signOut}>Cerrar sesión</button>
      </div>
    </div>}
    <div className="private-shell-content">{children}</div>
  </div>;
}
