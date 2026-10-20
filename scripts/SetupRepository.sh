#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd -- "$repo_root"
if ! command -v dotnet >/dev/null 2>&1; then
    echo "Install a stable .NET 10 SDK from https://dotnet.microsoft.com/download/dotnet/10.0, then reopen your terminal." >&2
    exit 1
fi
if ! sdk="$(dotnet --version 2>/dev/null)" || [[ ! "$sdk" =~ ^10\.[0-9]+\.[0-9]+$ ]]; then
    echo "No stable .NET 10 SDK found. Install one (10.0.100 or newer; release candidates do not work) from https://dotnet.microsoft.com/download/dotnet/10.0, then reopen your terminal." >&2
    exit 1
fi
exec dotnet run --project "$repo_root/scripts/SWLOR.ContributorSetup/SWLOR.ContributorSetup.csproj" -p:RunPostBuildEvent=Never -- "$@"
