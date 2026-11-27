using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Tests.Service;

public class AidEspionageAnimationTests
{
    private Support.FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new Support.FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void InstallFacades() => _facadeScope = _facades.Install();

    [TearDown]
    public void RemoveFacades() => _facadeScope.Dispose();

    [Test]
    public void EveryAidAndGhostRankPlaysOneShotWithoutChangingCastTimersOrNativeNpcPlayback()
    {
        var entries = ActiveAbilityAnimationCatalog.Entries
            .Where(entry => entry.Category == "First Aid" || entry.Id == "GhostProtocol").ToArray();
        entries.Should().HaveCount(13);
        foreach (var entry in entries)
        {
            // An entry can bind ranks from several definitions (Ghost Protocol's clip also plays for
            // Shared Presence), so build every definition and keep the entry's ranks.
            var abilities = typeof(IAbilityListDefinition).Assembly.GetTypes()
                .Where(type => !type.IsAbstract && !type.IsInterface && typeof(IAbilityListDefinition).IsAssignableFrom(type))
                .SelectMany(type => ((IAbilityListDefinition)Activator.CreateInstance(type)!).BuildAbilities())
                .Where(pair => entry.Feats.Contains(pair.Key))
                .ToDictionary(pair => pair.Key, pair => pair.Value);
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
