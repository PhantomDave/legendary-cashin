#!/usr/bin/env bash
set -e

echo "==> Restoring .NET packages..."
dotnet restore /workspace/backend/CashinService.slnx

echo "==> Installing dotnet tools..."
dotnet tool install --global dotnet-ef

echo "==> Installing frontend root packages..."
cd /workspace/frontend
bun install

echo "==> Installing Angular project packages..."
cd /workspace/frontend/WhereIsMyMoneyUI
bun install

echo "==> Dev-container setup complete!"


