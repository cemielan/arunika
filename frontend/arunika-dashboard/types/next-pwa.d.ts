declare module "next-pwa" {
  import type { NextConfig } from "next";

  interface PWAOptions {
    dest?: string;
    register?: boolean;
    skipWaiting?: boolean;
    disable?: boolean;
    runtimeCaching?: Array<{
      urlPattern: RegExp | string;
      handler: string;
      options?: {
        cacheName?: string;
        expiration?: { maxEntries?: number; maxAgeSeconds?: number };
        cacheableResponse?: { statuses?: number[] };
        networkTimeoutSeconds?: number;
      };
    }>;
    [key: string]: unknown;
  }

  export default function withPWA(options?: PWAOptions): (nextConfig: NextConfig) => NextConfig;
}