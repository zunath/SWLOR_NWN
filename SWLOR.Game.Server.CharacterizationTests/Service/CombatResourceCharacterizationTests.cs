using NUnit.Framework;
using SWLOR.Game.Server.CharacterizationTests.Support;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.CharacterizationTests.Service;

/// <summary>Locks the healing budget and stamina restore arithmetic of <see cref="Combat"/>.</summary>
public class CombatResourceCharacterizationTests
{
    [Test]
    public void CalculateMaxHPHealingBudget()
    {
        var table = new SnapshotTable(
            "Combat.CalculateMaxHPHealingBudget",
            "A percentage of maximum HP, rounded down; never negative.",
            "maximum HP {-100,0,1,70,999,1000,1000000000} x percent {-5,0,3,6,12,15,24,50,100}.",
            new[] { "maximumHP", "percent" },
            new[] { "budget" });

        foreach (var row in Grid.Cross(
                     new[] { -100, 0, 1, 70, 999, 1000, 1000000000 },
                     new[] { -5, 0, 3, 6, 12, 15, 24, 50, 100 }))
        {
            table.Add(row[0], row[1], Combat.CalculateMaxHPHealingBudget(row[0], row[1]));
        }

        Snapshot.Verify("Combat", "CalculateMaxHPHealingBudget", table);
    }

    [Test]
    public void CalculateDamageEligibleForHealing()
    {
        var table = new SnapshotTable(
            "Combat.CalculateDamageEligibleForHealing",
            "The part of a hit that can fund damage-derived healing: overkill and damage already queued against the target do not count.",
            "damage {-10,0,1,50,684,2000} x target HP {-5,0,20,100,1000} x pending damage {-5,0,40,100,2000}.",
            new[] { "damage", "targetHP", "pendingDamage" },
            new[] { "eligibleDamage" });

        foreach (var row in Grid.Cross(
                     new[] { -10, 0, 1, 50, 684, 2000 },
                     new[] { -5, 0, 20, 100, 1000 },
                     new[] { -5, 0, 40, 100, 2000 }))
        {
            table.Add(row[0], row[1], row[2], Combat.CalculateDamageEligibleForHealing(row[0], row[1], row[2]));
        }

        Snapshot.Verify("Combat", "CalculateDamageEligibleForHealing", table);
    }

    [Test]
    public void CalculateCappedDamageDerivedHealingAmount()
    {
        var table = new SnapshotTable(
            "Combat.CalculateCappedDamageDerivedHealingAmount",
            "Healing granted from one hit: the request, limited to the per-hit share of the damage minus the healing already applied by that hit.",
            "damage {-5,0,1,3,100,1001} x healing already applied {-10,0,25,50,500} x requested healing {-5,0,1,25,60,10000}.",
            new[] { "damage", "healingAlreadyApplied", "requestedHealing" },
            new[] { "healing" });

        foreach (var row in Grid.Cross(
                     new[] { -5, 0, 1, 3, 100, 1001 },
                     new[] { -10, 0, 25, 50, 500 },
                     new[] { -5, 0, 1, 25, 60, 10000 }))
        {
            table.Add(row[0], row[1], row[2], Combat.CalculateCappedDamageDerivedHealingAmount(row[0], row[1], row[2]));
        }

        Snapshot.Verify("Combat", "CalculateCappedDamageDerivedHealingAmount", table);
    }

    [Test]
    public void CalculateHitPointsSpentThroughTemporaryHitPoints()
    {
        var table = new SnapshotTable(
            "Combat.CalculateHitPointsSpentThroughTemporaryHitPoints",
            "The real HP an HP-cost payment takes once temporary HP has absorbed its share.",
            "payment {-10,0,1,50,100,5000} x temporary HP {-10,0,1,30,100,6000}.",
            new[] { "payment", "temporaryHitPoints" },
            new[] { "hitPointsSpent" });

        foreach (var row in Grid.Cross(
                     new[] { -10, 0, 1, 50, 100, 5000 },
                     new[] { -10, 0, 1, 30, 100, 6000 }))
        {
            table.Add(row[0], row[1], Combat.CalculateHitPointsSpentThroughTemporaryHitPoints(row[0], row[1]));
        }

        Snapshot.Verify("Combat", "CalculateHitPointsSpentThroughTemporaryHitPoints", table);
    }

    [Test]
    public void CalculateHitPointSpendStaminaRestore()
    {
        var table = new SnapshotTable(
            "Combat.CalculateHitPointSpendStaminaRestore",
            "Stamina restored after spending HP: a percentage of maximum stamina, scaled down when the HP payment was clipped short of its cost.",
            "maximum stamina {0,1,100,333} x percent {0,5,33,150} x HP spent {0,1,25,50,100} x HP cost {0,50,100}.",
            new[] { "maxStamina", "percent", "hitPointsSpent", "hitPointCost" },
            new[] { "staminaRestore" });

        foreach (var row in Grid.Cross(
                     new[] { 0, 1, 100, 333 },
                     new[] { 0, 5, 33, 150 },
                     new[] { 0, 1, 25, 50, 100 },
                     new[] { 0, 50, 100 }))
        {
            table.Add(row[0], row[1], row[2], row[3], Combat.CalculateHitPointSpendStaminaRestore(row[0], row[1], row[2], row[3]));
        }

        Snapshot.Verify("Combat", "CalculateHitPointSpendStaminaRestore", table);
    }

    [Test]
    public void CalculateAbilityHitStaminaRestore()
    {
        var table = new SnapshotTable(
            "Combat.CalculateAbilityHitStaminaRestore",
            "Stamina an ability hit can refund: the request, limited so at least 1 stamina of the cost is always kept.",
            "ability stamina cost {-5,0,1,2,10,40} x already refunded {-5,0,3,9,10,50} x requested {-5,0,1,5,100}.",
            new[] { "cost", "refunded", "requested" },
            new[] { "restore" });

        foreach (var row in Grid.Cross(
                     new[] { -5, 0, 1, 2, 10, 40 },
                     new[] { -5, 0, 3, 9, 10, 50 },
                     new[] { -5, 0, 1, 5, 100 }))
        {
            table.Add(row[0], row[1], row[2], Combat.CalculateAbilityHitStaminaRestore(row[0], row[1], row[2]));
        }

        Snapshot.Verify("Combat", "CalculateAbilityHitStaminaRestore", table);
    }
}
