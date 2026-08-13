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

$existingDescriptions = [ordered]@{
    "Throw Rock I" = "Hurls stone or loose debris with the Force up to 15m, dealing 22 physical DMG plus WIL/PER scaling to one target. This power gains +10% hit chance."
    "Throw Rock II" = "Hurls a heavier stone or debris with the Force up to 15m, dealing 40 physical DMG plus WIL/PER scaling to one target. This power gains +10% hit chance."
    "Throw Rock III" = "Hurls a crushing mass of stone and debris with the Force up to 15m, dealing 60 physical DMG plus WIL/PER scaling to one target. This power gains +10% hit chance."
    "Force Judgment I" = "Deals 18 force DMG plus WIL scaling to one target and reduces outgoing weapon and force damage by 4% for 30 seconds. This power gains +10% hit chance."
    "Force Judgment II" = "Deals 32 force DMG plus WIL scaling to the selected target and one enemy within 5m, reducing outgoing weapon and force damage by 6% for 30 seconds. This power gains +10% hit chance."
    "Force Judgment III" = "Deals 48 force DMG plus WIL scaling to the selected target and enemies within 5m, reducing outgoing weapon and force damage by 8% for 30 seconds. This power gains +10% hit chance."
    "Radiant Lance I" = "Fires a focused lance of radiant Force energy in an 8m x 2.5m line, dealing 16 force DMG plus WIL scaling to hostile targets in the line. This power gains +10% hit chance."
    "Radiant Lance II" = "Fires a focused lance of radiant Force energy in an 8m x 2.5m line, dealing 30 force DMG plus WIL scaling to hostile targets in the line. This power gains +10% hit chance."
    "Radiant Lance III" = "Fires a focused lance of radiant Force energy in an 8m x 2.5m line, dealing 44 force DMG plus WIL scaling to hostile targets in the line. This power gains +10% hit chance."
}

$forceBurstRows = @(
    [ordered]@{
        _AfterPerkName = "Force Lightning I"
        Style = "Alter"; "SP Price" = "3.0"; "Perk Name" = "Force Burst I"; "Skill Reqs." = "Force 10"
        "Char. Type" = "Force"; Type = "Combat"; Alignment = "Light"; "Affinity Shift" = "+1"
        Description = "Deals 18 force DMG plus WIL scaling to the selected target and enemies within 5m. This power gains +10% hit chance."
        "Primary Stat" = "WIL"; "Secondary Stat" = "None"; "Scaling Source" = "Combat Formula"
        FP = "4.0"; STM = "-"; "Casting Time" = "1.5 seconds"; "Cooldown Time" = "15 seconds"
        "Dev Status" = "Implemented"; "Additional Requirements" = ""
        Notes = "Restored Light Alter area-damage line for ordinary-enemy solo pacing and telekinetic group pressure."
    },
    [ordered]@{
        _AfterPerkName = "Force Choke III"
        Style = "Alter"; "SP Price" = "4.0"; "Perk Name" = "Force Burst II"; "Skill Reqs." = "Force 30"
        "Char. Type" = "Force"; Type = "Combat"; Alignment = "Light"; "Affinity Shift" = "+1"
        Description = "Deals 34 force DMG plus WIL scaling to the selected target and enemies within 5m. This power gains +10% hit chance."
        "Primary Stat" = "WIL"; "Secondary Stat" = "None"; "Scaling Source" = "Combat Formula"
        FP = "5.0"; STM = "-"; "Casting Time" = "1.5 seconds"; "Cooldown Time" = "15 seconds"
        "Dev Status" = "Implemented"; "Additional Requirements" = ""
        Notes = "Replacement tier: increases the restored 5m telekinetic burst without adding control or sustain riders."
    },
    [ordered]@{
        _AfterPerkName = "Throw Lightsaber III"
        Style = "Alter"; "SP Price" = "4.0"; "Perk Name" = "Force Burst III"; "Skill Reqs." = "Force 46"
        "Char. Type" = "Force"; Type = "Combat"; Alignment = "Light"; "Affinity Shift" = "+1"
        Description = "Deals 50 force DMG plus WIL scaling to the selected target and enemies within 5m. This power gains +10% hit chance."
        "Primary Stat" = "WIL"; "Secondary Stat" = "None"; "Scaling Source" = "Combat Formula"
        FP = "6.0"; STM = "-"; "Casting Time" = "1.5 seconds"; "Cooldown Time" = "15 seconds"
        "Dev Status" = "Implemented"; "Additional Requirements" = ""
        Notes = "Final restored burst rank supports late-game ordinary-enemy pacing while remaining below Dark sustain and execute packages."
    }
)

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

function Set-FormulaCell {
    param(
        [System.Xml.Linq.XElement]$Cell,
        [string]$Formula,
        [double]$CachedValue,
        [System.Xml.Linq.XNamespace]$Namespace
    )

    $Cell.SetAttributeValue("t", $null)
    $Cell.RemoveNodes()
    $Cell.Add([System.Xml.Linq.XElement]::new($Namespace + "f", $Formula))
    $Cell.Add([System.Xml.Linq.XElement]::new(
        $Namespace + "v",
        $CachedValue.ToString("0.###", [Globalization.CultureInfo]::InvariantCulture)))
}

if (!(Test-Path -LiteralPath $workbookFullPath)) {
    throw "Workbook '$workbookFullPath' was not found."
}

$tempWorkbookPath = Join-Path ([IO.Path]::GetTempPath()) ("swlor-light-consular-{0}.xlsx" -f [guid]::NewGuid())
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
        $forceSheet = $workbookXml.SelectNodes("//d:sheets/d:sheet", $manager) |
            Where-Object { $_.GetAttribute("name") -eq "Force" } |
            Select-Object -First 1
        if ($null -eq $forceSheet) {
            throw "Workbook sheet 'Force' was not found."
        }

        $relationshipId = $forceSheet.GetAttribute("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")
        $entryPath = $relationshipPaths[$relationshipId]
        $sharedStrings = [Collections.Generic.List[string]]::new()
        if ($null -ne $zip.GetEntry("xl/sharedStrings.xml")) {
            $sharedDocument = [System.Xml.Linq.XDocument]::Parse((Read-ZipEntryText $zip "xl/sharedStrings.xml"))
            $sharedNamespace = [System.Xml.Linq.XNamespace]"http://schemas.openxmlformats.org/spreadsheetml/2006/main"
            foreach ($item in $sharedDocument.Descendants($sharedNamespace + "si")) {
                $sharedStrings.Add((($item.Descendants($sharedNamespace + "t") | ForEach-Object Value) -join ""))
            }
        }

        $worksheet = [System.Xml.Linq.XDocument]::Parse(
            (Read-ZipEntryText $zip $entryPath),
            [System.Xml.Linq.LoadOptions]::PreserveWhitespace)
        $namespace = [System.Xml.Linq.XNamespace]"http://schemas.openxmlformats.org/spreadsheetml/2006/main"
        $rows = @($worksheet.Descendants($namespace + "row"))
        $headerRow = $rows | Where-Object {
            $_.Elements($namespace + "c") | Where-Object {
                (Get-CellText $_ $sharedStrings $namespace) -eq "Perk Name"
            }
        } | Select-Object -First 1
        if ($null -eq $headerRow) {
            throw "Force sheet does not contain a Perk Name header."
        }

        $headerColumns = @{}
        foreach ($cell in $headerRow.Elements($namespace + "c")) {
            $column = ([regex]::Match($cell.Attribute("r").Value, "^[A-Z]+")).Value
            $headerColumns[(Get-CellText $cell $sharedStrings $namespace)] = $column
        }

        $rowsByPerkName = @{}
        foreach ($row in $rows) {
            $nameCell = $row.Elements($namespace + "c") | Where-Object {
                $_.Attribute("r").Value -match "^$($headerColumns['Perk Name'])\d+$"
            } | Select-Object -First 1
            if ($null -ne $nameCell) {
                $rowsByPerkName[(Get-CellText $nameCell $sharedStrings $namespace)] = $row
            }
        }

        foreach ($entry in $existingDescriptions.GetEnumerator()) {
            if (-not $rowsByPerkName.ContainsKey($entry.Key)) {
                throw "Perk '$($entry.Key)' was not found on the Force sheet."
            }
            $row = $rowsByPerkName[$entry.Key]
            $rowNumber = $row.Attribute("r").Value
            $descriptionCell = $row.Elements($namespace + "c") | Where-Object {
                $_.Attribute("r").Value -eq "$($headerColumns['Description'])$rowNumber"
            } | Select-Object -First 1
            Set-InlineCellText $descriptionCell ([string]$entry.Value) $namespace
        }

        $templateRows = @(
            $rowsByPerkName["Throw Rock I"],
            $rowsByPerkName["Throw Rock II"],
            $rowsByPerkName["Throw Rock III"]
        )
        $sheetData = $worksheet.Descendants($namespace + "sheetData") | Select-Object -First 1

        foreach ($values in $forceBurstRows) {
            $perkName = $values["Perk Name"]
            if ($rowsByPerkName.ContainsKey($perkName)) {
                $rowsByPerkName[$perkName].Remove()
                $rowsByPerkName.Remove($perkName)
            }
        }

        for ($index = 0; $index -lt $forceBurstRows.Count; $index++) {
            $values = $forceBurstRows[$index]
            $perkName = $values["Perk Name"]
            $afterPerkName = $values["_AfterPerkName"]
            if (-not $rowsByPerkName.ContainsKey($afterPerkName)) {
                throw "Insertion anchor '$afterPerkName' was not found on the Force sheet."
            }

            $rowNumber = 1 + [int]$rowsByPerkName[$afterPerkName].Attribute("r").Value
            $rowsToShift = @($sheetData.Elements($namespace + "row") | Where-Object {
                [int]$_.Attribute("r").Value -ge $rowNumber
            } | Sort-Object { [int]$_.Attribute("r").Value } -Descending)
            foreach ($rowToShift in $rowsToShift) {
                $shiftedRowNumber = 1 + [int]$rowToShift.Attribute("r").Value
                $rowToShift.SetAttributeValue("r", $shiftedRowNumber)
                foreach ($cell in $rowToShift.Elements($namespace + "c")) {
                    $column = ([regex]::Match($cell.Attribute("r").Value, "^[A-Z]+")).Value
                    $cell.SetAttributeValue("r", "$column$shiftedRowNumber")
                }
            }

            $row = [System.Xml.Linq.XElement]::new($templateRows[$index])
            $row.SetAttributeValue("r", $rowNumber)
            foreach ($cell in $row.Elements($namespace + "c")) {
                $column = ([regex]::Match($cell.Attribute("r").Value, "^[A-Z]+")).Value
                $cell.SetAttributeValue("r", "$column$rowNumber")
            }
            $nextRow = $sheetData.Elements($namespace + "row") | Where-Object {
                [int]$_.Attribute("r").Value -gt $rowNumber
            } | Sort-Object { [int]$_.Attribute("r").Value } | Select-Object -First 1
            if ($null -ne $nextRow) {
                $nextRow.AddBeforeSelf($row)
            }
            else {
                $sheetData.Add($row)
            }
            $rowsByPerkName[$perkName] = $row

            foreach ($field in $values.Keys) {
                if ($field.StartsWith("_")) {
                    continue
                }
                if (-not $headerColumns.ContainsKey($field)) {
                    throw "Column '$field' was not found on the Force sheet."
                }
                $reference = "$($headerColumns[$field])$rowNumber"
                $cell = $row.Elements($namespace + "c") | Where-Object {
                    $_.Attribute("r").Value -eq $reference
                } | Select-Object -First 1
                if ($null -eq $cell) {
                    throw "Cell '$reference' was not found in the Force row template."
                }
                Set-InlineCellText $cell ([string]$values[$field]) $namespace
            }
        }

        $formulaUpdates = @(
            @{ Cell = "D4"; Formula = "SUM(B39,B64,B82)"; CachedValue = 251 },
            @{ Cell = "B39"; Formula = "SUM(B8:B38)"; CachedValue = 105 },
            @{ Cell = "B64"; Formula = "SUM(B41:B63)"; CachedValue = 87 },
            @{ Cell = "B82"; Formula = "SUM(B66:B81)"; CachedValue = 59 }
        )
        foreach ($formulaUpdate in $formulaUpdates) {
            $formulaCell = $worksheet.Descendants($namespace + "c") | Where-Object {
                $_.Attribute("r").Value -eq $formulaUpdate.Cell
            } | Select-Object -First 1
            if ($null -eq $formulaCell) {
                throw "Formula cell '$($formulaUpdate.Cell)' was not found on the Force sheet."
            }
            Set-FormulaCell $formulaCell $formulaUpdate.Formula $formulaUpdate.CachedValue $namespace
        }

        $existingEntry = $zip.GetEntry($entryPath)
        $existingEntry.Delete()
        $replacement = $zip.CreateEntry($entryPath, [IO.Compression.CompressionLevel]::Optimal)
        $stream = $replacement.Open()
        try {
            $writer = [IO.StreamWriter]::new($stream, [Text.UTF8Encoding]::new($false))
            try {
                $worksheet.Save($writer, [System.Xml.Linq.SaveOptions]::DisableFormatting)
            }
            finally {
                $writer.Dispose()
            }
        }
        finally {
            $stream.Dispose()
        }
    }
    finally {
        $zip.Dispose()
    }

    [IO.File]::Copy($tempWorkbookPath, $workbookFullPath, $true)
}
finally {
    if ([IO.File]::Exists($tempWorkbookPath)) {
        [IO.File]::Delete($tempWorkbookPath)
    }
}

Write-Host "Updated 9 Light Consular rows and restored 3 Force Burst rows in the Combat Upgrade Design Bible."
