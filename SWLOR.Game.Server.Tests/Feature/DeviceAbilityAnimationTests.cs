using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Devices;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class DeviceAbilityAnimationTests
{
    [Test]
    public void HostileDeviceAbilities_DeclareAnimation()
    {
        var missingAnimations = BuildDeviceAbilities()
            .Where(pair => pair.Value.IsHostileAbility)
            .Where(pair => pair.Value.ImpactAction != null)
            .Where(pair => pair.Value.AnimationType == Animation.Invalid)
            .Where(pair => pair.Value.ImpactAnimationType == Animation.Invalid)
            .Select(pair => $"{pair.Key}: {pair.Value.Name}")
            .ToList();

        missingAnimations.Should().BeEmpty();
    }

    private static Dictionary<FeatType, AbilityDetail> BuildDeviceAbilities()
    {
        var definitionType = typeof(IAbilityListDefinition);
        var deviceNamespace = typeof(OverloadBarrageAbilityDefinition).Namespace;

        var definitions = typeof(OverloadBarrageAbilityDefinition)
            .Assembly
            .GetTypes()
            .Where(type =>
                type.IsClass &&
                !type.IsAbstract &&
                type.Namespace == deviceNamespace &&
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

    private static void AssertImpactAnimation(
        IReadOnlyDictionary<FeatType, AbilityDetail> abilities,
        Animation expectedAnimation,
        params FeatType[] feats)
    {
        foreach (var feat in feats)
        {
            abilities[feat].ImpactAnimationType.Should().Be(expectedAnimation, $"{feat} should use the expected impact animation");
        }
    }

    private static void AssertActivationAnimation(
        IReadOnlyDictionary<FeatType, AbilityDetail> abilities,
        Animation expectedAnimation,
        params FeatType[] feats)
    {
        foreach (var feat in feats)
        {
            abilities[feat].AnimationType.Should().Be(expectedAnimation, $"{feat} should use the expected activation animation");
        }
    }
}
