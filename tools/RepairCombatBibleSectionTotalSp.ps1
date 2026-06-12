param(
    [string]$BibleWorkbookPath = "design\bible\SWLOR Design Bible - Combat Upgrade.xlsx"
)

# Recomputes the cached values of section "Total" rows on every perk sheet.
# Section total cells hold SUM(range) formulas; price edits made by the apply
# scripts leave the cached <v> values stale, and downstream tooling such as
# RepairCombatBibleTotalSp.ps1 reads those cached values. This script parses
# each Total row's SUM range, sums the current price cells, and rewrites the
# cached value to match. Run it after any price change, before
# RepairCombatBibleTotalSp.ps1.

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"

function Resolve-RepoPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return Join-Path $repoRoot $Path
}

function Read-ZipEntryText {
    param(
        [System.IO.Compression.ZipArchive]$Zip,
        [string]$EntryPath
    )

    $entry = $Zip.GetEntry($EntryPath)
    if ($null -eq $entry) {
        throw "Workbook entry '$EntryPath' was not found."
    }

    $stream = $entry.Open()
    try {
        $reader = [System.IO.StreamReader]::new($stream)
        try {
            return $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}

function Write-ZipEntryXml {
    param(
        [System.IO.Compression.ZipArchive]$Zip,
        [string]$EntryPath,
        [xml]$Xml
    )

    $entry = $Zip.GetEntry($EntryPath)
    if ($null -ne $entry) {
        $entry.Delete()
    }

    $newEntry = $Zip.CreateEntry($EntryPath)
    $stream = $newEntry.Open()
    try {
        $settings = [System.Xml.XmlWriterSettings]::new()
        $settings.Encoding = [System.Text.UTF8Encoding]::new($false)
        $settings.Indent = $false
        $writer = [System.Xml.XmlWriter]::Create($stream, $settings)
        try {
            $Xml.Save($writer)
        }
        finally {
            $writer.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}

function Get-WorkbookEntryPath {
    param([string]$RelationshipTarget)

    $target = $RelationshipTarget.Replace("\", "/").TrimStart("/")
    if ($target.StartsWith("xl/", [System.StringComparison]::OrdinalIgnoreCase)) {
        return $target
    }

    return "xl/$target"
}

function Get-OpenXmlColumnIndex {
    param([string]$CellReference)

    $letters = ([regex]::Match($CellReference, "^[A-Z]+")).Value
    if ([string]::IsNullOrWhiteSpace($letters)) {
        return 0
    }

    $index = 0
    foreach ($character in $letters.ToCharArray()) {
        $index = ($index * 26) + ([int][char]$character - [int][char]"A" + 1)
    }

    return $index
}

function Get-OpenXmlRowNumber {
    param([string]$CellReference)

    return [int]([regex]::Match($CellReference, "\d+$")).Value
}

$workbookPath = Resolve-RepoPath $BibleWorkbookPath
if (!(Test-Path $workbookPath)) {
    throw "Workbook '$workbookPath' was not found."
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::Open($workbookPath, [System.IO.Compression.ZipArchiveMode]::Update)
$updates = @()

try {
    [xml]$workbookXml = Read-ZipEntryText -Zip $zip -EntryPath "xl/workbook.xml"
    [xml]$relationshipsXml = Read-ZipEntryText -Zip $zip -EntryPath "xl/_rels/workbook.xml.rels"

    $relationshipsById = @{}
    foreach ($relationship in $relationshipsXml.Relationships.Relationship) {
        $relationshipsById[$relationship.Id] = Get-WorkbookEntryPath $relationship.Target
    }

    $workbookNamespace = [System.Xml.XmlNamespaceManager]::new($workbookXml.NameTable)
    $workbookNamespace.AddNamespace("d", $namespace)
    $workbookNamespace.AddNamespace("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")

    foreach ($sheet in $workbookXml.SelectNodes("//d:sheets/d:sheet", $workbookNamespace)) {
        $sheetName = $sheet.GetAttribute("name")
        $relationshipId = $sheet.GetAttribute("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")
        $sheetPath = $relationshipsById[$relationshipId]
        if ([string]::IsNullOrWhiteSpace($sheetPath) -or $null -eq $zip.GetEntry($sheetPath)) {
            continue
        }

        [xml]$worksheetXml = Read-ZipEntryText -Zip $zip -EntryPath $sheetPath
        $worksheetNamespace = [System.Xml.XmlNamespaceManager]::new($worksheetXml.NameTable)
        $worksheetNamespace.AddNamespace("d", $namespace)

        $numberByCell = @{}
        foreach ($cell in $worksheetXml.SelectNodes("//d:sheetData/d:row/d:c", $worksheetNamespace)) {
            if ($cell.GetAttribute("t") -in @("s", "inlineStr", "str", "b", "e")) {
                continue
            }

            $valueNode = $cell.SelectSingleNode("d:v", $worksheetNamespace)
            if ($null -eq $valueNode -or [string]::IsNullOrWhiteSpace($valueNode.InnerText)) {
                continue
            }

            $parsed = 0.0
            if ([double]::TryParse($valueNode.InnerText, [System.Globalization.NumberStyles]::Float, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$parsed)) {
                $numberByCell[$cell.GetAttribute("r")] = $parsed
            }
        }

        $sheetChanged = $false
        foreach ($cell in $worksheetXml.SelectNodes("//d:sheetData/d:row/d:c[d:f]", $worksheetNamespace)) {
            $formulaNode = $cell.SelectSingleNode("d:f", $worksheetNamespace)
            $match = [regex]::Match($formulaNode.InnerText, "^SUM\(([A-Z]+)(\d+):([A-Z]+)(\d+)\)$")
            if (!$match.Success -or $match.Groups[1].Value -ne $match.Groups[3].Value) {
                continue
            }

            $column = $match.Groups[1].Value
            $startRow = [int]$match.Groups[2].Value
            $endRow = [int]$match.Groups[4].Value
            $sum = 0.0
            for ($row = $startRow; $row -le $endRow; $row++) {
                $reference = "$column$row"
                if ($numberByCell.ContainsKey($reference)) {
                    $sum += $numberByCell[$reference]
                }
            }

            $valueNode = $cell.SelectSingleNode("d:v", $worksheetNamespace)
            if ($null -eq $valueNode) {
                $valueNode = $worksheetXml.CreateElement("v", $namespace)
                [void]$cell.AppendChild($valueNode)
            }

            $newValue = $sum.ToString("0.############", [System.Globalization.CultureInfo]::InvariantCulture)
            if ($valueNode.InnerText -ne $newValue) {
                $updates += [pscustomobject]@{
                    Sheet = $sheetName
                    Cell = $cell.GetAttribute("r")
                    Formula = $formulaNode.InnerText
                    OldValue = $valueNode.InnerText
                    NewValue = $newValue
                }
                $valueNode.InnerText = $newValue
                $numberByCell[$cell.GetAttribute("r")] = $sum
                $sheetChanged = $true
            }
        }

        if ($sheetChanged) {
            Write-ZipEntryXml -Zip $zip -EntryPath $sheetPath -Xml $worksheetXml
        }
    }
}
finally {
    $zip.Dispose()
}

$updates | Format-Table Sheet, Cell, Formula, OldValue, NewValue -AutoSize
Write-Host "Recomputed cached section Total SP values in '$workbookPath'."
