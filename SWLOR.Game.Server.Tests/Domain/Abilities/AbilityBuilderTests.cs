using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Domain.Abilities;

/// <summary>The ability builder writes what it is told onto the active ability and refuses values that would break an ability.</summary>
[TestFixture]
public class AbilityBuilderTests
{
    private static AbilityBuilder NewAbility() =>
        new AbilityBuilder().Create(FeatType.GuardCounter3, PerkType.GuardCounter).Name("Probe");

    private static AbilityDetail Built(AbilityBuilder builder) => builder.Build()[FeatType.GuardCounter3];

    [Test]
    public void TheTargetingHelpersDescribeTheirShape()
    {
        var detail = Built(NewAbility().HasTargetingSphere(Spell.Fireball, 5f, AbilityTargetingFlags.HarmsEnemies));
        detail.Targeting.Shape.Should().Be(AbilityTargetingShapeType.Sphere);
        detail.Targeting.Spell.Should().Be(Spell.Fireball);
        detail.Targeting.SizeX.Should().Be(5f);
        detail.Targeting.UpdatesClientTargeting.Should().BeTrue();

        detail = Built(NewAbility().HasTargetingLine(Spell.Fireball, 8f, 2f, AbilityTargetingFlags.HarmsEnemies));
        detail.Targeting.Shape.Should().Be(AbilityTargetingShapeType.Rect);
        (detail.Targeting.SizeX, detail.Targeting.SizeY).Should().Be((8f, 2f));

        detail = Built(NewAbility().HasTargetingCone(Spell.Fireball, 6f, 3f, AbilityTargetingFlags.HelpsAllies));
        detail.Targeting.Shape.Should().Be(AbilityTargetingShapeType.Cone);
        detail.Targeting.Flags.Should().Be(AbilityTargetingFlags.HelpsAllies);
    }

    [Test]
    public void ActivationOnlyTargetingNeverUpdatesTheClientTargeting()
    {
        var sphere = Built(NewAbility().HasActivationTargetingSphere(5f, AbilityTargetingFlags.None)).Targeting;
        var line = Built(NewAbility().HasActivationTargetingLine(8f, 2f, AbilityTargetingFlags.None)).Targeting;
        var cone = Built(NewAbility().HasActivationTargetingCone(6f, 3f, AbilityTargetingFlags.None)).Targeting;

        sphere.Shape.Should().Be(AbilityTargetingShapeType.Sphere);
        line.Shape.Should().Be(AbilityTargetingShapeType.Rect);
        cone.Shape.Should().Be(AbilityTargetingShapeType.Cone);
        new[] { sphere, line, cone }.Should().OnlyContain(t => !t.UpdatesClientTargeting && t.Spell == Spell.Invalid);
    }

    [Test]
    public void AdditionalActivationTargetingIsAppendedInOrder()
    {
        var detail = Built(NewAbility()
            .AddActivationTargetingSphere(5f, AbilityTargetingFlags.HarmsEnemies)
            .AddActivationTargetingLine(8f, 2f, AbilityTargetingFlags.HarmsAllies)
            .AddActivationTargetingCone(6f, 3f, AbilityTargetingFlags.HelpsAllies)
            .AddActivationTargeting(AbilityTargetingShapeType.HSphere, 4f, 0f, AbilityTargetingFlags.IgnoresSelf));

        detail.Targeting.Should().BeNull("the additional shapes do not replace the main one");
        detail.AdditionalActivationTargeting.Select(t => t.Shape).Should().Equal(
            AbilityTargetingShapeType.Sphere, AbilityTargetingShapeType.Rect, AbilityTargetingShapeType.Cone, AbilityTargetingShapeType.HSphere);
        detail.AdditionalActivationTargeting.Should().OnlyContain(t => !t.UpdatesClientTargeting);
    }

    [Test]
    public void ASizeResolverIsKeptWithTheTargeting()
    {
        AbilityTargetingSizeResolver resolver = (creature, size) => size * 2f;

        var detail = Built(NewAbility().HasTargetingSphere(Spell.Invalid, 5f, AbilityTargetingFlags.None, resolver));

        detail.Targeting.SizeResolver.Should().BeSameAs(resolver);
    }

    [Test]
    public void ASimpleSettingIsWrittenOntoTheAbility()
    {
        AbilityActivationDelayAction delay = (activator, target, level) => 1.5f;

        var detail = Built(NewAbility().CanBeUsedInSpace().PlaysSoundWhenActivating("snd_x").HasActivationDelay(delay));

        detail.CanBeUsedInSpace.Should().BeTrue();
        detail.ActivationSound.Should().Be("snd_x");
        detail.ActivationDelay.Should().BeSameAs(delay);
    }

    [Test]
    public void AFixedActivationDelayMustBeAPositiveFiniteNumberOfSeconds()
    {
        foreach (var bad in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
            ((Action)(() => NewAbility().HasFixedActivationDelay(bad))).Should().Throw<ArgumentOutOfRangeException>();

        var detail = Built(NewAbility().HasFixedActivationDelay(2f));

        detail.HasFixedActivationDelay.Should().BeTrue();
        detail.ActivationDelay(1, 2, 3).Should().Be(2f);
    }

    [Test]
    public void AnImmediateImpactAnimationNeedsADurationUpToTenMinutes()
    {
        foreach (var bad in new[] { 0f, -1f, 601f, float.NaN })
            ((Action)(() => NewAbility().UsesImmediateNativeImpactAnimation(bad))).Should().Throw<ArgumentOutOfRangeException>();

        Built(NewAbility().UsesImmediateNativeImpactAnimation(2.5f)).ImmediateNativeImpactAnimationDuration.Should().Be(2.5f);
    }

    [Test]
    public void ANativePreviewNeedsARealAnimationAndAPositiveSpeed()
    {
        ((Action)(() => NewAbility().UsesNativeAnimationPreview(Animation.Invalid, 1f))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => NewAbility().UsesNativeAnimationPreview((Animation)99999, 1f))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => NewAbility().UsesNativeAnimationPreview(Animation.Salute, 1f, 0f))).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void AnAnimationOverwriteNeedsBothNamesAndAPositiveRestoreDelay()
    {
        ((Action)(() => NewAbility().UsesAnimationOverwrite(Animation.Salute, " ", "replacement"))).Should().Throw<ArgumentException>();
        ((Action)(() => NewAbility().UsesAnimationOverwrite(Animation.Salute, "source", ""))).Should().Throw<ArgumentException>();
        ((Action)(() => NewAbility().UsesAnimationOverwrite(Animation.Salute, "source", "replacement", 0f)))
            .Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => NewAbility().UsesImpactAnimationOverwrite(Animation.Salute, null!, "replacement"))).Should().Throw<ArgumentException>();
        ((Action)(() => NewAbility().UsesImpactAnimationOverwrite(Animation.Salute, "source", null!))).Should().Throw<ArgumentException>();
        ((Action)(() => NewAbility().UsesImpactAnimationOverwrite(Animation.Salute, "source", "replacement", -2f)))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void ARefundRemovalNeedsAStatusEffectTypeAndRecordsEachTypeOnce()
    {
        ((Action)(() => NewAbility().RemoveStatusEffectOnPerkRefund(null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => NewAbility().RemoveSourceOwnedStatusEffectOnPerkRefund(null!))).Should().Throw<ArgumentNullException>();

        var detail = Built(NewAbility()
            .RemoveStatusEffectOnPerkRefund(typeof(string)).RemoveStatusEffectOnPerkRefund(typeof(string))
            .RemoveSourceOwnedStatusEffectOnPerkRefund(typeof(int)).RemoveSourceOwnedStatusEffectOnPerkRefund(typeof(int)));

        detail.StatusEffectTypesRemovedOnPerkRefund.Should().Equal(typeof(string));
        detail.SourceOwnedStatusEffectTypesRemovedOnPerkRefund.Should().Equal(typeof(int));
    }

    [Test]
    public void AMimicryTechniqueNeedsARealSourceFeatARankFromZeroToFiftyAndASlot()
    {
        ((Action)(() => NewAbility().MimicryTechnique(FeatType.Invalid, 10, 1))).Should().Throw<ArgumentException>();
        ((Action)(() => NewAbility().MimicryTechnique(FeatType.GuardCounter1, -1, 1))).Should().Throw<ArgumentException>();
        ((Action)(() => NewAbility().MimicryTechnique(FeatType.GuardCounter1, 51, 1))).Should().Throw<ArgumentException>();
        ((Action)(() => NewAbility().MimicryTechnique(FeatType.GuardCounter1, 10, 0))).Should().Throw<ArgumentException>();

        var detail = Built(NewAbility().MimicryTechnique(FeatType.GuardCounter1, 10, 2));

        detail.IsMimicryTechnique.Should().BeTrue();
        detail.MimicrySourceFeat.Should().Be(FeatType.GuardCounter1);
        detail.MimicrySkillRequirement.Should().Be(10);
    }
}
