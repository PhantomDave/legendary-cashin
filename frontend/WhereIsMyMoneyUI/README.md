# WhereIsMyMoneyUI PrimeNG Boilerplate

This app is a starter structure for Angular + PrimeNG with lazy feature pages and a reusable shell layout.

## Stack

- Angular standalone components
- PrimeNG (`primeng`, `primeicons`, `@primeuix/themes`)
- Tailwind base import in `src/styles.css`

## Folder structure (Angular-style, feature-first)

```text
src/app
  core/
	navigation/        # app-level constants/types (menu items, config)
	services/          # cross-feature infrastructure (api, auth, storage)
	guards/            # route guards used across multiple features
	interceptors/      # HTTP interceptors
  layout/
	shell/             # top-level frame: header, nav, <router-outlet>
  shared/
	ui/                # reusable presentational components
	pipes/             # reusable pipes
	directives/        # reusable directives
  features/
	dashboard/
	  pages/           # route-level components
	  components/      # feature-only components
	  models/          # feature models/types (interfaces, aliases)
	  services/        # feature data orchestration
	  state/           # signals/store/facades for the feature
	transactions/
	  pages/
	  components/
	  models/
	  services/
	  state/
	not-found/
	  pages/
```

## Naming conventions

- Components: `something.component.ts|html|scss`
- Services: `something.service.ts`
- Models and DTOs: `something.model.ts`
- Type aliases/unions: `something.types.ts`
- Keep page-level types in `features/<feature>/models` instead of inline in component files.

## Routing pattern

- `src/app/app.routes.ts` uses a shell route with lazy feature `loadComponent` pages.
- New features should be added under `src/app/features/<feature-name>/...` and wired once in `app.routes.ts`.
- Route components belong in `pages/`; child reusable parts belong in `components/`.

## Theme setup

- PrimeNG is configured in `src/app/app.config.ts` with Aura preset and ripple enabled.
- Global style imports are in `src/styles.css` (`tailwindcss` and `primeicons`).

## Run locally

```bash
bun install
bun run start
```

## Verify boilerplate health

```bash
bun run build
bun run test -- --watch=false
```

## Angular CLI with Bun

```bash
bunx ng generate component features/reports/pages/reports-page
bunx ng generate service features/reports/services/reports
bunx ng build
```

`bunx ng build` and `bun run build` both use Angular CLI build behavior described in Angular docs.

## Next suggested additions

- Add `core/services/api` and `core/services/auth` as app-wide infrastructure.
- For each feature, move contracts to `models/` and avoid inline interfaces in components.
- Keep feature state in `features/<feature>/state` and expose a facade to pages.
