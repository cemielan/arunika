"use client";

import { useState } from "react";
import Link from "next/link";
import { getSupabaseClient } from "@/lib/supabase";
import { siteUrl } from "@/lib/api";
import { AuthShell, Field } from "@/components/editorial";

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState("");
  const [emailError, setEmailError] = useState<string | undefined>();
  const [loading, setLoading] = useState(false);
  const [sent, setSent] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!email.trim()) {
      setEmailError("Email is required.");
      return;
    }

    setLoading(true);
    setEmailError(undefined);

    try {
      await getSupabaseClient().auth.resetPasswordForEmail(email.trim(), {
        redirectTo: `${siteUrl()}/reset-password`,
      });
      setSent(true);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to send reset email";
      setEmailError(message);
    } finally {
      setLoading(false);
    }
  };

  if (sent) {
    return (
      <AuthShell
        kicker="Account recovery"
        title="Check your email"
        lede={
          <>
            If an account exists for <strong className="text-foreground">{email}</strong>, you will
            receive a password reset link shortly.
          </>
        }
        footer={
          <Link href="/login" className="font-medium text-accent hover:underline">
            Back to sign in
          </Link>
        }
      >
        <p className="text-sm leading-relaxed text-muted">
          Didn&apos;t receive it? Check your spam folder, then{" "}
          <button onClick={() => setSent(false)} className="font-medium text-accent hover:underline">
            try again
          </button>
          .
        </p>
      </AuthShell>
    );
  }

  return (
    <AuthShell
      kicker="Account recovery"
      title="Forgot password"
      lede="Enter your email and we'll send you a reset link."
      footer={
        <>
          Remember your password?{" "}
          <Link href="/login" className="font-medium text-accent hover:underline">
            Sign in
          </Link>
          .
        </>
      }
    >
      <form onSubmit={handleSubmit} className="flex flex-col gap-6" noValidate>
        <Field label="Email" error={emailError}>
          <input
            type="email"
            value={email}
            aria-invalid={Boolean(emailError)}
            onChange={(e) => { setEmail(e.target.value); setEmailError(undefined); }}
            className="editorial-input"
            placeholder="you@example.com"
          />
        </Field>

        <button type="submit" className="editorial-btn w-full" disabled={loading}>
          {loading ? "Sending…" : "Send reset link"}
        </button>
      </form>
    </AuthShell>
  );
}
