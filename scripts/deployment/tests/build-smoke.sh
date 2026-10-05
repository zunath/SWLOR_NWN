#!/usr/bin/env bash
# Run in the verification container with repository mounted read-only at /repo.
# Uses current real C# projects and native tools, but only tiny content fixtures.
set -Eeuo pipefail
mkdir -p /src /tools /work /artifacts /nuget
for project in SWLOR.CLI SWLOR.Game.Server SWLOR.NWN.API SWLOR.NWN.Formats; do
    mkdir -p "/src/$project"
    tar -C "/repo/$project" --exclude=bin --exclude=obj -cf - . | tar -C "/src/$project" -xf -
done
cp /repo/Directory.Build.targets /src/
curl --fail --show-error --silent --location --connect-timeout 10 --max-time 180 \
    https://github.com/niv/neverwinter.nim/releases/download/2.1.2/neverwinter-x86_64-linux-gnu.zip -o /tmp/tools.zip
printf '%s  %s\n' f9cc2e50fbe6f750954d11b824434a92b12913cc0a3442e95dcc5eefd4ddc387 /tmp/tools.zip | sha256sum --check --strict
unzip -q /tmp/tools.zip -d /tools
chmod +x /tools/nwn_erf /tools/nwn_gff /tools/nwn_tlk
python3 /repo/scripts/deployment/tests/build_fixture.py
# Execute the exact production container build script, not a parallel test copy.
bash /repo/scripts/deployment/swlor-production-build.sh 'Star Wars LOR v2.mod'
test -s /artifacts/dotnet/SWLOR.Game.Server.dll
test -s /artifacts/hak/fixture.hak
test -s /artifacts/tlk/sw_tlk.tlk
test -s '/artifacts/modules/Star Wars LOR v2.mod'
printf 'Real Docker build, HAK/SET packing, TLK staging, and module packing passed.\n'
