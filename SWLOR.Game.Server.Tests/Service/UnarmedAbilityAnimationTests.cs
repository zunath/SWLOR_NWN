using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class UnarmedAbilityAnimationTests
{
    [TestCase("Uppercut", 1f)]
    [TestCase("Haymaker", 2f)]
    [TestCase("PerfectForm", 1f)]
    public void HeavyStrikesPlayDuringTheirExistingCastWithoutAddingASecondGesture(string id, float castSeconds)
    {
        var entry = ActiveAbilityAnimationCatalog.Entries.Single(entry => entry.Id == id);
        var type = typeof(IAbilityListDefinition).Assembly.GetTypes().Single(type => type.Name == id + "AbilityDefinition");
        var abilities = ((IAbilityListDefinition)Activator.CreateInstance(type)!).BuildAbilities();
        AbilityAnimationBinding.Apply(abilities, new[] { entry });
        AbilityAnimationBinding.Apply(abilities, new[] { entry });

        foreach (var ability in abilities.Values)
        {
            ability.ActivationDelay(0, 0, ability.AbilityLevel).Should().Be(castSeconds);
            ability.ImpactDelay.Should().Be(0f, "presentation must not delay damage or resumed combat");
            ability.UsesImmediateAuthoredAnimation.Should().BeTrue();
            ability.FitsAuthoredAnimationToCastWindow.Should().BeTrue();
            var duration = AbilityAnimationBinding.ImmediateDuration(ability, castSeconds)!.Value;
            duration.Should().BeLessThanOrEqualTo(castSeconds);
            (entry.Clip.Duration / entry.Clip.PlaybackSpeed(duration)).Should().BeApproximately(duration, .0001f,
                "the complete authored strike must finish before combat resumes");
            AbilityAnimationBinding.ActivationClip(ability, true, castSeconds).Should().BeSameAs(entry.Clip);
            AbilityAnimationBinding.ActivationClip(ability, false, castSeconds).Should().BeNull();
            AbilityAnimationBinding.ImpactClip(ability, true).Should().BeNull("the player already plays the strike while casting");
            ability.ImpactAnimationType.Should().Be(Animation.DoubleStrike, "NPCs retain their native impact fallback");
            ability.QueuedAttackAnimation.Should().BeNull("the gesture cannot occupy an extra combat action");
        }
    }

    [TestCase("Jab")]
    [TestCase("PalmStrike")]
    [TestCase("FinishingCombination")]
    [TestCase("DirtyJab")]
    [TestCase("LowKick")]
    public void InstantStrikesRetainOneImpactGesture(string id)
    {
        var entry = ActiveAbilityAnimationCatalog.Entries.Single(entry => entry.Id == id);
        var type = typeof(IAbilityListDefinition).Assembly.GetTypes().Single(type => type.Name == id + "AbilityDefinition");
        var abilities = ((IAbilityListDefinition)Activator.CreateInstance(type)!).BuildAbilities();
        AbilityAnimationBinding.Apply(abilities, new[] { entry });

        foreach (var ability in abilities.Values)
        {
            ability.ActivationDelay(0, 0, ability.AbilityLevel).Should().Be(0f);
            AbilityAnimationBinding.ActivationClip(ability, true, 0f).Should().BeNull();
            AbilityAnimationBinding.ImpactClip(ability, true).Should().BeSameAs(entry.Clip);
            ability.UsesImmediateAuthoredAnimation.Should().BeFalse();
            ability.FitsAuthoredAnimationToCastWindow.Should().BeFalse();
        }
    }

    [TestCase(1f)]
    [TestCase(.5f)]
    [TestCase(.1f)]
    public void PerfectFormCompletesItsStrikeInsideAnAdjustedCast(float castSeconds)
    {
        var entry = ActiveAbilityAnimationCatalog.Entries.Single(entry => entry.Id == "PerfectForm");
        var ability = new AbilityDetail { AuthoredAnimation = entry.Clip, FitsAuthoredAnimationToCastWindow = true };
        var duration = AbilityAnimationBinding.ImmediateDuration(ability, castSeconds)!.Value;
        var speed = entry.Clip.PlaybackSpeed(duration);

        duration.Should().Be(castSeconds);
        (1.2f / speed).Should().BeLessThan(castSeconds, "Perfect Form's completed strike is at 1.2 seconds in the source clip");
        (entry.Clip.Duration / speed).Should().BeApproximately(castSeconds, .0001f);
    }

    [TestCase(0f)]
    [TestCase(-1f)]
    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    public void NoCastWindowOmitsTheWindupWithoutRequestingAnInvalidPlaybackSpeed(float castSeconds)
    {
        var ability = new AbilityDetail
        {
            AuthoredAnimation = new AnimationClip("sw_test", 1.8f),
            FitsAuthoredAnimationToCastWindow = true
        };
        AbilityAnimationBinding.ImmediateDuration(ability, castSeconds).Should().Be(0f);
    }

    [Test]
    public void ExistingImmediateGesturesDoNotOptIntoCastFitting()
    {
        var type = typeof(IAbilityListDefinition).Assembly.GetTypes().Single(type => type.Name == "FlowStanceAbilityDefinition");
        var abilities = ((IAbilityListDefinition)Activator.CreateInstance(type)!).BuildAbilities();
        foreach (var ability in abilities.Values)
        {
            ability.UsesImmediateAuthoredAnimation.Should().BeTrue();
            ability.FitsAuthoredAnimationToCastWindow.Should().BeFalse();
            AbilityAnimationBinding.ImmediateDuration(ability, 0f).Should().BeNull();
        }
    }
}
