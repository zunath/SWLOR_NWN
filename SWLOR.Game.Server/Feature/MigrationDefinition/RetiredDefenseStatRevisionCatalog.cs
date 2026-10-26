using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// Player-obtainable items whose blueprints carried an elemental Defense property (Poison, Fire or Ice). Only
/// Physical and Force Defense count, so the revision removes the retired property from owned copies, along with the
/// base amount the combat replacement already converted into the matching Resistance on older copies. Three Dantooine
/// items also rise to their slot's loot standard (Kwi Heavy Armor, Kwi Light Cloak, Kwi Hyper Gloves).
/// </summary>
public static class RetiredDefenseStatRevisionCatalog
{
    // Persisted on items; the name predates the catalog covering more than the Carapace Bracer.
    public const string RevisionVariable = "CARAPACE_BRACER_STATS_REVISION";
    public const int CurrentRevision = 1;

    private static LootStatChange[] Retired(CombatDamageType type, int amount, params LootStatChange[] raised) =>
        new[] { new LootStatChange(ItemPropertyType.Defense, (int)type, amount, 0) }.Concat(raised).ToArray();

    /// <summary>The retired elemental Defense an item's revision removes.</summary>
    public static LootStatChange RetiredDefense(string resref) =>
        Changes[resref].Single(x => x.Type == ItemPropertyType.Defense && x.Current == 0 &&
                                    ((CombatDamageType)x.Subtype).IsElementalDamageType());

    public static IReadOnlyDictionary<string, LootStatChange[]> Changes { get; } =
        new Dictionary<string, LootStatChange[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["cara_bracer"] = Retired(CombatDamageType.Poison, 30),
            ["cara_wraps"] = Retired(CombatDamageType.Poison, 30),
            ["cara_tunic"] = Retired(CombatDamageType.Poison, 30),
            ["cara_armor"] = Retired(CombatDamageType.Poison, 30),
            ["cara_robes"] = Retired(CombatDamageType.Poison, 30),
            ["cara_boots"] = Retired(CombatDamageType.Poison, 30),
            ["cara_leggings"] = Retired(CombatDamageType.Poison, 30),
            ["cara_shoes"] = Retired(CombatDamageType.Poison, 30),
            ["cara_gloves"] = Retired(CombatDamageType.Poison, 30),
            ["cara_cap"] = Retired(CombatDamageType.Poison, 30),
            ["cara_headdress"] = Retired(CombatDamageType.Poison, 30),
            ["cara_helmet"] = Retired(CombatDamageType.Poison, 30),
            // Equipped by Hutlar Byysk NPCs; droppable equipment reaches players through corpse loot.
            ["byysk_shield002"] = Retired(CombatDamageType.Poison, 100),
            ["byyskchief_w2"] = Retired(CombatDamageType.Poison, 100),
            ["apprentice_dark_"] = Retired(CombatDamageType.Fire, 4),
            ["dan_heavy_armor"] = Retired(CombatDamageType.Fire, 10,
                new LootStatChange(ItemPropertyType.Defense, (int)CombatDamageType.Physical, 20, 25),
                new LootStatChange(ItemPropertyType.Defense, (int)CombatDamageType.Force, 0, 20)),
            ["dan_h_cloak"] = Retired(CombatDamageType.Ice, 10,
                new LootStatChange(ItemPropertyType.Defense, (int)CombatDamageType.Force, 0, 8)),
            ["dan_hyper_gloves"] = Retired(CombatDamageType.Ice, 5,
                new LootStatChange(ItemPropertyType.Defense, (int)CombatDamageType.Force, 10, 21),
                new LootStatChange(ItemPropertyType.HPBonus, 0, 0, 21)),
        };

    /// <summary>
    /// The combat migration converted older copies' retired elemental Defense into Resistance of that element. No
    /// version of these blueprints carried Resistance or a saving throw, and nothing adds Resistance to an existing
    /// loot item, so that Resistance can only have come from the retired Defense. Every owned copy ends up like a new
    /// drop: the revision removes all Resistance of the retired element.
    /// </summary>
    public static IEnumerable<LootStatChange> ConvertedResistanceChanges(string resref, Func<int, int> resistanceTotal)
    {
        if (!Changes.ContainsKey(resref))
            yield break;
        var retired = RetiredDefense(resref);
        if (((CombatDamageType)retired.Subtype).TryGetElementalResistanceType(out var resistance) &&
            resistanceTotal((int)resistance) is var total and > 0)
            yield return new LootStatChange(ItemPropertyType.Resistance, (int)resistance, total, 0);
    }
}
