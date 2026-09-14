# SPDX-License-Identifier: MIT

[CmdletBinding(DefaultParameterSetName = "Verify")]
param(
    [Parameter(ParameterSetName = "Capture", Mandatory = $true)]
    [switch]$Capture,

    [Parameter(ParameterSetName = "Verify", Mandatory = $true)]
    [switch]$Verify,

    [Parameter(ParameterSetName = "Capture")]
    [string]$OutputPath
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$baselinePath = if ([string]::IsNullOrWhiteSpace($OutputPath))
{
    Join-Path $repositoryRoot "SWLOR.NWN.Formats.Corpus.Tests\LicensedCorpusBaseline.json"
}
else
{
    [IO.Path]::GetFullPath($OutputPath)
}

$testArguments = @(
    "test",
    "SWLOR.NWN.Formats.Corpus.Tests\SWLOR.NWN.Formats.Corpus.Tests.csproj",
    "-p:RunPostBuildEvent=Never",
    "--filter",
    "TestCategory=LicensedCorpus",
    "--logger",
    "console;verbosity=normal",
    "--nologo"
)

Push-Location $repositoryRoot
try
{
    # Baseline evidence must never come from a silently skipped run: the availability gate turns
    # missing licensed assets into a hard failure under this variable.
    $env:SWLOR_REQUIRE_LICENSED_CORPUS = "1"
    $testOutput = @(& dotnet @testArguments 2>&1)
    $testExitCode = $LASTEXITCODE
}
finally
{
    Pop-Location
}

$testOutput | ForEach-Object { Write-Output $_ }
if ($testExitCode -ne 0)
{
    throw "Licensed corpus verification failed with exit code $testExitCode."
}

$text = ($testOutput | ForEach-Object { "$_".Trim() }) -join "`n"

function Match-Summary
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$Pattern,

        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    $match = [regex]::Match($text, $Pattern)
    if (-not $match.Success)
    {
        throw "Could not find the $Name summary in licensed-corpus output."
    }
    return $match
}

function Number
{
    param(
        [Parameter(Mandatory = $true)]
        [System.Text.RegularExpressions.Match]$Match,

        [Parameter(Mandatory = $true)]
        [string]$Group
    )

    return [int64]::Parse($Match.Groups[$Group].Value, [Globalization.CultureInfo]::InvariantCulture)
}

$keyBif = Match-Summary `
    "KEY/BIF corpus keys=(?<keys>\d+) declared-bifs=(?<declaredBifs>\d+) unique-bifs=(?<uniqueBifs>\d+) resources=(?<available>\d+) requested=(?<requested>\d+) executed=(?<executed>\d+) failed=(?<failed>\d+) skipped=(?<skipped>\d+) input-sha256=(?<inputHash>[0-9a-f]+) semantic-sha256=(?<semanticHash>[0-9a-f]+)" `
    "KEY/BIF"
$twoDa = Match-Summary `
    "2DA corpus requested=(?<requested>\d+) executed=(?<executed>\d+) failed=(?<failed>\d+) skipped=(?<skipped>\d+) expected-invalid=(?<expectedInvalid>\d+) input-sha256=(?<inputHash>[0-9a-f]+) semantic-sha256=(?<semanticHash>[0-9a-f]+)" `
    "2DA"
$tlk = Match-Summary `
    "TLK corpus requested=(?<requested>\d+) executed=(?<executed>\d+) failed=(?<failed>\d+) skipped=(?<skipped>\d+) input-sha256=(?<inputHash>[0-9a-f]+) semantic-sha256=(?<semanticHash>[0-9a-f]+)" `
    "TLK"
$gff = Match-Summary `
    "GFF/ITP corpus requested=(?<requested>\d+) executed=(?<executed>\d+) failed=(?<failed>\d+) skipped=(?<skipped>\d+) input-sha256=(?<inputHash>[0-9a-f]+) semantic-sha256=(?<semanticHash>[0-9a-f]+)" `
    "GFF/ITP"
$tga = Match-Summary `
    "TGA corpus available-loose=(?<availableLoose>\d+) available-archive=(?<availableArchive>\d+) requested=(?<requested>\d+) executed=(?<executed>\d+) failed=(?<failed>\d+) skipped=(?<skipped>\d+) expected-invalid=(?<expectedInvalid>\d+) input-sha256=(?<inputHash>[0-9a-f]+) semantic-sha256=(?<semanticHash>[0-9a-f]+)" `
    "TGA"
$plt = Match-Summary `
    "PLT corpus available-loose=(?<availableLoose>\d+) available-archive=(?<availableArchive>\d+) requested=(?<requested>\d+) executed=(?<executed>\d+) failed=(?<failed>\d+) skipped=(?<skipped>\d+) expected-invalid=(?<expectedInvalid>\d+) input-sha256=(?<inputHash>[0-9a-f]+) semantic-sha256=(?<semanticHash>[0-9a-f]+)" `
    "PLT"
$binaryMdl = Match-Summary `
    "Binary MDL corpus available=(?<allAvailable>\d+) binary=(?<available>\d+) requested=(?<requested>\d+) executed=(?<executed>\d+) failed=(?<failed>\d+) skipped=(?<skipped>\d+) meshes=(?<meshes>\d+) skins=(?<skins>\d+) emitters=(?<emitters>\d+) animations=(?<animations>\d+) input-sha256=(?<inputHash>[0-9a-f]+) semantic-sha256=(?<semanticHash>[0-9a-f]+)" `
    "binary MDL"
$asciiMdl = Match-Summary `
    "ASCII MDL corpus requested=(?<requested>\d+) executed=(?<executed>\d+) failed=(?<failed>\d+) skipped=(?<skipped>\d+) meshes=(?<meshes>\d+) skins=(?<skins>\d+) emitters=(?<emitters>\d+) animations=(?<animations>\d+) semantic-sample=(?<semanticSample>\d+) input-sha256=(?<inputHash>[0-9a-f]+) semantic-sha256=(?<semanticHash>[0-9a-f]+)" `
    "ASCII MDL"
$moduleJson = Match-Summary `
    "Module JSON corpus requested=(?<requested>\d+) executed=(?<executed>\d+) failed=(?<failed>\d+) skipped=(?<skipped>\d+) bytes=(?<bytes>\d+) input-sha256=(?<inputHash>[0-9a-f]+)" `
    "Module JSON"
$mdlScope = Match-Summary `
    "MDL corpus total requested=(?<requested>\d+) executed=(?<executed>\d+) failed=(?<failed>\d+) skipped=(?<skipped>\d+) binary=(?<binary>\d+) ascii=(?<ascii>\d+)" `
    "MDL scope"

$tlkEntryCounts = @(
    [regex]::Matches($text, "TLK corpus file=[^\r\n]+ entries=(?<entries>\d+)") |
        ForEach-Object { Number $_ "entries" }
)

# The corpus tests read whatever revision is checked out in the SWLOR_Haks worktree, so the
# manifest must record that revision - not the HEAD gitlink, which lags during a staged
# submodule bump. Fail on divergence the checkout cannot explain (dirty worktree).
$haksPath = Join-Path $repositoryRoot "SWLOR_Haks"
$submoduleHead = "$(& git -C $haksPath rev-parse HEAD)".Trim()
if ($submoduleHead -notmatch "^[0-9a-f]{40}$")
{
    throw "Could not resolve the checked-out SWLOR_Haks revision."
}
$haksStatus = @(& git -C $haksPath status --porcelain)
if ($haksStatus.Count -gt 0)
{
    throw "SWLOR_Haks worktree is dirty; the baseline would not be attributable to revision $submoduleHead."
}
$gitlinkLine = & git -C $repositoryRoot ls-tree HEAD SWLOR_Haks
$gitlinkMatch = [regex]::Match("$gitlinkLine", "\b(?<sha>[0-9a-f]{40})\b")
if ($gitlinkMatch.Success -and $gitlinkMatch.Groups["sha"].Value -ne $submoduleHead)
{
    Write-Output "NOTE: SWLOR_Haks checkout $submoduleHead differs from the HEAD gitlink $($gitlinkMatch.Groups["sha"].Value) (staged submodule bump); recording the checkout."
}

$manifest = [ordered]@{
    schemaVersion = 2
    swlorHaksGitlink = $submoduleHead
    formats = [ordered]@{
        keyBif = [ordered]@{
            keys = Number $keyBif "keys"
            declaredBifs = Number $keyBif "declaredBifs"
            uniqueBifs = Number $keyBif "uniqueBifs"
            available = Number $keyBif "available"
            requested = Number $keyBif "requested"
            executed = Number $keyBif "executed"
            failed = Number $keyBif "failed"
            skipped = Number $keyBif "skipped"
            inputSha256 = $keyBif.Groups["inputHash"].Value
            semanticSha256 = $keyBif.Groups["semanticHash"].Value
        }
        twoDa = [ordered]@{
            available = Number $twoDa "requested"
            requested = Number $twoDa "requested"
            executed = Number $twoDa "executed"
            failed = Number $twoDa "failed"
            skipped = Number $twoDa "skipped"
            expectedInvalid = Number $twoDa "expectedInvalid"
            inputSha256 = $twoDa.Groups["inputHash"].Value
            semanticSha256 = $twoDa.Groups["semanticHash"].Value
        }
        tlk = [ordered]@{
            available = Number $tlk "requested"
            requested = Number $tlk "requested"
            executed = Number $tlk "executed"
            failed = Number $tlk "failed"
            skipped = Number $tlk "skipped"
            entryCounts = $tlkEntryCounts
            inputSha256 = $tlk.Groups["inputHash"].Value
            semanticSha256 = $tlk.Groups["semanticHash"].Value
        }
        gffItp = [ordered]@{
            available = Number $gff "requested"
            requested = Number $gff "requested"
            executed = Number $gff "executed"
            failed = Number $gff "failed"
            skipped = Number $gff "skipped"
            inputSha256 = $gff.Groups["inputHash"].Value
            semanticSha256 = $gff.Groups["semanticHash"].Value
        }
        tga = [ordered]@{
            availableLoose = Number $tga "availableLoose"
            availableArchive = Number $tga "availableArchive"
            requested = Number $tga "requested"
            executed = Number $tga "executed"
            failed = Number $tga "failed"
            skipped = Number $tga "skipped"
            expectedInvalid = Number $tga "expectedInvalid"
            inputSha256 = $tga.Groups["inputHash"].Value
            semanticSha256 = $tga.Groups["semanticHash"].Value
        }
        plt = [ordered]@{
            availableLoose = Number $plt "availableLoose"
            availableArchive = Number $plt "availableArchive"
            requested = Number $plt "requested"
            executed = Number $plt "executed"
            failed = Number $plt "failed"
            skipped = Number $plt "skipped"
            expectedInvalid = Number $plt "expectedInvalid"
            inputSha256 = $plt.Groups["inputHash"].Value
            semanticSha256 = $plt.Groups["semanticHash"].Value
        }
        binaryMdl = [ordered]@{
            available = Number $binaryMdl "available"
            requested = Number $binaryMdl "requested"
            executed = Number $binaryMdl "executed"
            failed = Number $binaryMdl "failed"
            skipped = Number $binaryMdl "skipped"
            meshes = Number $binaryMdl "meshes"
            skins = Number $binaryMdl "skins"
            emitters = Number $binaryMdl "emitters"
            animations = Number $binaryMdl "animations"
            inputSha256 = $binaryMdl.Groups["inputHash"].Value
            semanticSha256 = $binaryMdl.Groups["semanticHash"].Value
        }
        asciiMdl = [ordered]@{
            available = Number $asciiMdl "requested"
            requested = Number $asciiMdl "requested"
            executed = Number $asciiMdl "executed"
            failed = Number $asciiMdl "failed"
            skipped = Number $asciiMdl "skipped"
            meshes = Number $asciiMdl "meshes"
            skins = Number $asciiMdl "skins"
            emitters = Number $asciiMdl "emitters"
            animations = Number $asciiMdl "animations"
            semanticSample = Number $asciiMdl "semanticSample"
            inputSha256 = $asciiMdl.Groups["inputHash"].Value
            semanticSha256 = $asciiMdl.Groups["semanticHash"].Value
        }
        moduleJson = [ordered]@{
            available = Number $moduleJson "requested"
            requested = Number $moduleJson "requested"
            executed = Number $moduleJson "executed"
            failed = Number $moduleJson "failed"
            skipped = Number $moduleJson "skipped"
            bytes = Number $moduleJson "bytes"
            inputSha256 = $moduleJson.Groups["inputHash"].Value
        }
        mdlScope = [ordered]@{
            available = Number $mdlScope "requested"
            requested = Number $mdlScope "requested"
            executed = Number $mdlScope "executed"
            failed = Number $mdlScope "failed"
            skipped = Number $mdlScope "skipped"
            binary = Number $mdlScope "binary"
            ascii = Number $mdlScope "ascii"
        }
    }
}

$json = (($manifest | ConvertTo-Json -Depth 8) -replace "`r`n", "`n") + "`n"
if ($Capture)
{
    $parent = Split-Path -Parent $baselinePath
    if (-not (Test-Path -LiteralPath $parent))
    {
        New-Item -ItemType Directory -Path $parent | Out-Null
    }
    [IO.File]::WriteAllText($baselinePath, $json, [Text.UTF8Encoding]::new($false))
    Write-Output "Captured deterministic licensed-corpus baseline: $baselinePath"
    exit 0
}

if (-not (Test-Path -LiteralPath $baselinePath))
{
    throw "Licensed-corpus baseline is missing: $baselinePath"
}
$expected = Get-Content -Raw -LiteralPath $baselinePath | ConvertFrom-Json
$expectedJson = (($expected | ConvertTo-Json -Depth 8) -replace "`r`n", "`n") + "`n"
if ($expectedJson -cne $json)
{
    throw "Licensed-corpus baseline differs. Review the evidence, then use -Capture intentionally."
}

Write-Output "Licensed-corpus baseline verified byte-deterministically after canonicalization."
