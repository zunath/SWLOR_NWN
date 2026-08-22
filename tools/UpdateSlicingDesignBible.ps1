[CmdletBinding()]
param(
    [string]$WorkbookPath = "design\bible\SWLOR Design Bible - Combat Upgrade.xlsx"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$workbookFullPath = if ([IO.Path]::IsPathRooted($WorkbookPath)) { $WorkbookPath } else { Join-Path $repoRoot $WorkbookPath }
$payloadPath = Join-Path ([IO.Path]::GetTempPath()) ("swlor-slicing-sheet-{0}.xml" -f [guid]::NewGuid().ToString("N"))
$recipePayloadPath = Join-Path ([IO.Path]::GetTempPath()) ("swlor-slicing-recipes-{0}.json" -f [guid]::NewGuid().ToString("N"))

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Read-ZipEntryBytes {
    param([IO.Compression.ZipArchive]$Zip, [string]$EntryPath)
    $entry = $Zip.GetEntry($EntryPath)
    if ($null -eq $entry) { throw "Workbook entry '$EntryPath' was not found." }
    $stream = $entry.Open()
    try {
        $memory = [IO.MemoryStream]::new()
        try { $stream.CopyTo($memory); return $memory.ToArray() }
        finally { $memory.Dispose() }
    }
    finally { $stream.Dispose() }
}

function Read-ZipEntryText {
    param([IO.Compression.ZipArchive]$Zip, [string]$EntryPath)
    return [Text.Encoding]::UTF8.GetString((Read-ZipEntryBytes -Zip $Zip -EntryPath $EntryPath))
}

function Repair-InvalidXmlSpacePrefixes {
    param([Parameter(Mandatory)][string]$Text)

    return [regex]::Replace(
        $Text,
        '\s+d\d+p\d+:space="preserve"\s+xmlns:d\d+p\d+="http://www\.w3\.org/XML/1998/namespace"',
        ' xml:space="preserve"')
}

function Write-ZipEntryText {
    param([IO.Compression.ZipArchive]$Zip, [string]$EntryPath, [string]$Text)
    $existing = $Zip.GetEntry($EntryPath)
    if ($null -ne $existing) { $existing.Delete() }
    $entry = $Zip.CreateEntry($EntryPath, [IO.Compression.CompressionLevel]::Optimal)
    $stream = $entry.Open()
    try {
        $bytes = [Text.UTF8Encoding]::new($false).GetBytes($Text)
        $stream.Write($bytes, 0, $bytes.Length)
    }
    finally { $stream.Dispose() }
}

function Get-EntryHashes {
    param([string]$Path, [Collections.Generic.HashSet[string]]$Excluded)
    $result = @{}
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $zip = [IO.Compression.ZipFile]::OpenRead($Path)
        try {
            foreach ($entry in $zip.Entries) {
                if ($Excluded.Contains($entry.FullName)) { continue }
                $bytes = Read-ZipEntryBytes -Zip $zip -EntryPath $entry.FullName
                $result[$entry.FullName] = ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace("-", "")
            }
        }
        finally { $zip.Dispose() }
    }
    finally { $sha.Dispose() }
    return $result
}

$touchedEntries = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($entryPath in @("xl/workbook.xml", "xl/_rels/workbook.xml.rels", "[Content_Types].xml", "xl/worksheets/sheet70.xml")) {
    [void]$touchedEntries.Add($entryPath)
}
foreach ($entryPath in @("xl/worksheets/sheet30.xml", "xl/worksheets/sheet37.xml", "xl/worksheets/sheet38.xml", "xl/worksheets/sheet39.xml", "xl/worksheets/sheet40.xml")) {
    [void]$touchedEntries.Add($entryPath)
}

function Set-CellValue {
    param(
        [xml]$Document,
        [Xml.XmlElement]$Cell,
        [object]$Value,
        [int]$Style
    )
    while ($Cell.HasChildNodes) { [void]$Cell.RemoveChild($Cell.FirstChild) }
    $Cell.SetAttribute("s", [string]$Style)
    $namespaceUri = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    if ($Value -is [byte] -or $Value -is [int16] -or $Value -is [int32] -or $Value -is [int64] -or
        $Value -is [single] -or $Value -is [double] -or $Value -is [decimal]) {
        $Cell.RemoveAttribute("t")
        $valueNode = $Document.CreateElement("v", $namespaceUri)
        $valueNode.InnerText = [Convert]::ToString($Value, [Globalization.CultureInfo]::InvariantCulture)
        [void]$Cell.AppendChild($valueNode)
    }
    else {
        $Cell.SetAttribute("t", "inlineStr")
        $inline = $Document.CreateElement("is", $namespaceUri)
        $textNode = $Document.CreateElement("t", $namespaceUri)
        [void]$textNode.SetAttribute("xml:space", "preserve")
        $textNode.InnerText = [string]$Value
        [void]$inline.AppendChild($textNode)
        [void]$Cell.AppendChild($inline)
    }
}

function Set-WorksheetRecipeRows {
    param(
        [IO.Compression.ZipArchive]$Zip,
        [string]$EntryPath,
        [object[]]$Rows
    )
    [xml]$document = Repair-InvalidXmlSpacePrefixes -Text (Read-ZipEntryText -Zip $Zip -EntryPath $EntryPath)
    $namespaceUri = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    $namespace = [Xml.XmlNamespaceManager]::new($document.NameTable)
    $namespace.AddNamespace("d", $namespaceUri)
    $sheetData = $document.SelectSingleNode("/d:worksheet/d:sheetData", $namespace)
    $maximumRow = 1
    foreach ($spec in $Rows) {
        $rowNumber = [int]$spec.row
        $maximumRow = [Math]::Max($maximumRow, $rowNumber)
        $row = $sheetData.SelectSingleNode("d:row[@r='$rowNumber']", $namespace)
        if ($null -eq $row) {
            $row = $document.CreateElement("row", $namespaceUri)
            $row.SetAttribute("r", [string]$rowNumber)
            $nextRow = @($sheetData.SelectNodes("d:row", $namespace)) |
                Where-Object { [int]$_.GetAttribute("r") -gt $rowNumber } |
                Select-Object -First 1
            if ($null -ne $nextRow) { [void]$sheetData.InsertBefore($row, $nextRow) }
            else { [void]$sheetData.AppendChild($row) }
        }
        else {
            while ($row.HasChildNodes) { [void]$row.RemoveChild($row.FirstChild) }
        }

        foreach ($property in $spec.cells.PSObject.Properties) {
            $cell = $document.CreateElement("c", $namespaceUri)
            $cell.SetAttribute("r", "$($property.Name)$rowNumber")
            Set-CellValue -Document $document -Cell $cell -Value $property.Value -Style ([int]$spec.style)
            [void]$row.AppendChild($cell)
        }
    }

    $dimension = $document.SelectSingleNode("/d:worksheet/d:dimension", $namespace)
    if ($null -ne $dimension -and $dimension.GetAttribute("ref") -match "^([A-Z]+\d+):([A-Z]+)(\d+)$") {
        $existingMaximum = [int]$Matches[3]
        if ($maximumRow -gt $existingMaximum) {
            $dimension.SetAttribute("ref", "$($Matches[1]):$($Matches[2])$maximumRow")
        }
    }
    Write-ZipEntryText -Zip $Zip -EntryPath $EntryPath -Text $document.OuterXml
}

function Set-EspionageDescriptions {
    param([IO.Compression.ZipArchive]$Zip, [object]$Descriptions)
    $entryPath = "xl/worksheets/sheet30.xml"
    [xml]$document = Repair-InvalidXmlSpacePrefixes -Text (Read-ZipEntryText -Zip $Zip -EntryPath $entryPath)
    $namespaceUri = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    $namespace = [Xml.XmlNamespaceManager]::new($document.NameTable)
    $namespace.AddNamespace("d", $namespaceUri)
    foreach ($property in $Descriptions.PSObject.Properties) {
        $cell = $document.SelectSingleNode("//d:c[@r='$($property.Name)']", $namespace)
        if ($null -eq $cell) { throw "Espionage cell '$($property.Name)' was not found." }
        $style = if ($cell.HasAttribute("s")) { [int]$cell.GetAttribute("s") } else { 0 }
        Set-CellValue -Document $document -Cell $cell -Value $property.Value -Style $style
    }
    Write-ZipEntryText -Zip $Zip -EntryPath $entryPath -Text $document.OuterXml
}

try {
    & python (Join-Path $PSScriptRoot "GenerateSlicingBibleSheet.py") --output $payloadPath --recipes-output $recipePayloadPath
    if ($LASTEXITCODE -ne 0) { throw "Slicing worksheet payload generation failed with exit code $LASTEXITCODE." }
    $sheetXml = [IO.File]::ReadAllText($payloadPath, [Text.Encoding]::UTF8)
    [xml]$sheetValidation = $sheetXml
    if ($sheetValidation.DocumentElement.LocalName -ne "worksheet") { throw "Generated Slicing payload is not a worksheet." }
    $recipePayload = Get-Content -LiteralPath $recipePayloadPath -Raw | ConvertFrom-Json

    $beforeHashes = Get-EntryHashes -Path $workbookFullPath -Excluded $touchedEntries
    $zip = [IO.Compression.ZipFile]::Open($workbookFullPath, [IO.Compression.ZipArchiveMode]::Update)
    try {
        $workbookXml = Read-ZipEntryText -Zip $zip -EntryPath "xl/workbook.xml"
        if ($workbookXml -notmatch 'name="Slicing"') {
            $workbookXml = $workbookXml.Replace(
                "</sheets>",
                '<sheet name="Slicing" sheetId="70" r:id="rId74"/></sheets>')
        }

        $relationshipsXml = Read-ZipEntryText -Zip $zip -EntryPath "xl/_rels/workbook.xml.rels"
        if ($relationshipsXml -notmatch 'Id="rId74"') {
            $relationshipsXml = $relationshipsXml.Replace(
                "</Relationships>",
                '<Relationship Id="rId74" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet70.xml"/></Relationships>')
        }

        $contentTypesXml = Read-ZipEntryText -Zip $zip -EntryPath "[Content_Types].xml"
        if ($contentTypesXml -notmatch 'PartName="/xl/worksheets/sheet70.xml"') {
            $contentTypesXml = $contentTypesXml.Replace(
                "</Types>",
                '<Override PartName="/xl/worksheets/sheet70.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>')
        }

        Write-ZipEntryText -Zip $zip -EntryPath "xl/workbook.xml" -Text $workbookXml
        Write-ZipEntryText -Zip $zip -EntryPath "xl/_rels/workbook.xml.rels" -Text $relationshipsXml
        Write-ZipEntryText -Zip $zip -EntryPath "[Content_Types].xml" -Text $contentTypesXml
        Write-ZipEntryText -Zip $zip -EntryPath "xl/worksheets/sheet70.xml" -Text $sheetXml
        foreach ($worksheet in $recipePayload.worksheets.PSObject.Properties) {
            Set-WorksheetRecipeRows -Zip $zip -EntryPath $worksheet.Name -Rows @($worksheet.Value)
        }
        Set-EspionageDescriptions -Zip $zip -Descriptions $recipePayload.espionageDescriptions
    }
    finally { $zip.Dispose() }

    $afterHashes = Get-EntryHashes -Path $workbookFullPath -Excluded $touchedEntries
    $changedUntouched = @($beforeHashes.Keys | Where-Object {
        -not $afterHashes.ContainsKey($_) -or $beforeHashes[$_] -ne $afterHashes[$_]
    })
    if ($changedUntouched.Count -gt 0) {
        throw "Untouched workbook entries changed: $($changedUntouched -join ', ')"
    }

    $layoutPath = Join-Path $repoRoot "tools\CombatUpgradeBibleWorkbookLayout.json"
    $layoutText = [IO.File]::ReadAllText($layoutPath, [Text.Encoding]::UTF8)
    if ($layoutText -notmatch '"Slicing"\s*:') {
        $layoutEntry = ',"Slicing":[{"min":1,"max":1,"width":17},{"min":2,"max":2,"width":22},{"min":3,"max":3,"width":24},{"min":4,"max":4,"width":16},{"min":5,"max":5,"width":34},{"min":6,"max":6,"width":20},{"min":7,"max":7,"width":25},{"min":8,"max":8,"width":80}]'
        $insertAt = $layoutText.LastIndexOf("}}", [StringComparison]::Ordinal)
        if ($insertAt -lt 0) { throw "Could not locate columnsBySheet end in the Bible layout manifest." }
        $layoutText = $layoutText.Insert($insertAt, $layoutEntry)
        [IO.File]::WriteAllText($layoutPath, $layoutText, [Text.UTF8Encoding]::new($false))
    }

    Write-Host "Updated Slicing, its five perk descriptions, 25 recipe rows, and the layout manifest while preserving untouched workbook entry bytes."
}
finally {
    if (Test-Path -LiteralPath $payloadPath) { Remove-Item -LiteralPath $payloadPath -Force }
    if (Test-Path -LiteralPath $recipePayloadPath) { Remove-Item -LiteralPath $recipePayloadPath -Force }
}
