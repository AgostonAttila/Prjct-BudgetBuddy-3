# BudgetBuddy API - Clean Architecture

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16+-336791?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

Personal finance management REST API for tracking transactions, budgets, investments, and generating financial reports — built with Clean Architecture.

## Tech Stack

**Framework & Runtime:**
- .NET 10.0
- C# 13 with Nullable Reference Types

**Database & Caching:**
- PostgreSQL 16+ with Row-Level Security (RLS)
- Entity Framework Core 10.0 with Repository pattern
- Npgsql with NodaTime support
- Hybrid Cache (L1 in-process + L2 Redis) via `Microsoft.Extensions.Caching.Hybrid`
- Redis 7+ for distributed cache & token blacklist

**Architecture & Patterns:**
- Clean Architecture (Domain → Application → Infrastructure → API)
- CQRS with MediatR 12.4 + pipeline behaviors
- Repository pattern with interface-based contracts
- Minimal APIs with Carter 8.2

**Key Libraries:**
- **Mapping:** Mapster 7.4 (fast object-to-object mapping)
- **Validation:** FluentValidation 11.9
- **Date/Time:** NodaTime 3.2 (LocalDate, Instant) + IClock for testability
- **Authentication:** ASP.NET Core Identity with 2FA, QRCoder 1.7
- **Email:** MailKit (SMTP, HTML/plain text)
- **Logging:** Serilog (Console, File, Seq) with PII masking
- **Observability:** OpenTelemetry with OTLP exporter (Jaeger) + Prometheus
- **API Documentation:** Scalar 1.2
- **Security:** NetEscapades.AspNetCore.SecurityHeaders, nClam 9.0, Data Protection API
- **Excel/CSV:** ClosedXML 0.104 for export
- **Bulk Operations:** EFCore.BulkExtensions 10.0 (batch inserts/updates)
- **Background Jobs:** Quartz.NET 3.13 (cron-scheduled daily jobs)
- **Rate Limiting:** Built-in ASP.NET Core rate limiter (global, per-IP, per-auth, per-endpoint)
- **Azure:** Key Vault integration for production secrets & encryption keys
- **Architecture Tests:** ArchUnitNET 0.13.3

## Features

### Core Financial Features

- **Accounts:** Multi-currency account management with real-time balance calculation
- **Transactions:** Income/expense/transfer tracking with categories, labels, payees
- **Transfers:** Account-to-account money transfers (creates paired transactions automatically, FX conversion included)
- **Budgets:** Monthly budget planning with spending tracking
- **Budget Alerts:** Automatic threshold notifications — Safe (<80%), Warning (80–99%), Exceeded (≥100%) with multi-currency conversion
- **Categories:** Hierarchical category management
- **Category Types:** Sub-level category classification with icon and color
- **Currencies:** Global currency master data (code, symbol, name)
- **Investments:** Portfolio tracking for Stocks, ETFs, Crypto, Bonds, Mutual Funds
- **Reports:** Income vs. expense, spending by category, monthly summaries, investment performance
- **Dashboard:** Real-time financial overview aggregating all data in one call
- **User Settings:** Per-user preferences (default currency, language, date format)

### Data Operations

- **Export:** Excel and CSV export for transactions and investments
- **Batch Delete:** Delete multiple transactions or investments in one request
- **Batch Update:** Update category/labels on multiple transactions at once

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
| `SecurityEventCleanupJob` | 02:00 | Purges reviewed security events older than 90 days |

All jobs support individual enable/disable via `BackgroundJobs` config. Cron expressions are fully configurable.

### Security Features

**Authentication & Authorization:**
- JWT token authentication with automatic rotation (15 min access / 7 day refresh)
- Dual authentication scheme: **Bearer token** (API/mobile) + **Cookie** (web SPA)
- Token blacklist with Redis for instant revocation
- Two-Factor Authentication (2FA) with TOTP and recovery codes
- 2FA enforcement for Admin role via `Require2FA` policy
- Brute force protection: 5 failed login attempts → 5 min lockout
- Role-Based Access Control (Admin / Premium / User policies)

**Data Protection:**
- Connection string encryption with Data Protection API + Azure Key Vault
- SSL/TLS encryption for database connections (SSL Mode=Require enforced in production)
- Column-level encryption for sensitive fields (Transaction.Payee, Transaction.Note)
- PostgreSQL Row-Level Security (RLS) enforced via EF Core interceptor
- PII masking in logs (email, IP, GUID automatic masking)

**Security Monitoring:**
- 20+ security event types tracked: login, logout, 2FA, token operations, etc.
- Real-time security alerts with brute force detection
- Comprehensive audit trail with before/after change tracking on all entities
- Automatic audit logging via EF Core interceptor

**File Security:**
- ClamAV antivirus integration for file upload scanning
- IP-based rate limiting (multiple policies: global 1000 req/min, per-IP 450 req/min, per-auth, 2FA 10/min, refresh 3/15min)

**Compliance:**
- GDPR ready (Article 30: Audit trail, Article 32: Encryption, Article 33: Breach alerts)
- SOC 2 compliant (Logging, access control, change management)
- PCI-DSS aligned (Encrypted fields, audit trail, 2FA + RBAC)
- OWASP Top 10 (2021) protections implemented

### Caching

- **Hybrid Cache (L1 + L2):** In-process memory cache (L1) backed by Redis (L2)
- **Cache invalidation:** Automatic via MediatR `CacheInvalidationBehavior` pipeline
- **Cache tags:** Feature-scoped invalidation (`CacheTags` constants)
- **Fallback:** In-memory only if Redis is unavailable (test/dev environments)

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
Domain/
├── Entities/          # Core domain models (Account, Transaction, Budget, Investment…)
│   ├── AuditLog.cs
│   ├── SecurityEvent.cs
│   ├── PriceSnapshot.cs
│   └── ExchangeRateSnapshot.cs
├── Enums/             # TransactionType, PaymentType, InvestmentType, ExportFormat
├── Exceptions/        # DomainException, NotFoundException, DomainValidationException
├── Contracts/         # AuditableEntity, IUserOwnedEntity, SensitiveDataAttribute
└── Constants/         # AppRoles, AppPolicies

Application/
├── Features/          # CQRS handlers and validators, organized by domain slice
│   ├── Accounts/
│   ├── Transactions/
│   ├── Investments/
│   ├── Budgets/
│   ├── BudgetAlerts/
│   ├── Categories/
│   ├── CategoryTypes/
│   ├── Currencies/
│   ├── Transfers/
│   ├── Reports/
│   ├── Dashboard/
│   ├── MarketData/
│   ├── Auth/
│   ├── Authentication/
│   ├── TwoFactor/
│   ├── UserSettings/
│   ├── Security/
│   └── Audit/
└── Common/
    ├── Behaviors/     # ValidationBehavior, LoggingBehavior, CacheInvalidationBehavior
    ├── Contracts/     # Service interfaces (ITokenService, IEmailService, IEncryptionService…)
    ├── Repositories/  # Repository interfaces (IAccountRepository, ITransactionRepository…)
    ├── Handlers/      # UserAwareHandler<,> base class
    ├── Constants/     # CacheTags for cache invalidation
    └── Models/        # BatchDeleteResult, EmailMessage

Infrastructure/
├── Persistence/
│   ├── AppDbContext.cs
│   ├── Configurations/    # IEntityTypeConfiguration<T> per entity
│   ├── Repositories/      # Generic + specialized repository implementations
│   ├── Interceptors/      # AuditableEntityInterceptor, RLSInterceptor, AuditLogInterceptor
│   ├── Converters/        # EncryptedStringConverter, HashedStringConverter
│   ├── Migrations/
│   └── Seeders/           # RoleSeeder, AdminUserSeeder, DataSeeder
├── Security/
│   ├── Authentication/    # TokenService, TokenBlacklistService
│   └── Encryption/        # EncryptionService, DataProtectionService
├── BackgroundJobs/
│   ├── BudgetAlerts/
│   ├── MarketData/
│   └── Security/          # SecurityEventCleanupJob
├── Financial/             # PriceService, FX rates, CoinGecko + Yahoo Finance providers
├── Services/              # ReportService, DashboardService, BudgetAlertService…
├── Notification/          # EmailService, AuthenticationEmailService (MailKit)
├── DataExchange/          # CsvExportService, ExcelExportService (ClosedXML)
└── Logging/               # PiiMaskingEnricher, SensitiveDataDestructuringPolicy

API/
├── Endpoints/         # Carter modules (one per feature)
├── Filters/           # IdempotencyFilter, TwoFactorRateLimitFilter, UserIdEndpointFilter
├── Middlewares/       # CorrelationId, RequestTimeLogging, TokenBlacklist, SecurityEvent
├── Mappings/          # Mapster IRegister implementations
├── Authorization/     # Require2FAHandler
├── Exceptions/        # GlobalExceptionHandler → ProblemDetails
└── Extensions/        # DI setup (ApiExtensions, AuthExtensions, SecurityExtensions…)

Tests/
└── BudgetBuddy.ArchitectureTests/
    ├── LayerDependencyTests.cs
    ├── NamingConventionTests.cs
    ├── DomainLayerTests.cs
    ├── ApplicationLayerTests.cs
    ├── InfrastructureLayerTests.cs
    └── ApiLayerTests.cs
```

## Architecture

The project follows **Clean Architecture** with strict dependency rules enforced by automated tests:

```
API  →  Application  →  Domain
          ↑
    Infrastructure
```

- **Domain** has zero external dependencies — only NodaTime and ASP.NET Core Identity (for `IdentityUser<Guid>`)
- **Application** depends on Domain only; all infrastructure concerns are accessed via interfaces
- **Infrastructure** implements Application contracts (repositories, services) and depends on Domain + Application
- **API** wires everything together; depends on all layers for DI registration only

### MediatR Pipeline Behaviors

Every command/query passes through these behaviors in order:

1. `LoggingBehavior<,>` — logs request name and duration
2. `ValidationBehavior<,>` — runs FluentValidation, throws on failure
3. `CacheInvalidationBehavior<,>` — invalidates cache tags on write operations

## Testing

Architecture rules are enforced automatically using **ArchUnitNET**:

| Test Class | What It Enforces |
|------------|-----------------|
| `LayerDependencyTests` | Domain ↛ Application/Infrastructure/API; Application ↛ Infrastructure/API; Infrastructure ↛ API |
| `NamingConventionTests` | Commands/Queries/Handlers/Validators/Modules follow naming conventions |
| `DomainLayerTests` | Entities, enums, and exceptions live in the correct namespaces |
| `ApplicationLayerTests` | Handlers in `Features`, interfaces in `Common.Contracts`, behaviors in `Common.Behaviors` |
| `InfrastructureLayerTests` | DbContext in `Persistence`, entity configs in `Persistence.Configurations` |
| `ApiLayerTests` | Carter modules in `Endpoints`, filters in `Filters`, Mapster registrations in `Mappings` |

```bash
# Run architecture tests
dotnet test Tests/BudgetBuddy.ArchitectureTests/
```

## Getting Started

### Prerequisites

**Required:**
- .NET 10 SDK
- PostgreSQL 16+

**Recommended for Development:**
- **Redis:** Token blacklist & distributed caching (falls back to in-memory if unavailable)
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
| `Jwt__SecretKey` | Yes | — | JWT signing key (min 64 bytes) — use user-secrets in dev |
| `ASPNETCORE_ENVIRONMENT` | No | `Production` | Development / Staging / Production |
| `KeyVault__Url` | Production | — | Azure Key Vault URL |
| `ASPNETCORE_URLS` | No | `http://+:5000;https://+:5001` | Kestrel binding |
| `ClamAV__ServerUrl` | Production | `localhost` | ClamAV host |
| `ClamAV__Port` | No | `3310` | ClamAV port |
| `Security__RequireHttps` | Production | `false` | Enforce HTTPS redirects |
| `Email__Enabled` | No | `false` | Enable SMTP email sending |
| `Email__SmtpHost` | If email enabled | — | SMTP server hostname |
| `Email__SmtpPort` | If email enabled | `587` | SMTP port |
| `Email__Username` | If email enabled | — | SMTP credentials |
| `Email__Password` | If email enabled | — | SMTP credentials |
| `Email__FromEmail` | If email enabled | — | Sender address |
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
    "BudgetAlerts":          { "Enabled": true, "CronExpression": "0 0 8 * * ?"  },
    "DailyPriceSnapshot":    { "Enabled": true, "CronExpression": "0 0 22 * * ?" },
    "DailyFxSnapshot":       { "Enabled": true, "CronExpression": "0 0 16 * * ?" },
    "BackfillMarketData":    { "Enabled": true, "CronExpression": "0 0 3 * * ?"  },
    "SecurityEventCleanup":  { "Enabled": true, "CronExpression": "0 0 2 * * ?"  }
  },
  "Localization": {
    "DefaultCulture": "en-US",
    "SupportedCultures": ["en-US", "hu-HU"]
  },
  "RateLimit": {
    "GlobalPermitLimit": 1000,
    "PerIpPermitLimit": 450,
    "TwoFactorPermitLimit": 10,
    "RefreshTokenPermitLimit": 3,
    "RefreshTokenWindowMinutes": 15
  }
}
```

**Development Setup:**

```bash
# Set connection strings
set ConnectionStrings__DefaultConnection=Host=localhost;Database=budgetbuddy;Username=postgres;Password=YourPassword;SSL Mode=Prefer
set ConnectionStrings__Redis=localhost:6379

# Set JWT secret key (required, minimum 64 bytes)
dotnet user-secrets set "Jwt:SecretKey" "$(openssl rand -base64 64)"

# Production (SSL required)
export ConnectionStrings__DefaultConnection="Host=your-db.postgres.database.azure.com;Database=budgetbuddy;Username=admin;Password=YourPassword;SSL Mode=Require;Trust Server Certificate=false"
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
# Restore packages
dotnet restore

# Apply migrations (includes RLS, SecurityEvent, AuditLog, PriceSnapshot, ExchangeRateSnapshot tables)
dotnet ef database update --project Infrastructure --startup-project API

# Run application (seeds Admin role & admin user on first start)
dotnet run --project API
```

**Default Admin User (Development Only):**
- Email: `admin@budgetbuddy.com`
- Password: `Admin@123456`
- **IMPORTANT:** Change password and enable 2FA on first login!

### API Endpoints

**Documentation:** `https://localhost:5001/scalar/v1`

**API Versioning:** `/api/v1/*` (via Asp.Versioning.Http)

**Core Endpoints:**
```
GET/POST/PUT/DELETE /api/accounts
GET/POST/PUT/DELETE /api/transactions
GET/POST/PUT/DELETE /api/budgets
GET/POST/PUT/DELETE /api/investments
GET/POST/PUT/DELETE /api/categories
GET/POST/PUT/DELETE /api/categorytypes
GET/POST/PUT/DELETE /api/currencies
POST                /api/transfers
GET                 /api/budget-alerts
GET/PUT             /api/user-settings

GET /api/reports/income-expense
GET /api/reports/spending-by-category
GET /api/reports/monthly-summary
GET /api/reports/investment-performance
GET /api/dashboard

POST /api/transactions/batch-delete
POST /api/transactions/batch-update
GET  /api/transactions/export
GET  /api/investments/export
GET  /api/accounts/{id}/balance
GET  /api/investments/portfolio-value
```

**Auth & Security Endpoints:**
```
POST /auth/login
POST /auth/refresh
POST /auth/logout
POST /auth/logout-all
POST /api/twofactor/setup
POST /api/twofactor/verify
POST /api/twofactor/recovery-codes
GET  /api/security/events          (Admin)
GET  /api/security/alerts          (Admin)
GET  /api/audit/{entity}/{id}      (Admin)
```

**Observability:**
```
GET /health         Overall health
GET /health/ready   Readiness probe (PostgreSQL + Redis)
GET /health/live    Liveness probe
GET /metrics        Prometheus scrape
```

## Rate Limiting

| Policy | Limit | Window |
|--------|-------|--------|
| Global | 1000 req/min | Fixed window |
| Per-IP | 450 req/min | Sliding window |
| Per authenticated user | Token bucket (10 cap, 5 replenish/min) | Token bucket |
| 2FA endpoints | 10 attempts/min | Fixed window |
| Refresh token | 3 attempts / 15 min | Fixed window |

## Troubleshooting

**Migration Errors:**
```bash
dotnet ef database drop --force --project Infrastructure --startup-project API
dotnet ef database update --project Infrastructure --startup-project API
```

**Port Already in Use:**
```bash
set ASPNETCORE_URLS=http://localhost:5050;https://localhost:5051
```

**JWT Secret Missing / Too Short:**
```bash
dotnet user-secrets set "Jwt:SecretKey" "$(openssl rand -base64 64)"
```
Secret must be at least 64 bytes — validated at startup.

**Connection Refused (PostgreSQL):**
- Verify PostgreSQL is running: `pg_isready`
- Check connection string credentials
- Ensure PostgreSQL accepts TCP connections (`postgresql.conf`)

**Redis Connection Issues:**
- Test: `redis-cli ping` (should return PONG)
- Application falls back to in-memory hybrid cache if Redis is unavailable

**ClamAV Not Scanning Files:**
- ClamAV takes 2–3 minutes to start and update virus definitions
- Check logs: `docker logs clamav`
- Test port: `nc -zv localhost 3310`

**Seq Not Receiving Logs:**
- Verify Seq is running: `http://localhost:5341`
- Check `appsettings.json` → `Serilog:WriteTo:Seq:serverUrl`

**Background Jobs Not Running:**
- Check `BackgroundJobs:Enabled` is `true` in config
- Check individual job `Enabled` flags
- Cron expression format: Quartz cron (6 fields: sec min hour day month weekday)

**Price Data Missing:**
- CoinGecko free tier has rate limits — add a free API key in `PriceService:CoinGeckoApiKey`
- Yahoo Finance requires no key but may throttle heavy usage
- Historical backfill runs automatically on startup and nightly at 03:00 UTC

**Architecture Tests Failing:**
- Naming conventions must be followed exactly (e.g. `CreateAccountCommand`, `CreateAccountHandler`)
- Interfaces must be placed in `Common.Contracts`; implementations in the corresponding Infrastructure namespace
- New Carter modules must live in `API/Endpoints/` and end with `Module`

## Production Deployment

### Pre-Deployment Checklist

**Required Infrastructure:**
- PostgreSQL 16+ with all migrations applied
- Redis for token blacklist & distributed caching
- ClamAV for antivirus file scanning
- Azure Key Vault for secrets & encryption keys

**Security Configuration (Azure Key Vault):**
```bash
az keyvault secret set --vault-name <vault> --name "ConnectionStrings--DefaultConnection" --value "<conn-string>"
az keyvault secret set --vault-name <vault> --name "ConnectionStrings--Redis" --value "<redis-conn>"
az keyvault secret set --vault-name <vault> --name "Jwt--SecretKey" --value "<64-byte-base64-key>"
az keyvault secret set --vault-name <vault> --name "DataProtection--MasterKey" --value "<base64-key>"
```

**Environment Variables (Production):**
```bash
ASPNETCORE_ENVIRONMENT=Production
KeyVault__Url=https://<your-vault>.vault.azure.net/
ClamAV__ServerUrl=tcp://<clamav-host>
ClamAV__Port=3310
Security__RequireHttps=true
OpenTelemetry__OtlpEndpoint=http://<jaeger-host>:4317
```

**Critical Steps:**
1. Apply all migrations before starting the application
2. Create admin user and enable 2FA immediately
3. Update `Cors:AllowedOrigins` in `appsettings.Production.json` to your frontend domain
4. Verify rate limit thresholds for your expected traffic
5. Configure email SMTP if budget alert emails are needed
6. Set up monitoring alerts on `/health/ready` for on-call
7. `SecurityEventCleanupJob` runs nightly and keeps only the last 90 days — adjust retention via `SecurityEventCleanupJobSettings:RetentionDays` if needed
8. Schedule regular database backups (especially AuditLog — compliance requirement)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
