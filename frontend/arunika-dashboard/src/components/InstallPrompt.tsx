"use client";

import { startTransition, useCallback, useEffect, useState } from "react";

// Chromium-only, so it is absent from the DOM lib.
interface BeforeInstallPromptEvent extends Event {
  prompt: () => Promise<void>;
  userChoice: Promise<{ outcome: "accepted" | "dismissed" }>;
}

const DISMISSED_KEY = "arunika:install-prompt-dismissed";

function isStandalone() {
  return (
    window.matchMedia("(display-mode: standalone)").matches ||
    // Non-standard, but the only reliable signal on iOS Safari.
    (window.navigator as Navigator & { standalone?: boolean }).standalone === true
  );
}

function isIosSafari() {
  const ua = window.navigator.userAgent;
  const isIos = /iPad|iPhone|iPod/.test(ua) || (ua.includes("Macintosh") && navigator.maxTouchPoints > 1);
  // Chrome and Firefox on iOS cannot install; only Safari exposes the option.
  return isIos && !/CriOS|FxiOS|EdgiOS/.test(ua);
}

/**
 * Invites the user to install Arunika to their home screen.
 *
 * Android/desktop Chromium fires `beforeinstallprompt`, so installation can be
 * triggered directly. iOS has no such API, so Safari users get the manual
 * Share → "Add to Home Screen" instructions instead.
 */
export function InstallPrompt() {
  const [installEvent, setInstallEvent] = useState<BeforeInstallPromptEvent | null>(null);
  const [showIosHint, setShowIosHint] = useState(false);

  useEffect(() => {
    let dismissed = false;
    try {
      dismissed = window.localStorage.getItem(DISMISSED_KEY) === "1";
    } catch {
      // Private browsing can throw on access; treat it as "not dismissed".
    }
    if (dismissed || isStandalone()) return;

    const onBeforeInstallPrompt = (event: Event) => {
      // Suppress the browser's own mini-infobar so the in-app card is the only
      // prompt the user sees.
      event.preventDefault();
      setInstallEvent(event as BeforeInstallPromptEvent);
    };
    window.addEventListener("beforeinstallprompt", onBeforeInstallPrompt);

    const onInstalled = () => setInstallEvent(null);
    window.addEventListener("appinstalled", onInstalled);

    // Wrapped in a transition because the platform check is a client-only
    // read: setting state synchronously in an effect would cascade a render.
    if (isIosSafari()) startTransition(() => setShowIosHint(true));

    return () => {
      window.removeEventListener("beforeinstallprompt", onBeforeInstallPrompt);
      window.removeEventListener("appinstalled", onInstalled);
    };
  }, []);

  const dismiss = useCallback(() => {
    setInstallEvent(null);
    setShowIosHint(false);
    try {
      window.localStorage.setItem(DISMISSED_KEY, "1");
    } catch {
      // Nothing to persist to; the prompt simply returns on the next visit.
    }
  }, []);

  const install = useCallback(async () => {
    if (!installEvent) return;
    await installEvent.prompt();
    // The event can only be used once, whatever the user chose.
    setInstallEvent(null);
    await installEvent.userChoice;
  }, [installEvent]);

  if (!installEvent && !showIosHint) return null;

  return (
    <div className="editorial-panel fixed bottom-4 left-4 z-40 w-[calc(100%-2rem)] max-w-sm bg-surface p-4 shadow-lg sm:bottom-6 sm:left-6">
      <button
        onClick={dismiss}
        className="editorial-rubric absolute right-3 top-3 text-muted transition-colors hover:text-foreground"
      >
        Dismiss
      </button>

      <p className="editorial-display pr-16 text-lg text-foreground">Install Arunika</p>

      {installEvent ? (
        <>
          <p className="mt-2 text-sm leading-relaxed text-muted">
            Add Arunika to your home screen for full-screen access and offline reading.
          </p>
          <button type="button" className="editorial-btn mt-4" onClick={() => void install()}>
            Install app
          </button>
        </>
      ) : (
        <p className="mt-2 text-sm leading-relaxed text-muted">
          Tap <span className="font-medium text-foreground">Share</span> in Safari&rsquo;s toolbar,
          then <span className="font-medium text-foreground">Add to Home Screen</span>.
        </p>
      )}
    </div>
  );
}
