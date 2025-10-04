# PowerShell script to update ObjectPlugin references to use dependency injection

# List of files that need to be updated (excluding the ones already done)
$files = @(
    "SWLOR.Component.Associate\Service\BeastMasteryService.cs",
    "SWLOR.Component.Space\Service\SpaceService.cs",
    "SWLOR.Component.Combat\Service\StatService.cs",
    "SWLOR.Component.World\Service\SpawnManagementService.cs",
    "SWLOR.Component.Associate\UI\ViewModel\DroidAssemblyViewModel.cs",
    "SWLOR.Component.Associate\UI\ViewModel\IncubatorViewModel.cs",
    "SWLOR.Component.Character\UI\ViewModel\OutfitViewModel.cs",
    "SWLOR.Component.Market\UI\ViewModel\MarketBuyViewModel.cs",
    "SWLOR.Component.Market\UI\ViewModel\MarketListingViewModel.cs",
    "SWLOR.Component.Crafting\UI\ViewModel\RefineryViewModel.cs",
    "SWLOR.Component.Crafting\UI\ViewModel\ResearchViewModel.cs",
    "SWLOR.Component.Space\UI\ViewModel\ShipManagementViewModel.cs",
    "SWLOR.Component.Admin\UI\ViewModel\CreatureManagerViewModel.cs",
    "SWLOR.Component.Inventory\Feature\ItemDefinition\FishingRodItemDefinition.cs",
    "SWLOR.Component.Inventory\UI\ViewModel\BankViewModel.cs",
    "SWLOR.Component.Properties\UI\ViewModel\ManageStructuresViewModel.cs",
    "SWLOR.Component.Properties\UI\ViewModel\PropertyItemStorageViewModel.cs",
    "SWLOR.Component.Migration\Model\LegacyMigrationBase.cs",
    "SWLOR.Component.Migration\Feature\ServerMigration\_2_LegacyServerMigration.cs",
    "SWLOR.Component.Migration\Feature\ServerMigration\_7_UpdateStoredWeapons.cs",
    "SWLOR.Component.Migration\Model\ServerMigrationBase.cs",
    "SWLOR.Component.World\Service\WalkmeshService.cs"
)

foreach ($file in $files) {
    if (Test-Path $file) {
        Write-Host "Processing $file..."
        
        # Read the file content
        $content = Get-Content $file -Raw
        
        # Replace ObjectPlugin. with _objectPlugin.
        $content = $content -replace 'ObjectPlugin\.', '_objectPlugin.'
        
        # Write back to file
        Set-Content $file -Value $content -NoNewline
    } else {
        Write-Host "File not found: $file"
    }
}

Write-Host "Done updating ObjectPlugin references!"
