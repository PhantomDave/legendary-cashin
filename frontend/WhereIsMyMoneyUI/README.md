# WhereIsMyMoneyUI

Angular 22 SPA for the [Where Is My Money](../../README.md) personal finance tracker.

## Stack

- **Angular 22** standalone components, signals-based state
- **PrimeNG 21** + PrimeIcons 7 + PrimeUX Themes 2 for UI
- **Tailwind CSS 4** (PostCSS import in `src/styles.css`)
- **Bun 1.3.3** as package manager and script runner
- **Vitest** for unit testing
- **TypeScript 6** in strict mode

## Folder structure

```text
src/app/
  core/
    navigation/      # app-level constants (menu items, route names)
    services/        # cross-feature infrastructure (api, auth, budget, category, transaction, import, account, toast)
    guards/          # auth.guard.ts — protects authenticated routes
    interceptors/    # auth.interceptor.ts — attaches Bearer token from cookie
  layout/
    shell/           # top-level frame: header, nav, <router-outlet>
  shared/            # reusable pipes, directives, and presentational components
  components/        # feature components (dialogs, charts, steppers, tables)
  pages/             # route-level components (dashboard, transactions, categories, budgets, import, account, not-found)
```

## Naming conventions

- Components: `something.component.ts|html|scss`
- Services: `something.service.ts`
- Models and DTOs: `something.model.ts`
- Type aliases/unions: `something.types.ts`

## Routing pattern

- `src/app/app.routes.ts` uses a shell route with lazy `loadComponent` pages.
- New pages go in `pages/` and are wired once in `app.routes.ts`.
- Shared sub-components for a page go in `components/`.

## Theme setup

- PrimeNG is configured in `src/app/app.config.ts` with Aura preset and ripple enabled.
- Global style imports are in `src/styles.css` (`tailwindcss` and `primeicons`).
- Use semantic PrimeNG tokens (`--p-content-background`, etc.) — see AGENTS.md Styling section.

## Run locally

```bash
bun install
bun run start
```

## Verify

```bash
bun run build
bun run test -- --watch=false
```

## Angular CLI with Bun

```bash
bunx ng generate component components/my-component
bunx ng generate service core/services/my-service
```
