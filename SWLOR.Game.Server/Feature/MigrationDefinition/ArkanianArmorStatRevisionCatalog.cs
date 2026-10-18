using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// Moves crafted Arkanian Dragon Breastplates onto the current endgame breastplate values. The shipped
/// blueprint's base Ice Defense is retired; it becomes the base <see cref="IceResistance"/>, revised from each copy's Ice values.
/// </summary>
public static class ArkanianArmorStatRevisionCatalog
{
    public const string RevisionVariable = "ARKANIAN_ARMOR_STATS_REVISION";
    public const int CurrentRevision = 1;
    public const int IceResistance = 30;

    public static IReadOnlyDictionary<string, LootStatChange[]> Changes { get; } =
        new Dictionary<string, LootStatChange[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["ark_armor"] = new[]
            {
                new LootStatChange(ItemPropertyType.HPBonus, 0, 95, 110),
                new LootStatChange(ItemPropertyType.Defense, (int)CombatDamageType.Physical, 28, 32),
                new LootStatChange(ItemPropertyType.Defense, (int)CombatDamageType.Force, 0, 25),
                new LootStatChange(ItemPropertyType.STMRegen, 0, 2, 3),
                new LootStatChange(ItemPropertyType.RequiresSkill, (int)SkillType.Armor, 40, 50),
            },
        };
}
