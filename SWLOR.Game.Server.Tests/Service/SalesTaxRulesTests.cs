using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CityStoreService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The city Sales tax: a whole percent from 0 to 10, added to every price in the city's stores, the tax part rounded
/// down and credited to the treasury, with the key added to old cities by the release migration.
/// </summary>
public class SalesTaxRulesTests
{
    #region The rate

    [Test]
    public void TheTaxKey_IsTheReservedSalesValue()
    {
        ((int)PropertyTaxType.Sales).Should().Be(5);
    }

    [TestCase(-3, 0)]
    [TestCase(0, 0)]
    [TestCase(1, 1)]
    [TestCase(7, 7)]
    [TestCase(10, 10)]
    [TestCase(11, 10)]
    [TestCase(900, 10)]
    public void TheRate_IsClampedFromZeroToTen(int entered, int expected)
    {
        SalesTaxRules.ClampPercent(entered).Should().Be(expected);
    }

    [Test]
    public void ACityWithNoSalesKey_ChargesNothing()
    {
        CityStores.GetSalesTax(new WorldProperty()).Should().Be(0);
        CityStores.GetSalesTax(null).Should().Be(0);
    }

    [Test]
    public void ACityWithASalesKey_ChargesItsClampedRate()
    {
        var city = new WorldProperty();
        city.Taxes[PropertyTaxType.Sales] = 6;
        CityStores.GetSalesTax(city).Should().Be(6);

        city.Taxes[PropertyTaxType.Sales] = 40;
        CityStores.GetSalesTax(city).Should().Be(10, "a stored rate past the cap still charges only the cap");
    }

    #endregion

    #region The tax part of a price

    // The price already holds the tax: a store at 100% markup opened with a bonus of t sells at 100 + t percent.
    [TestCase(105, 5, 5)]
    [TestCase(110, 10, 10)]
    [TestCase(1050, 5, 50)]
    [TestCase(107, 7, 7)]
    [TestCase(1000, 7, 65)]
    [TestCase(7, 5, 0)]
    [TestCase(21, 5, 1)]
    [TestCase(1, 10, 0)]
    public void TheTaxPart_IsTheRateOverOneHundredPlusTheRate_RoundedDown(int price, int percent, int expected)
    {
        SalesTaxRules.GetTaxPortion(price, percent).Should().Be(expected);
    }

    [Test]
    public void NoRateOrNoPrice_PaysNoTax()
    {
        SalesTaxRules.GetTaxPortion(500, 0).Should().Be(0);
        SalesTaxRules.GetTaxPortion(0, 10).Should().Be(0);
        SalesTaxRules.GetTaxPortion(-50, 10).Should().Be(0);
    }

    [Test]
    public void ARateAboveTheCap_PaysOnlyTheCap()
    {
        SalesTaxRules.GetTaxPortion(110, 50).Should().Be(SalesTaxRules.GetTaxPortion(110, 10));
    }

    [Test]
    public void TheTaxPart_NeverExceedsThePriceItIsPartOf()
    {
        for (var percent = 0; percent <= 10; percent++)
        {
            for (var price = 0; price < 400; price++)
                SalesTaxRules.GetTaxPortion(price, percent).Should().BeLessThanOrEqualTo(price);
        }
    }

    [Test]
    public void TheTaxPart_MatchesTheWholeCreditsAddedToTheBasePrice()
    {
        // A base price of 20 cr at 5% shows 21 cr, and the one credit added is the tax.
        SalesTaxRules.GetTaxPortion(21, 5).Should().Be(1);
        // A base price of 200 cr at 10% shows 220 cr, and the 20 credits added are the tax.
        SalesTaxRules.GetTaxPortion(220, 10).Should().Be(20);
    }

    [Test]
    public void AStoreWithItsOwnMarkup_TakesTheRateOverItsMarkupPlusTheRate()
    {
        SalesTaxRules.GetTaxPortion(125, 5, 120).Should().Be(5);
    }

    [Test]
    public void TheOpeningLine_SaysWhatTheShopperPays()
    {
        SalesTaxRules.Describe(0).Should().Be("This city charges no sales tax.");
        SalesTaxRules.Describe(5).Should().Be("Prices include this city's 5% sales tax.");
        SalesTaxRules.Describe(30).Should().Be("Prices include this city's 10% sales tax.");
    }

    #endregion

    #region Manage City

    [TestCase(true, "4", 0, 4)]
    [TestCase(true, "15", 3, 10)]
    [TestCase(true, "-2", 3, 0)]
    [TestCase(true, "abc", 3, 3)]
    [TestCase(false, "9", 3, 3)]
    [TestCase(false, "abc", 3, 3)]
    public void ManageCity_SavesTheEnteredRateClamped_AndOnlyWithEditTaxes(bool canEditTaxes, string entered, int current, int expected)
    {
        ManageCityViewModel.ResolveSalesTax(canEditTaxes, entered, current).Should().Be(expected);
    }

    #endregion

    #region Migration

    [Test]
    public void TheMigration_AddsASalesKeyAtZeroToACityWithout()
    {
        var city = new WorldProperty();
        city.Taxes[PropertyTaxType.Citizenship] = 500;

        SalesTaxMigration.MigrateCity(city).Should().BeTrue();

        city.Taxes[PropertyTaxType.Sales].Should().Be(0);
        city.Taxes[PropertyTaxType.Citizenship].Should().Be(500, "the other taxes are not touched");
    }

    [Test]
    public void TheMigration_KeepsARateAlreadySet_AndRunsTwiceWithNoChange()
    {
        var city = new WorldProperty();
        city.Taxes[PropertyTaxType.Sales] = 8;

        SalesTaxMigration.MigrateCity(city).Should().BeFalse();
        city.Taxes[PropertyTaxType.Sales].Should().Be(8);

        var older = new WorldProperty();
        SalesTaxMigration.MigrateCity(older).Should().BeTrue();
        SalesTaxMigration.MigrateCity(older).Should().BeFalse();
    }

    [Test]
    public void TheReleaseMigration_RunsTheSalesTaxStep()
    {
        var source = File.ReadAllText(Path.Combine(
            Root, "SWLOR.Game.Server", "Feature", "MigrationDefinition", "ServerMigration", "_32_PlayerCityRevamp.cs"));

        source.Should().Contain("SalesTaxMigration.Run(Version);");
    }

    #endregion

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        }
    }
}
