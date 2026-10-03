# Lossless authoring sources are separate from the DDS-only deployed HAK.
if (!("GameplayIconTexture" -as [type])) {
    $decoder = Join-Path $PSScriptRoot "GameplayIconTexture.cs"
    $digest = (Get-FileHash -LiteralPath $decoder).Hash
    $assembly = Join-Path ([IO.Path]::GetTempPath()) "swlor-icon-decoder-$digest.dll"
    $mutex = [Threading.Mutex]::new($false, "Local\SWLORIconDecoder-$digest")
    $locked = $false
    try {
        try { $locked = $mutex.WaitOne(30000) }
        catch [Threading.AbandonedMutexException] { $locked = $true }
        if (!$locked) { throw "Timed out compiling the gameplay icon decoder." }
        if (!(Test-Path -LiteralPath $assembly)) {
            $stagedAssembly = Join-Path ([IO.Path]::GetTempPath()) ([Guid]::NewGuid().ToString() + ".dll")
            try {
                Add-Type -Path $decoder -OutputAssembly $stagedAssembly
                Move-Item -LiteralPath $stagedAssembly -Destination $assembly
            }
            finally { if (Test-Path -LiteralPath $stagedAssembly) { Remove-Item -LiteralPath $stagedAssembly } }
        }
    }
    finally {
        if ($locked) { $mutex.ReleaseMutex() }
        $mutex.Dispose()
    }
    Add-Type -Path $assembly
}

function Get-GameplayIconSourceDirectory([string]$RuntimeDirectory) {
    $full = [IO.Path]::GetFullPath($RuntimeDirectory)
    return Join-Path ([IO.Path]::GetDirectoryName($full)) ([IO.Path]::GetFileName($full) + "_source\production")
}

function Publish-GameplayIconDds([string]$RuntimeDirectory, [string]$MagickPath = "magick") {
    $source = Get-GameplayIconSourceDirectory $RuntimeDirectory
    $manifest = Join-Path (Split-Path -Parent $source) "dds-conversions.csv"
    $python = if ($env:SWLOR_PYTHON) { $env:SWLOR_PYTHON } else { "python" }
    & $python (Join-Path $PSScriptRoot "ConvertGameplayIconsToDds.py") `
        --source $source --output $RuntimeDirectory --manifest $manifest --magick $MagickPath
    if ($LASTEXITCODE -ne 0) { throw "Gameplay icon DDS export failed." }
}

function Test-GameplayIconDdsExports([string]$RuntimeDirectory) {
    $source = Get-GameplayIconSourceDirectory $RuntimeDirectory
    $manifest = Join-Path (Split-Path -Parent $source) "dds-conversions.csv"
    if (!(Test-Path -LiteralPath $manifest)) { throw "Missing DDS export manifest: $manifest" }
    if (@(Get-ChildItem -LiteralPath $RuntimeDirectory -Filter "*.tga").Count -gt 0) {
        throw "Runtime TGA icons shadow DDS textures. Keep lossless artwork in $source."
    }
    $rows = @(Import-Csv -LiteralPath $manifest)
    $names = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($row in $rows) {
        $name = $row.IconResRef
        if ($name -notmatch '^[a-zA-Z0-9_&''-]{1,16}$' -or !$names.Add($name)) { throw "Invalid DDS manifest resource: $name" }
        $tga = Join-Path $source "$name.tga"
        $dds = Join-Path $RuntimeDirectory "$name.dds"
        $txi = Join-Path $RuntimeDirectory "$name.txi"
        if (!(Test-Path -LiteralPath $tga) -or !(Test-Path -LiteralPath $dds) -or !(Test-Path -LiteralPath $txi)) {
            throw "Missing source, DDS or TXI for $name."
        }
        if ((Get-FileHash -LiteralPath $tga).Hash -ne $row.SourceSHA256 -or
            (Get-FileHash -LiteralPath $dds).Hash -ne $row.DdsSHA256) { throw "Stale DDS export for $name; run Publish-GameplayIconDds." }
        if ([IO.File]::ReadAllText($txi) -ne "mipmap 0`n") { throw "Invalid DDS mipmap directive for $name." }
        [void][GameplayIconTexture]::ReadBottomLeftTga($dds)
    }
    foreach ($file in Get-ChildItem -LiteralPath $source -Filter '*.tga') {
        if (!$names.Contains($file.BaseName)) { throw "Unexported icon source: $($file.Name)" }
    }
    Write-Host "Validated $($rows.Count) DDS exports against retained lossless artwork."
}
