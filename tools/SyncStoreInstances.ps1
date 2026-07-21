param(
    [switch]$Check,
    [switch]$CreateMissingBlueprints,
    [switch]$Build,
    [string]$ModuleRoot = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

if ([string]::IsNullOrWhiteSpace($ModuleRoot)) {
    $ModuleRoot = Join-Path $repoRoot "Module"
}

if ($Check -and $CreateMissingBlueprints) {
    throw "Use only one mode: -Check, -CreateMissingBlueprints, or sync mode."
}

$mode = if ($Check) {
    "--checkStoreInstances"
} elseif ($CreateMissingBlueprints) {
    "--createMissingStoreBlueprints"
} else {
    "--syncStoreInstances"
}
$project = Join-Path $repoRoot "SWLOR.CLI\SWLOR.CLI.csproj"
$cliDll = Join-Path $repoRoot "SWLOR.CLI\bin\Debug\net10.0\SWLOR.CLI.dll"

if ($Build -or !(Test-Path $cliDll)) {
    dotnet restore $project
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    # Clean checkouts need the CLI's project references built, but not the server deployment step.
    dotnet build $project --no-restore -p:RunPostBuildEvent=Never
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

dotnet $cliDll $mode --storeModuleRoot $ModuleRoot
exit $LASTEXITCODE
