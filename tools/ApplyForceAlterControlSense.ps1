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

function Get-OpenXmlColumnIndex {
    param([string]$CellReference)

    $letters = ($CellReference -replace "[^A-Z]", "")
    $index = 0
    foreach ($character in $letters.ToCharArray()) {
        $index = ($index * 26) + ([int][char]$character - [int][char]'A' + 1)
    }

    return $index
}

function Get-OpenXmlColumnName {
    param([int]$Index)

    $name = ""
    while ($Index -gt 0) {
        $Index--
        $name = [char]([int][char]'A' + ($Index % 26)) + $name
        $Index = [math]::Floor($Index / 26)
    }

    return $name
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

    $valueNode = $Cell.GetElementsByTagName("v", $namespace)
    if ($valueNode.Count -gt 0) {
        return $valueNode[0].InnerText
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

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return
    }

    $valueElement = $WorksheetXml.CreateElement("v", $namespace)
    $valueElement.InnerText = $Value
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

function Copy-ForceRow {
    param([object]$Source)

    return [ordered]@{
        Style = $Source.Style
        Price = $Source.Price
        PerkName = $Source.PerkName
        SkillRequirements = $Source.SkillRequirements
        CharacterType = $Source.CharacterType
        Type = $Source.Type
        Alignment = $Source.Alignment
        AffinityShift = $Source.AffinityShift
        Description = $Source.Description
        PrimaryStat = $Source.PrimaryStat
        SecondaryStat = $Source.SecondaryStat
        ScalingSource = $Source.ScalingSource
        FP = $Source.FP
        STM = $Source.STM
        CastingTime = $Source.CastingTime
        CooldownTime = $Source.CooldownTime
        DevStatus = $Source.DevStatus
        AdditionalRequirements = $Source.AdditionalRequirements
        Notes = $Source.Notes
    }
}

$workbookPath = Resolve-RepoPath $BibleWorkbookPath
if (!(Test-Path $workbookPath)) {
    throw "Workbook '$workbookPath' was not found."
}

$removedPerks = @(
    "Saber Rend I",
    "Saber Rend II",
    "Mind Shroud I",
    "Mind Shroud II",
    "Soothing Guard I",
    "Bastion of Light",
    "Guardian's Mercy",
    "Consular's Clarity",
    "Merciful Resolve",
    "Force Body I",
    "Force Body II",
    "Dark Bargain I",
    "Dark Bargain II",
    "Shared Suffering",
    "Broken Will"
)

$categoryLayouts = @(
    @{
        Category = "Alter"
        StartRow = 8
        TotalRow = 38
        TotalFormula = "SUM(B8:B37)"
        TotalValue = "107"
        Names = @(
            "Guardian Ward I",
            "Force Spark I",
            "Creeping Terror I",
            "Force Push I",
            "Deflective Presence",
            "Force Choke I",
            "Throw Lightsaber I",
            "Force Lightning I",
            "Force Leap I",
            "Guardian Ward II",
            "Creeping Terror II",
            "Force Spark II",
            "Force Choke II",
            "Force Lightning II",
            "Force Intercept",
            "Force Push II",
            "Force Choke III",
            "Reflective Barrier",
            "Throw Lightsaber II",
            "Guardian Ward III",
            "Force Leap II",
            "Purifying Wave",
            "Creeping Terror III",
            "Ravager's Pressure",
            "Throw Lightsaber III",
            "Guardian Ward IV",
            "Force Push III",
            "Force Spark III",
            "Force Choke IV",
            "Last Stand of the Light"
        )
    },
    @{
        Category = "Control"
        StartRow = 41
        TotalRow = 60
        TotalFormula = "SUM(B41:B59)"
        TotalValue = "69"
        Names = @(
            "Benevolence I",
            "Renewal I",
            "Serene Focus",
            "Benevolence II",
            "Renewal II",
            "Force Mend",
            "Force Sanctuary",
            "Benevolence III",
            "Renewal III",
            "Harmonic Restoration",
            "Force Drain I",
            "Ravager Stance I",
            "Force Drain II",
            "Devouring Strike",
            "Force Drain III",
            "Ravager Stance II",
            "Cruel Momentum",
            "Force Convergence",
            "Hunger of the Dark"
        )
    },
    @{
        Category = "Sense"
        StartRow = 62
        TotalRow = 74
        TotalFormula = "SUM(B62:B73)"
        TotalValue = "44"
        Names = @(
            "Mind Trick I",
            "Precognition",
            "Force Judgment I",
            "Mind Trick II",
            "Courageous Resolve",
            "Force Judgment II",
            "Weaken Resolve I",
            "Force Judgment III",
            "Weaken Resolve II",
            "Nightmare Field",
            "Collapse Will",
            "Eclipse of Resolve"
        )
    }
)

$renameByName = @{
    "Ravager Stance I" = "Fury Stance I"
    "Ravager Stance II" = "Fury Stance II"
    "Ravager's Pressure" = "Unstable Pressure"
}

$skillRequirementOverrides = @{
    "Harmonic Restoration" = "Force 45"
    "Force Convergence" = "Force 48"
}

$descriptionOverrides = @{
    "Deflective Presence" = "Alter powers that grant temporary HP, absorb damage, or prevent defeat grant affected allies +4 Attack Deflection for 10 seconds."
    "Courageous Resolve" = "When you use a Sense power, you and nearby allies gain +10 Fear Resistance rating, +10 Daze Resistance rating, and +10 Confusion Resistance rating for 12 seconds. Allies with temporary HP from one of your Force powers gain +15 instead."
    "Reflective Barrier" = "Alter powers that grant temporary HP reflect 8% of force and energy damage taken, plus WIL scaling, back to the attacker while the temporary HP remains."
    "Devouring Strike" = "Control powers that damage enemies deal 15% more damage to targets below 35% HP."
    "Ravager's Pressure" = "Force Spark and Force Lightning mark affected enemies with unstable pressure for 12 seconds, reducing Evasion by 5%. Enemies below 35% HP also suffer +5% force damage taken while marked."
    "Collapse Will" = "Nightmare Field and Eclipse of Resolve also apply Exposed and Force Erosion for 18 seconds."
}

$notesOverrides = @{
    "Guardian Ward I" = "Opening Alter shield rank."
    "Deflective Presence" = "Alter protection trait placed early so protection support begins before later Ward ranks."
    "Guardian Ward II" = "Second shield rank moved out of the opening band so the Ward line has a real progression."
    "Courageous Resolve" = "Sense resolve trait that provides mental protection without adding another active button."
    "Reflective Barrier" = "Reflect value is lower than dedicated damage bonuses because it rides on Alter protection powers."
    "Guardian Ward III" = "Third shield rank lands in the mid-late tree instead of immediately after Guardian Ward II."
    "Purifying Wave" = "Alter offensive pressure plus minor cleanup."
    "Guardian Ward IV" = "Final regular Ward rank is delayed into the upper tree before the Force 50 capstone."
    "Last Stand of the Light" = "Alter capstone: shared Capstone timer; cooldown includes 45s duration (5:45 total), ignores recast reduction, and has no weapon/tool activation requirement."
    "Force Mend" = "Control cleansing trait for restorative Force powers."
    "Harmonic Restoration" = "Control recovery payoff for sustained healing and restoration play."
    "Ravager Stance I" = "Converted from a timed buff into a Control stance so the tradeoff is intentional and persistent."
    "Ravager Stance II" = "Replacement tier: stronger Control stance benefits without turning the perk into a short-duration attack button."
    "Devouring Strike" = "Execute bonus for Control sustain attacks, kept in line with other low-health damage traits."
    "Ravager's Pressure" = "Alter pressure trait tied to the Force Spark and Force Lightning damage lines."
    "Cruel Momentum" = "Control sustain trait. Hunger of the Dark remains the Control capstone."
    "Force Convergence" = "Control resource trait: provides a recovery window without relying on another specific perk line."
    "Hunger of the Dark" = "Control capstone sustain remains strong but no longer stacks into heavy self-healing loops as aggressively."
    "Collapse Will" = "High-tier Sense debuff setup. Converted from separate active ability to Trait row for the active-button budget. Former active values: FP 9; casting 1 second; cooldown 75 seconds."
    "Eclipse of Resolve" = "Sense capstone debuff remains broad, but its duration and penalties are below Leadership's major command windows."
}

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

    $sourceRowsByName = @{}
    foreach ($rowNode in $forceWorksheetXml.SelectNodes("//d:sheetData/d:row", $namespaceManager)) {
        $rowNumberText = $rowNode.GetAttribute("r")
        if ([string]::IsNullOrWhiteSpace($rowNumberText)) {
            continue
        }

        $rowNumber = [int]$rowNumberText
        if ($rowNumber -le 7) {
            continue
        }

        $source = [ordered]@{ Row = $rowNumber }
        foreach ($entry in $columns.GetEnumerator()) {
            $cellRef = "$($entry.Value)$rowNumber"
            $cell = $forceWorksheetXml.SelectSingleNode("//d:c[@r='$cellRef']", $namespaceManager)
            $source[$entry.Key] = Get-CellText -Cell $cell -SharedStrings $sharedStrings
        }

        if (![string]::IsNullOrWhiteSpace($source.PerkName) -and $source.PerkName -ne "Total") {
            $sourceRowsByName[$source.PerkName] = [pscustomobject]$source
        }
    }

    foreach ($removedPerk in $removedPerks) {
        if (!$sourceRowsByName.ContainsKey($removedPerk)) {
            throw "Expected removed Force perk '$removedPerk' was not found in the workbook."
        }
    }

    foreach ($layout in $categoryLayouts) {
        foreach ($name in $layout.Names) {
            if (!$sourceRowsByName.ContainsKey($name)) {
                throw "Expected Force perk '$name' was not found in the workbook."
            }
        }
    }

    foreach ($rowNumber in 8..92) {
        $rowNode = Get-RowNode -WorksheetXml $forceWorksheetXml -RowNumber $rowNumber
        foreach ($columnName in $columns.Values) {
            $cell = Get-Cell -WorksheetXml $forceWorksheetXml -RowNode $rowNode -CellReference "$columnName$rowNumber"
            Clear-Cell -Cell $cell
        }
    }

    $writtenRows = @()
    foreach ($layout in $categoryLayouts) {
        $rowNumber = [int]$layout.StartRow
        foreach ($name in $layout.Names) {
            $source = Copy-ForceRow -Source $sourceRowsByName[$name]
            $source.Style = $layout.Category
            if ($renameByName.ContainsKey($name)) {
                $source.PerkName = $renameByName[$name]
            }

            if ($skillRequirementOverrides.ContainsKey($name)) {
                $source.SkillRequirements = $skillRequirementOverrides[$name]
            }

            if ($descriptionOverrides.ContainsKey($name)) {
                $source.Description = $descriptionOverrides[$name]
            }

            if ($notesOverrides.ContainsKey($name)) {
                $source.Notes = $notesOverrides[$name]
            }

            $source.Notes = $source.Notes `
                -replace "Light Guardian", "Alter" `
                -replace "Light Consular", "Control" `
                -replace "Dark Ravager", "Control" `
                -replace "Dark Manipulator", "Sense"
            $source.Description = $source.Description `
                -replace "Light Guardian", "Alter" `
                -replace "Light Consular", "Control" `
                -replace "Dark Ravager", "Control" `
                -replace "Dark Manipulator", "Sense" `
                -replace "Ravager Stance", "Fury Stance"

            $rowNode = Get-RowNode -WorksheetXml $forceWorksheetXml -RowNumber $rowNumber
            foreach ($entry in $columns.GetEnumerator()) {
                $cellReference = "$($entry.Value)$rowNumber"
                $value = $source[$entry.Key]
                if ($entry.Key -eq "Price") {
                    Set-NumberCell -WorksheetXml $forceWorksheetXml -RowNode $rowNode -CellReference $cellReference -Value $value -Style "4"
                }
                else {
                    Set-TextCell -WorksheetXml $forceWorksheetXml -RowNode $rowNode -CellReference $cellReference -Value $value -Style "4"
                }
            }

            $writtenRows += [pscustomobject]@{
                Category = $layout.Category
                Row = $rowNumber
                Price = [int]$source.Price
                PerkName = $source.PerkName
            }
            $rowNumber++
        }

        $totalRow = Get-RowNode -WorksheetXml $forceWorksheetXml -RowNumber $layout.TotalRow
        Set-TextCell -WorksheetXml $forceWorksheetXml -RowNode $totalRow -CellReference "A$($layout.TotalRow)" -Value "Total" -Style "2"
        Set-FormulaCell -WorksheetXml $forceWorksheetXml -RowNode $totalRow -CellReference "B$($layout.TotalRow)" -Formula $layout.TotalFormula -CachedValue $layout.TotalValue -Style "2"
    }

    $grandTotalRow = Get-RowNode -WorksheetXml $forceWorksheetXml -RowNumber 4
    Set-FormulaCell -WorksheetXml $forceWorksheetXml -RowNode $grandTotalRow -CellReference "D4" -Formula "SUM(B38,B60,B74)" -CachedValue "220" -Style "2"

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
    Group-Object Category |
    ForEach-Object {
        [pscustomobject]@{
            Category = $_.Name
            Perks = $_.Count
            SP = ($_.Group | Measure-Object Price -Sum).Sum
        }
    } |
    Sort-Object Category |
    Format-Table -AutoSize

[pscustomobject]@{
    RemovedPerks = $removedPerks.Count
    RemovedSP = 55
    ForceTotalSP = 220
} | Format-List

Write-Host "Recategorized Force into Alter, Control, and Sense in '$workbookPath'."
