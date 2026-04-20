# BudgetBuddy API - Modular Monolith

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16+-336791?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

Personal finance management REST API for tracking transactions, budgets, investments, and generating financial reports. Built as a **Modular Monolith** — each domain is a fully isolated module with its own database schema, DbContext, and internal vertical slices, deployed as a single process.

## Architecture

This project uses a **Modular Monolith** pattern: the application ships as one deployable unit, but the codebase is split into independent modules. Each module:

- owns its own PostgreSQL schema (e.g. `accounts`, `transactions`, `budgets`)
- has its own `DbContext` with schema-isolated migrations
- exposes its own endpoints via Carter `ICarterModule`
- communicates with other modules through shared contracts (`IAccountBalanceService`, `IInvestmentCalculationService`, etc.) — never through direct DbContext access across boundaries
- uses CQRS internally (MediatR commands/queries per feature slice)

Cross-cutting concerns (security, audit, caching, observability, background jobs) live in `Shared.*` assemblies and are injected into modules.

## Modules

| Module | Schema | Responsibility |
|--------|--------|----------------|
| `Auth` | `auth` | Identity, JWT, 2FA, refresh tokens, security events, audit trail, user settings |
| `Accounts` | `accounts` | Multi-currency account management and balance tracking |
| `Transactions` | `transactions` | Income/expense/transfer tracking, import/export, batch ops, outbox |
| `Budgets` | `budgets` | Monthly budget planning, spending tracking, budget alerts |
| `Investments` | `investments` | Portfolio management, price snapshots, FX snapshots, market data |
| `Analytics` | _(read-only)_ | Dashboard, reports (income/expense, spending by category, monthly summary, investment performance) |
| `ReferenceData` | `referencedata` | Currencies, categories, category types |

## Tech Stack

**Framework & Runtime:**
- .NET 10.0
- C# 13 with Nullable Reference Types

**Database & Caching:**
- PostgreSQL 16+ with Row-Level Security (RLS) — each module in its own schema
- Entity Framework Core 10.0 — one `DbContext` per module + shared `AppDbContext`
- Npgsql with NodaTime support
- Redis 7+ for distributed caching and token blacklist

**Architecture & Patterns:**
- Modular Monolith with schema-per-module isolation
- CQRS with MediatR 12.4 (commands/queries within each module slice)
- Direct DbContext usage (no repository abstraction)
- Minimal APIs with Carter 8.2
- Domain Events via Transactional Outbox (Transactions module)

**Key Libraries:**
- **Mapping:** Mapster 7.4 (fast object-to-object mapping)
- **Validation:** FluentValidation 11.9
- **Date/Time:** NodaTime 3.2 (LocalDate, Instant) + IClock for testability
- **Authentication:** ASP.NET Core Identity with 2FA, QRCoder 1.7
- **Email:** MailKit (SMTP, HTML/plain text, attachments)
- **Logging:** Serilog (Console, File, Seq) with PII masking
- **Observability:** OpenTelemetry with OTLP exporter (Jaeger) + Prometheus
- **API Documentation:** Scalar 1.2
- **Security:** NetEscapades.AspNetCore.SecurityHeaders, nClam 9.0, Data Protection API
- **Caching:** Redis (StackExchange.Redis) for token blacklist and distributed cache
- **Excel/CSV:** ClosedXML 0.104 for import/export, custom CSV service
- **Bulk Operations:** EFCore.BulkExtensions 10.0 (batch inserts/updates)
- **Background Jobs:** Quartz.NET (cron-scheduled jobs)
- **Rate Limiting:** Built-in ASP.NET Core rate limiter (fixed window, sliding window, token bucket)
- **Azure:** Key Vault integration for production secrets and encryption keys

## Features

### Core Financial Features

- **Accounts:** Multi-currency account management with real-time balance calculation
- **Transactions:** Income/expense/transfer tracking with categories, labels, payees
- **Transfers:** Account-to-account money transfers (creates paired transactions automatically)
- **Budgets:** Monthly budget planning with spending tracking and budget-vs-actual comparison
- **Budget Alerts:** Automatic threshold notifications — Safe (<80%), Warning (80–99%), Exceeded (≥100%) with multi-currency conversion
- **Categories:** User-scoped category management with icon and color
- **Category Types:** Sub-level category classification
- **Currencies:** Global currency master data (code, symbol, name)
- **Investments:** Portfolio tracking for Stocks, ETFs, Crypto, Bonds, Mutual Funds
- **Reports:** Income vs. expense, spending by category, monthly summaries, investment performance
- **Dashboard:** Real-time financial overview aggregating all module data in one call
- **User Settings:** Per-user preferences (default currency, language, date format)

### Data Operations

- **Import:** Excel (XLSX) and CSV transaction import with row-level error reporting and duplicate detection
- **Export:** Excel and CSV export for transactions and investments
- **Batch Delete:** Delete multiple transactions or investments in one request
- **Batch Update:** Update category/labels on multiple transactions at once

### Domain Events & Outbox

The Transactions module publishes domain events (e.g. `TransactionCreated`) via a **Transactional Outbox**. The outbox processor (`TransactionsOutboxProcessorJob`) runs every 30 seconds, picks up unprocessed messages, dispatches them to MediatR, and marks them as processed. This enables reliable cross-module event delivery without distributed transactions.

### Market Data & Financial Providers

**Live Price Data:**
- **Yahoo Finance** — Stock and ETF prices (no API key required)
- **CoinGecko** — Cryptocurrency prices (optional API key for higher rate limits)
- Prices cached for 15 minutes; fallback to last known price on provider failure

**Foreign Exchange Rates:**
- **Frankfurter** (ECB data) — Real-time and historical FX rates for 30+ currencies
- Rates cached for 4 hours

**Historical Data Backfill:**
- On startup, the app automatically detects and fills gaps in price and FX snapshots
- Covers the full history from each user's earliest investment or transaction date

### Background Jobs (Quartz.NET)

| Job | Schedule (UTC) | Description |
|-----|----------------|-------------|
| `DailyPriceSnapshotJob` | 22:00 | Fetches closing prices for all tracked investment symbols |
| `DailyFxSnapshotJob` | 16:00 | Fetches ECB exchange rates and saves FX snapshots |
| `BackfillMarketDataJob` | 03:00 | Detects and fills historical price/FX gaps |
| `BudgetAlertJob` | 08:00 | Evaluates all budgets and generates alert summaries |
| `TransactionsOutboxProcessorJob` | Every 30s | Dispatches pending domain events from the outbox |

All jobs support individual enable/disable via `BackgroundJobs` config. Cron expressions are fully configurable.

### Security Features

**Authentication & Authorization:**
- JWT token authentication with automatic rotation (15 min access / 7 day refresh)
- Token blacklist with Redis for instant revocation
- Two-Factor Authentication (2FA) with TOTP and recovery codes
- 2FA enforcement for Admin role with brute force protection (5 attempts / 15 min)
- Role-Based Access Control (Admin / User / Premium policies)
- Batched recovery code distribution (5+5 codes)

**Data Protection:**
- Connection string encryption with Data Protection API + Azure Key Vault
- SSL/TLS encryption for database connections (SSL Mode=Require in production)
- Column-level encryption for sensitive fields (Transaction.Payee, Transaction.Note)
- PostgreSQL Row-Level Security (RLS) for database-level row isolation per user — enforced via EF Core interceptors at the connection level
- PII masking in logs (email, IP, GUID automatic masking)

**Security Monitoring:**
- 20+ security event types tracked: login, logout, 2FA, token operations, etc.
- Real-time security alerts with brute force detection
- Comprehensive audit trail with before/after change tracking on all entities
- Automatic audit logging for all entity modifications via `AuditLogInterceptor`

**File Security:**
- ClamAV antivirus integration for file upload scanning
- CSRF protection for all endpoints including file uploads
- IP-based rate limiting (300–500 req/min depending on tier)

**Compliance:**
- GDPR ready (Article 30: Audit trail, Article 32: Encryption, Article 33: Breach alerts)
- SOC 2 compliant (Logging, access control, change management)
- PCI-DSS aligned (Card masking, audit trail, 2FA + RBAC)
- OWASP Top 10 (2021) protections implemented

### Observability

- **OpenTelemetry:** Distributed tracing with OTLP exporter (Jaeger compatible)
- **Prometheus:** Metrics at `/metrics` (ASP.NET Core, Kestrel, HTTP client, runtime)
- **Serilog:** Structured logging to Console, rolling File, and Seq
- **Health Checks:**
  - `/health` — Overall application health
  - `/health/ready` — Readiness probe (PostgreSQL + Redis connectivity)
  - `/health/live` — Liveness probe (no external dependency check)
  - `/metrics` — Prometheus scrape endpoint

## Project Structure

```
BudgetBuddy.API.ModularMonolith.sln
│
├── Host/
│   └── BudgetBuddy.API.ModularMonolith/       # Entry point — wires all modules together
│       ├── Program.cs                          # DI registration, middleware pipeline, startup migrations
│       └── Seeders/                            # Demo data seeder (dev only, --seed flag)
│
├── Modules/
│   ├── BudgetBuddy.Module.Auth/                # schema: auth
│   │   ├── Features/
│   │   │   ├── Authentication/                 # Login, Logout, RefreshToken
│   │   │   ├── TwoFactor/                      # Enable/Disable/Verify 2FA, RecoveryCodes
│   │   │   ├── Security/                       # SecurityEvents, SecurityAlerts (Admin)
│   │   │   ├── Audit/                          # Entity audit history (Admin)
│   │   │   └── UserSettings/                   # Get/Update user preferences
│   │   ├── Persistence/
│   │   │   ├── AuthDbContext.cs
│   │   │   ├── Migrations/
│   │   │   └── Seeders/                        # RoleSeeder, AdminUserSeeder
│   │   └── AuthModule.cs
│   │
│   ├── BudgetBuddy.Module.Accounts/            # schema: accounts
│   │   ├── Features/
│   │   │   └── Accounts/                       # CRUD + GetAccountBalance
│   │   ├── Persistence/
│   │   │   ├── AccountsDbContext.cs
│   │   │   └── Migrations/
│   │   └── AccountsModule.cs
│   │
│   ├── BudgetBuddy.Module.Transactions/        # schema: transactions
│   │   ├── Features/
│   │   │   ├── Transactions/                   # CRUD, BatchDelete, BatchUpdate, Import, Export
│   │   │   └── Transfers/                      # CreateTransfer (paired transaction logic)
│   │   ├── Persistence/
│   │   │   ├── TransactionsDbContext.cs
│   │   │   └── Migrations/                     # Includes OutboxMessages table
│   │   └── TransactionsModule.cs
│   │
│   ├── BudgetBuddy.Module.Budgets/             # schema: budgets
│   │   ├── Features/
│   │   │   ├── Budgets/                        # CRUD, GetBudgetVsActual
│   │   │   └── BudgetAlerts/                   # GetBudgetAlerts + BudgetAlertJob
│   │   ├── Persistence/
│   │   │   ├── BudgetsDbContext.cs
│   │   │   └── Migrations/
│   │   └── BudgetsModule.cs
│   │
│   ├── BudgetBuddy.Module.Investments/         # schema: investments
│   │   ├── Features/
│   │   │   ├── Investments/                    # CRUD, BatchDelete, Export, GetPortfolioValue
│   │   │   └── MarketData/                     # PriceSnapshot, FxSnapshot, Backfill jobs
│   │   ├── Persistence/
│   │   │   ├── InvestmentsDbContext.cs
│   │   │   └── Migrations/
│   │   └── InvestmentsModule.cs
│   │
│   ├── BudgetBuddy.Module.Analytics/           # no own schema (read-only, queries multiple schemas)
│   │   ├── Features/
│   │   │   ├── Dashboard/                      # GetDashboard
│   │   │   └── Reports/                        # IncomeVsExpense, SpendingByCategory, MonthlySummary, InvestmentPerformance
│   │   └── AnalyticsModule.cs
│   │
│   └── BudgetBuddy.Module.ReferenceData/       # schema: referencedata
│       ├── Features/
│       │   ├── Currencies/                     # CRUD
│       │   ├── Categories/                     # CRUD
│       │   └── CategoryTypes/                  # CRUD
│       ├── Persistence/
│       │   ├── ReferenceDataDbContext.cs
│       │   └── Migrations/
│       └── ReferenceDataModule.cs
│
├── Shared/
│   ├── BudgetBuddy.Shared.Kernel/              # Shared domain primitives
│   │   ├── Entities/                           # User, AuditableEntity base
│   │   └── Contracts/                          # IModule, cross-module service interfaces
│   │
│   ├── BudgetBuddy.Shared.Contracts/           # Inter-module service contracts
│   │   └── (IAccountBalanceService, IInvestmentCalculationService, etc.)
│   │
│   └── BudgetBuddy.Shared.Infrastructure/      # Cross-cutting infrastructure
│       ├── Extensions/                         # DI, security, caching, database, observability extensions
│       ├── Persistence/
│       │   ├── AppDbContext.cs                 # Shared context: AuditLogs (public schema)
│       │   ├── Interceptors/                   # RLS, AuditLog, AuditableEntity, Outbox interceptors
│       │   ├── ConnectionStrings/              # Encrypted connection string provider
│       │   └── Extensions/                     # MigrateExtension (auto-migrate on startup)
│       ├── Security/                           # DataProtection, Encryption, TokenBlacklist, ClamAV
│       ├── Financial/                          # IPriceService, ICurrencyConversionService, providers
│       ├── Logging/                            # PII masking, structured log enrichers
│       ├── DataExchange/                       # Excel import/export, CSV service
│       └── Notification/                       # Email service (MailKit)
│
└── Tests/
    └── BudgetBuddy.ArchitectureTests/          # NetArchTest — module boundary enforcement
```

## Database Schema Overview

Each module owns exactly one PostgreSQL schema. The `AppDbContext` (shared) owns the `public` schema for cross-cutting tables.

| Schema | Tables | Owner |
|--------|--------|-------|
| `public` | `AuditLogs` | `AppDbContext` |
| `auth` | `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `RefreshTokens`, `SecurityEvents`, `__EFMigrationsHistory` | `AuthDbContext` |
| `accounts` | `Accounts`, `__EFMigrationsHistory` | `AccountsDbContext` |
| `transactions` | `Transactions`, `OutboxMessages`, `__EFMigrationsHistory` | `TransactionsDbContext` |
| `budgets` | `Budgets`, `__EFMigrationsHistory` | `BudgetsDbContext` |
| `investments` | `Investments`, `PriceSnapshots`, `ExchangeRateSnapshots`, `__EFMigrationsHistory` | `InvestmentsDbContext` |
| `referencedata` | `Currencies`, `Categories`, `CategoryTypes`, `__EFMigrationsHistory` | `ReferenceDataDbContext` |

Cross-module foreign keys (e.g. `transactions.CategoryId → referencedata.Categories`) are modeled as plain properties without EF navigation — modules do not hold references to each other's DbContexts.

Row-Level Security is enforced at the PostgreSQL connection level via the `RowLevelSecurityInterceptor`, which sets `app.current_user_id` and `app.is_admin` session variables before every query.

## Getting Started

### Prerequisites

**Required:**
- .NET 10 SDK
- PostgreSQL 16+

**Recommended for Development:**
- **Redis:** Token blacklist and distributed caching (falls back to in-memory if unavailable)
- **ClamAV:** Antivirus file scanning (optional for dev, required for production)
- **Seq:** Structured log viewer (`http://localhost:5341`)
- **Jaeger:** Distributed tracing UI (OTLP endpoint `localhost:4317`)
- **Docker:** Easiest way to run all infrastructure locally

### Configuration

**Environment Variables:**

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `ConnectionStrings__DefaultConnection` | Yes | — | PostgreSQL connection string |
| `ConnectionStrings__Redis` | Recommended | — | Redis connection string |
| `Jwt__SecretKey` | Yes | — | JWT signing key (min 32 chars) — use user-secrets in dev |
| `ASPNETCORE_ENVIRONMENT` | No | `Production` | `Development` / `Staging` / `Production` |
| `KeyVault__Url` | Production | — | Azure Key Vault URL |
| `ASPNETCORE_URLS` | No | `http://+:5000;https://+:5001` | Kestrel binding |
| `ClamAV__ServerUrl` | Production | `localhost` | ClamAV host |
| `ClamAV__Port` | No | `3310` | ClamAV port |
| `Security__RequireHttps` | Production | `false` | Enforce HTTPS redirects |
| `Email__Enabled` | No | `false` | Enable SMTP email sending |
| `Email__SmtpHost` | If email on | — | SMTP server hostname |
| `Email__SmtpPort` | If email on | `587` | SMTP port |
| `Email__Username` | If email on | — | SMTP credentials |
| `Email__Password` | If email on | — | SMTP credentials |
| `Email__FromEmail` | If email on | — | Sender address |
| `OpenTelemetry__OtlpEndpoint` | No | `http://localhost:4317` | Jaeger OTLP endpoint |

**Config Sections (appsettings.json):**

```json
{
  "PriceService": {
    "CoinGeckoBaseUrl": "https://api.coingecko.com/api/v3/",
    "CoinGeckoApiKey": "",
    "YahooFinanceBaseUrl": "https://query1.finance.yahoo.com/",
    "CacheDurationMinutes": 15,
    "TimeoutSeconds": 10
  },
  "ExchangeRates": {
    "BaseUrl": "https://api.frankfurter.app",
    "CacheDurationHours": 4,
    "TimeoutSeconds": 10
  },
  "BackgroundJobs": {
    "Enabled": true,
    "BudgetAlerts":               { "Enabled": true, "CronExpression": "0 0 8 * * ?"   },
    "DailyPriceSnapshot":         { "Enabled": true, "CronExpression": "0 0 22 * * ?"  },
    "DailyFxSnapshot":            { "Enabled": true, "CronExpression": "0 0 16 * * ?"  },
    "BackfillMarketData":         { "Enabled": true, "CronExpression": "0 0 3 * * ?"   },
    "TransactionsOutboxProcessor":{ "Enabled": true, "CronExpression": "0/30 * * * * ?" }
  },
  "Localization": {
    "DefaultCulture": "en-US",
    "SupportedCultures": ["en-US", "hu-HU"]
  }
}
```

**Development Setup:**

```bash
# Set connection strings (PowerShell)
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Database=budgetbuddy;Username=postgres;Password=YourPassword;SSL Mode=Prefer"
$env:ConnectionStrings__Redis = "localhost:6379"

# Set JWT secret via user-secrets (recommended — never commit to git)
dotnet user-secrets set "Jwt:SecretKey" "your-very-long-random-secret-key-at-least-32-chars" `
  --project Host/BudgetBuddy.API.ModularMonolith

# Or generate a cryptographically random key (Linux/macOS/WSL)
dotnet user-secrets set "Jwt:SecretKey" "$(openssl rand -base64 64)" `
  --project Host/BudgetBuddy.API.ModularMonolith

# Or generate with PowerShell
$secret = [System.Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
dotnet user-secrets set "Jwt:SecretKey" $secret --project Host/BudgetBuddy.API.ModularMonolith
```

**Docker Quick Start:**

```bash
# PostgreSQL
docker run -d --name postgres -e POSTGRES_PASSWORD=YourPassword -p 5432:5432 postgres:16

# Redis
docker run -d --name redis -p 6379:6379 redis:7-alpine

# ClamAV
docker run -d --name clamav -p 3310:3310 clamav/clamav:latest

# Seq (structured logs)
docker run -d --name seq -e ACCEPT_EULA=Y -p 5341:80 datalust/seq:latest

# Jaeger (distributed tracing)
docker run -d --name jaeger -p 4317:4317 -p 16686:16686 jaegertracing/all-in-one:latest
```

### Run Application

```bash
# From the solution root
cd Servers/BudgetBuddy.API.ModularMonolith

# Restore packages
dotnet restore

# Run — migrations are applied automatically on startup for all 7 DbContexts
dotnet run --project Host/BudgetBuddy.API.ModularMonolith

# Run with demo seed data (Development only)
dotnet run --project Host/BudgetBuddy.API.ModularMonolith -- --seed
```

**Startup sequence:**
1. All 7 DbContexts are auto-migrated in order: `AppDbContext` → `AuthDbContext` → `AccountsDbContext` → `TransactionsDbContext` → `BudgetsDbContext` → `InvestmentsDbContext` → `ReferenceDataDbContext`
2. Roles (`Admin`, `User`, `Premium`) are seeded if missing
3. Default admin user is created if missing (Development only)
4. If `--seed` flag is passed, demo data is inserted (currencies, categories, accounts, transactions, budgets, investments)
5. Market data backfill runs in the background for any missing price/FX history
6. Quartz.NET scheduler starts all 5 background jobs

**Default Admin User (Development Only):**
- Email: `admin@budgetbuddy.com`
- Password: `Admin@123456`
- **IMPORTANT:** Change password and enable 2FA on first login!

### Adding Migrations

Each module has its own `DbContext` and migration history. Always specify `--project` and `--context`:

```bash
# Auth module
dotnet ef migrations add <MigrationName> \
  --project Modules/BudgetBuddy.Module.Auth \
  --startup-project Host/BudgetBuddy.API.ModularMonolith \
  --context AuthDbContext

# Accounts module
dotnet ef migrations add <MigrationName> \
  --project Modules/BudgetBuddy.Module.Accounts \
  --startup-project Host/BudgetBuddy.API.ModularMonolith \
  --context AccountsDbContext

# Transactions module
dotnet ef migrations add <MigrationName> \
  --project Modules/BudgetBuddy.Module.Transactions \
  --startup-project Host/BudgetBuddy.API.ModularMonolith \
  --context TransactionsDbContext

# Budgets module
dotnet ef migrations add <MigrationName> \
  --project Modules/BudgetBuddy.Module.Budgets \
  --startup-project Host/BudgetBuddy.API.ModularMonolith \
  --context BudgetsDbContext

# Investments module
dotnet ef migrations add <MigrationName> \
  --project Modules/BudgetBuddy.Module.Investments \
  --startup-project Host/BudgetBuddy.API.ModularMonolith \
  --context InvestmentsDbContext

# ReferenceData module
dotnet ef migrations add <MigrationName> \
  --project Modules/BudgetBuddy.Module.ReferenceData \
  --startup-project Host/BudgetBuddy.API.ModularMonolith \
  --context ReferenceDataDbContext

# Shared AppDbContext (AuditLogs)
dotnet ef migrations add <MigrationName> \
  --project Shared/BudgetBuddy.Shared.Infrastructure \
  --startup-project Host/BudgetBuddy.API.ModularMonolith \
  --context AppDbContext
```

Migrations run automatically on the next `dotnet run` — no manual `database update` needed in development.

## API Endpoints

**Documentation:** `http://localhost:5160/scalar/v1`

**Auth (ASP.NET Core Identity):**
```
POST /auth/register
POST /auth/login
POST /auth/logout-with-revoke
POST /auth/logout-all
POST /auth/refresh-token
POST /auth/forgot-password
POST /auth/reset-password
```

**Two-Factor Authentication:**
```
GET  /api/twofactor/status
POST /api/twofactor/enable
POST /api/twofactor/verify
POST /api/twofactor/disable
GET  /api/twofactor/recovery-codes
```

**Accounts:**
```
GET    /api/accounts
POST   /api/accounts
PUT    /api/accounts/{id}
DELETE /api/accounts/{id}
GET    /api/accounts/{id}/balance
```

**Transactions:**
```
GET    /api/transactions
POST   /api/transactions
PUT    /api/transactions/{id}
DELETE /api/transactions/{id}
POST   /api/transactions/batch-delete
POST   /api/transactions/batch-update
POST   /api/transactions/import
GET    /api/transactions/export
POST   /api/transfers
```

**Budgets:**
```
GET    /api/budgets
POST   /api/budgets
PUT    /api/budgets/{id}
DELETE /api/budgets/{id}
GET    /api/budgets/{id}/vs-actual
GET    /api/budget-alerts
```

**Investments:**
```
GET    /api/investments
POST   /api/investments
PUT    /api/investments/{id}
DELETE /api/investments/{id}
POST   /api/investments/batch-delete
GET    /api/investments/export
GET    /api/investments/portfolio-value
```

**Reference Data:**
```
GET/POST/PUT/DELETE /api/currencies
GET/POST/PUT/DELETE /api/categories
GET/POST/PUT/DELETE /api/categorytypes
```

**Analytics:**
```
GET /api/dashboard
GET /api/reports/income-expense
GET /api/reports/spending-by-category
GET /api/reports/monthly-summary
GET /api/reports/investment-performance
```

**User Settings:**
```
GET /api/user-settings
PUT /api/user-settings
```

**Admin Only:**
```
GET /api/security/events
GET /api/security/alerts
GET /api/audit/{entity}/{id}
```

**Observability:**
```
GET /health
GET /health/ready
GET /health/live
GET /metrics
```

## Architecture Tests

The solution includes an architecture test project (`BudgetBuddy.ArchitectureTests`) that enforces module boundary rules using NetArchTest:

- Modules must not reference other modules' internal assemblies
- All handlers must inherit from the correct base types
- Infrastructure types must not leak into domain or feature layers
- Shared contracts are the only allowed cross-module coupling

```bash
dotnet test Tests/BudgetBuddy.ArchitectureTests
```

## Troubleshooting

**Migrations fail on startup (`relation already exists`):**

If the database has existing tables but the `__EFMigrationsHistory` table is missing the entry, manually mark the migration as applied:

```sql
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('<migration-id>', '10.0.3');
```

Then restart the application.

**`No migrations were found in assembly 'X'`:**

The DbContext has no migration files yet. Generate an initial migration:

```bash
dotnet ef migrations add InitialCreate \
  --project Modules/BudgetBuddy.Module.<Name> \
  --startup-project Host/BudgetBuddy.API.ModularMonolith \
  --context <Name>DbContext
```

**JWT Secret Missing:**
```bash
dotnet user-secrets set "Jwt:SecretKey" "your-secret" --project Host/BudgetBuddy.API.ModularMonolith
```

**Port Already in Use:**
```bash
$env:ASPNETCORE_URLS = "http://localhost:5050"
dotnet run --project Host/BudgetBuddy.API.ModularMonolith
```

**Redis Connection Issues:**
- Test: `redis-cli ping` (should return PONG)
- Application falls back to in-memory cache if Redis is unavailable

**ClamAV Not Scanning:**
- ClamAV takes 2–3 minutes on startup to update virus definitions
- Check: `docker logs clamav`
- Test port: `Test-NetConnection localhost -Port 3310`
- In development, ClamAV is disabled by default (`appsettings.Development.json`)

**Background Jobs Not Running:**
- Check `BackgroundJobs:Enabled` is `true` in config
- Check individual job `Enabled` flags
- Cron expression format: Quartz 6-field (sec min hour day month weekday)

**Price Data Missing:**
- CoinGecko free tier has rate limits — add a free API key in `PriceService:CoinGeckoApiKey`
- Historical backfill runs automatically on startup and nightly at 03:00 UTC

## Production Deployment

### Pre-Deployment Checklist

- ✅ PostgreSQL 16+ accessible with SSL Mode=Require
- ✅ Redis for token blacklist and distributed caching
- ✅ ClamAV for antivirus file scanning
- ✅ Azure Key Vault for secrets and encryption keys
- ✅ All migrations applied (automatic on startup)
- ✅ `ASPNETCORE_ENVIRONMENT=Production` set
- ✅ `Security__RequireHttps=true` set
- ✅ `Cors:AllowedOrigins` updated to your frontend domain

**Azure Key Vault Secrets:**

```bash
az keyvault secret set --vault-name <vault> --name "ConnectionStrings--DefaultConnection" --value "<conn>"
az keyvault secret set --vault-name <vault> --name "ConnectionStrings--Redis"             --value "<redis>"
az keyvault secret set --vault-name <vault> --name "Jwt--SecretKey"                       --value "<secret>"
az keyvault secret set --vault-name <vault> --name "DataProtection--MasterKey"            --value "<base64-key>"
```

**Production Environment Variables:**

```bash
ASPNETCORE_ENVIRONMENT=Production
KeyVault__Url=https://<your-vault>.vault.azure.net/
ClamAV__ServerUrl=<clamav-host>
ClamAV__Port=3310
Security__RequireHttps=true
OpenTelemetry__OtlpEndpoint=http://<jaeger-host>:4317
```

**Critical Post-Deploy Steps:**
1. Log in as `admin@budgetbuddy.com`, change password, enable 2FA immediately
2. Verify `/health/ready` returns 200 (PostgreSQL + Redis connected)
3. Verify Quartz jobs appear in logs within their first scheduled window
4. Set up monitoring alerts on `/health/ready` for on-call
5. Implement data retention cleanup for `SecurityEvents` and `AuditLogs` (recommended: 90 days)
6. Schedule regular PostgreSQL backups (AuditLog is a compliance requirement)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
