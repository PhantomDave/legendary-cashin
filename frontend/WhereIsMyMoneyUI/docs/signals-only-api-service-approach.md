# Signals-First API Service Approach (Angular)

This document defines the best approach for connecting the frontend to the backend while keeping **component-facing state Signal-only**.

> Important: Angular `HttpClient` is Observable-based internally. A practical "signals only" approach means **Observables stay in the data layer**, while pages/components consume **Signals only** via facades.

## Goals

- Keep UI state fully signal-driven (`signal`, `computed`, `resource`).
- Keep HTTP and transport concerns isolated in API clients.
- Standardize loading/error/success patterns across features.
- Centralize auth token usage and avoid direct `localStorage` access in pages.
- Make migration easy from the current structure.

## Current baseline (in this repo)

- Generic transport service: `src/app/services/api.service.ts`
- Auth interceptor: `src/app/interceptors/auth.interceptor.ts`
- Auth guard: `src/app/guards/auth.guard.ts`
- Routes include shell and account pages: `src/app/app.routes.ts`

## Recommended target architecture

Use a feature-first structure while introducing a small core layer:

```text
src/app
  core/
    services/
      api/
        http-api.service.ts        # low-level wrapper around HttpClient
      auth/
        auth-session.service.ts    # token/session signals + persistence boundary
      errors/
        api-error.mapper.ts        # server error -> UI-safe error model
    interceptors/
      auth.interceptor.ts
      error.interceptor.ts
    guards/
      auth.guard.ts
  features/
    <feature>/
      models/
        *.dto.ts                   # backend contracts
        *.model.ts                 # UI domain models
      services/
        <feature>-api.service.ts   # endpoint-specific data access
      state/
        <feature>.facade.ts        # signal-only API consumed by components
      pages/
        ...
```

## Layer responsibilities

1. `http-api.service.ts` (transport only)
- Concatenate base URL, set common options, call `HttpClient`.
- Return typed `Observable<T>`.
- No UI state logic.

2. `<feature>-api.service.ts` (endpoint client)
- Own endpoint URLs for one feature (`/accounts`, `/budgets`, etc.).
- Map DTOs when needed, keep request/response typing strict.
- Return `Observable` results.

3. `<feature>.facade.ts` (signals facade)
- Convert endpoint operations into signal state.
- Expose only signals/functions to pages/components.
- Own `data`, `isLoading`, `error`, and action methods (`load`, `create`, `refresh`).

4. Components/pages
- Read only signals from the facade.
- No direct `HttpClient`, no direct `ApiService`, no direct `localStorage`.

## Recommended state pattern for facades

Each facade should expose a consistent contract:

- `items` or `entity` (data signal)
- `isLoading` (signal)
- `error` (signal with UI-safe error type)
- `hasData` / `isEmpty` (computed)
- actions: `load()`, `reload()`, `create(input)`, etc.

Use one of two patterns depending on request type:

- **Read/query-heavy state**: use `resource` (or `toSignal` with explicit trigger signal).
- **Command/mutation-heavy state**: use explicit action methods that set `isLoading/error` signals and update state on success.

## Auth and token lifecycle

### Keep these rules

- Interceptor injects token into outgoing requests.
- Guard checks an auth session signal, not `localStorage` directly.
- A dedicated `auth-session.service.ts` is the single source of truth for token state.

### Minimal flow

1. On app start, hydrate token into `authSession` signal from storage.
2. `authInterceptor` reads token from `authSession` (or fallback storage during migration).
3. On `401`, clear session and redirect to login/register route.
4. Guard returns `UrlTree` when session is unauthenticated.

## Error handling standard

- Normalize backend errors into a small `UiError` model:
  - `code` (optional backend/business code)
  - `message` (user-safe)
  - `status` (HTTP status)
- Never pass raw backend error objects directly to templates.
- Keep logging details in interceptor/service, not in components.

## Loading and UX standard

- Page-level initial fetch: full-page skeleton/spinner.
- Inline actions (create/update/delete): button-level pending state.
- Prevent duplicate submissions while mutation is pending.

## Migration plan from current code

### Phase 1: Core boundaries

- Keep current `ApiService` temporarily.
- Add `auth-session.service.ts` and route all token reads/writes through it.
- Update `auth.interceptor.ts` and `auth.guard.ts` to use session service.

### Phase 2: Pilot one feature

- Create one endpoint client and one facade (for example budgets).
- Move page to consume facade signals only.
- Introduce `UiError` mapping and standardized loading state.

### Phase 3: Rollout and cleanup

- Repeat for remaining features.
- Remove direct API calls from components.
- Move old `src/app/services/*` into `core/services/*` and feature services.

## Practical dos and don'ts

### Do

- Keep `Observable` in transport/data-access layer only.
- Keep components dumb and signal-driven.
- Use strict DTO/model mapping at feature boundaries.
- Keep facade contracts stable and predictable.

### Don't

- Mix `subscribe()` calls in components.
- Read/write `localStorage` in guards/pages/components.
- Return raw `HttpErrorResponse` to UI.
- Put business logic into interceptors.

## Example facade shape (interface only)

```ts
export interface BudgetsFacade {
  budgets: Signal<BudgetModel[]>;
  isLoading: Signal<boolean>;
  error: Signal<UiError | null>;
  hasBudgets: Signal<boolean>;

  load(): void;
  create(input: CreateBudgetInput): Promise<void>;
  refresh(): void;
}
```

## Decision note

Given Angular's HTTP API, the strongest long-term approach is:

- **Observable transport + Signal facade boundary**

This gives reliable HTTP integration, testable layering, and a true signals-only developer experience at the component level.

