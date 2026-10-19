using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Feature.AbilityDefinition.TwinPistol;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Service;

public class TwinPistolPresentationTests
{
    [Test]
    public void CrossSkillRanksKeepANeutralFallbackAndReservePairedClipsForCompatibleEquipment()
    {
        var abilities = typeof(TwinPistolAbilityDefinitionBase).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(TwinPistolAbilityDefinitionBase).IsAssignableFrom(type))
            .SelectMany(type => ((IAbilityListDefinition)Activator.CreateInstance(type)!).BuildAbilities())
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        abilities.Should().HaveCount(22);
        AbilityAnimationBinding.Apply(abilities, ActiveAbilityAnimationCatalog.Entries
            .Where(entry => entry.Feats.Any(abilities.ContainsKey)));
        foreach (var ability in abilities.Values)
        {
            ability.AnimationEquipmentRequirement.Should().Be(AnimationEquipmentRequirement.TwinPistol);
            AbilityAnimationBinding.ActivationClip(ability, true, 0, equipmentCompatible: false).Should().BeNull();
            AbilityAnimationBinding.ImpactClip(ability, true, equipmentCompatible: false).Should().BeNull();
            if (ability.IsChanneled)
            {
                // The explicit channel owns its activation choreography; cross-trained
                // weapons use the neutral impact gesture for each fired volley instead.
                ability.HasGeneratedAnimationBinding.Should().BeFalse();
                AbilityAnimationBinding.ActivationType(ability, true, 0, equipmentCompatible: false).Should().Be(Animation.Invalid);
                ability.ImpactAnimationType.Should().Be(Animation.CastOutAnimation);
                AbilityAnimationBinding.ActivationClip(ability, true, 0).Should().Be(AuthoredAnimation.FocusedBurst);
            }
            else if (ability.UsesAuthoredImpactAnimation)
            {
                ability.ImpactAnimationType.Should().Be(Animation.CastOutAnimation);
                AbilityAnimationBinding.ImpactClip(ability, true).Should().NotBeNull();
            }
            else
            {
                AbilityAnimationBinding.ActivationType(ability, true, 0, equipmentCompatible: false).Should().Be(Animation.CastOutAnimation);
                AbilityAnimationBinding.ActivationClip(ability, true, 0).Should().NotBeNull();
            }
        }
    }

    [TestCase(0f)]
    [TestCase(90f)]
    [TestCase(180f)]
    [TestCase(270f)]
    public void CompanionLaunchRemainsLeftOfTheActorAtEveryCardinalFacing(float facing)
    {
        var radians = facing * MathF.PI / 180f;
        var forward = new Vector3(MathF.Cos(radians), MathF.Sin(radians), 0f);
        var left = new Vector3(-forward.Y, forward.X, 0f);
        var origin = new Vector3(20f, 30f, 2f);
        var delta = PairedRangedPresentation.ResolveMuzzle(origin, facing) - origin;
        Vector3.Dot(delta, left).Should().BeGreaterThan(.1f);
        Vector3.Dot(delta, forward).Should().BeGreaterThan(.5f);
        delta.Z.Should().BeGreaterThan(1f);
    }

    [TestCase(0f)]
    [TestCase(90f)]
    [TestCase(180f)]
    [TestCase(270f)]
    public void MainAndCompanionMuzzlesRemainDistinctAfterMovementAndTurning(float facing)
    {
        var radians = facing * MathF.PI / 180f;
        var forward = new Vector3(MathF.Cos(radians), MathF.Sin(radians), 0f);
        var right = new Vector3(forward.Y, -forward.X, 0f);
        var origin = new Vector3(35f, 45f, 3f);
        var main = PairedRangedPresentation.ResolveMuzzle(origin, facing, false);
        var companion = PairedRangedPresentation.ResolveMuzzle(origin, facing, true);

        Vector3.Dot(main - origin, right).Should().BeApproximately(.23f, .00001f);
        Vector3.Dot(companion - origin, right).Should().BeApproximately(-.23f, .00001f);
        Vector3.Distance(main, companion).Should().BeApproximately(.46f, .00001f);
        Vector3.Dot(main - origin, forward).Should().BeApproximately(.82f, .00001f);
        main.Z.Should().BeApproximately(origin.Z + 1.52f, .00001f);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void VisualScaleChangesOnlyTheAuthoredOffset(bool companion)
    {
        var origin = new Vector3(20f, 30f, 2f);
        var normal = PairedRangedPresentation.ResolveMuzzle(origin, 45f, companion) - origin;
        var large = PairedRangedPresentation.ResolveMuzzle(origin, 45f, companion, 2f) - origin;
        Vector3.Distance(large, normal * 2f).Should().BeLessThan(.00001f);
        PairedRangedPresentation.FlightSeconds(0f).Should().BeGreaterThan(0f);
        PairedRangedPresentation.FlightSeconds(25f).Should().Be(.5f);
    }

    [TestCase(true, 11u, true)]
    [TestCase(true, 12u, true)]
    [TestCase(true, 13u, false)]
    [TestCase(false, 11u, false)]
    [TestCase(false, 12u, false)]
    [TestCase(true, 0x7f000000u, false)]
    public void ActiveProjectilesRequireTheCurrentPairAndOneOfItsTwoWeapons(bool activePair, uint weapon, bool expected)
    {
        PairedRangedPresentation.ShouldPresentAbilityWeapon(activePair, weapon, 11u, 12u).Should().Be(expected);
    }

    [Test]
    public void SimultaneousBothHandRushAndOtherLegalLaunchesKeepIndependentAnchorsUntilArrival()
    {
        var leases = new long[PairedRangedPresentation.SourceAnchorCount];
        PairedRangedPresentation.MissAnchorCount.Should().Be(leases.Length);
        var slots = Enumerable.Range(0, 15)
            .Select(_ => PairedRangedPresentation.LeaseProjectileAnchor(leases, 1000, 25f)).ToArray();
        slots.Should().OnlyContain(slot => slot >= 0);
        slots.Distinct().Should().HaveCount(15, "each overlapping hand and target retains its own source");
        leases.Take(15).Should().OnlyContain(until => until == 1600);
        PairedRangedPresentation.LeaseProjectileAnchor(leases, 1000, 25f).Should().BeGreaterThanOrEqualTo(0);
        PairedRangedPresentation.LeaseProjectileAnchor(leases, 1000, 25f).Should().Be(-1,
            "cosmetic overload cannot reposition an in-flight source or grow the object pool");
        PairedRangedPresentation.LeaseProjectileAnchor(leases, 1599, 25f).Should().Be(-1);
        PairedRangedPresentation.LeaseProjectileAnchor(leases, 1600, 25f).Should().Be(0);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void MissesUseAnIndependentEndpointBesideTheIntendedTarget(bool companion)
    {
        var muzzle = PairedRangedPresentation.ResolveMuzzle(new Vector3(20f, 30f, 2f), 90f, companion);
        var target = new Vector3(22f, 40f, 2f);
        var endpoint = PairedRangedPresentation.ResolveMissDestination(muzzle, target);
        var hitPoint = target + Vector3.UnitZ;
        var horizontalDirection = new Vector3(hitPoint.X - muzzle.X, hitPoint.Y - muzzle.Y, 0);

        Vector3.Distance(endpoint, hitPoint).Should().BeApproximately(1.5f, .00001f);
        Vector3.Dot(endpoint - hitPoint, horizontalDirection).Should().BeApproximately(0f, .0001f);
        endpoint.Z.Should().Be(hitPoint.Z);
    }

    [Test]
    public void AVerticalMissStillAvoidsTheTargetWithoutInvalidCoordinates()
    {
        var target = new Vector3(20f, 30f, 2f);
        var endpoint = PairedRangedPresentation.ResolveMissDestination(target + Vector3.UnitZ * 2f, target);
        Vector3.Distance(endpoint, target + Vector3.UnitZ).Should().BeApproximately(1.5f, .00001f);
    }

    [Test]
    public void TheInertCompanionHasItsOwnGripAndNeverMatchesAShieldOrMeleePair()
    {
        var companion = AbilityAnimationEquipment.ResolveGrip(BaseItem.CompanionPistol, 1, 1, 0, 3);
        companion.Should().Be(AnimationWeaponGrip.CompanionPistol);
        AbilityAnimationEquipment.IsCompatible(AnimationEquipmentRequirement.TwinPistol,
            AnimationWeaponGrip.Pistol, companion).Should().BeTrue();
        AbilityAnimationEquipment.IsCompatible(AnimationEquipmentRequirement.WeaponAndShield,
            AnimationWeaponGrip.OneHanded, companion).Should().BeFalse();
        AbilityAnimationEquipment.IsCompatible(AnimationEquipmentRequirement.DualWield,
            AnimationWeaponGrip.OneHanded, companion).Should().BeFalse();
        AbilityAnimationEquipment.IsCompatible(AnimationEquipmentRequirement.TwinPistol,
            AnimationWeaponGrip.Pistol, AnimationWeaponGrip.Empty).Should().BeFalse();
    }
}
