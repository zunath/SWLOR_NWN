using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service;

public class AidEspionageAnimationTests
{
    [Test]
    public void EveryAidAndGhostRankPlaysOneShotWithoutChangingCastTimersOrNativeNpcPlayback()
    {
        var entries = ActiveAbilityAnimationCatalog.Entries
            .Where(entry => entry.Category == "First Aid" || entry.Id == "GhostProtocol").ToArray();
        entries.Should().HaveCount(13);
        foreach (var entry in entries)
        {
            var type = typeof(IAbilityListDefinition).Assembly.GetTypes()
                .Single(type => type.Name == entry.Id + "AbilityDefinition");
            var abilities = ((IAbilityListDefinition)Activator.CreateInstance(type)!).BuildAbilities();
            var native = abilities.ToDictionary(pair => pair.Key, pair => pair.Value.AnimationType);
            var expectedCast = entry.Id switch
            {
                "EmergencyTriage" or "GhostProtocol" => 0f,
                "MedKit" or "KoltoMist" => 1.5f,
                "Resuscitation" => 4f,
                _ => 1f
            };
            AbilityAnimationBinding.Apply(abilities, new[] { entry });
            AbilityAnimationBinding.Apply(abilities, new[] { entry });
            foreach (var (feat, ability) in abilities)
            {
                ability.ActivationDelay(0, 0, ability.AbilityLevel).Should().Be(expectedCast);
                ability.ImpactDelay.Should().Be(0);
                ability.UsesImmediateAuthoredAnimation.Should().BeTrue();
                AbilityAnimationBinding.ActivationClip(ability, true, 0).Should().BeSameAs(entry.Clip,
                    "cast-speed bonuses and instant casts must not skip the intended gesture");
                AbilityAnimationBinding.ActivationClip(ability, false, 0).Should().BeNull();
                AbilityAnimationBinding.ActivationType(ability, false, 0).Should().Be(native[feat]);
                ability.AuthoredImpactAnimation.Should().BeNull("activation must not also play a second impact animation");
                ability.QueuedAttackAnimation.Should().BeNull();
                ability.PreviewAnimation.Should().BeSameAs(entry.Clip);
                ability.BreaksStealth.Should().Be(ability.SkillType == SkillType.FirstAid);
            }
        }
    }

    [Test]
    public void NativeStealthModeHasNoAuthoredClipOrTesterEntry()
    {
        ActiveAbilityAnimationCatalog.Entries.Should().NotContain(entry => entry.Id == "Stealth");
        AnimationPreviewCatalog.Search("Stealth").Should().NotContain(entry => entry.Id == "Stealth");
        typeof(AuthoredAnimation).GetField("Stealth").Should().BeNull();
    }
}
