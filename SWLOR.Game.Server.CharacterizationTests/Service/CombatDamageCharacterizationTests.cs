using NUnit.Framework;
using SWLOR.Game.Server.CharacterizationTests.Support;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.CharacterizationTests.Service;

/// <summary>
/// Locks the damage roll and the pure damage adjustment helpers of <see cref="Combat"/>. Fixtures that seed the
/// game random generator are not parallelizable.
/// </summary>
[NonParallelizable]
public class CombatDamageCharacterizationTests
{
    private const int DamageRollSeed = 1701;
    private const int DamageRollsPerCase = 24;
    private const int LongRunSeed = 1702;
    private const int LongRunRolls = 500;
    private const int LongRunChunk = 25;

    private static readonly string[] DamageInputColumns =
    {
        "attackerAttack", "attackerDMG", "attackerStat", "defenderDefense", "defenderStat", "critical", "deltaCap"
    };

    [Test]
    public void CalculateDamageRange()
    {
        var table = new SnapshotTable(
            "Combat.CalculateDamageRange",
            "Minimum and maximum damage of one attack from the attack/defense ratio (clamped), DMG, the capped stat delta and the critical rating.",
            "Three families. (1) attack {0,20,100,400,100000} x defense {-5,0,1,40,100,300} x DMG {0,1,12,45} with stats 10/10, no critical, no cap. " +
            "(2) attack 100, defense 100, stat pairs {10/10,60/10,10/60,200/0,0/200} x deltaCap {-1,0,1,5,20} x DMG {0,12,45}. " +
            "(3) critical {-1,0,1,2,3,4,5,8} x DMG {0,1,12,45,150} x attack/defense {100/100,400/100,20/300}, stats 10/10.",
            DamageInputColumns,
            new[] { "minDamage", "maxDamage" });

        void AddRow(int attack, int dmg, int attackerStat, int defense, int defenderStat, int critical, int deltaCap)
        {
            var (min, max) = Combat.CalculateDamageRange(attack, dmg, attackerStat, defense, defenderStat, critical, deltaCap);
            table.Add(attack, dmg, attackerStat, defense, defenderStat, critical, deltaCap, min, max);
        }

        foreach (var row in Grid.Cross(
                     new[] { 0, 20, 100, 400, 100000 },
                     new[] { -5, 0, 1, 40, 100, 300 },
                     new[] { 0, 1, 12, 45 }))
        {
            AddRow(row[0], row[2], 10, row[1], 10, 0, 0);
        }

        var statPairs = new (int Attacker, int Defender)[] { (10, 10), (60, 10), (10, 60), (200, 0), (0, 200) };
        foreach (var (attackerStat, defenderStat) in statPairs)
        {
            foreach (var row in Grid.Cross(new[] { -1, 0, 1, 5, 20 }, new[] { 0, 12, 45 }))
                AddRow(100, row[1], attackerStat, 100, defenderStat, 0, row[0]);
        }

        var ratios = new (int Attack, int Defense)[] { (100, 100), (400, 100), (20, 300) };
        foreach (var (attack, defense) in ratios)
        {
            foreach (var row in Grid.Cross(new[] { -1, 0, 1, 2, 3, 4, 5, 8 }, new[] { 0, 1, 12, 45, 150 }))
                AddRow(attack, row[1], 10, defense, 10, row[0], 0);
        }

        Snapshot.Verify("Combat", "CalculateDamageRange", table);
    }

    [Test]
    public void CalculateDamage_SeededSequences()
    {
        var table = new SnapshotTable(
            "Combat.CalculateDamage",
            $"A random damage roll inside the damage range. Every row restarts the generator at the seed, then records {DamageRollsPerCase} consecutive rolls.",
            "attack/defense {100/100,400/100,20/300,100000/1} x DMG {0,12,45} x critical {0,2}, stats 40/10 and deltaCap 5; " +
            "then zero attack, no cap, critical 5, a negative defense and a zero-DMG critical.",
            DamageInputColumns,
            new[] { "rangeMin", "rangeMax", "rolls" },
            DamageRollSeed);

        var cases = new List<int[]>();
        var ratios = new (int Attack, int Defense)[] { (100, 100), (400, 100), (20, 300), (100000, 1) };
        foreach (var (attack, defense) in ratios)
        {
            foreach (var row in Grid.Cross(new[] { 0, 12, 45 }, new[] { 0, 2 }))
                cases.Add(new[] { attack, row[0], 40, defense, 10, row[1], 5 });
        }

        cases.Add(new[] { 0, 12, 40, 100, 10, 0, 5 });
        cases.Add(new[] { 100, 45, 40, 100, 10, 0, 0 });
        cases.Add(new[] { 100, 45, 40, 100, 10, 5, 0 });
        cases.Add(new[] { 100, 12, 10, -5, 10, 0, 0 });
        cases.Add(new[] { 100, 0, 40, 100, 10, 2, 5 });

        using var random = new SeededRandom(DamageRollSeed);
        foreach (var c in cases)
        {
            random.Reseed(DamageRollSeed);
            var (min, max) = Combat.CalculateDamageRange(c[0], c[1], c[2], c[3], c[4], c[5], c[6]);
            var rolls = new int[DamageRollsPerCase];
            for (var i = 0; i < rolls.Length; i++)
                rolls[i] = Combat.CalculateDamage(c[0], c[1], c[2], c[3], c[4], c[5], c[6]);

            table.Add(c[0], c[1], c[2], c[3], c[4], c[5], c[6], min, max, rolls);
        }

        Snapshot.Verify("Combat", "CalculateDamage", table);
    }

    [Test]
    public void CalculateDamage_LongRun()
    {
        var table = new SnapshotTable(
            "Combat.CalculateDamage (long run)",
            $"One uninterrupted stream of {LongRunRolls} rolls per case from a single seed, recorded {LongRunChunk} per row, to catch drift in how the generator is consumed.",
            "Case typical: attack 100, DMG 45, stats 40/10, defense 100, cap 5. Case critical: the same with critical rating 2. " +
            "The generator is restarted at the seed for each case.",
            new[] { "case", "firstRollIndex" },
            new[] { "rolls" },
            LongRunSeed);

        using var random = new SeededRandom(LongRunSeed);
        foreach (var (label, critical) in new[] { ("typical", 0), ("critical", 2) })
        {
            random.Reseed(LongRunSeed);
            for (var start = 0; start < LongRunRolls; start += LongRunChunk)
            {
                var rolls = new int[LongRunChunk];
                for (var i = 0; i < rolls.Length; i++)
                    rolls[i] = Combat.CalculateDamage(100, 45, 40, 100, 10, critical, 5);

                table.Add(label, start, rolls);
            }
        }

        Snapshot.Verify("Combat", "CalculateDamage.LongRun", table);
    }

    [Test]
    public void ClampCriticalDamagePercentAdjustment()
    {
        var table = new SnapshotTable(
            "Combat.ClampCriticalDamagePercentAdjustment",
            "Caps the total critical damage bonus at the maximum; negative adjustments pass through.",
            "Values around the cap of 200 plus the int extremes.",
            new[] { "adjustment" },
            new[] { "clamped" });

        foreach (var adjustment in new[] { int.MinValue, -1000, -100, -1, 0, 1, 50, 199, 200, 201, 1000, int.MaxValue })
            table.Add(adjustment, Combat.ClampCriticalDamagePercentAdjustment(adjustment));

        Snapshot.Verify("Combat", "ClampCriticalDamagePercentAdjustment", table);
    }

    [Test]
    public void ApplyOrdinaryAttackDamageMultiplier()
    {
        var table = new SnapshotTable(
            "Combat.ApplyOrdinaryAttackDamageMultiplier(int, int)",
            "Scales damage by (100 + percent) / 100, rounding down; never negative.",
            "damage {-5,0,1,7,27,100,1000,2147483647} x percent {-1000,-100,-50,-15,-1,0,25,100,300}.",
            new[] { "damage", "percentAdjustment" },
            new[] { "damageAfterMultiplier" });

        foreach (var row in Grid.Cross(
                     new[] { -5, 0, 1, 7, 27, 100, 1000, int.MaxValue },
                     new[] { -1000, -100, -50, -15, -1, 0, 25, 100, 300 }))
        {
            table.Add(row[0], row[1], Combat.ApplyOrdinaryAttackDamageMultiplier(row[0], row[1]));
        }

        Snapshot.Verify("Combat", "ApplyOrdinaryAttackDamageMultiplier", table);
    }

    [Test]
    public void CapOutgoingDamageBonus()
    {
        var table = new SnapshotTable(
            "Combat.CapOutgoingDamageBonus",
            "Caps percentage bonuses on base damage at the maximum bonus percent, then adds flat bonus damage; at least 1 for a positive base.",
            "baseDamage {0,1,10,100,1000000} x adjustedDamage {0,10,15,100,250} x flatBonus {-20,0,5} x maximumBonusPercent {100,25,0}.",
            new[] { "baseDamage", "adjustedDamage", "flatBonus", "maximumBonusPercent" },
            new[] { "damage" });

        foreach (var row in Grid.Cross(
                     new[] { 0, 1, 10, 100, 1000000 },
                     new[] { 0, 10, 15, 100, 250 },
                     new[] { -20, 0, 5 },
                     new[] { 100, 25, 0 }))
        {
            table.Add(row[0], row[1], row[2], row[3], Combat.CapOutgoingDamageBonus(row[0], row[1], row[2], row[3]));
        }

        Snapshot.Verify("Combat", "CapOutgoingDamageBonus", table);
    }

    [Test]
    public void ApplyCombinedDamageTakenAdjustment()
    {
        var table = new SnapshotTable(
            "Combat.ApplyCombinedDamageTakenAdjustment",
            "Caps the combined target-status and generic damage reduction; the result is at least 1 for positive damage.",
            "damage {-3,0,1,10,100,1000} x targetStatusAdjustment {-120,-85,-40,0,30} x genericAdjustment {-120,-95,-50,-10,0,60}.",
            new[] { "damage", "targetStatusAdjustment", "genericAdjustment" },
            new[] { "damage" });

        foreach (var row in Grid.Cross(
                     new[] { -3, 0, 1, 10, 100, 1000 },
                     new[] { -120, -85, -40, 0, 30 },
                     new[] { -120, -95, -50, -10, 0, 60 }))
        {
            table.Add(row[0], row[1], row[2], Combat.ApplyCombinedDamageTakenAdjustment(row[0], row[1], row[2]));
        }

        Snapshot.Verify("Combat", "ApplyCombinedDamageTakenAdjustment", table);
    }

    [Test]
    public void ApplyPercentDamageAdjustment()
    {
        var table = new SnapshotTable(
            "Combat.ApplyPercentDamageAdjustment",
            "Raises or lowers damage by a percentage, rounding the change up; reductions are limited to 95 percent and the result is at least 1.",
            "damage {-5,0,1,2,3,10,99,100,1001,1000000} x adjustment {-1000,-100,-95,-50,-33,-1,0,1,33,100,250}.",
            new[] { "damage", "adjustment" },
            new[] { "damage" });

        foreach (var row in Grid.Cross(
                     new[] { -5, 0, 1, 2, 3, 10, 99, 100, 1001, 1000000 },
                     new[] { -1000, -100, -95, -50, -33, -1, 0, 1, 33, 100, 250 }))
        {
            table.Add(row[0], row[1], Combat.ApplyPercentDamageAdjustment(row[0], row[1]));
        }

        Snapshot.Verify("Combat", "ApplyPercentDamageAdjustment", table);
    }

    [Test]
    public void ApplyTargetHPDamageAdjustment()
    {
        var table = new SnapshotTable(
            "Combat.ApplyTargetHPDamageAdjustment",
            "Adjusts damage by a percentage when the target is at or below a health threshold.",
            "damage {0,10,100} x current/max HP {0/100,25/100,30/100,31/100,100/100,50/0,5/-10,100/50} x threshold {0,30,100} x adjustment {0,-40,50}.",
            new[] { "damage", "currentHP", "maxHP", "thresholdPercent", "adjustmentPercent" },
            new[] { "damage" });

        var hitPoints = new (int Current, int Max)[]
        {
            (0, 100), (25, 100), (30, 100), (31, 100), (100, 100), (50, 0), (5, -10), (100, 50)
        };
        foreach (var damage in new[] { 0, 10, 100 })
        {
            foreach (var (current, max) in hitPoints)
            {
                foreach (var row in Grid.Cross(new[] { 0, 30, 100 }, new[] { 0, -40, 50 }))
                {
                    table.Add(damage, current, max, row[0], row[1],
                        Combat.ApplyTargetHPDamageAdjustment(damage, current, max, row[0], row[1]));
                }
            }
        }

        Snapshot.Verify("Combat", "ApplyTargetHPDamageAdjustment", table);
    }

    [Test]
    public void ApplyDefenseIgnore()
    {
        var table = new SnapshotTable(
            "Combat.ApplyDefenseIgnore",
            "Removes a percentage of a defense value, rounding the removed amount up; no effect on non-positive defense or percent.",
            "defense {-5,0,1,3,10,99,100,1000,100000} x ignore percent {-10,0,1,10,33,50,99,100,150}.",
            new[] { "defense", "defenseIgnorePercent" },
            new[] { "defense" });

        foreach (var row in Grid.Cross(
                     new[] { -5, 0, 1, 3, 10, 99, 100, 1000, 100000 },
                     new[] { -10, 0, 1, 10, 33, 50, 99, 100, 150 }))
        {
            table.Add(row[0], row[1], Combat.ApplyDefenseIgnore(row[0], row[1]));
        }

        Snapshot.Verify("Combat", "ApplyDefenseIgnore", table);
    }

    [Test]
    public void GetIncomingPhysicalToForceConversionPortion()
    {
        var table = new SnapshotTable(
            "Combat.GetIncomingPhysicalToForceConversionPortion",
            "How much of an incoming physical hit is re-typed to Force: rounded to the nearest point (away from zero), never more than the hit.",
            "physical damage {-5,0,1,2,3,5,10,33,100,1001} x conversion percent {-10,0,1,15,25,33,50,75,100,150}.",
            new[] { "physicalDamage", "conversionPercent" },
            new[] { "forcePortion" });

        foreach (var row in Grid.Cross(
                     new[] { -5, 0, 1, 2, 3, 5, 10, 33, 100, 1001 },
                     new[] { -10, 0, 1, 15, 25, 33, 50, 75, 100, 150 }))
        {
            table.Add(row[0], row[1], Combat.GetIncomingPhysicalToForceConversionPortion(row[0], row[1]));
        }

        Snapshot.Verify("Combat", "GetIncomingPhysicalToForceConversionPortion", table);
    }

    [Test]
    public void GetRangedDeflectionReflectionAmount()
    {
        var table = new SnapshotTable(
            "Combat.GetRangedDeflectionReflectionAmount",
            "Damage reflected by Deflecting Return: a percentage of the DMG of the attack weapon, capped at a percentage of the DMG of the deflector weapon.",
            "attack weapon DMG {-5,0,10,45,200} x reflect percent {-10,0,25,50,150} x deflector weapon DMG {0,10,30} x cap percent {0,40,100}.",
            new[] { "attackWeaponDamage", "reflectPercent", "deflectorWeaponDamage", "capPercent" },
            new[] { "reflected" });

        foreach (var row in Grid.Cross(
                     new[] { -5, 0, 10, 45, 200 },
                     new[] { -10, 0, 25, 50, 150 },
                     new[] { 0, 10, 30 },
                     new[] { 0, 40, 100 }))
        {
            table.Add(row[0], row[1], row[2], row[3], Combat.GetRangedDeflectionReflectionAmount(row[0], row[1], row[2], row[3]));
        }

        Snapshot.Verify("Combat", "GetRangedDeflectionReflectionAmount", table);
    }

    [Test]
    public void GetRangedDeflectionReflectionRates()
    {
        var table = new SnapshotTable(
            "Combat.GetRangedDeflectionReflectionRates",
            "The reflect and cap percentages for Deflecting Return: base values, an Embattled high-stack bonus, or a capstone override.",
            "Every combination of: base reflect {0,25}, base cap {-10,50}, Embattled stacks {4,5}, high-stack threshold {0,5}, " +
            "high-stack bonus {-10,15}, override reflect {0,80}, override cap {0,60}.",
            new[]
            {
                "baseReflectPercent", "baseCapPercent", "embattledStacks", "embattledHighStackThreshold",
                "embattledHighStackBonusPercent", "overrideReflectPercent", "overrideCapPercent"
            },
            new[] { "reflectPercent", "capPercent" });

        foreach (var row in Grid.Cross(
                     new[] { 0, 25 }, new[] { -10, 50 }, new[] { 4, 5 }, new[] { 0, 5 },
                     new[] { -10, 15 }, new[] { 0, 80 }, new[] { 0, 60 }))
        {
            var (reflect, cap) = Combat.GetRangedDeflectionReflectionRates(row[0], row[1], row[2], row[3], row[4], row[5], row[6]);
            table.Add(row[0], row[1], row[2], row[3], row[4], row[5], row[6], reflect, cap);
        }

        Snapshot.Verify("Combat", "GetRangedDeflectionReflectionRates", table);
    }
}
