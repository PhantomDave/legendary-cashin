# Dev-container image: .NET 10 SDK + Bun (for Angular CLI)
FROM mcr.microsoft.com/dotnet/sdk:10.0

# Install base tools, fish shell, Node.js, Bun, and Angular CLI
RUN apt-get update \
    && apt-get install -y \
        curl \
        gnupg \
        unzip \
        git \
        postgresql-client \
        bash \
        fish \
        ca-certificates \
        openssh-client \
        less \
        procps \
        jq \
    && curl -fsSL https://deb.nodesource.com/setup_22.x | bash - \
    && apt-get install -y nodejs \
    && rm -rf /var/lib/apt/lists/* \
    && curl -fsSL https://bun.sh/install | bash

ENV PATH="/root/.bun/bin:$PATH"

RUN bun install -g @angular/cli@21 \
    && dotnet dev-certs https 2>/dev/null || true

WORKDIR /workspace


