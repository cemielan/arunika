"use client";

import { createContext, useCallback, useContext, useEffect, useState } from "react";
import type { ReactNode } from "react";
import type { User } from "@supabase/supabase-js";
import { getSupabaseClient } from "./supabase";
import { getMe, siteUrl, upsertMe } from "./api";
import {
  normalizeEmail,
  validateEmail,
  validatePasswordForSignIn,
  validatePasswordForSignUp,
} from "./auth-validation";

export type Notification = {
  status: "danger" | "success";
  title: string;
  message: string;
};

type AppUser = {
  id: string;
  email: string;
};

type AuthContextValue = {
  user: AppUser | null;
  isAuthenticated: boolean;
  ready: boolean;
  signIn: (email: string, password: string) => Promise<void>;
  signUp: (email: string, password: string) => Promise<boolean>;
  resendConfirmationEmail: (email: string) => Promise<void>;
  signOut: () => Promise<void>;
  showNotification: (n: Notification) => void;
  dismissNotification: () => void;
  notification: Notification | null;
};

const AuthContext = createContext<AuthContextValue | null>(null);

function toAppUser(user: User | null): AppUser | null {
  return user?.email ? { id: user.id, email: user.email } : null;
}

function friendlyAuthError(error: { message: string } | null): string {
  if (!error) return "Something went wrong. Please try again.";
  const message = error.message;
  if (message.includes("Invalid login credentials")) return "Invalid email or password.";
  if (message.includes("User already registered")) {
    return "An account with this email already exists. Check your inbox for the confirmation email.";
  }
  if (message.includes("Email not confirmed")) return "Please verify your email first.";
  return message;
}

function assertSignInInput(email: string, password: string): string {
  const normalizedEmail = normalizeEmail(email);
  const emailError = validateEmail(normalizedEmail);
  if (emailError) throw new Error(emailError);

  const passwordError = validatePasswordForSignIn(password);
  if (passwordError) throw new Error(passwordError);

  return normalizedEmail;
}

function assertSignUpInput(email: string, password: string): string {
  const normalizedEmail = normalizeEmail(email);
  const emailError = validateEmail(normalizedEmail);
  if (emailError) throw new Error(emailError);

  const passwordError = validatePasswordForSignUp(password);
  if (passwordError) throw new Error(passwordError);

  return normalizedEmail;
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AppUser | null>(null);
  const [ready, setReady] = useState(false);
  const [notification, setNotification] = useState<Notification | null>(null);

  const ensureSupabaseUser = useCallback(async (token: string): Promise<User> => {
    const { data, error } = await getSupabaseClient().auth.getUser(token);
    if (error || !data.user?.email) {
      throw new Error("This session is no longer valid. Please sign in again.");
    }

    if (!data.user.email_confirmed_at) {
      throw new Error("Please verify your email first.");
    }

    return data.user;
  }, []);

  const ensureBackendProfile = useCallback(async (token: string) => {
    try {
      const me = await getMe(token);
      if (me === null) {
        await upsertMe(token);
        const hydrated = await getMe(token);
        if (hydrated === null) {
          throw new Error("Unable to initialize your account profile. Please contact support.");
        }
      }
    } catch (error) {
      if (error instanceof Error) throw error;
      throw new Error("Unable to complete sign in because your account profile could not be verified.");
    }
  }, []);

  useEffect(() => {
    let cancelled = false;

    const initializeAuth = async () => {
      try {
        const { data } = await getSupabaseClient().auth.getSession();
        const token = data.session?.access_token;

        if (!token) {
          if (!cancelled) setUser(null);
          return;
        }

        const verifiedUser = await ensureSupabaseUser(token);
        await ensureBackendProfile(token);
        if (!cancelled) setUser(toAppUser(verifiedUser));
      } catch {
        await getSupabaseClient().auth.signOut();
        if (!cancelled) setUser(null);
      } finally {
        if (!cancelled) setReady(true);
      }
    };

    void initializeAuth();

    const { data: sub } = getSupabaseClient().auth.onAuthStateChange((_event, session) => {
      if (!session?.access_token) {
        setUser(null);
        return;
      }

      void (async () => {
        try {
          const verifiedUser = await ensureSupabaseUser(session.access_token);
          await ensureBackendProfile(session.access_token);
          if (!cancelled) setUser(toAppUser(verifiedUser));
        } catch {
          await getSupabaseClient().auth.signOut();
          if (!cancelled) setUser(null);
        }
      })();
    });
    return () => {
      cancelled = true;
      sub.subscription.unsubscribe();
    };
  }, [ensureBackendProfile, ensureSupabaseUser]);

  const dismissNotification = useCallback(() => setNotification(null), []);

  useEffect(() => {
    if (!notification) return;
    const timer = setTimeout(dismissNotification, 5000);
    return () => clearTimeout(timer);
  }, [notification, dismissNotification]);

  const syncProfile = useCallback(async () => {
    const { data } = await getSupabaseClient().auth.getSession();
    const token = data.session?.access_token;
    if (!token) return;
    try {
      await upsertMe(token);
    } catch {
      // Profile sync feeds the daily digest; failure here is non-critical.
    }
  }, []);

  const signIn = useCallback(async (email: string, password: string) => {
    const normalizedEmail = assertSignInInput(email, password);

    const { data, error } = await getSupabaseClient().auth.signInWithPassword({
      email: normalizedEmail,
      password,
    });
    if (error) throw new Error(friendlyAuthError(error));

    const token = data.session?.access_token;
    if (!token) {
      await getSupabaseClient().auth.signOut();
      throw new Error("Sign in session could not be established. Please try again.");
    }

    try {
      const verifiedUser = await ensureSupabaseUser(token);
      await ensureBackendProfile(token);
      setUser(toAppUser(verifiedUser));
    } catch (error) {
      await getSupabaseClient().auth.signOut();
      if (error instanceof Error) throw error;
      throw new Error("Unable to complete sign in because your account profile could not be verified.");
    }

    await syncProfile();
  }, [ensureBackendProfile, ensureSupabaseUser, syncProfile]);

  const signUp = useCallback(async (email: string, password: string): Promise<boolean> => {
    const normalizedEmail = assertSignUpInput(email, password);

    const { data, error } = await getSupabaseClient().auth.signUp({
      email: normalizedEmail,
      password,
      options: {
        emailRedirectTo: `${siteUrl()}/verify`,
      },
    });

    const maybeIdentities = data.user?.identities;
    const userAlreadyExists = Array.isArray(maybeIdentities) && maybeIdentities.length === 0;

    if (!error && userAlreadyExists) {
      throw new Error("An account with this email already exists. Please sign in instead.");
    }

    if (error) {
      const user = data.user as User | null;
      if (error.message.includes("already registered") && user?.email_confirmed_at) {
        throw new Error("An account with this email already exists. Please sign in instead.");
      }
      throw new Error(friendlyAuthError(error));
    }

    return data.session === null;
  }, []);

  const resendConfirmationEmail = useCallback(async (email: string) => {
    const normalizedEmail = normalizeEmail(email);
    const emailError = validateEmail(normalizedEmail);
    if (emailError) throw new Error(emailError);

    const { error } = await getSupabaseClient().auth.resend({
      type: "signup",
      email: normalizedEmail,
      options: {
        emailRedirectTo: `${siteUrl()}/verify`,
      },
    });
    if (error) throw new Error(friendlyAuthError(error));
  }, []);

  const signOut = useCallback(async () => {
    await getSupabaseClient().auth.signOut();
    setUser(null);
  }, []);

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated: user !== null,
        ready,
        signIn,
        signUp,
        resendConfirmationEmail,
        signOut,
        showNotification: setNotification,
        dismissNotification,
        notification,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

/**
 * Toast, built as a bordered editorial panel rather than HeroUI's rounded,
 * filled Alert — consistent with the rest of the site, and with no icon,
 * since the status is already named in words. The one colour cue (the status
 * word) is never the only carrier of meaning: the word itself says "Success"
 * or "Error".
 */
export function NotificationBanner() {
  const { notification, dismissNotification } = useAuth();

  if (!notification) return null;

  const tone = notification.status === "success" ? "text-success" : "text-danger";
  const label = notification.status === "success" ? "Success" : "Error";

  return (
    // Mobile: docked below the header rather than the bottom corner, both to
    // clear the home-indicator safe area and because the install prompt
    // already lives bottom-left there — at full toast width the two would
    // otherwise overlap. Desktop keeps the original bottom-right placement,
    // where the install prompt never grows wide enough to collide.
    <div className="fixed inset-x-4 top-[calc(env(safe-area-inset-top)+4.25rem)] z-50 animate-in fade-in slide-in-from-top-2 duration-300 sm:inset-x-auto sm:top-auto sm:right-6 sm:bottom-[calc(1.5rem+env(safe-area-inset-bottom))] sm:w-full sm:max-w-sm">
      <div className="editorial-panel relative bg-background/80 p-4 shadow-lg backdrop-blur supports-backdrop-filter:bg-background/60">
        <button
          onClick={dismissNotification}
          className="editorial-rubric absolute right-4 top-4 text-muted transition-colors hover:text-foreground"
        >
          Dismiss
        </button>
        <span className={`editorial-rubric ${tone}`}>{label}</span>
        <p className="editorial-display mt-1 pr-16 text-lg text-foreground">{notification.title}</p>
        <p className="mt-1.5 text-sm leading-relaxed text-muted">{notification.message}</p>
      </div>
    </div>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within an AuthProvider");
  return ctx;
}
