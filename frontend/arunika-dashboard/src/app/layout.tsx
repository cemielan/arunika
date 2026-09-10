import type { Metadata, Viewport } from "next";
import { Geist, Geist_Mono, Playfair_Display } from "next/font/google";
import { NavBar } from "@/components/NavBar";
import { Footer } from "@/components/Footer";
import { AuthProvider, NotificationBanner } from "@/lib/auth-context";
import { InstallPrompt } from "@/components/InstallPrompt";
import { ServiceWorkerRegistrar } from "@/components/ServiceWorkerRegistrar";
import "./globals.css";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

// Display face for the briefing's editorial layout — headlines, the masthead
// and section rules only. Body copy stays on Geist, which holds up far better
// at small sizes and on low-DPI screens than a high-contrast serif does.
const playfair = Playfair_Display({
  variable: "--font-serif",
  subsets: ["latin"],
  display: "swap",
});

export const metadata: Metadata = {
  title: "Arunika",
  description: "The First Light of Market Intelligence.",
  applicationName: "Arunika",
  manifest: "/manifest.webmanifest",
  appleWebApp: {
    // iOS has no manifest support, so standalone display, the title under the
    // home-screen icon, and the status bar style all come from these tags.
    capable: true,
    title: "Arunika",
    statusBarStyle: "default",
  },
  icons: {
    // Only the SVG is offered as the tab favicon: it is the one icon that can
    // follow the OS theme. Listing the PNGs here too would let browsers pick a
    // fixed white-background raster over the adaptive mark.
    icon: [{ url: "/icon.svg", type: "image/svg+xml" }],
    // iOS ignores SVG and manifest icons; it reads apple-touch-icon only.
    apple: [{ url: "/apple-touch-icon.png", sizes: "180x180", type: "image/png" }],
  },
  other: {
    // Next emits only the modern `mobile-web-app-capable`. iOS 16.4+ takes
    // standalone display from the manifest, but older iOS reads nothing but
    // this legacy Apple tag, so keep it for those devices.
    "apple-mobile-web-app-capable": "yes",
  },
};

export const viewport: Viewport = {
  // Tints the Android toolbar and iOS status bar. These are the app's own
  // surface colours, so the system chrome follows the active theme instead of
  // sitting on a fixed colour.
  themeColor: [
    { media: "(prefers-color-scheme: light)", color: "#f5f5f5" },
    { media: "(prefers-color-scheme: dark)", color: "#060607" },
  ],
  // Let the installed app draw behind the iOS status bar / Android system bars.
  viewportFit: "cover",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html
      lang="en"
      suppressHydrationWarning
      className={`${geistSans.variable} ${geistMono.variable} ${playfair.variable} h-full antialiased`}
    >
      <body className="flex min-h-full flex-col bg-background text-foreground">
        <AuthProvider>
          <NavBar />
          <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-6 sm:px-6 sm:py-8">{children}</main>
          <Footer />
          <NotificationBanner />
          <InstallPrompt />
          <ServiceWorkerRegistrar />
        </AuthProvider>
      </body>
    </html>
  );
}
