using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class DoubleWeaponRecalibrationTests
{
    [TestCase("h_twinblade_5", 0, 34, 30)]
    [TestCase("trn_saberstaff_3", 0, 20, 17)]
    [TestCase("chi_twinblade", 0, 37, 32)]
    [TestCase("bloodprice_edge", 0, 49, 49)]
    [TestCase("ss_custom", 2, 19, 17)]
    [TestCase("ss_custom", 3, 23, 21)]
    [TestCase("ss_custom", 4, 27, 25)]
    [TestCase("ss_custom", 5, 33, 29)]
    [TestCase("ss_custom", 6, 37, 32)]
    [TestCase("ss_custom", 7, 37, 35)]
    [TestCase("trn_saberstaff_1", 6, 37, 32)]
    [TestCase("trn_saberstaff_3", 3, 20, 17)]
    [TestCase("trn_saberstaff_3", 6, 38, 32)]
    [TestCase("chi_twinelec", 6, 37, 32)]
    [TestCase("asc_trnsabstaff", 6, 31, 27)]
    [TestCase("bloodprice_edge", 6, 49, 49)]
    [TestCase("custom", 6, 37, 32)]
    [TestCase("custom", 5, 25, 21)]
    [TestCase("custom", 0, 7, 5)]
    [TestCase("custom", 0, int.MinValue, 1)]
    [TestCase("custom", 6, int.MaxValue, int.MaxValue - 5)]
    public void ExistingDamageBonusesSurviveBaseRatingRebalance(string resref, int tier, int damage, int expected)
    {
        var method = typeof(SaberRecalibration).Assembly
            .GetType("SWLOR.Game.Server.Feature.MigrationDefinition.DoubleWeaponRecalibration")!
            .GetMethod("CalculateDamage", BindingFlags.Public | BindingFlags.Static)!;
        method.Invoke(null, new object[] { resref, tier, damage }).Should().Be(expected);
    }

    [TestCase(BaseItem.TwoBladedSword, true)]
    [TestCase(BaseItem.DoubleAxe, true)]
    [TestCase(BaseItem.Saberstaff, true)]
    [TestCase(BaseItem.TwinElectroBlade, true)]
    [TestCase(BaseItem.Lightsaber, false)]
    [TestCase(BaseItem.QuarterStaff, false)]
    [TestCase(BaseItem.Pistol, false)]
    public void DoubleWeaponClassificationMatchesNativeTwoEndFamilies(BaseItem baseItem, bool expected)
    {
        Item.IsDoubleWeaponType(baseItem).Should().Be(expected);
    }
}
