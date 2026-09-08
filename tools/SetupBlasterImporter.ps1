param(
    [string]$Destination = (Join-Path $PSScriptRoot '../.tmp/blaster-tools')
)
$ErrorActionPreference = 'Stop'
$Destination = [IO.Path]::GetFullPath($Destination)
New-Item -ItemType Directory -Force -Path $Destination | Out-Null
$archive = Join-Path $Destination 'gr2-4.2.1.zip'
$url = 'https://github.com/SWTOR-Slicers/Granny2-Plug-In-Blender-2.8x/archive/refs/tags/4.2.1.zip'
if (-not (Test-Path -LiteralPath $archive)) {
    Invoke-WebRequest -Uri $url -OutFile $archive
}
$expected = '1E14FDE636E56354FEA82DA4107686D0A9DDC68CB1F51FE0A3918AD23B502C7A'
if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne $expected) {
    throw "Importer archive checksum differs from the tested release. Review it before updating the pinned hash: $archive"
}
Expand-Archive -LiteralPath $archive -DestinationPath $Destination -Force
Write-Output (Join-Path $Destination 'Granny2-Plug-In-Blender-2.8x-4.2.1')
