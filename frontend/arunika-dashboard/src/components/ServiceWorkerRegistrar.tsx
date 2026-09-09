"use client";

import { useEffect } from "react";

/**
 * Registers the service worker that makes Arunika installable and usable
 * offline. Renders nothing.
 *
 * Registration is skipped in development so an active worker never serves a
 * stale bundle over the dev server's hot updates.
 */
export function ServiceWorkerRegistrar() {
  useEffect(() => {
    if (process.env.NODE_ENV !== "production") return;
    if (typeof navigator === "undefined" || !("serviceWorker" in navigator)) return;

    let cancelled = false;

    const register = async () => {
      try {
        const registration = await navigator.serviceWorker.register("/sw.js", { scope: "/" });
        if (cancelled) return;

        // A worker that is already waiting (or becomes ready later) is only
        // holding back because the old one still controls open tabs. Tell it to
        // activate; `controllerchange` below then swaps in the new build.
        const promoteWaiting = () => registration.waiting?.postMessage("SKIP_WAITING");
        promoteWaiting();
        registration.addEventListener("updatefound", () => {
          registration.installing?.addEventListener("statechange", promoteWaiting);
        });
      } catch (error) {
        // A failed registration must never break the app: the site still works
        // fully online without a worker.
        console.error("Service worker registration failed", error);
      }
    };

    // On a first visit the page is uncontrolled and the new worker claims it
    // immediately, which is not an update and must not trigger a reload. Only
    // a controller *swap* means a newer build took over.
    const hadController = Boolean(navigator.serviceWorker.controller);
    let reloading = false;
    const onControllerChange = () => {
      if (!hadController || reloading) return;
      reloading = true;
      window.location.reload();
    };
    navigator.serviceWorker.addEventListener("controllerchange", onControllerChange);

    // Wait for load so registration never competes with the first paint.
    if (document.readyState === "complete") void register();
    else window.addEventListener("load", () => void register(), { once: true });

    return () => {
      cancelled = true;
      navigator.serviceWorker.removeEventListener("controllerchange", onControllerChange);
    };
  }, []);

  return null;
}
