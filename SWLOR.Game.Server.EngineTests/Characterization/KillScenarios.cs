using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.EngineTests.Characterization.Support;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Characterization;

/// <summary>
/// Output locking for a kill: a headless player earns combat points against a real module creature, the creature dies
/// through the normal death events, and the scenario records the skill XP the player received and the loot the
/// creature's corpse holds.
/// </summary>
public static class KillScenarios
{
    private const int Seed = 20261030;
    private const string Creature = "sanddemon";

    private sealed record Contribution(SkillType Skill, int Rank, int CombatPoints);

    private static readonly Contribution[] Contributions =
    {
        new(SkillType.Vibroblade, 12, 6),
        new(SkillType.Pistol, 30, 2),
        new(SkillType.Force, 8, 3),
        new(SkillType.FirstAid, 20, 1),
        new(SkillType.Leadership, 5, 1),
        new(SkillType.Armor, 16, 0)
    };

    [EngineTest("Characterization: kill awards skill XP and leaves corpse loot", Category = "Characterization",
        TimeoutSeconds = 240f)]
    public static async Task KillXPAndCorpseLoot(EngineTestContext ctx)
    {
        using var clock = new FrozenClock();
        using var arena = await ScenarioArena.CreateAsync(ctx);
        var scenario = arena.Context;

        using var player = await PlayerAbilityFixture.CreateAsync(scenario, -1f);
        player.Update(record =>
        {
            foreach (var skill in Enum.GetValues<SkillType>().Where(skill => skill != SkillType.Invalid))
            {
                record.Skills[skill].Rank = 0;
                record.Skills[skill].XP = 0;
            }

            foreach (var contribution in Contributions)
                record.Skills[contribution.Skill].Rank = contribution.Rank;

            record.XPDebt = 0;
            record.DMXPBonus = 0;
        });

        var npc = await ScenarioActors.SpawnQuietAsync(scenario, Creature, 2f, 0f);
        var level = Stat.GetNPCStats(npc).Level;
        scenario.Assert(level > 0, "The creature has a stat skin with a level");
        foreach (var contribution in Contributions.Where(contribution => contribution.CombatPoints > 0))
            CombatPoint.AddCombatPoint(player.Creature, npc, contribution.Skill, contribution.CombatPoints);

        var recordBefore = DB.Get<Player>(player.Id);
        var snapshotBefore = Contributions.ToDictionary(contribution => contribution.Skill,
            contribution => recordBefore.Skills[contribution.Skill].XP);

        scenario.SeedRandom(Seed);
        await scenario.ExecuteInCreatureContextAsync(npc, () =>
            ApplyEffectToObject(DurationType.Instant, EffectDeath(), npc));
        await scenario.WaitUntilAsync(() => GetIsDead(npc), 10f, "the creature to die");
        // The death events run after the death effect: combat points become XP, loot becomes a corpse.
        await scenario.DelaySecondsAsync(2f);

        var xpTable = new SnapshotTable(
            "CombatPoint.OnCreatureDeath / Skill.GiveSkillXP",
            "A player with combat points against a creature kills it; each contributing skill receives XP by the " +
            "creature's level, the skill's rank and its share of the combat points in its category.",
            $"Creature {Creature} (level {level}) killed with Random seeded {Seed}. The headless player has the listed skill " +
            "ranks and no other skills, no XP debt and no DM bonus. Combat points are added directly with " +
            "CombatPoint.AddCombatPoint. Armor is exempt from combat points and always earns XP.",
            new[] { "skill", "rank", "combatPoints" },
            new[] { "rankAfter", "xpBefore", "xpAfter" },
            Seed) { AllowConstantOutputs = true };

        var record = DB.Get<Player>(player.Id);
        foreach (var contribution in Contributions)
        {
            var after = record.Skills[contribution.Skill];
            xpTable.Add(contribution.Skill.ToString(), contribution.Rank, contribution.CombatPoints, after.Rank,
                snapshotBefore[contribution.Skill], after.XP);
        }

        ScenarioSnapshot.Verify("Skill", "KillXP", xpTable);

        var corpse = FindCorpse(arena.Area, npc);
        scenario.Assert(GetIsObjectValid(corpse), "The dead creature left a corpse container");
        var items = ItemDescription.DescribeInventory(corpse);
        var lootTable = new SnapshotTable(
            "Loot.SpawnLootOnCreatureDeath / Loot.ProcessCorpse",
            "A killed creature rolls its LOOT_TABLE_n entries and moves its items and gold into a corpse container.",
            $"Creature {Creature} killed with Random seeded {Seed} immediately before the death effect; each row is one item " +
            "in the corpse, written as 'blueprint xStackSize [Type/Subtype/Cost/Param, ...]'.",
            new[] { "item" },
            new[] { "gold" },
            Seed) { AllowConstantOutputs = true };
        var gold = GetGold(corpse);
        if (items.Count == 0)
            lootTable.Add("(no items)", gold);
        foreach (var item in items)
            lootTable.Add(item, gold);

        ScenarioSnapshot.Verify("Loot", "KillCorpse", lootTable);
    }

    private static uint FindCorpse(uint area, uint body)
    {
        for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
        {
            if (GetLocalObject(obj, Loot.CorpseBodyVariable) == body && GetObjectType(obj) == ObjectType.Placeable)
                return obj;
        }

        return OBJECT_INVALID;
    }
}
