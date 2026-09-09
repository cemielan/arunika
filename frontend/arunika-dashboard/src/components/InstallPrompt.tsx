"use client";

import { startTransition, useCallback, useEffect, useState } from "react";
import { Button } from "@heroui/react";
import { Download, Share, SquarePlus, X } from "lucide-react";

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
    <div className="fixed bottom-4 left-4 z-40 w-[calc(100%-2rem)] max-w-sm rounded-xl border border-border bg-surface p-4 shadow-lg sm:bottom-6 sm:left-6">
      <button
        onClick={dismiss}
        aria-label="Dismiss install prompt"
        className="absolute right-2 top-2 text-muted hover:text-foreground"
      >
        <X size={16} />
      </button>

      <p className="pr-6 text-sm font-semibold">Install Arunika</p>

      {installEvent ? (
        <>
          <p className="mt-1 text-sm text-muted">
            Add Arunika to your home screen for full-screen access and offline reading.
          </p>
          <Button variant="primary" size="sm" className="mt-3 gap-1.5!" onPress={() => void install()}>
            <Download size={16} />
            Install app
          </Button>
        </>
      ) : (
        <p className="mt-1 flex flex-wrap items-center gap-1 text-sm text-muted">
          Tap
          <Share size={16} aria-label="Share" className="inline shrink-0" />
          in Safari&rsquo;s toolbar, then
          <SquarePlus size={16} aria-hidden="true" className="inline shrink-0" />
          <span className="font-medium text-foreground">Add to Home Screen</span>.
        </p>
      )}
    </div>
  );
}
