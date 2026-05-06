# AGENTS Guide

## Scope and precedence
- This root guide applies to the whole repo (`/workspace`).
- Frontend-specific AI rules in `frontend/WhereIsMyMoneyUI/AGENTS.md` are stricter for files under that app.
- Treat `README.md` at repo root as stale (it references old `CashinService` paths).

## Repo map (what exists now)
- `backend/src/WhereIsMyMoney.Api`: ASP.NET Core API (`net10.0`) with EF Core + PostgreSQL + JWT auth.
- `backend/src/WhereIsMyMoney.Import`: CLI/web hybrid import tool for Enable Banking (external API + browser callback flow).
- `frontend/WhereIsMyMoneyUI`: Angular 21 + PrimeNG SPA using Bun.
- `docker-compose.yml`: local Postgres (`cashin/cashin`) used by API default connection string.

## Critical runtime flow
- API startup (`backend/src/WhereIsMyMoney.Api/Program.cs`) runs `db.Database.MigrateAsync()` automatically; schema is migration-driven.
- All API controllers are globally protected via `app.MapControllers().RequireAuthorization()`.
- Public endpoints must be explicitly marked `[AllowAnonymous]` (currently account registration + authentication).
- Frontend auth token is `authToken` in cookies; requests gain `Authorization: Bearer ...` via `auth.interceptor.ts`.
- Frontend API base URL is `http://localhost:5080` in `frontend/WhereIsMyMoneyUI/src/environments/environment.ts`.

## Backend patterns to follow
- Controllers are thin; business/data logic lives in stores (`Services/*Store.cs`) using `AppDbContext`.
- Account scoping comes from JWT claim in `ApiControllerBase.GetAccountId()` (`NameIdentifier` or `sub`).
- For account-owned resources, controller sets/validates `AccountId` before store calls (see `BudgetsController`, `TransactionsController`, `CategoriesController`).
- Response DTO projection is done with `ToResponse(...)` helpers inside stores.
- Monetary fields use `decimal(18,2)` in `AppDbContext` model configuration.

## Frontend patterns to follow
- Use app-level services with Angular signals for state/loading/error (`budget.service.ts`, `transaction.service.ts`, `account.service.ts`).
- Route protection is centralized in `guards/auth.guard.ts` + cookie presence checks.
- HTTP is wrapped by `ApiService` (promise-based over `HttpClient` + `firstValueFrom`), not direct per-component `HttpClient` calls.
- UI is standalone-component based with PrimeNG configured in `app.config.ts`.

## Developer workflows
- Start DB:
  - `docker compose up -d db`
- Run API:
  - `dotnet run --project backend/src/WhereIsMyMoney.Api/WhereIsMyMoney.Api.csproj`
- Build backend solution:
  - `dotnet build backend/WhereIsMyMoney.slnx`
- Run frontend:
  - `cd frontend/WhereIsMyMoneyUI && bun install && bun run start`
- Frontend checks:
  - `cd frontend/WhereIsMyMoneyUI && bun run build && bun run test -- --watch=false`

## Integration points and gotchas
- CORS in API currently allows only `http://localhost:4200` with credentials.
- JWT settings are in `backend/src/WhereIsMyMoney.Api/appsettings.json`; local key is a placeholder and should be overridden per environment.
- Import tool depends on Enable Banking credentials (`backend/src/WhereIsMyMoney.Import/appsettings.json`) and external tools (ngrok/browser callback).
- The import flow currently hardcodes an ngrok redirect override in `WhereIsMyMoney.Import/Program.cs`; keep this in mind when debugging auth callbacks.
- Route shape note: `accounts/{id:long}` and `accounts/{email}` coexist in `AccountsController`; keep constraints explicit when adding routes.

