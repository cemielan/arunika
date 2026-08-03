"use client";

import { Suspense, useEffect, useState } from "react";
import Link from "next/link";
import { Button, Card, ErrorMessage, Typography } from "@heroui/react";
import { getSupabaseClient } from "@/lib/supabase";

export default function ResetPasswordPage() {
  return (
    <Suspense fallback={<div className="mx-auto mt-16 text-center text-muted">Loading...</div>}>
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
      <div className="mx-auto mt-8 flex max-w-sm flex-col gap-6 px-4 sm:mt-16">
        <div className="text-center">
          <Typography.Heading level={1} className="text-2xl">Password reset</Typography.Heading>
          <Typography.Paragraph color="muted" className="mt-1">
            Your password has been reset successfully.
          </Typography.Paragraph>
        </div>
        <Card variant="default" className="p-6 text-center">
          <Link href="/login" className="font-medium text-accent hover:underline">
            Sign in with your new password
          </Link>
        </Card>
      </div>
    );
  }

  if (mode === "expired") {
    return (
      <div className="mx-auto mt-8 flex max-w-sm flex-col gap-6 px-4 sm:mt-16">
        <div className="text-center">
          <Typography.Heading level={1} className="text-2xl">Reset link expired</Typography.Heading>
          <Typography.Paragraph color="muted" className="mt-1">
            This password reset link is invalid or expired. Request a new one.
          </Typography.Paragraph>
        </div>
        <Card variant="default" className="p-6 text-center">
          <Link href="/forgot-password" className="font-medium text-accent hover:underline">
            Send a new reset link
          </Link>
        </Card>
      </div>
    );
  }

  return (
    <div className="mx-auto mt-8 flex max-w-sm flex-col gap-6 px-4 sm:mt-16">
      <div className="text-center">
        <Typography.Heading level={1} className="text-2xl">Reset your password</Typography.Heading>
        <Typography.Paragraph color="muted" className="mt-1">
          Choose a new password for your account.
        </Typography.Paragraph>
      </div>

      <Card variant="default" className="p-6">
        <form onSubmit={handleReset} className="flex flex-col gap-4" noValidate>
          <div className="flex flex-col gap-1">
            <Typography.Paragraph size="sm" weight="medium">New Password</Typography.Paragraph>
            <input
              type="password"
              value={password}
              onChange={(e) => { setPassword(e.target.value); setFieldErrors((prev) => ({ ...prev, password: undefined })); }}
              className={`rounded-lg border bg-background px-3 py-2 text-sm outline-none focus:border-accent ${fieldErrors.password ? "border-danger" : "border-border"}`}
              placeholder="Min. 8 characters"
            />
            {fieldErrors.password && <ErrorMessage>{fieldErrors.password}</ErrorMessage>}
          </div>

          <div className="flex flex-col gap-1">
            <Typography.Paragraph size="sm" weight="medium">Confirm Password</Typography.Paragraph>
            <input
              type="password"
              value={confirmPassword}
              onChange={(e) => { setConfirmPassword(e.target.value); setFieldErrors((prev) => ({ ...prev, confirmPassword: undefined })); }}
              className={`rounded-lg border bg-background px-3 py-2 text-sm outline-none focus:border-accent ${fieldErrors.confirmPassword ? "border-danger" : "border-border"}`}
              placeholder="Repeat your password"
            />
            {fieldErrors.confirmPassword && <ErrorMessage>{fieldErrors.confirmPassword}</ErrorMessage>}
          </div>

          <Button type="submit" variant="primary" isDisabled={loading || mode === "verifying"}>
            {loading ? "Resetting..." : mode === "verifying" ? "Verifying link..." : "Reset password"}
          </Button>
        </form>
      </Card>

      <Typography.Paragraph size="sm" color="muted" className="text-center">
        <Link href="/login" className="font-medium text-accent hover:underline">
          Back to sign in
        </Link>
      </Typography.Paragraph>
    </div>
  );
}
