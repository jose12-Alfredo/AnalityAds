import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "AnalitiAds",
  description: "Gestiona clientes, cuentas publicitarias y rendimiento de Meta Ads de forma segura.",
  applicationName: "AnalitiAds",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="es">
      <body>{children}</body>
    </html>
  );
}
