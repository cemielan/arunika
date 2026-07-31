"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { Button, Card, ErrorMessage, Typography } from "@heroui/react";
import { useAuth } from "@/lib/auth-context";

type FieldErrors = {
  email?: string;
  password?: string;
  confirmPassword?: string;
};

export default function RegisterPage() {
  const router = useRouter();
  const { signUp, showNotification } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [loading, setLoading] = useState(false);

  const validate = (): boolean => {
    const errors: FieldErrors = {};
    if (!email.trim()) errors.email = "Email is required.";
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

    setLoading(true);

    try {
      await signUp(email, password);
      showNotification({ status: "success", title: "Account created", message: "Check your email for the verification code." });
      router.push(`/verify?email=${encodeURIComponent(email)}`);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Registration failed";
      if (message.includes("already exists")) {
        showNotification({ status: "success", title: "Account created", message: message });
        router.push(`/verify?email=${encodeURIComponent(email)}`);
        return;
      }
      showNotification({ status: "danger", title: "Sign up failed", message });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="mx-auto mt-8 flex max-w-sm flex-col gap-6 px-4 sm:mt-16">
      <div className="text-center">
        <Typography.Heading level={1} className="text-2xl">Create Account</Typography.Heading>
        <Typography.Paragraph color="muted" className="mt-1">
          Join Arunika for free.
        </Typography.Paragraph>
      </div>

      <Card variant="default" className="p-6">
        <form onSubmit={handleSubmit} className="flex flex-col gap-4" noValidate>
          <div className="flex flex-col gap-1">
            <Typography.Paragraph size="sm" weight="medium">Email</Typography.Paragraph>
            <input
              type="email"
              value={email}
              onChange={(e) => { setEmail(e.target.value); setFieldErrors((prev) => ({ ...prev, email: undefined })); }}
              onBlur={() => { if (!email.trim()) setFieldErrors((prev) => ({ ...prev, email: "Email is required." })); }}
              className={`rounded-lg border bg-background px-3 py-2 text-sm outline-none focus:border-accent ${fieldErrors.email ? "border-danger" : "border-border"}`}
              placeholder="you@example.com"
            />
            {fieldErrors.email && <ErrorMessage>{fieldErrors.email}</ErrorMessage>}
          </div>

          <div className="flex flex-col gap-1">
            <Typography.Paragraph size="sm" weight="medium">Password</Typography.Paragraph>
            <input
              type="password"
              value={password}
              onChange={(e) => { setPassword(e.target.value); setFieldErrors((prev) => ({ ...prev, password: undefined })); }}
              onBlur={() => {
                if (!password) setFieldErrors((prev) => ({ ...prev, password: "Password is required." }));
                else if (password.length < 8) setFieldErrors((prev) => ({ ...prev, password: "Password must be at least 8 characters." }));
              }}
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
              onBlur={() => {
                if (!confirmPassword) setFieldErrors((prev) => ({ ...prev, confirmPassword: "Please confirm your password." }));
                else if (password !== confirmPassword) setFieldErrors((prev) => ({ ...prev, confirmPassword: "Passwords do not match." }));
              }}
              className={`rounded-lg border bg-background px-3 py-2 text-sm outline-none focus:border-accent ${fieldErrors.confirmPassword ? "border-danger" : "border-border"}`}
              placeholder="Repeat your password"
            />
            {fieldErrors.confirmPassword && <ErrorMessage>{fieldErrors.confirmPassword}</ErrorMessage>}
          </div>

          <Button type="submit" variant="primary" isDisabled={loading}>
            {loading ? "Creating account..." : "Create Account"}
          </Button>
        </form>
      </Card>

      <Typography.Paragraph size="sm" color="muted" className="text-center">
        Already have an account?{" "}
        <Link href="/login" className="font-medium text-accent hover:underline">
          Sign in
        </Link>
        .
      </Typography.Paragraph>
    </div>
  );
}
