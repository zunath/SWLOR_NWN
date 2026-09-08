using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.AbilityDefinition.Katar;
using SWLOR.Game.Server.Feature.AbilityDefinition.Force;
using SWLOR.Game.Server.Feature.AbilityDefinition.Espionage;
using SWLOR.Game.Server.Feature.AbilityDefinition.FirstAid;
using SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry;
using SWLOR.Game.Server.Feature;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class AbilityAnimationBindingTests
{
    private static readonly AnimationClip Clip = new("sw_test", 1f);
    private static AbilityAnimationEntry Entry(params FeatType[] feats) => new("Test", "Test", "Force", Clip, feats);

    [Test]
    public void NativeThrowSelectorsStillSuspendPistolRemappingForPlayersAndNpcs()
    {
        foreach (var definition in new IAbilityListDefinition[]
                 { new KoltoMistAbilityDefinition(), new PiercingQuillsTechniqueAbilityDefinition() })
        {
            var abilities = definition.BuildAbilities();
            AbilityAnimationBinding.Apply(abilities, ActiveAbilityAnimationCatalog.Entries
                .Where(entry => entry.Feats.Any(abilities.ContainsKey)));
            foreach (var ability in abilities.Values)
            foreach (var isPlayer in new[] { true, false })
            {
                ability.PreviewAnimation.Should().NotBeNull();
                AbilityAnimationBinding.ActivationClip(ability, isPlayer).Should().BeNull();
                var selected = AbilityAnimationBinding.ActivationType(ability, isPlayer);
                selected.Should().Be(Animation.ThrowGrenade);
                PistolAnimationRemap.ShouldSuspendForExplicitThrow(selected, true).Should().BeTrue();
            }
        }
    }

    [Test]
    public void MovementImpactAbilitiesKeepNativeActionOrderingAndCatalogPreviews()
    {
        foreach (var definition in new IAbilityListDefinition[]
                 { new ForceLeapAbilityDefinition(), new ForceInterceptAbilityDefinition(), new ShadowStepAbilityDefinition() })
        {
            var abilities = definition.BuildAbilities();
            var before = abilities.ToDictionary(pair => pair.Key, pair => pair.Value.AnimationType);
            var entries = ActiveAbilityAnimationCatalog.Entries.Where(entry => entry.Feats.Any(abilities.ContainsKey)).ToArray();
            entries.Should().NotBeEmpty();
            AbilityAnimationBinding.Apply(abilities, entries);
            foreach (var (feat, ability) in abilities)
            {
                ability.PreservesNativeAnimationChoreography.Should().BeTrue();
                ability.PreviewAnimation.Should().NotBeNull();
                ability.AuthoredAnimation.Should().BeNull();
                ability.AnimationType.Should().Be(before[feat]);
            }
        }
    }

    [Test]
    public void KatarGuardCounterKeepsEquipmentOwnedSwingMappingsAndRemainsPreviewable()
    {
        var abilities = new GuardCounterAbilityDefinition().BuildAbilities();
        var before = abilities.ToDictionary(pair => pair.Key, pair => pair.Value.AnimationType);
        var entries = ActiveAbilityAnimationCatalog.Entries.Where(entry => entry.Id == "GuardCounter").ToArray();
        entries.Should().ContainSingle();
        AbilityAnimationBinding.Apply(abilities, entries);
        foreach (var (feat, ability) in abilities)
        {
            ability.SkillType.Should().Be(SkillType.Katar);
            ability.PreviewAnimation.Should().BeSameAs(entries[0].Clip);
            ability.QueuedAttackAnimation.Should().BeNull("Katar owns 1h-to-unarmed replacements which queued cleanup would erase");
            ability.HasGeneratedAnimationBinding.Should().BeFalse();
            ability.AnimationType.Should().Be(before[feat]);
        }
    }

    [Test]
    public void GeneratedCatalogBindsEveryDeclaredLiveRank()
    {
        var abilities = typeof(IAbilityListDefinition).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && !type.IsInterface && typeof(IAbilityListDefinition).IsAssignableFrom(type))
            .SelectMany(type => ((IAbilityListDefinition)Activator.CreateInstance(type)!).BuildAbilities())
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        AbilityAnimationBinding.Apply(abilities, ActiveAbilityAnimationCatalog.Entries);
        foreach (var entry in ActiveAbilityAnimationCatalog.Entries)
        foreach (var feat in entry.Feats)
            abilities[feat].PreviewAnimation.Should().BeSameAs(entry.Clip);
    }

    [Test]
    public void EveryRankUsesItsExistingActivationContractWithoutChangingTiming()
    {
        var cast = new AbilityDetail { ActivationType = AbilityActivationType.Casted, ImpactDelay = .25f };
        var queued = new AbilityDetail { ActivationType = AbilityActivationType.Weapon };
        var abilities = new Dictionary<FeatType, AbilityDetail> { [(FeatType)1] = cast, [(FeatType)2] = queued };
        AbilityAnimationBinding.Apply(abilities, new[] { Entry((FeatType)1, (FeatType)2) });
        cast.AuthoredAnimation.Should().BeSameAs(Clip);
        cast.ImpactDelay.Should().Be(.25f);
        queued.AuthoredAnimation.Should().BeNull();
        queued.QueuedAttackAnimation.Should().BeSameAs(Clip);
        abilities.Values.Should().OnlyContain(ability => ability.PreviewAnimation == Clip);
    }

    [Test]
    public void NativeTimingSensitiveContractsKeepTheirPlaybackAndGainPreviewReferences()
    {
        var abilities = new[]
        {
            new AbilityDetail { IsChanneled = true },
            new AbilityDetail { CanBeUsedInSpace = true },
            new AbilityDetail { PreservesStealthDuringActivation = true },
            new AbilityDetail { AnimationReplacementAnimationName = "throw" },
            new AbilityDetail { ImpactAnimationType = Animation.PointForward },
            new AbilityDetail { SkillType = SkillType.Pistol },
            new AbilityDetail { SkillType = SkillType.Rifle },
            new AbilityDetail { SkillType = SkillType.Throwing },
        }.Select((ability, index) => (ability, feat: (FeatType)(index + 1)))
            .ToDictionary(pair => pair.feat, pair => pair.ability);
        AbilityAnimationBinding.Apply(abilities, new[] { Entry(abilities.Keys.ToArray()) });
        abilities.Values.Should().OnlyContain(ability => ability.PreviewAnimation == Clip && ability.AuthoredAnimation == null);
        abilities[(FeatType)4].AnimationReplacementAnimationName.Should().Be("throw");
    }

    [Test]
    public void GeneratedBindingsPreserveNativeNpcPlaybackAndExplicitBindingsRemainAvailable()
    {
        var cast = new AbilityDetail { ActivationType = AbilityActivationType.Casted, AnimationType = Animation.LoopingGetMid };
        var queued = new AbilityDetail { ActivationType = AbilityActivationType.Weapon };
        AbilityAnimationBinding.Apply(new Dictionary<FeatType, AbilityDetail> { [(FeatType)1] = cast, [(FeatType)2] = queued },
            new[] { Entry((FeatType)1, (FeatType)2) });
        AbilityAnimationBinding.ActivationClip(cast, false).Should().BeNull();
        AbilityAnimationBinding.ActivationType(cast, false).Should().Be(Animation.LoopingGetMid);
        AbilityAnimationBinding.ActivationClip(cast, true).Should().BeSameAs(Clip);
        AbilityAnimationBinding.QueuedClip(queued, false).Should().BeNull();
        AbilityAnimationBinding.QueuedClip(queued, true).Should().BeSameAs(Clip);
        AbilityAnimationBinding.ActivationClip(new AbilityDetail { AuthoredAnimation = Clip }, false).Should().BeSameAs(Clip);
    }

    [Test]
    public void StaleDuplicateAndPassiveMappingsFailInsteadOfSilentlySkippingCoverage()
    {
        var abilities = new Dictionary<FeatType, AbilityDetail> { [(FeatType)1] = new() };
        Action missing = () => AbilityAnimationBinding.Apply(abilities, new[] { Entry((FeatType)2) });
        missing.Should().Throw<InvalidOperationException>().WithMessage("*missing*");
        Action duplicate = () => AbilityAnimationBinding.Apply(abilities, new[] { Entry((FeatType)1, (FeatType)1) });
        duplicate.Should().Throw<InvalidOperationException>().WithMessage("*Duplicate*");
        abilities[(FeatType)1].IsMimicryTrait = true;
        Action passive = () => AbilityAnimationBinding.Apply(abilities, new[] { Entry((FeatType)1) });
        passive.Should().Throw<InvalidOperationException>().WithMessage("*passive*");
    }
}
