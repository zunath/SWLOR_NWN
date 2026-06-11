param(
    [string]$OutputDirectory = "SWLOR_Haks\swlor2_tga"
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command magick -ErrorAction SilentlyContinue)) {
    throw "ImageMagick 'magick' command was not found."
}

$OutputDirectory = (Resolve-Path $OutputDirectory).Path
$PlusMinus = [char]0x00B1

function Invoke-Magick {
    param([string[]]$Arguments)

    & magick @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "ImageMagick failed with exit code $LASTEXITCODE."
    }
}

function New-PazaakCard {
    param(
        [string]$Resref,
        [string]$Text,
        [string]$PanelColor,
        [switch]$Back
    )

    $out = Join-Path $OutputDirectory "$Resref.tga"

    $args = @(
        "-size", "128x192", "canvas:#8f9073",
        "-fill", "#a7a88b", "-draw", "roundrectangle 4,4 124,188 4,4",
        "-fill", "#76775f", "-draw", "rectangle 12,12 116,180",
        "-fill", "#9a9b7d", "-draw", "rectangle 15,15 113,177",
        "-fill", "#3b3d34", "-draw", "rectangle 19,24 109,59 rectangle 19,88 109,124 rectangle 19,135 109,173",
        "-fill", $PanelColor, "-draw", "polygon 23,28 105,28 105,52 87,52 73,39 61,52 23,52",
        "-fill", $PanelColor, "-draw", "polygon 23,94 61,94 73,107 87,94 105,94 105,118 23,118",
        "-fill", $PanelColor, "-draw", "rectangle 23,139 105,169",
        "-fill", "#a7a88b", "-stroke", "#555747", "-strokewidth", "2",
        "-draw", "polygon 64,46 91,73 64,101 37,73",
        "-fill", "#20252b", "-stroke", "#444844", "-strokewidth", "2",
        "-draw", "polygon 24,64 104,64 100,91 28,91"
    )

    if (-not $Back) {
        $args += @(
            "-font", "Arial-Black", "-gravity", "Center",
            "-pointsize", "28", "-stroke", "#101318", "-strokewidth", "4", "-fill", "#101318",
            "-annotate", "+2-20", $Text,
            "-pointsize", "28", "-stroke", "#101318", "-strokewidth", "1", "-fill", "#ffffff",
            "-annotate", "+0-21", $Text
        )
    }

    $args += @(
        "-fill", "none", "-stroke", "#555747", "-strokewidth", "2",
        "-draw", "roundrectangle 4,4 124,188 4,4 rectangle 15,15 113,177 rectangle 19,24 109,59 rectangle 19,88 109,124 rectangle 19,135 109,173",
        $out
    )

    Invoke-Magick $args
}

function New-PazaakBack {
    param([string]$Resref)

    New-PazaakCard -Resref $Resref -Text "" -PanelColor "#2fd39b" -Back
}

for ($value = 1; $value -le 10; $value++) {
    New-PazaakCard -Resref "pz_main$value" -Text "$value" -PanelColor "#2fd39b"
}

for ($value = 1; $value -le 6; $value++) {
    New-PazaakCard -Resref "pz_p$value" -Text "+$value" -PanelColor "#5b53de"
    New-PazaakCard -Resref "pz_m$value" -Text "-$value" -PanelColor "#ed514b"
    New-PazaakCard -Resref "pz_pm$value" -Text "$PlusMinus$value" -PanelColor "#d8ef2f"
}

New-PazaakCard -Resref "pz_g12" -Text ("1{0}2" -f $PlusMinus) -PanelColor "#d8ef2f"
New-PazaakCard -Resref "pz_gdbl" -Text "x2" -PanelColor "#5b53de"
New-PazaakCard -Resref "pz_gtie" -Text "TIE" -PanelColor "#f1c23d"
New-PazaakCard -Resref "pz_g24" -Text "2/4" -PanelColor "#f1c23d"
New-PazaakCard -Resref "pz_g36" -Text "3/6" -PanelColor "#f1c23d"
New-PazaakBack -Resref "pz_back"
New-PazaakBack -Resref "pazaak_card"
