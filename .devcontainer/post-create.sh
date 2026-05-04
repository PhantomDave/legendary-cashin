#!/usr/bin/env bash
set -e

echo "==> Restoring .NET packages..."
dotnet restore /workspace/backend/CashinService.slnx

echo "==> Installing frontend packages with Bun..."
cd /workspace/frontend/WhereIsMyMoneyUI
bun install

echo "==> Dev-container setup complete!"


