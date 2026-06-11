param(
    [string]$BibleWorkbookPath = "design/bible/SWLOR Design Bible - Combat Upgrade.xlsx"
)

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return Join-Path (Get-Location) $Path
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

function Get-WorksheetPath {
    param(
        [xml]$WorkbookXml,
        [hashtable]$RelationshipsById,
        [string]$SheetName
    )

    $workbookNamespace = [System.Xml.XmlNamespaceManager]::new($WorkbookXml.NameTable)
    $workbookNamespace.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")
    $workbookNamespace.AddNamespace("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")

    foreach ($sheet in $WorkbookXml.SelectNodes("//d:sheets/d:sheet", $workbookNamespace)) {
        if ($sheet.GetAttribute("name") -ne $SheetName) {
            continue
        }

        $relationshipId = $sheet.GetAttribute("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")
        return $RelationshipsById[$relationshipId]
    }

    throw "Workbook sheet '$SheetName' was not found."
}

function Get-RowNode {
    param(
        [xml]$WorksheetXml,
        [int]$RowNumber
    )

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    $namespaceManager = [System.Xml.XmlNamespaceManager]::new($WorksheetXml.NameTable)
    $namespaceManager.AddNamespace("d", $namespace)
    $rowNode = $WorksheetXml.SelectSingleNode("//d:sheetData/d:row[@r='$RowNumber']", $namespaceManager)
    if ($null -ne $rowNode) {
        return $rowNode
    }

    $sheetData = $WorksheetXml.SelectSingleNode("//d:sheetData", $namespaceManager)
    $rowNode = $WorksheetXml.CreateElement("row", $namespace)
    $rowNode.SetAttribute("r", $RowNumber.ToString())
    [void]$sheetData.AppendChild($rowNode)
    return $rowNode
}

function Get-Cell {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlElement]$RowNode,
        [string]$CellReference
    )

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    foreach ($cell in $RowNode.GetElementsByTagName("c", $namespace)) {
        if ($cell.GetAttribute("r") -eq $CellReference) {
            return $cell
        }
    }

    $cell = $WorksheetXml.CreateElement("c", $namespace)
    $cell.SetAttribute("r", $CellReference)
    [void]$RowNode.AppendChild($cell)
    return $cell
}

function Get-SharedStringValues {
    param([xml]$SharedStringsXml)

    $namespaceManager = [System.Xml.XmlNamespaceManager]::new($SharedStringsXml.NameTable)
    $namespaceManager.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")
    $items = $SharedStringsXml.SelectNodes("//d:sst/d:si", $namespaceManager)
    $values = @()
    foreach ($item in $items) {
        $values += (($item.SelectNodes(".//d:t", $namespaceManager) | ForEach-Object { $_.InnerText }) -join "")
    }

    return $values
}

function Get-CellText {
    param(
        [System.Xml.XmlElement]$Cell,
        [string[]]$SharedStrings
    )

    if ($null -eq $Cell) {
        return ""
    }

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    $type = $Cell.GetAttribute("t")
    if ($type -eq "s") {
        $valueNodes = $Cell.GetElementsByTagName("v", $namespace)
        if ($valueNodes.Count -eq 0) {
            return ""
        }

        return $SharedStrings[[int]$valueNodes[0].InnerText]
    }

    if ($type -eq "inlineStr") {
        return (($Cell.GetElementsByTagName("t", $namespace) | ForEach-Object { $_.InnerText }) -join "")
    }

    $valueNodes = $Cell.GetElementsByTagName("v", $namespace)
    if ($valueNodes.Count -gt 0) {
        return $valueNodes[0].InnerText
    }

    return ""
}

function Clear-Cell {
    param([System.Xml.XmlElement]$Cell)

    $cellReference = $Cell.GetAttribute("r")
    $style = $Cell.GetAttribute("s")
    while ($Cell.HasChildNodes) {
        [void]$Cell.RemoveChild($Cell.FirstChild)
    }

    $Cell.RemoveAllAttributes()
    $Cell.SetAttribute("r", $cellReference)
    if (![string]::IsNullOrWhiteSpace($style)) {
        $Cell.SetAttribute("s", $style)
    }
}

function Normalize-IntegerText {
    param([string]$Value)

    if ($Value -match "^\d+(\.0+)?$") {
        return ([int][decimal]$Value).ToString()
    }

    return $Value
}

function Set-TextCell {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlElement]$RowNode,
        [string]$CellReference,
        [string]$Value,
        [string]$Style = ""
    )

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    $cell = Get-Cell -WorksheetXml $WorksheetXml -RowNode $RowNode -CellReference $CellReference
    Clear-Cell -Cell $cell
    if (![string]::IsNullOrWhiteSpace($Style)) {
        $cell.SetAttribute("s", $Style)
    }

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return
    }

    $cell.SetAttribute("t", "inlineStr")
    $inlineString = $WorksheetXml.CreateElement("is", $namespace)
    $textElement = $WorksheetXml.CreateElement("t", $namespace)
    $textElement.InnerText = $Value
    [void]$inlineString.AppendChild($textElement)
    [void]$cell.AppendChild($inlineString)
}

function Set-NumberCell {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlElement]$RowNode,
        [string]$CellReference,
        [string]$Value,
        [string]$Style = ""
    )

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    $cell = Get-Cell -WorksheetXml $WorksheetXml -RowNode $RowNode -CellReference $CellReference
    Clear-Cell -Cell $cell
    if (![string]::IsNullOrWhiteSpace($Style)) {
        $cell.SetAttribute("s", $Style)
    }

    if ([string]::IsNullOrWhiteSpace($Value) -or $Value -eq "-") {
        Set-TextCell -WorksheetXml $WorksheetXml -RowNode $RowNode -CellReference $CellReference -Value $Value -Style $Style
        return
    }

    $valueElement = $WorksheetXml.CreateElement("v", $namespace)
    $valueElement.InnerText = (Normalize-IntegerText $Value)
    [void]$cell.AppendChild($valueElement)
}

function Set-FormulaCell {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlElement]$RowNode,
        [string]$CellReference,
        [string]$Formula,
        [string]$CachedValue,
        [string]$Style = ""
    )

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    $cell = Get-Cell -WorksheetXml $WorksheetXml -RowNode $RowNode -CellReference $CellReference
    Clear-Cell -Cell $cell
    if (![string]::IsNullOrWhiteSpace($Style)) {
        $cell.SetAttribute("s", $Style)
    }

    $formulaElement = $WorksheetXml.CreateElement("f", $namespace)
    $formulaElement.InnerText = $Formula
    [void]$cell.AppendChild($formulaElement)

    $valueElement = $WorksheetXml.CreateElement("v", $namespace)
    $valueElement.InnerText = $CachedValue
    [void]$cell.AppendChild($valueElement)
}

function Get-RequirementRank {
    param([string]$Requirement)

    if ([string]::IsNullOrWhiteSpace($Requirement) -or $Requirement -eq "-") {
        return 0
    }

    if ($Requirement -match "Force\s+(\d+)") {
        return [int]$Matches[1]
    }

    return 999
}

$workbookPath = Resolve-RepoPath $BibleWorkbookPath
if (!(Test-Path $workbookPath)) {
    throw "Workbook '$workbookPath' was not found."
}

$targetStyleByPerk = @{
    "Force Leap I" = "Control"
    "Force Leap II" = "Control"
    "Guardian Ward I" = "Control"
    "Guardian Ward II" = "Control"
    "Guardian Ward III" = "Control"
    "Guardian Ward IV" = "Control"
    "Force Intercept" = "Sense"
    "Deflective Presence" = "Control"
    "Reflective Barrier" = "Control"
    "Force Drain I" = "Alter"
    "Force Drain II" = "Alter"
    "Force Drain III" = "Alter"
    "Devouring Strike" = "Alter"
}

$notesByPerk = @{
    "Guardian Ward I" = "Opening Control shield rank."
    "Guardian Ward II" = "Second Control shield rank, spread out so the Ward line has a real progression."
    "Guardian Ward III" = "Third Control shield rank lands in the mid-late tree instead of immediately after Guardian Ward II."
    "Guardian Ward IV" = "Final regular Control Ward rank is delayed into the upper tree before the Force 50 capstone."
    "Force Leap I" = "Control mobility attack. No affinity scaling."
    "Force Leap II" = "Replacement tier: selected target receives the full lower-rank effect. Control mobility attack. No affinity scaling."
    "Force Intercept" = "Sense ally-protection response. Cannot target self."
    "Deflective Presence" = "Control protection trait placed early so protection support begins before later Ward ranks."
    "Reflective Barrier" = "Reflect value is lower than dedicated damage bonuses because it rides on Control protection powers."
    "Force Drain I" = "Alter life siphon line: sustain-focused rather than chain damage."
    "Force Drain II" = "Replacement tier: improves single-target siphon damage and low-health sustain."
    "Force Drain III" = "Replacement tier: top life siphon rank remains single-target so it does not overlap with Force Lightning's chain role."
    "Devouring Strike" = "Execute bonus for Alter damage powers, kept in line with other low-health damage traits."
}

$descriptionByPerk = @{
    "Deflective Presence" = "Control powers that grant temporary HP, absorb damage, or prevent defeat grant affected allies +4 Attack Deflection for 10 seconds."
    "Reflective Barrier" = "Control powers that grant temporary HP reflect 8% of force and energy damage taken, plus WIL scaling, back to the attacker while the temporary HP remains."
    "Devouring Strike" = "Alter powers that damage enemies deal 15% more damage to targets below 35% HP."
}

$categoryLayouts = @{
    Alter = @{
        StartRow = 8
        TotalRow = 33
        TotalFormula = "SUM(B8:B32)"
        TotalValue = "85"
    }
    Control = @{
        StartRow = 35
        TotalRow = 58
        TotalFormula = "SUM(B35:B57)"
        TotalValue = "87"
    }
    Sense = @{
        StartRow = 60
        TotalRow = 73
        TotalFormula = "SUM(B60:B72)"
        TotalValue = "48"
    }
}

$categoryOrder = @("Alter", "Control", "Sense")

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::Open($workbookPath, [System.IO.Compression.ZipArchiveMode]::Update)

try {
    [xml]$workbookXml = Read-ZipEntryText -Zip $zip -EntryPath "xl/workbook.xml"
    [xml]$relationshipsXml = Read-ZipEntryText -Zip $zip -EntryPath "xl/_rels/workbook.xml.rels"
    [xml]$sharedStringsXml = Read-ZipEntryText -Zip $zip -EntryPath "xl/sharedStrings.xml"
    $sharedStrings = Get-SharedStringValues -SharedStringsXml $sharedStringsXml

    $relationshipsById = @{}
    foreach ($relationship in $relationshipsXml.Relationships.Relationship) {
        $relationshipsById[$relationship.Id] = "xl/" + $relationship.Target.TrimStart("/")
    }

    $forceSheetPath = Get-WorksheetPath -WorkbookXml $workbookXml -RelationshipsById $relationshipsById -SheetName "Force"
    [xml]$forceWorksheetXml = Read-ZipEntryText -Zip $zip -EntryPath $forceSheetPath

    $namespaceManager = [System.Xml.XmlNamespaceManager]::new($forceWorksheetXml.NameTable)
    $namespaceManager.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")

    $columns = [ordered]@{
        Style = "A"
        Price = "B"
        PerkName = "C"
        SkillRequirements = "D"
        CharacterType = "E"
        Type = "F"
        Alignment = "G"
        AffinityShift = "H"
        Description = "I"
        PrimaryStat = "J"
        SecondaryStat = "K"
        ScalingSource = "L"
        FP = "M"
        STM = "N"
        CastingTime = "O"
        CooldownTime = "P"
        DevStatus = "Q"
        AdditionalRequirements = "R"
        Notes = "S"
    }

    $forceRows = @()
    foreach ($rowNode in $forceWorksheetXml.SelectNodes("//d:sheetData/d:row", $namespaceManager)) {
        $rowNumberText = $rowNode.GetAttribute("r")
        if ([string]::IsNullOrWhiteSpace($rowNumberText)) {
            continue
        }

        $rowNumber = [int]$rowNumberText
        if ($rowNumber -le 7) {
            continue
        }

        $row = [ordered]@{ OriginalRow = $rowNumber }
        foreach ($entry in $columns.GetEnumerator()) {
            $cellRef = "$($entry.Value)$rowNumber"
            $cell = $forceWorksheetXml.SelectSingleNode("//d:c[@r='$cellRef']", $namespaceManager)
            $row[$entry.Key] = Get-CellText -Cell $cell -SharedStrings $sharedStrings
        }

        if ([string]::IsNullOrWhiteSpace($row.PerkName) -or $row.PerkName -eq "Total") {
            continue
        }

        $forceRows += [pscustomobject]$row
    }

    foreach ($perkName in $targetStyleByPerk.Keys) {
        if (!@($forceRows | Where-Object { $_.PerkName -eq $perkName }).Count) {
            throw "Expected Force perk '$perkName' was not found in the workbook."
        }
    }

    foreach ($row in $forceRows) {
        if ($targetStyleByPerk.ContainsKey($row.PerkName)) {
            $row.Style = $targetStyleByPerk[$row.PerkName]
        }

        if ($notesByPerk.ContainsKey($row.PerkName)) {
            $row.Notes = $notesByPerk[$row.PerkName]
        }

        if ($descriptionByPerk.ContainsKey($row.PerkName)) {
            $row.Description = $descriptionByPerk[$row.PerkName]
        }

        $row.Price = Normalize-IntegerText $row.Price
        $row.AffinityShift = Normalize-IntegerText $row.AffinityShift
        $row.FP = Normalize-IntegerText $row.FP
        $row.STM = Normalize-IntegerText $row.STM
    }

    foreach ($rowNumber in 8..92) {
        $rowNode = Get-RowNode -WorksheetXml $forceWorksheetXml -RowNumber $rowNumber
        foreach ($columnName in $columns.Values) {
            $cell = Get-Cell -WorksheetXml $forceWorksheetXml -RowNode $rowNode -CellReference "$columnName$rowNumber"
            Clear-Cell -Cell $cell
        }
    }

    $writtenRows = @()
    foreach ($category in $categoryOrder) {
        $layout = $categoryLayouts[$category]
        $rowNumber = [int]$layout.StartRow
        $categoryRows = $forceRows |
            Where-Object { $_.Style -eq $category } |
            Sort-Object @{ Expression = { Get-RequirementRank $_.SkillRequirements } }, @{ Expression = { [int]$_.OriginalRow } }

        foreach ($row in $categoryRows) {
            $rowNode = Get-RowNode -WorksheetXml $forceWorksheetXml -RowNumber $rowNumber
            foreach ($column in $columns.GetEnumerator()) {
                $cellReference = "$($column.Value)$rowNumber"
                $value = $row.($column.Key)
                if ($column.Key -in @("Price", "AffinityShift", "FP", "STM") -and $value -match "^\d+(\.0+)?$") {
                    Set-NumberCell -WorksheetXml $forceWorksheetXml -RowNode $rowNode -CellReference $cellReference -Value $value -Style "4"
                }
                else {
                    Set-TextCell -WorksheetXml $forceWorksheetXml -RowNode $rowNode -CellReference $cellReference -Value $value -Style "4"
                }
            }

            $writtenRows += [pscustomobject]@{
                Style = $category
                Row = $rowNumber
                Price = [int]$row.Price
                PerkName = $row.PerkName
                Type = $row.Type
            }
            $rowNumber++
        }

        $totalRow = Get-RowNode -WorksheetXml $forceWorksheetXml -RowNumber $layout.TotalRow
        Set-TextCell -WorksheetXml $forceWorksheetXml -RowNode $totalRow -CellReference "A$($layout.TotalRow)" -Value "Total" -Style "2"
        Set-FormulaCell -WorksheetXml $forceWorksheetXml -RowNode $totalRow -CellReference "B$($layout.TotalRow)" -Formula $layout.TotalFormula -CachedValue $layout.TotalValue -Style "2"
    }

    $grandTotalRow = Get-RowNode -WorksheetXml $forceWorksheetXml -RowNumber 4
    Set-FormulaCell -WorksheetXml $forceWorksheetXml -RowNode $grandTotalRow -CellReference "D4" -Formula "SUM(B33,B58,B73)" -CachedValue "220" -Style "2"

    $workbookNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    $workbookNamespaceManager = [System.Xml.XmlNamespaceManager]::new($workbookXml.NameTable)
    $workbookNamespaceManager.AddNamespace("d", $workbookNamespace)
    $calcPr = $workbookXml.SelectSingleNode("//d:calcPr", $workbookNamespaceManager)
    if ($null -eq $calcPr) {
        $calcPr = $workbookXml.CreateElement("calcPr", $workbookNamespace)
        [void]$workbookXml.DocumentElement.AppendChild($calcPr)
    }

    $calcPr.SetAttribute("calcMode", "auto")
    $calcPr.SetAttribute("fullCalcOnLoad", "1")
    $calcPr.SetAttribute("forceFullCalc", "1")

    Write-ZipEntryXml -Zip $zip -EntryPath $forceSheetPath -Xml $forceWorksheetXml
    Write-ZipEntryXml -Zip $zip -EntryPath "xl/workbook.xml" -Xml $workbookXml
}
finally {
    $zip.Dispose()
}

$writtenRows |
    Group-Object Style |
    ForEach-Object {
        [pscustomobject]@{
            Style = $_.Name
            Perks = $_.Count
            SP = ($_.Group | Measure-Object Price -Sum).Sum
        }
    } |
    Sort-Object Style |
    Format-Table -AutoSize

Write-Host "Updated Force category moves in '$workbookPath'."
