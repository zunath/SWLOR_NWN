#!/usr/bin/env bash
# Runs only inside the disposable SDK container. No live server is mounted.
set -Eeuo pipefail
umask 0022
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
export DOTNET_CLI_HOME=/work/dotnet-home NUGET_PACKAGES=/nuget
export SWLOR_RESOURCE_CONVERSION_WORKERS=2
module_name="$1"
mkdir -p /work/source /artifacts/{dotnet,hak,tlk,modules}
for project in SWLOR.CLI SWLOR.Game.Server SWLOR.NWN.API SWLOR.NWN.Formats; do
    mkdir -p "/work/source/$project"
    # Exclude old local build products even when a source checkout was reused.
    tar -C "/src/$project" --exclude=bin --exclude=obj -cf - . |
        tar -C "/work/source/$project" -xf -
done
for file in Directory.Build.props Directory.Build.targets Directory.Packages.props NuGet.config global.json; do
    [[ ! -f "/src/$file" ]] || cp "/src/$file" /work/source/
done
dotnet build /work/source/SWLOR.CLI/SWLOR.CLI.csproj \
    --configuration Release --property:OS=Unix --property:RunPostBuildEvent=Never
cli=/work/source/SWLOR.CLI/bin/Release/net10.0
for tool in nwn_erf nwn_gff nwn_tlk; do
    ln -sfn "/tools/$tool" "$cli/$tool.exe"
done
cp -a /work/source/SWLOR.Game.Server/bin/Release/net10.0/. /artifacts/dotnet/
cd /work
dotnet "$cli/SWLOR.CLI.dll" --hak

# Check the packaged SET resources against their source bytes, including CRLF.
while IFS= read -r -d '' source_set; do
    relative="${source_set#/src/SWLOR_Haks/}"
    hak="${relative%%/*}"
    resource="${relative##*/}"
    mkdir -p "/work/set-check/$hak"
    (cd "/work/set-check/$hak"; /tools/nwn_erf --quiet -f "/artifacts/hak/$hak.hak" -x "$resource")
    cmp "$source_set" "/work/set-check/$hak/$resource"
done < <(find /src/SWLOR_Haks -type f -name '*.set' -print0)

cp -a /src/Module /work/source/Module
cd /work/source/Module
dotnet "$cli/SWLOR.CLI.dll" --pack "./$module_name" --no-prompt
cp "$module_name" /artifacts/modules/
# Checksum sidecars are build-cache files, not deployed content.
find /artifacts/hak -maxdepth 1 -name '*.md5' -delete
