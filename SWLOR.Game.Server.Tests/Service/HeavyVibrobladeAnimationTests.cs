using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class HeavyVibrobladeAnimationTests
{
    [Test]
    public void AllHeavyVibrobladeRanksUseTheirAuthoredMotionWithoutChangingCastOrWeaponTiming()
    {
        var entries = ActiveAbilityAnimationCatalog.Entries.Where(entry => entry.Category == "Heavy Vibroblade").ToArray();
        entries.Should().HaveCount(12);
        entries.Select(entry => entry.Clip.Name).Should().OnlyHaveUniqueItems();
        var definitions = typeof(IAbilityListDefinition).Assembly.GetTypes();
        foreach (var entry in entries)
        {
            var definition = definitions.Single(type => type.Name == entry.Id + "AbilityDefinition");
            var abilities = ((IAbilityListDefinition)Activator.CreateInstance(definition)!).BuildAbilities();
            abilities.Keys.Should().BeEquivalentTo(entry.Feats);
            var original = abilities.ToDictionary(pair => pair.Key, pair =>
                (pair.Value.AnimationType, pair.Value.ActivationType, pair.Value.ImpactAction, pair.Value.ActivationAction));
            AbilityAnimationBinding.Apply(abilities, new[] { entry });
            AbilityAnimationBinding.Apply(abilities, new[] { entry });
            foreach (var (feat, ability) in abilities)
            {
                ability.ActivationDelay(0, 0, ability.AbilityLevel).Should().Be(
                    entry.Id is "BastionStance" or "SoulDevourer" ? 2f : 0f, entry.Id);
                ability.ActivationType.Should().Be(original[feat].ActivationType);
                ability.ImpactDelay.Should().Be(0);
                ability.ImpactAnimationType.Should().Be(Animation.Invalid, "no second impact gesture should follow activation");
                ability.ImpactAction.Should().BeSameAs(original[feat].ImpactAction);
                ability.ActivationAction.Should().BeSameAs(original[feat].ActivationAction);
                ability.PreviewAnimation.Should().BeSameAs(entry.Clip);
                ability.AuthoredImpactAnimation.Should().BeNull();
                AbilityAnimationBinding.ActivationClip(ability, false, 0).Should().BeNull();
                AbilityAnimationBinding.QueuedClip(ability, false).Should().BeNull();
                if (entry.Id is "FortressStrike" or "SoulStrike")
                {
                    ability.ActivationType.Should().Be(AbilityActivationType.Weapon);
                    ability.UsesImmediateAuthoredAnimation.Should().BeFalse();
                    ability.AuthoredAnimation.Should().BeNull();
                    AbilityAnimationBinding.QueuedClip(ability, true).Should().BeSameAs(entry.Clip);
                }
                else
                {
                    ability.ActivationType.Should().Be(AbilityActivationType.Casted);
                    ability.UsesImmediateAuthoredAnimation.Should().BeTrue();
                    ability.QueuedAttackAnimation.Should().BeNull();
                    AbilityAnimationBinding.ActivationClip(ability, true, 0).Should().BeSameAs(entry.Clip,
                        "the one-shot must still play when the cast window is shorter than the motion");
                    AbilityAnimationBinding.ActivationType(ability, false, 0).Should().Be(original[feat].AnimationType);
                }
            }
        }
    }

    [Test]
    public void EveryHeavyVibrobladeMotionIsSearchableUnderItsCategoryAndInternalName()
    {
        var entries = ActiveAbilityAnimationCatalog.Entries.Where(entry => entry.Category == "Heavy Vibroblade").ToArray();
        var previews = AnimationPreviewCatalog.CreateEntries(Array.Empty<AbilityDetail>());
        AnimationPreviewCatalog.Search("", "Heavy Vibroblade", previews).Select(preview => preview.Id)
            .Should().BeEquivalentTo(entries.Select(entry => entry.Id));
        foreach (var entry in entries)
        {
            var matches = AnimationPreviewCatalog.Search(entry.Clip.Name, "Heavy Vibroblade", previews);
            matches.Should().Contain(preview => preview.Id == entry.Id && ReferenceEquals(preview.Clip, entry.Clip));
            matches.Single(preview => preview.Id == entry.Id).NativeAnimation.Should().BeNull();
        }
    }
}
