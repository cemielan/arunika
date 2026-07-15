# arunika
Arunika ingests news from many sources and turns it into structured, decision-ready intelligence — a summary, a topic, a sentiment, a market-impact score, affected sectors, and keywords — for every article, then rolls the most important ones into a daily briefing.

## Run Locally

### Prerequisites

- .NET 10 SDK
- [EF Core CLI tool](https://learn.microsoft.com/ef/core/cli/dotnet) (`dotnet tool install --global dotnet-ef`), needed for step 3
- Node.js LTS and npm
- Docker Desktop (running)
- A [Gemini API key](https://aistudio.google.com/apikey) and a [FinancialModelingPrep API key](https://site.financialmodelingprep.com/developer/docs)

### 1. Start local infrastructure (PostgreSQL and Redis)

From the repository root:

```powershell
docker-compose up -d
```

This starts:

- PostgreSQL at `localhost:5432`
- Redis at `localhost:6379`

### 2. Configure backend secrets (first time only)

In `backend/src/Arunika.Api`, set required user-secrets. This must be done **before** applying migrations in step 3, since the EF Core design-time tooling reads the connection string from the same configuration:

```powershell
cd backend\src\Arunika.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=arunika;Username=arunika;Password=devpassword"
dotnet user-secrets set "Gemini:ApiKey" "<your-gemini-api-key>"
dotnet user-secrets set "FinancialModelingPrep:ApiKey" "<your-financialmodelingprep-api-key>"
cd ..\..\..
```

### 3. Prepare backend database

From `backend/`, apply EF Core migrations:

```powershell
cd backend
dotnet ef database update --project src\Arunika.Infrastructure --startup-project src\Arunika.Api
```

### 4. Run backend API

From `backend/`:

```powershell
dotnet run --project src\Arunika.Api
```

Backend URLs:

- API: `http://localhost:5097`
- Swagger: `http://localhost:5097/swagger`
- Hangfire dashboard (development): `http://localhost:5097/hangfire`

Verify it's up:

```powershell
curl http://localhost:5097/health
```

> **Port note:** the backend must run on `5097`, not `3000` — `3000` is the frontend's port. If `dotnet run` logs `Now listening on: http://localhost:3000` instead of `5097`, another process (or a leftover `launchSettings.json` change) is misconfigured; check `backend/src/Arunika.Api/Properties/launchSettings.json` and make sure the `http` profile's `applicationUrl` is `http://localhost:5097`.
>
> A `GET /` request against the backend returning `404` is expected — the API has no root route, only `/health`, `/v1/news`, `/v1/briefing`, `/swagger`, and `/hangfire`.

### 5. Run frontend dashboard

In another terminal:

```powershell
cd frontend\arunika-dashboard
npm install
npm run dev
```

Frontend URL:

- Dashboard: `http://localhost:3000`

By default the dashboard calls the backend at `http://localhost:5097` (see `src/lib/api.ts`). Override with a `.env.local` file containing `NEXT_PUBLIC_API_BASE_URL=http://localhost:5097` if needed.

### 6. Stop services

When finished:

```powershell
docker-compose down
```

### Troubleshooting

- **Backend won't respond on `5097`**: confirm nothing else grabbed port `3000`/`5097` first (`Get-NetTCPConnection -LocalPort 3000,5097 -State Listen`), and that `launchSettings.json`'s `http` profile points at `5097` (see port note above).
- **`dotnet ef database update` fails with "Missing connection string"**: run step 2 (user-secrets) before step 3 (migrations).
- **Frontend shows an empty feed**: make sure the backend is running and reachable at the URL configured in `NEXT_PUBLIC_API_BASE_URL`/`src/lib/api.ts`, then restart `npm run dev` (Next.js caches server-fetch results).

## Deployment

The backend deploys as a Docker container (Render) and the frontend deploys as a standard Next.js app (Vercel).

### Backend on Render

1. **Create a Postgres database.** In the Render dashboard: **New > PostgreSQL**. Once created, copy the **Internal Database URL** (if the API will live in the same Render region) and note the connection details.
2. **Create a Redis instance** (optional today — the app doesn't use Redis yet, but it's provisioned locally). Skip unless/until the app depends on it.
3. **Create a Web Service** from this repository: **New > Web Service**, connect the GitHub repo, then:
   - **Root Directory**: `backend`
   - **Runtime**: `Docker` (Render auto-detects `backend/Dockerfile`, which exposes port `8080`)
   - **Instance Type**: your choice (Free tier works for testing)
4. **Set environment variables** on the Web Service (Render maps `Key__Nested` env vars to ASP.NET Core's nested configuration, matching `Section:Key` in user-secrets):
   | Key | Value |
   |---|---|
   | `ConnectionStrings__DefaultConnection` | Render Postgres connection string, e.g. `Host=<host>;Port=5432;Database=<db>;Username=<user>;Password=<password>;SSL Mode=Require;Trust Server Certificate=true` |
   | `Gemini__ApiKey` | your Gemini API key |
   | `FinancialModelingPrep__ApiKey` | your FinancialModelingPrep API key |
   | `ASPNETCORE_ENVIRONMENT` | `Production` |
5. **Deploy.** Render builds the image from `backend/Dockerfile` and starts the container; Hangfire creates its schema/tables automatically on first boot.
6. **Apply database migrations** against the Render database. Easiest from your machine, pointing at the Render Postgres's **External Database URL**:
   ```powershell
   cd backend
   dotnet ef database update --project src\Arunika.Infrastructure --startup-project src\Arunika.Api --connection "<external-connection-string>"
   ```
7. Once deployed, the API is reachable at `https://<your-service>.onrender.com`; Swagger is disabled outside Development, so use `Arunika.Api.http`/Postman/curl to validate `GET /health` and `GET /v1/news`.

### Frontend on Vercel

1. **Import the repository** into Vercel: **New Project**, select this repo.
2. **Root Directory**: `frontend/arunika-dashboard` (Vercel auto-detects the Next.js framework preset once this is set).
3. **Environment variable**: add `NEXT_PUBLIC_API_BASE_URL` set to your deployed Render backend URL, e.g. `https://<your-service>.onrender.com`.
4. **Deploy.** Vercel runs `npm install` and `npm run build` automatically.
5. Because `src/lib/api.ts` fetches happen server-side (Next.js server components/ISR), no CORS configuration is needed on the backend — the request never runs in the visitor's browser.

> Redeploy the frontend (or update the env var and redeploy) any time the Render backend URL changes.

