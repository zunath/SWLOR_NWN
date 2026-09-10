using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class ForceAbilityAnimationTests
{
    private static readonly HashSet<string> ImpactAbilities = new()
    {
        "EclipseOfResolve", "ForceBurst", "ForceChoke", "ForceDrain",
        "ForceJudgment", "ForceSpark", "MindTrick", "NightmareField", "RadiantLance", "WeakenResolve"
    };

    private static float ExpectedCast(string id, int level) => id switch
    {
        "ForcePush" or "HungerOfTheDark" or "ForceLeap" or "ForceIntercept" => 0f,
        "FuryStance" => 2f,
        "CreepingTerror" => level == 3 ? 1.5f : 1f,
        "Benevolence" or "ForceDrain" or "ForceSpark" or "GuardianWard" or "MindTrick" or
            "Renewal" or "WeakenResolve" => 1f,
        _ => 1.5f
    };

    [Test]
    public void EveryForceRankUsesTheIntendedStageWithoutChangingCastTimingOrNpcPlayback()
    {
        var entries = ActiveAbilityAnimationCatalog.Entries.Where(entry => entry.Category == "Force").ToArray();
        entries.Should().HaveCount(25);
        var definitions = typeof(IAbilityListDefinition).Assembly.GetTypes();
        foreach (var entry in entries)
        {
            var type = definitions.Single(type => type.Name == entry.Id + "AbilityDefinition");
            var abilities = ((IAbilityListDefinition)Activator.CreateInstance(type)!).BuildAbilities();
            abilities.Keys.Should().BeEquivalentTo(entry.Feats, "every real Force rank needs a catalog binding");
            var native = abilities.ToDictionary(pair => pair.Key, pair => pair.Value.AnimationType);
            var nativeImpact = abilities.ToDictionary(pair => pair.Key, pair => pair.Value.ImpactAnimationType);
            AbilityAnimationBinding.Apply(abilities, new[] { entry });
            AbilityAnimationBinding.Apply(abilities, new[] { entry });
            foreach (var (feat, ability) in abilities)
            {
                ability.ActivationDelay(0, 0, ability.AbilityLevel).Should().Be(ExpectedCast(entry.Id, ability.AbilityLevel), entry.Id);
                ability.ImpactDelay.Should().Be(0, entry.Id);
                ability.IsChanneled.Should().BeFalse("current Force effects are timed effects, not caster channels");
                ability.PreviewAnimation.Should().BeSameAs(entry.Clip);
                ability.QueuedAttackAnimation.Should().BeNull();
                AbilityAnimationBinding.ActivationClip(ability, false, 0).Should().BeNull();
                AbilityAnimationBinding.ImpactClip(ability, false).Should().BeNull();
                AbilityAnimationBinding.ActivationType(ability, false, 0).Should().Be(native[feat]);
                ability.ImpactAnimationType.Should().Be(nativeImpact[feat]);

                if (ImpactAbilities.Contains(entry.Id))
                {
                    ability.UsesAuthoredImpactAnimation.Should().BeTrue(entry.Id);
                    AbilityAnimationBinding.ImpactClip(ability, true).Should().BeSameAs(entry.Clip);
                    ability.AuthoredAnimation.Should().BeNull("impact-owned Force motions must not play twice");
                    ability.UsesImmediateAuthoredAnimation.Should().BeFalse();
                }
                else if (entry.Id is "ForceLeap" or "ForceIntercept" or "ThrowLightsaber" or "ForceLightning")
                {
                    ability.AuthoredAnimation.Should().BeNull("native movement, throw, and Lightning choreography must remain authoritative");
                    ability.AuthoredImpactAnimation.Should().BeNull();
                    ability.UsesImmediateAuthoredAnimation.Should().BeFalse();
                    ability.UsesAuthoredImpactAnimation.Should().BeFalse();
                    if (entry.Id is "ForceLeap" or "ForceIntercept")
                        ability.PreservesNativeAnimationChoreography.Should().BeTrue();
                    if (entry.Id == "ThrowLightsaber")
                        ability.ImpactAnimationType.Should().Be(Animation.SaberThrow);
                    if (entry.Id == "ForceLightning")
                    {
                        ability.ImpactAnimationType.Should().Be(Animation.CastOutAnimation);
                        ability.ImmediateNativeImpactAnimationDuration.Should().Be(3f,
                            "master Lightning plays the native CastOut gesture for three seconds at impact");
                    }
                }
                else
                {
                    ability.UsesImmediateAuthoredAnimation.Should().BeTrue(entry.Id);
                    AbilityAnimationBinding.ActivationClip(ability, true, 0).Should().BeSameAs(entry.Clip,
                        "instant and shortened casts still play the one-shot without extending their action queue");
                    ability.AuthoredImpactAnimation.Should().BeNull();
                    ability.UsesAuthoredImpactAnimation.Should().BeFalse();
                }
            }
        }
    }

    [Test]
    public void AllForceMotionsIncludingNativeGameplayExceptionsRemainSearchableInTheTester()
    {
        var entries = ActiveAbilityAnimationCatalog.Entries.Where(entry => entry.Category == "Force").ToArray();
        var previews = AnimationPreviewCatalog.CreateEntries(Array.Empty<AbilityDetail>());
        AnimationPreviewCatalog.Search("", "Force", previews).Select(entry => entry.Id)
            .Should().BeEquivalentTo(entries.Select(entry => entry.Id));
        foreach (var entry in entries)
            AnimationPreviewCatalog.Search(entry.Id, "Force", previews)
                .Should().Contain(preview => preview.Id == entry.Id && ReferenceEquals(preview.Clip, entry.Clip));
    }
}
