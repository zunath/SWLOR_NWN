using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class RareEliteWeaponEngineTests
{
    /// <summary>
    /// Spawns every rare creature registered in any spawn table and checks that none carries a weapon in its
    /// inventory instead of its hands. The engine moves a blueprint's held weapon to inventory at spawn when the
    /// creature lacks the feat baseitems.2da requires for it, which leaves the elite fighting unarmed.
    /// </summary>
    [EngineTest("Rare elites spawn with their weapon in hand", Category = "RareEliteWeapon", TimeoutSeconds = 180f)]
    public static async Task RareElitesSpawnArmed(EngineTestContext ctx)
    {
        var rareCreatures = GetRareCreatureResrefs();
        ctx.Assert(rareCreatures.Count > 0, "Spawn definitions must register at least one rare creature");

        var unarmed = new List<string>();
        foreach (var resref in rareCreatures)
        {
            var elite = ctx.SpawnCreature(resref);
            await ctx.WaitFrameAsync();

            var carried = GetCarriedWeapons(elite);
            if (carried.Count > 0)
                unarmed.Add($"{resref} carries [{string.Join(",", carried)}] instead of holding it");

            DestroyObject(elite);
            await ctx.WaitFrameAsync();
        }

        ctx.Log($"Checked {rareCreatures.Count} rare creatures");
        ctx.Assert(unarmed.Count == 0, string.Join("; ", unarmed));
    }

    /// <summary>Rare creature resrefs from every spawn list definition, the same tables the spawn service loads.</summary>
    private static List<string> GetRareCreatureResrefs()
    {
        return typeof(ISpawnListDefinition).Assembly.GetTypes()
            .Where(type => typeof(ISpawnListDefinition).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            .SelectMany(type => ((ISpawnListDefinition)Activator.CreateInstance(type)!).BuildSpawnTables().Values)
            .SelectMany(table => table.Spawns)
            .Where(spawn => spawn.IsRare && spawn.Type == ObjectType.Creature)
            .Select(spawn => spawn.Resref)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(resref => resref, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Resrefs of weapons sitting in a creature's inventory rather than in an equipment slot.</summary>
    private static List<string> GetCarriedWeapons(uint creature)
    {
        var weapons = new List<string>();
        for (var item = GetFirstItemInInventory(creature); GetIsObjectValid(item); item = GetNextItemInInventory(creature))
        {
            if (Item.WeaponBaseItemTypes.Contains(GetBaseItemType(item)))
                weapons.Add(GetResRef(item));
        }

        return weapons;
    }
}
