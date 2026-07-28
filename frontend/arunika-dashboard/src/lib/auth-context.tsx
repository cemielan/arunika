"use client";

import { createContext, useCallback, useContext, useEffect, useState } from "react";
import type { ReactNode } from "react";
import type { UserDto } from "./api";
import { Alert } from "@heroui/react";

type AuthState = {
  user: UserDto | null;
  accessToken: string | null;
};

export type Notification = {
  status: "danger" | "success";
  title: string;
  message: string;
};

type AuthContextValue = AuthState & {
  login: (token: string, refreshToken: string, user: UserDto) => void;
  logout: () => void;
  isAuthenticated: boolean;
  showNotification: (n: Notification) => void;
  dismissNotification: () => void;
  notification: Notification | null;
};

const AuthContext = createContext<AuthContextValue | null>(null);

const STORAGE_KEY_TOKEN = "arunika_access_token";
const STORAGE_KEY_REFRESH = "arunika_refresh_token";
const STORAGE_KEY_USER = "arunika_user";

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>(() => {
    if (typeof window === "undefined") return { user: null, accessToken: null };
    try {
      const token = localStorage.getItem(STORAGE_KEY_TOKEN);
      const userStr = localStorage.getItem(STORAGE_KEY_USER);
      if (token && userStr) {
        return { user: JSON.parse(userStr) as UserDto, accessToken: token };
      }
    } catch { }
    return { user: null, accessToken: null };
  });

  const [notification, setNotification] = useState<Notification | null>(null);

  const dismissNotification = useCallback(() => setNotification(null), []);

  useEffect(() => {
    if (!notification) return;
    const timer = setTimeout(dismissNotification, 5000);
    return () => clearTimeout(timer);
  }, [notification, dismissNotification]);

  const login = useCallback((token: string, _refreshToken: string, user: UserDto) => {
    localStorage.setItem(STORAGE_KEY_TOKEN, token);
    localStorage.setItem(STORAGE_KEY_REFRESH, _refreshToken);
    localStorage.setItem(STORAGE_KEY_USER, JSON.stringify(user));
    setState({ user, accessToken: token });
  }, []);

  const logout = useCallback(() => {
    localStorage.removeItem(STORAGE_KEY_TOKEN);
    localStorage.removeItem(STORAGE_KEY_REFRESH);
    localStorage.removeItem(STORAGE_KEY_USER);
    setState({ user: null, accessToken: null });
  }, []);

  const showNotification = useCallback((n: Notification) => setNotification(n), []);

  return (
    <AuthContext.Provider
      value={{ ...state, login, logout, isAuthenticated: state.user !== null, showNotification, dismissNotification, notification }}
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
