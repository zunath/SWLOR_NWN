using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Pistol;
using SWLOR.Game.Server.Feature.AbilityDefinition.Rifle;
using SWLOR.Game.Server.Feature.AbilityDefinition.Throwing;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class RangedAbilityImpactAnimationTests
{
    [Test]
    public void HostileRangedAbilities_DeclareImpactAnimation()
    {
        var missingAnimations = BuildRangedAbilities()
            .Where(pair => IsRangedSkill(pair.Value.SkillType))
            .Where(pair => pair.Value.IsHostileAbility)
            .Where(pair => pair.Value.ImpactAction != null)
            .Where(pair => pair.Value.ImpactAnimationType == Animation.Invalid)
            .Where(pair => !pair.Value.SuppressesImpactAnimation)
            .Select(pair => $"{pair.Key}: {pair.Value.Name}")
            .ToList();

        missingAnimations.Should().BeEmpty();
    }

    [Test]
    public void RangedAbilities_CanChooseSpecificImpactAnimations()
    {
        var quickDraw = new QuickDrawAbilityDefinition().BuildAbilities()[FeatType.QuickDraw1];
        var doubleShot = new DoubleShotAbilityDefinition().BuildAbilities()[FeatType.DoubleShot1];
        var aimedShot = new AimedShotAbilityDefinition().BuildAbilities()[FeatType.AimedShot1];
        var piercingRound = new PiercingRoundAbilityDefinition().BuildAbilities()[FeatType.PiercingRound1];
        var explosiveToss = new ExplosiveTossAbilityDefinition().BuildAbilities()[FeatType.ExplosiveToss1];

        quickDraw.ImpactAnimationType.Should().Be(Animation.QuickDraw);
        doubleShot.ImpactAnimationType.Should().Be(Animation.DoubleShot);
        aimedShot.ImpactAnimationType.Should().Be(Animation.PointPistol);
        piercingRound.ImpactAnimationType.Should().Be(Animation.Invalid);
        piercingRound.SuppressesImpactAnimation.Should().BeTrue();
        explosiveToss.ImpactAnimationType.Should().Be(Animation.ThrowGrenade);
    }

    private static Dictionary<FeatType, AbilityDetail> BuildRangedAbilities()
    {
        var rangedNamespaces = new[]
        {
            typeof(QuickDrawAbilityDefinition).Namespace,
            typeof(AimedShotAbilityDefinition).Namespace,
            typeof(ExplosiveTossAbilityDefinition).Namespace
        };
        var definitionType = typeof(IAbilityListDefinition);

        var definitions = typeof(QuickDrawAbilityDefinition)
            .Assembly
            .GetTypes()
            .Where(type =>
                type.IsClass &&
                !type.IsAbstract &&
                rangedNamespaces.Contains(type.Namespace) &&
                definitionType.IsAssignableFrom(type))
            .Select(type => (IAbilityListDefinition)Activator.CreateInstance(type)!)
            .ToList();

        var abilities = new Dictionary<FeatType, AbilityDetail>();
        foreach (var definition in definitions)
        {
            foreach (var (feat, ability) in definition.BuildAbilities())
            {
                abilities.Add(feat, ability);
            }
        }

        return abilities;
    }

    private static bool IsRangedSkill(SkillType skillType)
    {
        return skillType is SkillType.Pistol or SkillType.Rifle or SkillType.Throwing;
    }
}
