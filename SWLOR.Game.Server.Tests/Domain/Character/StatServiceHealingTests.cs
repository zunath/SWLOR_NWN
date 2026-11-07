using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using ServiceRandom = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>Stat reads over their sources, healing adjustments and what a received heal sets off.</summary>
public class StatServiceHealingTests : StatServiceTestBase
{
    [TearDown]
    public void ResetRolls() => ServiceRandom.ResetSeed();

    private static StatAdjustmentSource Source(string key, StatType stat, int value) =>
        new(key, new Dictionary<StatType, int> { [stat] = value });

    [Test]
    public void GetStatAdjustment_SumsAdditiveStatsFromEverySource()
    {
        SetStat(Npc, StatType.Attack, 1);
        Service.PerkStats[(Npc, StatType.Attack)] = 2;
        Bench.MimicryStats[(Npc, StatType.Attack)] = 4;
        Service.TemporaryStats[(Npc, StatType.Attack)] = 8;

        Service.GetStatAdjustmentExcludingTemporaryModifiers(Npc, StatType.Attack).Should().Be(7);
        Service.GetStatAdjustment(Npc, StatType.Attack).Should().Be(15);
    }

    [Test]
    public void GetStatAdjustment_TakesTheHighestOfAMaximumStat()
    {
        SetStat(Npc, StatType.OpeningAutoAttackIdleSeconds, 1);
        Service.PerkStats[(Npc, StatType.OpeningAutoAttackIdleSeconds)] = 9;
        Bench.MimicryStats[(Npc, StatType.OpeningAutoAttackIdleSeconds)] = 4;
        Service.TemporaryStats[(Npc, StatType.OpeningAutoAttackIdleSeconds)] = 8;

        Service.GetStatAdjustment(Npc, StatType.OpeningAutoAttackIdleSeconds).Should().Be(9);
    }

    [Test]
    public void GetStatAdjustment_CombinesTheBitsOfABitwiseStat()
    {
        SetStat(Npc, StatType.StatusAppliedRequiredCategory, 1);
        Service.PerkStats[(Npc, StatType.StatusAppliedRequiredCategory)] = 2;
        Bench.MimicryStats[(Npc, StatType.StatusAppliedRequiredCategory)] = 4;
        Service.TemporaryStats[(Npc, StatType.StatusAppliedRequiredCategory)] = 1;

        Service.GetStatAdjustment(Npc, StatType.StatusAppliedRequiredCategory).Should().Be(7);
    }

    [Test]
    public void GetStatSources_OfNoSourceIsEmpty() =>
        Service.GetStatSources(Npc, StatType.Attack).Should().BeEmpty();

    [Test]
    public void GetStatSources_OfOneKindOfSourceHandsBackThatList()
    {
        IReadOnlyList<StatAdjustmentSource> list = new[] { Source("a", StatType.Attack, 1) };

        Service.PerkSources[StatType.Attack] = list;
        Service.GetStatSources(Npc, StatType.Attack).Should().BeSameAs(list);
        Service.PerkSources.Clear();

        Bench.StatusSources[StatType.Attack] = list;
        Service.GetStatSources(Npc, StatType.Attack).Should().BeSameAs(list);
        Bench.StatusSources.Clear();

        Service.TemporarySources[StatType.Attack] = list;
        Service.GetStatSources(Npc, StatType.Attack).Should().BeSameAs(list);
        Service.TemporarySources.Clear();

        Bench.MimicrySources[StatType.Attack] = list;
        Service.GetStatSources(Npc, StatType.Attack).Should().BeSameAs(list);
    }

    [Test]
    public void GetStatSources_OfSeveralKindsListsPerksThenEffectsThenTemporaryThenTraits()
    {
        Bench.MimicrySources[StatType.Attack] = new[] { Source("trait", StatType.Attack, 1) };
        Service.TemporarySources[StatType.Attack] = new[] { Source("temporary", StatType.Attack, 1) };
        Bench.StatusSources[StatType.Attack] = new[] { Source("effect", StatType.Attack, 1) };
        Service.PerkSources[StatType.Attack] = new[] { Source("perk", StatType.Attack, 1) };

        Service.GetStatSources(Npc, StatType.Attack).Select(source => source.Key)
            .Should().Equal("perk", "effect", "temporary", "trait");
    }

    [TestCase(0, 50, 0)]
    [TestCase(-5, 50, -5)]
    [TestCase(10, 0, 10)]
    [TestCase(10, -20, 10)]
    [TestCase(10, 25, 13)]
    public void CalculateOutgoingAbilityHealingAmount_OnlyRaisesAPositiveHealRoundedUp(int amount, int adjustment, int expected) =>
        Service.CalculateOutgoingAbilityHealingAmount(amount, adjustment).Should().Be(expected);

    [Test]
    public void ApplyOutgoingAbilityHealingAdjustment_ReadsTheSourceOrABeastsMaster()
    {
        Bench.AddNpc();
        SetStat(Npc, StatType.OutgoingAbilityHealingPercentAdjustment, 50);
        Service.ApplyOutgoingAbilityHealingAdjustment(Npc, 10).Should().Be(15);

        const uint Beast = 300;
        Bench.AddNpc(Beast);
        Bench.Beasts.Add(Beast);
        Bench.Masters[Beast] = Npc;
        Service.ApplyOutgoingAbilityHealingAdjustment(Beast, 10).Should().Be(15);
    }

    [Test]
    public void ApplyOutgoingAbilityHealingAdjustment_LeavesAnEmptyHealOrAnInvalidSourceAlone()
    {
        Service.ApplyOutgoingAbilityHealingAdjustment(Npc, 0).Should().Be(0);
        Service.ApplyOutgoingAbilityHealingAdjustment(ObjectInvalid, 10).Should().Be(10);
    }

    [TestCase(10, 100, 50, 10)]
    [TestCase(90, 100, 50, 50)]
    [TestCase(10, 0, 50, 0)]
    [TestCase(10, 100, 100, 0)]
    [TestCase(-5, 100, 50, 0)]
    [TestCase(10, 100, 0, 0)]
    public void CalculateEffectiveHealingAmount_IsWhatFitsUnderTheMaximum(int amount, int maxHP, int currentHP, int expected) =>
        Service.CalculateEffectiveHealingAmount(amount, currentHP, maxHP).Should().Be(expected);

    [Test]
    public void ApplyHealingReceivedAdjustment_AppliesThePercentAndNeverGivesLessThanOne()
    {
        SetStat(Npc, StatType.HealingReceivedPercentAdjustment, 50);
        Service.ApplyHealingReceivedAdjustment(Npc, 10, false).Should().Be(15);

        SetStat(Npc, StatType.HealingReceivedPercentAdjustment, -500);
        Service.ApplyHealingReceivedAdjustment(Npc, 10, false).Should().Be(1);

        Service.ApplyHealingReceivedAdjustment(Npc, 0).Should().Be(0);
        Service.ApplyHealingReceivedAdjustment(Npc, -3).Should().Be(-3);
    }

    [Test]
    public void ApplyHealingReceivedAdjustment_RunsTheReactionsUnlessToldNot()
    {
        Bench.CurrentHitPoints[Npc] = 50;
        Bench.MaxHitPoints[Npc] = () => 100;

        Service.ApplyHealingReceivedAdjustment(Npc, 10);
        Bench.Log.Should().Equal("HealingReaction:200");

        Bench.Log.Clear();
        Service.ApplyHealingReceivedAdjustment(Npc, 10, false);
        Bench.Log.Should().BeEmpty();
    }

    [Test]
    public void ApplyHealingReceivedEffects_ReactsToWhatIsLeftAfterThePvpCut()
    {
        Bench.CurrentHitPoints[Npc] = 50;
        Bench.MaxHitPoints[Npc] = () => 100;
        SetStat(Npc, StatType.HealingReceivedAttackPercentAdjustment, 5);
        SetStat(Npc, StatType.HealingReceivedAttackDurationSeconds, 6);

        Bench.PvpHealingReduction = 100;
        Service.ApplyHealingReceivedEffects(Npc, 10);
        Bench.Log.Should().BeEmpty("nothing of the heal lands");

        Bench.PvpHealingReduction = 0;
        Service.ApplyHealingReceivedEffects(Npc, 10);
        Bench.Log.Should().Equal(
            "HealingReaction:200",
            "Temporary:AttackPercentAdjustment:5:6:HealingReceivedAttackPercentAdjustment");
    }

    [Test]
    public void ApplyHealingReceivedEffects_DoesNotReactToAHealWithNothingToRestore()
    {
        Bench.CurrentHitPoints[Npc] = 100;
        Bench.MaxHitPoints[Npc] = () => 100;

        Service.ApplyHealingReceivedEffects(Npc, 10);

        Bench.Log.Should().BeEmpty();
    }

    [TestCase(0, 6)]
    [TestCase(5, 0)]
    public void TheAttackBoost_NeedsAnAmountAndADuration(int percent, int duration)
    {
        Bench.CurrentHitPoints[Npc] = 50;
        Bench.MaxHitPoints[Npc] = () => 100;
        SetStat(Npc, StatType.HealingReceivedAttackPercentAdjustment, percent);
        SetStat(Npc, StatType.HealingReceivedAttackDurationSeconds, duration);

        Service.ApplyHealingReceivedEffects(Npc, 10);

        Bench.Log.Should().Equal("HealingReaction:200");
    }

    private void GiveStaminaRestore(int chance, int stamina)
    {
        Bench.AddNpc();
        Bench.Skin(Npc, (ItemPropertyType.Stamina, -1, 100));
        Bench.CurrentHitPoints[Npc] = 50;
        Bench.MaxHitPoints[Npc] = () => 100;
        SetStat(Npc, StatType.HealingReceivedStaminaRestoreChance, chance);
        SetStat(Npc, StatType.HealingReceivedStaminaRestore, stamina);
        SetStat(Npc, StatType.HealingReceivedStaminaRestoreCooldownSeconds, 12);
    }

    [Test]
    public void TheStaminaRestore_OnlyFollowsAnInstantAbilityHealThroughItsGateAndRoll()
    {
        GiveStaminaRestore(100, 5);

        Service.ApplyHealingReceivedEffects(Npc, 10);
        Bench.Log.Should().Equal(new[] { "HealingReaction:200" }, "a heal over time never restores Stamina");

        Bench.Log.Clear();
        Service.ApplyHealingReceivedEffects(Npc, 10, true);
        Bench.Log.Should().Equal(
            "HealingReaction:200",
            "TryTrigger:HealingReceivedStaminaRestore:12",
            "TriggerAttribution:HealingReceivedStaminaRestore",
            "Script:pc_stm_adjusted",
            "StaminaRestoredEffects:200",
            "Feedback:5:STM");
        Bench.Locals.Int(Npc, "STAMINA").Should().Be(5);
    }

    [Test]
    public void TheStaminaRestore_StopsAtAClosedGate()
    {
        GiveStaminaRestore(100, 5);
        Service.StatTriggerAvailable = false;

        Service.ApplyHealingReceivedEffects(Npc, 10, true);

        Bench.Log.Should().Equal("HealingReaction:200", "TryTrigger:HealingReceivedStaminaRestore:12");
    }

    [TestCase(0, 5)]
    [TestCase(100, 0)]
    public void TheStaminaRestore_NeedsAChanceAndAnAmount(int chance, int stamina)
    {
        GiveStaminaRestore(chance, stamina);

        Service.ApplyHealingReceivedEffects(Npc, 10, true);

        Bench.Log.Should().Equal("HealingReaction:200");
    }

    [Test]
    public void TheStaminaRestore_RollsOnceAgainstTheChance()
    {
        GiveStaminaRestore(1, 5);
        // The first percentile roll of this seed is above 1.
        ServiceRandom.SetSeed(11);

        Service.ApplyHealingReceivedEffects(Npc, 10, true);

        Bench.Log.Should().Equal("HealingReaction:200");
    }

    [Test]
    public void TheStaminaRestore_ChanceScalesWithAnAbilityUpToItsMaximum()
    {
        GiveStaminaRestore(1, 5);
        SetStat(Npc, StatType.HealingReceivedStaminaRestoreChanceMaximum, 100);
        // The stat holds the ability's value plus one.
        SetStat(Npc, StatType.HealingReceivedStaminaRestoreChanceScalingAbility, (int)AbilityType.Vitality + 1);
        Bench.Abilities[(Npc, AbilityType.Vitality)] = 500;

        Service.ApplyHealingReceivedEffects(Npc, 10, true);

        Bench.Log.Should().Contain("TryTrigger:HealingReceivedStaminaRestore:12", "the chance rose to 100");
    }

    [Test]
    public void TheStaminaRestore_ChanceDoesNotScaleWithAnUnknownAbility()
    {
        GiveStaminaRestore(1, 5);
        SetStat(Npc, StatType.HealingReceivedStaminaRestoreChanceMaximum, 100);
        SetStat(Npc, StatType.HealingReceivedStaminaRestoreChanceScalingAbility, 9999);
        ServiceRandom.SetSeed(11);

        Service.ApplyHealingReceivedEffects(Npc, 10, true);

        Bench.Log.Should().Equal("HealingReaction:200");
    }

    [TestCase(10, 0, 1f, 0, 0f, 10)]
    [TestCase(0, 10, 1f, 0, 0f, 0)]
    [TestCase(-5, 10, 1f, 0, 0f, -5)]
    [TestCase(3, 1, 0.5f, 0, 0f, 5)]
    [TestCase(10, 20, 0.5f, 10, 0.25f, 135)]
    [TestCase(10, -20, 0.5f, 0, 0f, 10)]
    public void ScaleEffect_RoundsTheBonusUp(int baseAmount, int primary, float primaryRate, int secondary, float secondaryRate, int expected) =>
        Service.ScaleEffect(baseAmount, primary, primaryRate, secondary, secondaryRate).Should().Be(expected);
}
