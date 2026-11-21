using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CityStoreService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Who may shop at a city's stores, which limited items a store stocks at a level, and how a limited item is found when
/// it is bought.
/// </summary>
public class CityStoreRulesTests
{
    #region Who may shop

    [Test]
    public void AnyoneMayShop_CitizenOrNot_WhenTheCityHasNoEmbargo()
    {
        CityStoreRules.CanShop(string.Empty, "city-a").Should().BeTrue();
        CityStoreRules.CanShop(null, "city-a").Should().BeTrue();
        CityStoreRules.CanShop(string.Empty, string.Empty).Should().BeTrue("a visitor with no city of their own shops too");
    }

    [Test]
    public void ACitizenOfTheCityUnderEmbargo_CannotShop()
    {
        CityStoreRules.CanShop("city-b", "city-b").Should().BeFalse();
    }

    [Test]
    public void ACitizenOfAnyOtherCity_StillShopsWhileOneCityIsUnderEmbargo()
    {
        CityStoreRules.CanShop("city-b", "city-a").Should().BeTrue();
        CityStoreRules.CanShop("city-b", string.Empty).Should().BeTrue("a visitor with no city is not embargoed");
    }

    [Test]
    public void TheHostCitysOwnCitizens_ShopAtTheirOwnStores()
    {
        // An Embargo names another city, so it never matches the host's own citizens.
        CityStoreRules.CanShop("city-b", "host").Should().BeTrue();
    }

    #endregion

    #region Limited items

    private static readonly CityStoreDetail Detail = new(
        CityStoreKind.Cantina,
        "Test",
        new[] { (1, "cs_test_1") },
        new[]
        {
            new CityStoreLimitedItem("house_drink", 1, 6),
            new CityStoreLimitedItem("house_banner", 3, 2)
        });

    [Test]
    public void ALimitedItem_IsFoundOnlyOnceTheStoreReachesItsLevel()
    {
        CityStoreRules.FindLimitedItem(Detail, 1, "house_drink").WeeklyLimit.Should().Be(6);
        CityStoreRules.FindLimitedItem(Detail, 2, "house_banner").Should().BeNull();
        CityStoreRules.FindLimitedItem(Detail, 3, "house_banner").WeeklyLimit.Should().Be(2);
    }

    [Test]
    public void AnItemWithNoAllowance_IsNotALimitedItem()
    {
        CityStoreRules.FindLimitedItem(Detail, 5, "bread").Should().BeNull();
        CityStoreRules.FindLimitedItem(null, 5, "house_drink").Should().BeNull();
    }

    [Test]
    public void TheLimitedResrefs_ListEachItemOnce()
    {
        CityStoreRules.GetLimitedResrefs(Detail).Should().BeEquivalentTo("house_drink", "house_banner");
        CityStoreRules.GetLimitedResrefs(null).Should().BeEmpty();
    }

    [Test]
    public void ABlueprintLookup_OfAMissingStore_IsEmpty()
    {
        CityStoreRules.GetBlueprint(null, 3).Should().BeEmpty();
    }

    #endregion
}
