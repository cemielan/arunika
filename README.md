# arunika
Arunika ingests news from many sources and turns it into structured, decision-ready intelligence — a summary, a topic, a sentiment, a market-impact score, affected sectors, and keywords — for every article, then rolls the most important ones into a daily briefing.

## Run Locally

### Prerequisites

- .NET 10 SDK
- Node.js LTS and npm
- Docker Desktop (running)

### 1. Start local infrastructure (PostgreSQL and Redis)

From the repository root:

```powershell
docker-compose up -d
```

This starts:

- PostgreSQL at `localhost:5432`
- Redis at `localhost:6379`

### 2. Prepare backend database

From `backend/`, apply EF Core migrations:

```powershell
cd backend
dotnet ef database update --project src\Arunika.Infrastructure --startup-project src\Arunika.Api
```

### 3. Configure backend secrets (first time only)

In `backend/src/Arunika.Api`, set required user-secrets:

```powershell
cd src\Arunika.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=arunika;Username=arunika;Password=devpassword"
dotnet user-secrets set "Gemini:ApiKey" "<your-gemini-api-key>"
cd ..\..
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

### 5. Run frontend dashboard

In another terminal:

```powershell
cd frontend\arunika-dashboard
npm install
npm run dev
```

Frontend URL:

- Dashboard: `http://localhost:3000`

### 6. Stop services

When finished:

```powershell
docker-compose down
```
