using NUnit.Framework;
using SWLOR.Game.Server.CharacterizationTests.Support;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatService;
using GameRandom = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.CharacterizationTests.Service;

/// <summary>
/// Locks <see cref="Combat.CalculateAutoAttackProcDamage"/>: which sources spend a roll, which rolls win, and the
/// damage added. The seeded table uses the same roll the game uses (a d100), so the fixture is not parallelizable.
/// </summary>
[NonParallelizable]
public class CombatAutoAttackProcCharacterizationTests
{
    private const int SeededRollSeed = 1703;
    private const int SeededTrials = 40;

    private static readonly string[] SourceSets =
    {
        "none", "50:10", "0:10", "100:0", "100:-4", "-5:10", "30:5,60:7,90:-3", "100:10,100:10,100:10"
    };

    private static readonly string[] SeededSourceSets = { "50:10", "5:100", "30:5,60:7,90:-3", "95:3,10:20,40:-6" };

    private static readonly int[][] ScriptedRolls =
    {
        Array.Empty<int>(), new[] { 1 }, new[] { 30 }, new[] { 31 }, new[] { 50 }, new[] { 51 }, new[] { 100 },
        new[] { 1, 1, 1 }, new[] { 30, 60, 90 }, new[] { 31, 61, 91 }, new[] { 100, 100, 100 }, new[] { 5, 95, 5, 95 }
    };

    [Test]
    public void CalculateAutoAttackProcDamage()
    {
        var table = new SnapshotTable(
            "Combat.CalculateAutoAttackProcDamage",
            "Total flat damage from auto-attack proc sources. A source rolls only when it has a positive chance and a non-zero bonus, and wins when the roll is at most its chance.",
            "Source sets (chance:bonus per source) x scripted rolls handed out in order; rolls beyond the script return 101 and never win. " +
            "Rolls consumed shows exactly which sources spent a roll.",
            new[] { "sources", "scriptedRolls" },
            new[] { "damage", "rollsConsumed" });

        foreach (var sourceSet in SourceSets)
        {
            foreach (var script in ScriptedRolls)
            {
                var consumed = 0;
                int Roll()
                {
                    var value = consumed < script.Length ? script[consumed] : 101;
                    consumed++;
                    return value;
                }

                var damage = Combat.CalculateAutoAttackProcDamage(BuildSources(sourceSet), Roll);
                table.Add(sourceSet, script, damage, consumed);
            }
        }

        Snapshot.Verify("Combat", "CalculateAutoAttackProcDamage", table);
    }

    [Test]
    public void CalculateAutoAttackProcDamage_Seeded()
    {
        var table = new SnapshotTable(
            "Combat.CalculateAutoAttackProcDamage (seeded d100)",
            $"The damage of {SeededTrials} consecutive auto-attacks using the game d100 as the roll; the generator restarts at the seed for each source set.",
            "Four source sets: one proc source at 50 percent, a rare large proc, and two multi-source sets with a negative bonus.",
            new[] { "sources" },
            new[] { "damagePerAttack" },
            SeededRollSeed);

        using var random = new SeededRandom(SeededRollSeed);
        foreach (var sourceSet in SeededSourceSets)
        {
            random.Reseed(SeededRollSeed);
            var sources = BuildSources(sourceSet);
            var damage = new int[SeededTrials];
            for (var i = 0; i < damage.Length; i++)
                damage[i] = Combat.CalculateAutoAttackProcDamage(sources, () => GameRandom.D100(1));

            table.Add(sourceSet, damage);
        }

        Snapshot.Verify("Combat", "CalculateAutoAttackProcDamage.Seeded", table);
    }

    private static List<StatAdjustmentSource> BuildSources(string sourceSet)
    {
        var sources = new List<StatAdjustmentSource>();
        if (sourceSet == "none")
            return sources;

        var index = 0;
        foreach (var pair in sourceSet.Split(','))
        {
            var parts = pair.Split(':');
            sources.Add(new StatAdjustmentSource($"proc-{index++}", new Dictionary<StatType, int>
            {
                [StatType.AutoAttackDamageBonusChance] = int.Parse(parts[0]),
                [StatType.AutoAttackDamageBonus] = int.Parse(parts[1])
            }));
        }

        return sources;
    }
}
