using System;
using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>Adjusts already-revised loot to the budget of its corrected equipment slot.</summary>
public static class EquipmentTypeStatRevisionCatalog
{
    public const string RevisionVariable = "EQUIPMENT_TYPE_STATS_REVISION";
    public const int CurrentRevision = 1;
    public static IReadOnlyDictionary<string, LootStatChange[]> Changes { get; } =
        new Dictionary<string, LootStatChange[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["hexweave"] = new[] { new LootStatChange(ItemPropertyType.Attack, 0, 11, 8) },
        };
}
