"use client";

import { Suspense, useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";
import { Button, Card, ErrorMessage, Typography } from "@heroui/react";
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
  const { resendConfirmationEmail, showNotification } = useAuth();
  const [linkError, setLinkError] = useState<string | undefined>();
  const [resending, setResending] = useState(false);

  useEffect(() => {
    const query = new URLSearchParams(window.location.search);
    const hash = new URLSearchParams(window.location.hash.substring(1));
    const tokenHash = query.get("token_hash") ?? hash.get("token_hash");
    const type = (query.get("type") ?? hash.get("type")) as "signup" | "invite" | "magiclink" | "recovery" | "email_change" | null;
    const hasCode = query.has("code") || hash.has("code");

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
          setLinkError("The confirmation link is invalid or expired. Use the button below to resend the email.");
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

  const handleResend = async () => {
    if (!email.trim()) {
      showNotification({ status: "danger", title: "Error", message: "Enter your email on the sign up page first." });
      return;
    }
    setResending(true);
    try {
      await resendConfirmationEmail(email);
      showNotification({ status: "success", title: "Email sent", message: "A new confirmation email is on its way. Check your inbox and spam folder." });
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
        <Typography.Heading level={1} className="text-2xl">Confirm your email</Typography.Heading>
        <Typography.Paragraph color="muted" className="mt-1">
          We sent a confirmation email to <strong>{email}</strong>. Open it and click the{" "}
          <strong>Confirm email</strong> button inside.
        </Typography.Paragraph>
      </div>

      <Card variant="default" className="p-6 text-center">
        <Typography.Paragraph size="sm" color="muted">
          No email? Check your spam folder, then resend it below.
        </Typography.Paragraph>
        <div className="mt-4 flex flex-col gap-3">
          <Button variant="primary" onPress={handleResend} isDisabled={resending}>
            {resending ? "Sending..." : "Resend confirmation email"}
          </Button>
          <Link href="/login" className="text-sm font-medium text-accent hover:underline">
            Back to sign in
          </Link>
        </div>
        {linkError && <div className="mt-4"><ErrorMessage>{linkError}</ErrorMessage></div>}
      </Card>
    </div>
  );
}
