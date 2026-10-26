[CmdletBinding()]
param(
    [string]$WorkbookPath = "design\bible\SWLOR Design Bible.xlsx"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$workbookFullPath = if ([IO.Path]::IsPathRooted($WorkbookPath)) { $WorkbookPath } else { Join-Path $repoRoot $WorkbookPath }
$payloadPath = Join-Path ([IO.Path]::GetTempPath()) ("swlor-slicing-sheet-{0}.xml" -f [guid]::NewGuid().ToString("N"))
$recipePayloadPath = Join-Path ([IO.Path]::GetTempPath()) ("swlor-slicing-recipes-{0}.json" -f [guid]::NewGuid().ToString("N"))
# Edits go to a copy that replaces the workbook only after every check passes, so a
# failed update never leaves a half-written workbook behind.
$stagedWorkbookPath = Join-Path ([IO.Path]::GetTempPath()) ("swlor-slicing-workbook-{0}.xlsx" -f [guid]::NewGuid().ToString("N"))

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

function Get-SharedStrings {
    param([IO.Compression.ZipArchive]$Zip)
    [xml]$document = Read-ZipEntryText -Zip $Zip -EntryPath "xl/sharedStrings.xml"
    $namespace = [Xml.XmlNamespaceManager]::new($document.NameTable)
    $namespace.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")
    return @($document.SelectNodes("/d:sst/d:si", $namespace) | ForEach-Object {
        (@($_.SelectNodes(".//d:t", $namespace)) | ForEach-Object { $_.InnerText }) -join ""
    })
}

function Get-CellText {
    param([Xml.XmlElement]$Row, [string]$Column, [string[]]$SharedStrings, [Xml.XmlNamespaceManager]$Namespace)
    $cell = @($Row.SelectNodes("d:c", $Namespace)) |
        Where-Object { $_.GetAttribute("r") -match "^$Column\d+$" } |
        Select-Object -First 1
    if ($null -eq $cell) { return "" }
    if ($cell.GetAttribute("t") -eq "s") {
        $value = $cell.SelectSingleNode("d:v", $Namespace)
        return $(if ($null -eq $value) { "" } else { $SharedStrings[[int]$value.InnerText] })
    }
    return (@($cell.SelectNodes(".//d:t | d:v", $Namespace)) | ForEach-Object { $_.InnerText }) -join ""
}

# Rows are found by content, not by fixed row numbers, because other edits to these
# shared tabs move rows. A missing or duplicated key stops the update instead of
# writing into whatever row now sits at an old address.
function Find-RowByCellText {
    param([Xml.XmlNode]$SheetData, [string]$Column, [string]$Text, [string[]]$SharedStrings,
          [Xml.XmlNamespaceManager]$Namespace, [string]$EntryPath)
    $found = @($SheetData.SelectNodes("d:row", $Namespace) | Where-Object {
        (Get-CellText -Row $_ -Column $Column -SharedStrings $SharedStrings -Namespace $Namespace) -eq $Text
    })
    if ($found.Count -ne 1) {
        throw "Expected exactly one row in '$EntryPath' with column $Column '$Text', found $($found.Count)."
    }
    return $found[0]
}

function Set-WorksheetRecipeRows {
    param(
        [IO.Compression.ZipArchive]$Zip,
        [string]$EntryPath,
        [object[]]$Rows,
        [string[]]$SharedStrings
    )
    [xml]$document = Repair-InvalidXmlSpacePrefixes -Text (Read-ZipEntryText -Zip $Zip -EntryPath $EntryPath)
    $namespaceUri = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    $namespace = [Xml.XmlNamespaceManager]::new($document.NameTable)
    $namespace.AddNamespace("d", $namespaceUri)
    $sheetData = $document.SelectSingleNode("/d:worksheet/d:sheetData", $namespace)
    foreach ($spec in $Rows) {
        $row = Find-RowByCellText -SheetData $sheetData -Column "D" -Text ([string]$spec.cells.D) `
            -SharedStrings $SharedStrings -Namespace $namespace -EntryPath $EntryPath
        $rowNumber = $row.GetAttribute("r")
        while ($row.HasChildNodes) { [void]$row.RemoveChild($row.FirstChild) }

        foreach ($property in $spec.cells.PSObject.Properties) {
            $cell = $document.CreateElement("c", $namespaceUri)
            $cell.SetAttribute("r", "$($property.Name)$rowNumber")
            Set-CellValue -Document $document -Cell $cell -Value $property.Value -Style ([int]$spec.style)
            [void]$row.AppendChild($cell)
        }
    }

    Write-ZipEntryText -Zip $Zip -EntryPath $EntryPath -Text $document.OuterXml
}

function Set-EspionageDescriptions {
    param([IO.Compression.ZipArchive]$Zip, [object]$Descriptions, [string[]]$SharedStrings)
    $entryPath = "xl/worksheets/sheet30.xml"
    [xml]$document = Repair-InvalidXmlSpacePrefixes -Text (Read-ZipEntryText -Zip $Zip -EntryPath $entryPath)
    $namespaceUri = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    $namespace = [Xml.XmlNamespaceManager]::new($document.NameTable)
    $namespace.AddNamespace("d", $namespaceUri)
    $sheetData = $document.SelectSingleNode("/d:worksheet/d:sheetData", $namespace)
    foreach ($property in $Descriptions.PSObject.Properties) {
        $row = Find-RowByCellText -SheetData $sheetData -Column "C" -Text $property.Name `
            -SharedStrings $SharedStrings -Namespace $namespace -EntryPath $entryPath
        $address = "G$($row.GetAttribute("r"))"
        $cell = $row.SelectSingleNode("d:c[@r='$address']", $namespace)
        if ($null -eq $cell) { throw "Espionage cell '$address' for '$($property.Name)' was not found." }
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

    Copy-Item -LiteralPath $workbookFullPath -Destination $stagedWorkbookPath
    $beforeHashes = Get-EntryHashes -Path $stagedWorkbookPath -Excluded $touchedEntries
    $zip = [IO.Compression.ZipFile]::Open($stagedWorkbookPath, [IO.Compression.ZipArchiveMode]::Update)
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
        $sharedStrings = Get-SharedStrings -Zip $zip
        foreach ($worksheet in $recipePayload.worksheets.PSObject.Properties) {
            Set-WorksheetRecipeRows -Zip $zip -EntryPath $worksheet.Name -Rows @($worksheet.Value) -SharedStrings $sharedStrings
        }
        Set-EspionageDescriptions -Zip $zip -Descriptions $recipePayload.espionageDescriptions -SharedStrings $sharedStrings
    }
    finally { $zip.Dispose() }

    $afterHashes = Get-EntryHashes -Path $stagedWorkbookPath -Excluded $touchedEntries
    $changedUntouched = @($beforeHashes.Keys | Where-Object {
        -not $afterHashes.ContainsKey($_) -or $beforeHashes[$_] -ne $afterHashes[$_]
    })
    if ($changedUntouched.Count -gt 0) {
        throw "Untouched workbook entries changed: $($changedUntouched -join ', ')"
    }
    Copy-Item -LiteralPath $stagedWorkbookPath -Destination $workbookFullPath -Force

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
    if (Test-Path -LiteralPath $stagedWorkbookPath) { Remove-Item -LiteralPath $stagedWorkbookPath -Force }
}
