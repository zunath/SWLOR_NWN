using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CityAffairsService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>What the DM city window shows: active citizens, the planet filter, the list order, and each line.</summary>
public class DMCityRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    private static DMCitySummary City(
        string id,
        string name,
        PlanetType planet = PlanetType.Korriban,
        string planetName = "Korriban",
        CityStoryRequestStatus? request = null) => new()
    {
        CityId = id,
        CityName = name,
        Planet = planet,
        PlanetName = planetName,
        Level = 2,
        MayorName = "Aria Vale",
        CitizenCount = 9,
        ActiveCitizenCount = 4,
        StoryRequest = request.HasValue
            ? new CityStoryRequest { Text = "Ruins by the road.", PosterTitle = "Mayor", PostedUtc = Now, Status = request.Value }
            : null,
        StoryPosterName = request.HasValue ? "Aria Vale" : string.Empty
    };

    #region Active citizens

    [Test]
    public void ACitizenIsActiveWithinSevenDaysOfTheirLastLogin()
    {
        DMCityRules.ActiveWindowDays.Should().Be(7);

        var logins = new DateTime?[]
        {
            Now.AddDays(-1),
            Now.AddDays(-7),
            Now.AddDays(-7).AddSeconds(-1),
            Now.AddDays(-30),
            null
        };

        DMCityRules.CountActiveCitizens(logins, Now).Should().Be(2);
    }

    [Test]
    public void NoLoginsMeansNoActiveCitizens()
    {
        DMCityRules.CountActiveCitizens(Array.Empty<DateTime?>(), Now).Should().Be(0);
        DMCityRules.CountActiveCitizens(null, Now).Should().Be(0);
    }

    #endregion

    #region Planet filter and order

    [Test]
    public void NoPlanetSelectedListsEveryCity()
    {
        DMCityRules.MatchesPlanet(PlanetType.Invalid, PlanetType.Korriban).Should().BeTrue();
        DMCityRules.MatchesPlanet(PlanetType.Invalid, PlanetType.Invalid).Should().BeTrue();
    }

    [Test]
    public void APlanetFilterKeepsOnlyThatPlanetsCouncil()
    {
        DMCityRules.MatchesPlanet(PlanetType.Korriban, PlanetType.Korriban).Should().BeTrue();
        DMCityRules.MatchesPlanet(PlanetType.Korriban, PlanetType.Dathomir).Should().BeFalse();
    }

    [Test]
    public void CitiesWithAnOpenRequestComeFirst_ThenByPlanetAndName()
    {
        var cities = new[]
        {
            City("a", "Zed", PlanetType.Dathomir, "Dathomir"),
            City("b", "Alpha", PlanetType.Korriban, "Korriban"),
            City("c", "Beta", PlanetType.Korriban, "Korriban", CityStoryRequestStatus.Taken),
            City("d", "Omega", PlanetType.Tatooine, "Tatooine", CityStoryRequestStatus.Open),
            City("e", "Gamma", PlanetType.Korriban, "Korriban", CityStoryRequestStatus.Done)
        };

        DMCityRules.Sort(cities).Select(city => city.CityId).Should().Equal("d", "a", "b", "c", "e");
    }

    [Test]
    public void SortingNothingGivesNothing()
    {
        DMCityRules.Sort(null).Should().BeEmpty();
    }

    #endregion

    #region Lines

    [Test]
    public void TheListLineShowsTheActiveCitizensAndMarksAnOpenRequest()
    {
        DMCityRules.BuildListLabel(City("a", "Dawnreach")).Should().Be("Dawnreach (Korriban) - 4 of 9 active");
        DMCityRules.BuildListLabel(City("a", "Dawnreach", request: CityStoryRequestStatus.Open)).Should().EndWith("- REQUEST");
        DMCityRules.BuildListLabel(City("a", "Dawnreach", request: CityStoryRequestStatus.Taken)).Should().NotContain("REQUEST");
    }

    [Test]
    public void TheDetailsListThePeopleEdictsAndGazette()
    {
        var city = new DMCitySummary
        {
            CityName = "Dawnreach",
            PlanetName = "Korriban",
            Level = 3,
            MayorName = "Aria Vale",
            OfficerLines = new[] { "Bram (Marshal) [online]", "Cade" },
            CitizenCount = 11,
            ActiveCitizenCount = 6,
            EdictNames = new[] { "Open Gates", "Festival Season" },
            GazetteLines = new[] { "2026-11-02 Taxes changed." }
        };

        var details = DMCityRules.BuildDetails(city);

        details.Should().Contain("Dawnreach, Korriban, level 3");
        details.Should().Contain("Mayor: Aria Vale");
        details.Should().Contain("Officers: Bram (Marshal) [online]; Cade");
        details.Should().Contain("11, 6 logged in within 7 days");
        details.Should().Contain("Active Edicts: Open Gates, Festival Season");
        details.Should().Contain("2026-11-02 Taxes changed.");
    }

    [Test]
    public void TheDetailsSayWhenThereAreNoOfficersEdictsOrEntries()
    {
        var details = DMCityRules.BuildDetails(City("a", "Dawnreach"));

        details.Should().Contain("Officers: none").And.Contain("Active Edicts: none").And.Contain("none");
    }

    [Test]
    public void TheDetailsShowAnActingMayor_AndAMayorWhoSteppedDown()
    {
        var acting = City("a", "Dawnreach");
        var actingDetails = DMCityRules.BuildDetails(new DMCitySummary
        {
            CityName = acting.CityName, PlanetName = acting.PlanetName, MayorName = "Aria Vale", ActingMayorName = "Bram"
        });
        actingDetails.Should().Contain("Acting Mayor: Bram").And.Contain("Mayor: Aria Vale");

        var steppedDown = DMCityRules.BuildDetails(new DMCitySummary
        {
            CityName = "Dawnreach", PlanetName = "Korriban", MayorName = "Aria Vale", MayorSteppedDown = true, ActingMayorName = "Bram"
        });
        steppedDown.Should().Contain("Mayor of record: Aria Vale (stepped down").And.Contain("Interim holder: Bram");
    }

    [Test]
    public void AStationIsMarkedAsOne()
    {
        DMCityRules.BuildDetails(new DMCitySummary { CityName = "Vesh One", PlanetName = "Dathomir", IsStation = true })
            .Should().Contain("(station)");
    }

    [Test]
    public void TheRequestTextShowsItsStatusPosterAndWords()
    {
        var text = DMCityRules.BuildRequestText(City("a", "Dawnreach", request: CityStoryRequestStatus.Open));

        text.Should().Contain("Open - posted 2026-11-02 UTC by Aria Vale (Mayor)").And.Contain("Ruins by the road.");
        DMCityRules.BuildRequestText(City("a", "Dawnreach")).Should().Be("This city has no story request.");
    }

    [Test]
    public void ARequestWithoutAPosterNameShowsTheTitle()
    {
        var city = new DMCitySummary
        {
            StoryRequest = new CityStoryRequest { Text = "Hello.", PosterTitle = "Marshal", PostedUtc = Now, Status = CityStoryRequestStatus.Taken }
        };

        DMCityRules.BuildRequestText(city).Should().Contain("by Marshal").And.StartWith("Taken");
    }

    [Test]
    public void ThePersonLineShowsTheTitleAndWhetherTheyAreOnline()
    {
        DMCityRules.BuildPersonLine("Bram", "Marshal", true).Should().Be("Bram (Marshal) [online]");
        DMCityRules.BuildPersonLine("Bram", "", false).Should().Be("Bram");
    }

    [Test]
    public void AGazetteLineMarksSecretEntries()
    {
        var entry = new CityGazetteEntry { TimeUtc = Now, Text = "A bribe was taken.", OfficialTitle = "Officer", IsSecret = true };

        DMCityRules.BuildGazetteLine(entry).Should().Be("2026-11-02 [secret] A bribe was taken. (Officer)");
        DMCityRules.BuildGazetteLine(new CityGazetteEntry { TimeUtc = Now, Text = "Taxes changed." }).Should().Be("2026-11-02 Taxes changed.");
    }

    [Test]
    public void TheSummonsFeedbackReportsWhoWasReached()
    {
        DMCityRules.BuildSummonsFeedback("Korriban", 0, 0, 0).Should().Contain("No cities");
        DMCityRules.BuildSummonsFeedback("Korriban", 3, 9, 2).Should().Contain("3 cities").And.Contain("9 mayors and officers").And.Contain("2 online");
    }

    #endregion
}
