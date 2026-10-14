using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.FirstAid;
using SWLOR.Game.Server.Feature.AbilityDefinition.Force;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class QuickCastTests
{
    [Test]
    public void Modes_StartDisabled_AreIndependentAndResetEachSession()
    {
        const uint player = 0x7ffffffe;
        const uint otherPlayer = 0x7ffffffd;
        try
        {
            QuickCast.IsEnabled(player, QuickCastTargetType.Self).Should().BeFalse();
            QuickCast.IsEnabled(player, QuickCastTargetType.CurrentEnemy).Should().BeFalse();
            QuickCast.SetEnabled(player, QuickCastTargetType.Self, true);
            QuickCast.IsEnabled(player, QuickCastTargetType.Self).Should().BeTrue();
            QuickCast.IsEnabled(player, QuickCastTargetType.CurrentEnemy).Should().BeFalse();
            QuickCast.IsEnabled(otherPlayer, QuickCastTargetType.Self).Should().BeFalse();
            QuickCast.SetEnabled(player, QuickCastTargetType.CurrentEnemy, true);
            QuickCast.SetEnabled(player, QuickCastTargetType.Self, false);
            QuickCast.IsEnabled(player, QuickCastTargetType.CurrentEnemy).Should().BeTrue();
            QuickCast.IsEnabled(player, QuickCastTargetType.Self).Should().BeFalse();
            QuickCast.ResetSession(player);
            QuickCast.IsEnabled(player, QuickCastTargetType.CurrentEnemy).Should().BeFalse();
            QuickCast.IsEnabled(player, QuickCastTargetType.Self).Should().BeFalse();
        }
        finally { QuickCast.ResetSession(player); }
    }

    [TestCase(EffectTypeScript.Paralyze)]
    [TestCase(EffectTypeScript.CutsceneParalyze)]
    [TestCase(EffectTypeScript.Dazed)]
    [TestCase(EffectTypeScript.Stunned)]
    [TestCase(EffectTypeScript.Sleep)]
    [TestCase(EffectTypeScript.Petrify)]
    [TestCase(EffectTypeScript.Knockdown)]
    [TestCase(EffectTypeScript.Confused)]
    [TestCase(EffectTypeScript.Frightened)]
    [TestCase(EffectTypeScript.Dominated)]
    [TestCase(EffectTypeScript.CutsceneDominated)]
    [TestCase(EffectTypeScript.Turned)]
    public void NativeActionControl_BlocksQuickCasting(EffectTypeScript effectType) =>
        QuickCastPolicy.BlocksCasting(effectType).Should().BeTrue();

    [TestCase(EffectTypeScript.CutsceneImmobilize)]
    [TestCase(EffectTypeScript.Entangle)]
    [TestCase(EffectTypeScript.MovementSpeedDecrease)]
    [TestCase(EffectTypeScript.Blindness)]
    public void EffectsThatAllowStationaryAbilities_DoNotBlockQuickCasting(EffectTypeScript effectType) =>
        QuickCastPolicy.BlocksCasting(effectType).Should().BeFalse();

    [Test]
    public void SelfCast_RequiresExplicitDefinitionOptIn()
    {
        var builder = new AbilityBuilder().Create(FeatType.MedKit1, PerkType.MedKit)
            .IsCastedAbility().IsSingleTargetAbility().RequiresTarget();
        QuickCastPolicy.IsEligible(builder.Build()[FeatType.MedKit1], QuickCastTargetType.Self).Should().BeFalse();
        builder.AllowsSelfQuickCast();
        QuickCastPolicy.IsEligible(builder.Build()[FeatType.MedKit1], QuickCastTargetType.Self).Should().BeTrue();
        QuickCastPolicy.IsEligible(builder.Build()[FeatType.MedKit1], QuickCastTargetType.CurrentEnemy).Should().BeFalse();
    }

    [Test]
    public void ActualDefinitions_AllowSelfSupportAndHostileCasts_ExcludeRevivalAndAimedAreas()
    {
        var support = new MedKitAbilityDefinition().BuildAbilities();
        support.Values.Should().OnlyContain(ability => QuickCastPolicy.IsEligible(ability, QuickCastTargetType.Self));
        new BenevolenceAbilityDefinition().BuildAbilities().Values.Should()
            .OnlyContain(ability => QuickCastPolicy.IsEligible(ability, QuickCastTargetType.Self));
        new ResuscitationAbilityDefinition().BuildAbilities().Values.Should()
            .OnlyContain(ability => !QuickCastPolicy.IsEligible(ability, QuickCastTargetType.Self));
        new ForceInterceptAbilityDefinition().BuildAbilities().Values.Should()
            .OnlyContain(ability => !QuickCastPolicy.IsEligible(ability, QuickCastTargetType.Self));
        new ForceDrainAbilityDefinition().BuildAbilities().Values.Should()
            .OnlyContain(ability => QuickCastPolicy.IsEligible(ability, QuickCastTargetType.CurrentEnemy));
        new ForcePushAbilityDefinition().BuildAbilities().Values.Should()
            .OnlyContain(ability => !QuickCastPolicy.IsEligible(ability, QuickCastTargetType.CurrentEnemy));
        new ForceLightningAbilityDefinition().BuildAbilities().Values.Should()
            .OnlyContain(ability => !QuickCastPolicy.IsEligible(ability, QuickCastTargetType.CurrentEnemy));
    }

    [Test]
    public void QuickCast_DoesNotOfferQueuedWeaponsSelfAreasOrMissingTargets()
    {
        var ability = new AbilityDetail
        {
            ActivationType = AbilityActivationType.Casted, IsSingleTargetAbility = true,
            RequiresTarget = true, IsHostileAbility = true
        };
        QuickCastPolicy.IsEligible(ability, QuickCastTargetType.CurrentEnemy).Should().BeTrue();
        ability.ActivationType = AbilityActivationType.Weapon;
        QuickCastPolicy.IsEligible(ability, QuickCastTargetType.CurrentEnemy).Should().BeFalse();
        ability.ActivationType = AbilityActivationType.Casted;
        ability.IsAreaAbility = true;
        QuickCastPolicy.IsEligible(ability, QuickCastTargetType.CurrentEnemy).Should().BeFalse();
        ability.IsAreaAbility = false;
        ability.RequiresTarget = false;
        QuickCastPolicy.IsEligible(ability, QuickCastTargetType.CurrentEnemy).Should().BeFalse();
        ability.RequiresTarget = true;
        ability.Targeting = new AbilityTargetingDetail(Spell.Invalid, AbilityTargetingShapeType.Cone, 5f, 2f, AbilityTargetingFlags.HarmsEnemies, null);
        QuickCastPolicy.IsEligible(ability, QuickCastTargetType.CurrentEnemy).Should().BeFalse();
    }

    [Test]
    public void AbilityLookup_RequiresOwnedEligibleExactNameOrId()
    {
        var abilities = new MedKitAbilityDefinition().BuildAbilities();
        bool KnowsFirstRank(FeatType feat) => feat == FeatType.MedKit1;
        QuickCastPolicy.TryFindAbility(abilities, " med kit i ", QuickCastTargetType.Self, KnowsFirstRank, out var feat).Should().BeTrue();
        feat.Should().Be(FeatType.MedKit1);
        QuickCastPolicy.TryFindAbility(abilities, ((int)FeatType.MedKit1).ToString(), QuickCastTargetType.Self, KnowsFirstRank, out feat).Should().BeTrue();
        feat.Should().Be(FeatType.MedKit1);
        QuickCastPolicy.TryFindAbility(abilities, nameof(FeatType.MedKit1), QuickCastTargetType.Self, KnowsFirstRank, out _).Should().BeTrue();
        QuickCastPolicy.TryFindAbility(abilities, "Med Kit II", QuickCastTargetType.Self, KnowsFirstRank, out _).Should().BeFalse();
        QuickCastPolicy.TryFindAbility(abilities, ((int)FeatType.MedKit2).ToString(), QuickCastTargetType.Self, KnowsFirstRank, out _).Should().BeFalse();
        QuickCastPolicy.TryFindAbility(abilities, "Med Kit I", QuickCastTargetType.CurrentEnemy, KnowsFirstRank, out _).Should().BeFalse();
        QuickCastPolicy.TryFindAbility(abilities, "Med Kit", QuickCastTargetType.Self, KnowsFirstRank, out _).Should().BeFalse();
        QuickCastPolicy.TryFindAbility(abilities, "2147483647", QuickCastTargetType.Self, _ => true, out _).Should().BeFalse();
    }

    [Test]
    public void AmbiguousNamesFailClosed()
    {
        var abilities = new MedKitAbilityDefinition().BuildAbilities();
        abilities[FeatType.MedKit2].Name = abilities[FeatType.MedKit1].Name;
        QuickCastPolicy.TryFindAbility(abilities, "Med Kit I", QuickCastTargetType.Self, _ => true, out _).Should().BeFalse();
    }

}
