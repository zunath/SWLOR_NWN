using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

public static class CarapaceBracerStatRevisionCatalog
{
    public const string RevisionVariable = "CARAPACE_BRACER_STATS_REVISION";
    public const int CurrentRevision = 1;

    public static IReadOnlyDictionary<string, LootStatChange[]> Changes { get; } =
        new Dictionary<string, LootStatChange[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["cara_bracer"] = new[]
            {
                new LootStatChange(ItemPropertyType.Defense, (int)CombatDamageType.Poison, 30, 0),
            },
            ["cara_wraps"] = new[]
            {
                new LootStatChange(ItemPropertyType.Defense, (int)CombatDamageType.Poison, 30, 0),
            },
        };
}
