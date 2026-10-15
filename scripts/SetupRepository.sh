#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd -- "$repo_root"
if ! command -v dotnet >/dev/null 2>&1; then
    echo "Install a stable .NET 10 SDK from https://dotnet.microsoft.com/download/dotnet/10.0, then reopen your terminal." >&2
    exit 1
fi
if ! sdk="$(dotnet --version 2>/dev/null)" || [[ ! "$sdk" =~ ^10\.[0-9]+\.[0-9]+$ ]]; then
    echo "Install a stable .NET 10 SDK. global.json selects the newest installed .NET 10 feature band; previews are not used." >&2
    exit 1
fi
exec dotnet run --project "$repo_root/scripts/SWLOR.ContributorSetup/SWLOR.ContributorSetup.csproj" -p:RunPostBuildEvent=Never -- "$@"
