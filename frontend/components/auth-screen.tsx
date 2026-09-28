"use client";

import Link from "next/link";
import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";
import { ApiError, publicApiRequest } from "@/lib/api";
import { saveSession, type AuthSession } from "@/lib/auth-session";

type AuthMode = "login" | "register";

function errorMessage(error: unknown) {
  if (error instanceof ApiError) {
    if (error.status === 401) return "El correo o la contraseña no son correctos.";
    if (error.status === 400) return error.problem.detail || "Revisa los datos del formulario.";
    if (error.status === 409) return error.problem.detail || "Ese correo o agencia ya existe.";
    return error.problem.detail || "No pudimos completar la solicitud.";
  }
  return error instanceof Error ? error.message : "No pudimos completar la solicitud.";
}

export function AuthScreen({ mode }: { mode: AuthMode }) {
  const router = useRouter();
  const [agencyName, setAgencyName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const isRegister = mode === "register";

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsSubmitting(true);
    setError(null);

    try {
      const session = await publicApiRequest<AuthSession>(
        isRegister ? "/api/v1/auth/register" : "/api/v1/auth/login",
        {
          method: "POST",
          body: JSON.stringify(isRegister ? { agencyName, email, password } : { email, password, agencyId: null }),
        },
      );
      saveSession(session);
      router.replace("/app");
    } catch (requestError) {
      setError(errorMessage(requestError));
      setIsSubmitting(false);
    }
  }

  return (
    <main className="auth-page">
      <section className="auth-intro">
        <Link className="brand-lockup" href="/login" aria-label="AnalitiAds"><span className="brand-mark">A</span><span>Analiti<span>Ads</span></span></Link>
        <p className="eyebrow">Área segura de agencia</p>
        <h1>{isRegister ? "Crea tu espacio de trabajo." : "Vuelve a tu operación."}</h1>
        <p>AnalitiAds conecta la estructura de Meta y conserva las credenciales sensibles exclusivamente en el backend.</p>
      </section>
      <section className="auth-card" aria-labelledby="auth-title">
        <p className="eyebrow">{isRegister ? "Registro" : "Inicio de sesión"}</p>
        <h2 id="auth-title">{isRegister ? "Registra tu agencia" : "Inicia sesión"}</h2>
        <p className="auth-helper">{isRegister ? "Crea la agencia y el usuario propietario en un solo paso." : "Usa las credenciales de tu agencia para continuar."}</p>
        {error && <div className="notice notice-error" role="alert">{error}</div>}
        <form className="auth-form" onSubmit={(event) => void submit(event)}>
          {isRegister && <label>Nombre de la agencia<input value={agencyName} onChange={(event) => setAgencyName(event.target.value)} required maxLength={200} autoComplete="organization" /></label>}
          <label>Correo electrónico<input type="email" value={email} onChange={(event) => setEmail(event.target.value)} required autoComplete="email" /></label>
          <label>Contraseña<input type="password" value={password} onChange={(event) => setPassword(event.target.value)} required minLength={12} autoComplete={isRegister ? "new-password" : "current-password"} /></label>
          {isRegister && <p className="password-hint">Mínimo 12 caracteres, con mayúscula, minúscula y número.</p>}
          <button className="primary-button" type="submit" disabled={isSubmitting}>{isSubmitting ? "Validando…" : isRegister ? "Crear agencia" : "Iniciar sesión"}</button>
        </form>
        <p className="auth-switch">{isRegister ? "¿Ya tienes acceso?" : "¿Aún no tienes una agencia?"} <Link href={isRegister ? "/login" : "/registro"}>{isRegister ? "Inicia sesión" : "Regístrate"}</Link></p>
      </section>
    </main>
  );
}
