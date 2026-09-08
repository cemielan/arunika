# Contributing to Arunika

Thank you for your interest in contributing! This document outlines the guidelines for contributing to the Arunika project.

---

## Code of Conduct

By participating, you agree to abide by our [Code of Conduct](CODE_OF_CONDUCT.md). Please report unacceptable behavior to kadmiel@arunika.dev.

---

## How to Contribute

### Reporting Bugs

1. Check [existing issues](https://github.com/Kadmiel/arunika/issues) first
2. Open a new issue with:
   - Clear title: `[Bug] <short description>`
   - Steps to reproduce
   - Expected vs actual behavior
   - Environment (OS, .NET/Node versions, Docker)
   - Screenshots/logs if applicable

### Suggesting Features

1. Check [existing issues](https://github.com/Kadmiel/arunika/issues) and [discussions](https://github.com/Kadmiel/arunika/discussions)
2. Open a new issue with:
   - Clear title: `[Feature] <short description>`
   - Problem statement
   - Proposed solution
   - Alternatives considered
   - Mockups/examples if UI-related

### Pull Requests

1. **Fork** the repository
2. **Create a branch** from `main`:
   ```bash
   git checkout -b feat/your-feature-name
   # or
   git checkout -b fix/your-bug-fix
   ```
3. **Make changes** following the guidelines below
4. **Test locally** — ensure CI passes
5. **Submit PR** with:
   - Clear title: `feat: <description>` / `fix: <description>`
   - Description of changes
   - Link to related issue(s)
   - Screenshots for UI changes

---

## Development Setup

See [README.md](README.md#quick-start) for full setup instructions.

### Pre-commit Checks

Run before committing:

```bash
# Backend
cd backend
dotnet build
dotnet test
dotnet format --verify-no-changes

# Frontend
cd frontend/arunika-dashboard
npm run lint
npm run build
```

---

## Coding Standards

### Backend (C# / .NET 10)

- **Architecture**: Clean Architecture — Domain → Application → Infrastructure → API
- **Naming**: PascalCase for types/methods, camelCase for parameters/locals
- **Async**: `async`/`await` throughout; suffix `Async` on async methods
- **Nullability**: Enable nullable reference types; use `?` annotations
- **Dependencies**: Domain has **zero** external dependencies
- **Testing**: xUnit + FluentAssertions; integration tests use Testcontainers

#### Project Responsibilities

| Project | Responsibility | Allowed Dependencies |
|---------|----------------|---------------------|
| `Arunika.Domain` | Entities, enums, domain events | None (stdlib only) |
| `Arunika.Application` | Interfaces, DTOs, use cases, validators | Domain, MediatR, FluentValidation |
| `Arunika.Infrastructure` | EF Core, AI clients, fetchers, Hangfire, Email | Application, EF Core, Gemini, Redis, etc. |
| `Arunika.Api` | Controllers, middleware, DI, Swagger | Application, Infrastructure |

### Frontend (TypeScript / Next.js 16)

- **Router**: App Router (Server Components by default)
- **Components**: Functional + hooks; prefer Server Components
- **Styling**: Tailwind CSS 4 utilities; HeroUI for complex primitives
- **State**: React Context for auth; server state via fetch in Server Components
- **Validation**: Zod schemas in `src/lib/*-validation.ts`
- **Types**: Strict TypeScript; no `any` without `// @ts-expect-error` comment
- **Imports**: Path aliases `@/*` → `src/*`

#### Component Guidelines

```tsx
// ✅ Good: Server Component with typed props
interface ArticleCardProps {
  article: NewsListItemDto;
}

export default function ArticleCard({ article }: ArticleCardProps) {
  return <article className="...">...</article>;
}

// ✅ Good: Client Component when interactivity needed
'use client';

export default function NewsFilters({ onChange }: NewsFiltersProps) {
  const [filters, setFilters] = useState(defaultFilters);
  return <form onSubmit={...}>...</form>;
}
```

---

## Commit Message Convention

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types

| Type | Description |
|------|-------------|
| `feat` | New feature |
| `fix` | Bug fix |
| `docs` | Documentation only |
| `style` | Formatting, missing semicolons, etc. |
| `refactor` | Code change that neither fixes nor adds features |
| `perf` | Performance improvement |
| `test` | Adding/updating tests |
| `chore` | Maintenance (deps, build config, etc.) |
| `ci` | CI/CD changes |

### Examples

```
feat(api): add sector filter to GET /v1/news

fix(frontend): handle null impactScore in ArticleCard

docs(readme): update deployment steps for Render

refactor(domain): extract SectorImpact value object

test(application): add DedupeHasher unit tests

chore(deps): update EF Core to 10.0.2
```

### Scopes

- `api` — backend controllers/middleware
- `application` — use cases, services, DTOs
- `domain` — entities, enums, events
- `infrastructure` — EF Core, external clients, jobs
- `frontend` — Next.js app
- `ci` / `cd` — GitHub Actions
- `docker` — Dockerfile, docker-compose

---

## Branch Naming

| Prefix | Purpose |
|--------|---------|
| `feat/` | New feature |
| `fix/` | Bug fix |
| `docs/` | Documentation |
| `refactor/` | Refactoring |
| `test/` | Test additions |
| `chore/` | Maintenance |

Examples: `feat/daily-briefing`, `fix/duplicate-detection`, `docs/api-endpoints`

---

## Pull Request Checklist

- [ ] Branch targets `main`
- [ ] Title follows Conventional Commits
- [ ] Description explains **what** and **why**
- [ ] Linked issue(s) with `Fixes #123` or `Relates to #123`
- [ ] Backend: `dotnet build` + `dotnet test` pass
- [ ] Frontend: `npm run lint` + `npm run build` pass
- [ ] No `// TODO` or `// FIXME` without linked issue
- [ ] No commented-out code
- [ ] New code has tests (unit for logic, integration for API)
- [ ] UI changes include screenshots
- [ ] Breaking changes documented in PR description

---

## Review Process

1. **Automated checks** must pass (CI workflow)
2. **At least one approval** from maintainers
3. **No requested changes** outstanding
4. **Squash and merge** — keeps history clean

---

## Release Process

1. Maintainer creates a GitHub Release with tag `v<major>.<minor>.<patch>`
2. CD workflow deploys to production (Vercel + Render)
3. Changelog updated in `CHANGELOG.md`

---

## Questions?

- Open a [Discussion](https://github.com/Kadmiel/arunika/discussions)
- Email: kadmiel@arunika.dev

---

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).