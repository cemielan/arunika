# Security Policy

## Supported Versions

We provide security updates for the following versions:

| Version | Supported |
|---------|-----------|
| `main` branch (latest) | ✅ |
| Latest tagged release | ✅ |
| Previous major version | ⚠️ Critical only |
| Older versions | ❌ |

> **Note:** Arunika is pre-1.0. Breaking changes may occur. Pin dependencies in production.

---

## Reporting a Vulnerability

**Do not open a public issue** for security vulnerabilities.

### Private Disclosure

Email: **security@arunika.dev** (or kadmiel@arunika.dev)

Include:
- Description of the vulnerability
- Steps to reproduce / proof of concept
- Affected component(s): backend / frontend / infrastructure
- Potential impact
- Suggested fix (if any)

### Response Timeline

| Phase | Target |
|-------|--------|
| Acknowledgment | 48 hours |
| Initial assessment | 7 days |
| Fix development | 30 days (critical: 7 days) |
| Public disclosure | After fix released + 14 days |

We follow [Coordinated Vulnerability Disclosure](https://en.wikipedia.org/wiki/Coordinated_vulnerability_disclosure).

---

## Security Best Practices

### Secrets Management

- **Never commit secrets** — API keys, connection strings, JWT secrets
- **Local dev**: `dotnet user-secrets` (backend) + `.env.local` (frontend, gitignored)
- **Production**: Platform secret stores (Render env vars, Vercel env vars)
- **Rotate keys** after any suspected exposure

### Authentication & Authorization

- JWT access tokens: 15 min expiry, RS256 signing
- Refresh tokens: 7 days, httpOnly Secure SameSite=Strict cookies
- API Keys: Hashed (BCrypt) before storage; prefix `ak_` for identification
- Rate limiting: Per-key tiers (default: 60 req/min, 1,000/day)
- CORS: Dashboard origin allowlisted; public API key-gated

### Input Validation & Sanitization

- **Backend**: FluentValidation on all DTOs; EF Core parameterized queries
- **Frontend**: Zod schemas on all forms; server-side re-validation
- **Ingested content**: HTML sanitized before storage/rendering
- **File uploads**: Not supported in V1

### Data Protection

- Passwords: BCrypt (work factor 12)
- PII: Email only; no financial/PII storage
- Encryption at rest: Render Postgres (managed), Vercel (managed)
- Encryption in transit: TLS 1.2+ everywhere

### Dependency Security

- **Dependabot** enabled for npm + NuGet (see `.github/dependabot.yml`)
- **Supply chain**: Lockfiles committed (`package-lock.json`, `project.assets.json`)
- **Audit**: `dotnet list package --vulnerable` / `npm audit` in CI

### Infrastructure

- **Docker**: Non-root user, minimal base image (`mcr.microsoft.com/dotnet/aspnet:10.0`)
- **Network**: Backend not publicly exposed except via Render load balancer
- **Database**: Private networking; no public endpoint
- **Logs**: No secrets in structured logs (Serilog destructuring policies)

---

## Security Headers (Frontend)

Configured in `next.config.ts`:

```ts
const securityHeaders = [
  { key: 'X-DNS-Prefetch-Control', value: 'on' },
  { key: 'X-Content-Type-Options', value: 'nosniff' },
  { key: 'X-Frame-Options', value: 'DENY' },
  { key: 'X-XSS-Protection', value: '1; mode=block' },
  { key: 'Referrer-Policy', value: 'origin-when-cross-origin' },
  { key: 'Permissions-Policy', value: 'camera=(), microphone=(), geolocation=()' },
];
```

---

## Known Considerations

| Area | Status | Mitigation |
|------|--------|------------|
| AI prompt injection | ⚠️ Monitored | Structured output schema; input length limits |
| News content XSS | ⚠️ Monitored | Sanitization on ingest; React auto-escapes |
| Rate limit bypass | ✅ Mitigated | Per-key + IP fallback; Redis-backed |
| Token replay | ✅ Mitigated | Short access token; refresh rotation |

---

## Compliance

- **GDPR**: Email-only PII; deletion endpoint planned (V2)
- **Financial Advice Disclaimer**: Displayed in UI and API docs — Arunika is informational only
- **News Redistribution**: Confirm source terms before commercial use (see [Design Doc](Arunika-Application-Design.md#15-compliance-and-legal-considerations))

---

## Security Checklist for Contributors

- [ ] No secrets in code, config, or logs
- [ ] Input validated on both client and server
- [ ] Dependencies updated; no known vulnerabilities
- [ ] Auth checks on all protected endpoints
- [ ] Rate limiting on public endpoints
- [ ] HTTPS enforced in production
- [ ] Security headers present
- [ ] Error messages don't leak stack traces/internals

---

## Contact

Security team: **security@arunika.dev**

PGP Key: [Available on request](mailto:security@arunika.dev?subject=PGP%20Key%20Request)

---

*Last updated: 2026-09-08*