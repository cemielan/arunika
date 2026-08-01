"use client";

import { Suspense, useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { Button, Card, ErrorMessage, InputOTP, Typography } from "@heroui/react";
import { getSupabaseClient } from "@/lib/supabase";
import { upsertMe } from "@/lib/api";
import { useAuth } from "@/lib/auth-context";

export default function VerifyPage() {
  return (
    <Suspense fallback={<div className="mx-auto mt-16 text-center text-muted">Loading...</div>}>
      <VerifyForm />
    </Suspense>
  );
}

function VerifyForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const email = searchParams.get("email") ?? "";
  const { verifyOtp, resendOtp, showNotification } = useAuth();
  const [otp, setOtp] = useState("");
  const [otpError, setOtpError] = useState<string | undefined>();
  const [loading, setLoading] = useState(false);
  const [resending, setResending] = useState(false);

  useEffect(() => {
    const query = new URLSearchParams(window.location.search);
    const hash = new URLSearchParams(window.location.hash.substring(1));
    const tokenHash = query.get("token_hash") ?? hash.get("token_hash");
    const type = (query.get("type") ?? hash.get("type")) as "signup" | "invite" | "magiclink" | "recovery" | "email_change" | null;
    const hasCode = query.has("code") || hash.has("code");
    if (!tokenHash && !type && !hasCode) return;

    let cancelled = false;
    const finalize = async () => {
      const { data } = await getSupabaseClient().auth.getSession();
      if (!data.session) return;
      try {
        await upsertMe(data.session.access_token);
      } catch {
        // Profile sync feeds the daily digest; failure here is non-critical.
      }
      showNotification({ status: "success", title: "Email verified", message: "Your account is ready." });
      router.push("/");
    };

    if (!tokenHash && !type && !hasCode) {
      getSupabaseClient().auth.getSession().then(({ data }) => {
        if (cancelled || !data.session) return;
        router.push("/");
      });
      return () => {
        cancelled = true;
      };
    }

    if (tokenHash && type) {
      (async () => {
        const { error } = await getSupabaseClient().auth.verifyOtp({ token_hash: tokenHash, type });
        if (cancelled) return;
        if (error) {
          setOtpError("The confirmation link is invalid or expired. Enter the code from the email instead.");
          return;
        }
        await finalize();
      })();
      return () => {
        cancelled = true;
      };
    }

    const { data: sub } = getSupabaseClient().auth.onAuthStateChange((_event, session) => {
      if (cancelled || !session) return;
      sub.subscription.unsubscribe();
      finalize();
    });
    getSupabaseClient().auth.getSession().then(({ data }) => {
      if (cancelled || !data.session) return;
      sub.subscription.unsubscribe();
      finalize();
    });
    return () => {
      cancelled = true;
      sub.subscription.unsubscribe();
    };
  }, [router, showNotification]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!otp.trim()) {
      setOtpError("Verification code is required.");
      return;
    }

    setLoading(true);

    try {
      await verifyOtp(email, otp);
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
          Click the confirmation link we sent to <strong>{email}</strong>. If it doesn&apos;t work, enter the 6-digit
          code from the same email below.
        </Typography.Paragraph>
      </div>

      <Card variant="default" className="p-6">
        <form onSubmit={handleSubmit} className="flex flex-col gap-4" noValidate>
          <div className="flex flex-col gap-1">
            <Typography.Paragraph size="sm" weight="medium">Verification Code (optional)</Typography.Paragraph>
            <InputOTP
              maxLength={6}
              value={otp}
              onChange={(value) => { setOtp(value.replace(/\D/g, "").slice(0, 6)); setOtpError(undefined); }}
              isInvalid={!!otpError}
              isDisabled={loading}
              className="justify-center"
              autoFocus
            >
              <InputOTP.Group>
                <InputOTP.Slot index={0} />
                <InputOTP.Slot index={1} />
                <InputOTP.Slot index={2} />
              </InputOTP.Group>
              <InputOTP.Separator />
              <InputOTP.Group>
                <InputOTP.Slot index={3} />
                <InputOTP.Slot index={4} />
                <InputOTP.Slot index={5} />
              </InputOTP.Group>
            </InputOTP>
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
