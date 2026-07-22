[CmdletBinding()]
param(
    [string]$Feat2daPath = "SWLOR_Haks\sw_2da\feat.2da",
    [string]$Spells2daPath = "SWLOR_Haks\sw_2da\spells.2da",
    [string]$ManifestPath = "SWLOR.Game.Server\Readmes\GameplayIconManifest.csv"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot

function Resolve-RepoPath {
    param([string]$Path)

    if ([IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return Join-Path $repoRoot $Path
}

function Get-HeaderLineIndex {
    param([string[]]$Lines)

    for ($index = 1; $index -lt $Lines.Count; $index++) {
        if (![string]::IsNullOrWhiteSpace($Lines[$index])) {
            return $index
        }
    }

    throw "Could not locate a 2DA header line."
}

function Get-TokenMatches {
    param([string]$Line)

    return @([regex]::Matches($Line, "\S+"))
}

function Set-TokenPreservingWhitespace {
    param(
        [string]$Line,
        [int]$TokenIndex,
        [string]$Value
    )

    $matches = Get-TokenMatches $Line
    if ($TokenIndex -lt 0 -or $TokenIndex -ge $matches.Count) {
        throw "Line does not contain token index $TokenIndex."
    }

    $match = $matches[$TokenIndex]
    return $Line.Substring(0, $match.Index) + $Value + $Line.Substring($match.Index + $match.Length)
}

$techniques = [ordered]@{
    ApexCollapseTechnique = [pscustomobject]@{ Name = "Apex Collapse"; Icon = "ife_mapexcoll"; Category = "Self" }
    BloodFrenzyFlurryTechnique = [pscustomobject]@{ Name = "Blood Frenzy Flurry"; Icon = "ife_mbldfrenflur"; Category = "Harmful" }
    BonecrusherBiteTechnique = [pscustomobject]@{ Name = "Bonecrusher Bite"; Icon = "ife_mbonebite"; Category = "Passive" }
    BraceBreakerTechnique = [pscustomobject]@{ Name = "Brace Breaker"; Icon = "ife_mbracebrk"; Category = "Control" }
    ChitinGuardTechnique = [pscustomobject]@{ Name = "Chitin Guard"; Icon = "ife_mchitinguard"; Category = "Passive" }
    ConcussiveChallengeTechnique = [pscustomobject]@{ Name = "Concussive Challenge"; Icon = "ife_mconcchal"; Category = "Control" }
    CripplingTalonsTechnique = [pscustomobject]@{ Name = "Crippling Talons"; Icon = "ife_mcriptalons"; Category = "Passive" }
    CrossfireDrillTechnique = [pscustomobject]@{ Name = "Crossfire Drill"; Icon = "ife_mcrossdrill"; Category = "Harmful" }
    EssenceScarTechnique = [pscustomobject]@{ Name = "Essence Scar"; Icon = "ife_messscar"; Category = "Passive" }
    FinalEclipseTechnique = [pscustomobject]@{ Name = "Final Eclipse"; Icon = "ife_mfineclipse"; Category = "Harmful" }
    FinalLineTechnique = [pscustomobject]@{ Name = "Final Line"; Icon = "ife_mfinline"; Category = "Harmful" }
    FinalMandateTechnique = [pscustomobject]@{ Name = "Final Mandate"; Icon = "ife_mfinmand"; Category = "Beneficial" }
    FinalSuppressionTechnique = [pscustomobject]@{ Name = "Final Suppression"; Icon = "ife_mfinsup"; Category = "Control" }
    FinishingDriveTechnique = [pscustomobject]@{ Name = "Finishing Drive"; Icon = "ife_mfindrive"; Category = "Self" }
    GlacialSlimeTechnique = [pscustomobject]@{ Name = "Glacial Slime"; Icon = "ife_mglacslime"; Category = "Passive" }
    HoarfrostGlobTechnique = [pscustomobject]@{ Name = "Hoarfrost Glob"; Icon = "ife_mhoarglob"; Category = "Passive" }
    HoldfastSlamTechnique = [pscustomobject]@{ Name = "Holdfast Slam"; Icon = "ife_mholdslam"; Category = "Harmful" }
    InnerCircleBindTechnique = [pscustomobject]@{ Name = "Inner Circle Bind"; Icon = "ife_mincirbind"; Category = "Control" }
    InnerCirclePounceTechnique = [pscustomobject]@{ Name = "Inner Circle Pounce"; Icon = "ife_mincirpoun"; Category = "Harmful" }
    InnerCircleSurgeTechnique = [pscustomobject]@{ Name = "Inner Circle Surge"; Icon = "ife_mincirsurge"; Category = "Harmful" }
    InnerCircleVolleyTechnique = [pscustomobject]@{ Name = "Inner Circle Volley"; Icon = "ife_mincirvol"; Category = "Harmful" }
    InnerRingFlurryTechnique = [pscustomobject]@{ Name = "Inner Ring Flurry"; Icon = "ife_minringflur"; Category = "Harmful" }
    InnerVoidTechnique = [pscustomobject]@{ Name = "Inner Void"; Icon = "ife_minnervoid"; Category = "Harmful" }
    IronCarapaceTechnique = [pscustomobject]@{ Name = "Iron Carapace"; Icon = "ife_mironcarap"; Category = "Passive" }
    LastBastionTechnique = [pscustomobject]@{ Name = "Last Bastion"; Icon = "ife_mlastbast"; Category = "Beneficial" }
    LockstepCrushTechnique = [pscustomobject]@{ Name = "Lockstep Crush"; Icon = "ife_mlockcrush"; Category = "Harmful" }
    MaulingBiteTechnique = [pscustomobject]@{ Name = "Mauling Bite"; Icon = "ife_mmaulbite"; Category = "Passive" }
    MercilessAngleTechnique = [pscustomobject]@{ Name = "Merciless Angle"; Icon = "ife_mmercangle"; Category = "Harmful" }
    OpeningCutTechnique = [pscustomobject]@{ Name = "Opening Cut"; Icon = "ife_mopencut"; Category = "Passive" }
    PackHarrierTechnique = [pscustomobject]@{ Name = "Pack Harrier"; Icon = "ife_mpackharr"; Category = "Control" }
    PressureLockTechnique = [pscustomobject]@{ Name = "Pressure Lock"; Icon = "ife_mpreslock"; Category = "Control" }
    RallyBreakerTechnique = [pscustomobject]@{ Name = "Rally Breaker"; Icon = "ife_mrallybrk"; Category = "Harmful" }
    RangefinderShotTechnique = [pscustomobject]@{ Name = "Rangefinder Shot"; Icon = "ife_mrngshot"; Category = "Passive" }
    RendingBiteTechnique = [pscustomobject]@{ Name = "Rending Bite"; Icon = "ife_mrendbite"; Category = "Passive" }
    RendingCarveTechnique = [pscustomobject]@{ Name = "Rending Carve"; Icon = "ife_mrendcarve"; Category = "Passive" }
    RimePounceTechnique = [pscustomobject]@{ Name = "Rime Pounce"; Icon = "ife_mrimepounce"; Category = "Passive" }
    SerratedSlashTechnique = [pscustomobject]@{ Name = "Serrated Slash"; Icon = "ife_mserrslash"; Category = "Passive" }
    SignalSnareTechnique = [pscustomobject]@{ Name = "Signal Snare"; Icon = "ife_msigsnare"; Category = "Control" }
    SnapRushTechnique = [pscustomobject]@{ Name = "Snap Rush"; Icon = "ife_msnaprush"; Category = "Self" }
    StimCanisterTechnique = [pscustomobject]@{ Name = "Stim Canister"; Icon = "ife_mstimcan"; Category = "Beneficial" }
    SustainBurnTechnique = [pscustomobject]@{ Name = "Sustain Burn"; Icon = "ife_msusburn"; Category = "Self" }
    WardenClampTechnique = [pscustomobject]@{ Name = "Warden Clamp"; Icon = "ife_mwardclamp"; Category = "Control" }
    WardenMarkTechnique = [pscustomobject]@{ Name = "Warden Mark"; Icon = "ife_mwardmark"; Category = "Harmful" }
    WardenMaulTechnique = [pscustomobject]@{ Name = "Warden Maul"; Icon = "ife_mwardmaul"; Category = "Control" }
    WardenOrderTechnique = [pscustomobject]@{ Name = "Warden Order"; Icon = "ife_mwardorder"; Category = "Beneficial" }
    WardenRendTechnique = [pscustomobject]@{ Name = "Warden Rend"; Icon = "ife_mwardrend"; Category = "Harmful" }
    WardenSweepTechnique = [pscustomobject]@{ Name = "Warden Sweep"; Icon = "ife_mwardsweep"; Category = "Self" }
    WardenWallTechnique = [pscustomobject]@{ Name = "Warden Wall"; Icon = "ife_mwardwall"; Category = "Self" }
    WillFractureTechnique = [pscustomobject]@{ Name = "Will Fracture"; Icon = "ife_mwillfract"; Category = "Control" }
}

$featPath = Resolve-RepoPath $Feat2daPath
$spellsPath = Resolve-RepoPath $Spells2daPath
$manifestPath = Resolve-RepoPath $ManifestPath
$featLines = [IO.File]::ReadAllLines($featPath)
$spellLines = [IO.File]::ReadAllLines($spellsPath)

$featHeaderIndex = Get-HeaderLineIndex $featLines
$spellHeaderIndex = Get-HeaderLineIndex $spellLines
$featHeaders = @(Get-TokenMatches $featLines[$featHeaderIndex] | ForEach-Object { $_.Value })
$spellHeaders = @(Get-TokenMatches $spellLines[$spellHeaderIndex] | ForEach-Object { $_.Value })
$featLabelIndex = [array]::IndexOf($featHeaders, "LABEL") + 1
$featIconIndex = [array]::IndexOf($featHeaders, "ICON") + 1
$spellLabelIndex = [array]::IndexOf($spellHeaders, "Label") + 1
$spellIconIndex = [array]::IndexOf($spellHeaders, "IconResRef") + 1

if ($featLabelIndex -le 0 -or $featIconIndex -le 0 -or $spellLabelIndex -le 0 -or $spellIconIndex -le 0) {
    throw "Required LABEL/ICON columns were not found."
}

$spellUpdated = 0
for ($index = $spellHeaderIndex + 1; $index -lt $spellLines.Count; $index++) {
    $tokens = @(Get-TokenMatches $spellLines[$index] | ForEach-Object { $_.Value })
    if ($tokens.Count -le [Math]::Max($spellLabelIndex, $spellIconIndex)) {
        continue
    }

    $label = $tokens[$spellLabelIndex]
    if (!$techniques.Contains($label)) {
        continue
    }

    $expectedIcon = $techniques[$label].Icon
    if ($tokens[$spellIconIndex] -eq $expectedIcon) {
        continue
    }

    $spellLines[$index] = Set-TokenPreservingWhitespace $spellLines[$index] $spellIconIndex $expectedIcon
    $spellUpdated++
}

$featUpdated = 0
for ($index = $featHeaderIndex + 1; $index -lt $featLines.Count; $index++) {
    $tokens = @(Get-TokenMatches $featLines[$index] | ForEach-Object { $_.Value })
    if ($tokens.Count -le [Math]::Max($featLabelIndex, $featIconIndex)) {
        continue
    }

    $label = $tokens[$featLabelIndex]
    if (!$techniques.Contains($label)) {
        continue
    }

    $expectedIcon = $techniques[$label].Icon
    if ($tokens[$featIconIndex] -eq $expectedIcon) {
        continue
    }

    $featLines[$index] = Set-TokenPreservingWhitespace $featLines[$index] $featIconIndex $expectedIcon
    $featUpdated++
}

[IO.File]::WriteAllLines($featPath, $featLines)
[IO.File]::WriteAllLines($spellsPath, $spellLines)

$manifest = @(Import-Csv -LiteralPath $manifestPath)
$manifestUpdated = 0
foreach ($row in $manifest) {
    if (!$techniques.Contains($row.Key)) {
        continue
    }

    $technique = $techniques[$row.Key]
    if ($row.IconResRef -ne $technique.Icon -or
        $row.SemanticCategory -ne $technique.Category -or
        $row.DisplayName -ne $technique.Name) {
        $row.IconResRef = $technique.Icon
        $row.SemanticCategory = $technique.Category
        $row.DisplayName = $technique.Name
        $manifestUpdated++
    }
}

$manifest | Export-Csv -LiteralPath $manifestPath -NoTypeInformation -Encoding UTF8
Write-Host "Synchronized $featUpdated feat rows, $spellUpdated spell rows, and $manifestUpdated manifest rows for $($techniques.Count) unique Mimicry technique icons."
