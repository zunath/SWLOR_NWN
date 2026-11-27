# Builds the status effect icons of the astromech names (ief_am_*) from their source subjects.
#
# An astromech ability applies the status effect class of the humanoid ability it copies and shows it under its own
# name and artwork (the `statuses` list of tools/AstromechSkillMirrors.json). Those icons belong to no status effect
# class, so tools/UpdateGameplayIconStandards.ps1 does not generate them. This script draws them with that tool's own
# New-StatusIcon (backdrop, semantic frame and rank badge), so they match every other status icon.
#
#   powershell -ExecutionPolicy Bypass -File tools/StampAstromechStatusIcons.ps1
#
# Sources: SWLOR_Haks/sw_ability_source/astromech/<line>/status_png/<icon>.png (256x256).
# Output:  SWLOR_Haks/sw_ability/<icon>.tga (32x32).
param(
    [string]$ConfigPath = "tools\AstromechSkillMirrors.json",
    [string]$SourceRoot = "SWLOR_Haks\sw_ability_source\astromech",
    [string]$IconPath = "SWLOR_Haks\sw_ability"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$repoRoot = Split-Path -Parent $PSScriptRoot
function Resolve-FromRepo([string]$Path) {
    if ([IO.Path]::IsPathRooted($Path)) { return $Path }
    return Join-Path $repoRoot $Path
}

# Load the constants and functions of the icon tool without running it.
$toolPath = Join-Path $PSScriptRoot "UpdateGameplayIconStandards.ps1"
$ast = [System.Management.Automation.Language.Parser]::ParseFile($toolPath, [ref]$null, [ref]$null)
foreach ($statement in $ast.EndBlock.Statements) {
    if ($statement -is [System.Management.Automation.Language.AssignmentStatementAst]) {
        # Assignments that read the tool's parameters belong to its run and are skipped.
        try { . ([scriptblock]::Create($statement.Extent.Text)) } catch { }
    }
    elseif ($statement -is [System.Management.Automation.Language.FunctionDefinitionAst]) {
        . ([scriptblock]::Create($statement.Extent.Text))
    }
}

$statuses = @((Get-Content -Raw -Path (Resolve-FromRepo $ConfigPath) | ConvertFrom-Json).statuses)
if ($statuses.Count -eq 0) {
    throw "No statuses in $ConfigPath."
}

$staging = Join-Path ([IO.Path]::GetTempPath()) ("astromech-status-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $staging | Out-Null
try {
    foreach ($status in $statuses) {
        $source = @(Get-ChildItem -Path (Resolve-FromRepo $SourceRoot) -Recurse -Filter "$($status.icon).png" |
            Where-Object { $_.Directory.Name -eq "status_png" })
        if ($source.Count -ne 1) {
            throw "Expected one status_png source for $($status.icon), found $($source.Count)."
        }
        Copy-Item -LiteralPath $source[0].FullName -Destination (Join-Path $staging "$($status.icon).png")
    }

    $IconSize = 32
    $StatusIconSourcePath = $staging
    $script:RankBadgeByResRef = @{}
    foreach ($status in $statuses) {
        if (![string]::IsNullOrWhiteSpace($status.rank)) {
            $script:RankBadgeByResRef[$status.icon.ToLowerInvariant()] = $status.rank
        }
    }

    foreach ($status in $statuses) {
        $entry = [pscustomobject]@{
            Type = "StatusEffect"
            Key = $status.label
            DisplayName = $status.name
            SemanticCategory = $status.category
            Rank = $status.rank
            IconResRef = $status.icon
        }
        New-StatusIcon $entry (Join-Path (Resolve-FromRepo $IconPath) "$($status.icon).tga")
    }
}
finally {
    Remove-Item -LiteralPath $staging -Recurse -Force
}

Write-Host "Stamped $($statuses.Count) astromech status icons."
