using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Service;

public class TwinPistolPresentationTests
{
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

    [Test]
    public void VisualScaleChangesOnlyTheAuthoredOffset()
    {
        var origin = new Vector3(20f, 30f, 2f);
        var normal = PairedRangedPresentation.ResolveMuzzle(origin, 45f) - origin;
        var large = PairedRangedPresentation.ResolveMuzzle(origin, 45f, 2f) - origin;
        Vector3.Distance(large, normal * 2f).Should().BeLessThan(.00001f);
        PairedRangedPresentation.FlightSeconds(0f).Should().BeGreaterThan(0f);
        PairedRangedPresentation.FlightSeconds(25f).Should().Be(.5f);
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
