#!/usr/bin/env bash
set -e

echo "==> Restoring .NET packages..."
dotnet restore /workspace/backend/WhereIsMyMoney.slnx

echo "==> Installing dotnet tools..."
dotnet tool install --global dotnet-ef

echo "==> Installing Angular project packages..."
cd /workspace/frontend/WhereIsMyMoneyUI
bun install

echo "==> Dev-container setup complete!"


