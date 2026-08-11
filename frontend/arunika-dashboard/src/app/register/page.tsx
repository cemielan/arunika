"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { Mail } from "lucide-react";
import { Button, Card, ErrorMessage, Modal, Typography, useOverlayState } from "@heroui/react";
import { useAuth } from "@/lib/auth-context";
import {
  hasAuthFieldErrors,
  normalizeEmail,
  validateSignUpFields,
  type AuthFieldErrors,
} from "@/lib/auth-validation";

type SignupOutcome = {
  email: string;
  needsVerification: boolean;
};

export default function RegisterPage() {
  const router = useRouter();
  const { signUp, showNotification } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [fieldErrors, setFieldErrors] = useState<AuthFieldErrors>({});
  const [loading, setLoading] = useState(false);
  const [signupOutcome, setSignupOutcome] = useState<SignupOutcome | null>(null);
  const modalState = useOverlayState();

  const validate = (): boolean => {
    const errors = validateSignUpFields(email, password, confirmPassword);
    setFieldErrors(errors);
    return !hasAuthFieldErrors(errors);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;

    setLoading(true);
    const normalizedEmail = normalizeEmail(email);

    try {
      const needsVerification = await signUp(normalizedEmail, password);
      setSignupOutcome({ email: normalizedEmail, needsVerification });
      modalState.open();
    } catch (err) {
      const message = err instanceof Error ? err.message : "Registration failed";
      if (message.includes("sign in instead")) {
        showNotification({ status: "success", title: "Account already exists", message: "Please sign in with your existing account." });
        router.push("/login");
        return;
      }
      if (message.includes("already exists")) {
        showNotification({ status: "success", title: "Account created", message });
        router.push(`/verify?email=${encodeURIComponent(normalizedEmail)}`);
        return;
      }
      showNotification({ status: "danger", title: "Sign up failed", message });
    } finally {
      setLoading(false);
    }
  };

  const handleContinue = () => {
    modalState.close();
    if (signupOutcome?.needsVerification) {
      showNotification({ status: "success", title: "Account created", message: "Check your email for the confirmation link." });
      router.push(`/verify?email=${encodeURIComponent(signupOutcome.email)}`);
    } else {
      showNotification({ status: "success", title: "Account created", message: "Welcome to Arunika." });
      router.push("/");
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
              onBlur={() => setFieldErrors((prev) => ({ ...prev, email: validateSignUpFields(email, password, confirmPassword).email }))}
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
              onBlur={() => setFieldErrors((prev) => ({ ...prev, password: validateSignUpFields(email, password, confirmPassword).password }))}
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
              onBlur={() => setFieldErrors((prev) => ({ ...prev, confirmPassword: validateSignUpFields(email, password, confirmPassword).confirmPassword }))}
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

      {signupOutcome && (
        <Modal state={modalState}>
          <Modal.Backdrop isDismissable={false} />
          <Modal.Container size="sm">
            <Modal.Dialog>
              <Modal.Header>
                <Modal.Icon className="text-accent">
                  <Mail className="h-5 w-5" />
                </Modal.Icon>
                <Modal.Heading className="text-lg">Daily email summary</Modal.Heading>
              </Modal.Header>
              <Modal.Body>
                <Typography.Paragraph size="sm" className="leading-relaxed">
                  Welcome to Arunika, {signupOutcome.email}.
                </Typography.Paragraph>
                <Typography.Paragraph size="sm" color="muted" className="leading-relaxed">
                  We will use your email to send you the day&apos;s briefing summary every morning,
                  shortly after it is generated. You can opt out of these emails at any time.
                </Typography.Paragraph>
              </Modal.Body>
              <Modal.Footer>
                <Button variant="primary" onPress={handleContinue}>
                  Got it
                </Button>
              </Modal.Footer>
            </Modal.Dialog>
          </Modal.Container>
        </Modal>
      )}
    </div>
  );
}
