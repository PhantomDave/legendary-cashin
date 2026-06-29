# Where Is My Money

A personal finance tracker with automatic bank transaction imports via [Enable Banking](https://enablebanking.com).

## Stack

| Layer | Tech |
|---|---|
| Backend | .NET 10 / ASP.NET Core, EF Core 10, PostgreSQL |
| Frontend | Angular 22, PrimeNG 21, Tailwind 4, Bun |
| Auth | JWT (Bearer), stored as cookie on the client |
| Infra | Docker Compose, GitHub Actions, WireGuard homelab deploy |

## API endpoints

| Controller | Routes | Notes |
|---|---|---|
| Accounts | `POST /accounts`, `POST /authenticate`, `GET /accounts/{id}`, `GET /accounts/{email}` | Registration + login are `[AllowAnonymous]` |
| Transactions | `GET /transactions`, `POST /transactions`, `PATCH /transactions/{id}`, `DELETE /transactions/{id}` | Paginated list |
| Recurring | `GET /transactions/recurring`, `POST /transactions/recurring`, `PATCH /transactions/recurring/{id}`, `DELETE /transactions/recurring/{id}` | Auto-materialized by `ScheduledTransactionProcessor` |
| Budgets | `GET /budgets`, `POST /budgets`, `PATCH /budgets/{id}`, `DELETE /budgets/{id}` | |
| Categories | `GET /categories`, `POST /categories`, `PATCH /categories/{id}`, `DELETE /categories/{id}` | |
| Import | `POST /banking/initiate`, `GET /banking/callback`, `POST /banking/sessions`, `GET /banking/transactions`, `POST /banking/force-sync` | Enable Banking OAuth flow |
| System | `GET /`, `GET /health` | |

OpenAPI JSON: `GET /openapi/v1.json` · Scalar UI: `GET /scalar/v1`

## Local dev setup

```fish
# 1. Start the database
docker compose up -d db

# 2. Run the API (port 5080)
dotnet run --project backend/src/WhereIsMyMoney.Api/WhereIsMyMoney.Api.csproj

# 3. Run the frontend (port 4200)
cd frontend/WhereIsMyMoneyUI && bun install && bun run start
```

A **devcontainer** is provided (`.devcontainer/`) for VS Code, Rider, and WebStorm — ports 5080, 4200, and 5000 are forwarded automatically.

## Enable Banking import

Bank connection uses an OAuth2 browser-redirect flow via the Enable Banking API. See [`AUTO_IMPORT_FLOW.md`](AUTO_IMPORT_FLOW.md) for the full flow diagram and implementation details.

## Developer guide

See [`AGENTS.md`](AGENTS.md) for repo structure, backend/frontend patterns, and integration gotchas.
