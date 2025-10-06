# PowerShell script to convert static calls to DI services
# This script will help automate the conversion process

Write-Host "Starting static call conversion..."

# Define the mappings
$mappings = @{
    "Targeting\." = "_targetingService\."
    "HoloCom\." = "_holoComService\."
    "AnimationPlayer\." = "_animationPlayerService\."
    "Time\." = "_timeService\."
    "Activity\." = "_activityService\."
}

# Get all files that need conversion
$files = @(
    "SWLOR.Game.Server\Feature\GuiDefinition\ViewModel\CraftViewModel.cs",
    "SWLOR.Game.Server\Feature\GuiDefinition\ViewModel\MarketListingViewModel.cs",
    "SWLOR.Game.Server\Feature\GuiDefinition\ViewModel\CreatureManagerViewModel.cs",
    "SWLOR.Game.Server\Feature\GuiDefinition\ViewModel\PropertyItemStorageViewModel.cs",
    "SWLOR.Game.Server\Feature\GuiDefinition\ViewModel\DroidAssemblyViewModel.cs",
    "SWLOR.Game.Server\Feature\GuiDefinition\ViewModel\DroidAIViewModel.cs",
    "SWLOR.Game.Server\Feature\GuiDefinition\ViewModel\ShipManagementViewModel.cs",
    "SWLOR.Game.Server\Feature\GuiDefinition\ViewModel\RecipesViewModel.cs",
    "SWLOR.Game.Server\Feature\GuiDefinition\ViewModel\RefineryViewModel.cs",
    "SWLOR.Game.Server\Service\ChatCommand.cs",
    "SWLOR.Game.Server\Feature\ChatCommandDefinition\CharacterChatCommand.cs",
    "SWLOR.Game.Server\Service\Communication.cs",
    "SWLOR.Game.Server\Feature\DialogDefinition\HoloComDialog.cs",
    "SWLOR.Game.Server\Feature\CreatureDeathAnimation.cs"
)

foreach ($file in $files) {
    if (Test-Path $file) {
        Write-Host "Processing $file..."
        $content = Get-Content $file -Raw
        
        foreach ($mapping in $mappings.GetEnumerator()) {
            $content = $content -replace $mapping.Key, $mapping.Value
        }
        
        Set-Content $file -Value $content -NoNewline
        Write-Host "Updated $file"
    } else {
        Write-Host "File not found: $file"
    }
}

Write-Host "Conversion complete!"
