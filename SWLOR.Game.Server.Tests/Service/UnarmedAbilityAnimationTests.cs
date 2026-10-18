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
        }
    }
}
