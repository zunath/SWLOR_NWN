#Requires -PSEdition Core
<# .SYNOPSIS Exports registered ship module roles; requires PowerShell on .NET 10 and a built server assembly. #>
param([string]$AssemblyPath = "SWLOR.Game.Server/bin/Debug/net10.0/SWLOR.Game.Server.dll")
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
if ([Environment]::Version.Major -lt 10) { throw "Use pwsh on .NET 10 to load the server assembly." }
$repoRoot = Split-Path -Parent $PSScriptRoot
$assembly = [System.Reflection.Assembly]::LoadFrom((Join-Path $repoRoot $AssemblyPath))
$interface = $assembly.GetType("SWLOR.Game.Server.Service.SpaceService.IShipModuleListDefinition", $true)
$rows = foreach ($type in $assembly.GetTypes() | Where-Object { !$_.IsAbstract -and $interface.IsAssignableFrom($_) }) {
    $definition = [Activator]::CreateInstance($type)
    foreach ($module in $definition.BuildShipModules().GetEnumerator()) {
        $detail = $module.Value
        [pscustomobject]@{
            Tag = $module.Key
            Name = $detail.Name
            GameplayUse = [regex]::Replace($detail.Description, '[ \t]+(?=\r?$)', '', [System.Text.RegularExpressions.RegexOptions]::Multiline)
            ModuleType = $detail.Type.ToString()
            FittingSlot = $detail.PowerType.ToString()
            Active = $null -ne $detail.ModuleActivatedAction
            Timed = $null -ne $detail.CalculateRecastAction
            CapitalOnly = $detail.CapitalClassModule
            CanTargetSelf = $detail.CanTargetSelf
            PerkRequirements = ($detail.RequiredPerks.GetEnumerator() | Sort-Object Key | ForEach-Object { "$($_.Key):$($_.Value)" }) -join ";"
            IconResRef = $detail.Texture
            SourcePath = "SWLOR.Game.Server/Feature/ShipModuleDefinition/$($type.Name).cs"
        }
    }
}
$rows | Sort-Object Tag | Export-Csv -NoTypeInformation -LiteralPath (Join-Path $repoRoot "SWLOR.Game.Server/Readmes/ShipModuleIconAudit.csv")
Write-Host "Reviewed $(@($rows).Count) registered ship modules, including NPC configurations."
