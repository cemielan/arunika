# Arunika — Project To-Do List

A phase-by-phase build plan, starting from an empty folder. Phases 0–8 get you to a
working V1/MVP; phases 9–11 map onto the V2–V4 roadmap from the original brief.
Check items off as you go — nothing here assumes prior setup.

---

## If you only do 10 things this week

1. [ ] Install .NET 10 SDK, Node.js LTS, Docker Desktop.
2. [ ] Create the GitHub repo and push an empty solution.
3. [ ] Get API keys: one AI provider (OpenAI or Anthropic) and one news/market
   data source (see Phase 0).
4. [ ] Get `docker-compose up` running Postgres + Redis locally.
5. [ ] Define the `Article` and `ArticleAnalysis` entities and run your first
   EF Core migration.
6. [ ] Pull 10 real articles from one source into the database (no AI yet).
7. [ ] Get one article through the AI enrichment call end-to-end, by hand.
8. [ ] Stand up `GET /v1/news` returning real rows from the database.
9. [ ] Render that same list on a bare Next.js page.
10. [ ] Write down today's date and re-read Phase 0's licensing note before you
    start pulling news at volume.

Everything below is the same plan broken out in full.

---

## Phase 0 — Foundations (before writing code)

- [x] Install .NET 10 SDK (current LTS, supported through Nov 2028).
- [x] Install Node.js LTS + a package manager (npm or pnpm).
- [x] Install Docker Desktop.
- [x] Install your IDE of choice (Visual Studio / VS Code + C# Dev Kit / Rider) and
  the Next.js/React tooling for the frontend side.
- [x] Create the GitHub repository (monorepo — see design doc §12) with a
  README, `.gitignore` (Visual Studio + Node templates), and branch protection
  on `main`.
- [x] Decide and register for **one AI provider** to start: OpenAI or Anthropic.
  Get an API key, set a spending/usage cap while testing.
- [x] Decide and register for **one or two news/market-data sources** to start.
  Reasonable first picks: a general RSS aggregator plus one finance-specific API
  (Finnhub or Financial Modeling Prep both bundle news with sentiment/fundamentals).
  Read the commercial-use terms of the free tier before committing.
- [x] Create an Azure account/resource group if you already know you'll deploy
  there — this can also wait until Phase 8.
- [x] Set up a project board (GitHub Projects, Linear, or similar) and copy this
  list into it as your first set of cards.

## Phase 1 — Project scaffolding

- [x] `dotnet new sln` and create the four projects from the design doc:
  `Arunika.Domain`, `Arunika.Application`, `Arunika.Infrastructure`, `Arunika.Api`.
  Add project references: Api → Application + Infrastructure; Infrastructure →
  Application; Application → Domain.
- [x] `npx create-next-app` for `frontend/arunika-dashboard` (TypeScript,
  Tailwind, App Router).
- [x] Write `docker-compose.yml` for Postgres 16 + Redis 7 (see design doc §12);
  confirm `docker-compose up` works and you can connect with a DB client.
- [x] Set up secrets: `dotnet user-secrets init` in `Arunika.Api`; add a
  `.env.local` (git-ignored) for the frontend.
- [x] Add Swagger/OpenAPI to `Arunika.Api`; confirm `/swagger` loads on an empty
  API.
- [x] Add Serilog with a console sink; confirm structured logs appear on startup.
- [x] Add a `GET /health` endpoint.
- [x] Add a GitHub Actions workflow that builds the solution and runs `dotnet
  test` on every pull request.

## Phase 2 — Domain and database

- [x] Define the entities from design doc §4: `NewsSource`, `Article`,
  `ArticleAnalysis`, `Category`, `Sector`, `ArticleSectorImpact`, `Keyword`,
  `ArticleKeyword`, `Briefing`, `BriefingItem`.
- [x] Add the EF Core `DbContext` in Infrastructure; configure the
  `Article.Url` unique index and the `Article.PublishedAt` index.
- [x] Create and apply the first migration against your local `docker-compose`
  Postgres instance.
- [x] Seed `Category` (Politics, Economy, Markets, Banking, Technology,
  Commodities, Crypto) and `Sector` (Energy, Financials, Technology,
  Industrials, Consumer, Healthcare, Real Estate, Materials, Utilities,
  Communication Services).
- [x] Define repository/service interfaces in Application
  (`IArticleRepository`, `INewsFetcher`, `IAiEnrichmentService`) — no
  implementations yet, just the contracts the rest of the app codes against.

## Phase 3 — News collection (V1)

- [x] Implement a fetcher for your first source (RSS parser, or your chosen
  API's client) behind `INewsFetcher`.
- [x] Map fetched items into `Article` (title, url, raw content, published date,
  source).
- [x] Persist new articles; skip re-inserting ones that already exist by URL.
- [x] Add `FetchNewsJob` as a Hangfire recurring job (start with every 15 min).
- [x] Add basic error handling: a failed source shouldn't take down the whole
  job — log and continue to the next source.
- [x] Add your second source once the first is reliably flowing end-to-end.

## Phase 4 — Deduplication (pulled forward from V2 — do it before AI calls)

> The original roadmap put deduplication in V2, but doing it before you're
> paying for AI calls on every duplicate saves real money from day one — worth
> building a simple version now even if you formalize it later.

- [x] Compute and store a `dedupe_hash` (normalized title + source) on ingest.
- [x] Add an exact-match check against recent articles (last ~48h) before
  enrichment.
- [x] Add a simple title-similarity check (trigram similarity in Postgres is
  enough to start) to catch near-duplicates across outlets.
- [x] Link duplicates via `duplicate_of_id`; exclude them from feeds/briefings
  by default.

## Phase 5 — AI enrichment pipeline (V1: full structured output)

- [x] Build a thin client wrapper for your AI provider behind
  `IAiEnrichmentService`.
- [x] Implement the structured-output schema from design doc §5 (summary,
  category, sentiment, impact score, sector impact, keywords) as one call.
- [x] Parse the response into `ArticleAnalysis` and related rows
  (`ArticleSectorImpact`, `ArticleKeyword`).
- [x] Add retry with backoff on transient failures; mark
  `EnrichmentStatus = Failed` after repeated failure rather than blocking the
  pipeline.
- [x] Log token usage per call from the start.
- [x] Wire `EnrichArticleJob` to fire after a unique (non-duplicate) article is
  saved.

## Phase 6 — REST API (V1)

- [x] `GET /v1/news` — list with `category`, `page`, `pageSize` filters.
- [x] `GET /v1/articles/{id}` — full detail.
- [x] `GET /v1/briefing` — even a simple version (e.g. top N by impact score
  today, no generated executive summary yet) is fine to start.
- [x] Standardize the response envelope and error shape (design doc §7) across
  every endpoint now, before more endpoints exist to retrofit.
- [x] Flesh out Swagger with example requests/responses.
- [x] Write integration tests for each endpoint against a test database.

## Phase 7 — Dashboard (V1)

- [x] Build the Home page (briefing) and News Feed page, fetching from the
  REST API.
- [x] Build the Article Detail page.
- [x] Basic Tailwind styling — don't over-invest in a design system yet.
- [ ] Deploy a preview build (Vercel, or Azure Static Web Apps) so you can share
  progress without a full production deploy.

## Phase 8 — Testing, deployment, and V1 launch

- [x] Unit tests for dedup matching and any impact-score logic that isn't just
  "trust the model."
- [x] Manual QA pass through the dashboard on a real day's news.
- [ ] Provision hosting (decided against Azure — see design doc §16): Vercel
  project for the frontend, Render web service + managed PostgreSQL + managed
  Redis for the backend.
- [x] Add the GitHub Actions CD workflow: deploy to staging on merge to `main`,
  to production on a tagged release (`.github/workflows/cd.yml`, targets
  Vercel + Render — deploy steps skip with a warning until the corresponding
  secrets are configured; no resources provisioned yet).
- [x] Move secrets into Render/Vercel environment variables (encrypted at rest
  by the platform); confirm nothing sensitive is in `appsettings.json` or
  committed anywhere.
- [x] Set up production error/telemetry visibility (e.g. Render's built-in
  logs/metrics, or a lightweight APM like Sentry) and confirm you're seeing
  real request telemetry.
- [x] Smoke-test in production, then call V1 done.

Supabase migration (run from `backend/`). The connection string lives in the
`SUPABASE_DB_CONNECTION` environment variable, never in this file:

```powershell
dotnet ef database update --project src\Arunika.Infrastructure --startup-project src\Arunika.Api --connection $env:SUPABASE_DB_CONNECTION
```

Connection string shapes (fill the password from the Supabase dashboard under
Settings -> Database; do not paste the real value into a tracked file):

Pooler (use for the running app — Port 6543, transaction pooling):
Host=aws-0-<region>.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.<project-ref>;Password=<password>;SSL Mode=Require;Trust Server Certificate=true;GSS Encryption Mode=Disable

Direct (use for EF migrations — Port 5432):
Host=db.<project-ref>.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=<password>;SSL Mode=Require;Trust Server Certificate=true

---

## Phase 9 — V2: auth, categories, search, daily briefing automation

- [x] JWT auth: register/login/refresh endpoints, password hashing.
- [x] Category and sector filters fully wired through API + dashboard.
- [x] Full-text search (`tsvector` on `title`/`raw_content` to start; consider
  Meilisearch/Elasticsearch only if Postgres search stops being fast enough).
- [x] Automate `GenerateDailyBriefingJob`: select top stories by impact score,
  generate the executive summary via the AI pipeline, cache the result.
- [x] Add a login/account page to the dashboard.

## Phase 10 — V3: sentiment/impact refinement, sector analysis, email digest

- [x] Review real sentiment/impact output against a few weeks of production
  data; tune the prompt/schema if scores cluster oddly. Scores were clustering
  on multiples of 5; replaced the one-line description with a shared five-component
  rubric (`ImpactScoringRubric`, design doc §5) whose total is summed in code.
  Articles enriched before the rubric landed keep their old scores — decided
  deliberately, rather than spending API quota on a backfill. Only new and
  previously-failed articles are scored under the rubric, so the two scales mix
  until the pre-rubric articles age out of the 7-day briefing window.
- [ ] Build the sector-aggregation view (`/v1/sectors`) and dashboard page.
- [ ] Integrate an email provider (e.g. SendGrid); build daily/weekly digest
  templates.
- [ ] Add `SendEmailDigestJob`.

## Phase 11 — V4: personalized alerts, historical analytics, SDKs

- [ ] Watchlist model: users subscribe to a keyword, sector, or category.
- [ ] `EvaluateAlertsJob`: match new enriched articles against active
  watchlists, trigger notifications (email first; push later).
- [ ] Historical analytics: trend charts over time (sentiment by sector over
  weeks/months) — this is where the time-series shape of the schema starts to
  pay off.
- [ ] Public SDKs (JS/TypeScript and Python clients) wrapping the versioned REST
  API for third-party developers.
- [ ] API key self-service (developer portal) if you haven't needed one before
  now.

---

## Notes on pacing

Phases 0–8 are a realistic MVP for a small team or solo builder — rough order of
magnitude is 4–8 weeks depending on how much of the frontend polish you defer.
Treat that as a planning input, not a promise: the AI enrichment and
deduplication steps (Phases 4–5) are usually where first-time estimates run
short, since they're the parts that need real production data to tune properly.
