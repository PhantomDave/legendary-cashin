# Dev-container image: .NET 10 SDK + Bun (for Angular CLI)
FROM mcr.microsoft.com/dotnet/sdk:10.0

# Install base tools
RUN apt-get update && apt-get install -y curl gnupg unzip \
    git postgresql-client bash \
    && rm -rf /var/lib/apt/lists/*

# Install Bun
RUN curl -fsSL https://bun.sh/install | bash
ENV PATH="/root/.bun/bin:$PATH"

# Install Angular CLI globally via Bun
RUN bun install -g @angular/cli@21

# Trust the .NET dev certificate
RUN dotnet dev-certs https --trust 2>/dev/null || true

WORKDIR /workspace


