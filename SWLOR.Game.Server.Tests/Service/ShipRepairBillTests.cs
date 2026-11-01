using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.ShipDefinition;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

public class ShipRepairBillTests
{
    // 10 shield and 30 hull missing: 10 x 50 + 30 x 100 = 3500 before size and discounts.
    private const int ShieldMissing = 10;
    private const int HullMissing = 30;

    [TestCase(ShipSize.Small, 1.0, 3500)]
    [TestCase(ShipSize.Medium, 1.5, 5250)]
    [TestCase(ShipSize.Large, 2.5, 8750)]
    [TestCase(ShipSize.Capital, 4.0, 14000)]
    public void Bill_ScalesWithHullSize(ShipSize size, double multiplier, int expected)
    {
        var hull = new ShipHullEntry { Size = size, RepairMultiplier = multiplier };

        var bill = ShipRepairBill.Calculate(ShieldMissing, HullMissing, ShipRepairBill.GetSizeMultiplier(hull, false), 0, 10);

        bill.Should().Be(expected);
    }

    [Test]
    public void Bill_KeepsStarportAndSocialDiscounts()
    {
        // Starport level 3 is 15% and Social 20 is 20%: 35% off 3500 is 2275 before the size multiplier.
        ShipRepairBill.GetDiscount(3, 20).Should().BeApproximately(0.35, 1e-9);
        ShipRepairBill.Calculate(ShieldMissing, HullMissing, 1.0, 3, 20).Should().Be(2275);
        ShipRepairBill.Calculate(ShieldMissing, HullMissing, 2.5, 3, 20).Should().Be(5688);
        ShipRepairBill.Calculate(ShieldMissing, HullMissing, 4.0, 3, 20).Should().Be(9100);
    }

    [Test]
    public void Discount_CapsSocialAtTwentyAndTheTotalAtNinety()
    {
        ShipRepairBill.GetDiscount(0, 40).Should().BeApproximately(0.20, 1e-9);
        ShipRepairBill.GetDiscount(0, 5).Should().Be(0, "a Social score under 10 gives no discount");
        ShipRepairBill.GetDiscount(20, 40).Should().BeApproximately(0.90, 1e-9);
    }

    [Test]
    public void Bill_OfAFullyRepairedShip_IsZero()
    {
        ShipRepairBill.Calculate(0, 0, 4.0, 0, 10).Should().Be(0);
        ShipRepairBill.Calculate(-5, -5, 4.0, 0, 10).Should().Be(0);
    }

    [Test]
    public void LegacyShips_KeepTheirOldMultiplier()
    {
        ShipRepairBill.GetSizeMultiplier(null, true).Should().Be(5.0);
        ShipRepairBill.GetSizeMultiplier(null, false).Should().Be(1.0);
    }

    [Test]
    public void EveryRevampHull_UsesItsSizeMultiplierFromTheStarshipsTab()
    {
        var expected = new Dictionary<ShipSize, double>
        {
            [ShipSize.Small] = 1.0,
            [ShipSize.Medium] = 1.5,
            [ShipSize.Large] = 2.5,
            [ShipSize.Capital] = 4.0,
        };

        foreach (var hull in RevampShipCatalog.Hulls)
        {
            ShipRepairBill.GetSizeMultiplier(hull, false).Should().Be(expected[hull.Size], hull.Name);
        }
    }
}
