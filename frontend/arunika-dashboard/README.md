# Arunika Dashboard

> Next.js 16 frontend for the Arunika market intelligence platform.

[![Next.js](https://img.shields.io/badge/Next.js-16-black?logo=next.js)](https://nextjs.org/)
[![React](https://img.shields.io/badge/React-19-61DAFB?logo=react)](https://react.dev/)
[![Tailwind CSS](https://img.shields.io/badge/Tailwind_CSS-4-06B6D4?logo=tailwindcss)](https://tailwindcss.com/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5-3178C6?logo=typescript)](https://www.typescriptlang.org/)
[![HeroUI](https://img.shields.io/badge/HeroUI-3-purple?logo=heroui)](https://heroui.com/)

---

## Features

- **Today's Briefing** — Daily executive summary with top stories, sentiment, risk level
- **News Feed** — Filterable, paginated article list (category, sentiment, sector, date)
- **Article Detail** — Full enrichment: summary, impact rationale, sector breakdown, keywords
- **Sector Overview** — Real-time sector impact snapshot
- **Authentication** — Register, login, forgot/reset password (JWT + refresh tokens)
- **Dark/Light Mode** — System-aware with manual toggle
- **Responsive / PWA-Ready** — Works on mobile, installable

---

## Tech Stack

| Category | Technology |
|----------|------------|
| Framework | Next.js 16 (App Router) |
| UI Library | HeroUI v3 + Tailwind CSS 4 |
| State/Forms | React 19, React Hook Form, Zod |
| Auth | JWT (httpOnly cookies) + Supabase client |
| Animations | Framer Motion |
| Icons | Lucide React |
| Date/Time | date-fns / native Intl |
| Linting | ESLint 9 (Next.js config) |

---

## Project Structure

```
frontend/arunika-dashboard/
├── public/                    # Static assets
├── src/
│   ├── app/                   # App Router pages
│   │   ├── page.tsx           # Home → Today's Briefing
│   │   ├── news/
│   │   │   ├── page.tsx       # News Feed
│   │   │   └── [id]/page.tsx  # Article Detail
│   │   ├── sectors/page.tsx   # Sector Overview
│   │   ├── login/page.tsx
│   │   ├── register/page.tsx
│   │   ├── forgot-password/page.tsx
│   │   ├── reset-password/page.tsx
│   │   ├── verify/page.tsx
│   │   ├── layout.tsx         # Root layout + providers
│   │   ├── globals.css        # Tailwind + global styles
│   │   └── loading.tsx        # Route loading UI
│   ├── components/            # Reusable UI components
│   │   ├── NavBar.tsx
│   │   ├── NewsFilters.tsx
│   │   ├── NewsPagination.tsx
│   │   ├── MarketPulse.tsx
│   │   ├── ThemeSwitch.tsx
│   │   ├── Footer.tsx
│   │   └── badges.tsx         # Sentiment/Impact/Sector badges
│   ├── hooks/
│   │   └── useSpringVector.ts
│   └── lib/                   # Utilities & clients
│       ├── api.ts             # Typed API client (fetch wrapper)
│       ├── auth-context.tsx   # Auth state (React Context)
│       ├── auth-validation.ts # Zod schemas for auth forms
│       ├── formatDate.ts      # Date formatting helpers
│       ├── newsHref.ts        # URL builders for news routes
│       └── supabase.ts        # Supabase client (email edge functions)
├── .env.local                 # Local env (gitignored)
├── next.config.ts
├── tailwind.config.ts
├── tsconfig.json
├── package.json
└── README.md
```

---

## Getting Started

### Prerequisites

- Node.js 20+ LTS
- npm / pnpm / yarn
- Backend API running at `http://localhost:5097` (see root README)

### Install & Run

```bash
cd frontend/arunika-dashboard

# Install dependencies
npm install

# Development server (http://localhost:3000)
npm run dev

# Production build
npm run build

# Start production server
npm start

# Lint
npm run lint
```

### Environment Variables

Create `.env.local` (copy from `.env.example` if present):

```env
# Required
NEXT_PUBLIC_API_BASE_URL=http://localhost:5097

# Optional: Supabase (for email verification / password reset)
NEXT_PUBLIC_SUPABASE_URL=https://<project>.supabase.co
NEXT_PUBLIC_SUPABASE_ANON_KEY=<anon-key>
```

> **Note:** The dashboard uses **server-side fetching** (Next.js Server Components / ISR) for the briefing and news feed. No CORS configuration needed on the backend — requests never run in the visitor's browser.

---

## Key Implementation Details

### API Client (`src/lib/api.ts`)

- Typed fetch wrapper with automatic JWT handling
- Reads access token from httpOnly cookie (set by backend)
- Auto-refresh on 401 via `/v1/auth/refresh`
- Centralized error handling with typed `ApiError`

### Authentication (`src/lib/auth-context.tsx`)

- React Context providing `user`, `login()`, `logout()`, `refresh()`
- Token stored in memory; refresh token in httpOnly cookie
- Hydrates on app load via `/v1/auth/me`

### UI Components

- **HeroUI** for accessible, styled primitives (Button, Card, Table, Modal, etc.)
- **Tailwind CSS 4** for utility styling — no custom CSS needed
- **Framer Motion** for page transitions and micro-interactions
- **Lucide React** for consistent iconography

### Pages Overview

| Route | Description | Data Fetching |
|-------|-------------|---------------|
| `/` | Today's Briefing | Server Component (SSG/ISR) |
| `/news` | Filterable News Feed | Server Component + search params |
| `/news/[id]` | Article Detail | Server Component |
| `/sectors` | Sector Impact | Server Component |
| `/login` `/register` | Auth forms | Client Components |
| `/forgot-password` `/reset-password` | Password flow | Client Components |

---

## Deployment (Vercel)

1. Push to GitHub
2. Import in Vercel → **Root Directory: `frontend/arunika-dashboard`**
3. Add Environment Variable:
   - `NEXT_PUBLIC_API_BASE_URL` → your Render backend URL
   - (Optional) Supabase vars for auth emails
4. Deploy — Vercel auto-detects Next.js and runs `npm run build`

### Preview Deployments

Every push to a branch creates a Preview Deployment on Vercel. Share the URL for review.

---

## Mobile / PWA

### As a PWA (Implemented)

The dashboard already ships as an installable Progressive Web App. No extra
dependency is involved: `next-pwa` is webpack-based and never runs under Next
16's Turbopack builds, so the service worker is hand-written.

| Piece | Location |
| --- | --- |
| Manifest (`/manifest.webmanifest`) | `src/app/manifest.ts` |
| Service worker | `public/sw.js` |
| Registration | `src/components/ServiceWorkerRegistrar.tsx` |
| Install prompt (Android card / iOS instructions) | `src/components/InstallPrompt.tsx` |
| Offline fallback | `public/offline.html` |
| Icons (standard, maskable, Apple touch) | `public/icon-*.png`, `public/apple-touch-icon.png` — rendered from `public/logo-dark.svg` |
| Theme-adaptive tab favicon | `src/app/icon.svg` |

Two things to know when working on it:

- The worker only registers in production builds (`npm run build && npm start`),
  so `next dev` is never served from a stale cache.
- API and Supabase responses are deliberately never cached, because Cache
  Storage is unencrypted and outlives sign-out.
- Installed home-screen icons cannot follow the OS theme on either platform, so
  they are fixed as the black logo on white. Only the tab favicon adapts. Regenerate
  the PNGs from `logo-dark.svg` if the logo changes — see the guide for sizes.

Install steps for Android and iOS, testing instructions, and troubleshooting are
in [`Arunika-PWA-Guide.md`](../../Arunika-PWA-Guide.md).

### As Native Apps (Capacitor)

```bash
npm install @capacitor/core @capacitor/cli
npm install @capacitor/android @capacitor/ios
npx cap init arunika com.arunika.app
npx cap add android ios
npm run build && npx cap sync
```

- Android: `npx cap open android` → Android Studio
- iOS: `npx cap open ios` → Xcode

**No separate repo needed** — Capacitor config lives in `frontend/arunika-dashboard/`.

---

## Scripts Reference

| Command | Description |
|---------|-------------|
| `npm run dev` | Start dev server with Turbopack |
| `npm run build` | Production build |
| `npm start` | Run production server |
| `npm run lint` | Run ESLint |

---

## Contributing

See root [CONTRIBUTING.md](../CONTRIBUTING.md).

---

## License

MIT — see root [LICENSE](../LICENSE).