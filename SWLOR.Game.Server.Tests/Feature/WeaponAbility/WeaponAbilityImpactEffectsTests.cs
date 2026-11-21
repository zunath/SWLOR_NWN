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

/// <summary>What happens after the last hit of a weapon ability: restores by target count, critical and deflection riders, healing and haste, with fake services.</summary>
[TestFixture]
[NonParallelizable]
public class WeaponAbilityImpactEffectsTests
{
    private WeaponAbilityPartBench _bench = null!;
    private WeaponAbilityImpactEffects _part = null!;

    [SetUp]
    public void Build()
    {
        _bench = new WeaponAbilityPartBench();
        _part = _bench.Part<WeaponAbilityImpactEffects>();
    }

    [TearDown]
    public void Remove() => _bench.Dispose();

    private string After(WeaponAbilityProfile profile, int totalDamage, int successfulHits = 0, AbilityImpactSummary? summary = null, bool clear = true) =>
        _bench.Run(() => _part.AfterImpact(profile, Attacker, totalDamage, successfulHits, summary!, clear));

    [Test]
    public void StaminaRestoresByTargetCountOnlyAtTheMinimumNumberOfTargets()
    {
        var profile = new WeaponAbilityProfile { RestoreStaminaIfMinimumTargetsHit = 9, StaminaRestoreMinimumTargets = 3 };

        _bench.Run(() => _part.RestoreResourcesForTargetCount(profile, Attacker, new AbilityImpactSummary { ImpactedTargetCount = 2 }))
            .Should().NotContain("RestoreAbilityHitStamina");
        _bench.Run(() => _part.RestoreResourcesForTargetCount(profile, Attacker, null!)).Should().NotContain("RestoreAbilityHitStamina");
        _bench.Run(() => _part.RestoreResourcesForTargetCount(profile, Attacker, new AbilityImpactSummary { ImpactedTargetCount = 3 }))
            .Should().Contain("RestoreAbilityHitStamina(Attacker").And.Contain(", 9");
    }

    [Test]
    public void TheTargetCountRestoreNeedsBothSettings()
    {
        var summary = new AbilityImpactSummary { ImpactedTargetCount = 5 };

        _bench.Run(() => _part.RestoreResourcesForTargetCount(new WeaponAbilityProfile { RestoreStaminaIfMinimumTargetsHit = 9 }, Attacker, summary))
            .Should().NotContain("RestoreAbilityHitStamina");
        _bench.Run(() => _part.RestoreResourcesForTargetCount(new WeaponAbilityProfile { StaminaRestoreMinimumTargets = 2 }, Attacker, summary))
            .Should().NotContain("RestoreAbilityHitStamina");
    }

    [Test]
    public void AfterAnImpactTheIdleBonusSnapshotsAreClearedUnlessTheCallerKeepsThem()
    {
        After(new WeaponAbilityProfile(), 0).Should().Contain("ClearAbilityActivationIdleBonuses(Attacker").And.Contain("ClearWeaponAbilityActivationIdleBonuses(Attacker");
        After(new WeaponAbilityProfile(), 0, clear: false).Should().NotContain("ClearAbilityActivationIdleBonuses");
    }

    [Test]
    public void ACriticalHitAppliesTheSelfStatusForItsDurationOrPermanently()
    {
        var summary = new AbilityImpactSummary { CriticalHitCount = 1 };
        var timed = new WeaponAbilityProfile
        {
            SelfStatusEffectOnCriticalHitFactory = _ => new DisorientedStatusEffect(),
            SelfStatusEffectOnCriticalHitDurationSeconds = 9
        };
        var permanent = new WeaponAbilityProfile { SelfStatusEffectOnCriticalHitFactory = _ => new DisorientedStatusEffect(), SelfStatusEffectOnCriticalHitIsPermanent = true };

        After(timed, 0, summary: new AbilityImpactSummary()).Should().NotContain("ApplyStatusEffect");
        After(timed, 0, summary: summary).Should().Contain("ApplyStatusEffect(Attacker, Attacker, DisorientedStatusEffect, 9");
        After(permanent, 0, summary: summary).Should().Contain("ApplyStatusEffect(Attacker, Attacker, DisorientedStatusEffect, 0");
    }

    [Test]
    public void FPComesBackAfterARangedDeflectionOnlyWhenATargetWasStruckEvenAtZeroDamage()
    {
        var profile = new WeaponAbilityProfile { RestoreFPAfterRangedDeflection = 6, RestoreFPAfterRangedDeflectionWindowSeconds = 5 };
        var summary = new AbilityImpactSummary { ImpactedTargetCount = 1 };
        _bench.Leaf<IStatService>().Script(nameof(IStatService.RestoreFP), _ => 6);

        After(profile, 0, summary: summary).Should().NotContain("RestoreFP");

        _bench.Leaf<ICombatDefenseMemoryService>().Script(nameof(ICombatDefenseMemoryService.HasRecentDeflection), _ => true);
        After(profile, 0, summary: new AbilityImpactSummary()).Should().NotContain("RestoreFP");
        After(profile, 0, summary: summary).Should().Contain("HasRecentDeflection(Attacker, Ranged, 5").And.Contain("RestoreFP(Attacker, 6")
            .And.Contain("ApplyAbilityRestoredFPEffects(Attacker");
    }

    [Test]
    public void ACriticalRestoreSendsItsOwnFeedbackOnlyWithoutALabel()
    {
        _bench.Leaf<ICombatResourcesService>().Script(nameof(ICombatResourcesService.RestoreAbilityHitStamina), _ => 7);
        var summary = new AbilityImpactSummary { CriticalHitCount = 2 };

        After(new WeaponAbilityProfile { RestoreStaminaIfAnyCriticalHit = 7 }, 0, summary: summary)
            .Should().Contain("RestoreAbilityHitStamina(Attacker").And.Contain("True");
        After(new WeaponAbilityProfile { RestoreStaminaIfAnyCriticalHit = 7, RestoreStaminaIfAnyCriticalHitFeedbackLabel = "Edge" }, 0, summary: summary)
            .Should().Contain("RestoreAbilityHitStamina(Attacker").And.Contain("False");
        After(new WeaponAbilityProfile { RestoreStaminaIfAnyCriticalHit = 7 }, 0, summary: new AbilityImpactSummary())
            .Should().NotContain("RestoreAbilityHitStamina");
    }

    [Test]
    public void WithNoDamageOnlyAStruckTargetLetsTheLandedModifiersRun()
    {
        var profile = new WeaponAbilityProfile { SelfHastePercent = 10, SelfHasteDurationSeconds = 6, RestoreStaminaAfterImpact = 5 };

        var none = After(profile, 0, summary: new AbilityImpactSummary());
        var struck = After(profile, 0, summary: new AbilityImpactSummary { ImpactedTargetCount = 1 });

        none.Should().NotContain("AddCapped").And.NotContain("RestoreAbilityHitStamina");
        struck.Should().Contain("AddCapped(Attacker, AttackDelayReductionPercent, 10, 6").And.NotContain("RestoreAbilityHitStamina");
    }

    [Test]
    public void DamageRestoresAfterTheImpactAndAppliesTheLandedModifiers()
    {
        _bench.Leaf<IStatService>().Script(nameof(IStatService.RestoreFP), _ => 3);
        var profile = new WeaponAbilityProfile { RestoreStaminaAfterImpact = 5, RestoreFPAfterImpact = 3, SelfHastePercent = 10, SelfHasteDurationSeconds = 6 };

        var text = After(profile, 40, 1, new AbilityImpactSummary { ImpactedTargetCount = 1 });

        text.Should().Contain("RestoreAbilityHitStamina(Attacker").And.Contain("RestoreFP(Attacker, 3").And.Contain("ApplyAbilityRestoredFPEffects(Attacker")
            .And.Contain("AddCapped(Attacker, AttackDelayReductionPercent, 10, 6");
    }

    [Test]
    public void AllHitsLandingRestoresAndHastesOnlyWhenEveryHitLanded()
    {
        _bench.Leaf<IStatService>().Script(nameof(IStatService.RestoreFP), _ => 2);
        var profile = new WeaponAbilityProfile
        {
            HitCount = 3,
            RestoreStaminaIfAllHitsLand = 4,
            RestoreFPIfAllHitsLand = 2,
            SelfHastePercentIfAllHitsLand = 8,
            SelfHasteDurationSecondsIfAllHitsLand = 5,
            SelfHasteMaximumPercentIfAllHitsLand = 16
        };

        After(profile, 30, 2).Should().NotContain("RestoreAbilityHitStamina").And.NotContain("RestoreFP").And.NotContain("AddCapped");

        var text = After(profile, 30, 3);
        text.Should().Contain("RestoreAbilityHitStamina(Attacker").And.Contain("RestoreFP(Attacker, 2")
            .And.Contain("AddCapped(Attacker, AttackDelayReductionPercent, 8, 5, 16");
    }

    [Test]
    public void AllHitsLandingNeedsAMultiHitAbility()
    {
        var profile = new WeaponAbilityProfile { HitCount = 1, RestoreStaminaIfAllHitsLand = 4 };

        After(profile, 30, 1).Should().NotContain("RestoreAbilityHitStamina");
    }

    [Test]
    public void HealingIsAnInstantEffectOnTheActivatorOnlyForDamageDealtAndAPercentSetting()
    {
        // The heal effect is opaque in the transcript (the amount is inside the engine effect), so the test pins when it is applied.
        const string heal = "ApplyEffectToObject(Instant, Effect, Attacker";

        After(new WeaponAbilityProfile { HealPercentOfDamage = 50, HealMaximum = 10 }, 100).Should().Contain(heal);
        After(new WeaponAbilityProfile { HealPercentOfDamage = 50 }, 0).Should().NotContain(heal);
        After(new WeaponAbilityProfile(), 100).Should().NotContain(heal);
    }
}
