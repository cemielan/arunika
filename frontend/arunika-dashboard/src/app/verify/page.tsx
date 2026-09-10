"use client";

import { Suspense, useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";
import { getSupabaseClient } from "@/lib/supabase";
import { upsertMe } from "@/lib/api";
import { useAuth } from "@/lib/auth-context";
import { AuthShell } from "@/components/editorial";

export default function VerifyPage() {
  return (
    <Suspense
      fallback={
        <div className="mx-auto mt-16 text-center">
          <span className="editorial-rubric text-muted">Loading</span>
        </div>
      }
    >
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

  const friendlyResendError = (message: string): string => {
    if (message.toLowerCase().includes("rate limit")) {
      return "Too many resend attempts. Please wait a minute and try again.";
    }

    if (message.toLowerCase().includes("smtp") || message.toLowerCase().includes("email provider")) {
      return "Email delivery is not configured correctly in Supabase SMTP settings.";
    }

    return message;
  };

  useEffect(() => {
    const query = new URLSearchParams(window.location.search);
    const hash = new URLSearchParams(window.location.hash.substring(1));
    const tokenHash = query.get("token_hash") ?? hash.get("token_hash");
    const type = (query.get("type") ?? hash.get("type")) as "signup" | "invite" | "magiclink" | "recovery" | "email_change" | null;
    const hasCode = query.has("code") || hash.has("code");

    let cancelled = false;
    const finalize = async () => {
      const { data } = await getSupabaseClient().auth.getSession();
      if (!data.session?.access_token) return;

      const { data: userData, error: userError } = await getSupabaseClient().auth.getUser(data.session.access_token);
      if (userError || !userData.user?.email) {
        setLinkError("This verification session is no longer valid. Please sign in again.");
        await getSupabaseClient().auth.signOut();
        return;
      }

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
      showNotification({ status: "danger", title: "Error", message: friendlyResendError(message) });
    } finally {
      setResending(false);
    }
  };

  return (
    <AuthShell
      kicker="One more step"
      title="Confirm your email"
      lede={
        <>
          We sent a confirmation email to{" "}
          <strong className="text-foreground">{email}</strong>. Open it and click the{" "}
          <strong className="text-foreground">Confirm email</strong> button inside.
        </>
      }
      footer={
        <Link href="/login" className="font-medium text-accent hover:underline">
          Back to sign in
        </Link>
      }
    >
      <div className="flex flex-col gap-5">
        <p className="text-sm leading-relaxed text-muted">
          No email? Check your spam folder, then resend it below.
        </p>

        <button
          type="button"
          onClick={handleResend}
          className="editorial-btn w-full"
          disabled={resending}
        >
          {resending ? "Sending…" : "Resend confirmation email"}
        </button>

        {linkError && <p className="text-xs leading-relaxed text-danger">{linkError}</p>}
      </div>
    </AuthShell>
  );
}
