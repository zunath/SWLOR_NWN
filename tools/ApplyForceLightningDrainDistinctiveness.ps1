param(
    [string]$BibleWorkbookPath = "design/bible/SWLOR Design Bible - Combat Upgrade.xlsx",
    [switch]$ReorderOnly
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

    if ([string]::IsNullOrWhiteSpace($CellReference)) {
        return 0
    }

    $letters = ($CellReference -replace "[^A-Z]", "")
    $index = 0
    foreach ($character in $letters.ToCharArray()) {
        $index = ($index * 26) + ([int][char]$character - [int][char]'A' + 1)
    }

    return $index
}

function Get-CanonicalHeader {
    param([string]$Text)

    $normalized = ($Text -replace "[\s\.\?]+", "").ToLowerInvariant()
    switch ($normalized) {
        "perkname" { return "PerkName" }
        "name" { return "PerkName" }
        "skillreqs" { return "SkillRequirements" }
        "skillrequirements" { return "SkillRequirements" }
        "requirements" { return "SkillRequirements" }
        "type" { return "Type" }
        "description" { return "Description" }
        "notes" { return "Notes" }
        default { return $null }
    }
}

function Get-OpenXmlCellText {
    param(
        [System.Xml.XmlElement]$Cell,
        [System.Collections.Generic.IList[string]]$SharedStrings
    )

    if ($null -eq $Cell) {
        return ""
    }

    $namespaceManager = [System.Xml.XmlNamespaceManager]::new($Cell.OwnerDocument.NameTable)
    $namespaceManager.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")

    $type = $Cell.GetAttribute("t")
    if ($type -eq "inlineStr") {
        $textNode = $Cell.SelectSingleNode("d:is/d:t", $namespaceManager)
        if ($null -eq $textNode) {
            return ""
        }

        return $textNode.InnerText
    }

    $valueNode = $Cell.SelectSingleNode("d:v", $namespaceManager)
    if ($null -eq $valueNode) {
        return ""
    }

    if ($type -eq "s") {
        return $SharedStrings[[int]$valueNode.InnerText]
    }

    return $valueNode.InnerText
}

function Get-WorksheetCell {
    param(
        [System.Xml.XmlElement]$RowNode,
        [int]$ColumnIndex
    )

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    foreach ($cell in $RowNode.GetElementsByTagName("c", $namespace)) {
        if ((Get-OpenXmlColumnIndex $cell.GetAttribute("r")) -eq $ColumnIndex) {
            return $cell
        }
    }

    return $null
}

function Set-CellText {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlElement]$RowNode,
        [int]$ColumnIndex,
        [string]$Text
    )

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    $cell = Get-WorksheetCell -RowNode $RowNode -ColumnIndex $ColumnIndex
    if ($null -eq $cell) {
        $cell = $WorksheetXml.CreateElement("c", $namespace)
        [void]$RowNode.AppendChild($cell)
    }

    while ($cell.HasChildNodes) {
        [void]$cell.RemoveChild($cell.FirstChild)
    }

    $cell.SetAttribute("t", "inlineStr")
    $inlineString = $WorksheetXml.CreateElement("is", $namespace)
    $textElement = $WorksheetXml.CreateElement("t", $namespace)
    $textElement.InnerText = $Text
    [void]$inlineString.AppendChild($textElement)
    [void]$cell.AppendChild($inlineString)
}

$rowUpdatesByName = @{
    "Mind Trick I" = @{
        Description = "Attempts to inflict Confusion on one non-mechanical target for 10 seconds. Caster Willpower increases duration, while target Willpower and Mind Resistance reduce it."
        Notes = "Sense mind-control utility. Confusion mechanics are defined in the Status Effects tab."
    }
    "Mind Trick II" = @{
        Description = "Attempts to inflict Confusion on the selected non-mechanical target and one nearby non-mechanical target for 10 seconds. Caster Willpower increases duration, while target Willpower and Mind Resistance reduce it."
        Notes = "Replacement tier: expands target count and uses the same Willpower contest as rank I. Confusion mechanics are defined in the Status Effects tab."
    }
    "Force Flow" = @{
        PerkName = "Force Convergence"
        Description = "After spending FP on a Force power, restore 20% of your maximum FP over 10 seconds and gain +5% Force ability Accuracy for 10 seconds. This can trigger once every 45 seconds."
        Notes = "Universal capstone replacement: provides a meaningful recovery window without relying on another specific perk line."
    }
    "Guardian's Mercy" = @{
        SkillRequirements = "Force 48"
        Description = "Light Guardian powers that grant temporary HP, absorb damage, cleanse minor negative effects, or prevent defeat also grant +10 Trauma Resistance rating and +5 Guard for 15 seconds. If the ally is below 50% HP, they also recover 5% of maximum HP plus WIL scaling. This can trigger once every 20 seconds per target."
        Notes = "Moved below Force 50 so Last Stand of the Light is the sole Light Guardian capstone; strengthened into a rescue-support trait."
    }
    "Renewal II" = @{
        Description = "Applies regeneration to a single ally, restoring 4% of maximum HP plus WIL scaling every 3 seconds for 18 seconds."
        Notes = "Replacement tier: increased from 3% per tick so the upgrade is more noticeable."
    }
    "Renewal III" = @{
        Description = "Applies regeneration to a single ally, restoring 6% of maximum HP plus WIL scaling every 3 seconds for 18 seconds."
        Notes = "Replacement tier: increased from 4% per tick to create a clear top-rank heal-over-time payoff."
    }
    "Guided Judgment" = @{
        PerkName = "Consular's Clarity"
        Description = "When you restore HP to an ally other than yourself with a Light Consular power, that ally gains +8 Mind Resistance rating and +8 Trauma Resistance rating for 12 seconds. If the ally is below 50% HP, they also gain +5% healing received for 12 seconds."
        Notes = "Replaces Force Judgment-specific boosting with a support trait that fits Light Consular's recovery identity."
    }
    "Judgment Focus" = @{
        PerkName = "Merciful Resolve"
        SkillRequirements = "Force 45"
        Description = "When your Light Consular healing restores an ally other than yourself above 90% HP, grant that ally temporary HP equal to 6% of maximum HP plus WIL scaling for 12 seconds. This can trigger once every 20 seconds per target."
        Notes = "Moved below Force 50 so Harmonic Restoration is the sole Light Consular capstone; replaces Force Judgment-specific damage boosting."
    }
    "Harmonic Restoration" = @{
        Description = "When you restore HP to an ally below 50% HP with a Light Consular power, up to two nearby allies recover 6% of maximum HP plus WIL scaling and gain +10 Trauma Resistance rating for 12 seconds. This can trigger once every 20 seconds."
        Notes = "Light Consular capstone: revised into a stronger group recovery payoff and removed Force Judgment-specific damage boosting."
    }
    "Force Lightning I" = @{
        Description = "Deals 10 force DMG plus WIL scaling to one target, then arcs to up to two nearby enemies for 50% damage. Affected targets suffer Shock for 6 seconds."
        Notes = "Chain pressure line: lower primary damage than Force Drain, but adds multi-target Shock pressure."
    }
    "Force Lightning II" = @{
        Description = "Deals 18 force DMG plus WIL scaling to one target, then arcs to up to three nearby enemies for 50% damage. Affected targets suffer Shock for 8 seconds."
        Notes = "Replacement tier: stronger chain pressure and longer Shock duration, still distinct from Force Drain's single-target sustain."
    }
    "Force Drain I" = @{
        Description = "Deals 14 force DMG plus WIL scaling to one target and heals you for 30% of damage dealt. If the target is below 50% HP, healing increases to 40%."
        Notes = "Single-target life siphon line: sustain-focused rather than chain damage."
    }
    "Force Drain II" = @{
        Description = "Deals 24 force DMG plus WIL scaling to one target and heals you for 35% of damage dealt. If the target is below 50% HP, healing increases to 45%."
        Notes = "Replacement tier: improves single-target siphon damage and low-health sustain."
    }
    "Force Drain III" = @{
        Description = "Deals 36 force DMG plus WIL scaling to one target and heals you for 40% of damage dealt. If the target is below 50% HP, healing increases to 50%."
        Notes = "Replacement tier: top life siphon rank remains single-target so it does not overlap with Force Lightning's chain role."
    }
    "Fury Stance I" = @{
        PerkName = "Ravager Stance I"
        Type = "Stance"
        Description = "While active, gain +8% weapon and force damage and +10% critical damage, but take 5% more damage and suffer -5% Defense and Force Defense. Only one stance may be active."
        Notes = "Converted from a timed buff into a stance so the tradeoff is intentional and persistent."
    }
    "Force Maelstrom" = @{
        PerkName = "Ravager's Pressure"
        SkillRequirements = "Force 35"
        Description = "Damaging Dark Ravager powers mark affected enemies with unstable pressure for 12 seconds, reducing Evasion by 5%. Enemies below 35% HP also suffer +5% force damage taken while marked."
        Notes = "Replaces a Force Lightning/Hunger-specific booster with a broader Dark Ravager pressure trait."
    }
    "Fury Stance II" = @{
        PerkName = "Ravager Stance II"
        Type = "Stance"
        Description = "While active, gain +12% weapon and force damage and +15% critical damage, but take 5% more damage and suffer -5% Defense and Force Defense. Only one stance may be active."
        Notes = "Replacement tier: stronger stance benefits without turning the perk into a short-duration attack button."
    }
    "Overflowing Hunger" = @{
        PerkName = "Cruel Momentum"
        SkillRequirements = "Force 45"
        Description = "When an enemy you damaged within the last 6 seconds is defeated, restore 2 FP and gain +5% Force ability Accuracy for 10 seconds. This can trigger once every 10 seconds."
        Notes = "Moved below Force 50 so Hunger of the Dark is the sole Dark Ravager capstone; replaces the narrow overheal trigger."
    }
    "Nightmare Field" = @{
        Description = "Nearby enemies suffer -10% physical and Force ability Accuracy and -10% Evasion for 18 seconds."
        Notes = "Uses percentages instead of raw Accuracy and Evasion numbers so the debuff scales with the rest of the combat upgrade."
    }
    "Dread Certainty" = @{
        PerkName = "Broken Will"
        SkillRequirements = "Force 45"
        Description = "Enemies that fail a save against one of your Force powers suffer +10% FP and STM costs and -5% outgoing weapon and force damage for 12 seconds. This can trigger once every 12 seconds per target."
        Notes = "Moved below Force 50 so Eclipse of Resolve is the sole Dark Manipulator capstone; replaces the broad perk-line booster with a standalone pressure trait."
    }
}

$workbookPath = Resolve-RepoPath $BibleWorkbookPath
if (!(Test-Path $workbookPath)) {
    throw "Workbook '$workbookPath' was not found."
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::Open($workbookPath, [System.IO.Compression.ZipArchiveMode]::Update)

try {
    $sharedStrings = [System.Collections.Generic.List[string]]::new()
    if ($null -ne $zip.GetEntry("xl/sharedStrings.xml")) {
        [xml]$sharedStringsXml = Read-ZipEntryText -Zip $zip -EntryPath "xl/sharedStrings.xml"
        $sharedStringsNamespace = [System.Xml.XmlNamespaceManager]::new($sharedStringsXml.NameTable)
        $sharedStringsNamespace.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")
        foreach ($sharedStringNode in $sharedStringsXml.SelectNodes("//d:si", $sharedStringsNamespace)) {
            $sharedStrings.Add(($sharedStringNode.SelectNodes(".//d:t", $sharedStringsNamespace) | ForEach-Object { $_.InnerText }) -join "") | Out-Null
        }
    }

    [xml]$workbookXml = Read-ZipEntryText -Zip $zip -EntryPath "xl/workbook.xml"
    [xml]$relationshipsXml = Read-ZipEntryText -Zip $zip -EntryPath "xl/_rels/workbook.xml.rels"

    $relationshipsById = @{}
    foreach ($relationship in $relationshipsXml.Relationships.Relationship) {
        $relationshipsById[$relationship.Id] = "xl/" + $relationship.Target.TrimStart("/")
    }

    $workbookNamespace = [System.Xml.XmlNamespaceManager]::new($workbookXml.NameTable)
    $workbookNamespace.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")
    $workbookNamespace.AddNamespace("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")

    $forceSheetPath = $null
    foreach ($sheet in $workbookXml.SelectNodes("//d:sheets/d:sheet", $workbookNamespace)) {
        if ($sheet.GetAttribute("name") -ne "Force") {
            continue
        }

        $relationshipId = $sheet.GetAttribute("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")
        $forceSheetPath = $relationshipsById[$relationshipId]
        break
    }

    if ([string]::IsNullOrWhiteSpace($forceSheetPath)) {
        throw "Workbook sheet 'Force' was not found."
    }

    [xml]$worksheetXml = Read-ZipEntryText -Zip $zip -EntryPath $forceSheetPath
    $worksheetNamespace = [System.Xml.XmlNamespaceManager]::new($worksheetXml.NameTable)
    $worksheetNamespace.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")

    $columnByHeader = @{}
    $headerColumnIndexes = @()
    $headerRowNumber = 0
    foreach ($rowNode in $worksheetXml.SelectNodes("//d:sheetData/d:row", $worksheetNamespace)) {
        $rowNumberText = $rowNode.GetAttribute("r")
        if ([string]::IsNullOrWhiteSpace($rowNumberText)) {
            continue
        }

        $cells = @{}
        foreach ($cell in $rowNode.SelectNodes("d:c", $worksheetNamespace)) {
            $columnIndex = Get-OpenXmlColumnIndex $cell.GetAttribute("r")
            if ($columnIndex -gt 0) {
                $cells[$columnIndex] = Get-OpenXmlCellText -Cell $cell -SharedStrings $sharedStrings
            }
        }

        if (($cells.Values -join "|") -match "Perk Name|PerkName") {
            $headerRowNumber = [int]$rowNumberText
            $headerColumnIndexes = @($cells.Keys | Sort-Object)
            foreach ($cellEntry in $cells.GetEnumerator()) {
                $canonicalHeader = Get-CanonicalHeader $cellEntry.Value
                if (![string]::IsNullOrWhiteSpace($canonicalHeader) -and !$columnByHeader.ContainsKey($canonicalHeader)) {
                    $columnByHeader[$canonicalHeader] = $cellEntry.Key
                }
            }
            break
        }
    }

    $requiredHeaders = [System.Collections.Generic.HashSet[string]]::new()
    [void]$requiredHeaders.Add("PerkName")
    if (!$ReorderOnly) {
        foreach ($update in $rowUpdatesByName.Values) {
            foreach ($updateColumn in $update.Keys) {
                [void]$requiredHeaders.Add($updateColumn)
            }
        }
    }

    foreach ($requiredHeader in $requiredHeaders) {
        if (!$columnByHeader.ContainsKey($requiredHeader)) {
            throw "Workbook sheet 'Force' is missing required column '$requiredHeader'."
        }
    }

    $updatedRows = [System.Collections.Generic.List[object]]::new()
    if (!$ReorderOnly) {
        foreach ($rowNode in $worksheetXml.SelectNodes("//d:sheetData/d:row", $worksheetNamespace)) {
            $rowNumberText = $rowNode.GetAttribute("r")
            if ([string]::IsNullOrWhiteSpace($rowNumberText) -or [int]$rowNumberText -le $headerRowNumber) {
                continue
            }

            $perkNameCell = Get-WorksheetCell -RowNode $rowNode -ColumnIndex $columnByHeader["PerkName"]
            $perkName = Get-OpenXmlCellText -Cell $perkNameCell -SharedStrings $sharedStrings
            if (!$rowUpdatesByName.ContainsKey($perkName)) {
                continue
            }

            $update = $rowUpdatesByName[$perkName]
            foreach ($updateColumn in $update.Keys) {
                Set-CellText -WorksheetXml $worksheetXml -RowNode $rowNode -ColumnIndex $columnByHeader[$updateColumn] -Text $update[$updateColumn]
            }

            $newPerkName = $perkName
            if ($update.ContainsKey("PerkName")) {
                $newPerkName = $update["PerkName"]
            }

            $updatedRows.Add([pscustomobject]@{ Row = [int]$rowNumberText; PerkName = $perkName; NewPerkName = $newPerkName }) | Out-Null
        }

        $missingNames = $rowUpdatesByName.Keys | Where-Object { $name = $_; -not ($updatedRows | Where-Object { $_.PerkName -eq $name }) }
        if ($missingNames) {
            throw "Did not find expected Force perks: $($missingNames -join ', ')"
        }
    }

    function Get-RowNodeByNumber {
        param([int]$RowNumber)

        return $worksheetXml.SelectSingleNode("//d:sheetData/d:row[@r='$RowNumber']", $worksheetNamespace)
    }

    function Get-PerkNameAtRow {
        param([int]$RowNumber)

        $rowNode = Get-RowNodeByNumber -RowNumber $RowNumber
        if ($null -eq $rowNode) {
            return ""
        }

        $cell = Get-WorksheetCell -RowNode $rowNode -ColumnIndex $columnByHeader["PerkName"]
        return Get-OpenXmlCellText -Cell $cell -SharedStrings $sharedStrings
    }

    function Get-RowTextByColumn {
        param([int]$RowNumber)

        $rowNode = Get-RowNodeByNumber -RowNumber $RowNumber
        if ($null -eq $rowNode) {
            throw "Workbook sheet 'Force' is missing row '$RowNumber'."
        }

        $values = @{}
        foreach ($columnIndex in $headerColumnIndexes) {
            $cell = Get-WorksheetCell -RowNode $rowNode -ColumnIndex $columnIndex
            $values[$columnIndex] = Get-OpenXmlCellText -Cell $cell -SharedStrings $sharedStrings
        }

        return $values
    }

    function Set-RowTextByColumn {
        param(
            [int]$RowNumber,
            [hashtable]$ValuesByColumn
        )

        $rowNode = Get-RowNodeByNumber -RowNumber $RowNumber
        if ($null -eq $rowNode) {
            throw "Workbook sheet 'Force' is missing row '$RowNumber'."
        }

        foreach ($columnIndex in $headerColumnIndexes) {
            $value = ""
            if ($ValuesByColumn.ContainsKey($columnIndex)) {
                $value = $ValuesByColumn[$columnIndex]
            }

            Set-CellText -WorksheetXml $worksheetXml -RowNode $rowNode -ColumnIndex $columnIndex -Text $value
        }
    }

    function Apply-RowReorder {
        param(
            [string]$Name,
            [hashtable]$ExpectedNameByRow,
            [hashtable]$SourceRowByTargetRow
        )

        foreach ($entry in $ExpectedNameByRow.GetEnumerator()) {
            $currentName = Get-PerkNameAtRow -RowNumber ([int]$entry.Key)
            if ($currentName -ne $entry.Value) {
                return $false
            }
        }

        $sourceValuesByRow = @{}
        foreach ($sourceRow in ($SourceRowByTargetRow.Values | Sort-Object -Unique)) {
            $sourceValuesByRow[[int]$sourceRow] = Get-RowTextByColumn -RowNumber ([int]$sourceRow)
        }

        foreach ($entry in $SourceRowByTargetRow.GetEnumerator()) {
            Set-RowTextByColumn -RowNumber ([int]$entry.Key) -ValuesByColumn $sourceValuesByRow[[int]$entry.Value]
        }

        return $true
    }

    $reorderedRows = [System.Collections.Generic.List[object]]::new()
    $rowReorders = @(
        @{
            Name = "Light Guardian Force 48/50 order"
            Expected = @{ 37 = "Last Stand of the Light"; 38 = "Guardian's Mercy" }
            Mapping = @{ 37 = 38; 38 = 37 }
        },
        @{
            Name = "Light Consular Force 45/48 order"
            Expected = @{ 53 = "Force Judgment III"; 54 = "Merciful Resolve" }
            Mapping = @{ 53 = 54; 54 = 53 }
        },
        @{
            Name = "Dark Ravager Force 45/48/50 order"
            Expected = @{ 71 = "Force Spark III"; 72 = "Hunger of the Dark"; 73 = "Cruel Momentum" }
            Mapping = @{ 71 = 73; 72 = 71; 73 = 72 }
        },
        @{
            Name = "Dark Manipulator Force 45/48/50 order"
            Expected = @{ 89 = "Force Choke IV"; 90 = "Eclipse of Resolve"; 91 = "Broken Will" }
            Mapping = @{ 89 = 91; 90 = 89; 91 = 90 }
        }
    )

    foreach ($rowReorder in $rowReorders) {
        if (Apply-RowReorder -Name $rowReorder.Name -ExpectedNameByRow $rowReorder.Expected -SourceRowByTargetRow $rowReorder.Mapping) {
            $reorderedRows.Add([pscustomobject]@{ Reorder = $rowReorder.Name }) | Out-Null
        }
    }

    Write-ZipEntryXml -Zip $zip -EntryPath $forceSheetPath -Xml $worksheetXml
}
finally {
    $zip.Dispose()
}

$updatedRows | Sort-Object Row | Format-Table -AutoSize
$reorderedRows | Format-Table -AutoSize
Write-Host "Updated targeted Force design feedback in '$workbookPath'."
