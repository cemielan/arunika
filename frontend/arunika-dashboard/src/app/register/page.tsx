"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { Button, Modal, Typography, useOverlayState } from "@heroui/react";
import { useAuth } from "@/lib/auth-context";
import { AuthShell, Field } from "@/components/editorial";
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
    <AuthShell
      kicker="Subscribe"
      title="Create Account"
      lede="Join Arunika for free and get the briefing in your inbox each morning."
      footer={
        <>
          Already have an account?{" "}
          <Link href="/login" className="font-medium text-accent hover:underline">
            Sign in
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
            onBlur={() => setFieldErrors((prev) => ({ ...prev, email: validateSignUpFields(email, password, confirmPassword).email }))}
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
            onBlur={() => setFieldErrors((prev) => ({ ...prev, password: validateSignUpFields(email, password, confirmPassword).password }))}
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
            onBlur={() => setFieldErrors((prev) => ({ ...prev, confirmPassword: validateSignUpFields(email, password, confirmPassword).confirmPassword }))}
            className="editorial-input"
            placeholder="Repeat your password"
          />
        </Field>

        <button type="submit" className="editorial-btn w-full" disabled={loading}>
          {loading ? "Creating account…" : "Create Account"}
        </button>
      </form>

      {signupOutcome && (
        <Modal state={modalState}>
          <Modal.Backdrop isDismissable={false} />
          <Modal.Container size="sm">
            <Modal.Dialog>
              <Modal.Header>
                <Modal.Heading className="editorial-display text-xl">
                  Daily email summary
                </Modal.Heading>
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
    </AuthShell>
  );
}
