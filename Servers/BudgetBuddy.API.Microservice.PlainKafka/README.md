# BudgetBuddy API - Microservices

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16+-336791?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Kafka](https://img.shields.io/badge/Kafka-7.9.0-231F20?logo=apachekafka)](https://kafka.apache.org/)
[![Keycloak](https://img.shields.io/badge/Keycloak-26.0-4D9CF2?logo=keycloak)](https://www.keycloak.org/)
[![Kubernetes](https://img.shields.io/badge/Kubernetes-Helm-326CE5?logo=kubernetes)](https://helm.sh/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

Personal finance management REST API — the microservices edition of BudgetBuddy. Each domain is a fully independent service with its own database, Kafka topic set, and deployment unit. An API Gateway (YARP) is the single entry point for all clients.

## Architecture

This project is a **microservices platform**: each domain ships as a separately deployable process. Services communicate asynchronously through **Kafka** (event-driven) and synchronously through the **API Gateway** (YARP). There are no direct service-to-service HTTP calls in the critical path.

Each service:

- owns its own **PostgreSQL database** (no shared DB, no cross-schema queries)
- publishes and consumes events via **Kafka topics** (one topic set per domain)
- exposes its own REST endpoints via Carter `ICarterModule`
- validates its own JWT tokens (Keycloak) — defense-in-depth after the gateway
- maintains local **read models** (snapshots) populated by Kafka consumers — no cross-service queries at runtime
- uses CQRS internally (MediatR commands/queries per feature slice)

Cross-cutting concerns (security, audit, caching, observability, background jobs, messaging) live in `Shared.*` assemblies and are injected into each service.

## Services

| Service | Database | Responsibility |
|---------|----------|----------------|
| `Gateway` | — | YARP reverse proxy — JWT validation, CORS, routing to all services |
| `Accounts` | `accounts` | Multi-currency account management and balance tracking |
| `Transactions` | `transactions` | Income/expense/transfer tracking, import/export, saga orchestration |
| `Budgets` | `budgets` | Monthly budget planning, spending tracking, budget alerts |
| `Investments` | `investments` | Portfolio management, price snapshots, valuation |
| `Analytics` | _(read-only, event-driven)_ | Dashboard, spending trends, investment performance reports |
| `ReferenceData` | `referencedata` | Currencies, categories, category types |
| `Financial` | — | Currency conversion and market data (Yahoo Finance, CoinGecko, Frankfurter) |
| `Notifications` | — | Event-driven alerts and email delivery |

## Tech Stack

**Framework & Runtime:**
- .NET 10.0
- C# 13 with Nullable Reference Types, TreatWarningsAsErrors

**Databases & Caching:**
- PostgreSQL 16+ — one database per service, Row-Level Security (RLS) on all user tables
- Entity Framework Core 10.0 — one `DbContext` per service + read-only query DbContext
- Npgsql with NodaTime support
- Redis 7+ — distributed cache, idempotency keys, token management

**Messaging:**
- Apache Kafka (KRaft, no Zookeeper) — 19 domain topics + 6 Dead Letter Queues
- Confluent.Kafka 2.6.0
- Transactional Outbox pattern per service (Quartz.NET processor, 30s interval)
- Saga orchestration for cross-service balance reservation (Transactions ↔ Accounts)

**API & Gateway:**
- YARP 2.2.0 — reverse proxy at the gateway layer
- Carter 8.2 — minimal API endpoint modules per service
- Keycloak 26.0 — OAuth2/OIDC identity provider, JWT issuance and validation
- API Versioning (Asp.Versioning 8.1)

**Architecture & Patterns:**
- Microservices with one-database-per-service isolation
- CQRS with MediatR 12.4
- Event-driven architecture with Kafka
- Outbox pattern for reliable event publishing
- Saga pattern (choreography) for distributed transactions
- Read models / denormalized snapshots for query-side isolation
- Direct DbContext usage (no repository abstraction)

**Key Libraries:**
- **Mapping:** Mapster 7.4
- **Validation:** FluentValidation 11.9
- **Date/Time:** NodaTime 3.2 (LocalDate, Instant)
- **Background Jobs:** Quartz.NET 3.13
- **Observability:** OpenTelemetry with OTLP exporter (Jaeger) + Prometheus + Grafana
- **Logging:** Serilog (Console, File, Seq)
- **Resilience:** Microsoft.Extensions.Http.Resilience (retry + circuit breaker)
- **Caching:** HybridCache (L1 in-memory + L2 Redis) + ASP.NET Core Output Cache
- **Excel/CSV:** ClosedXML 0.105
- **Bulk Operations:** EFCore.BulkExtensions 10.0
- **Email:** MailKit 4.16
- **Antivirus:** nClam 5.0 (ClamAV)
- **Feature Flags:** Microsoft.FeatureManagement 4.0
- **Static Analysis:** SonarAnalyzer.CSharp 9.32

## Features

### Core Financial Features

- **Accounts:** Multi-currency account management; balance computed from transaction snapshots
- **Transactions:** Income/expense/transfer tracking with categories, labels, payees; Excel import; bulk operations
- **Transfers:** Account-to-account money transfers with saga-based balance reservation
- **Budgets:** Monthly budget planning with spending tracking and budget-vs-actual comparison
- **Budget Alerts:** Automatic threshold notifications — Safe (<80%), Warning (80–99%), Exceeded (≥100%)
- **Investments:** Portfolio tracking for Stocks, ETFs, Crypto, Bonds, Mutual Funds; price snapshots; valuation
- **Reports:** Income vs. expense, spending by category, monthly summaries, investment performance
- **Dashboard:** Real-time financial overview assembled from local read models (no cross-service HTTP at runtime)
- **Reference Data:** User-scoped categories with icon/color; global currencies; category types

### Event-Driven Architecture

Services communicate exclusively through Kafka events. Each service maintains local snapshots (read models) populated by its consumers — so query-side operations never need to call another service.

| Publisher | Topic | Consumers |
|-----------|-------|-----------|
| Accounts | `budgetbuddy.accounts.created/updated/deleted` | Transactions, Analytics |
| Accounts | `budgetbuddy.accounts.changelog` (compacted) | Transactions (event sourcing) |
| Accounts | `budgetbuddy.accounts.balance-reserved` | Transactions (saga) |
| Transactions | `budgetbuddy.transactions.created/updated/deleted` | Accounts, Budgets, Analytics |
| Transactions | `budgetbuddy.transactions.reversed` | Accounts (saga compensation) |
| Budgets | `budgetbuddy.budgets.created/updated/deleted` | Analytics |
| Budgets | `budgetbuddy.budgets.alert-triggered` | Notifications, Analytics |
| Investments | `budgetbuddy.investments.created/updated/deleted/price-updated` | Analytics |
| ReferenceData | `budgetbuddy.referencedata.currency-synced/category-changed` | Transactions, Budgets, Analytics |

Each domain has a Dead Letter Queue (`budgetbuddy.<domain>.dlq`, 7-day retention) for failed consumer messages.

### Saga: Distributed Balance Reservation

When a transaction is created, the Transactions service orchestrates a saga across the Accounts service:

1. Transactions publishes `balance-reserve-requested`
2. Accounts validates and reserves the balance, publishes `balance-reserved`
3. Transactions commits the transaction on success; publishes `transactions.reversed` on failure
4. `SagaTimeoutJob` handles stuck sagas (compensation after timeout)

### Outbox Pattern

Each service persists domain events to an `OutboxMessage` table within the same DB transaction as the domain write. A Quartz.NET job (`OutboxProcessorJob`, every 30s) picks up unprocessed messages, publishes them to Kafka, and marks them processed. This guarantees at-least-once delivery without distributed transactions.

### Market Data & Financial Providers

**Live Price Data:**
- **Yahoo Finance** — Stock and ETF prices (no API key required)
- **CoinGecko** — Cryptocurrency prices (optional API key for higher rate limits)
- Prices cached 15 minutes; fallback to last known price on provider failure

**Foreign Exchange Rates:**
- **Frankfurter** (ECB data) — Real-time and historical FX rates for 30+ currencies
- Rates cached 4 hours

### Caching Strategy (3 Layers)

| Layer | Mechanism | TTL |
|-------|-----------|-----|
| HTTP Output Cache | ASP.NET Core response cache | 30s – 60 min per endpoint |
| L1 In-Process | HybridCache | 1 min |
| L2 Distributed | Redis | 5 min |

Cache tags used for selective invalidation on write operations.

### Background Jobs (Quartz.NET)

| Job | Service | Schedule (UTC) | Description |
|-----|---------|----------------|-------------|
| `OutboxProcessorJob` | All services | Every 30s | Publishes pending domain events to Kafka |
| `DailyPriceSnapshotJob` | Financial | 22:00 | Fetches closing prices for all tracked symbols |
| `DailyFxSnapshotJob` | Financial | 16:00 | Fetches ECB exchange rates |
| `BackfillMarketDataJob` | Financial | 03:00 | Detects and fills historical price/FX gaps |
| `BudgetAlertJob` | Budgets | 08:00 | Evaluates all budgets, triggers alert events |
| `SagaTimeoutJob` | Transactions | Every 5 min | Compensates stuck/timed-out sagas |
| `AuditLogRetentionJob` | Transactions | 02:00 | Purges audit logs older than retention period |

All jobs are individually enable/disable configurable via `BackgroundJobs` config per service.

### Security

**Authentication & Authorization:**
- Keycloak 26.0 — OAuth2/OIDC, JWT issuance, user management
- JWT validation at Gateway + re-validation at each service (defense-in-depth)
- Role-Based Access Control (Admin / User / Premium policies)
- Idempotency-Key header support (Redis-backed, 24h TTL) to prevent duplicate writes

**Data Protection:**
- PostgreSQL Row-Level Security (RLS) — enforced via EF Core interceptor (`RowLevelSecurityInterceptor`), sets `app.current_user_id` per connection
- SSL/TLS for all database connections (SSL Mode=Require in production)
- Azure Key Vault for production secrets and Data Protection keys

**Network Security:**
- HSTS headers at gateway (TLS termination point)
- CORS enforcement at gateway
- Keycloak Authority validation — fail-fast in production if not configured
- Rate limiting: fixed window + per-user sliding window

**File Security:**
- ClamAV antivirus integration for Excel import scanning
- Multipart form validation

**Compliance:**
- GDPR ready (audit trail, encryption, breach alerts)
- SOC 2 aligned (logging, access control)
- OWASP Top 10 (2021) protections

### Observability

- **OpenTelemetry:** Distributed tracing with OTLP exporter (Jaeger compatible); instrumented: ASP.NET Core, HTTP client, EF Core, runtime
- **Prometheus:** Metrics at `/metrics` per service
- **Grafana:** Pre-provisioned dashboards (from `Infrastructure/grafana/provisioning`)
- **Serilog:** Structured logging to Console, rolling File, and Seq
- **Health Checks:**
  - `/health` — Overall application health
  - `/health/ready` — Readiness probe (PostgreSQL + Redis)
  - `/health/live` — Liveness probe (no external dependency check)
  - `/metrics` — Prometheus scrape endpoint

## Project Structure

```
BudgetBuddy.API.Microservice.slnx
│
├── Gateway/
│   └── BudgetBuddy.Gateway/                    # YARP reverse proxy — entry point for all clients
│       ├── Program.cs                          # Keycloak JWT, YARP routing, OpenTelemetry
│       └── appsettings.json                    # Route/cluster config, Keycloak, OTel
│
├── Services/
│   ├── BudgetBuddy.Service.Accounts/           # DB: accounts
│   │   ├── Features/
│   │   │   └── Accounts/                       # CRUD, GetBalance, changelog snapshots
│   │   ├── Consumers/                          # Kafka: transactions events → account snapshot
│   │   ├── Persistence/
│   │   │   ├── AccountsDbContext.cs
│   │   │   └── Migrations/
│   │   └── Program.cs
│   │
│   ├── BudgetBuddy.Service.Transactions/       # DB: transactions
│   │   ├── Features/
│   │   │   ├── Transactions/                   # CRUD, BatchDelete, BatchUpdate, Import
│   │   │   └── Transfers/                      # CreateTransfer (saga-coordinated)
│   │   ├── Consumers/                          # Kafka: account/category snapshots, saga events
│   │   ├── Sagas/                              # TransactionSagaState, SagaTimeoutJob
│   │   ├── Persistence/
│   │   │   ├── TransactionsDbContext.cs        # Write context
│   │   │   ├── TransactionsReadDbContext.cs    # Read-only query context
│   │   │   └── Migrations/
│   │   └── Program.cs
│   │
│   ├── BudgetBuddy.Service.Budgets/            # DB: budgets
│   │   ├── Features/
│   │   │   ├── Budgets/                        # CRUD, GetBudgetVsActual
│   │   │   └── BudgetAlerts/                   # GetAlerts, BudgetAlertJob
│   │   ├── Consumers/                          # Kafka: transaction events → spending tracking
│   │   ├── Persistence/
│   │   │   ├── BudgetsDbContext.cs
│   │   │   └── Migrations/
│   │   └── Program.cs
│   │
│   ├── BudgetBuddy.Service.Investments/        # DB: investments
│   │   ├── Features/
│   │   │   ├── Investments/                    # CRUD, BatchDelete, Export, GetPortfolioValue
│   │   │   └── MarketData/                     # PriceSnapshot, FxSnapshot
│   │   ├── Consumers/                          # Kafka: account and price updates
│   │   ├── Persistence/
│   │   │   ├── InvestmentsDbContext.cs
│   │   │   └── Migrations/
│   │   └── Program.cs
│   │
│   ├── BudgetBuddy.Service.Analytics/          # no own DB (read-only, event-driven read models)
│   │   ├── Features/
│   │   │   ├── Dashboard/                      # GetDashboard (from local read models)
│   │   │   └── Reports/                        # IncomeVsExpense, SpendingByCategory,
│   │   │                                       #   MonthlySummary, InvestmentPerformance
│   │   └── Consumers/                          # All domain events → local projections
│   │
│   ├── BudgetBuddy.Service.ReferenceData/      # DB: referencedata
│   │   ├── Features/
│   │   │   ├── Currencies/                     # CRUD + currency sync
│   │   │   ├── Categories/                     # CRUD (user-scoped + built-in)
│   │   │   └── CategoryTypes/                  # CRUD
│   │   ├── Persistence/
│   │   │   ├── ReferenceDataDbContext.cs
│   │   │   └── Migrations/
│   │   └── Program.cs
│   │
│   ├── BudgetBuddy.Service.Financial/          # no DB (external API integration)
│   │   ├── Features/
│   │   │   └── Prices/                         # GetPrice, GetBatchPrices, GetFxRate
│   │   └── Program.cs
│   │
│   └── BudgetBuddy.Service.Notifications/      # no DB
│       ├── Features/
│       │   └── Notifications/                  # GetNotifications, MarkRead
│       ├── Consumers/                          # Kafka: budget alerts, user events
│       └── Program.cs
│
├── Shared/
│   ├── BudgetBuddy.Shared.Kernel/              # Domain primitives (no infrastructure)
│   │   ├── Contracts/                          # ICommand, IQuery, IEvent, AuditableEntity
│   │   ├── Enums/                              # TransactionType, BudgetStatus, etc.
│   │   ├── Exceptions/                         # Domain exceptions
│   │   ├── Identity/                           # User context / claims extraction
│   │   ├── Pagination/                         # Cursor-based pagination
│   │   └── Constants/                          # Cache tags, app-wide constants
│   │
│   ├── BudgetBuddy.Shared.Infrastructure/      # Cross-cutting infrastructure
│   │   ├── Extensions/                         # DI, security, caching, OTel, resilience
│   │   ├── Persistence/
│   │   │   └── Interceptors/                   # RLS, AuditLog, Outbox interceptors
│   │   ├── Messaging/                          # Kafka producer/consumer, outbox, DLQ handling
│   │   ├── Security/                           # Keycloak JWT validation, CORS, idempotency
│   │   ├── Caching/                            # 3-layer cache setup (Output + HybridCache + Redis)
│   │   ├── Financial/                          # ICurrencyConversionService, IPriceService
│   │   ├── DataExchange/                       # Excel import/export (ClosedXML), ClamAV scanning
│   │   ├── Notification/                       # Email service (MailKit)
│   │   ├── Jobs/                               # Outbox processor, audit retention (Quartz base)
│   │   └── Logging/                            # Serilog configuration, Seq integration
│   │
│   └── BudgetBuddy.Shared.Messages/            # Kafka event contracts
│       ├── Topics/                             # Topic name constants
│       ├── ConsumerGroups/                     # Named consumer group constants
│       └── Contracts/                          # Event DTOs for all domains
│
├── Tests/
│   ├── BudgetBuddy.ArchitectureTests/          # NetArchTest — service boundary enforcement
│   ├── BudgetBuddy.SharedKernel.UnitTests/     # Unit tests for shared kernel abstractions
│   ├── BudgetBuddy.Service.UnitTests/          # Domain logic unit tests (Accounts, Budgets,
│   │                                           #   Investments, Transactions)
│   └── BudgetBuddy.Integration.Tests/         # Integration tests (Testcontainers: Postgres + Kafka)
│
└── Infrastructure/
    ├── grafana/provisioning/                   # Pre-built Grafana dashboards and datasources
    ├── keycloak/                               # Realm import (budgetbuddy realm, roles, clients)
    └── prometheus/                             # prometheus.yml scrape config
```

## Kafka Topics

All topics follow the convention `budgetbuddy.<domain>.<event>`.

| Topic | Partitions | Compacted | Description |
|-------|-----------|-----------|-------------|
| `budgetbuddy.accounts.created` | 3 | No | New account created |
| `budgetbuddy.accounts.updated` | 3 | No | Account details changed |
| `budgetbuddy.accounts.deleted` | 3 | No | Account deleted |
| `budgetbuddy.accounts.changelog` | 1 | **Yes** | Full account state (event sourcing) |
| `budgetbuddy.accounts.balance-reserved` | 3 | No | Saga: balance reservation result |
| `budgetbuddy.transactions.created` | 6 | No | New transaction |
| `budgetbuddy.transactions.updated` | 6 | No | Transaction changed |
| `budgetbuddy.transactions.deleted` | 6 | No | Transaction deleted |
| `budgetbuddy.transactions.reversed` | 3 | No | Saga: compensation event |
| `budgetbuddy.budgets.created` | 3 | No | Budget created |
| `budgetbuddy.budgets.updated` | 3 | No | Budget changed |
| `budgetbuddy.budgets.deleted` | 3 | No | Budget deleted |
| `budgetbuddy.budgets.alert-triggered` | 3 | No | Budget threshold crossed |
| `budgetbuddy.investments.created` | 3 | No | Investment created |
| `budgetbuddy.investments.updated` | 3 | No | Investment changed |
| `budgetbuddy.investments.deleted` | 3 | No | Investment deleted |
| `budgetbuddy.investments.price-updated` | 3 | No | Price snapshot updated |
| `budgetbuddy.referencedata.currency-synced` | 1 | No | Currency master data updated |
| `budgetbuddy.referencedata.category-changed` | 2 | No | Category created/updated/deleted |
| `budgetbuddy.accounts.dlq` | 1 | No | DLQ — 7-day retention |
| `budgetbuddy.transactions.dlq` | 1 | No | DLQ — 7-day retention |
| `budgetbuddy.budgets.dlq` | 1 | No | DLQ — 7-day retention |
| `budgetbuddy.investments.dlq` | 1 | No | DLQ — 7-day retention |
| `budgetbuddy.referencedata.dlq` | 1 | No | DLQ — 7-day retention |

## Getting Started

### Prerequisites

**Required:**
- .NET 10 SDK
- Docker + Docker Compose (runs all infrastructure locally)

**Infrastructure (via Docker Compose):**
- PostgreSQL 16 — one instance per service in local dev
- Kafka (KRaft) — single-node for local dev
- Keycloak 26 — identity provider with pre-imported realm
- Redis 7 — distributed cache and idempotency store
- ClamAV — antivirus scanning (takes 2–3 min on startup to update definitions)
- Seq — structured log viewer
- Prometheus + Grafana — metrics and dashboards
- Jaeger — distributed tracing

### Start Infrastructure

```bash
# From the solution root
cd Servers/BudgetBuddy.API.Microservice

# Copy and edit the environment file
cp .env.example .env
# Edit .env: set passwords and secrets

# Start all infrastructure
docker compose up -d

# Wait for Keycloak to be ready (~30 seconds)
docker compose logs -f keycloak
```

Default ports after `docker compose up`:

| Service | Port | URL |
|---------|------|-----|
| Keycloak | 8080 | `http://localhost:8080` |
| Kafka UI | 8090 | `http://localhost:8090` |
| Redis | 6379 | — |
| ClamAV | 3310 | — |
| Seq | 8085 | `http://localhost:8085` |
| Prometheus | 9090 | `http://localhost:9090` |
| Grafana | 3000 | `http://localhost:3000` |
| Jaeger | 16686 | `http://localhost:16686` |

### Configuration

Each service reads from its own `appsettings.json`. The most important settings:

**Connection Strings:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=<service>;Username=postgres;Password=...",
    "Redis": "localhost:6379"
  }
}
```

**Keycloak:**
```json
{
  "Keycloak": {
    "Authority": "http://localhost:8080/realms/budgetbuddy",
    "Audience": "budgetbuddy-api",
    "RequireHttps": false
  }
}
```

**Kafka:**
```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092"
  }
}
```

**Environment Variables:**

| Variable | Required | Description |
|----------|----------|-------------|
| `ConnectionStrings__DefaultConnection` | Yes | PostgreSQL connection string per service |
| `ConnectionStrings__Redis` | Recommended | Redis connection string |
| `Keycloak__Authority` | Yes | Keycloak realm URL |
| `Kafka__BootstrapServers` | Yes | Kafka broker address |
| `ASPNETCORE_ENVIRONMENT` | No | `Development` / `Staging` / `Production` |
| `KeyVault__Url` | Production | Azure Key Vault URL |
| `OpenTelemetry__OtlpEndpoint` | No | Jaeger OTLP endpoint (default: `http://localhost:4317`) |
| `INTERNAL_API_KEY` | Yes | Internal service-to-service API key (min 32 chars) |

**Full config sections (appsettings.json):**

```json
{
  "BackgroundJobs": {
    "Enabled": true,
    "OutboxProcessor": { "Enabled": true, "CronExpression": "0/30 * * * * ?" },
    "DailyPriceSnapshot": { "Enabled": true, "CronExpression": "0 0 22 * * ?" },
    "DailyFxSnapshot": { "Enabled": true, "CronExpression": "0 0 16 * * ?" },
    "BackfillMarketData": { "Enabled": true, "CronExpression": "0 0 3 * * ?" },
    "BudgetAlert": { "Enabled": true, "CronExpression": "0 0 8 * * ?" },
    "SagaTimeout": { "Enabled": true, "CronExpression": "0 0/5 * * * ?" },
    "AuditLogRetention": { "Enabled": true, "CronExpression": "0 0 2 * * ?" }
  },
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
  "RateLimit": {
    "Fixed": { "PermitLimit": 600, "WindowMinutes": 1 },
    "PerUser": { "PermitLimit": 300, "WindowMinutes": 1 }
  }
}
```

### Run Services

Migrations run automatically on startup. Start each service from its own project directory or run all from the solution root:

```bash
# Gateway
dotnet run --project Gateway/BudgetBuddy.Gateway

# Individual services
dotnet run --project Services/BudgetBuddy.Service.Accounts
dotnet run --project Services/BudgetBuddy.Service.Transactions
dotnet run --project Services/BudgetBuddy.Service.Budgets
dotnet run --project Services/BudgetBuddy.Service.Investments
dotnet run --project Services/BudgetBuddy.Service.Analytics
dotnet run --project Services/BudgetBuddy.Service.ReferenceData
dotnet run --project Services/BudgetBuddy.Service.Financial
dotnet run --project Services/BudgetBuddy.Service.Notifications
```

Default service ports (local dev):

| Service | Port |
|---------|------|
| Gateway | 5000 / 5001 |
| Accounts | 5100 |
| Transactions | 5200 |
| Budgets | 5300 |
| Investments | 5400 |
| Analytics | 5500 |
| ReferenceData | 5600 |
| Financial | 5700 |
| Notifications | 5800 |

**API Documentation (per service):** `http://localhost:<port>/scalar/v1`

**Startup sequence (per service):**
1. DbContext auto-migrated on startup
2. Kafka topics created if missing (via `kafka-init` in Docker Compose in local dev)
3. Keycloak Authority validated — fatal on startup in Production if not reachable
4. Quartz.NET scheduler starts configured background jobs
5. Kafka consumers begin polling their topic groups
6. Outbox processor starts (30s interval)

### Adding Migrations

Each service has its own DbContext and migration history. Always specify `--project` and `--context`:

```bash
# Accounts
dotnet ef migrations add <MigrationName> \
  --project Services/BudgetBuddy.Service.Accounts \
  --startup-project Services/BudgetBuddy.Service.Accounts \
  --context AccountsDbContext

# Transactions
dotnet ef migrations add <MigrationName> \
  --project Services/BudgetBuddy.Service.Transactions \
  --startup-project Services/BudgetBuddy.Service.Transactions \
  --context TransactionsDbContext

# Budgets
dotnet ef migrations add <MigrationName> \
  --project Services/BudgetBuddy.Service.Budgets \
  --startup-project Services/BudgetBuddy.Service.Budgets \
  --context BudgetsDbContext

# Investments
dotnet ef migrations add <MigrationName> \
  --project Services/BudgetBuddy.Service.Investments \
  --startup-project Services/BudgetBuddy.Service.Investments \
  --context InvestmentsDbContext

# ReferenceData
dotnet ef migrations add <MigrationName> \
  --project Services/BudgetBuddy.Service.ReferenceData \
  --startup-project Services/BudgetBuddy.Service.ReferenceData \
  --context ReferenceDataDbContext
```

Migrations are applied automatically on the next service startup.

## API Endpoints

All endpoints are accessed through the Gateway at `http://localhost:5000`.
Direct service ports are available for development only.

**Authentication (Keycloak — not proxied through services):**
```
POST /auth/realms/budgetbuddy/protocol/openid-connect/token
POST /auth/realms/budgetbuddy/protocol/openid-connect/logout
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
GET    /api/portfolio/value
```

**Analytics:**
```
GET /api/dashboard
GET /api/reports/income-expense
GET /api/reports/spending-by-category
GET /api/reports/monthly-summary
GET /api/reports/investment-performance
```

**Reference Data:**
```
GET/POST/PUT/DELETE /api/currencies
GET/POST/PUT/DELETE /api/categories
GET/POST/PUT/DELETE /api/category-types
```

**Market Data (Financial Service):**
```
GET /api/prices/{symbol}
GET /api/prices/batch
GET /api/prices/fx/{from}/{to}
```

**Observability (per service):**
```
GET /health
GET /health/ready
GET /health/live
GET /metrics
```

## Testing

```bash
# All tests
dotnet test

# Architecture tests only
dotnet test Tests/BudgetBuddy.ArchitectureTests

# Unit tests
dotnet test Tests/BudgetBuddy.SharedKernel.UnitTests
dotnet test Tests/BudgetBuddy.Service.UnitTests

# Integration tests (requires Docker — Testcontainers spins up Postgres + Kafka)
dotnet test Tests/BudgetBuddy.Integration.Tests

# With coverage report
dotnet test --collect:"XPlat Code Coverage"
```

**Coverage target:** 80% minimum, enforced in CI.

**Architecture tests** (NetArchTest) enforce:
- Services must not reference other services' assemblies
- Domain layer must not depend on infrastructure
- All handlers must inherit from correct MediatR base types
- Shared.Messages is the only allowed cross-service coupling point

**Integration tests** use Testcontainers to spin up real PostgreSQL and Kafka instances, and WireMock.Net for external HTTP providers (Yahoo Finance, CoinGecko, Frankfurter).

## Kubernetes Deployment (Helm)

A single parametrized Helm chart with per-service value overrides:

```bash
# Deploy a service (example: accounts)
helm upgrade --install accounts-service charts/budgetbuddy \
  -f charts/budgetbuddy/values-accounts.yaml \
  --set image.tag=<commit-sha> \
  --namespace budgetbuddy

# Deploy all services
for svc in accounts transactions budgets investments analytics referencedata financial notifications gateway; do
  helm upgrade --install ${svc}-service charts/budgetbuddy \
    -f charts/budgetbuddy/values-${svc}.yaml \
    --set image.tag=<commit-sha> \
    --namespace budgetbuddy
done
```

**Default Helm values:**

| Parameter | Default |
|-----------|---------|
| `replicaCount` | 2 |
| `resources.requests.cpu` | 100m |
| `resources.requests.memory` | 256Mi |
| `resources.limits.cpu` | 500m |
| `resources.limits.memory` | 512Mi |
| HPA min replicas | 2 |
| HPA max replicas | 10 |
| HPA CPU target | 70% |

Each deployment includes:
- Liveness probe: `/health/live` (30s interval)
- Readiness probe: `/health/ready` (10s interval)
- Startup probe (5s initial delay)
- Pod Disruption Budget (PDB) — at least 1 pod available during rollouts

## CI/CD

GitHub Actions pipeline (`.github/workflows/ci.yml`) triggers on push and PRs to `main`.

**Job 1 — build-and-test:**
1. NuGet vulnerability check — fails on HIGH/CRITICAL
2. Build (Release, TreatWarningsAsErrors)
3. Unit tests + SharedKernel tests
4. Coverage validation (80% minimum enforced)
5. Architecture tests (NetArchTest)
6. Integration tests (Testcontainers)
7. Test results uploaded (dorny/test-reporter)

**Job 2 — container-scan** (runs after build-and-test, matrix over all 9 images):
1. Build Docker image per service
2. Trivy vulnerability scan — fails on HIGH/CRITICAL unfixed CVEs

## Troubleshooting

**Keycloak not starting:**
- Check `docker compose logs keycloak`
- Realm import fails if `Infrastructure/keycloak/` realm JSON is malformed
- Keycloak admin UI: `http://localhost:8080` (admin/admin123 from `.env`)

**Service fails to start: `Keycloak Authority not reachable`:**
- Keycloak must be running and healthy before services start
- Set `Keycloak__RequireHttps=false` for local dev
- Check `Keycloak__Authority` matches your realm URL exactly

**Kafka consumers not receiving messages:**
- Check consumer group offsets via Kafka UI: `http://localhost:8090`
- Verify topic exists (created by `kafka-init` on `docker compose up`)
- Check `Kafka__BootstrapServers` is reachable from the service

**Messages ending up in DLQ:**
- Check Seq for deserialization errors or handler exceptions
- DLQ topics: `budgetbuddy.<domain>.dlq` (visible in Kafka UI)
- 7-day retention — reprocessing requires a custom consumer or manual offset reset

**Saga stuck / transaction not completing:**
- `SagaTimeoutJob` runs every 5 minutes and compensates timed-out sagas
- Check `TransactionSagaState` table in the transactions database
- Check Seq for saga step failures

**Migrations fail on startup (`relation already exists`):**

```sql
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('<migration-id>', '10.0.3');
```

Then restart the service.

**ClamAV not scanning files:**
- ClamAV takes 2–3 minutes on startup to update virus definitions
- Check: `docker compose logs clamav`
- Test: `docker exec clamav clamdscan --version`

**Price data missing:**
- CoinGecko free tier has rate limits — add a free API key in `PriceService:CoinGeckoApiKey`
- Historical backfill runs automatically at 03:00 UTC via `BackfillMarketDataJob`

**Redis connection issues:**
- Test: `docker exec redis redis-cli ping` (should return PONG)
- Services fall back to in-memory cache if Redis is unavailable, but idempotency and token management require Redis

**Background jobs not running:**
- Check `BackgroundJobs:Enabled: true` in the service's config
- Check individual job `Enabled` flags
- Cron expression format: Quartz 6-field (`sec min hour day month weekday`)

## Production Deployment

### Pre-Deployment Checklist

- ✅ PostgreSQL 16+ accessible with SSL Mode=Require for each service database
- ✅ Kafka cluster (minimum 3 brokers for production, KRaft or ZK)
- ✅ Keycloak cluster with `budgetbuddy` realm configured
- ✅ Redis Sentinel or Redis Cluster for HA
- ✅ ClamAV for file upload scanning
- ✅ Azure Key Vault for secrets and Data Protection keys
- ✅ All service migrations applied (automatic on startup)
- ✅ `ASPNETCORE_ENVIRONMENT=Production` set on all services
- ✅ `Keycloak__RequireHttps=true` set
- ✅ HSTS enabled at gateway level
- ✅ Internal API key set (min 32 chars, rotated regularly)
- ✅ Grafana dashboards provisioned, Prometheus scrape targets configured

**Azure Key Vault Secrets (example for Accounts service):**

```bash
az keyvault secret set --vault-name <vault> --name "Accounts--ConnectionStrings--DefaultConnection" --value "<conn>"
az keyvault secret set --vault-name <vault> --name "Accounts--ConnectionStrings--Redis"             --value "<redis>"
az keyvault secret set --vault-name <vault> --name "Keycloak--Authority"                            --value "<realm-url>"
az keyvault secret set --vault-name <vault> --name "Kafka--BootstrapServers"                        --value "<brokers>"
az keyvault secret set --vault-name <vault> --name "DataProtection--MasterKey"                      --value "<base64-key>"
az keyvault secret set --vault-name <vault> --name "INTERNAL_API_KEY"                               --value "<secret>"
```

**Critical Post-Deploy Steps:**
1. Verify `/health/ready` returns 200 on all services
2. Verify Kafka consumer groups are balanced (Kafka UI or `kafka-consumer-groups.sh`)
3. Verify Quartz jobs appear in logs within their first scheduled window
4. Set up monitoring alerts on `/health/ready` for on-call
5. Implement data retention policy for DLQ topics (7-day default; archive or purge)
6. Schedule regular PostgreSQL backups per service database
7. Configure Keycloak brute force protection and session limits in realm settings

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
