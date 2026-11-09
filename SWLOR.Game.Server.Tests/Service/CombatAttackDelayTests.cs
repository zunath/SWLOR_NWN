using SWLOR.Game.Server.EngineTests.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.Game.Server.Domain.Items;
using static SWLOR.Game.Server.Composition.GameServices;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Tests.Service;

public class CombatAttackDelayTests
{
    // The Item facade resolves the item service from a container.
    private FacadeContainer _itemFacades = null!;
    private IDisposable _itemFacadeScope = null!;

    [OneTimeSetUp]
    public void BuildItemFacades() => _itemFacades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeItemFacades() => _itemFacades.Dispose();

    [SetUp]
    public void InstallItemFacades() => _itemFacadeScope = _itemFacades.Install();

    [TearDown]
    public void RemoveItemFacades() => _itemFacadeScope.Dispose();

    [TestCase(true, false, 5)]
    [TestCase(false, true, 6)]
    [TestCase(true, true, 5)]
    public void TemporaryNoDelayAtMinimumDelay_GrantsTheMatchingHandAnExtraRoll(bool main, bool off, int expected)
    {
        const uint attacker = 0x7F000025;
        GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker);
        try
        {
            var delay = CombatRules.MinimumAttackDelayMilliseconds;
            GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(attacker, delay, delay, true, delay, 0, 0, 2,
                temporaryNoDelayBudget: new LimitedAttackTimingBudget(1, main, off)).Should().Be(expected,
                "an off-hand proc cannot be spent on the odd extra main-hand roll");
        }
        finally { GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker); }
    }

    [Test]
    public void TemporaryOffHandNoDelay_IsNotCappedByAnExpiringHasteEffect()
    {
        const uint attacker = 0x7F000026;
        GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker);
        try
        {
            var delay = CombatRules.MinimumAttackDelayMilliseconds;
            GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(attacker, delay, delay, true, delay, 1, 0, 2,
                limitedReductionBudget: new LimitedAttackTimingBudget(1, false, true),
                temporaryNoDelayBudget: new LimitedAttackTimingBudget(1, false, true)).Should().Be(6);
            GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(attacker, delay, delay, false, delay, 0, 0, 2).Should().Be(4,
                "expiring haste must not restore fractional progress already spent by the temporary bonus");
        }
        finally { GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker); }
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void MixedDualWieldHaste_CountsOnlyTheMatchingHandAndKeepsItsDebt(bool main, bool off)
    {
        const uint attacker = 0x7F000023;
        GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker);
        try
        {
            GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(attacker, 1000, 1750, false, 1750, 4, 0, 2,
                new LimitedAttackTimingBudget(4, main, off)).Should().Be(2);
            GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(attacker, 1000, 1750, false, 1750, 3, 0, 2,
                new LimitedAttackTimingBudget(3, main, off)).Should().Be(4,
                "the first pair spent only one charge, so fractional accelerated progress remains available");
            GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(attacker, 1000, 1750, false, 1750, 1, 0, 2,
                new LimitedAttackTimingBudget(1, main, off)).Should().Be(main ? 2 : 3);
            GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(attacker, 1750, 1750, false, 1750, 0, 0, 2)
                .Should().Be(2, "acceleration ends when the matching hand spends its last charge");
        }
        finally { GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker); }
    }

    [TestCase(true, false, 3)]
    [TestCase(false, true, 4)]
    [TestCase(true, true, 3)]
    public void ScopedNoDelay_GrantsAnExtraRollToTheMatchingHand(bool main, bool off, int expected)
    {
        const uint attacker = 0x7F000024;
        GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker);
        try
        {
            GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(attacker, 584, 1750, true, 1750, 0, 1, 2,
                limitedNoDelayBudget: new LimitedAttackTimingBudget(1, main, off)).Should().Be(expected);
        }
        finally { GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker); }
    }

    [TestCase(3500, 2)]
    [TestCase(1750, 2)]
    [TestCase(875, 4)]
    public void DualWieldCycles_ScheduleBothHandsOnTheSharedTimer(int delay, int expected)
    {
        const uint attacker = 0x7F000020;
        GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker);
        try
        {
            GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(attacker, delay, delay, false, delay, 0, 0, 2)
                .Should().Be(expected);
        }
        finally { GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker); }
    }

    [TestCase(1, 2)]
    [TestCase(2, 2)]
    [TestCase(3, 3)]
    [TestCase(4, 4)]
    public void DualWieldLimitedHaste_CapsActualWeaponRolls(int charges, int expected)
    {
        const uint attacker = 0x7F000021;
        GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker);
        try
        {
            GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(attacker, 750, 1750, false, 1750, charges, 0, 2)
                .Should().Be(expected, "baseline hands remain available but haste cannot double its charged rolls");
            var next = GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(attacker, 1750, 1750, false, 1750, 0, 0, 2);
            next.Should().Be(2, "expired acceleration must not leak into the next cycle");
        }
        finally { GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker); }
    }

    [Test]
    public void DualWieldFinalNoDelayCharge_GrantsOneExtraRollInsteadOfAnExtraPair()
    {
        const uint attacker = 0x7F000022;
        GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker);
        try
        {
            GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(attacker, 584, 1750, true, 1750, 0, 1, 2)
                .Should().Be(3);
        }
        finally { GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker); }
    }

    [Test]
    public void CalculateAttackDelayMilliseconds_UsesSingleWeaponDelay()
    {
        var delay = CombatRules.CalculateAttackDelayMilliseconds(210, 0, 0, 0);

        delay.Should().Be(3500);
        CombatRules.CalculateEffectiveAttackDelay(delay).Should().Be(1750);
    }

    [Test]
    public void CalculateAttackDelayMilliseconds_DualWieldCountsDefaultDelayOnce()
    {
        var delay = CombatRules.CalculateAttackDelayMilliseconds(210, 210, 0, 0);

        delay.Should().Be(5250);
        CombatRules.CalculateEffectiveAttackDelay(delay).Should().Be(3500);
    }

    [Test]
    public void CalculateAttackDelayMilliseconds_AppliesOffhandReductionBeforeCombiningDualWieldDelay()
    {
        var delay = CombatRules.CalculateAttackDelayMilliseconds(210, 210, 0, 30);

        delay.Should().Be(4200);
        CombatRules.CalculateEffectiveAttackDelay(delay).Should().Be(2450);
    }

    [Test]
    public void CalculateEffectiveAttackDelay_SubtractsDefaultDelayFromHigherAttackerDelay()
    {
        var attackerDelay = CombatRules.BaseAttackDelayMilliseconds + 2500;

        var effectiveDelay = CombatRules.CalculateEffectiveAttackDelay(attackerDelay);

        effectiveDelay.Should().Be(2500);
    }

    [Test]
    public void CalculateEffectiveAttackDelay_AllowsDelaysBelowDefaultAfterBaselineSubtraction()
    {
        var attackerDelay = CombatRules.BaseAttackDelayMilliseconds + 1250;

        var effectiveDelay = CombatRules.CalculateEffectiveAttackDelay(attackerDelay);

        effectiveDelay.Should().Be(1250);
    }

    [Test]
    public void CalculateAttackDelayMilliseconds_FastestWeaponDelayCanBenefitFromHaste()
    {
        var unmodifiedDelay = CombatRules.CalculateAttackDelayMilliseconds(220, 0, 0, 0);
        var hastenOneDelay = CombatRules.CalculateAttackDelayMilliseconds(220, 0, 15, 0);
        var hastenTwoDelay = CombatRules.CalculateAttackDelayMilliseconds(220, 0, 25, 0);

        CombatRules.CalculateEffectiveAttackDelay(unmodifiedDelay).Should().BeGreaterThan(CombatRules.BaseAttackDelayMilliseconds);
        CombatRules.CalculateEffectiveAttackDelay(hastenOneDelay).Should().BeGreaterThan(CombatRules.MinimumAttackDelayMilliseconds);
        CombatRules.CalculateEffectiveAttackDelay(hastenTwoDelay).Should().BeGreaterThan(CombatRules.MinimumAttackDelayMilliseconds);
        CombatRules.CalculateEffectiveAttackDelay(hastenOneDelay).Should().BeLessThan(CombatRules.CalculateEffectiveAttackDelay(unmodifiedDelay));
        CombatRules.CalculateEffectiveAttackDelay(hastenTwoDelay).Should().BeLessThan(CombatRules.CalculateEffectiveAttackDelay(hastenOneDelay));
    }

    [Test]
    public void CalculateAttackDelayMilliseconds_NegativeHasteIncreasesDelay()
    {
        var unmodifiedDelay = CombatRules.CalculateAttackDelayMilliseconds(210, 0, 0, 0);
        var slowedDelay = CombatRules.CalculateAttackDelayMilliseconds(210, 0, -10, 0);

        unmodifiedDelay.Should().Be(3500);
        slowedDelay.Should().Be(3850);
        CombatRules.CalculateEffectiveAttackDelay(slowedDelay)
            .Should()
            .BeGreaterThan(CombatRules.CalculateEffectiveAttackDelay(unmodifiedDelay));
    }

    [Test]
    public void NaturalWeaponDelay_UsesFastestWeaponDelayAndBenefitsFromHaste()
    {
        var naturalWeaponTypes = new[]
        {
            BaseItem.CreatureSlashWeapon,
            BaseItem.CreaturePierceWeapon,
            BaseItem.CreatureBludgeonWeapon,
            BaseItem.CreatureSlashPierceWeapon
        };

        foreach (var naturalWeaponType in naturalWeaponTypes)
        {
            WeaponDelay.GetWeaponDelay(naturalWeaponType).Should().Be(ItemPropertyAttackDelay.Delay240);
        }

        var unmodifiedDelay = CombatRules.CalculateAttackDelayMilliseconds(240, 0, 0, 0);
        var hastenOneDelay = CombatRules.CalculateAttackDelayMilliseconds(240, 0, 15, 0);
        var hastenTwoDelay = CombatRules.CalculateAttackDelayMilliseconds(240, 0, 25, 0);

        CombatRules.CalculateEffectiveAttackDelay(unmodifiedDelay).Should().Be(2250);
        CombatRules.CalculateEffectiveAttackDelay(hastenOneDelay).Should().Be(1650);
        CombatRules.CalculateEffectiveAttackDelay(hastenTwoDelay).Should().Be(1250);
    }

    [Test]
    public void LegacySlingPistolDelay_UsesPistolDelay()
    {
        WeaponDelay.GetWeaponDelay(BaseItem.Sling).Should().Be(ItemPropertyAttackDelay.Delay250);
    }

    [Test]
    public void CalculateEffectiveAttackDelay_DualWieldPreservesItsRateAdvantageAtTheSingleWeaponFloor()
    {
        var delay = CombatRules.CalculateAttackDelayMilliseconds(210, 210, 45, 30);

        CombatRules.CalculateEffectiveAttackDelay(delay).Should().Be(818);
    }

    [Test]
    public void CalculateEffectiveAttackDelay_UsesDefaultDelayWhenAttackerDelayIsSameOrLower()
    {
        var attackerDelays = new[]
        {
            0,
            CombatRules.BaseAttackDelayMilliseconds - 1,
            CombatRules.BaseAttackDelayMilliseconds
        };

        foreach (var attackerDelay in attackerDelays)
        {
            var effectiveDelay = CombatRules.CalculateEffectiveAttackDelay(attackerDelay);

            effectiveDelay.Should().Be(CombatRules.BaseAttackDelayMilliseconds);
        }
    }

    [Test]
    public void MinimumAttackDelay_SupportsMaxAttacksPerSwingWithoutOverflow()
    {
        CombatRules.MinimumAttackDelayMilliseconds.Should().Be(584);
        (CombatRules.BaseAttackDelayMilliseconds / (float)CombatRules.MinimumAttackDelayMilliseconds)
            .Should()
            .BeLessThanOrEqualTo(CombatRules.MaxAttacksPerSwing);
    }

    [Test]
    public void CalculateAttackSwingDelay_FloorsAtBaseDelay()
    {
        CombatRules.CalculateAttackSwingDelay(584).Should().Be(CombatRules.BaseAttackDelayMilliseconds);
        CombatRules.CalculateAttackSwingDelay(CombatRules.BaseAttackDelayMilliseconds).Should().Be(CombatRules.BaseAttackDelayMilliseconds);
        CombatRules.CalculateAttackSwingDelay(2500).Should().Be(2500);
    }

    [Test]
    public void CalculateAttacksPerSwing_ResolvesOneAttackWhenDelayAtOrAboveSwingFloor()
    {
        foreach (var effectiveDelay in new[] { CombatRules.BaseAttackDelayMilliseconds, 2500, 5000 })
        {
            var attacks = CombatRules.CalculateAttacksPerSwing(effectiveDelay, 0f, out var attackDebt);

            attacks.Should().Be(1);
            attackDebt.Should().Be(0f);
        }
    }

    [Test]
    public void CalculateAttacksPerSwing_ResolvesTwoAttacksWhenDelayIsHalfSwingFloor()
    {
        var attacks = CombatRules.CalculateAttacksPerSwing(CombatRules.BaseAttackDelayMilliseconds / 2, 0f, out var attackDebt);

        attacks.Should().Be(2);
        attackDebt.Should().BeApproximately(0f, 0.01f);
    }

    [Test]
    public void CalculateAttacksPerSwing_CarriesFractionalAttacksBetweenSwings()
    {
        // 1000ms delay = 1.75 attacks per 1750ms swing; long-run average must match.
        const int effectiveDelay = 1000;
        var attackDebt = 0f;
        var totalAttacks = 0;
        const int swings = 100;

        for (var i = 0; i < swings; i++)
        {
            totalAttacks += CombatRules.CalculateAttacksPerSwing(effectiveDelay, attackDebt, out attackDebt);
        }

        var expectedAttacks = swings * (CombatRules.BaseAttackDelayMilliseconds / (float)effectiveDelay);
        totalAttacks.Should().BeCloseTo((int)expectedAttacks, 2);
    }

    // A no-delay buff (Venom Tempo, Fast Strikes, the Lightsaber critical no-delay, Last Word) is
    // applied by lowering the effective delay to MinimumAttackDelayMilliseconds. That is a no-op for
    // a build already at that floor, so ConsumeAttacksPerSwing guarantees the buff is worth an extra
    // attack regardless of how fast the attacker already swings.

    [TestCase(1750, TestName = "NoDelayBuff_GrantsExtraAttack_SlowBuild")]
    [TestCase(3500, TestName = "NoDelayBuff_GrantsExtraAttack_DualWieldBuild")]
    [TestCase(875, TestName = "NoDelayBuff_GrantsExtraAttack_HastedBuild")]
    [TestCase(750, TestName = "NoDelayBuff_GrantsExtraAttack_HeavilyHastedBuild")]
    public void ConsumeAttacksPerSwing_NoDelayBuffAlwaysBeatsTheUnbuffedSwing(int unbuffedDelay)
    {
        const uint attacker = 0x7F000001;
        GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker);
        var unbuffed = GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(attacker, unbuffedDelay, unbuffedDelay, false);

        GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker);
        var buffed = GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(
            attacker,
            CombatRules.MinimumAttackDelayMilliseconds,
            unbuffedDelay,
            true);

        GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker);

        buffed.Should().BeGreaterThan(
            unbuffed,
            "a no-delay buff must be worth at least one extra attack even when the attacker is " +
            "already at the attack-delay floor");
        buffed.Should().BeLessThanOrEqualTo(CombatRules.MaxAttacksPerSwing);
    }

    [Test]
    public void ConsumeAttacksPerSwing_AtTheDelayFloor_NoDelayBuffIsNotANoOp()
    {
        // Regression: a heavily hasted or dual-wielding Vibroknife build sits at the floor already,
        // so overriding the delay with the same floor value used to change nothing at all.
        // Both delays are genuinely equal here, which is what the real attack path produces for this
        // build - so the buff state has to travel as its own flag rather than be inferred.
        const uint attacker = 0x7F000002;
        GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker);
        var unbuffed = GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(
            attacker,
            CombatRules.MinimumAttackDelayMilliseconds,
            CombatRules.MinimumAttackDelayMilliseconds,
            false);

        GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker);
        var buffed = GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(
            attacker,
            CombatRules.MinimumAttackDelayMilliseconds,
            CombatRules.MinimumAttackDelayMilliseconds,
            true);

        GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker);

        unbuffed.Should().Be(2);
        buffed.Should().Be(3);
    }

    [Test]
    public void ConsumeAttacksPerSwing_WithoutABuff_IsUnchanged()
    {
        // The guarantee must only engage when a no-delay buff is actually active, so ordinary swings
        // keep their existing attack counts and debt accounting.
        const uint attacker = 0x7F000003;
        foreach (var delay in new[] { 3500, 1750, 875, CombatRules.MinimumAttackDelayMilliseconds })
        {
            GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker);
            var viaOverload = GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(attacker, delay, delay, false);

            GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker);
            var viaOriginal = GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(attacker, delay);

            GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker);
            viaOverload.Should().Be(viaOriginal, $"delay {delay} should be unaffected by the guarantee");
        }
    }

    [TestCase(2, 1, 1, 1)]
    [TestCase(3, 1, 2, 2)]
    [TestCase(3, 2, 1, 2)]
    [TestCase(3, 2, 3, 3)]
    public void CapAttacksPerSwingForLimitedAttackEffect_DoesNotOverscheduleCharges(
        int acceleratedAttacks,
        int baselineAttacks,
        int remainingAttacks,
        int expectedAttacks)
    {
        CombatRules.CapAttacksPerSwingForLimitedAttackEffect(
                acceleratedAttacks,
                baselineAttacks,
                remainingAttacks)
            .Should().Be(expectedAttacks);
    }

    [TestCase(2, 1, 1, 2)]
    [TestCase(3, 2, 1, 3)]
    [TestCase(4, 1, 3, 3)]
    public void CapAttacksPerSwingForLimitedNoDelay_PreservesTheGuaranteedExtraRoll(
        int acceleratedAttacks,
        int baselineAttacks,
        int remainingAttacks,
        int expectedAttacks)
    {
        CombatRules.CapAttacksPerSwingForLimitedAttackEffect(
                acceleratedAttacks,
                baselineAttacks,
                0,
                remainingAttacks)
            .Should().Be(expectedAttacks);
    }

    [Test]
    public void ConsumeAttacksPerSwing_DropsAcceleratedDebtWhenTheSwingConsumesEveryRemainingCharge()
    {
        const uint attacker = 0x7F000004;
        const int acceleratedDelay = 750;
        const int baselineDelay = 1750;
        const int remainingCharges = 2;
        GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker);

        var attacks = GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(
            attacker,
            acceleratedDelay,
            baselineDelay,
            false,
            baselineDelay,
            remainingCharges);

        var debts = CombatMembers.FieldValue<Dictionary<uint, float>>("_attackSwingDebts");
        try
        {
            attacks.Should().Be(2);
            debts.Should().NotContainKey(attacker,
                "both matching rolls consume the two remaining charges, so accelerated debt must expire with the effect");
        }
        finally
        {
            GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker);
        }
    }

    [Test]
    public void ConsumeAttacksPerSwing_DoesNotScheduleAnAcceleratedRollBeyondTheFinalCharge()
    {
        const uint attacker = 0x7F000007;
        const int acceleratedDelay = 875;
        const int baselineDelay = 1750;
        GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker);

        var debts = CombatMembers.FieldValue<Dictionary<uint, float>>("_attackSwingDebts");

        try
        {
            // One carried roll makes this swing worth three accelerated rolls but two baseline
            // rolls. The one remaining charge is consumed by the first baseline roll, so it must
            // not add a third roll to the count passed to ResolveAttack.
            debts[attacker] = 1f;
            var attacks = GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(
                attacker,
                acceleratedDelay,
                baselineDelay,
                false,
                baselineDelay,
                1);

            attacks.Should().Be(2);
            debts.Should().NotContainKey(attacker,
                "the uncharged third roll must not be pre-scheduled and limited-speed debt expires with the charge");
        }
        finally
        {
            GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker);
        }
    }

    [Test]
    public void ConsumeAttacksPerSwing_FinalLimitedNoDelayChargeStillGrantsAnExtraFloorRoll()
    {
        const uint attacker = 0x7F000009;
        GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker);

        try
        {
            var attacks = GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(
                attacker,
                CombatRules.MinimumAttackDelayMilliseconds,
                CombatRules.MinimumAttackDelayMilliseconds,
                true,
                CombatRules.MinimumAttackDelayMilliseconds,
                0,
                1);

            attacks.Should().Be(CombatRules.MaxAttacksPerSwing,
                "the final Dead Man's Hand charge guarantees one extra roll even when baseline cadence is already at the floor");
        }
        finally
        {
            GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker);
        }
    }

    [Test]
    public void NativeAttackAction_StillCapsLimitedChargesWhenAnArmedNoDelayOverlaps()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server",
            "Native",
            "OnAIActionAttackObject.cs"));
        var countStart = source.IndexOf(
            "var limitedDelayReductionRemainingAttacks = hasLimitedAttackDelayReduction",
            System.StringComparison.Ordinal);
        var countEnd = source.IndexOf(
            "// The delay the attacker would have without a no-delay buff.",
            countStart,
            System.StringComparison.Ordinal);

        countStart.Should().BeGreaterThanOrEqualTo(0);
        countEnd.Should().BeGreaterThan(countStart);
        var countSetup = source[countStart..countEnd];
        countSetup.Should().Contain("limitedAttackDelayReductionRemainingAttacks");
        countSetup.Should().Contain("limitedAttackNoDelayRemainingAttacks");
        countSetup.Should().NotContain("if (hasArmedNoDelay)",
            "an armed timing effect must not disable the cap that protects concurrently active limited charges");
    }

    [Test]
    public void ConsumeAttacksPerSwing_PreservesAcceleratedDebtWhenChargesRemainAfterTheSwing()
    {
        const uint attacker = 0x7F000005;
        const int acceleratedDelay = 750;
        const int baselineDelay = 1750;
        const int remainingCharges = 3;
        GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker);

        var attacks = GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(
            attacker,
            acceleratedDelay,
            baselineDelay,
            false,
            baselineDelay,
            remainingCharges);

        var debts = CombatMembers.FieldValue<Dictionary<uint, float>>("_attackSwingDebts");
        CombatRules.CalculateAttacksPerSwing(acceleratedDelay, 0f, out var expectedAcceleratedDebt);

        try
        {
            attacks.Should().Be(2);
            debts[attacker].Should().BeApproximately(expectedAcceleratedDebt, 0.0001f,
                "two matching rolls leave one charge active, so its accelerated debt still belongs to the live effect");
        }
        finally
        {
            GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker);
        }
    }

    [Test]
    public void ConsumeAttacksPerSwing_DropsAcceleratedDebtAccumulatedBeforeTheFinalCharge()
    {
        const uint attacker = 0x7F000006;
        const int acceleratedDelay = 1400;
        const int baselineDelay = 1750;
        GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker);

        var debts = CombatMembers.FieldValue<Dictionary<uint, float>>("_attackSwingDebts");

        try
        {
            var firstSwingAttacks = GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(
                attacker,
                acceleratedDelay,
                baselineDelay,
                false,
                baselineDelay,
                2);
            firstSwingAttacks.Should().Be(1);
            debts[attacker].Should().BeApproximately(0.25f, 0.0001f,
                "the first accelerated one-roll swing carries fractional debt while one charge remains");

            var finalSwingAttacks = GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(
                attacker,
                acceleratedDelay,
                baselineDelay,
                false,
                baselineDelay,
                1);
            finalSwingAttacks.Should().Be(1);
            debts.Should().NotContainKey(attacker,
                "the parallel baseline ledger must replace debt accumulated over earlier limited-speed swings");
        }
        finally
        {
            GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker);
        }
    }

    [Test]
    public void ConsumeAttacksPerSwing_SuppressedLimitedSpeedCalculatesFromBaselineDebt()
    {
        const uint attacker = 0x7F000008;
        const int acceleratedDelay = 1000;
        const int baselineDelay = 1400;
        GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker);

        var debts = CombatMembers.FieldValue<Dictionary<uint, float>>("_attackSwingDebts");

        try
        {
            var acceleratedAttacks = GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(
                attacker,
                acceleratedDelay,
                baselineDelay,
                false,
                baselineDelay,
                2);
            acceleratedAttacks.Should().Be(1);
            debts[attacker].Should().BeApproximately(0.75f, 0.0001f);

            var suppressedAttacks = GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(
                attacker,
                baselineDelay,
                baselineDelay,
                false,
                baselineDelay,
                0);

            suppressedAttacks.Should().Be(1,
                "Signal Jammer must not turn accelerated debt into a second roll on the suppressed swing");
            debts[attacker].Should().BeApproximately(0.5f, 0.0001f,
                "only debt earned at the unsuppressed baseline cadence may survive the final charge");
        }
        finally
        {
            GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker);
        }
    }

    [Test]
    public void CalculateAttacksPerSwing_CapsAttacksAtMaxPerSwing()
    {
        var attacks = CombatRules.CalculateAttacksPerSwing(CombatRules.MinimumAttackDelayMilliseconds, 5f, out var attackDebt);

        attacks.Should().Be(CombatRules.MaxAttacksPerSwing);
        attackDebt.Should().BeLessThanOrEqualTo(CombatRules.MaxAttacksPerSwing);
    }

    [Test]
    public void CalculateAttacksPerSwing_MinimumDelayAveragesToMaxAttacksPerSwing()
    {
        var attackDebt = 0f;
        var totalAttacks = 0;
        const int swings = 60;

        for (var i = 0; i < swings; i++)
        {
            totalAttacks += CombatRules.CalculateAttacksPerSwing(CombatRules.MinimumAttackDelayMilliseconds, attackDebt, out attackDebt);
        }

        var expectedAttacks = swings * (CombatRules.BaseAttackDelayMilliseconds / (float)CombatRules.MinimumAttackDelayMilliseconds);
        totalAttacks.Should().BeCloseTo((int)expectedAttacks, 2);
    }

    [Test]
    public void ConsumeAttacksPerSwing_TracksDebtPerAttacker()
    {
        const uint attackerOne = 100;
        const uint attackerTwo = 200;
        const int effectiveDelay = 1000;

        GameServices.CombatAttackTiming.ClearAttackSwingDebt(attackerOne);
        GameServices.CombatAttackTiming.ClearAttackSwingDebt(attackerTwo);

        GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(attackerOne, effectiveDelay).Should().Be(1);
        GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(attackerTwo, effectiveDelay).Should().Be(1);
        GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(attackerOne, effectiveDelay).Should().Be(2);
        GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(attackerTwo, effectiveDelay).Should().Be(2);

        GameServices.CombatAttackTiming.ClearAttackSwingDebt(attackerOne);
        GameServices.CombatAttackTiming.ClearAttackSwingDebt(attackerTwo);
    }

    [Test]
    public void ClearAttackSwingDebt_ResetsStoredDebt()
    {
        const uint attacker = 300;
        const int effectiveDelay = 1000;

        GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker);

        GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(attacker, effectiveDelay).Should().Be(1);
        GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker);
        GameServices.CombatAttackTiming.ConsumeAttacksPerSwing(attacker, effectiveDelay).Should().Be(1);

        GameServices.CombatAttackTiming.ClearAttackSwingDebt(attacker);
    }

    [Test]
    public void CalculateEffectiveAttackDelay_UsesFastestFloorWhenNoDelayAttackIsQueued()
    {
        var attackerDelay = CombatRules.BaseAttackDelayMilliseconds + 2000;

        var effectiveDelay = CombatRules.CalculateEffectiveAttackDelay(attackerDelay, true);

        effectiveDelay.Should().Be(CombatRules.MinimumAttackDelayMilliseconds);
    }

    [Test]
    public void CanConsumeNextAbilityNoDelay_RequiresHostileAbility()
    {
        CombatRules.CanConsumeNextAbilityNoDelay(new AbilityDetail
        {
            IsHostileAbility = true,
            SkillType = SkillType.Pistol
        })
            .Should()
            .BeTrue();

        CombatRules.CanConsumeNextAbilityNoDelay(new AbilityDetail
        {
            IsHostileAbility = false
        })
            .Should()
            .BeFalse();

        CombatRules.CanConsumeNextAbilityNoDelay(new AbilityDetail
        {
            IsHostileAbility = true,
            SkillType = SkillType.Invalid
        })
            .Should()
            .BeFalse("an equipped weapon must not turn a skillless cast into a weapon attack");
    }

    [Test]
    public void ConsumeNextAbilityDelayReduction_ReturnsNothingWhenUnarmedOrNonHostile()
    {
        const uint creature = 310;

        GameServices.CombatAttackTiming.ConsumeNextAbilityDelayReductionPercent(creature, new AbilityDetail
        {
            IsHostileAbility = true,
            SkillType = SkillType.Pistol
        }).Should().Be(0);

        GameServices.CombatAttackTiming.ConsumeNextAbilityDelayReductionPercent(creature, new AbilityDetail
        {
            IsHostileAbility = false,
            SkillType = SkillType.Pistol
        }).Should().Be(0);
    }

    [Test]
    public void ConsumeNextAbilityDelayReduction_RejectsSkilllessHostileCasts()
    {
        var skilllessHostileAbility = new AbilityDetail
        {
            IsHostileAbility = true,
            SkillType = SkillType.Invalid
        };

        GameServices.CombatAttackTiming.ConsumeNextAbilityDelayReductionPercent(310, skilllessHostileAbility)
            .Should()
            .Be(0);
    }

    [Test]
    public void WeaponAbilitiesDeclareSkillsWhileSkilllessCastsStayOutsideWeaponTimingEffects()
    {
        var abilities = typeof(IAbilityListDefinition).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract &&
                           !type.IsInterface &&
                           typeof(IAbilityListDefinition).IsAssignableFrom(type))
            .SelectMany(type =>
                ((IAbilityListDefinition)System.Activator.CreateInstance(type)!)
                .BuildAbilities()
                .Select(pair => (Definition: type.Name, Feat: pair.Key, Detail: pair.Value)))
            .ToArray();

        var skilllessWeaponAbilities = abilities
            .Where(ability => ability.Detail.ActivationType == AbilityActivationType.Weapon &&
                              ability.Detail.SkillType == SkillType.Invalid)
            .Select(ability => $"{ability.Definition}:{ability.Feat}")
            .ToArray();
        skilllessWeaponAbilities.Should().BeEmpty(
            "weapon timing effects require an explicitly declared weapon skill");

        var skilllessHostileCasts = abilities
            .Where(ability => ability.Detail.IsHostileAbility &&
                              ability.Detail.ActivationType != AbilityActivationType.Weapon &&
                              ability.Detail.SkillType == SkillType.Invalid)
            .ToArray();
        skilllessHostileCasts.Should().NotBeEmpty("Provoke-style hostile casts are intentionally skillless");
        skilllessHostileCasts.Should().OnlyContain(ability =>
            !CombatRules.CanConsumeNextAbilityNoDelay(ability.Detail));
    }

    [Test]
    public void ConsumeNextAbilityDelayReduction_PreservesArmedBuffWhileRangedNoDelayStatusApplies()
    {
        var source = CombatSource.Read();
        var method = source[source.IndexOf(
            "public static int ConsumeNextAbilityDelayReductionPercent(uint creature, AbilityDetail ability)",
            System.StringComparison.Ordinal)..];
        method = method[..method.IndexOf(
            "private static int ConsumeNextAbilityDelayReductionPercent(uint creature, SkillType skillType)",
            System.StringComparison.Ordinal)];

        var rangedStatusGuard = method.IndexOf("if (hasRangedStatusNoDelay)", System.StringComparison.Ordinal);
        var consumingCall = method.IndexOf(
            "var temporaryReductionPercent = ConsumeNextAbilityDelayReductionPercent(creature, skillType);",
            System.StringComparison.Ordinal);

        rangedStatusGuard.Should().BeGreaterThanOrEqualTo(0);
        method[(rangedStatusGuard)..consumingCall].Should().Contain("return 100;");
        consumingCall.Should().BeGreaterThan(rangedStatusGuard,
            "the ranged status must return before the temporary next-ability arm can be consumed");
    }

    [Test]
    public void ConsumeNextAutoAttackNoDelay_ChecksSuppressionBeforeReadingArmedState()
    {
        var source = CombatSource.Read();
        var methodStart = source.IndexOf(
            "public static bool ConsumeNextAutoAttackNoDelay(uint creature, SkillType skillType)",
            System.StringComparison.Ordinal);
        var methodEnd = source.IndexOf(
            "public static int ConsumeNextAutoAttackCriticalRateBonus(uint creature, SkillType skillType)",
            methodStart,
            System.StringComparison.Ordinal);
        methodStart.Should().BeGreaterThanOrEqualTo(0);
        methodEnd.Should().BeGreaterThan(methodStart);

        var method = source[methodStart..methodEnd];
        var suppressionGuard = method.IndexOf(
            "if (IsAttackDelayReductionSuppressed(creature))",
            System.StringComparison.Ordinal);
        var armedStateRead = method.IndexOf(
            "StatType.NextAutoAttackNoDelayAllSkills",
            System.StringComparison.Ordinal);
        suppressionGuard.Should().BeGreaterThanOrEqualTo(0);
        armedStateRead.Should().BeGreaterThan(suppressionGuard,
            "suppressed timing must return before an armed all-skills or skill-specific modifier can be consumed");
    }

    [Test]
    public void ConsumeNextAutoAttackNoDelay_PreservesArmedBuffWhileRangedNoDelayStatusApplies()
    {
        var source = CombatSource.Read();
        var methodStart = source.IndexOf(
            "public static bool ConsumeNextAutoAttackNoDelay(uint creature, SkillType skillType)",
            System.StringComparison.Ordinal);
        var methodEnd = source.IndexOf(
            "public static int ConsumeNextAutoAttackCriticalRateBonus(uint creature, SkillType skillType)",
            methodStart,
            System.StringComparison.Ordinal);
        methodStart.Should().BeGreaterThanOrEqualTo(0);
        methodEnd.Should().BeGreaterThan(methodStart);

        var method = source[methodStart..methodEnd];
        var rangedStatusGuard = method.IndexOf(
            "if (appliesToRangedStatus)",
            System.StringComparison.Ordinal);
        var firstConsume = method.IndexOf(
            "_temporaryStatModifier.Consume(",
            System.StringComparison.Ordinal);

        rangedStatusGuard.Should().BeGreaterThanOrEqualTo(0);
        firstConsume.Should().BeGreaterThan(rangedStatusGuard);
        method[rangedStatusGuard..firstConsume].Should().Contain("return true;");
    }

    [Test]
    public void LimitedAttackSpeedBonuses_AreSuppressedByAttackDelayReductionSuppression()
    {
        const uint creature = 311;
        var creatureEffects = GetCreatureEffects();

        try
        {
            var tracker = new CreatureStatusEffect();
            creatureEffects[creature] = tracker;
            tracker.Add(new LimitedHasteStatusEffect(
                20,
                2,
                SkillType.Pistol,
                EffectIconType.ReloadTempoStatusEffect,
                new AbilityImpactSummary()));

            GameServices.CombatAttackTiming.ConsumeNextAbilityDelayReductionPercent(creature, new AbilityDetail
            {
                IsHostileAbility = true,
                SkillType = SkillType.Pistol
            }).Should().Be(20);

            tracker.Add(new DeadMansHandStatusEffect());
            StatusEffect.TryGetLimitedAttackNoDelay(
                    creature,
                    SkillType.Pistol,
                    out var noDelayRemainingAttacks)
                .Should()
                .BeTrue();
            noDelayRemainingAttacks.Should().Be(3);
            GameServices.CombatAttackTiming.HasNextAutoAttackNoDelay(creature, SkillType.Pistol).Should().BeTrue();
            GameServices.CombatAttackTiming.ConsumeNextAbilityDelayReductionPercent(creature, new AbilityDetail
            {
                IsHostileAbility = true,
                SkillType = SkillType.Pistol
            }).Should().Be(100);

            var signalJammer = new SignalJammerStatusEffect();
            tracker.Add(signalJammer);
            signalJammer.StatGroup.Stats[StatType.AttackDelayReductionSuppressed].Should().Be(1);
            Stat.GetStatTypeCategory(StatType.AttackDelayReductionSuppressed)
                .Should().Be(StatTypeCategory.BeneficialWhenNegative);
            Stat.GetStatTypeAggregation(StatType.AttackDelayReductionSuppressed)
                .Should().Be(StatTypeAggregation.Maximum);

            StatusEffect.TryGetLimitedAttackDelayReduction(
                    creature,
                    SkillType.Pistol,
                    out var reductionPercent,
                    out var remainingAttacks)
                .Should()
                .BeFalse();
            reductionPercent.Should().Be(0);
            remainingAttacks.Should().Be(0);
            StatusEffect.TryGetLimitedAttackNoDelay(
                    creature,
                    SkillType.Pistol,
                    out noDelayRemainingAttacks)
                .Should()
                .BeFalse();
            noDelayRemainingAttacks.Should().Be(0);
            GameServices.CombatAttackTiming.HasNextAutoAttackNoDelay(creature, SkillType.Pistol).Should().BeFalse();
            GameServices.CombatAttackTiming.ConsumeNextAutoAttackNoDelay(creature, SkillType.Pistol).Should().BeFalse();
            GameServices.CombatAttackTiming.ConsumeNextAbilityDelayReductionPercent(creature, new AbilityDetail
            {
                IsHostileAbility = true,
                SkillType = SkillType.Pistol
            }).Should().Be(0);
        }
        finally
        {
            creatureEffects.Remove(creature);
        }
    }

    [Test]
    public void ReloadTempo_DeclaresLimitedHasteForTheNextTwoAttacks()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server", "Feature", "PerkDefinition", "PistolPerkDefinition.cs"));
        var reloadTempo = source[source.IndexOf("private void ReloadTempo()", StringComparison.Ordinal)..];
        reloadTempo = reloadTempo[..reloadTempo.IndexOf("private void ", 1, StringComparison.Ordinal)];

        reloadTempo.Should().Contain("gain +20% Haste for your next two attacks");
        reloadTempo.Should().Contain(".IncreasesStat(StatType.CriticalHitLimitedHastePercentAdjustment, 20)");
        reloadTempo.Should().Contain(".IncreasesStat(StatType.CriticalHitLimitedHasteDurationSeconds, 30)");
        reloadTempo.Should().Contain(".IncreasesStat(StatType.CriticalHitLimitedHasteAttackCount, 2)");
        reloadTempo.Should().Contain(
            ".IncreasesStat(StatType.CriticalHitLimitedHasteStatusEffectIcon, (int)EffectIconType.ReloadTempoStatusEffect)");

        var combat = CombatSource.Read();
        var grant = combat[combat.IndexOf(
            "private static void ApplyCriticalHitLimitedHaste(",
            StringComparison.Ordinal)..combat.IndexOf(
            "private static void ApplyCriticalNextAutoAttackNoDelay(",
            StringComparison.Ordinal)];
        grant.Should().Contain("SkillType.Invalid,",
            "the triggering critical is Pistol-scoped, but the two promised Haste charges apply to any attacks");
    }

    [Test]
    public void ReloadTempo_TriggersBeforeThePositiveCriticalDamageGuard()
    {
        var source = CombatSource.Read();
        var method = source[source.IndexOf(
            "public static void ApplyCriticalHitEffects(",
            StringComparison.Ordinal)..];
        method = method[..method.IndexOf(
            "private static void ApplyCriticalHitSelfEffects(",
            StringComparison.Ordinal)];

        var limitedHasteTrigger = method.IndexOf(
            "ApplyCriticalHitLimitedHaste(attacker, skillType);",
            StringComparison.Ordinal);
        var positiveDamageGuard = method.IndexOf("if (damage <= 0)", StringComparison.Ordinal);

        limitedHasteTrigger.Should().BeGreaterThanOrEqualTo(0);
        limitedHasteTrigger.Should().BeLessThan(positiveDamageGuard,
            "a fully mitigated critical still earns Reload Tempo's next-two-attacks Haste");

        var damageRoll = File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server", "Native", "GetDamageRoll.cs"));
        var landedDamageHandlingStart = damageRoll.IndexOf(
            "if (isLandedAttack)",
            StringComparison.Ordinal);
        var landedDamageHandlingEnd = damageRoll.IndexOf(
            "ProfilerPlugin.PopPerfScope();",
            landedDamageHandlingStart,
            StringComparison.Ordinal);
        var landedDamageHandling = damageRoll[landedDamageHandlingStart..landedDamageHandlingEnd];
        landedDamageHandling.Should().Contain("GameServices.CombatHitEffects.ApplyCriticalHitEffects(");
        landedDamageHandling.Should().NotContain("if (isLandedAttack && totalDamage > 0)",
            "a fully mitigated landed native critical must still notify critical-hit effects");
    }

    [Test]
    public void WeaponDelayMigration_CoversLivePlayerInventoryAndSerializedItems()
    {
        var root = FindRepositoryRoot();
        var playerMigrationSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "CharacterMigration",
            "_14_15_CombatUpgradeMigrations.cs"));
        var serverMigrationSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "ServerMigration",
            "StoredItemDataMigration.cs"));
        var weaponDelayMigrationSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "SerializedItemWeaponDamageTypeMigration.cs"));

        playerMigrationSource.Should().Contain("SavedWeaponDamageTypeMigration.MigrateCharacter(character);");
        serverMigrationSource.Should().Contain("SerializedItemWeaponDamageTypeMigration.MigrateObject(obj)");
        weaponDelayMigrationSource.Should().Contain("ItemPropertyType.Delay");
        weaponDelayMigrationSource.Should().Contain("WeaponDelay.GetWeaponDelay(baseItem)");
        weaponDelayMigrationSource.Should().Contain("BuildWeaponBaseItemTypes");
        weaponDelayMigrationSource.Should().Contain("StaffBaseItemTypes");
        weaponDelayMigrationSource.Should().Contain("[\"t_knife\"] = ItemPropertyAttackDelay.Delay220");
        weaponDelayMigrationSource.Should().Contain("[\"t_shuriken\"] = ItemPropertyAttackDelay.Delay220");
        weaponDelayMigrationSource.Should().Contain("[\"t_rifle\"] = ItemPropertyAttackDelay.Delay300");
        weaponDelayMigrationSource.Should().Contain("[\"t_twinblade\"] = ItemPropertyAttackDelay.Delay230");
        weaponDelayMigrationSource.Should().Contain("[\"byyskwarriorswor\"] = ItemPropertyAttackDelay.Delay220");
        weaponDelayMigrationSource.Should().Contain("[\"sith_blade\"] = ItemPropertyAttackDelay.Delay220");
        weaponDelayMigrationSource.Should().Contain("[\"wswss002\"] = ItemPropertyAttackDelay.Delay220");
        weaponDelayMigrationSource.Should().Contain("GetHasInventory(obj)");
        weaponDelayMigrationSource.Should().Contain("GetItemInSlot((InventorySlot)index, creature)");
    }

    [Test]
    public void ModuleWeaponDelayProperties_AreNormalized()
    {
        var root = FindRepositoryRoot();
        var moduleRoot = Path.Combine(root.FullName, "Module");
        var files = Directory.EnumerateFiles(Path.Combine(moduleRoot, "uti"), "*.json")
            .Concat(Directory.EnumerateFiles(Path.Combine(moduleRoot, "git"), "*.json"));
        var findings = new List<string>();

        foreach (var file in files)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            InspectWeaponDelays(document.RootElement, Path.GetRelativePath(root.FullName, file), string.Empty, findings);
        }

        findings.Should().BeEmpty(string.Join("\n", findings.Take(25)));
    }

    [Test]
    public void ModuleShieldItems_DoNotHaveDelayProperties()
    {
        var root = FindRepositoryRoot();
        var moduleRoot = Path.Combine(root.FullName, "Module");
        var files = Directory.EnumerateFiles(Path.Combine(moduleRoot, "uti"), "*.json")
            .Concat(Directory.EnumerateFiles(Path.Combine(moduleRoot, "git"), "*.json"))
            .Concat(Directory.EnumerateFiles(Path.Combine(moduleRoot, "utc"), "*.json"));
        var findings = new List<string>();

        foreach (var file in files)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            InspectShieldDelays(document.RootElement, Path.GetRelativePath(root.FullName, file), string.Empty, findings);
        }

        findings.Should().BeEmpty(string.Join("\n", findings.Take(25)));
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull("the repository root should be discoverable from the test directory");
        return directory!;
    }

    private static Dictionary<uint, CreatureStatusEffect> GetCreatureEffects()
    {
        return StatusEffectInternals.CreatureEffects;
    }

    private static readonly IReadOnlyDictionary<int, ItemPropertyAttackDelay> WeaponDelayByBaseItem = BuildWeaponDelayByBaseItem();
    private static readonly IReadOnlySet<int> ShieldBaseItems = Item.ShieldBaseItemTypes
        .Select(x => (int)x)
        .ToHashSet();

    private static IReadOnlyDictionary<int, ItemPropertyAttackDelay> BuildWeaponDelayByBaseItem()
    {
        var delays = new Dictionary<int, ItemPropertyAttackDelay>();
        AddWeaponDelays(delays, Item.VibrobladeBaseItemTypes, ItemPropertyAttackDelay.Delay230);
        AddWeaponDelays(delays, Item.KatarBaseItemTypes, ItemPropertyAttackDelay.Delay220);
        AddWeaponDelays(delays, Item.TwinBladeBaseItemTypes, ItemPropertyAttackDelay.Delay230);
        AddWeaponDelays(delays, Item.VibroknifeBaseItemTypes, ItemPropertyAttackDelay.Delay220);
        AddWeaponDelays(delays, Item.StaffBaseItemTypes, ItemPropertyAttackDelay.Delay270);
        AddWeaponDelays(delays, Item.RifleBaseItemTypes, ItemPropertyAttackDelay.Delay300);
        AddWeaponDelays(delays, Item.HeavyVibrobladeBaseItemTypes, ItemPropertyAttackDelay.Delay300);
        AddWeaponDelays(delays, Item.PistolBaseItemTypes, ItemPropertyAttackDelay.Delay250);
        AddWeaponDelays(delays, Item.LightsaberBaseItemTypes, ItemPropertyAttackDelay.Delay240);
        AddWeaponDelays(delays, Item.SpearBaseItemTypes, ItemPropertyAttackDelay.Delay280);
        AddWeaponDelays(delays, Item.ThrowingWeaponBaseItemTypes, ItemPropertyAttackDelay.Delay220);
        AddWeaponDelays(delays, Item.SaberstaffBaseItemTypes, ItemPropertyAttackDelay.Delay240);
        AddWeaponDelays(delays, Item.CreatureBaseItemTypes, ItemPropertyAttackDelay.Delay240);

        return delays;
    }

    private static void AddWeaponDelays(
        Dictionary<int, ItemPropertyAttackDelay> delays,
        IEnumerable<BaseItem> baseItems,
        ItemPropertyAttackDelay delay)
    {
        foreach (var baseItem in baseItems)
            delays[(int)baseItem] = delay;
    }

    private static void InspectWeaponDelays(
        JsonElement element,
        string file,
        string path,
        ICollection<string> findings)
    {
        InspectItemDelays(element, file, path, findings, InspectWeaponDelay);
    }

    private static void InspectShieldDelays(
        JsonElement element,
        string file,
        string path,
        ICollection<string> findings)
    {
        InspectItemDelays(element, file, path, findings, InspectShieldDelay);
    }

    private static void InspectItemDelays(
        JsonElement element,
        string file,
        string path,
        ICollection<string> findings,
        Action<int, JsonElement, bool, string, ICollection<string>> inspectItemDelay)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (TryGetWrappedInt(element, "BaseItem", out var baseItem) &&
                    TryGetWrappedValue(element, "PropertiesList", out var propertiesList))
                {
                    inspectItemDelay(
                        baseItem,
                        propertiesList,
                        IsEconomyRestrictedJsonItem(element, baseItem),
                        $"{file}:{path}",
                        findings);
                }

                foreach (var property in element.EnumerateObject())
                {
                    if (property.Name == "__struct_id")
                        continue;

                    InspectItemDelays(
                        property.Value,
                        file,
                        string.IsNullOrWhiteSpace(path) ? property.Name : $"{path}.{property.Name}",
                        findings,
                        inspectItemDelay);
                }
                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    InspectItemDelays(item, file, $"{path}[{index}]", findings, inspectItemDelay);
                    index++;
                }
                break;
        }
    }

    private static void InspectWeaponDelay(
        int baseItem,
        JsonElement propertiesList,
        bool isEconomyRestricted,
        string findingPath,
        ICollection<string> findings)
    {
        if (!WeaponDelayByBaseItem.TryGetValue(baseItem, out var expectedDelay))
            return;

        var delayCosts = GetDelayCostValues(propertiesList).ToList();
        if (isEconomyRestricted &&
            Item.IsDoubleWeaponType((BaseItem)baseItem) &&
            delayCosts.Count == 1 && delayCosts[0] == 29)
        {
            return;
        }

        if (delayCosts.Count == 0)
        {
            findings.Add($"{findingPath} missing weapon Delay");
        }
        else if (delayCosts.Any(x => x != (int)expectedDelay))
        {
            findings.Add($"{findingPath} weapon Delay [{string.Join(", ", delayCosts)}] should be {(int)expectedDelay} ({expectedDelay})");
        }
    }

    private static void InspectShieldDelay(
        int baseItem,
        JsonElement propertiesList,
        bool isEconomyRestricted,
        string findingPath,
        ICollection<string> findings)
    {
        if (!ShieldBaseItems.Contains(baseItem))
            return;

        var delayCosts = GetDelayCostValues(propertiesList).ToList();
        if (delayCosts.Count > 0)
        {
            findings.Add($"{findingPath} shield Delay [{string.Join(", ", delayCosts)}] should be removed");
        }
    }

    private static IEnumerable<int> GetDelayCostValues(JsonElement propertiesList)
    {
        if (propertiesList.ValueKind != JsonValueKind.Array)
            yield break;

        foreach (var property in propertiesList.EnumerateArray())
        {
            if (TryGetWrappedInt(property, "PropertyName", out var propertyName) &&
                propertyName == 98 &&
                TryGetWrappedInt(property, "CostTable", out var costTable) &&
                costTable == 52 &&
                TryGetWrappedInt(property, "CostValue", out var costValue))
            {
                yield return costValue;
            }
        }
    }

    private static bool TryGetWrappedValue(JsonElement element, string propertyName, out JsonElement value)
    {
        value = default;
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var wrapper) ||
            wrapper.ValueKind != JsonValueKind.Object ||
            !wrapper.TryGetProperty("value", out value))
        {
            return false;
        }

        return true;
    }

    private static bool TryGetWrappedInt(JsonElement element, string propertyName, out int value)
    {
        value = 0;
        return TryGetWrappedValue(element, propertyName, out var wrapperValue) &&
               wrapperValue.ValueKind == JsonValueKind.Number &&
               wrapperValue.TryGetInt32(out value);
    }

    private static bool IsEconomyRestrictedJsonItem(JsonElement item, int baseItem)
    {
        string name = null;
        if (TryGetWrappedValue(item, "LocalizedName", out var localizedName) &&
            localizedName.ValueKind == JsonValueKind.Object)
        {
            name = localizedName.EnumerateObject()
                .Select(property => property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString()
                    : null)
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        }

        var noEconomy = false;
        if (TryGetWrappedValue(item, "VarTable", out var variables) &&
            variables.ValueKind == JsonValueKind.Array)
        {
            noEconomy = variables.EnumerateArray().Any(variable =>
                TryGetWrappedValue(variable, "Name", out var variableName) &&
                variableName.ValueKind == JsonValueKind.String &&
                string.Equals(variableName.GetString(), ItemService.NoEconomyVariable, StringComparison.Ordinal) &&
                TryGetWrappedInt(variable, "Type", out var variableType) && variableType == 1 &&
                TryGetWrappedInt(variable, "Value", out var variableValue) && variableValue == 1);
        }

        // Embedded items without a name or explicit NO_ECONOMY metadata do not qualify for
        // the exception. Keep the broad corpus check fail-closed for player weapons.
        if (name == null && !noEconomy)
            return false;

        return Item.IsEconomyRestricted((BaseItem)baseItem, name, noEconomy, hasInventoryIcon: true);
    }
}
