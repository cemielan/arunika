"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { Button, Card, ErrorMessage, Typography } from "@heroui/react";
import { login as apiLogin } from "@/lib/api";
import { useAuth } from "@/lib/auth-context";

type FieldErrors = {
  email?: string;
  password?: string;
};

export default function LoginPage() {
  const router = useRouter();
  const { login, showNotification } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [loading, setLoading] = useState(false);

  const validate = (): boolean => {
    const errors: FieldErrors = {};
    if (!email.trim()) errors.email = "Email is required.";
    if (!password) errors.password = "Password is required.";
    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;

    setLoading(true);

    try {
      const result = await apiLogin(email, password);
      login(result.accessToken, result.refreshToken, result.user);
      showNotification({ status: "success", title: "Welcome back", message: "You have been signed in successfully." });
      router.push("/");
    } catch (err) {
      const message = err instanceof Error ? err.message : "Login failed";
      showNotification({ status: "danger", title: "Sign in failed", message });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="mx-auto mt-16 flex max-w-sm flex-col gap-6">
      <div className="text-center">
        <Typography.Heading level={1} className="text-2xl">Sign In</Typography.Heading>
        <Typography.Paragraph color="muted" className="mt-1">
          Welcome back to Arunika.
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
              onBlur={() => { if (!password) setFieldErrors((prev) => ({ ...prev, password: "Password is required." })); }}
              className={`rounded-lg border bg-background px-3 py-2 text-sm outline-none focus:border-accent ${fieldErrors.password ? "border-danger" : "border-border"}`}
              placeholder="Your password"
            />
            {fieldErrors.password && <ErrorMessage>{fieldErrors.password}</ErrorMessage>}
          </div>

          <Button type="submit" variant="primary" isDisabled={loading}>
            {loading ? "Signing in..." : "Sign In"}
          </Button>
        </form>
      </Card>

      <Typography.Paragraph size="sm" color="muted" className="text-center">
        Don&apos;t have an account?{" "}
        <Link href="/register" className="font-medium text-accent hover:underline">
          Create one
        </Link>
        .
      </Typography.Paragraph>
    </div>
  );
}
