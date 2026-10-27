using System.Reflection;
using NUnit.Framework;
using SWLOR.Game.Server.CharacterizationTests.Support;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.CharacterizationTests.Service;

/// <summary>Locks the hit chance, critical chance and public tuning constants of <see cref="Combat"/>.</summary>
public class CombatHitAndCriticalCharacterizationTests
{
    [Test]
    public void CalculateHitRate()
    {
        var table = new SnapshotTable(
            "Combat.CalculateHitRate",
            "Hit chance in percent, clamped between the minimum hit rate and the maximum hit rate.",
            "Cross product of accuracy {0,7,40,100,250,100000}, evasion {0,5,40,100,300}, modifier {-100,-10,0,25,200} " +
            "and minimum {20 normal, 35 boss}; then boundary rows at both clamps and at half-point rounding; " +
            "the last rows call the overload without a minimum (column shows -1, the default 20 applies).",
            new[] { "attackerAccuracy", "defenderEvasion", "percentageModifier", "minimumHitRate" },
            new[] { "hitRate" });

        foreach (var row in Grid.Cross(
                     new[] { 0, 7, 40, 100, 250, 100000 },
                     new[] { 0, 5, 40, 100, 300 },
                     new[] { -100, -10, 0, 25, 200 },
                     new[] { 20, 35 }))
        {
            table.Add(row[0], row[1], row[2], row[3], Combat.CalculateHitRate(row[0], row[1], row[2], row[3]));
        }

        var boundaryPairs = new (int Accuracy, int Evasion)[]
        {
            (-3, 0), (-1, 0), (0, -1), (1, 0), (0, 1), (2, 0), (0, 2), (3, 0), (0, 3),
            (40, 0), (41, 0), (42, 0), (0, 79), (0, 80), (0, 81), (0, 109), (0, 110), (0, 111)
        };
        foreach (var minimum in new[] { 20, 35 })
        {
            foreach (var (accuracy, evasion) in boundaryPairs)
                table.Add(accuracy, evasion, 0, minimum, Combat.CalculateHitRate(accuracy, evasion, 0, minimum));
            foreach (var modifier in new[] { -56, -55, 20, 21 })
                table.Add(0, 0, modifier, minimum, Combat.CalculateHitRate(0, 0, modifier, minimum));
        }

        // The overload without a minimum uses the default of 20.
        foreach (var (accuracy, evasion) in boundaryPairs)
            table.Add(accuracy, evasion, 5, -1, Combat.CalculateHitRate(accuracy, evasion, 5));

        Snapshot.Verify("Combat", "CalculateHitRate", table);
    }

    [Test]
    public void CalculateCriticalRate()
    {
        var table = new SnapshotTable(
            "Combat.CalculateCriticalRate",
            "Critical chance in percent: base 5, plus skill rank / 10, plus perception advantage / 5 (0 to 3), plus modifier, clamped to 5-50.",
            "Perception/vitality pairs chosen around the divide-by-five steps and the 0-3 clamp, crossed with skill rank " +
            "{-10,0,9,10,55,100,600} and modifier {-50,0,3,100}.",
            new[] { "attackerPER", "defenderVIT", "skillRank", "criticalModifier" },
            new[] { "criticalRate" });

        var pairs = new (int Per, int Vit)[]
        {
            (0, 30), (9, 10), (10, 10), (14, 10), (15, 10), (19, 10), (20, 10), (24, 10), (25, 10), (30, 10), (60, 0), (100, 0)
        };
        foreach (var (per, vit) in pairs)
        {
            foreach (var rank in new[] { -10, 0, 9, 10, 55, 100, 600 })
            {
                foreach (var modifier in new[] { -50, 0, 3, 100 })
                    table.Add(per, vit, rank, modifier, Combat.CalculateCriticalRate(per, vit, rank, modifier));
            }
        }

        Snapshot.Verify("Combat", "CalculateCriticalRate", table);
    }

    [Test]
    public void CalculateAbilityCriticalChance()
    {
        var table = new SnapshotTable(
            "Combat.CalculateAbilityCriticalChance",
            "Ability critical chance in percent: the minimum critical rate plus the total adjustment, clamped to 5-50.",
            "Total percent adjustment values from -1000 to 1000000 including both clamp boundaries.",
            new[] { "totalPercentAdjustment" },
            new[] { "criticalChance" });

        foreach (var adjustment in new[] { -1000, -100, -5, -1, 0, 1, 20, 44, 45, 46, 100, 1000000 })
            table.Add(adjustment, Combat.CalculateAbilityCriticalChance(adjustment));

        Snapshot.Verify("Combat", "CalculateAbilityCriticalChance", table);
    }

    [Test]
    public void PublicConstants()
    {
        var table = new SnapshotTable(
            "Combat (public const fields)",
            "Every public constant of the Combat class: hit and critical rates, damage caps, attack delay tuning, " +
            "StandardCriticalRating and the engagement ranges.",
            "All public const fields of Combat, sorted by name.",
            new[] { "constant" },
            new[] { "type", "value" });

        var constants = typeof(Combat)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && !field.IsInitOnly)
            .OrderBy(field => field.Name, StringComparer.Ordinal);
        foreach (var field in constants)
            table.Add(field.Name, field.FieldType.Name, field.GetRawConstantValue());

        Snapshot.Verify("Combat", "PublicConstants", table);
    }
}
