"use client";

import { Suspense, useEffect, useState } from "react";
import Link from "next/link";
import { getSupabaseClient } from "@/lib/supabase";
import { AuthShell, Field } from "@/components/editorial";

export default function ResetPasswordPage() {
  return (
    <Suspense
      fallback={
        <div className="mx-auto mt-16 text-center">
          <span className="editorial-rubric text-muted">Loading</span>
        </div>
      }
    >
      <ResetForm />
    </Suspense>
  );
}

function ResetForm() {
  const [mode, setMode] = useState<"verifying" | "new-password" | "expired">("verifying");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [fieldErrors, setFieldErrors] = useState<{ password?: string; confirmPassword?: string }>({});
  const [loading, setLoading] = useState(false);
  const [reset, setReset] = useState(false);

  useEffect(() => {
    const query = new URLSearchParams(window.location.search);
    const hash = new URLSearchParams(window.location.hash.substring(1));
    const tokenHash = query.get("token_hash") ?? hash.get("token_hash");
    const type = (query.get("type") ?? hash.get("type")) as "recovery" | null;
    const hasAccessToken = hash.has("access_token");

    if (!tokenHash && !type && !hasAccessToken) {
      const id = window.setTimeout(() => setMode("expired"), 0);
      return () => window.clearTimeout(id);
    }

    let cancelled = false;

    if (tokenHash && type) {
      getSupabaseClient().auth
        .verifyOtp({ token_hash: tokenHash, type })
        .then(({ error }) => {
          if (cancelled) return;
          if (error) {
            setMode("expired");
          } else {
            setMode("new-password");
          }
        });
      return () => {
        cancelled = true;
      };
    }

    const timeout = window.setTimeout(() => {
      if (cancelled) return;
      sub.subscription.unsubscribe();
      setMode("expired");
    }, 15000);
    const { data: sub } = getSupabaseClient().auth.onAuthStateChange((_event, session) => {
      if (cancelled || !session) return;
      window.clearTimeout(timeout);
      sub.subscription.unsubscribe();
      setMode("new-password");
    });
    getSupabaseClient().auth.getSession().then(({ data }) => {
      if (cancelled || !data.session) return;
      window.clearTimeout(timeout);
      sub.subscription.unsubscribe();
      setMode("new-password");
    });
    return () => {
      cancelled = true;
      window.clearTimeout(timeout);
      sub.subscription.unsubscribe();
    };
  }, []);

  const validate = (): boolean => {
    const errors: { password?: string; confirmPassword?: string } = {};
    if (!password) errors.password = "Password is required.";
    else if (password.length < 8) errors.password = "Password must be at least 8 characters.";
    if (!confirmPassword) errors.confirmPassword = "Please confirm your password.";
    else if (password !== confirmPassword) errors.confirmPassword = "Passwords do not match.";
    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleReset = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;

    setLoading(true);

    try {
      const { error } = await getSupabaseClient().auth.updateUser({ password });
      if (error) throw new Error(error.message);
      await getSupabaseClient().auth.signOut();
      setReset(true);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to reset password";
      setFieldErrors({ password: message });
    } finally {
      setLoading(false);
    }
  };

  if (reset) {
    return (
      <AuthShell
        kicker="Account recovery"
        title="Password reset"
        lede="Your password has been reset successfully."
      >
        <Link href="/login" className="font-medium text-accent hover:underline">
          Sign in with your new password
        </Link>
      </AuthShell>
    );
  }

  if (mode === "expired") {
    return (
      <AuthShell
        kicker="Account recovery"
        title="Reset link expired"
        lede="This password reset link is invalid or expired. Request a new one."
      >
        <Link href="/forgot-password" className="font-medium text-accent hover:underline">
          Send a new reset link
        </Link>
      </AuthShell>
    );
  }

  return (
    <AuthShell
      kicker="Account recovery"
      title="Reset your password"
      lede="Choose a new password for your account."
      footer={
        <Link href="/login" className="font-medium text-accent hover:underline">
          Back to sign in
        </Link>
      }
    >
      <form onSubmit={handleReset} className="flex flex-col gap-6" noValidate>
        <Field label="New Password" error={fieldErrors.password}>
          <input
            type="password"
            value={password}
            aria-invalid={Boolean(fieldErrors.password)}
            onChange={(e) => { setPassword(e.target.value); setFieldErrors((prev) => ({ ...prev, password: undefined })); }}
            className="editorial-input"
            placeholder="Min. 8 characters"
          />
        </Field>

        <Field label="Confirm Password" error={fieldErrors.confirmPassword}>
          <input
            type="password"
            value={confirmPassword}
            aria-invalid={Boolean(fieldErrors.confirmPassword)}
            onChange={(e) => { setConfirmPassword(e.target.value); setFieldErrors((prev) => ({ ...prev, confirmPassword: undefined })); }}
            className="editorial-input"
            placeholder="Repeat your password"
          />
        </Field>

        <button
          type="submit"
          className="editorial-btn w-full"
          disabled={loading || mode === "verifying"}
        >
          {loading ? "Resetting…" : mode === "verifying" ? "Verifying link…" : "Reset password"}
        </button>
      </form>
    </AuthShell>
  );
}
