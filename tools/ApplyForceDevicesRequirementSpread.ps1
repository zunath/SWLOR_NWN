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

function New-RequirementRow {
    param(
        [string]$Tab,
        [int]$Row,
        [string]$PerkName,
        [string]$SkillRequirements
    )

    return [pscustomobject]@{
        Tab = $Tab
        Row = $Row
        PerkName = $PerkName
        SkillRequirements = $SkillRequirements
    }
}

function Add-RequirementRows {
    param(
        [System.Collections.Generic.List[object]]$Rows,
        [string]$Tab,
        [int[]]$WorkbookRows,
        [string[]]$PerkNames,
        [string[]]$SkillRequirements
    )

    if ($WorkbookRows.Count -ne $PerkNames.Count -or $PerkNames.Count -ne $SkillRequirements.Count) {
        throw "Requirement row input length mismatch for '$Tab'."
    }

    for ($index = 0; $index -lt $WorkbookRows.Count; $index++) {
        $Rows.Add((New-RequirementRow -Tab $Tab -Row $WorkbookRows[$index] -PerkName $PerkNames[$index] -SkillRequirements $SkillRequirements[$index])) | Out-Null
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

function Apply-RequirementRows {
    param(
        [xml]$WorksheetXml,
        [System.Collections.Generic.IList[string]]$SharedStrings,
        [object[]]$ExpectedRows,
        [string]$Tab
    )

    $worksheetNamespace = [System.Xml.XmlNamespaceManager]::new($WorksheetXml.NameTable)
    $worksheetNamespace.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")

    $columnByHeader = @{}
    $headerRowNumber = 0
    foreach ($rowNode in $WorksheetXml.SelectNodes("//d:sheetData/d:row", $worksheetNamespace)) {
        $rowNumberText = $rowNode.GetAttribute("r")
        if ([string]::IsNullOrWhiteSpace($rowNumberText)) {
            continue
        }

        $cells = @{}
        foreach ($cell in $rowNode.SelectNodes("d:c", $worksheetNamespace)) {
            $columnIndex = Get-OpenXmlColumnIndex $cell.GetAttribute("r")
            if ($columnIndex -gt 0) {
                $cells[$columnIndex] = Get-OpenXmlCellText -Cell $cell -SharedStrings $SharedStrings
            }
        }

        if (($cells.Values -join "|") -match "Perk Name|PerkName") {
            $headerRowNumber = [int]$rowNumberText
            foreach ($cellEntry in $cells.GetEnumerator()) {
                $canonicalHeader = Get-CanonicalHeader $cellEntry.Value
                if (![string]::IsNullOrWhiteSpace($canonicalHeader) -and !$columnByHeader.ContainsKey($canonicalHeader)) {
                    $columnByHeader[$canonicalHeader] = $cellEntry.Key
                }
            }
            break
        }
    }

    foreach ($requiredHeader in @("PerkName", "SkillRequirements")) {
        if (!$columnByHeader.ContainsKey($requiredHeader)) {
            throw "Workbook sheet '$Tab' is missing required column '$requiredHeader'."
        }
    }

    $expectedByRow = @{}
    foreach ($expectedRow in $ExpectedRows) {
        $expectedByRow[[int]$expectedRow.Row] = $expectedRow
    }

    $updatedRows = [System.Collections.Generic.List[object]]::new()
    $validatedRows = [System.Collections.Generic.List[object]]::new()
    foreach ($rowNode in $WorksheetXml.SelectNodes("//d:sheetData/d:row", $worksheetNamespace)) {
        $rowNumberText = $rowNode.GetAttribute("r")
        if ([string]::IsNullOrWhiteSpace($rowNumberText) -or [int]$rowNumberText -le $headerRowNumber) {
            continue
        }

        $rowNumber = [int]$rowNumberText
        if (!$expectedByRow.ContainsKey($rowNumber)) {
            continue
        }

        $expectedRow = $expectedByRow[$rowNumber]
        $perkNameCell = Get-WorksheetCell -RowNode $rowNode -ColumnIndex $columnByHeader["PerkName"]
        $actualPerkName = Get-OpenXmlCellText -Cell $perkNameCell -SharedStrings $SharedStrings
        if ($actualPerkName -ne $expectedRow.PerkName) {
            throw "Workbook sheet '$Tab' row '$rowNumber' expected '$($expectedRow.PerkName)' but found '$actualPerkName'."
        }

        $skillRequirementsCell = Get-WorksheetCell -RowNode $rowNode -ColumnIndex $columnByHeader["SkillRequirements"]
        $actualSkillRequirements = Get-OpenXmlCellText -Cell $skillRequirementsCell -SharedStrings $SharedStrings
        if ($actualSkillRequirements -ne $expectedRow.SkillRequirements) {
            Set-CellText -WorksheetXml $WorksheetXml -RowNode $rowNode -ColumnIndex $columnByHeader["SkillRequirements"] -Text $expectedRow.SkillRequirements
            $updatedRows.Add([pscustomobject]@{
                Tab = $Tab
                Row = $rowNumber
                PerkName = $actualPerkName
                OldSkillRequirements = $actualSkillRequirements
                NewSkillRequirements = $expectedRow.SkillRequirements
            }) | Out-Null
        }

        $validatedRows.Add([pscustomobject]@{
            Tab = $Tab
            Row = $rowNumber
            PerkName = $actualPerkName
            SkillRequirements = $expectedRow.SkillRequirements
        }) | Out-Null
    }

    $missingRows = $ExpectedRows | Where-Object {
        $expectedRow = $_
        -not ($validatedRows | Where-Object { $_.Row -eq $expectedRow.Row -and $_.PerkName -eq $expectedRow.PerkName })
    }
    if ($missingRows) {
        throw "Workbook sheet '$Tab' did not validate expected rows: $($missingRows | ForEach-Object { "$($_.Row):$($_.PerkName)" } -join ', ')"
    }

    return [pscustomobject]@{
        UpdatedRows = $updatedRows
        ValidatedRows = $validatedRows
    }
}

$targetRows = [System.Collections.Generic.List[object]]::new()

Add-RequirementRows -Rows $targetRows -Tab "Force" -WorkbookRows @(8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23) -PerkNames @("Force Push I","Throw Lightsaber I","Force Leap I","Mind Trick I","Saber Rend I","Mind Shroud I","Precognition","Force Push II","Throw Lightsaber II","Force Leap II","Mind Trick II","Saber Rend II","Mind Shroud II","Throw Lightsaber III","Force Push III","Force Convergence") -SkillRequirements @("Force 5","Force 8","Force 12","Force 15","Force 18","Force 22","Force 25","Force 28","Force 30","Force 35","Force 38","Force 40","Force 42","Force 45","Force 48","Force 50")
Add-RequirementRows -Rows $targetRows -Tab "Force" -WorkbookRows @(26,27,28,29,30,31,32,33,34,35,36,37,38) -PerkNames @("Guardian Ward I","Deflective Presence","Soothing Guard I","Guardian Ward II","Courageous Resolve","Force Intercept","Reflective Barrier","Guardian Ward III","Purifying Wave","Bastion of Light","Guardian Ward IV","Guardian's Mercy","Last Stand of the Light") -SkillRequirements @("-","Force 5","Force 10","Force 15","Force 20","Force 25","Force 30","Force 35","Force 38","Force 42","Force 45","Force 48","Force 50")
Add-RequirementRows -Rows $targetRows -Tab "Force" -WorkbookRows @(41,42,43,44,45,46,47,48,49,50,51,52,53,54,55) -PerkNames @("Benevolence I","Force Judgment I","Renewal I","Serene Focus","Benevolence II","Renewal II","Force Judgment II","Force Mend","Consular's Clarity","Force Sanctuary","Benevolence III","Renewal III","Merciful Resolve","Force Judgment III","Harmonic Restoration") -SkillRequirements @("-","Force 5","Force 8","Force 12","Force 15","Force 20","Force 25","Force 30","Force 35","Force 38","Force 40","Force 42","Force 45","Force 48","Force 50")
Add-RequirementRows -Rows $targetRows -Tab "Force" -WorkbookRows @(58,59,60,61,62,63,64,65,66,67,68,69,70,71,72,73) -PerkNames @("Force Spark I","Force Body I","Force Lightning I","Force Drain I","Ravager Stance I","Force Spark II","Force Lightning II","Force Drain II","Devouring Strike","Force Body II","Ravager's Pressure","Force Drain III","Ravager Stance II","Cruel Momentum","Force Spark III","Hunger of the Dark") -SkillRequirements @("-","Force 5","Force 8","Force 12","Force 15","Force 18","Force 22","Force 25","Force 28","Force 30","Force 35","Force 38","Force 42","Force 45","Force 48","Force 50")
Add-RequirementRows -Rows $targetRows -Tab "Force" -WorkbookRows @(76,77,78,79,80,81,82,83,84,85,86,87,88,89,90,91) -PerkNames @("Creeping Terror I","Force Choke I","Weaken Resolve I","Dark Bargain I","Creeping Terror II","Force Choke II","Nightmare Field","Weaken Resolve II","Force Choke III","Dark Bargain II","Shared Suffering","Creeping Terror III","Collapse Will","Broken Will","Force Choke IV","Eclipse of Resolve") -SkillRequirements @("-","Force 5","Force 8","Force 12","Force 15","Force 18","Force 22","Force 25","Force 28","Force 30","Force 35","Force 38","Force 42","Force 45","Force 48","Force 50")

Add-RequirementRows -Rows $targetRows -Tab "Devices" -WorkbookRows @(9,10,11,12,13,14,15,16,17,18,19,20) -PerkNames @("Integrated Toolkit I","Deploy Cover","Signal Jammer","Integrated Toolkit II","Disruption Pulse","Diagnostic Sweep","Power Surge","Integrated Toolkit III","Adaptive Circuits","Overclock Routine","Tactical Uplink","Emergency Override") -SkillRequirements @("Devices 5","Devices 8","Devices 12","Devices 15","Devices 18","Devices 22","Devices 25","Devices 30","Devices 35","Devices 40","Devices 45","Devices 50")
Add-RequirementRows -Rows $targetRows -Tab "Devices" -WorkbookRows @(24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40) -PerkNames @("Frag Grenade I","Blast Radius I","Concussion Grenade I","Flash Grenade I","Frag Grenade II","Ion Grenade I","Blast Radius II","Adhesive Grenade I","Concussion Grenade II","Cluster Grenade","Flash Grenade II","Ion Grenade II","Frag Grenade III","Adhesive Grenade II","Blast Radius III","Concussion Grenade III","Thermal Detonator") -SkillRequirements @("-","Devices 5","Devices 8","Devices 12","Devices 15","Devices 18","Devices 22","Devices 25","Devices 28","Devices 30","Devices 35","Devices 38","Devices 40","Devices 42","Devices 45","Devices 48","Devices 50")
Add-RequirementRows -Rows $targetRows -Tab "Devices" -WorkbookRows @(44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60) -PerkNames @("Blaster Beacon I","Beacon Targeting I","Incendiary Field I","Remote Charge I","Blaster Beacon II","Pulse Relay I","Beacon Targeting II","Shock Beacon I","Incendiary Field II","Remote Charge II","Blaster Beacon III","Pulse Relay II","Shock Beacon II","Incendiary Field III","Beacon Targeting III","Remote Charge III","Killzone Beacon") -SkillRequirements @("-","Devices 5","Devices 8","Devices 12","Devices 15","Devices 18","Devices 22","Devices 25","Devices 28","Devices 30","Devices 35","Devices 38","Devices 40","Devices 42","Devices 45","Devices 48","Devices 50")
Add-RequirementRows -Rows $targetRows -Tab "Devices" -WorkbookRows @(64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,80) -PerkNames @("Deflector Shield I","Capacitor Rig I","Weapon Jam I","Power Cell I","Deflector Shield II","Rayshield Screen I","Capacitor Rig II","Dampening Field I","Weapon Jam II","Power Cell II","Deflector Shield III","Rayshield Screen II","Dampening Field II","Group Deflector","Capacitor Rig III","Power Cell III","Emergency Bunker") -SkillRequirements @("-","Devices 5","Devices 8","Devices 12","Devices 15","Devices 18","Devices 22","Devices 25","Devices 28","Devices 30","Devices 35","Devices 38","Devices 40","Devices 42","Devices 45","Devices 48","Devices 50")
Add-RequirementRows -Rows $targetRows -Tab "Devices" -WorkbookRows @(84,85,86,87,88,89,90,91,92,93,94,95,96,97,98,99,100) -PerkNames @("Flamethrower I","Wrist Rocket I","Sonic Burst I","Gadget Harness I","Flamethrower II","Rail Dart I","Gadget Harness II","Wrist Rocket II","Sonic Burst II","Cryo Sprayer I","Flamethrower III","Rail Dart II","Wrist Rocket III","Sonic Burst III","Gadget Harness III","Cryo Sprayer II","Overload Barrage") -SkillRequirements @("-","Devices 5","Devices 8","Devices 12","Devices 15","Devices 18","Devices 22","Devices 25","Devices 28","Devices 30","Devices 35","Devices 38","Devices 40","Devices 42","Devices 45","Devices 48","Devices 50")

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

    $updatedRows = [System.Collections.Generic.List[object]]::new()
    $validatedRows = [System.Collections.Generic.List[object]]::new()
    foreach ($tab in @("Force", "Devices")) {
        $sheetPath = Get-WorksheetPath -WorkbookXml $workbookXml -RelationshipsById $relationshipsById -SheetName $tab
        [xml]$worksheetXml = Read-ZipEntryText -Zip $zip -EntryPath $sheetPath
        $result = Apply-RequirementRows -WorksheetXml $worksheetXml -SharedStrings $sharedStrings -ExpectedRows @($targetRows | Where-Object { $_.Tab -eq $tab }) -Tab $tab
        foreach ($updatedRow in $result.UpdatedRows) {
            $updatedRows.Add($updatedRow) | Out-Null
        }
        foreach ($validatedRow in $result.ValidatedRows) {
            $validatedRows.Add($validatedRow) | Out-Null
        }

        Write-ZipEntryXml -Zip $zip -EntryPath $sheetPath -Xml $worksheetXml
    }
}
finally {
    $zip.Dispose()
}

if ($updatedRows.Count -gt 0) {
    $updatedRows | Sort-Object Tab, Row | Format-Table -AutoSize
}
else {
    Write-Host "No requirement values needed changes."
}

$validatedRows |
    Group-Object Tab |
    Select-Object Name, Count |
    Format-Table -AutoSize

Write-Host "Validated Force and Devices requirement spread in '$workbookPath'."
