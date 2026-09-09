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
            new AbilityDetail { PreviewAnimation = clip, NativeAnimationPreview = Animation.FireForgetTaunt },
            new AbilityDetail { PreviewAnimation = clip, NativeAnimationPreview = Animation.PointForward }
        };
        Action create = () => AnimationPreviewCatalog.CreateEntries(abilities);
        create.Should().Throw<InvalidOperationException>();
    }
}
