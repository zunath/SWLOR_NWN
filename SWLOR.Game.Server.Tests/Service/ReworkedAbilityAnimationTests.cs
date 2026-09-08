using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class ReworkedAbilityAnimationTests
{
    private static IEnumerable<AbilityAnimationEntry> Entries => ActiveAbilityAnimationCatalog.Entries.Where(entry =>
        entry.Category == "Devices" || entry.Id is "CallBeast" or "GuardingBond" or "PredatoryBond");

    [Test]
    public void EveryReworkedAbilityRankUsesItsDeclaredGameplayStageAndKeepsNativeNpcFallback()
    {
        Entries.Should().HaveCount(30);
        foreach (var entry in Entries)
        {
            var type = typeof(IAbilityListDefinition).Assembly.GetTypes().Single(type => type.Name == entry.Id + "AbilityDefinition");
            var abilities = ((IAbilityListDefinition)Activator.CreateInstance(type)!).BuildAbilities();
            var native = abilities.ToDictionary(pair => pair.Key, pair => pair.Value.AnimationType);
            AbilityAnimationBinding.Apply(abilities, new[] { entry });
            AbilityAnimationBinding.Apply(abilities, new[] { entry });
            foreach (var (feat, ability) in abilities)
            {
                ability.PreviewAnimation.Should().BeSameAs(entry.Clip);
                AbilityAnimationBinding.ActivationClip(ability, false, 0).Should().BeNull();
                AbilityAnimationBinding.ImpactClip(ability, false).Should().BeNull();
                AbilityAnimationBinding.ActivationType(ability, false, 0).Should().Be(native[feat]);
                if (entry.Id == "Flamethrower")
                {
                    ability.ImmediateNativeImpactAnimationDuration.Should().Be(2.1f);
                    ability.ImpactAnimationType.Should().Be(Animation.CastOutAnimation);
                    ability.AuthoredAnimation.Should().BeNull();
                    ability.AuthoredImpactAnimation.Should().BeNull();
                }
                else if (ability.UsesAuthoredImpactAnimation)
                {
                    ability.AuthoredAnimation.Should().BeNull("impact-owned devices must not play twice");
                    AbilityAnimationBinding.ImpactClip(ability, true).Should().BeSameAs(entry.Clip);
                    ability.UsesImmediateAuthoredAnimation.Should().BeFalse();
                }
                else
                {
                    ability.UsesImmediateAuthoredAnimation.Should().BeTrue();
                    AbilityAnimationBinding.ActivationClip(ability, true, 0).Should().BeSameAs(entry.Clip,
                        "instant gestures play directly without adding their duration to the action queue");
                    ability.AuthoredImpactAnimation.Should().BeNull();
                }
                ability.QueuedAttackAnimation.Should().BeNull();
            }
        }
    }

    [TestCase("ClusterGrenade")]
    [TestCase("ConcussionGrenade")]
    [TestCase("FlashGrenade")]
    [TestCase("FragGrenade")]
    public void GrenadesKeepNativeNpcThrowAndInstantImpactWhilePlayersUseAuthoredClips(string id)
    {
        var entry = Entries.Single(entry => entry.Id == id);
        var type = typeof(IAbilityListDefinition).Assembly.GetTypes().Single(type => type.Name == id + "AbilityDefinition");
        var abilities = ((IAbilityListDefinition)Activator.CreateInstance(type)!).BuildAbilities();
        AbilityAnimationBinding.Apply(abilities, new[] { entry });
        foreach (var ability in abilities.Values)
        {
            ability.ImpactAnimationType.Should().Be(Animation.ThrowGrenade);
            ability.AuthoredImpactAnimation.Should().BeSameAs(entry.Clip);
            AbilityAnimationBinding.ImpactClip(ability, false).Should().BeNull();
            AbilityAnimationBinding.ImpactClip(ability, true).Should().BeSameAs(entry.Clip);
            ability.AuthoredAnimation.Should().BeNull();
            ability.ImpactDelay.Should().Be(0);
        }
    }

    [Test]
    public void ImmediateGesturePipelinesDoNotEnqueueAnimationActions()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln"))) directory = directory.Parent;
        directory.Should().NotBeNull();
        var root = directory!.FullName;
        var activation = File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server/Feature/UsePerkFeat.cs"));
        activation.Should().MatchRegex(@"if \(ability\.UsesImmediateAuthoredAnimation\)\s*NamedAnimation\.Play\(activator, authoredClip\);\s*else\s*NamedAnimation\.Queue");
        var impact = File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server/Service/Ability.cs"));
        var authoredBranch = impact[impact.IndexOf("if (authoredImpact != null)")..impact.IndexOf("if (trackedAbility?.ImmediateNativeImpactAnimationDuration")];
        authoredBranch.Should().Contain("NamedAnimation.Play(activator, authoredImpact)");
        authoredBranch.Should().NotContain("ActionPlayAnimation");
        authoredBranch.Should().NotContain("PistolAnimationRemap");
        authoredBranch.Should().NotContain("throwr");
        impact.Should().Contain("trackedAbility.ImmediateNativeImpactAnimationDuration, immediate: true)");
        var remap = File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server/Feature/PistolAnimationRemap.cs"));
        remap.Should().Contain("if (immediate) PlayAnimation(animation, speed, durationSeconds);");
        remap.Should().Contain("if (immediate) NamedAnimation.ReleaseForNativePlayback(creature);");
    }
}
