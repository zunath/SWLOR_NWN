using System;
using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>Final Foundry corrections applied during the existing loot migration's item visit.</summary>
public static class FoundryLootStatRevisionCatalog
{
    public const string RevisionVariable = "FOUNDRY_LOOT_STATS_REVISION";
    public const int CurrentRevision = 1;

    public static IReadOnlyDictionary<string, LootStatChange[]> Changes { get; } =
        new Dictionary<string, LootStatChange[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["grapple_bracer"] = new[] { new LootStatChange(ItemPropertyType.HPBonus, 0, 63, 60) },
        };
}
