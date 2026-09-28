"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { apiRequest } from "@/lib/api";
import { clearSession, readSession, saveSession, type AuthSession } from "@/lib/auth-session";
import { PrivateShell } from "@/components/private-shell";

interface CurrentUser {
  userId: string;
  email: string;
  agencyId: string;
  agencyName: string;
  role: AuthSession["role"];
}

export default function PrivateLayout({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const [isChecking, setIsChecking] = useState(true);

  useEffect(() => {
    const handleSessionExpired = () => router.replace("/login");
    window.addEventListener("analitiads:session-expired", handleSessionExpired);
    const timer = window.setTimeout(() => {
      void (async () => {
        const session = readSession();
        if (!session) {
          router.replace("/login");
          return;
        }
        try {
          const currentUser = await apiRequest<CurrentUser>("/api/v1/auth/me", session.accessToken);
          saveSession({ ...session, ...currentUser });
          setIsChecking(false);
        } catch {
          clearSession();
          router.replace("/login");
        }
      })();
    }, 0);
    return () => {
      window.clearTimeout(timer);
      window.removeEventListener("analitiads:session-expired", handleSessionExpired);
    };
  }, [router]);

  if (isChecking) return <main className="private-loading">Validando sesión…</main>;
  return <PrivateShell>{children}</PrivateShell>;
}
