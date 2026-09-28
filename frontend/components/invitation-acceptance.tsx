"use client";

import Link from "next/link";
import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";
import { ApiError, publicApiRequest } from "@/lib/api";
import { saveSession, type AuthSession } from "@/lib/auth-session";

function acceptanceError(error: unknown) {
  if (!(error instanceof ApiError)) return error instanceof Error ? error.message : "No pudimos aceptar la invitación.";
  if (error.status === 401) return "La contraseña no coincide con la cuenta existente. Vuelve a intentarlo.";
  if (error.status === 409) return error.problem.detail || "Esta cuenta no puede usar una invitación externa para esta agencia.";
  if (error.status === 429) return `Se alcanzó el límite de intentos. Espera ${error.retryAfter ?? "60"} segundos antes de volver a intentarlo.`;
  if (error.status === 400) return error.problem.detail || "Revisa el código, el correo y la contraseña; la invitación puede haber vencido o ya no estar disponible.";
  return error.problem.detail || "No pudimos aceptar la invitación. Intenta nuevamente.";
}

export function InvitationAcceptance() {
  const router = useRouter();
  const [invitationToken, setInvitationToken] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      const session = await publicApiRequest<AuthSession>("/api/v1/auth/client-invitations/accept", {
        method: "POST",
        body: JSON.stringify({ invitationToken: invitationToken.trim(), email: email.trim(), password }),
      });
      saveSession(session);
      setInvitationToken("");
      setPassword("");
      router.replace("/app");
    } catch (requestError) {
      setError(acceptanceError(requestError));
      setPassword("");
      setIsSubmitting(false);
    }
  }

  return (
    <main className="auth-page invitation-page">
      <section className="auth-intro">
        <Link className="brand-lockup" href="/login" aria-label="AnalitiAds"><span className="brand-mark">A</span><span>Analiti<span>Ads</span></span></Link>
        <p className="eyebrow">Acceso de cliente</p>
        <h1>Entra al espacio que tu agencia preparó.</h1>
        <p>Tu acceso se limita a los clientes que te hayan asignado. El código de invitación solo se usa para activar este acceso.</p>
      </section>
      <section className="auth-card" aria-labelledby="acceptance-title">
        <p className="eyebrow">Invitación</p>
        <h2 id="acceptance-title">Aceptar invitación</h2>
        <p className="auth-helper">Si ya tienes cuenta, escribe tu contraseña actual. Si eres nuevo, crea una contraseña de al menos 12 caracteres con mayúscula, minúscula y número.</p>
        {error && <div className="notice notice-error" role="alert">{error}</div>}
        <form className="auth-form" onSubmit={(event) => void submit(event)}>
          <label>Código de invitación<input value={invitationToken} onChange={(event) => setInvitationToken(event.target.value)} required minLength={43} maxLength={128} autoComplete="one-time-code" spellCheck="false" /></label>
          <label>Correo electrónico<input type="email" value={email} onChange={(event) => setEmail(event.target.value)} required autoComplete="email" /></label>
          <label>Contraseña<input type="password" value={password} onChange={(event) => setPassword(event.target.value)} required minLength={12} maxLength={128} autoComplete="current-password" /></label>
          <button className="primary-button" type="submit" disabled={isSubmitting}>{isSubmitting ? "Activando acceso…" : "Aceptar invitación"}</button>
        </form>
        <p className="auth-switch">¿Ya activaste tu acceso? <Link href="/login">Inicia sesión</Link></p>
      </section>
    </main>
  );
}
