using NUnit.Framework;
using SWLOR.Game.Server.CharacterizationTests.Support;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.CharacterizationTests.Service;

/// <summary>
/// Locks the attack cadence arithmetic of <see cref="Combat"/>: weapon delay to milliseconds, the effective delay,
/// the swing delay and how many attacks one swing resolves. The swing debt is static state keyed by attacker id,
/// so the fixture is not parallelizable.
/// </summary>
[NonParallelizable]
public class CombatAttackDelayCharacterizationTests
{
    private const uint SequenceAttackerId = 0x7F00A000;
    private const int SwingsPerSequence = 8;

    [Test]
    public void CalculateAttackDelayMilliseconds()
    {
        var table = new SnapshotTable(
            "Combat.CalculateAttackDelayMilliseconds",
            "Raw attack delay in milliseconds from the main-hand and off-hand weapon delay units (60 units per second), after haste and off-hand reduction.",
            "main-hand units {0,90,105,150,240,600} x off-hand units {0,105,150,240} x delay reduction percent {-60,-25,0,15,50} x off-hand reduction percent {0,20,70}.",
            new[] { "rightHandDelayUnits", "leftHandDelayUnits", "attackDelayReductionPercent", "offhandAttackDelayReductionPercent" },
            new[] { "delayMilliseconds" });

        foreach (var row in Grid.Cross(
                     new[] { 0, 90, 105, 150, 240, 600 },
                     new[] { 0, 105, 150, 240 },
                     new[] { -60, -25, 0, 15, 50 },
                     new[] { 0, 20, 70 }))
        {
            table.Add(row[0], row[1], row[2], row[3], Combat.CalculateAttackDelayMilliseconds(row[0], row[1], row[2], row[3]));
        }

        Snapshot.Verify("Combat", "CalculateAttackDelayMilliseconds", table);
    }

    [Test]
    public void CalculateEffectiveAttackDelay()
    {
        var table = new SnapshotTable(
            "Combat.CalculateEffectiveAttackDelay",
            "The per-attack delay the attack loop uses: the raw delay minus the engine base delay, floored at the minimum, or exactly the minimum when forced.",
            "raw delay {-100,0,1000,1749,1750,1751,2000,2332,2333,2334,3500,5000,100000} x useDefaultMinimumDelay {false,true}. " +
            "The last column is the overload without the flag.",
            new[] { "attackerDelayMilliseconds", "useDefaultMinimumDelay" },
            new[] { "effectiveDelay", "effectiveDelayWithoutFlag" });

        foreach (var delay in new[] { -100, 0, 1000, 1749, 1750, 1751, 2000, 2332, 2333, 2334, 3500, 5000, 100000 })
        {
            foreach (var useDefaultMinimum in new[] { false, true })
            {
                table.Add(delay, useDefaultMinimum,
                    Combat.CalculateEffectiveAttackDelay(delay, useDefaultMinimum),
                    Combat.CalculateEffectiveAttackDelay(delay));
            }
        }

        Snapshot.Verify("Combat", "CalculateEffectiveAttackDelay", table);
    }

    [Test]
    public void CalculateAttackSwingDelay()
    {
        var table = new SnapshotTable(
            "Combat.CalculateAttackSwingDelay",
            "The time between swing animations: never faster than the base attack delay.",
            "effective delay values around the base delay of 1750 and the minimum delay.",
            new[] { "effectiveDelayMilliseconds" },
            new[] { "swingDelay" });

        foreach (var delay in new[] { -1, 0, 1, 583, 584, 1000, 1749, 1750, 1751, 3500 })
            table.Add(delay, Combat.CalculateAttackSwingDelay(delay));

        Snapshot.Verify("Combat", "CalculateAttackSwingDelay", table);
    }

    [Test]
    public void CalculateAttacksPerSwing()
    {
        var table = new SnapshotTable(
            "Combat.CalculateAttacksPerSwing",
            "How many attacks a swing resolves (1 to 3) and the fractional attack debt carried to the next swing.",
            "effective delay {-1,0,1,584,600,875,1000,1167,1750,1751,2500,3500} x carried debt {-1,0,0.25,0.5,0.99,1,2.5,3,10}.",
            new[] { "effectiveDelayMilliseconds", "attackDebt" },
            new[] { "attacks", "updatedAttackDebt" });

        foreach (var delay in new[] { -1, 0, 1, 584, 600, 875, 1000, 1167, 1750, 1751, 2500, 3500 })
        {
            foreach (var debt in new[] { -1f, 0f, 0.25f, 0.5f, 0.99f, 1f, 2.5f, 3f, 10f })
            {
                var attacks = Combat.CalculateAttacksPerSwing(delay, debt, out var updatedDebt);
                table.Add(delay, debt, attacks, updatedDebt);
            }
        }

        Snapshot.Verify("Combat", "CalculateAttacksPerSwing", table);
    }

    [Test]
    public void CapAttacksPerSwingForLimitedAttackEffect()
    {
        var table = new SnapshotTable(
            "Combat.CapAttacksPerSwingForLimitedAttackEffect",
            "Limits an accelerated swing to the baseline attacks plus the remaining charges of a limited speed effect.",
            "accelerated {0,1,3,6} x baseline {0,1,3,7} x remaining charges {-1,0,2,5} x no-delay charges {0,1,4}.",
            new[] { "acceleratedAttacks", "baselineAttacks", "remainingAttacks", "limitedNoDelayRemainingAttacks" },
            new[] { "attacks" });

        foreach (var row in Grid.Cross(
                     new[] { 0, 1, 3, 6 },
                     new[] { 0, 1, 3, 7 },
                     new[] { -1, 0, 2, 5 },
                     new[] { 0, 1, 4 }))
        {
            table.Add(row[0], row[1], row[2], row[3],
                Combat.CapAttacksPerSwingForLimitedAttackEffect(row[0], row[1], row[2], row[3]));
        }

        Snapshot.Verify("Combat", "CapAttacksPerSwingForLimitedAttackEffect", table);
    }

    [Test]
    public void ConsumeAttacksPerSwing_Sequences()
    {
        var table = new SnapshotTable(
            "Combat.ConsumeAttacksPerSwing",
            $"Attacks resolved by each of {SwingsPerSequence} consecutive swings of one attacker, carrying fractional attack debt between swings.",
            "Scenarios: plain cadence at effective delays {584,700,875,1000,1167,1400,1750,2300} with 1 or 2 attacks per cycle; " +
            "a one-shot no-delay buff on swing 1; limited speed charges (reduction or no-delay) that the caller spends by the attacks taken; " +
            "temporary no-delay charges bound to the main hand, off hand or both. Columns: effective delay, unbuffed delay, buff on swing 1, " +
            "delay without the limited reduction, reduction charges, no-delay charges, attacks per cycle, temporary charges, temporary hands.",
            new[]
            {
                "effectiveDelay", "unbuffedDelay", "noDelayBuffOnSwing1", "delayWithoutLimitedReduction",
                "reductionCharges", "noDelayCharges", "attacksPerCycle", "temporaryCharges", "temporaryHands"
            },
            new[] { "attacksPerSwing" });

        foreach (var cycle in new[] { 1, 2 })
        {
            foreach (var delay in new[] { 584, 700, 875, 1000, 1167, 1400, 1750, 2300 })
                AddSequence(table, delay, delay, false, delay, 0, 0, cycle, 0, "none");

            foreach (var unbuffed in new[] { 584, 1000, 1750 })
                AddSequence(table, 584, unbuffed, true, 584, 0, 0, cycle, 0, "none");

            foreach (var charges in new[] { 2, 5 })
                AddSequence(table, 700, 700, false, 1750, charges, 0, cycle, 0, "none");

            foreach (var charges in new[] { 1, 3 })
                AddSequence(table, 584, 584, false, 1750, 0, charges, cycle, 0, "none");

            foreach (var hands in new[] { "main", "off", "both" })
                AddSequence(table, 584, 584, true, 584, 0, 0, cycle, 1, hands);
        }

        Snapshot.Verify("Combat", "ConsumeAttacksPerSwing", table);
    }

    private static void AddSequence(
        SnapshotTable table,
        int effectiveDelay,
        int unbuffedDelay,
        bool noDelayBuffOnFirstSwing,
        int delayWithoutLimitedReduction,
        int reductionCharges,
        int noDelayCharges,
        int attacksPerCycle,
        int temporaryCharges,
        string temporaryHands)
    {
        var temporaryBudget = new LimitedAttackTimingBudget(
            temporaryCharges,
            temporaryHands is "main" or "both",
            temporaryHands is "off" or "both");
        var attacksPerSwing = new int[SwingsPerSequence];
        var reduction = reductionCharges;
        var noDelay = noDelayCharges;

        Combat.ClearAttackSwingDebt(SequenceAttackerId);
        try
        {
            for (var swing = 0; swing < SwingsPerSequence; swing++)
            {
                var firstSwing = swing == 0;
                attacksPerSwing[swing] = Combat.ConsumeAttacksPerSwing(
                    SequenceAttackerId,
                    effectiveDelay,
                    unbuffedDelay,
                    noDelayBuffOnFirstSwing && firstSwing,
                    delayWithoutLimitedReduction,
                    reduction,
                    noDelay,
                    attacksPerCycle,
                    temporaryNoDelayBudget: firstSwing ? temporaryBudget : default);
                reduction = Math.Max(0, reduction - attacksPerSwing[swing]);
                noDelay = Math.Max(0, noDelay - attacksPerSwing[swing]);
            }
        }
        finally
        {
            Combat.ClearAttackSwingDebt(SequenceAttackerId);
        }

        table.Add(effectiveDelay, unbuffedDelay, noDelayBuffOnFirstSwing, delayWithoutLimitedReduction,
            reductionCharges, noDelayCharges, attacksPerCycle, temporaryCharges, temporaryHands, attacksPerSwing);
    }
}
