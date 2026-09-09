/*
 * Arunika service worker.
 *
 * Hand-written rather than generated: Next 16 builds with Turbopack, so
 * webpack-based generators (next-pwa/workbox-webpack-plugin) never run.
 *
 * Caching policy, deliberately conservative:
 *   - Build output under /_next/static is content-hashed, so it is immutable
 *     and safe to serve cache-first.
 *   - Navigations are network-first so users never see a stale dashboard; the
 *     cached copy is only a fallback for offline/flaky networks.
 *   - Authenticated and cross-origin API traffic (the Arunika API, Supabase)
 *     is NEVER cached. Cache Storage is unencrypted and survives sign-out, so
 *     caching those responses would leave account data readable on a shared
 *     or lost device.
 */

const VERSION = "v1";
const STATIC_CACHE = `arunika-static-${VERSION}`;
const PAGE_CACHE = `arunika-pages-${VERSION}`;
const ASSET_CACHE = `arunika-assets-${VERSION}`;
const CURRENT_CACHES = [STATIC_CACHE, PAGE_CACHE, ASSET_CACHE];

const OFFLINE_URL = "/offline.html";
const PRECACHE_URLS = [OFFLINE_URL, "/icon-192.png", "/icon-512.png"];

self.addEventListener("install", (event) => {
  event.waitUntil(
    (async () => {
      const cache = await caches.open(STATIC_CACHE);
      // `reload` bypasses the HTTP cache so a new worker never precaches a
      // stale copy of the offline shell.
      await cache.addAll(PRECACHE_URLS.map((url) => new Request(url, { cache: "reload" })));
      await self.skipWaiting();
    })(),
  );
});

self.addEventListener("activate", (event) => {
  event.waitUntil(
    (async () => {
      const keys = await caches.keys();
      await Promise.all(keys.filter((key) => !CURRENT_CACHES.includes(key)).map((key) => caches.delete(key)));
      await self.clients.claim();
    })(),
  );
});

self.addEventListener("message", (event) => {
  if (event.data === "SKIP_WAITING") self.skipWaiting();
});

function isImmutableBuildAsset(url) {
  return url.origin === self.location.origin && url.pathname.startsWith("/_next/static/");
}

function isStaticAsset(url) {
  return (
    url.origin === self.location.origin &&
    /\.(?:png|jpe?g|svg|webp|avif|ico|woff2?|ttf)$/i.test(url.pathname)
  );
}

function isGoogleFont(url) {
  return url.hostname === "fonts.googleapis.com" || url.hostname === "fonts.gstatic.com";
}

async function cacheFirst(request, cacheName) {
  const cache = await caches.open(cacheName);
  const cached = await cache.match(request);
  if (cached) return cached;

  const response = await fetch(request);
  if (response.ok || response.type === "opaque") cache.put(request, response.clone());
  return response;
}

async function staleWhileRevalidate(request, cacheName) {
  const cache = await caches.open(cacheName);
  const cached = await cache.match(request);

  const refresh = fetch(request)
    .then((response) => {
      if (response.ok || response.type === "opaque") cache.put(request, response.clone());
      return response;
    })
    .catch(() => undefined);

  if (cached) return cached;

  const response = await refresh;
  if (response) return response;
  throw new Error(`Unable to fetch ${request.url}`);
}

async function networkFirstPage(request) {
  const cache = await caches.open(PAGE_CACHE);
  try {
    const response = await fetch(request);
    // Only shareable, non-personalised HTML is worth keeping: a response that
    // opts out of storage (`no-store`) is excluded on purpose.
    const cacheControl = response.headers.get("Cache-Control") ?? "";
    if (response.ok && !cacheControl.includes("no-store")) cache.put(request, response.clone());
    return response;
  } catch {
    const cached = await cache.match(request, { ignoreSearch: true });
    if (cached) return cached;

    const offline = await caches.match(OFFLINE_URL, { cacheName: STATIC_CACHE });
    if (offline) return offline;

    return new Response("You are offline.", {
      status: 503,
      headers: { "Content-Type": "text/plain; charset=utf-8" },
    });
  }
}

self.addEventListener("fetch", (event) => {
  const { request } = event;

  // Everything below is a read-through cache, which is only ever correct for
  // GET. Mutations and range requests go straight to the network.
  if (request.method !== "GET" || request.headers.has("range")) return;

  const url = new URL(request.url);
  if (url.protocol !== "http:" && url.protocol !== "https:") return;

  // Never touch credentialed traffic or auth flows.
  if (request.headers.has("authorization")) return;
  if (url.origin === self.location.origin && url.pathname.startsWith("/api/")) return;
  if (url.hostname.endsWith(".supabase.co")) return;

  if (request.mode === "navigate") {
    event.respondWith(networkFirstPage(request));
    return;
  }

  if (isImmutableBuildAsset(url)) {
    event.respondWith(cacheFirst(request, STATIC_CACHE));
    return;
  }

  if (isStaticAsset(url) || isGoogleFont(url)) {
    event.respondWith(staleWhileRevalidate(request, ASSET_CACHE));
  }
});
