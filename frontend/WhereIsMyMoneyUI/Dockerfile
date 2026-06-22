# ── Build stage ───────────────────────────────────────────────────────────────
FROM oven/bun:1.3-alpine AS build

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
    CMD wget -qO- http://localhost/ > /dev/null || exit 1
