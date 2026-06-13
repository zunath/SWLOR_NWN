param(
    [string]$Feat2daPath = "SWLOR_Haks\swlor2_2da\feat.2da"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ColumnWidths = @(
    7, 49, 11, 14, 19, 17, 9, 9, 9, 9, 9, 9, 13, 14, 14, 15, 15, 19,
    11, 8, 10, 12, 10, 13, 13, 13, 13, 13, 13, 13, 11, 18, 12, 20, 49,
    18, 14, 11, 16, 11, 13, 13, 12
)

function Format-FeatRow([string[]]$Tokens) {
    $parts = for ($i = 0; $i -lt $Tokens.Count; $i++) {
        $width = if ($i -lt $ColumnWidths.Count) { $ColumnWidths[$i] } else { 8 }
        $Tokens[$i].PadRight($width)
    }

    return ($parts -join "").TrimEnd()
}

function New-BlankFeatRow([int]$Row, [int]$ExpectedTokens) {
    $tokens = [System.Collections.Generic.List[string]]::new()
    $tokens.Add($Row.ToString())
    for ($i = 1; $i -lt $ExpectedTokens; $i++) {
        $tokens.Add("****")
    }

    return Format-FeatRow $tokens.ToArray()
}

$featPath = Resolve-Path $Feat2daPath
$lines = [System.Collections.Generic.List[string]]::new()
$lines.AddRange([System.IO.File]::ReadAllLines($featPath))
$headerIndex = -1
for ($i = 0; $i -lt $lines.Count; $i++) {
    $tokens = @($lines[$i].Trim() -split "\s+")
    if ($tokens.Count -gt 1 -and $tokens[0] -eq "LABEL" -and $tokens[1] -eq "FEAT") {
        $headerIndex = $i
        break
    }
}

if ($headerIndex -lt 0) {
    throw "Could not find feat.2da header row."
}

$dataStartIndex = $headerIndex + 1
$expectedTokens = (($lines[$headerIndex].Trim() -split "\s+").Count + 1)
$formatted = 0

$existingRows = @{}
for ($i = $dataStartIndex; $i -lt $lines.Count; $i++) {
    $tokens = @($lines[$i].Trim() -split "\s+")
    $row = 0
    if ($tokens.Count -gt 0 -and [int]::TryParse($tokens[0], [ref]$row)) {
        $existingRows[$row] = $true
    }
}

if (-not $existingRows.ContainsKey(1997)) {
    $insertIndex = 0
    for ($i = $dataStartIndex; $i -lt $lines.Count; $i++) {
        $tokens = @($lines[$i].Trim() -split "\s+")
        $row = 0
        if ($tokens.Count -gt 0 -and [int]::TryParse($tokens[0], [ref]$row) -and $row -eq 2000) {
            $insertIndex = $i
            break
        }
    }

    if ($insertIndex -eq 0) {
        throw "Could not find row 2000 for inserting placeholder feat rows."
    }

    $lines.Insert($insertIndex, (New-BlankFeatRow 1999 $expectedTokens))
    $lines.Insert($insertIndex, (New-BlankFeatRow 1998 $expectedTokens))
    $lines.Insert($insertIndex, (New-BlankFeatRow 1997 $expectedTokens))
    $formatted += 3
}

for ($i = $dataStartIndex; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    if ($line.Trim().Length -eq 0) {
        continue
    }

    $tokens = @($line.Trim() -split "\s+")
    $row = 0
    if (-not [int]::TryParse($tokens[0], [ref]$row)) {
        continue
    }

    $isPlaceholderOnly = (@($tokens | Select-Object -Skip 1 | Where-Object { $_ -ne "****" }).Count -eq 0)
    if ($isPlaceholderOnly -and $tokens.Count -ne $expectedTokens) {
        if ($tokens.Count -gt $expectedTokens) {
            $tokens = @($tokens | Select-Object -First $expectedTokens)
        }
        else {
            while ($tokens.Count -lt $expectedTokens) {
                $tokens += "****"
            }
        }

        $lines[$i] = Format-FeatRow $tokens
        $formatted++
        continue
    }

    if ($row -eq 1995 -and $tokens.Count -eq ($expectedTokens + 1)) {
        $tokens = @($tokens | Select-Object -First $expectedTokens)
        $lines[$i] = Format-FeatRow $tokens
        $formatted++
        continue
    }

    if ($row -ge 2000 -and $row -le 2578) {
        if ($tokens.Count -ne $expectedTokens) {
            throw "Row $row has $($tokens.Count) tokens, expected $expectedTokens."
        }

        $lines[$i] = Format-FeatRow $tokens
        $formatted++
    }
}

[System.IO.File]::WriteAllLines($featPath, $lines)
Write-Host "Formatted $formatted feat.2da rows."
