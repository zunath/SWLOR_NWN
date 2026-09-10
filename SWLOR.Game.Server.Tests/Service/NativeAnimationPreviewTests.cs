using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Armor;
using SWLOR.Game.Server.Feature.AbilityDefinition.Force;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class NativeAnimationPreviewTests
{
    [Test]
    public void ThrowLightsaberRanksAndTesterRetainMastersNativeThrowAtDoubleSpeed()
    {
        var abilities = new ThrowLightsaberAbilityDefinition().BuildAbilities();
        var entry = ActiveAbilityAnimationCatalog.Entries.Single(entry => entry.Id == "ThrowLightsaber");
        abilities.Should().HaveCount(3);
        AbilityAnimationBinding.Apply(abilities, new[] { entry });
        foreach (var ability in abilities.Values)
        {
            ability.ActivationDelay(0, 0, ability.AbilityLevel).Should().Be(1.5f);
            ability.ImpactAnimationType.Should().Be(Animation.SaberThrow);
            ability.PreservesNativeAnimationChoreography.Should().BeTrue();
            ability.AuthoredAnimation.Should().BeNull();
            ability.AuthoredImpactAnimation.Should().BeNull();
            ability.NativeAnimationPreview.Should().Be(Animation.SaberThrow);
            ability.NativeAnimationPreviewSpeed.Should().Be(2f);
        }
        var preview = AnimationPreviewCatalog.CreateEntries(abilities.Values).Single(item => item.Id == entry.Id);
        preview.NativeAnimation.Should().Be(Animation.SaberThrow);
        preview.NativeAnimationSpeed.Should().Be(2f);
        preview.PreviewDuration.Should().BeApproximately(1.9165f, .0001f);
        preview.DurationText.Should().Be("Native");
    }

    [TestCase(0f)]
    [TestCase(-1f)]
    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    public void NativePreviewRejectsInvalidSpeed(float speed)
    {
        var builder = new AbilityBuilder().Create(FeatType.ThrowLightsaber1, SWLOR.Game.Server.Service.PerkService.PerkType.ThrowLightsaber);
        Action configure = () => builder.UsesNativeAnimationPreview(Animation.SaberThrow, 3.833f, speed);
        configure.Should().Throw<ArgumentOutOfRangeException>();
        Action preview = () => SWLOR.Game.Server.Service.NamedAnimation.PlayNativePreview(0, Animation.SaberThrow, speed);
        preview.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void DifferentNativeSpeedsAcrossRanksAreRejected()
    {
        var clip = ActiveAbilityAnimationCatalog.Entries.Single(entry => entry.Id == "ThrowLightsaber").Clip;
        var abilities = new[]
        {
            new AbilityDetail { PreviewAnimation = clip, NativeAnimationPreview = Animation.SaberThrow, NativeAnimationPreviewDuration = 3.833f, NativeAnimationPreviewSpeed = 1f },
            new AbilityDetail { PreviewAnimation = clip, NativeAnimationPreview = Animation.SaberThrow, NativeAnimationPreviewDuration = 3.833f, NativeAnimationPreviewSpeed = 2f }
        };
        Action create = () => AnimationPreviewCatalog.CreateEntries(abilities);
        create.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void BothProvokeRanksKeepNativeTauntAndTheirTesterEntryOverridesTheHistoricalClip()
    {
        var abilities = new ProvokeAbilityDefinition().BuildAbilities();
        var entry = ActiveAbilityAnimationCatalog.Entries.Single(entry => entry.Id == "Provoke");
        AbilityAnimationBinding.Apply(abilities, new[] { entry });
        abilities.Should().HaveCount(2);
        foreach (var ability in abilities.Values)
        {
            ability.ActivationDelay(0, 0, ability.AbilityLevel).Should().Be(1f);
            ability.PreservesNativeAnimationChoreography.Should().BeTrue();
            ability.AuthoredAnimation.Should().BeNull();
            ability.AuthoredImpactAnimation.Should().BeNull();
            AbilityAnimationBinding.ActivationType(ability, true, 1f).Should().Be(Animation.FireForgetTaunt);
            AbilityAnimationBinding.ActivationType(ability, false, 1f).Should().Be(Animation.FireForgetTaunt);
            ability.NativeAnimationPreview.Should().Be(Animation.FireForgetTaunt);
        }
        var previews = AnimationPreviewCatalog.CreateEntries(abilities.Values);
        var preview = AnimationPreviewCatalog.Search("Provoke", entries: previews).Single();
        preview.NativeAnimation.Should().Be(Animation.FireForgetTaunt);
        preview.DurationText.Should().Be("Native");
        preview.PreviewDuration.Should().Be(3f);
        var clock = new PreviewClock();
        var model = new SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.AnimationDebugViewModel(clock);
        model.LoadCatalog(previews);
        model.TryReservePreview(preview.PreviewDuration).Should().BeTrue();
        clock.Now = clock.Now.AddSeconds(2);
        model.TryReservePreview(AuthoredAnimation.SuppressionStance.Duration).Should().BeFalse();
        AnimationPreviewCatalog.Search(entry.Clip.Name, entries: previews).Should().Contain(preview);
    }

    [Test]
    public void NativeChoreographyWithoutAnExplicitPreviewOverrideStillUsesItsAuthoredPreview()
    {
        var abilities = new ForceLeapAbilityDefinition().BuildAbilities();
        var entry = ActiveAbilityAnimationCatalog.Entries.Single(entry => entry.Id == "ForceLeap");
        AbilityAnimationBinding.Apply(abilities, new[] { entry });
        var preview = AnimationPreviewCatalog.CreateEntries(abilities.Values).Single(preview => preview.Id == "ForceLeap");
        preview.NativeAnimation.Should().BeNull();
        preview.Clip.Should().BeSameAs(entry.Clip);
    }

    [Test]
    public void ConflictingNativeRankPreviewsFailRatherThanChoosingAnArbitraryRank()
    {
        var clip = ActiveAbilityAnimationCatalog.Entries.Single(entry => entry.Id == "Provoke").Clip;
        var abilities = new[]
        {
            new AbilityDetail { PreviewAnimation = clip, NativeAnimationPreview = Animation.FireForgetTaunt, NativeAnimationPreviewDuration = 3f },
            new AbilityDetail { PreviewAnimation = clip, NativeAnimationPreview = Animation.PointForward, NativeAnimationPreviewDuration = 3f }
        };
        Action create = () => AnimationPreviewCatalog.CreateEntries(abilities);
        create.Should().Throw<InvalidOperationException>();
    }

    private sealed class PreviewClock : TimeProvider
    {
        public DateTimeOffset Now = DateTimeOffset.UnixEpoch;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [TestCase(0f)]
    [TestCase(-1f)]
    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(601f)]
    public void NativePreviewRequiresAValidDuration(float duration)
    {
        var builder = new AbilityBuilder().Create(FeatType.Provoke1, SWLOR.Game.Server.Service.PerkService.PerkType.Provoke);
        Action configure = () => builder.UsesNativeAnimationPreview(Animation.FireForgetTaunt, duration);
        configure.Should().Throw<ArgumentOutOfRangeException>();
        var clip = AuthoredAnimation.Provoke;
        Action create = () => AnimationPreviewCatalog.CreateEntries(new[]
        {
            new AbilityDetail { PreviewAnimation = clip, NativeAnimationPreview = Animation.FireForgetTaunt, NativeAnimationPreviewDuration = duration }
        });
        create.Should().Throw<InvalidOperationException>();
    }
}
