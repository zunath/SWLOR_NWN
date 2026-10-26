using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// Player-obtainable items whose blueprints carried an elemental Defense property (Poison, Fire or Ice). Only
/// Physical and Force Defense count, so the revision removes the retired property from owned copies, along with the
/// base amount the combat replacement already converted into the matching Resistance on older copies.
/// </summary>
public static class RetiredDefenseStatRevisionCatalog
{
    // Persisted on items; the name predates the catalog covering more than the Carapace Bracer.
    public const string RevisionVariable = "CARAPACE_BRACER_STATS_REVISION";
    public const int CurrentRevision = 1;

    private static LootStatChange[] Retired(CombatDamageType type, int amount) =>
        new[] { new LootStatChange(ItemPropertyType.Defense, (int)type, amount, 0) };

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
            ["apprentice_dark_"] = Retired(CombatDamageType.Fire, 4),
            ["dan_heavy_armor"] = Retired(CombatDamageType.Fire, 10),
            ["dan_h_cloak"] = Retired(CombatDamageType.Ice, 10),
            ["dan_hyper_gloves"] = Retired(CombatDamageType.Ice, 5),
        };

    /// <summary>
    /// The combat migration converted older copies' base elemental Defense into Resistance. For a copy with no
    /// retired Defense left, returns the change that removes that converted base and keeps crafted Resistance.
    /// </summary>
    public static IEnumerable<LootStatChange> ConvertedResistanceChanges(string resref,
        Func<int, int> defenseTotal, Func<int, int> resistanceTotal)
    {
        if (!Changes.TryGetValue(resref, out var changes))
            yield break;
        foreach (var change in changes.Where(x => x.Type == ItemPropertyType.Defense && x.Previous > 0))
        {
            if (!((CombatDamageType)change.Subtype).TryGetElementalResistanceType(out var resistance))
                continue;
            if (defenseTotal(change.Subtype) == 0 && resistanceTotal((int)resistance) >= change.Previous)
                yield return new LootStatChange(ItemPropertyType.Resistance, (int)resistance, change.Previous, 0);
        }
    }
}
