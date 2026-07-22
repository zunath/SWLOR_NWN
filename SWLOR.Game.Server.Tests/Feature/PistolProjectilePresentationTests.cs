using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Native;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Game.Server.Tests.Feature;

public class PistolProjectilePresentationTests
{
    [TestCase(0, BaseItem.Sling, BaseItem.Pistol)]
    [TestCase(5, BaseItem.Sling, BaseItem.Pistol)]
    [TestCase(6, BaseItem.Sling, BaseItem.Sling)]
    [TestCase(7, BaseItem.Sling, BaseItem.Sling)]
    [TestCase(0, BaseItem.Pistol, BaseItem.Pistol)]
    public void ClientProjectileBaseItem_UsesStraightPistolPresentationOnlyForCanonicalWeaponShots(
        byte projectileType,
        BaseItem serverBaseItem,
        BaseItem expectedClientBaseItem)
    {
        var method = typeof(PistolProjectilePresentation).GetMethod(
            "GetClientBaseItemId",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        var result = (byte)method.Invoke(
            null,
            new object[] { projectileType, (byte)serverBaseItem })!;

        result.Should().Be((byte)expectedClientBaseItem);
    }

    [Test]
    public void NativeProjectileVector_MatchesTheEngineAbi()
    {
        var vectorType = typeof(PistolProjectilePresentation).GetNestedType(
            "NativeVector",
            BindingFlags.NonPublic)!;

        System.Runtime.InteropServices.Marshal.SizeOf(vectorType).Should().Be(12);
    }
}
