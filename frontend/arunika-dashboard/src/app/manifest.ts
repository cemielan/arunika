import type { MetadataRoute } from "next";

// Served at /manifest.webmanifest by Next's metadata file convention.
export default function manifest(): MetadataRoute.Manifest {
  return {
    name: "Arunika — Market Intelligence",
    short_name: "Arunika",
    description: "Market intelligence dashboard — structured news, sentiment, and daily briefings.",
    start_url: "/",
    scope: "/",
    display: "standalone",
    orientation: "portrait-primary",
    // Splash-screen backdrop on Android. Matched to the icons' own white
    // background so the icon does not sit on a visible lighter square.
    background_color: "#ffffff",
    // The manifest allows a single theme colour with no media query, so this is
    // the light-theme surface; `viewport.themeColor` in layout.tsx supplies the
    // per-theme values that browsers actually honour at runtime.
    theme_color: "#f5f5f5",
    lang: "en",
    categories: ["finance", "business", "news"],
    icons: [
      // `any` and `maskable` are kept as separate entries: Android applies its
      // own mask to maskable icons, so the maskable art carries extra padding
      // that would look inset if it were also used as the plain icon.
      { src: "/icon-192.png", sizes: "192x192", type: "image/png", purpose: "any" },
      { src: "/icon-512.png", sizes: "512x512", type: "image/png", purpose: "any" },
      { src: "/icon-maskable-192.png", sizes: "192x192", type: "image/png", purpose: "maskable" },
      { src: "/icon-maskable-512.png", sizes: "512x512", type: "image/png", purpose: "maskable" },
    ],
    shortcuts: [
      {
        name: "Today's Briefing",
        short_name: "Briefing",
        description: "View today's market briefing",
        url: "/",
      },
      {
        name: "News Feed",
        short_name: "News",
        description: "Browse the latest market news",
        url: "/news",
      },
      {
        name: "Sector Overview",
        short_name: "Sectors",
        description: "View sector impact analysis",
        url: "/sectors",
      },
    ],
  };
}
