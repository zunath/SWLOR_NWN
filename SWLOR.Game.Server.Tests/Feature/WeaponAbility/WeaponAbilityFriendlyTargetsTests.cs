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

/// <summary>
/// Friendly, self and party-facing weapon abilities with fake services. The validation that asks the targeting rules whether a creature is a
/// valid friendly target, and the guarded and ward lookups, read creature state the fakes cannot give: the call-trace fixture drives those
/// paths (a ward and a guarded ally that were recently hit) against the engine fake.
/// </summary>
[TestFixture]
[NonParallelizable]
public class WeaponAbilityFriendlyTargetsTests
{
    private WeaponAbilityPartBench _bench = null!;
    private WeaponAbilityFriendlyTargets _part = null!;

    [SetUp]
    public void Build()
    {
        _bench = new WeaponAbilityPartBench();
        _part = _bench.Part<WeaponAbilityFriendlyTargets>();
        _bench.Leaf<IStatusEffectApplicationService>().Script(nameof(IStatusEffectApplicationService.ApplyStatusEffect), _ => true);
    }

    [TearDown]
    public void Remove() => _bench.Dispose();

    private static WeaponAbilityProfile FriendlyStatus() => new() { FriendlyTargetStatusEffectFactory = () => new DisorientedStatusEffect() };

    [Test]
    public void TheFriendlyPredicatesReadTheProfileSettings()
    {
        WeaponAbilityFriendlyTargets.HasFriendlyTargetStatus(new WeaponAbilityProfile()).Should().BeFalse();
        WeaponAbilityFriendlyTargets.HasFriendlyTargetStatus(FriendlyStatus()).Should().BeTrue();
        WeaponAbilityFriendlyTargets.HasFriendlyTargetEffects(new WeaponAbilityProfile()).Should().BeFalse();
        WeaponAbilityFriendlyTargets.HasFriendlyTargetEffects(new WeaponAbilityProfile { FriendlyTargetTemporaryHPPercent = 5 }).Should().BeTrue();
        WeaponAbilityFriendlyTargets.HasFriendlyTargetEffects(new WeaponAbilityProfile { SelfGuardPercent = 5 }).Should().BeTrue();
        WeaponAbilityFriendlyTargets.HasImmediateSelfStatusEffect(new WeaponAbilityProfile()).Should().BeFalse();
        WeaponAbilityFriendlyTargets.HasImmediateSelfStatusEffect(new WeaponAbilityProfile { SelfStatusEffectFactory = () => new DisorientedStatusEffect() })
            .Should().BeTrue();
        WeaponAbilityFriendlyTargets.HasImmediateSelfStatusEffect(
                new WeaponAbilityProfile { SelfStatusEffectFactory = () => new DisorientedStatusEffect(), SelfStatDurationSeconds = 5 })
            .Should().BeFalse("a self status with a stat duration is applied by the stat modifiers instead");
    }

    [Test]
    public void ATargetThatIsNotGuardedResolvesToTheGuardedOneOnlyWhenTheProfileRequiresIt()
    {
        WeaponAbilityFriendlyTargets.ResolveFriendlyTarget(new WeaponAbilityProfile(), Attacker, Defender).Should().Be(Defender);
        WeaponAbilityFriendlyTargets.ResolveFriendlyTarget(new WeaponAbilityProfile { RequiresGuardedTarget = true }, Attacker, Defender)
            .Should().NotBe(Defender);
    }

    [Test]
    public void AHostileTargetNeedsARecentWardHitOnlyWhenTheProfileRequiresIt()
    {
        _part.ValidateHostileTarget(new WeaponAbilityProfile(), Attacker, Defender).Should().BeEmpty();
        _part.ValidateHostileTarget(new WeaponAbilityProfile { RequiresRecentWardHitTarget = true }, Attacker, Defender)
            .Should().Be("Target has not recently attacked your ward.");
    }

    [Test]
    public void ARequiredGuardedTargetThatIsGoneIsRefused()
    {
        _part.ValidateFriendlyTargetStatus(new WeaponAbilityProfile { RequiresGuardedTarget = true }, Attacker, Defender)
            .Should().Be("You do not have an active Guarded target within range.");
    }

    [Test]
    public void TheFriendlyStatusReplacesTheCastersEarlierOneAndLastsTheDurationOrFortyFiveSeconds()
    {
        var text = _bench.Run(() => _part.ApplyFriendlyTargetStatus(FriendlyStatus(), Attacker, Ally, 20).Should().BeTrue());
        text.Should().Contain("RemoveStatusEffectFromAllTargetsBySource(DisorientedStatusEffect, Attacker")
            .And.Contain("ApplyStatusEffect(Attacker, Ally, DisorientedStatusEffect, 20");

        _bench.Run(() => _part.ApplyFriendlyTargetStatus(FriendlyStatus(), Attacker, Ally, 0)).Should().Contain("DisorientedStatusEffect, 45");

        var persistent = new WeaponAbilityProfile
        {
            FriendlyTargetStatusEffectFactory = () => new DisorientedStatusEffect(),
            FriendlyTargetStatusPersistsUntilBroken = true
        };
        _bench.Run(() => _part.ApplyFriendlyTargetStatus(persistent, Attacker, Ally, 20)).Should().Contain("DisorientedStatusEffect, 0");
    }

    [Test]
    public void WithoutAFriendlyStatusNothingIsApplied()
    {
        _bench.Run(() => _part.ApplyFriendlyTargetStatus(new WeaponAbilityProfile(), Attacker, Ally, 20).Should().BeFalse())
            .Should().NotContain("ApplyStatusEffect");
    }

    [Test]
    public void ASelfStatusFirstRemovesTheStatusesItReplacesAndTheOtherStances()
    {
        var profile = new WeaponAbilityProfile
        {
            SelfStatusEffectFactory = () => new DisorientedStatusEffect(),
            SelfStatusEffectsToReplace = new[] { typeof(PoisonStatusEffect), null! }
        };

        var text = _bench.Run(() => _part.ApplySelfStatusEffect(profile, Attacker, 30));

        text.Should().Contain("RemoveStatusEffect(Attacker, PoisonStatusEffect").And.Contain("RemoveOtherStanceStatuses(Attacker, DisorientedStatusEffect")
            .And.Contain("ApplyStatusEffect(Attacker, Attacker, DisorientedStatusEffect, 30").And.Contain("PlaySuccessfulImpactVisualEffect(Attacker, Attacker");
        _bench.RunCalls(() => _part.ApplySelfStatusEffect(new WeaponAbilityProfile(), Attacker, 30)).Should().BeEmpty();
    }

    [Test]
    public void TheGuardedTargetGetsTheSelfStatusOnlyWhenTheProfileSaysSoAndItHasADuration()
    {
        var profile = new WeaponAbilityProfile { SelfStatusAlsoAppliesToGuardedTarget = true };

        _bench.RunCalls(() => _part.ApplySelfStatusToGuardedTarget(new WeaponAbilityProfile(), Attacker, typeof(DisorientedStatusEffect), 10)).Should().BeEmpty();
        _bench.RunCalls(() => _part.ApplySelfStatusToGuardedTarget(profile, Attacker, typeof(DisorientedStatusEffect), 0)).Should().BeEmpty();
        _bench.RunCalls(() => _part.ApplySelfStatusToGuardedTarget(profile, Attacker, null!, 10)).Should().BeEmpty();
        _bench.Run(() => _part.ApplySelfStatusToGuardedTarget(profile, Attacker, typeof(DisorientedStatusEffect), 10))
            .Should().NotContain("ApplyStatusEffect", "no target is guarded by the caster");
    }

    [Test]
    public void ASelfGuardReplacesTheCastersGuardAndPlaysTheImpactEffect()
    {
        var text = _bench.Run(() => _part.ApplyFriendlyTargetEffects(new WeaponAbilityProfile { SelfGuardPercent = 12, SelfGuardDurationSeconds = 20 }, Attacker, Ally, "key"));

        text.Should().Contain("Replace(Attacker, Guard, 12, 20, GeneratedWeaponAbility:GuardingPlayerGuard)")
            .And.Contain("PlaySuccessfulImpactVisualEffect(Attacker, Attacker");
    }

    [Test]
    public void AFriendlyStatusImpactAppliesTheStatusThenTheEffectsThenTheAfterActivationModifiers()
    {
        var profile = new WeaponAbilityProfile
        {
            FriendlyTargetStatusEffectFactory = () => new DisorientedStatusEffect(),
            SelfGuardPercent = 12, SelfGuardDurationSeconds = 20, SelfHastePercent = 10, SelfHasteDurationSeconds = 6
        };

        var text = _bench.Run(() => _part.ApplyFriendlyImpact(profile, Attacker, Ally, true, null!, 30, "key", SkillType.Invalid));

        var status = text.IndexOf("ApplyStatusEffect(Attacker, Ally, DisorientedStatusEffect", StringComparison.Ordinal);
        var guard = text.IndexOf("GeneratedWeaponAbility:GuardingPlayerGuard", StringComparison.Ordinal);
        var haste = text.IndexOf("AddCapped(Attacker, AttackDelayReductionPercent, 10, 6", StringComparison.Ordinal);
        status.Should().BeGreaterThanOrEqualTo(0);
        guard.Should().BeGreaterThan(status);
        haste.Should().BeGreaterThan(guard);
        text.Should().Contain("PlaySuccessfulImpactVisualEffect(Attacker, Ally");
    }

    [Test]
    public void AFriendlyStatusThatCannotBeAppliedEndsTheImpactWithoutTheEffects()
    {
        _bench.Leaf<IStatusEffectApplicationService>().Script(nameof(IStatusEffectApplicationService.ApplyStatusEffect), _ => false);
        var profile = new WeaponAbilityProfile
        {
            FriendlyTargetStatusEffectFactory = () => new DisorientedStatusEffect(), SelfHastePercent = 10, SelfHasteDurationSeconds = 6
        };

        _bench.Run(() => _part.ApplyFriendlyImpact(profile, Attacker, Ally, true, null!, 30, "key", SkillType.Invalid))
            .Should().NotContain("AddCapped").And.NotContain("PlaySuccessfulImpactVisualEffect");
    }

    [Test]
    public void FriendlyEffectsWithoutAStatusApplyOnTheFriendlyTarget()
    {
        var profile = new WeaponAbilityProfile { SelfGuardPercent = 12, SelfGuardDurationSeconds = 20 };

        _bench.Run(() => _part.ApplyFriendlyImpact(profile, Attacker, Ally, true, null!, 30, "key", SkillType.Invalid))
            .Should().Contain("GeneratedWeaponAbility:GuardingPlayerGuard").And.Contain("PlaySuccessfulImpactVisualEffect(Attacker, Ally");
    }

    [Test]
    public void ASelfOnlyImpactAppliesTheImmediateSelfStatus()
    {
        var profile = new WeaponAbilityProfile { SelfStatusEffectFactory = () => new DisorientedStatusEffect() };

        _bench.Run(() => _part.ApplyFriendlyImpact(profile, Attacker, Attacker, false, null!, 30, "key", SkillType.Invalid))
            .Should().Contain("ApplyStatusEffect(Attacker, Attacker, DisorientedStatusEffect, 30");
    }

    [Test]
    public void AToggledStatusReplacesTheOtherStancesAndAppliesForTheDuration()
    {
        var text = _bench.Run(() =>
            _part.ApplyFriendlyImpact(new WeaponAbilityProfile(), Attacker, Attacker, false, typeof(DisorientedStatusEffect), 0, "key", SkillType.Invalid));

        text.Should().Contain("RemoveOtherStanceStatuses(Attacker, DisorientedStatusEffect").And.Contain("ApplyStatusEffect(Attacker, Attacker, DisorientedStatusEffect, 0")
            .And.Contain("PlaySuccessfulImpactVisualEffect(Attacker, Attacker");
    }

    [Test]
    public void AnImpactWithNothingToApplyStillRunsTheAfterActivationModifiers()
    {
        var profile = new WeaponAbilityProfile { SelfHastePercent = 10, SelfHasteDurationSeconds = 6 };

        _bench.Run(() => _part.ApplyFriendlyImpact(profile, Attacker, Attacker, false, null!, 0, "key", SkillType.Invalid))
            .Should().Contain("AddCapped(Attacker, AttackDelayReductionPercent, 10, 6").And.NotContain("ApplyStatusEffect");
    }
}
