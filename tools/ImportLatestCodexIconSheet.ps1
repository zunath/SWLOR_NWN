param(
    [Parameter(Mandatory = $true)]
    [int]$BatchNumber,

    [string]$GeneratedImagesRoot = "C:\Users\Zunath\.codex\generated_images",
    [string]$OutputPath = "output\imagegen\gpt2_icon_production",
    [string]$TargetsPath = "output\imagegen\gpt2_icon_production\icon_targets.csv",
    [string]$IconOutputPath = "SWLOR_Haks\swlor2_tga",
    [string]$ImporterPath = "tools\ImportCodexIconContactSheet.ps1",
    [int]$BatchSize = 25,
    [int]$Columns = 5,
    [int]$Rows = 5
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($BatchNumber -lt 1) {
    throw "BatchNumber must be 1 or greater."
}

$latest = Get-ChildItem -LiteralPath $GeneratedImagesRoot -Recurse -Filter "*.png" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (!$latest) {
    throw "No generated PNG files found under $GeneratedImagesRoot."
}

$sheetDirectory = Join-Path $OutputPath "sheets"
New-Item -ItemType Directory -Force -Path $sheetDirectory | Out-Null

$sheetPath = Join-Path $sheetDirectory ("batch_{0:D4}.png" -f $BatchNumber)
Copy-Item -LiteralPath $latest.FullName -Destination $sheetPath -Force

& powershell -ExecutionPolicy Bypass -File $ImporterPath `
    -SheetPath $sheetPath `
    -TargetsPath $TargetsPath `
    -BatchNumber $BatchNumber `
    -IconOutputPath $IconOutputPath `
    -WorkPath (Join-Path $OutputPath "cropped") `
    -BatchSize $BatchSize `
    -Columns $Columns `
    -Rows $Rows `
    -AnatomyReviewed

Write-Host "Copied $($latest.FullName) to $sheetPath and imported batch $BatchNumber."
