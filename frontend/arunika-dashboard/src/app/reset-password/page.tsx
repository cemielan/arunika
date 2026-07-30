"use client";

import { Suspense, useState } from "react";
import { useSearchParams } from "next/navigation";
import Link from "next/link";
import { Button, Card, ErrorMessage, Typography } from "@heroui/react";
import { resetPassword } from "@/lib/api";

export default function ResetPasswordPage() {
  return (
    <Suspense fallback={<div className="mx-auto mt-16 text-center text-muted">Loading...</div>}>
      <ResetForm />
    </Suspense>
  );
}

function ResetForm() {
  const searchParams = useSearchParams();
  const email = searchParams.get("email") ?? "";
  const token = searchParams.get("token") ?? "";

  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [fieldErrors, setFieldErrors] = useState<{ password?: string; confirmPassword?: string }>({});
  const [loading, setLoading] = useState(false);
  const [reset, setReset] = useState(false);

  const validate = (): boolean => {
    const errors: { password?: string; confirmPassword?: string } = {};
    if (!password) errors.password = "Password is required.";
    else if (password.length < 8) errors.password = "Password must be at least 8 characters.";
    if (!confirmPassword) errors.confirmPassword = "Please confirm your password.";
    else if (password !== confirmPassword) errors.confirmPassword = "Passwords do not match.";
    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;

    if (!email || !token) {
      setFieldErrors({ password: "Invalid reset link." });
      return;
    }

    setLoading(true);

    try {
      await resetPassword(email, token, password);
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

  return (
    <div className="mx-auto mt-8 flex max-w-sm flex-col gap-6 px-4 sm:mt-16">
      <div className="text-center">
        <Typography.Heading level={1} className="text-2xl">Reset your password</Typography.Heading>
        <Typography.Paragraph color="muted" className="mt-1">
          Enter your new password for <strong>{email}</strong>.
        </Typography.Paragraph>
      </div>

      <Card variant="default" className="p-6">
        <form onSubmit={handleSubmit} className="flex flex-col gap-4" noValidate>
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

          <Button type="submit" variant="primary" isDisabled={loading}>
            {loading ? "Resetting..." : "Reset password"}
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
