using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Where a situation's treasury change lands now that the restricted treasury exists: gains cannot be withdrawn, costs
/// come out of restricted funds first, and the caller saves the city, so the change itself never touches the database.
/// </summary>
public class SituationTreasuryTests
{
    private static WorldProperty City(long treasury, long restricted = 0)
    {
        return new WorldProperty { Id = "city", PropertyType = PropertyType.City, Treasury = treasury, RestrictedTreasury = restricted };
    }

    [Test]
    public void AGain_GoesIntoTheRestrictedTreasuryAndLeavesTheWithdrawableOneAlone()
    {
        var city = City(1000);

        SituationTreasury.Apply(city, 4000);

        city.RestrictedTreasury.Should().Be(4000);
        city.Treasury.Should().Be(1000);
    }

    [Test]
    public void ACost_ComesOutOfRestrictedFundsFirst()
    {
        var city = City(4000, restricted: 3000);

        SituationTreasury.Apply(city, -5000);

        city.RestrictedTreasury.Should().Be(0);
        city.Treasury.Should().Be(2000);
    }

    [Test]
    public void ACostSmallerThanTheRestrictedBalance_LeavesTheTreasuryAlone()
    {
        var city = City(4000, restricted: 3000);

        SituationTreasury.Apply(city, -1200);

        city.RestrictedTreasury.Should().Be(1800);
        city.Treasury.Should().Be(4000);
    }

    [Test]
    public void NoChange_ChangesNothing()
    {
        var city = City(4000, restricted: 3000);

        SituationTreasury.Apply(city, 0);

        city.Treasury.Should().Be(4000);
        city.RestrictedTreasury.Should().Be(3000);
    }

    [Test]
    public void ASituationCanSpendBothBalances()
    {
        SituationTreasury.GetSpendable(City(4000, restricted: 3000)).Should().Be(7000);
    }

    [Test]
    public void AChoiceThatOnlyRestrictedFundsCanPayForIsAffordable()
    {
        var city = City(0, restricted: 15000);
        var spendable = SituationTreasury.GetSpendable(city);

        SituationRules.CanAfford(15000, spendable).Should().BeTrue();

        var change = SituationRules.ResolveTreasuryChange(-15000, spendable, 0);
        SituationTreasury.Apply(city, change);

        change.Should().Be(-15000);
        city.RestrictedTreasury.Should().Be(0);
        city.Treasury.Should().Be(0);
    }

    [Test]
    public void ACostIsLimitedToWhatBothBalancesHold()
    {
        var city = City(2000, restricted: 1000);

        var change = SituationRules.ResolveTreasuryChange(-15000, SituationTreasury.GetSpendable(city), 0);
        SituationTreasury.Apply(city, change);

        change.Should().Be(-3000);
        city.Treasury.Should().Be(0);
        city.RestrictedTreasury.Should().Be(0);
    }

    [Test]
    public void SituationsReachTheTreasuryOnlyThroughTheTreasuryService()
    {
        var treasury = File.ReadAllText(Path.Combine(RepositoryRoot, "SWLOR.Game.Server", "Service", "CityAffairsService", "SituationTreasury.cs"));
        var affairs = File.ReadAllText(Path.Combine(RepositoryRoot, "SWLOR.Game.Server", "Service", "CityAffairs.cs"));

        treasury.Should().Contain("CityTreasury.CreditRestricted(city, change, save: false);");
        treasury.Should().Contain("CityTreasury.Spend(city, -change, restrictedFirst: true, save: false);");
        treasury.Should().NotContain("city.Treasury +=");
        affairs.Should().NotContain("city.Treasury");
    }

    private static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;
            return directory!.FullName;
        }
    }
}
