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
- Docker Compose for local Postgres + Redis
- GitHub Actions CI (build + test) + CD (Vercel + Render)
- Comprehensive documentation: README, DESIGN, TODO, CONTRIBUTING, SECURITY

### Changed
- N/A (initial release)

### Deprecated
- N/A

### Removed
- N/A

### Fixed
- N/A

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