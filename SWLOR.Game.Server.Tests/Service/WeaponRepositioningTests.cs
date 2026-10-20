using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Tests.Service;

public class WeaponRepositioningTests
{
    [TestCase(2f, true)]
    [TestCase(6f, true)]
    [TestCase(1.9f, false)]
    [TestCase(6.1f, false)]
    public void GroundTargetedTravel_UsesTheAuthoredHorizontalRange(float distance, bool allowed) =>
        string.IsNullOrEmpty(WeaponRepositioning.ValidateGeometry(new Vector3(10, 10, 0),
            new Vector3(10 + distance, 10, 0), 2, 6)).Should().Be(allowed);

    [Test]
    public void MalformedCoordinates_AndNegativeMapCoordinatesAreRejectedBeforeNativeQueries()
    {
        foreach (var point in new[] { new Vector3(float.NaN, 10, 0), new Vector3(float.PositiveInfinity, 10, 0),
                     new Vector3(-1, 10, 0), new Vector3(10, -1, 0) })
            WeaponRepositioning.ValidateGeometry(new Vector3(10, 10, 0), point, 2, 6).Should().NotBeEmpty();
    }

    [Test]
    public void UnsupportedRanges_CannotExpandTheMovementBudget()
    {
        WeaponRepositioning.ValidateGeometry(new Vector3(10, 10, 0), new Vector3(15, 10, 0), 2, 10).Should().NotBeEmpty();
        WeaponRepositioning.ValidateGeometry(new Vector3(10, 10, 0), new Vector3(15, 10, 0), -1, 6).Should().NotBeEmpty();
    }

    [Test]
    public void OccupiedCorridor_BlocksCrossingAndArrivalButAllowsRetreatFromExistingContact()
    {
        var origin = new Vector3(10, 10, 0);
        var destination = new Vector3(14, 10, 0);
        WeaponRepositioning.IntersectsOccupiedCircle(origin, destination, new Vector3(12, 10, 0), 1).Should().BeTrue();
        WeaponRepositioning.IntersectsOccupiedCircle(origin, destination, destination, 1).Should().BeTrue();
        WeaponRepositioning.IntersectsOccupiedCircle(origin, destination, new Vector3(12, 12, 0), 1).Should().BeFalse();
        WeaponRepositioning.IntersectsOccupiedCircle(origin, destination, new Vector3(9.5f, 10, 0), 1).Should().BeFalse();
        WeaponRepositioning.IntersectsOccupiedCircle(origin, destination, new Vector3(10.5f, 10, 0), 1).Should().BeTrue();
    }
}
