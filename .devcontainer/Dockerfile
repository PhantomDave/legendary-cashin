# Dev-container image: .NET 10 SDK + Bun (for Angular CLI)
FROM mcr.microsoft.com/dotnet/sdk:10.0

# Install base tools, Bun, and Angular CLI
RUN apt-get update \
    && apt-get install -y curl gnupg unzip git postgresql-client bash \
    && rm -rf /var/lib/apt/lists/* \
    && curl -fsSL https://bun.sh/install | bash

ENV PATH="/root/.bun/bin:$PATH"

RUN bun install -g @angular/cli@21 \
    && dotnet dev-certs https 2>/dev/null || true

WORKDIR /workspace


