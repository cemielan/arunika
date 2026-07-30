"use client";

import { useState } from "react";
import Link from "next/link";
import { Button, Card, ErrorMessage, Typography } from "@heroui/react";
import { forgotPassword } from "@/lib/api";

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
      await forgotPassword(email);
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
      <div className="mx-auto mt-8 flex max-w-sm flex-col gap-6 px-4 sm:mt-16">
        <div className="text-center">
          <Typography.Heading level={1} className="text-2xl">Check your email</Typography.Heading>
          <Typography.Paragraph color="muted" className="mt-1">
            If an account exists for <strong>{email}</strong>, you will receive a password reset link shortly.
          </Typography.Paragraph>
        </div>
        <Card variant="default" className="p-6 text-center">
          <Typography.Paragraph size="sm" color="muted">
            Didn&apos;t receive it?{" "}
            <button
              onClick={() => setSent(false)}
              className="font-medium text-accent hover:underline"
            >
              Try again
            </button>
          </Typography.Paragraph>
        </Card>
        <Typography.Paragraph size="sm" color="muted" className="text-center">
          <Link href="/login" className="font-medium text-accent hover:underline">
            Back to sign in
          </Link>
        </Typography.Paragraph>
      </div>
    );
  }

  return (
    <div className="mx-auto mt-8 flex max-w-sm flex-col gap-6 px-4 sm:mt-16">
      <div className="text-center">
        <Typography.Heading level={1} className="text-2xl">Forgot password</Typography.Heading>
        <Typography.Paragraph color="muted" className="mt-1">
          Enter your email and we&apos;ll send you a reset link.
        </Typography.Paragraph>
      </div>

      <Card variant="default" className="p-6">
        <form onSubmit={handleSubmit} className="flex flex-col gap-4" noValidate>
          <div className="flex flex-col gap-1">
            <Typography.Paragraph size="sm" weight="medium">Email</Typography.Paragraph>
            <input
              type="email"
              value={email}
              onChange={(e) => { setEmail(e.target.value); setEmailError(undefined); }}
              className={`rounded-lg border bg-background px-3 py-2 text-sm outline-none focus:border-accent ${emailError ? "border-danger" : "border-border"}`}
              placeholder="you@example.com"
            />
            {emailError && <ErrorMessage>{emailError}</ErrorMessage>}
          </div>

          <Button type="submit" variant="primary" isDisabled={loading}>
            {loading ? "Sending..." : "Send reset link"}
          </Button>
        </form>
      </Card>

      <Typography.Paragraph size="sm" color="muted" className="text-center">
        Remember your password?{" "}
        <Link href="/login" className="font-medium text-accent hover:underline">
          Sign in
        </Link>
      </Typography.Paragraph>
    </div>
  );
}
