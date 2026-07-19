param(
    [int]$Skip = 0,
    [int]$Take = 50
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$keyItemPath = Join-Path $repoRoot 'SWLOR.Game.Server\Service\KeyItemService\KeyItemType.cs'
$source = Get-Content -Raw $keyItemPath

$pattern = '(?ms)^\s*\[KeyItem\(KeyItemCategoryType\.(?<Category>\w+),\s*"(?<Name>(?:[^"\\]|\\.)*)",\s*(?<Active>true|false),\s*"(?<Description>(?:[^"\\]|\\.)*)"\)\]\s*\r?\n\s*(?<Type>\w+)\s*=\s*(?<Id>\d+),'
$keyItemMatches = [regex]::Matches($source, $pattern)

function Convert-CSharpString([string]$value)
{
    return [regex]::Unescape($value)
}

function Get-CategoryDirection([string]$category)
{
    switch ($category)
    {
        'Maps' {
            return 'Depict a unique holographic map for the named location. Use an orbital chart for an orbit map, a topographic display for wilderness, and an architectural plan for a facility or settlement.'
        }
        'QuestItems' {
            return 'Depict the exact tangible quest object or record named below. Use the description for context, focus on the object rather than characters, and give it a distinctive silhouette and construction.'
        }
        'Documents' {
            return 'Depict the exact document named below with distinctive binding, age, construction, and closure details.'
        }
        'Keys' {
            return 'Depict the exact access key, pass, card, or device named below with distinctive materials, silhouette, security hardware, and color coding.'
        }
        default {
            throw "Unsupported Key Item icon category '$category'."
        }
    }
}

$records = foreach ($match in $keyItemMatches)
{
    $active = $match.Groups['Active'].Value -eq 'true'
    $category = $match.Groups['Category'].Value
    if (!$active -or $category -in @('Invalid', 'FieldNotes', 'Maps'))
    {
        continue
    }

    $id = [int]$match.Groups['Id'].Value
    $name = Convert-CSharpString $match.Groups['Name'].Value
    $description = Convert-CSharpString $match.Groups['Description'].Value
    $categoryDirection = Get-CategoryDirection $category

    $prompt = @"
Use case: stylized-concept
Asset type: Neverwinter Nights: Enhanced Edition Key Item UI icon, ultimately displayed at 40x40 pixels from a 64x64 TGA
Primary request: Create the unique Key Item icon for "$name".
Canonical description: $description
Category direction: $categoryDirection
Style/medium: polished hand-painted science-fantasy game icon, consistent "galactic archive" visual language
Composition/framing: square, one centered subject, bold unmistakable silhouette, generous padding, readable when reduced to 40x40, subtle inset octagonal archive frame
Lighting/mood: dark navy-black backdrop, restrained cyan holographic rim light, focused dramatic lighting
Category accent: Maps use amber-gold; Quest Items use violet-magenta; Documents use ivory-gold; Keys use red-orange
Constraints: completely original artwork and a composition distinct from every other Key Item; no readable text, letters, numbers, logos, watermark, UI labels, character portraits, or tiny peripheral clutter; crisp edges and strong value contrast
"@.Trim()

    [pscustomobject]@{
        Type = $match.Groups['Type'].Value
        Id = $id
        Category = $category
        Name = $name
        Description = $description
        Resref = "iki_{0:D4}" -f $id
        Prompt = $prompt
    }
}

$records = @($records)
if ($records.Count -ne 198)
{
    throw "Expected 198 active Key, Quest Item, and Document entries but found $($records.Count)."
}

$records |
    Sort-Object Id |
    Select-Object -Skip $Skip -First $Take |
    ConvertTo-Json -Depth 3 -Compress
