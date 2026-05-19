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

    [Test]
    public void HostileDeviceAbilities_UseDeviceAppropriateAnimations()
    {
        var abilities = BuildDeviceAbilities();

        AssertImpactAnimation(
            abilities,
            Animation.ThrowGrenade,
            FeatType.AdhesiveGrenade1,
            FeatType.AdhesiveGrenade2,
            FeatType.ClusterGrenade1,
            FeatType.ConcussionGrenade1,
            FeatType.ConcussionGrenade2,
            FeatType.ConcussionGrenade3,
            FeatType.FlashGrenade1,
            FeatType.FlashGrenade2,
            FeatType.FragGrenade1,
            FeatType.FragGrenade2,
            FeatType.FragGrenade3,
            FeatType.IonGrenade1,
            FeatType.IonGrenade2,
            FeatType.RemoteCharge1,
            FeatType.RemoteCharge2,
            FeatType.RemoteCharge3,
            FeatType.ThermalDetonator1);

        AssertImpactAnimation(
            abilities,
            Animation.CastOutAnimation,
            FeatType.CryoSprayer1,
            FeatType.CryoSprayer2,
            FeatType.Flamethrower1,
            FeatType.Flamethrower2,
            FeatType.Flamethrower3,
            FeatType.OverloadBarrage1,
            FeatType.RailDart1,
            FeatType.RailDart2,
            FeatType.SonicBurst1,
            FeatType.SonicBurst2,
            FeatType.SonicBurst3,
            FeatType.WeaponJam1,
            FeatType.WeaponJam2,
            FeatType.WristRocket1,
            FeatType.WristRocket2,
            FeatType.WristRocket3);

        AssertActivationAnimation(
            abilities,
            Animation.CastOutAnimation,
            FeatType.BlasterBeacon1,
            FeatType.BlasterBeacon2,
            FeatType.BlasterBeacon3,
            FeatType.IncendiaryField1,
            FeatType.IncendiaryField2,
            FeatType.IncendiaryField3,
            FeatType.KillzoneBeacon1,
            FeatType.ShockBeacon1,
            FeatType.ShockBeacon2);
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
