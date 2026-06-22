# ── Build stage ───────────────────────────────────────────────────────────────
# Bun stable Alpine images currently report Node.js v24.3.0, but Angular 22
# requires >= v24.15. Pinning this canary digest keeps builds reproducible
# until a stable Bun Alpine image satisfies the Angular runtime requirement.
FROM oven/bun:canary-alpine@sha256:73c0ba32511da25fb50b88eaec19f4b37b05c263ce08ec942a349801ccfdadcc AS build

WORKDIR /app

# Install dependencies first (layer-cached unless lockfile changes)
COPY package.json bun.lock ./
RUN bun install --frozen-lockfile

# Copy source and build
COPY . .
RUN bun run build

# ── Runtime stage ──────────────────────────────────────────────────────────────
FROM nginx:alpine AS runtime

# Copy built Angular app (dist output: dist/WhereIsMyMoneyUI/browser/)
COPY --from=build /app/dist/WhereIsMyMoneyUI/browser /usr/share/nginx/html

# Custom nginx config for SPA routing
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

HEALTHCHECK --interval=15s --timeout=5s --start-period=10s --retries=3 \
    CMD wget -qO- http://127.0.0.1/ > /dev/null || exit 1
