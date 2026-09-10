using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class CategoryAnimationRoutingTests
{
    [TestCase("RazorTrap", 2f)]
    [TestCase("ShockTrap", 2f)]
    [TestCase("TacticalEscape", 0f)]
    [TestCase("Reward", 0f)]
    [TestCase("SoothePet", 1f)]
    [TestCase("Tame", 18f)]
    [TestCase("BerserkerStance", 2f)]
    [TestCase("DefensiveStance", 2f)]
    public void SupportingGesturesPlayOnceWithoutExtendingTheirGameplayCast(string id, float castSeconds)
    {
        var entry = ActiveAbilityAnimationCatalog.Entries.Single(entry => entry.Id == id);
        var abilities = typeof(IAbilityListDefinition).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && !type.IsInterface && typeof(IAbilityListDefinition).IsAssignableFrom(type))
            .SelectMany(type => ((IAbilityListDefinition)Activator.CreateInstance(type)!).BuildAbilities())
            .Where(pair => entry.Feats.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value);
        abilities.Keys.Should().BeEquivalentTo(entry.Feats);
        var original = abilities.ToDictionary(pair => pair.Key, pair =>
            (pair.Value.AnimationType, pair.Value.ActivationDelay, pair.Value.ImpactDelay, pair.Value.ImpactAction));
        AbilityAnimationBinding.Apply(abilities, new[] { entry });
        foreach (var (feat, ability) in abilities)
        {
            (ability.ActivationDelay?.Invoke(0, 0, ability.AbilityLevel) ?? 0f).Should().Be(castSeconds);
            ability.ActivationDelay.Should().BeSameAs(original[feat].ActivationDelay);
            ability.ImpactDelay.Should().Be(original[feat].ImpactDelay);
            ability.ImpactAction.Should().BeSameAs(original[feat].ImpactAction);
            ability.UsesImmediateAuthoredAnimation.Should().BeTrue();
            AbilityAnimationBinding.ActivationClip(ability, true, 0).Should().BeSameAs(entry.Clip);
            AbilityAnimationBinding.ActivationClip(ability, false, 0).Should().BeNull();
            AbilityAnimationBinding.ActivationType(ability, false, 0).Should().Be(original[feat].AnimationType);
            ability.AuthoredImpactAnimation.Should().BeNull();
            ability.QueuedAttackAnimation.Should().BeNull();
        }
    }

    [Test]
    public void RangedScriptedImpactsUseNamedGesturesWhileQueuedAttacksKeepNativeFire()
    {
        var stanceIds = new[] { "GamblerStance", "SkirmisherStance", "SniperStance", "SuppressionStance", "FlurryStance", "OrdnanceStance" };
        var entries = ActiveAbilityAnimationCatalog.Entries.Where(entry => entry.Category is "Pistol" or "Rifle" or "Throwing").ToArray();
        var abilities = typeof(IAbilityListDefinition).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && !type.IsInterface && typeof(IAbilityListDefinition).IsAssignableFrom(type))
            .SelectMany(type => ((IAbilityListDefinition)Activator.CreateInstance(type)!).BuildAbilities())
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        var native = abilities.ToDictionary(pair => pair.Key, pair => (pair.Value.AnimationType, pair.Value.ImpactAnimationType,
            pair.Value.ActivationDelay, pair.Value.ImpactDelay, pair.Value.ImpactAction));
        AbilityAnimationBinding.Apply(abilities, entries);
        foreach (var entry in entries)
        foreach (var feat in entry.Feats)
        {
            var ability = abilities[feat];
            ability.ImpactAnimationType.Should().Be(native[feat].ImpactAnimationType);
            ability.QueuedAttackAnimation.Should().BeNull();
            ability.ActivationDelay.Should().BeSameAs(native[feat].ActivationDelay);
            ability.ImpactDelay.Should().Be(native[feat].ImpactDelay);
            ability.ImpactAction.Should().BeSameAs(native[feat].ImpactAction);
            AbilityAnimationBinding.ImpactClip(ability, false).Should().BeNull();
            AbilityAnimationBinding.ActivationClip(ability, false, 0).Should().BeNull();
            if (stanceIds.Contains(entry.Id))
            {
                ability.IsHostileAbility.Should().BeFalse();
                ability.AuthoredImpactAnimation.Should().BeNull();
                ability.ActivationDelay(0, 0, ability.AbilityLevel).Should().Be(2f);
                ability.ActivationType.Should().Be(AbilityActivationType.Casted);
                AbilityAnimationBinding.ActivationClip(ability, true, 0).Should().BeSameAs(entry.Clip);
            }
            else if (ability.ActivationType == AbilityActivationType.Weapon)
            {
                ability.AuthoredAnimation.Should().BeNull("queued ranged fire must keep its existing carrier");
                ability.AuthoredImpactAnimation.Should().BeNull();
                ability.AnimationType.Should().Be(native[feat].AnimationType);
            }
            else
            {
                ability.IsHostileAbility.Should().BeTrue();
                ability.AuthoredAnimation.Should().BeNull("scripted attacks play once at impact, not at both stages");
                AbilityAnimationBinding.ImpactClip(ability, true).Should().BeSameAs(entry.Clip);
                ability.AnimationType.Should().Be(native[feat].AnimationType);
            }
        }
        entries.Where(entry => stanceIds.Contains(entry.Id)).Should().HaveCount(6);
    }

    [Test]
    public void ReviveBeastPlaysItsKneelingMotionOnceWhileKeepingTheFourSecondReviveCast()
    {
        var abilities = new SWLOR.Game.Server.Feature.AbilityDefinition.Beastmaster.ReviveBeastAbilityDefinition().BuildAbilities();
        var entry = ActiveAbilityAnimationCatalog.Entries.Single(entry => entry.Id == "ReviveBeast");
        abilities.Keys.Should().BeEquivalentTo(entry.Feats);
        AbilityAnimationBinding.Apply(abilities, new[] { entry });
        foreach (var ability in abilities.Values)
        {
            ability.ActivationDelay(0, 0, ability.AbilityLevel).Should().Be(4f);
            ability.ImpactDelay.Should().Be(0f);
            ability.UsesImmediateAuthoredAnimation.Should().BeTrue();
            AbilityAnimationBinding.ActivationClip(ability, true, 0).Should().BeSameAs(entry.Clip);
            AbilityAnimationBinding.ActivationClip(ability, false, 0).Should().BeNull();
            AbilityAnimationBinding.ActivationType(ability, false, 0).Should().Be(Animation.LoopingGetMid);
            ability.AuthoredImpactAnimation.Should().BeNull();
        }
    }

    [TestCase("Katar", 11)]
    [TestCase("Lightsaber", 11)]
    [TestCase("Saberstaff", 10)]
    [TestCase("Spear", 10)]
    [TestCase("Staff", 11)]
    [TestCase("Twin Blade", 10)]
    [TestCase("Vibroknife", 10)]
    [TestCase("Leadership", 12)]
    [TestCase("Mimicry", 68)]
    public void CurrentRanksUseTheirCategoryMotionWithoutChangingGameplayContract(string category, int count)
    {
        var entries = ActiveAbilityAnimationCatalog.Entries.Where(entry => entry.Category == category).ToArray();
        entries.Should().HaveCount(count);
        var abilities = typeof(IAbilityListDefinition).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && !type.IsInterface && typeof(IAbilityListDefinition).IsAssignableFrom(type))
            .SelectMany(type => ((IAbilityListDefinition)Activator.CreateInstance(type)!).BuildAbilities())
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        var selected = entries.SelectMany(entry => entry.Feats).ToHashSet();
        var before = abilities.Where(pair => selected.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair =>
            (pair.Value.AnimationType, pair.Value.ActivationType, pair.Value.ActivationDelay,
                pair.Value.ImpactDelay, pair.Value.ImpactAction, pair.Value.ActivationAction));
        AbilityAnimationBinding.Apply(abilities, entries);
        AbilityAnimationBinding.Apply(abilities, entries);
        foreach (var entry in entries)
        foreach (var feat in entry.Feats)
        {
            var ability = abilities[feat];
            var original = before[feat];
            ability.PreviewAnimation.Should().BeSameAs(entry.Clip);
            ability.ActivationType.Should().Be(original.ActivationType, feat.ToString());
            ability.ActivationDelay.Should().BeSameAs(original.ActivationDelay);
            ability.ImpactDelay.Should().Be(original.ImpactDelay);
            ability.ActivationAction.Should().BeSameAs(original.ActivationAction);
            ability.ImpactAction.Should().BeSameAs(original.ImpactAction);
            AbilityAnimationBinding.ActivationClip(ability, false, 0).Should().BeNull();
            AbilityAnimationBinding.ImpactClip(ability, false).Should().BeNull();
            AbilityAnimationBinding.QueuedClip(ability, false).Should().BeNull();
            if (ability.ActivationType == AbilityActivationType.Weapon)
            {
                AbilityAnimationBinding.QueuedClip(ability, true).Should().BeSameAs(entry.Clip);
                ability.AuthoredAnimation.Should().BeNull();
                ability.AuthoredImpactAnimation.Should().BeNull();
            }
            else if (ability.PreservesNativeAnimationChoreography)
            {
                ability.AuthoredAnimation.Should().BeNull();
                ability.AuthoredImpactAnimation.Should().BeNull();
                ability.AnimationType.Should().Be(original.AnimationType);
            }
            else if (ability.IsHostileAbility && category is not ("Leadership" or "Mimicry"))
            {
                AbilityAnimationBinding.ImpactClip(ability, true).Should().BeSameAs(entry.Clip, feat.ToString());
                ability.AuthoredAnimation.Should().BeNull("a cast must not play the same motion at both stages");
            }
            else
            {
                AbilityAnimationBinding.ActivationClip(ability, true, 0).Should().BeSameAs(entry.Clip, feat.ToString());
                ability.AuthoredImpactAnimation.Should().BeNull();
                AbilityAnimationBinding.ActivationType(ability, false, 0).Should().Be(original.AnimationType);
            }
        }
    }
}
