# Changelog

All notable changes to this project are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased] — Main Branch

### Added
- Initial project structure: Clean Architecture backend + Next.js frontend
- Domain entities: Article, ArticleAnalysis, Category, Sector, Briefing, User, etc.
- AI enrichment pipeline with Gemini structured output (summary, sentiment, impact, sectors, keywords)
- News collection: RSS + Financial Modeling Prep fetchers with Hangfire scheduling
- Deduplication: exact hash + title similarity (trigram)
- REST API v1: news, articles, briefing, sectors, sentiment, trending endpoints
- JWT authentication (register, login, refresh) + API key infrastructure
- Next.js 16 dashboard: Briefing, News Feed, Article Detail, Sector Overview, Auth pages
- Docker Compose for local Postgres
- GitHub Actions CI (build + test) + CD (Vercel + Render)
- Comprehensive documentation: README, DESIGN, TODO, CONTRIBUTING, SECURITY

- Admin diagnostics endpoint `GET /v1/admin/ai/budget` reporting remaining Gemini
  free-tier budget, cooldowns and success/failure counts per model
- Briefing top stories now carry summary, sentiment, category, source,
  publication time and impact rationale, so the briefing page can run excerpts
- Impact scoring rubric (`ImpactScoringRubric`), shared verbatim by every
  enrichment provider: five rated components — breadth, magnitude, surprise,
  immediacy and certainty — plus band anchors with worked examples
- "How this briefing is made" note on the briefing page, explaining in three
  steps where a briefing comes from and how often it is rebuilt

### Changed
- Briefing page redesigned as a newsletter issue: masthead, drop-cap lede with a
  market-pulse rail, a lead story, a three-column grid and a ranked tail.
  Headlines use a Playfair Display display face; body copy stays on Geist.
  Hover states, the staggered entrance and the "Why it matters" disclosures are
  CSS and native `<details>`, so the page remains a server component.
- The editorial layout now runs across the whole app — news feed, article
  detail, sector analysis, the nav bar, footer, install prompt and every auth
  screen (sign in, sign up, forgot/reset password, verify) — from one shared
  set of primitives in `src/components/editorial.tsx` and an `.editorial-*`
  layer in `globals.css`
- Decorative iconography removed throughout: nav-tab icons, category filter
  pills, sector emoji, form and modal glyphs, the sun/moon theme toggle and the
  pagination chevrons are now words or typographic rules. The only marks kept
  are the three sentiment arrows, which encode data and always sit beside their
  own label
- Nav bar active state is a rule that slides between small-caps labels instead
  of a filled pill; the theme control is a word ("Light" / "Dark")
- News feed pagination is now real `<a>` links, so pages can be opened in a new
  tab and followed without JavaScript
- Gemini free-tier budget is now tracked per model (RPM *and* RPD) from
  `Gemini:ModelQuotas`, derated by `Gemini:QuotaSafetyPercent`, instead of a
  single shared counter
- The impact score is now the sum of the five rubric components, computed in
  application code instead of chosen by the model. The components are generated
  ahead of the total (`Schema.PropertyOrdering` on Gemini, prompt order on
  OpenRouter) so the reasoning precedes the number, and `impactRationale` is
  generated after it and is now required
- Enrichment temperature pinned at 0.2, so the same article scores the same way
  on two runs

### Deprecated
- N/A

### Removed
- N/A

### Fixed
- Gemini model rotation never actually rotated — `GetNextModel` always returned
  the primary model, so one model absorbed the whole enrichment load and
  exceeded its own RPM while the rest of the chain sat idle
- `Gemini:MaxRequestsPerMinute`, `ArticlesPerRotation` and `ModelQuotas` were
  configured but never read; the rate limiter was constructed with hardcoded
  limits in `DependencyInjection`
- A quota breach on any single model tripped a one-hour cooldown across the
  whole Gemini provider; it now parks only the offending model
- Enrichment could block a Hangfire worker for up to 24 hours waiting on an
  exhausted daily window; waits are now bounded and fall through to the next model
- Retry amplification: every error was treated as retryable across 3 attempts ×
  6 models, so one bad article could burn dozens of requests. Non-transient
  failures (safety blocks, schema violations, auth errors) no longer retry
- Impact scores clustered on multiples of 5, collapsing a 100-point scale into
  roughly 20 usable values, because the only guidance the models got was
  "integer 0-100 (likely near-term market significance)"
- Impact-ordered queries had no tiebreak, so tied stories came back in whatever
  order the query plan produced and the briefing's top ten could change between
  requests over unchanged data; ties now break on publication time then id
- Impact scores and sector magnitudes went from the model to the database
  unvalidated. OpenRouter is called with `response_format: json_object`, which
  enforces no schema, so an out-of-range value could be stored verbatim
- `EnrichArticleJob` re-ran for articles that were already enriched. Because
  `SaveAnalysisAsync` is an idempotent upsert, a job replayed after a worker
  died mid-run overwrote a finished analysis; it now returns early for articles
  marked `Completed`, so existing scores are never silently rewritten under a
  newer rubric

### Security
- N/A

---

## Version History Template

### [X.Y.Z] — YYYY-MM-DD

#### Added
- Feature descriptions

#### Changed
- Changes to existing functionality

#### Deprecated
- Soon-to-be removed features

#### Removed
- Removed features

#### Fixed
- Bug fixes

#### Security
- Vulnerability fixes

---

## Release Tags

| Version | Date | Tag |
|---------|------|-----|
| 0.1.0 (MVP) | TBD | `v0.1.0` |

---

## Migration Guides

### 0.x → 1.0 (Planned)

- Stable API contracts
- Database migration strategy
- Breaking changes documented

---

## Contributors

See [GitHub Contributors](https://github.com/Kadmiel/arunika/graphs/contributors).

---

*Generated with [Keep a Changelog](https://keepachangelog.com/) principles.*