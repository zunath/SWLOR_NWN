using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.AbilityDefinition.WeaponAbility;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using static SWLOR.Game.Server.Tests.Support.CombatTranscriptBench;

namespace SWLOR.Game.Server.Tests.Feature.WeaponAbility;

/// <summary>What each landed hit triggers: resource riders, source status changes, bleed conversion and defense ignore, with fake services.</summary>
[TestFixture]
[NonParallelizable]
public class WeaponAbilityHitRidersTests
{
    private WeaponAbilityPartBench _bench = null!;
    private WeaponAbilityHitRiders _part = null!;

    [SetUp]
    public void Build()
    {
        _bench = new WeaponAbilityPartBench();
        _part = _bench.Part<WeaponAbilityHitRiders>();
    }

    [TearDown]
    public void Remove() => _bench.Dispose();

    private string Hit(WeaponAbilityProfile profile) =>
        _bench.Run(() => _part.AfterSuccessfulHit(profile, Attacker, Defender, CombatDamageType.Physical, new WeaponAbilityProfile.StatusSpreadSnapshot()));

    [Test]
    public void AHitWithNoRidersMakesNoResourceOrStatusChange()
    {
        var text = Hit(new WeaponAbilityProfile());

        text.Should().NotContain("RestoreAbilityHitStamina").And.NotContain("RestoreFP").And.NotContain("ReduceStamina")
            .And.NotContain("ReduceFP").And.NotContain("RemoveStatusEffect").And.NotContain("ApplyStatusEffect").And.NotContain("Replace(");
    }

    [Test]
    public void OnHitRestoresGoToTheActivatorAndOnHitDrainsGoToTheTarget()
    {
        _bench.Leaf<IStatService>().Script(nameof(IStatService.RestoreFP), _ => 4);
        var profile = new WeaponAbilityProfile { RestoreStaminaOnHit = 3, RestoreFPOnHit = 4, DrainStaminaOnHit = 5, DrainFPOnHit = 6 };

        var text = Hit(profile);

        text.Should().Contain("RestoreAbilityHitStamina(Attacker").And.Contain("RestoreFP(Attacker, 4")
            .And.Contain("ApplyAbilityRestoredFPEffects(Attacker").And.Contain("ReduceStamina(Defender, 5").And.Contain("ReduceFP(Defender, 6");
    }

    [Test]
    public void TheRestoredFPEffectOnlyPlaysWhenFPCameBack()
    {
        var text = Hit(new WeaponAbilityProfile { RestoreFPOnHit = 4 });

        text.Should().Contain("RestoreFP(Attacker, 4").And.NotContain("ApplyAbilityRestoredFPEffects");
    }

    [Test]
    public void ConsumingSourceStatusesRemovesOnlyTheOnesTheActivatorAppliedAndTellsThePlayer()
    {
        var profile = new WeaponAbilityProfile
        {
            ConsumeSourceStatusEffectsOnHit = true,
            ExtraDamageSourceStatusEffect = typeof(PoisonStatusEffect),
            ExtraDamageSourceStackStatusEffect = typeof(DiseaseStatusEffect)
        };

        Hit(profile).Should().NotContain("RemoveStatusEffect");

        _bench.Leaf<IStatusEffectService>().Script(nameof(IStatusEffectService.HasStatusEffect), args => args.Length == 3);
        var text = Hit(profile);

        text.Should().Contain("RemoveStatusEffect(Defender, PoisonStatusEffect, Attacker").And.Contain("RemoveStatusEffect(Defender, DiseaseStatusEffect, Attacker")
            .And.Contain("You consume your Venom and Infection setup.");
    }

    [Test]
    public void ConsumingSourceStatusesIsOffWithoutTheSetting()
    {
        _bench.Leaf<IStatusEffectService>().Script(nameof(IStatusEffectService.HasStatusEffect), _ => true);

        Hit(new WeaponAbilityProfile { ExtraDamageSourceStatusEffect = typeof(PoisonStatusEffect) }).Should().NotContain("RemoveStatusEffect");
    }

    [Test]
    public void BleedTurnsIntoHemorrhageForTheDefaultOrTheAuthoredDuration()
    {
        _bench.Leaf<IStatusEffectApplicationService>().Script(nameof(IStatusEffectApplicationService.ApplyStatusEffect), _ => true);
        var profile = new WeaponAbilityProfile { ConsumeBleedIntoHemorrhage = true };
        Hit(profile).Should().NotContain("Hemorrhage");

        _bench.Leaf<IStatusEffectService>().Script(nameof(IStatusEffectService.HasStatusEffect), _ => true);
        var text = Hit(profile);
        text.Should().Contain("RemoveStatusEffect(Defender, BleedStatusEffect").And.Contain("ApplyStatusEffect(Attacker, Defender, HemorrhageStatusEffect, 30");

        text = Hit(new WeaponAbilityProfile { ConsumeBleedIntoHemorrhage = true, HemorrhageDurationSeconds = 12 });
        text.Should().Contain("ApplyStatusEffect(Attacker, Defender, HemorrhageStatusEffect, 12");
    }

    [Test]
    public void ADefenseIgnoreHitLowersTheTargetsPhysicalDefenseForTheStatedSeconds()
    {
        _bench.Leaf<IStatService>().Script(nameof(IStatService.GetStatAdjustment), args => (StatType)args[1]! switch
        {
            StatType.DefenseIgnoreHitPhysicalDefensePercentAdjustment => -15,
            StatType.DefenseIgnoreHitPhysicalDefenseDurationSeconds => 6,
            _ => 0
        });

        Hit(new WeaponAbilityProfile { DefenseIgnorePercent = 20 })
            .Should().Contain("Replace(Defender, PhysicalDefensePercentAdjustment, -15, 6, DefenseIgnoreHitPhysicalDefensePercentAdjustment)");
        Hit(new WeaponAbilityProfile()).Should().NotContain("Replace(");
    }

    [Test]
    public void ADefenseIgnoreHitNeedsBothAnAdjustmentAndADuration()
    {
        _bench.Leaf<IStatService>().Script(nameof(IStatService.GetStatAdjustment), args =>
            (StatType)args[1]! == StatType.DefenseIgnoreHitPhysicalDefensePercentAdjustment ? -15 : 0);

        Hit(new WeaponAbilityProfile { DefenseIgnorePercent = 20 }).Should().NotContain("Replace(");
    }

    [Test]
    public void ExtendingSourceStatusesAsksForEachListedStatusWithTheActivatorAsSource()
    {
        var profile = new WeaponAbilityProfile
        {
            SourceStatusExtensionSeconds = 4,
            SourceStatusEffectsToExtend = new[] { typeof(PoisonStatusEffect), typeof(DiseaseStatusEffect) }
        };
        StatusEffectCategory? categories = null;

        var text = _bench.Run(() => categories = _part.BeforeSuccessfulImpactRiders(profile, Attacker, Defender, CombatDamageType.Physical));

        categories.Should().BeNull();
        text.Should().Contain("ExtendStatusEffectDuration(Defender, PoisonStatusEffect, Attacker, 4")
            .And.Contain("ExtendStatusEffectDuration(Defender, DiseaseStatusEffect, Attacker, 4");
    }

    [Test]
    public void ExtendingDoesNothingWithoutADurationOrAList()
    {
        _bench.Run(() => _part.BeforeSuccessfulImpactRiders(new WeaponAbilityProfile { SourceStatusExtensionSeconds = 4 }, Attacker, Defender, CombatDamageType.Physical))
            .Should().NotContain("ExtendStatusEffectDuration");
        _bench.Run(() => _part.BeforeSuccessfulImpactRiders(
                new WeaponAbilityProfile { SourceStatusEffectsToExtend = new[] { typeof(PoisonStatusEffect) } }, Attacker, Defender, CombatDamageType.Physical))
            .Should().NotContain("ExtendStatusEffectDuration");
    }

    [Test]
    public void TheConditionalStatusTheRidersApplyCountsAsTheHitsCategories()
    {
        _bench.Leaf<IStatusEffectApplicationService>().Script(nameof(IStatusEffectApplicationService.ApplyStatusEffect), _ => true);
        _bench.Leaf<ICombatDefenseMemoryService>().Script(nameof(ICombatDefenseMemoryService.HasRecentDeflection), _ => true);
        var profile = new WeaponAbilityProfile
        {
            ConditionalStatusEffect = typeof(DisorientedStatusEffect),
            ConditionalStatusDurationSeconds = 10,
            ConditionalStatusAfterDeflectionWindowSeconds = 5
        };
        StatusEffectCategory? categories = null;

        _bench.Run(() => categories = _part.BeforeSuccessfulImpactRiders(profile, Attacker, Defender, CombatDamageType.Physical));

        categories.Should().Be(new DisorientedStatusEffect().Categories);
    }
}
