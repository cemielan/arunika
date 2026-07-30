"use client";

import { useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { Button, Card, ErrorMessage, Typography } from "@heroui/react";
import { verifyOtp, resendOtp } from "@/lib/api";
import { useAuth } from "@/lib/auth-context";

export default function VerifyPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const email = searchParams.get("email") ?? "";
  const { login, showNotification } = useAuth();
  const [otp, setOtp] = useState("");
  const [otpError, setOtpError] = useState<string | undefined>();
  const [loading, setLoading] = useState(false);
  const [resending, setResending] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!otp.trim()) {
      setOtpError("Verification code is required.");
      return;
    }

    setLoading(true);

    try {
      const result = await verifyOtp(email, otp);
      login(result.accessToken, result.refreshToken, result.user);
      showNotification({ status: "success", title: "Email verified", message: "Your account is ready." });
      router.push("/");
    } catch (err) {
      const message = err instanceof Error ? err.message : "Verification failed";
      setOtpError(message);
    } finally {
      setLoading(false);
    }
  };

  const handleResend = async () => {
    setResending(true);
    try {
      await resendOtp(email);
      showNotification({ status: "success", title: "Code resent", message: "A new code has been sent to your email." });
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to resend";
      showNotification({ status: "danger", title: "Error", message });
    } finally {
      setResending(false);
    }
  };

  return (
    <div className="mx-auto mt-8 flex max-w-sm flex-col gap-6 px-4 sm:mt-16">
      <div className="text-center">
        <Typography.Heading level={1} className="text-2xl">Verify your email</Typography.Heading>
        <Typography.Paragraph color="muted" className="mt-1">
          Enter the 6-digit code sent to {email}.
        </Typography.Paragraph>
      </div>

      <Card variant="default" className="p-6">
        <form onSubmit={handleSubmit} className="flex flex-col gap-4" noValidate>
          <div className="flex flex-col gap-1">
            <Typography.Paragraph size="sm" weight="medium">Verification Code</Typography.Paragraph>
            <input
              type="text"
              inputMode="numeric"
              maxLength={6}
              value={otp}
              onChange={(e) => { setOtp(e.target.value.replace(/\D/g, "").slice(0, 6)); setOtpError(undefined); }}
              className={`rounded-lg border bg-background px-3 py-2 text-lg text-center tracking-[8px] outline-none focus:border-accent ${otpError ? "border-danger" : "border-border"}`}
              placeholder="000000"
              autoFocus
            />
            {otpError && <ErrorMessage>{otpError}</ErrorMessage>}
          </div>

          <Button type="submit" variant="primary" isDisabled={loading}>
            {loading ? "Verifying..." : "Verify Email"}
          </Button>
        </form>
      </Card>

      <Typography.Paragraph size="sm" color="muted" className="text-center">
        Didn&apos;t receive it?{" "}
        <button
          onClick={handleResend}
          disabled={resending}
          className="font-medium text-accent hover:underline disabled:opacity-50"
        >
          {resending ? "Sending..." : "Resend code"}
        </button>
      </Typography.Paragraph>
    </div>
  );
}
