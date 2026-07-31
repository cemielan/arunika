"use client";

import { createContext, useCallback, useContext, useEffect, useState } from "react";
import type { ReactNode } from "react";
import type { User } from "@supabase/supabase-js";
import { Alert } from "@heroui/react";
import { supabase } from "./supabase";
import { upsertMe } from "./api";

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
  signUp: (email: string, password: string) => Promise<void>;
  verifyOtp: (email: string, token: string) => Promise<void>;
  resendOtp: (email: string) => Promise<void>;
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
    return "An account with this email already exists. Check your inbox for the verification code.";
  }
  if (message.includes("Email not confirmed")) return "Please verify your email first.";
  return message;
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AppUser | null>(null);
  const [ready, setReady] = useState(false);
  const [notification, setNotification] = useState<Notification | null>(null);

  useEffect(() => {
    supabase.auth.getSession().then(({ data }) => {
      setUser(toAppUser(data.session?.user ?? null));
      setReady(true);
    });
    const { data: sub } = supabase.auth.onAuthStateChange((_event, session) => {
      setUser(toAppUser(session?.user ?? null));
    });
    return () => sub.subscription.unsubscribe();
  }, []);

  const dismissNotification = useCallback(() => setNotification(null), []);

  useEffect(() => {
    if (!notification) return;
    const timer = setTimeout(dismissNotification, 5000);
    return () => clearTimeout(timer);
  }, [notification, dismissNotification]);

  const syncProfile = useCallback(async () => {
    const { data } = await supabase.auth.getSession();
    const token = data.session?.access_token;
    if (!token) return;
    try {
      await upsertMe(token);
    } catch {
      // Profile sync feeds the daily digest; failure here is non-critical.
    }
  }, []);

  const signIn = useCallback(async (email: string, password: string) => {
    const { error } = await supabase.auth.signInWithPassword({ email, password });
    if (error) throw new Error(friendlyAuthError(error));
    await syncProfile();
  }, [syncProfile]);

  const signUp = useCallback(async (email: string, password: string) => {
    const { error } = await supabase.auth.signUp({
      email,
      password,
      options: {
        emailRedirectTo: `${window.location.origin}/verify`,
      },
    });
    if (error) throw new Error(friendlyAuthError(error));
  }, []);

  const verifyOtp = useCallback(async (email: string, token: string) => {
    const { error } = await supabase.auth.verifyOtp({ email, token, type: "signup" });
    if (error) throw new Error(friendlyAuthError(error));
    await syncProfile();
  }, [syncProfile]);

  const resendOtp = useCallback(async (email: string) => {
    const { error } = await supabase.auth.resend({ type: "signup", email });
    if (error) throw new Error(friendlyAuthError(error));
  }, []);

  const signOut = useCallback(async () => {
    await supabase.auth.signOut();
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
        verifyOtp,
        resendOtp,
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

export function NotificationBanner() {
  const { notification, dismissNotification } = useAuth();

  if (!notification) return null;

  return (
    <div className="fixed bottom-4 right-4 z-50 w-[calc(100%-2rem)] max-w-sm animate-in fade-in slide-in-from-bottom-4 duration-300 sm:bottom-6 sm:right-6">
      <Alert status={notification.status}>
        <Alert.Indicator />
        <Alert.Content>
          <Alert.Title>{notification.title}</Alert.Title>
          <Alert.Description>{notification.message}</Alert.Description>
        </Alert.Content>
        <button
          onClick={dismissNotification}
          className="absolute right-2 top-2 text-sm text-muted hover:text-foreground"
        >
          &times;
        </button>
      </Alert>
    </div>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within an AuthProvider");
  return ctx;
}
