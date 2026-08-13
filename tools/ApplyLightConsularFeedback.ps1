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
    "Throw Rock I" = "Hurls stone or loose debris with the Force up to 15m, dealing 22 physical DMG plus WIL/PER scaling to one target."
    "Throw Rock II" = "Hurls a heavier stone or debris with the Force up to 15m, dealing 40 physical DMG plus WIL/PER scaling to one target."
    "Throw Rock III" = "Hurls a crushing mass of stone and debris with the Force up to 15m, dealing 60 physical DMG plus WIL/PER scaling to one target."
    "Force Judgment I" = "Deals 18 force DMG plus WIL scaling to one target and reduces outgoing weapon and force damage by 4% for 30 seconds."
    "Force Judgment II" = "Deals 32 force DMG plus WIL scaling to the selected target and one enemy within 5m, reducing outgoing weapon and force damage by 6% for 30 seconds."
    "Force Judgment III" = "Deals 48 force DMG plus WIL scaling to the selected target and enemies within 5m, reducing outgoing weapon and force damage by 8% for 30 seconds."
    "Radiant Lance I" = "Fires a focused lance of radiant Force energy in an 8m x 2.5m line, dealing 16 force DMG plus WIL scaling to hostile targets in the line."
    "Radiant Lance II" = "Fires a focused lance of radiant Force energy in an 8m x 2.5m line, dealing 30 force DMG plus WIL scaling to hostile targets in the line."
    "Radiant Lance III" = "Fires a focused lance of radiant Force energy in an 8m x 2.5m line, dealing 44 force DMG plus WIL scaling to hostile targets in the line."
}

$deviceTwinDescriptions = [ordered]@{
    "Arc Projector I" = "Projects a focused electrical arc up to 15m, dealing 22 electrical DMG plus PER scaling to one target."
    "Arc Projector II" = "Projects a stronger electrical arc up to 15m, dealing 40 electrical DMG plus PER scaling to one target."
    "Arc Projector III" = "Projects an overcharged electrical arc up to 15m, dealing 60 electrical DMG plus PER scaling to one target."
    "Ion Lance I" = "Fires a focused ion beam from a wrist projector in an 8m x 2.5m line, dealing 16 electrical DMG plus PER scaling to hostile targets in the line."
    "Ion Lance II" = "Fires a focused ion beam from a wrist projector in an 8m x 2.5m line, dealing 30 electrical DMG plus PER scaling to hostile targets in the line."
    "Ion Lance III" = "Fires a focused ion beam from a wrist projector in an 8m x 2.5m line, dealing 44 electrical DMG plus PER scaling to hostile targets in the line."
}

$existingPrices = [ordered]@{
    "Force Judgment I" = "2.0"
    "Force Judgment II" = "3.0"
    "Force Judgment III" = "3.0"
}

$forceBurstRow = [ordered]@{
    _AfterPerkName = "Force Choke III"
    Style = "Alter"; "SP Price" = "3.0"; "Perk Name" = "Force Burst"; "Skill Reqs." = "Force 30"
    "Char. Type" = "Force"; Type = "Combat"; Alignment = "Light"; "Affinity Shift" = "+1"
    Description = "Deals 50 force DMG plus WIL scaling to the selected target and enemies within 5m."
    "Primary Stat" = "WIL"; "Secondary Stat" = "None"; "Scaling Source" = "Combat Formula"
    FP = "6.0"; STM = "-"; "Casting Time" = "1.5 seconds"; "Cooldown Time" = "15 seconds"
    "Dev Status" = "Implemented"; "Additional Requirements" = ""
    Notes = "Single-rank Light Alter area damage restores telekinetic group pressure while preserving Force and Devices SP and ability-count parity."
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

function Set-NumericCellValue {
    param(
        [System.Xml.Linq.XElement]$Cell,
        [double]$Value,
        [System.Xml.Linq.XNamespace]$Namespace
    )

    $Cell.SetAttributeValue("t", $null)
    $Cell.RemoveNodes()
    $Cell.Add([System.Xml.Linq.XElement]::new(
        $Namespace + "v",
        $Value.ToString("0.###", [Globalization.CultureInfo]::InvariantCulture)))
}

function Get-RowsByPerkName {
    param(
        [System.Xml.Linq.XDocument]$Worksheet,
        [hashtable]$HeaderColumns,
        [Collections.Generic.List[string]]$SharedStrings,
        [System.Xml.Linq.XNamespace]$Namespace
    )

    $result = @{}
    foreach ($row in $Worksheet.Descendants($Namespace + "row")) {
        $nameCell = $row.Elements($Namespace + "c") | Where-Object {
            $_.Attribute("r").Value -match "^$($HeaderColumns['Perk Name'])\d+$"
        } | Select-Object -First 1
        if ($null -ne $nameCell) {
            $name = Get-CellText $nameCell $SharedStrings $Namespace
            if (-not [string]::IsNullOrWhiteSpace($name)) {
                $result[$name] = $row
            }
        }
    }
    return $result
}

function Remove-WorksheetRow {
    param(
        [System.Xml.Linq.XElement]$Row,
        [System.Xml.Linq.XDocument]$Worksheet,
        [System.Xml.Linq.XNamespace]$Namespace
    )

    $removedRowNumber = [int]$Row.Attribute("r").Value
    $Row.Remove()
    $rowsToShift = @($Worksheet.Descendants($Namespace + "row") | Where-Object {
        [int]$_.Attribute("r").Value -gt $removedRowNumber
    } | Sort-Object { [int]$_.Attribute("r").Value })
    foreach ($rowToShift in $rowsToShift) {
        $shiftedRowNumber = [int]$rowToShift.Attribute("r").Value - 1
        $rowToShift.SetAttributeValue("r", $shiftedRowNumber)
        foreach ($cell in $rowToShift.Elements($Namespace + "c")) {
            $column = ([regex]::Match($cell.Attribute("r").Value, "^[A-Z]+")).Value
            $cell.SetAttributeValue("r", "$column$shiftedRowNumber")
        }
    }
}

function Update-SheetFormulaCaches {
    param(
        [System.Xml.Linq.XDocument]$Worksheet,
        [hashtable]$HeaderColumns,
        [Collections.Generic.List[string]]$SharedStrings,
        [System.Xml.Linq.XNamespace]$Namespace
    )

    $perkNameHeaderCell = $Worksheet.Descendants($Namespace + "c") | Where-Object {
        (Get-CellText $_ $SharedStrings $Namespace) -eq "Perk Name"
    } | Select-Object -First 1
    if ($null -eq $perkNameHeaderCell) {
        throw "Perk Name header cell was not found while updating formula caches."
    }
    $headerRowNumber = [int]([regex]::Match($perkNameHeaderCell.Attribute("r").Value, "\d+$")).Value

    foreach ($formulaCell in @($Worksheet.Descendants($Namespace + "c") | Where-Object {
        $_.Attribute("r").Value -match "^B(\d+)$" -and
        [int]$Matches[1] -le $headerRowNumber -and
        $null -ne $_.Element($Namespace + "f")
    })) {
        $formulaCell.SetAttributeValue("t", $null)
        $formulaCell.RemoveNodes()
    }

    $styleRows = @{}
    foreach ($row in $Worksheet.Descendants($Namespace + "row")) {
        $rowNumber = [int]$row.Attribute("r").Value
        if ($rowNumber -le $headerRowNumber) {
            continue
        }
        $nameCell = $row.Elements($Namespace + "c") | Where-Object {
            $_.Attribute("r").Value -eq "$($HeaderColumns['Perk Name'])$rowNumber"
        } | Select-Object -First 1
        if ($null -eq $nameCell -or [string]::IsNullOrWhiteSpace((Get-CellText $nameCell $SharedStrings $Namespace))) {
            continue
        }

        $styleCell = $row.Elements($Namespace + "c") | Where-Object {
            $_.Attribute("r").Value -eq "$($HeaderColumns['Style'])$rowNumber"
        } | Select-Object -First 1
        $priceCell = $row.Elements($Namespace + "c") | Where-Object {
            $_.Attribute("r").Value -eq "$($HeaderColumns['SP Price'])$rowNumber"
        } | Select-Object -First 1
        $style = Get-CellText $styleCell $SharedStrings $Namespace
        $priceText = Get-CellText $priceCell $SharedStrings $Namespace
        $price = 0.0
        if ([string]::IsNullOrWhiteSpace($style) -or
            -not [double]::TryParse(
                $priceText,
                [Globalization.NumberStyles]::Number,
                [Globalization.CultureInfo]::InvariantCulture,
                [ref]$price)) {
            continue
        }

        if (-not $styleRows.ContainsKey($style)) {
            $styleRows[$style] = [Collections.Generic.List[object]]::new()
        }
        $styleRows[$style].Add([pscustomobject]@{ Row = $rowNumber; Price = $price })
    }

    $subtotalReferences = [Collections.Generic.List[string]]::new()
    $grandTotal = 0.0
    foreach ($style in @($styleRows.Keys | Sort-Object { ($styleRows[$_].Row | Measure-Object -Minimum).Minimum })) {
        $firstRow = ($styleRows[$style].Row | Measure-Object -Minimum).Minimum
        $lastRow = ($styleRows[$style].Row | Measure-Object -Maximum).Maximum
        $subtotal = ($styleRows[$style].Price | Measure-Object -Sum).Sum
        $subtotalRow = $lastRow + 1
        $subtotalCell = $Worksheet.Descendants($Namespace + "c") | Where-Object {
            $_.Attribute("r").Value -eq "B$subtotalRow"
        } | Select-Object -First 1
        if ($null -eq $subtotalCell) {
            throw "Subtotal cell 'B$subtotalRow' was not found after the '$style' rows."
        }
        Set-FormulaCell $subtotalCell "SUM(B${firstRow}:B${lastRow})" $subtotal $Namespace
        $subtotalReferences.Add("B$subtotalRow")
        $grandTotal += $subtotal
    }

    $grandTotalCell = $Worksheet.Descendants($Namespace + "c") | Where-Object {
        $_.Attribute("r").Value -eq "D4"
    } | Select-Object -First 1
    if ($null -eq $grandTotalCell) {
        throw "Grand total cell 'D4' was not found."
    }
    Set-FormulaCell $grandTotalCell ("SUM({0})" -f ($subtotalReferences -join ",")) $grandTotal $Namespace

    $dimension = $Worksheet.Descendants($Namespace + "dimension") | Select-Object -First 1
    if ($null -ne $dimension) {
        $lastWorksheetRow = ($Worksheet.Descendants($Namespace + "row") | ForEach-Object {
            [int]$_.Attribute("r").Value
        } | Measure-Object -Maximum).Maximum
        $currentReference = $dimension.Attribute("ref").Value
        $lastColumn = ([regex]::Match($currentReference, ":([A-Z]+)\d+$")).Groups[1].Value
        if (-not [string]::IsNullOrWhiteSpace($lastColumn)) {
            $dimension.SetAttributeValue("ref", "A1:$lastColumn$lastWorksheetRow")
        }
    }
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

        $devicesSheet = $workbookXml.SelectNodes("//d:sheets/d:sheet", $manager) |
            Where-Object { $_.GetAttribute("name") -eq "Devices" } |
            Select-Object -First 1
        if ($null -eq $devicesSheet) {
            throw "Workbook sheet 'Devices' was not found."
        }

        $relationshipId = $forceSheet.GetAttribute("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")
        $forceEntryPath = $relationshipPaths[$relationshipId]
        $devicesRelationshipId = $devicesSheet.GetAttribute("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")
        $devicesEntryPath = $relationshipPaths[$devicesRelationshipId]
        $sharedStrings = [Collections.Generic.List[string]]::new()
        if ($null -ne $zip.GetEntry("xl/sharedStrings.xml")) {
            $sharedDocument = [System.Xml.Linq.XDocument]::Parse((Read-ZipEntryText $zip "xl/sharedStrings.xml"))
            $sharedNamespace = [System.Xml.Linq.XNamespace]"http://schemas.openxmlformats.org/spreadsheetml/2006/main"
            foreach ($item in $sharedDocument.Descendants($sharedNamespace + "si")) {
                $sharedStrings.Add((($item.Descendants($sharedNamespace + "t") | ForEach-Object Value) -join ""))
            }
        }

        $worksheet = [System.Xml.Linq.XDocument]::Parse(
            (Read-ZipEntryText $zip $forceEntryPath),
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

        $rowsByPerkName = Get-RowsByPerkName $worksheet $headerColumns $sharedStrings $namespace

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

        foreach ($entry in $existingPrices.GetEnumerator()) {
            if (-not $rowsByPerkName.ContainsKey($entry.Key)) {
                throw "Perk '$($entry.Key)' was not found on the Force sheet."
            }
            $row = $rowsByPerkName[$entry.Key]
            $rowNumber = $row.Attribute("r").Value
            $priceCell = $row.Elements($namespace + "c") | Where-Object {
                $_.Attribute("r").Value -eq "$($headerColumns['SP Price'])$rowNumber"
            } | Select-Object -First 1
            Set-NumericCellValue $priceCell ([double]$entry.Value) $namespace
        }

        $sheetData = $worksheet.Descendants($namespace + "sheetData") | Select-Object -First 1
        $forceBurstWorksheetRow = if ($rowsByPerkName.ContainsKey("Force Burst")) {
            $rowsByPerkName["Force Burst"]
        }
        elseif ($rowsByPerkName.ContainsKey("Force Burst II")) {
            $rowsByPerkName["Force Burst II"]
        }
        elseif ($rowsByPerkName.ContainsKey("Force Burst I")) {
            $rowsByPerkName["Force Burst I"]
        }
        elseif ($rowsByPerkName.ContainsKey("Force Burst III")) {
            $rowsByPerkName["Force Burst III"]
        }
        else {
            $null
        }

        if ($null -ne $forceBurstWorksheetRow) {
            $obsoleteRows = @(
                "Force Burst",
                "Force Burst I",
                "Force Burst II",
                "Force Burst III"
            ) | Where-Object {
                $rowsByPerkName.ContainsKey($_) -and
                -not [object]::ReferenceEquals($rowsByPerkName[$_], $forceBurstWorksheetRow)
            } | ForEach-Object {
                $rowsByPerkName[$_]
            } | Sort-Object { [int]$_.Attribute("r").Value } -Descending
            foreach ($obsoleteRow in $obsoleteRows) {
                Remove-WorksheetRow $obsoleteRow $worksheet $namespace
            }
        }
        else {
            $afterPerkName = $forceBurstRow["_AfterPerkName"]
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

            if (-not $rowsByPerkName.ContainsKey("Throw Rock II")) {
                throw "Template row 'Throw Rock II' was not found on the Force sheet."
            }
            $forceBurstWorksheetRow = [System.Xml.Linq.XElement]::new($rowsByPerkName["Throw Rock II"])
            $forceBurstWorksheetRow.SetAttributeValue("r", $rowNumber)
            foreach ($cell in $forceBurstWorksheetRow.Elements($namespace + "c")) {
                $column = ([regex]::Match($cell.Attribute("r").Value, "^[A-Z]+")).Value
                $cell.SetAttributeValue("r", "$column$rowNumber")
            }
            $nextRow = $sheetData.Elements($namespace + "row") | Where-Object {
                [int]$_.Attribute("r").Value -gt $rowNumber
            } | Sort-Object { [int]$_.Attribute("r").Value } | Select-Object -First 1
            if ($null -ne $nextRow) {
                $nextRow.AddBeforeSelf($forceBurstWorksheetRow)
            }
            else {
                $sheetData.Add($forceBurstWorksheetRow)
            }
        }

        $forceBurstRowNumber = $forceBurstWorksheetRow.Attribute("r").Value
        foreach ($field in $forceBurstRow.Keys) {
            if ($field.StartsWith("_")) {
                continue
            }
            if (-not $headerColumns.ContainsKey($field)) {
                throw "Column '$field' was not found on the Force sheet."
            }
            $reference = "$($headerColumns[$field])$forceBurstRowNumber"
            $cell = $forceBurstWorksheetRow.Elements($namespace + "c") | Where-Object {
                $_.Attribute("r").Value -eq $reference
            } | Select-Object -First 1
            if ($null -eq $cell) {
                throw "Cell '$reference' was not found in the Force Burst row."
            }
            if ($field -eq "SP Price") {
                Set-NumericCellValue $cell ([double]$forceBurstRow[$field]) $namespace
            }
            else {
                Set-InlineCellText $cell ([string]$forceBurstRow[$field]) $namespace
            }
        }

        Update-SheetFormulaCaches $worksheet $headerColumns $sharedStrings $namespace
        Write-WorksheetEntry $zip $forceEntryPath $worksheet

        $devicesWorksheet = [System.Xml.Linq.XDocument]::Parse(
            (Read-ZipEntryText $zip $devicesEntryPath),
            [System.Xml.Linq.LoadOptions]::PreserveWhitespace)
        $devicesRows = @($devicesWorksheet.Descendants($namespace + "row"))
        $devicesHeaderRow = $devicesRows | Where-Object {
            $_.Elements($namespace + "c") | Where-Object {
                (Get-CellText $_ $sharedStrings $namespace) -eq "Perk Name"
            }
        } | Select-Object -First 1
        if ($null -eq $devicesHeaderRow) {
            throw "Devices sheet does not contain a Perk Name header."
        }

        $devicesHeaderColumns = @{}
        foreach ($cell in $devicesHeaderRow.Elements($namespace + "c")) {
            $column = ([regex]::Match($cell.Attribute("r").Value, "^[A-Z]+")).Value
            $devicesHeaderColumns[(Get-CellText $cell $sharedStrings $namespace)] = $column
        }
        $devicesRowsByPerkName = Get-RowsByPerkName $devicesWorksheet $devicesHeaderColumns $sharedStrings $namespace
        foreach ($entry in $deviceTwinDescriptions.GetEnumerator()) {
            if (-not $devicesRowsByPerkName.ContainsKey($entry.Key)) {
                throw "Perk '$($entry.Key)' was not found on the Devices sheet."
            }
            $row = $devicesRowsByPerkName[$entry.Key]
            $rowNumber = $row.Attribute("r").Value
            $descriptionCell = $row.Elements($namespace + "c") | Where-Object {
                $_.Attribute("r").Value -eq "$($devicesHeaderColumns['Description'])$rowNumber"
            } | Select-Object -First 1
            Set-InlineCellText $descriptionCell ([string]$entry.Value) $namespace
        }
        Update-SheetFormulaCaches $devicesWorksheet $devicesHeaderColumns $sharedStrings $namespace
        Write-WorksheetEntry $zip $devicesEntryPath $devicesWorksheet
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

Write-Host "Updated Light Consular balance, synchronized Devices twin damage, ensured one Force Burst row, and refreshed Force/Devices formula caches."
