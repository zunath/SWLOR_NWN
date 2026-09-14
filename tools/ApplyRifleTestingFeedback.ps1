[CmdletBinding()]
param(
    [string]$WorkbookPath = "design\bible\SWLOR Design Bible - Combat Upgrade.xlsx"
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

$descriptionUpdates = [ordered]@{
    "Containment Net" = "Targets with 3 or more Suppression stacks have -10% Damage Dealt while those stacks remain."
    "Kill Box" = "Target an enemy or location to deal weapon DMG + 20 to enemies within 8m and apply Kill Box for 45 seconds. While Kill Box remains, any player's ranged attacks against affected enemies add Suppression stacks lasting 30 seconds using the Kill Box caster's Suppressing Shot stack strength; each stack reduces Evasion by an additional 3%."
    "Scope Calibration" = "Ranged abilities gain +10% Accuracy and +8% Critical Rate against targets at least 10m away."
    "Headshot I" = "Queues your next auto-attack to deal weapon DMG + 16. If Headshot is used after 3 seconds without attacking, that attack gains +15% Critical Rate."
    "Dead Center" = "After 3 seconds without attacking, if your next attack is a critical hit, it deals +15% damage."
    "Headshot II" = "Queues your next auto-attack to deal weapon DMG + 30. If Headshot is used after 3 seconds without attacking, that attack gains +25% Critical Rate."
    "Breach Round" = "Hostile ranged attack abilities reduce the target's Defense by 10% for 30 seconds."
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.Xml.Linq

function Read-ZipEntryText {
    param([IO.Compression.ZipArchive]$Zip, [string]$EntryPath)

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

function Get-CellText {
    param(
        [System.Xml.Linq.XElement]$Cell,
        [Collections.Generic.List[string]]$SharedStrings,
        [System.Xml.Linq.XNamespace]$Namespace
    )

    $type = if ($null -ne $Cell.Attribute("t")) { $Cell.Attribute("t").Value } else { "" }
    if ($type -eq "inlineStr") {
        return (($Cell.Descendants($Namespace + "t") | ForEach-Object Value) -join "")
    }

    $value = $Cell.Element($Namespace + "v")
    if ($null -eq $value) {
        return ""
    }
    if ($type -eq "s") {
        return $SharedStrings[[int]$value.Value]
    }
    return $value.Value
}

function Set-InlineCellText {
    param(
        [System.Xml.Linq.XElement]$Cell,
        [string]$Value,
        [System.Xml.Linq.XNamespace]$Namespace
    )

    $Cell.SetAttributeValue("t", "inlineStr")
    $Cell.RemoveNodes()
    $text = [System.Xml.Linq.XElement]::new($Namespace + "t", $Value)
    if ($Value.Length -ne $Value.Trim().Length) {
        $text.SetAttributeValue([System.Xml.Linq.XNamespace]::Xml + "space", "preserve")
    }
    $inlineString = [System.Xml.Linq.XElement]::new($Namespace + "is")
    $inlineString.Add($text)
    $Cell.Add($inlineString)
}

function Write-WorksheetEntry {
    param(
        [IO.Compression.ZipArchive]$Zip,
        [string]$EntryPath,
        [System.Xml.Linq.XDocument]$Worksheet
    )

    $existingEntry = $Zip.GetEntry($EntryPath)
    $existingEntry.Delete()
    $replacement = $Zip.CreateEntry($EntryPath, [IO.Compression.CompressionLevel]::Optimal)
    $stream = $replacement.Open()
    try {
        $writer = [IO.StreamWriter]::new($stream, [Text.UTF8Encoding]::new($false))
        try {
            $Worksheet.Save($writer, [System.Xml.Linq.SaveOptions]::DisableFormatting)
        }
        finally {
            $writer.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}

function Assert-RifleDescriptionUpdates {
    param(
        [string]$Path,
        [Collections.IDictionary]$Updates
    )

    $validationZip = [IO.Compression.ZipFile]::OpenRead($Path)
    try {
        [xml]$workbookXml = Read-ZipEntryText $validationZip "xl/workbook.xml"
        [xml]$relationshipsXml = Read-ZipEntryText $validationZip "xl/_rels/workbook.xml.rels"
        $relationshipPaths = @{}
        foreach ($relationship in $relationshipsXml.Relationships.Relationship) {
            $relationshipPaths[$relationship.Id] = Get-WorkbookEntryPath $relationship.Target
        }

        $manager = [Xml.XmlNamespaceManager]::new($workbookXml.NameTable)
        $manager.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")
        $manager.AddNamespace("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")
        $rifleSheet = $workbookXml.SelectNodes("//d:sheets/d:sheet", $manager) |
            Where-Object { $_.GetAttribute("name") -eq "Rifle" } |
            Select-Object -First 1
        if ($null -eq $rifleSheet) {
            throw "Replacement workbook does not contain the Rifle sheet."
        }

        $relationshipId = $rifleSheet.GetAttribute(
            "id",
            "http://schemas.openxmlformats.org/officeDocument/2006/relationships")
        $rifleEntryPath = $relationshipPaths[$relationshipId]
        $worksheet = [System.Xml.Linq.XDocument]::Parse(
            (Read-ZipEntryText $validationZip $rifleEntryPath),
            [System.Xml.Linq.LoadOptions]::PreserveWhitespace)
        $namespace = [System.Xml.Linq.XNamespace]"http://schemas.openxmlformats.org/spreadsheetml/2006/main"
        $worksheetStrings = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($text in $worksheet.Descendants($namespace + "t")) {
            $worksheetStrings.Add($text.Value) | Out-Null
        }

        foreach ($entry in $Updates.GetEnumerator()) {
            if (!$worksheetStrings.Contains([string]$entry.Value)) {
                throw "Replacement workbook did not persist the Rifle description for '$($entry.Key)'."
            }
        }
    }
    finally {
        $validationZip.Dispose()
    }
}

if (!(Test-Path -LiteralPath $workbookFullPath)) {
    throw "Workbook '$workbookFullPath' was not found."
}

$workbookDirectory = [IO.Path]::GetDirectoryName($workbookFullPath)
$workbookName = [IO.Path]::GetFileName($workbookFullPath)
$tempWorkbookPath = Join-Path $workbookDirectory (".{0}.{1}.tmp.xlsx" -f $workbookName, [guid]::NewGuid())
$backupWorkbookPath = Join-Path $workbookDirectory (".{0}.{1}.backup.xlsx" -f $workbookName, [guid]::NewGuid())
$replacementValidated = $false
[IO.File]::Copy($workbookFullPath, $tempWorkbookPath, $false)
try {
    $zip = [IO.Compression.ZipFile]::Open($tempWorkbookPath, [IO.Compression.ZipArchiveMode]::Update)
    try {
        [xml]$workbookXml = Read-ZipEntryText $zip "xl/workbook.xml"
        [xml]$relationshipsXml = Read-ZipEntryText $zip "xl/_rels/workbook.xml.rels"

        $relationshipPaths = @{}
        foreach ($relationship in $relationshipsXml.Relationships.Relationship) {
            $relationshipPaths[$relationship.Id] = Get-WorkbookEntryPath $relationship.Target
        }

        $manager = [Xml.XmlNamespaceManager]::new($workbookXml.NameTable)
        $manager.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")
        $manager.AddNamespace("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")
        $rifleSheet = $workbookXml.SelectNodes("//d:sheets/d:sheet", $manager) |
            Where-Object { $_.GetAttribute("name") -eq "Rifle" } |
            Select-Object -First 1
        if ($null -eq $rifleSheet) {
            throw "Workbook sheet 'Rifle' was not found."
        }

        $relationshipId = $rifleSheet.GetAttribute("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")
        $rifleEntryPath = $relationshipPaths[$relationshipId]

        $sharedStrings = [Collections.Generic.List[string]]::new()
        if ($null -ne $zip.GetEntry("xl/sharedStrings.xml")) {
            $sharedDocument = [System.Xml.Linq.XDocument]::Parse((Read-ZipEntryText $zip "xl/sharedStrings.xml"))
            $sharedNamespace = [System.Xml.Linq.XNamespace]"http://schemas.openxmlformats.org/spreadsheetml/2006/main"
            foreach ($item in $sharedDocument.Descendants($sharedNamespace + "si")) {
                $sharedStrings.Add((($item.Descendants($sharedNamespace + "t") | ForEach-Object Value) -join ""))
            }
        }

        $worksheet = [System.Xml.Linq.XDocument]::Parse(
            (Read-ZipEntryText $zip $rifleEntryPath),
            [System.Xml.Linq.LoadOptions]::PreserveWhitespace)
        $namespace = [System.Xml.Linq.XNamespace]"http://schemas.openxmlformats.org/spreadsheetml/2006/main"
        $headerRow = $worksheet.Descendants($namespace + "row") | Where-Object {
            $_.Elements($namespace + "c") | Where-Object {
                (Get-CellText $_ $sharedStrings $namespace) -eq "Perk Name"
            }
        } | Select-Object -First 1
        if ($null -eq $headerRow) {
            throw "Rifle sheet does not contain a Perk Name header."
        }

        $headerColumns = @{}
        foreach ($cell in $headerRow.Elements($namespace + "c")) {
            $column = ([regex]::Match($cell.Attribute("r").Value, "^[A-Z]+")).Value
            $headerColumns[(Get-CellText $cell $sharedStrings $namespace)] = $column
        }

        foreach ($entry in $descriptionUpdates.GetEnumerator()) {
            $perkRow = $worksheet.Descendants($namespace + "row") | Where-Object {
                $row = $_
                $nameCell = $row.Elements($namespace + "c") | Where-Object {
                    $_.Attribute("r").Value -match "^$($headerColumns['Perk Name'])\d+$"
                } | Select-Object -First 1
                $null -ne $nameCell -and (Get-CellText $nameCell $sharedStrings $namespace) -eq $entry.Key
            } | Select-Object -First 1
            if ($null -eq $perkRow) {
                throw "Perk '$($entry.Key)' was not found on the Rifle sheet."
            }

            $rowNumber = $perkRow.Attribute("r").Value
            $descriptionReference = "$($headerColumns['Description'])$rowNumber"
            $descriptionCell = $perkRow.Elements($namespace + "c") | Where-Object {
                $_.Attribute("r").Value -eq $descriptionReference
            } | Select-Object -First 1
            if ($null -eq $descriptionCell) {
                throw "Description cell '$descriptionReference' was not found for perk '$($entry.Key)'."
            }
            Set-InlineCellText $descriptionCell ([string]$entry.Value) $namespace
        }

        Write-WorksheetEntry $zip $rifleEntryPath $worksheet
    }
    finally {
        $zip.Dispose()
    }

    try {
        [IO.File]::Replace($tempWorkbookPath, $workbookFullPath, $backupWorkbookPath, $true)
        Assert-RifleDescriptionUpdates $workbookFullPath $descriptionUpdates
        $replacementValidated = $true
    }
    catch {
        $replacementError = $_
        if ([IO.File]::Exists($backupWorkbookPath)) {
            [IO.File]::Replace($backupWorkbookPath, $workbookFullPath, $null, $true)
        }
        throw $replacementError
    }
}
finally {
    if ([IO.File]::Exists($tempWorkbookPath)) {
        [IO.File]::Delete($tempWorkbookPath)
    }
    if ($replacementValidated -and [IO.File]::Exists($backupWorkbookPath)) {
        [IO.File]::Delete($backupWorkbookPath)
    }
}

Write-Host "Updated Rifle descriptions from live testing feedback while preserving all other workbook entries."
