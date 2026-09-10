"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useAuth } from "@/lib/auth-context";
import { AuthShell, Field } from "@/components/editorial";
import {
  hasAuthFieldErrors,
  normalizeEmail,
  validateSignInFields,
  type AuthFieldErrors,
} from "@/lib/auth-validation";

export default function LoginPage() {
  const router = useRouter();
  const { signIn, showNotification } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [fieldErrors, setFieldErrors] = useState<AuthFieldErrors>({});
  const [loading, setLoading] = useState(false);

  const validate = (): boolean => {
    const errors = validateSignInFields(email, password);
    setFieldErrors(errors);
    return !hasAuthFieldErrors(errors);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;

    setLoading(true);

    try {
      const normalizedEmail = normalizeEmail(email);
      await signIn(normalizedEmail, password);
      showNotification({ status: "success", title: "Welcome back", message: "You have been signed in successfully." });
      router.push("/");
    } catch (err) {
      const message = err instanceof Error ? err.message : "Login failed";
      if (message.toLowerCase().includes("verify your email")) {
        router.push(`/verify?email=${encodeURIComponent(email)}`);
        return;
      }
      showNotification({ status: "danger", title: "Sign in failed", message });
    } finally {
      setLoading(false);
    }
  };

  return (
    <AuthShell
      kicker="Members"
      title="Sign In"
      lede="Welcome back to Arunika."
      footer={
        <>
          Don&apos;t have an account?{" "}
          <Link href="/register" className="font-medium text-accent hover:underline">
            Create one
          </Link>
          .
        </>
      }
    >
      <form onSubmit={handleSubmit} className="flex flex-col gap-6" noValidate>
        <Field label="Email" error={fieldErrors.email}>
          <input
            type="email"
            value={email}
            aria-invalid={Boolean(fieldErrors.email)}
            onChange={(e) => { setEmail(e.target.value); setFieldErrors((prev) => ({ ...prev, email: undefined })); }}
            onBlur={() => setFieldErrors((prev) => ({ ...prev, email: validateSignInFields(email, password).email }))}
            className="editorial-input"
            placeholder="you@example.com"
          />
        </Field>

        <Field label="Password" error={fieldErrors.password}>
          <input
            type="password"
            value={password}
            aria-invalid={Boolean(fieldErrors.password)}
            onChange={(e) => { setPassword(e.target.value); setFieldErrors((prev) => ({ ...prev, password: undefined })); }}
            onBlur={() => setFieldErrors((prev) => ({ ...prev, password: validateSignInFields(email, password).password }))}
            className="editorial-input"
            placeholder="Your password"
          />
        </Field>

        <div className="flex justify-end">
          <Link
            href="/forgot-password"
            className="editorial-rubric text-muted transition-colors hover:text-foreground"
          >
            Forgot password?
          </Link>
        </div>

        <button type="submit" className="editorial-btn w-full" disabled={loading}>
          {loading ? "Signing in…" : "Sign In"}
        </button>
      </form>
    </AuthShell>
  );
}
