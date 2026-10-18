using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Feature.AbilityDefinition.Unarmed;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Tests.Feature;

public class MeleeTechniqueTests
{
    private static readonly DateTime Start = new(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public void FinisherSweepPreservesManualTargetingAndTheTwoImpactBudget()
    {
        var abilities = new FinishingCombinationAbilityDefinition().BuildAbilities();
        abilities.Should().HaveCount(2);
        foreach (var ability in abilities.Values)
        {
            ability.RequiresTarget.Should().BeTrue();
            ability.IsSingleTargetAbility.Should().BeTrue();
            ability.IsAreaAbility.Should().BeFalse("the sweep is conditional per cast, not a permanent targeting mode");
            ability.MeleeTechnique.ImpactCount.Should().Be(2);
            ability.MeleeTechnique.WeaponDamagePercent.Should().Be(50);
            ability.MeleeTechnique.OpeningSweepRadiusMeters.Should().Be(5f);
            Ability.IsAreaImpact(uint.MaxValue, ability).Should().BeFalse();
            Ability.IsSingleTargetImpact(uint.MaxValue, ability).Should().BeTrue();
        }
        new MeleeTechniqueDetail().OpeningSweepRadiusMeters.Should().Be(0f,
            "other techniques do not acquire a sweep implicitly");
    }

    [Test]
    public void OpenerWorksWithoutWindowUpgradeOrOrdinaryAttack()
    {
        var sequence = new TechniqueSequence();
        sequence.Begin(11, MeleeTechniqueStage.Opening, Start.AddSeconds(4));
        sequence.Spend(11, MeleeTechniqueStage.Opening, Start.AddSeconds(1))
            .Should().Be(MeleeTechniqueStage.Opening);
        sequence.Stage.Should().Be(MeleeTechniqueStage.None);
    }

    [Test]
    public void WeaveAttemptConsumesItsBonusButPreservesOpeningAndExpiry()
    {
        var sequence = new TechniqueSequence();
        var expiry = Start.AddSeconds(6);
        sequence.Begin(11, MeleeTechniqueStage.Opening, expiry, 16);
        sequence.TakeWeave(11, Start.AddSeconds(1)).Should().Be(16);
        sequence.TakeWeave(11, Start.AddSeconds(1)).Should().Be(0, "duplicate callbacks and the other hand share one allowance");
        sequence.Stage.Should().Be(MeleeTechniqueStage.Opening);
        sequence.ExpiresAt.Should().Be(expiry, "ordinary attacks cannot refresh setup");
        sequence.Spend(11, MeleeTechniqueStage.Opening, Start.AddSeconds(2)).Should().Be(MeleeTechniqueStage.Opening,
            "the attempt may miss without making the bridge unavailable");
    }

    [TestCase(12u, 1)]
    [TestCase(11u, 6)]
    public void DifferentTargetOrExpiryCannotUseWeaveOrSetup(uint target, int seconds)
    {
        var sequence = new TechniqueSequence();
        sequence.Begin(11, MeleeTechniqueStage.Opening, Start.AddSeconds(6), 16);
        sequence.TakeWeave(target, Start.AddSeconds(seconds)).Should().Be(0);
        sequence.Spend(target, MeleeTechniqueStage.Opening | MeleeTechniqueStage.Finish, Start.AddSeconds(seconds))
            .Should().Be(MeleeTechniqueStage.None);
        sequence.Stage.Should().Be(MeleeTechniqueStage.None);
    }

    [Test]
    public void FinishAndDistractionCannotBeSpentAsAnOpening()
    {
        var sequence = new TechniqueSequence();
        sequence.Begin(11, MeleeTechniqueStage.Distraction, Start.AddSeconds(8));
        sequence.Spend(11, MeleeTechniqueStage.Opening, Start.AddSeconds(1)).Should().Be(MeleeTechniqueStage.None);
        sequence.Begin(11, MeleeTechniqueStage.Finish, Start.AddSeconds(6));
        sequence.Spend(11, MeleeTechniqueStage.Finish, Start.AddSeconds(1)).Should().Be(MeleeTechniqueStage.Finish);
    }

    [Test]
    public void ReplacementCannotBankThePreviousEnemyOrLine()
    {
        var sequence = new TechniqueSequence();
        sequence.Begin(11, MeleeTechniqueStage.Opening, Start.AddSeconds(6), 16);
        sequence.Begin(12, MeleeTechniqueStage.Distraction, Start.AddSeconds(8));
        sequence.Matches(11, Start).Should().BeFalse();
        sequence.TakeWeave(12, Start).Should().Be(0);
        sequence.Spend(12, MeleeTechniqueStage.Distraction, Start.AddSeconds(1)).Should().Be(MeleeTechniqueStage.Distraction);
    }

    [TestCase(31, 50, 15)]
    [TestCase(19, 50, 9)]
    [TestCase(31, 100, 31)]
    public void HalfWeaponImpactsRoundEachCapturedRatingDown(int damage, int percent, int expected) =>
        TechniqueSequence.ScaleWeaponDamage(damage, percent).Should().Be(expected);

    [TestCase(28, 2, 14, 14)]
    [TestCase(20, 2, 10, 10)]
    [TestCase(13, 2, 7, 6)]
    public void ConditionalPowerIsOneCastBudget(int total, int impacts, int first, int second)
    {
        TechniqueSequence.SplitPower(total, impacts, 0).Should().Be(first);
        TechniqueSequence.SplitPower(total, impacts, 1).Should().Be(second);
        (first + second).Should().Be(total);
        TechniqueSequence.SplitPower(total, impacts, 2).Should().Be(0);
    }

    [Test]
    public void DiscountDoesNotStackOrSurviveBenefitInvalidationAndRetainsItsCooldown()
    {
        var resource = new TechniqueResourceState();
        resource.TryGrantDiscount(2, 8, 18, Start).Should().BeTrue();
        resource.TryGrantDiscount(2, 8, 18, Start.AddSeconds(1)).Should().BeFalse();
        resource.TakeDiscount(0, Start.AddSeconds(2)).Should().Be(0, "zero-cost actions must not consume the charge");
        resource.GetDiscount(Start.AddSeconds(2)).Should().Be(2);
        resource.ClearBenefits();
        resource.GetDiscount(Start.AddSeconds(2)).Should().Be(0);
        resource.TryGrantDiscount(2, 8, 18, Start.AddSeconds(17)).Should().BeFalse();
        resource.TryGrantDiscount(2, 8, 18, Start.AddSeconds(18)).Should().BeTrue();
        resource.TakeDiscount(1, Start.AddSeconds(19)).Should().Be(1);
        resource.TakeDiscount(5, Start.AddSeconds(19)).Should().Be(0);
    }

    [Test]
    public void DiscountExpiresWithoutResettingItsTriggerGate()
    {
        var resource = new TechniqueResourceState();
        resource.TryGrantDiscount(2, 8, 18, Start).Should().BeTrue();
        resource.GetDiscount(Start.AddSeconds(8)).Should().Be(0);
        resource.TryGrantDiscount(2, 8, 18, Start.AddSeconds(8)).Should().BeFalse();
    }

    [TestCase(1, 1)]
    [TestCase(2, 2)]
    [TestCase(7, 2)]
    public void AnyPositivePaymentSpendsOneWholeDiscountCharge(int cost, int expectedDiscount)
    {
        var resource = new TechniqueResourceState();
        resource.TryGrantDiscount(2, 8, 18, Start).Should().BeTrue();
        resource.GetDiscount(Start.AddSeconds(1)).Should().Be(2);
        resource.GetDiscount(Start.AddSeconds(1)).Should().Be(2, "eligibility previews cannot consume the charge");
        resource.TakeDiscount(cost, Start.AddSeconds(1)).Should().Be(expectedDiscount);
        resource.TakeDiscount(20, Start.AddSeconds(1)).Should().Be(0,
            "a payment below the charge value cannot bank the unused remainder for another ability");
        resource.TryGrantDiscount(2, 8, 18, Start.AddSeconds(17)).Should().BeFalse();
    }

    [Test]
    public void BothLinesAndBothFinisherHitsShareOneRefundGate()
    {
        var resource = new TechniqueResourceState();
        resource.TryRefund(1, 2, 12, Start, out var first).Should().BeTrue();
        first.Should().Be(1, "a Payback-discounted activation cannot restore more than its actual payment");
        resource.TryRefund(9, 2, 12, Start, out _).Should().BeFalse();
        resource.ClearBenefits();
        resource.TryRefund(10, 2, 12, Start.AddSeconds(11), out _).Should().BeFalse();
        resource.TryRefund(10, 2, 12, Start.AddSeconds(12), out var second).Should().BeTrue();
        second.Should().Be(2);
    }

    [Test]
    public void FreeCastsDoNotConsumeTheRefundGate()
    {
        var resource = new TechniqueResourceState();
        resource.TryRefund(0, 2, 12, Start, out _).Should().BeFalse();
        resource.TryRefund(9, 0, 12, Start, out _).Should().BeFalse();
        resource.TryRefund(9, 2, 12, Start, out var refund).Should().BeTrue();
        refund.Should().Be(2);
    }

    [Test]
    public void OneSecondActionLockSurvivesSetupInvalidation()
    {
        var resource = new TechniqueResourceState();
        resource.LockAction(Start);
        resource.ClearBenefits();
        resource.CanActivate(Start.AddMilliseconds(999)).Should().BeFalse();
        resource.CanActivate(Start.AddSeconds(1)).Should().BeTrue();
    }

    [Test]
    public void SoftDebuffsUseStrongestAggregationAndHaveNoControlRiders()
    {
        var rattled = new RattledStatusEffect();
        var lowKick = new LowKickStatusEffect();
        rattled.StackingType.Should().Be(StatusEffectStackType.StackFromMultipleSources);
        lowKick.StackingType.Should().Be(StatusEffectStackType.StackFromMultipleSources);
        rattled.ActivationType.Should().Be(StatusEffectActivationType.Passive);
        lowKick.ActivationType.Should().Be(StatusEffectActivationType.Passive);
        new OpeningStatusEffect().ActivationType.Should().Be(StatusEffectActivationType.Passive);
        new FinishStatusEffect().ActivationType.Should().Be(StatusEffectActivationType.Passive);
        new PaybackStatusEffect().ActivationType.Should().Be(StatusEffectActivationType.Passive);
        rattled.Categories.Should().Be(StatusEffectCategory.Debuff);
        lowKick.Categories.Should().Be(StatusEffectCategory.Debuff);
        rattled.StatGroup.Stats.Where(pair => pair.Value != 0).Should().ContainSingle()
            .Which.Should().Be(new KeyValuePair<StatType, int>(StatType.OrdinaryWeaponAccuracyPenalty, 10));
        lowKick.StatGroup.Stats.Where(pair => pair.Value != 0).Should().ContainSingle()
            .Which.Should().Be(new KeyValuePair<StatType, int>(StatType.OrdinaryPhysicalDamagePenaltyPercent, 10));
        Stat.GetStatTypeAggregation(StatType.OrdinaryWeaponAccuracyPenalty).Should().Be(StatTypeAggregation.Maximum);
        Stat.GetStatTypeAggregation(StatType.OrdinaryPhysicalDamagePenaltyPercent).Should().Be(StatTypeAggregation.Maximum);
    }

    [Test]
    public void FinisherWindowsShareTheirGateAndCannotBeBankedByToggling()
    {
        var state = new TechniqueReactiveState();
        state.TryOpenWindow(11, MeleeTechniqueStage.Opening, 8, 12, Start).Should().BeTrue();
        state.GetWindowStage(Start.AddSeconds(7)).Should().Be(MeleeTechniqueStage.Opening);
        state.GetWindowStage(Start.AddSeconds(8)).Should().Be(MeleeTechniqueStage.None);
        state.TryOpenWindow(11, MeleeTechniqueStage.Finish, 8, 12, Start.AddSeconds(1)).Should().BeFalse();
        state.ClearBenefits();
        state.GetWindowStage(Start.AddSeconds(1)).Should().Be(MeleeTechniqueStage.None);
        state.TryOpenWindow(12, MeleeTechniqueStage.Finish, 8, 12, Start.AddSeconds(11)).Should().BeFalse();
        state.TryOpenWindow(12, MeleeTechniqueStage.Finish, 8, 12, Start.AddSeconds(12)).Should().BeTrue();
        state.WindowTarget.Should().Be(12);
        state.GetWindowStage(Start.AddSeconds(19)).Should().Be(MeleeTechniqueStage.Finish);
        state.GetWindowStage(Start.AddSeconds(20)).Should().Be(MeleeTechniqueStage.None);
    }

    [Test]
    public void RetaliationUsesOnlyItsBracedEnemyAndOneEightPowerBudget()
    {
        var state = new TechniqueReactiveState();
        state.BeginBrace(11, Start.AddSeconds(8), 4, Start);
        state.TryGrantRetaliation(12, 8, Start.AddSeconds(8), 18, Start.AddSeconds(1)).Should().BeFalse();
        state.TryGrantRetaliation(11, 8, Start.AddSeconds(8), 18, Start.AddSeconds(2)).Should().BeTrue();
        state.TryGrantRetaliation(11, 8, Start.AddSeconds(8), 18, Start.AddSeconds(3)).Should().BeFalse();
        state.GetRetaliation(12, Start.AddSeconds(3)).Should().Be(0);
        state.TakeRetaliation(11, Start.AddSeconds(3)).Should().Be(8);
        state.TakeRetaliation(11, Start.AddSeconds(3)).Should().Be(0, "a miss or interrupted paid cast has already spent the charge");
        state.ClearBenefits();
        state.BeginBrace(11, Start.AddSeconds(22), 4, Start.AddSeconds(17));
        state.TryGrantRetaliation(11, 8, Start.AddSeconds(22), 18, Start.AddSeconds(18)).Should().BeFalse();
        state.BeginBrace(11, Start.AddSeconds(28), 4, Start.AddSeconds(20));
        state.TryGrantRetaliation(11, 8, Start.AddSeconds(28), 18, Start.AddSeconds(20)).Should().BeTrue();
    }

    [Test]
    public void BraceAndRetaliationCannotOutliveTheirSetup()
    {
        var state = new TechniqueReactiveState();
        state.BeginBrace(11, Start.AddSeconds(8), 4, Start);
        state.TryGrantRetaliation(11, 8, Start.AddSeconds(8), 18, Start.AddSeconds(4)).Should().BeFalse();
        state.BeginBrace(11, Start.AddSeconds(2), 4, Start);
        state.TryGrantRetaliation(11, 8, Start.AddSeconds(2), 18, Start.AddSeconds(2)).Should().BeFalse();
        state.BeginBrace(11, Start.AddSeconds(8), 4, Start);
        state.TryGrantRetaliation(11, 8, Start.AddSeconds(8), 18, Start.AddSeconds(1)).Should().BeTrue();
        state.GetRetaliation(11, Start.AddSeconds(8)).Should().Be(0);
        state.ClearSetupBenefits();
        state.GetRetaliation(11, Start.AddSeconds(2)).Should().Be(0);
    }

    [Test]
    public void IndependentRetaliationNeedsNoOpenerAndSurvivesSetupChanges()
    {
        var state = new TechniqueReactiveState();
        state.TryGrantIndependentRetaliation(11, 24, 8, 18, Start).Should().BeTrue();
        state.RetaliationRequiresSetup.Should().BeFalse();
        state.ClearSetupBenefits();
        state.BeginBrace(11, Start.AddSeconds(6), 4, Start.AddSeconds(1));
        state.GetRetaliation(11, Start.AddSeconds(2)).Should().Be(24);
        state.TakeRetaliation(12, Start.AddSeconds(2)).Should().Be(0);
        state.GetRetaliation(11, Start.AddSeconds(2)).Should().Be(24, "another target cannot consume the charge");
        state.TryGrantIndependentRetaliation(12, 24, 8, 18, Start.AddSeconds(3)).Should().BeFalse();
        state.TakeRetaliation(11, Start.AddSeconds(3)).Should().Be(24);
        state.TakeRetaliation(11, Start.AddSeconds(3)).Should().Be(0);
        state.ClearBenefits();
        state.TryGrantIndependentRetaliation(12, 24, 8, 18, Start.AddSeconds(17)).Should().BeFalse();
        state.TryGrantIndependentRetaliation(12, 24, 8, 18, Start.AddSeconds(18)).Should().BeTrue();
    }

    [Test]
    public void IndependentRetaliationHasItsOwnExpiryAndRetainsItsGateWhenCleared()
    {
        var state = new TechniqueReactiveState();
        state.TryGrantIndependentRetaliation(0, 24, 8, 18, Start).Should().BeFalse();
        state.TryGrantIndependentRetaliation(11, 0, 8, 18, Start).Should().BeFalse();
        state.TryGrantIndependentRetaliation(11, 24, 0, 18, Start).Should().BeFalse();
        state.TryGrantIndependentRetaliation(11, 24, 8, 18, Start).Should().BeTrue();
        state.GetRetaliation(11, Start.AddSeconds(7)).Should().Be(24);
        state.GetRetaliation(11, Start.AddSeconds(8)).Should().Be(0);
        state.ClearRetaliation();
        state.TryGrantIndependentRetaliation(11, 24, 8, 18, Start.AddSeconds(17)).Should().BeFalse();
        state.TryGrantIndependentRetaliation(11, 24, 8, 18, Start.AddSeconds(18)).Should().BeTrue();
        state.ClearBenefits();
        state.GetRetaliation(11, Start.AddSeconds(19)).Should().Be(0);
    }

    [Test]
    public void ReactiveStatusClonesPreserveTheirPayloadAndExactExpiration()
    {
        var evasion = (SlipAwayStatusEffect)new SlipAwayStatusEffect(35, 15).Clone();
        var haste = (PressForwardStatusEffect)new PressForwardStatusEffect(30).Clone();
        evasion.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(35);
        evasion.StatGroup.Stats[StatType.MovementSpeedPercentAdjustment].Should().Be(15);
        haste.StatGroup.Stats[StatType.AttackDelayReductionPercent].Should().Be(30);
        evasion.ActivationType.Should().Be(StatusEffectActivationType.Passive);
        haste.ActivationType.Should().Be(StatusEffectActivationType.Passive);
        new RetaliationStatusEffect().ActivationType.Should().Be(StatusEffectActivationType.Passive);
    }

    [Test]
    public void SharedTechniqueServiceHasNoContentEquipmentOrStatusDependencies()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        directory.Should().NotBeNull();
        var source = File.ReadAllText(Path.Combine(directory!.FullName,
            "SWLOR.Game.Server", "Service", "MeleeTechnique.cs"));
        foreach (var dependency in new[]
        {
            "EquipmentPredicates", "PowerGlove", "Power Glove", "SkillType.Unarmed",
            "Feature.StatusEffectDefinition", "OpeningStatusEffect", "FinishStatusEffect",
            "SlipAwayStatusEffect", "PressForwardStatusEffect", "PaybackStatusEffect", "RetaliationStatusEffect"
        })
            source.Should().NotContain(dependency, "equipment and presentation belong to the registered content profile");
    }

    [Test]
    public void IncompleteProfilesFailClosed()
    {
        new MeleeTechniqueProfile().IsValid.Should().BeFalse();
        new MeleeTechniqueProfile
        {
            HasValidEquipment = _ => true,
            EquipmentError = "Equip the required weapon."
        }.IsValid.Should().BeFalse("equipment eligibility alone cannot replace the required presentation factories");
    }
}
