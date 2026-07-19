[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SheetName,
    [string]$WorkbookPath = "design\bible\SWLOR Design Bible - Combat Upgrade.xlsx",
    [int]$FirstRow = 1,
    [int]$LastRow = [int]::MaxValue,
    [switch]$IncludeFormulas
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$workbookFullPath = if ([IO.Path]::IsPathRooted($WorkbookPath)) {
    $WorkbookPath
}
else {
    Join-Path $repoRoot $WorkbookPath
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Read-ZipEntryText {
    param(
        [IO.Compression.ZipArchive]$Zip,
        [string]$EntryPath
    )

    $entry = $Zip.GetEntry($EntryPath)
    if ($null -eq $entry) {
        throw "Workbook entry '$EntryPath' was not found."
    }

    $reader = [IO.StreamReader]::new($entry.Open())
    try {
        return $reader.ReadToEnd()
    }
    finally {
        $reader.Dispose()
    }
}

function Get-WorkbookEntryPath {
    param([string]$Target)

    $normalized = $Target.Replace("\", "/")
    if ($normalized.StartsWith("/")) {
        return $normalized.TrimStart("/")
    }
    if ($normalized.StartsWith("xl/")) {
        return $normalized
    }
    return "xl/$normalized"
}

function Get-ColumnName {
    param([string]$Address)

    return ([regex]::Match($Address, "^[A-Z]+")).Value
}

function Get-CellText {
    param(
        [Xml.XmlElement]$Cell,
        [Collections.Generic.List[string]]$SharedStrings,
        [Xml.XmlNamespaceManager]$Namespace
    )

    $type = $Cell.GetAttribute("t")
    if ($type -eq "inlineStr") {
        $parts = foreach ($textNode in $Cell.SelectNodes(".//d:t", $Namespace)) {
            $textNode.InnerText
        }
        return $parts -join ""
    }

    $valueNode = $Cell.SelectSingleNode("d:v", $Namespace)
    if ($null -eq $valueNode) {
        return ""
    }

    $value = $valueNode.InnerText
    if ($type -eq "s") {
        return $SharedStrings[[int]$value]
    }

    return $value
}

$zip = [IO.Compression.ZipFile]::OpenRead($workbookFullPath)
try {
    $sharedStrings = [Collections.Generic.List[string]]::new()
    if ($null -ne $zip.GetEntry("xl/sharedStrings.xml")) {
        [xml]$sharedStringsXml = Read-ZipEntryText -Zip $zip -EntryPath "xl/sharedStrings.xml"
        $sharedNamespace = [Xml.XmlNamespaceManager]::new($sharedStringsXml.NameTable)
        $sharedNamespace.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")
        foreach ($item in $sharedStringsXml.SelectNodes("//d:si", $sharedNamespace)) {
            $parts = foreach ($textNode in $item.SelectNodes(".//d:t", $sharedNamespace)) {
                $textNode.InnerText
            }
            $sharedStrings.Add(($parts -join ""))
        }
    }

    [xml]$workbookXml = Read-ZipEntryText -Zip $zip -EntryPath "xl/workbook.xml"
    [xml]$relationshipsXml = Read-ZipEntryText -Zip $zip -EntryPath "xl/_rels/workbook.xml.rels"

    $relationships = @{}
    foreach ($relationship in $relationshipsXml.Relationships.Relationship) {
        $relationships[$relationship.Id] = Get-WorkbookEntryPath $relationship.Target
    }

    $workbookNamespace = [Xml.XmlNamespaceManager]::new($workbookXml.NameTable)
    $workbookNamespace.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")
    $workbookNamespace.AddNamespace("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")

    $sheet = $workbookXml.SelectNodes("//d:sheets/d:sheet", $workbookNamespace) |
        Where-Object { $_.GetAttribute("name") -eq $SheetName } |
        Select-Object -First 1
    if ($null -eq $sheet) {
        throw "Workbook sheet '$SheetName' was not found."
    }

    $relationshipId = $sheet.GetAttribute("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")
    [xml]$worksheetXml = Read-ZipEntryText -Zip $zip -EntryPath $relationships[$relationshipId]
    $worksheetNamespace = [Xml.XmlNamespaceManager]::new($worksheetXml.NameTable)
    $worksheetNamespace.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")

    foreach ($row in $worksheetXml.SelectNodes("//d:sheetData/d:row", $worksheetNamespace)) {
        $rowNumber = [int]$row.GetAttribute("r")
        if ($rowNumber -lt $FirstRow -or $rowNumber -gt $LastRow) {
            continue
        }

        $values = [Collections.Generic.List[string]]::new()
        foreach ($cell in $row.SelectNodes("d:c", $worksheetNamespace)) {
            $text = Get-CellText -Cell $cell -SharedStrings $sharedStrings -Namespace $worksheetNamespace
            $formulaNode = $cell.SelectSingleNode("d:f", $worksheetNamespace)
            if ($IncludeFormulas -and $null -ne $formulaNode) {
                $text = "$text [=$($formulaNode.InnerText)]"
            }
            if ([string]::IsNullOrWhiteSpace($text)) {
                continue
            }

            $text = $text -replace "\r?\n", " ⏎ "
            $values.Add("$(Get-ColumnName $cell.GetAttribute('r'))=$text")
        }

        if ($values.Count -gt 0) {
            [pscustomobject]@{
                Row = $rowNumber
                Values = $values -join " | "
            }
        }
    }
}
finally {
    $zip.Dispose()
}
