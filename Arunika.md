# Arunika

**Tagline:** *The First Light of Market Intelligence.*

## Philosophy

**Arunika** is an Indonesian word describing the first rays of sunlight
after sunrise. Symbolically, it represents hope, clarity, and a new
beginning.

For this project, Arunika represents the first clear understanding of
the world's most important political, economic, and financial events
before the market fully reacts.

> Every sunrise begins with understanding.

------------------------------------------------------------------------

# Vision

Arunika is an AI-powered market intelligence platform.

Instead of overwhelming users with hundreds of headlines, it transforms
global news into concise, actionable intelligence.

It answers three questions:

1.  What happened?
2.  Why does it matter?
3.  How could it affect today's markets?

------------------------------------------------------------------------

# Core Features

-   Aggregate news from multiple trusted sources.
-   Remove duplicate stories.
-   AI-generated summaries.
-   Topic classification (Politics, Economy, Markets, Banking,
    Technology, Commodities, Crypto).
-   Market sentiment analysis (Bullish, Bearish, Neutral).
-   Impact scoring.
-   Sector impact analysis.
-   Daily morning briefing.
-   REST API for developers.
-   Dashboard for end users.

------------------------------------------------------------------------

# High-Level Architecture

``` text
News Sources
    │
    ▼
News Collector
    │
    ▼
Deduplication
    │
    ▼
AI Pipeline
 ├── Summary
 ├── Classification
 ├── Sentiment
 ├── Impact Score
 └── Keywords
    │
    ▼
Database
    │
 ┌──┴─────────┐
 ▼            ▼
REST API   Dashboard
```

------------------------------------------------------------------------

# Example Workflow

Input headlines:

-   Fed keeps interest rates unchanged.
-   Oil prices rise 4%.
-   Japan GDP slows.
-   Tesla beats earnings.
-   Indonesia changes export policy.

Output:

## Morning Brief

-   Executive summary.
-   Top market-moving events.
-   Overall market sentiment.
-   Sector impact.
-   Risk level.
-   Suggested watchlist.

------------------------------------------------------------------------

# Public REST API

-   GET /v1/news
-   GET /v1/briefing
-   GET /v1/sentiment
-   GET /v1/sectors
-   GET /v1/trending
-   GET /v1/articles/{id}

------------------------------------------------------------------------

# Suggested Technology Stack

  Layer               Technology
  ------------------- --------------------------
  Backend             ASP.NET Core Web API
  Database            PostgreSQL or SQL Server
  ORM                 Entity Framework Core
  Scheduler           Hangfire
  AI                  OpenAI API
  Cache               Redis
  Frontend            React + Next.js
  Authentication      JWT
  API Documentation   Swagger/OpenAPI
  Deployment          Azure
  CI/CD               GitHub Actions
  Logging             Serilog

------------------------------------------------------------------------

# Development Roadmap

## Version 1 (MVP)

-   News collection
-   Database
-   AI summary
-   REST API
-   Simple dashboard

## Version 2

-   Deduplication
-   Authentication
-   Categories
-   Search
-   Daily briefing

## Version 3

-   Sentiment analysis
-   Impact score
-   Sector analysis
-   Email digest

## Version 4

-   Personalized alerts
-   Historical analytics
-   SDKs for developers

------------------------------------------------------------------------

# Long-Term Vision

Arunika is more than a news summarizer.

It is a market intelligence platform that provides the first clear light
of understanding before investors, developers, and businesses begin
their day.

**Mission:** Transform information overload into actionable insight.

**Tagline:** *The First Light of Market Intelligence.*
