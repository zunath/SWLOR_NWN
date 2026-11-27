using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.StationService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>Planetary councils: which council a city joins, who is summoned, and what the notice says.</summary>
public class PlanetaryCouncilRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    #region Which council

    [Test]
    public void APlanetCityJoinsItsOwnPlanetsCouncil()
    {
        PlanetaryCouncilRules.GetCouncilPlanet(false, false, TradeExchangeType.Invalid, PlanetType.Invalid, PlanetType.Korriban)
            .Should().Be(PlanetType.Korriban);
    }

    [Test]
    public void AnOrbitStationJoinsItsPlanetsCouncil()
    {
        PlanetaryCouncilRules.GetCouncilPlanet(true, false, TradeExchangeType.Tatooine, PlanetType.Tatooine, PlanetType.Invalid)
            .Should().Be(PlanetType.Tatooine);
    }

    [Test]
    public void AWildSpaceStationJoinsTheCouncilOfItsHomePortsPlanet()
    {
        PlanetaryCouncilRules.GetCouncilPlanet(true, true, TradeExchangeType.Dathomir, PlanetType.Dathomir, PlanetType.Invalid)
            .Should().Be(PlanetType.Dathomir);
        PlanetaryCouncilRules.GetCouncilPlanet(true, true, TradeExchangeType.Hutlar, PlanetType.Hutlar, PlanetType.Invalid)
            .Should().Be(PlanetType.Hutlar);
    }

    [Test]
    public void EveryExchangeMapsToThePlanetOfTheSameName()
    {
        foreach (var exchange in Enum.GetValues<TradeExchangeType>().Where(exchange => exchange != TradeExchangeType.Invalid))
        {
            var planet = PlanetaryCouncilRules.GetPlanetOfExchange(exchange);

            planet.Should().NotBe(PlanetType.Invalid, exchange.ToString());
            planet.ToString().Should().Be(exchange.ToString());
        }

        PlanetaryCouncilRules.GetPlanetOfExchange(TradeExchangeType.Invalid).Should().Be(PlanetType.Invalid);
    }

    [Test]
    public void OnlyVeshPulsarJoinsDathomir_AndEveryOtherWildSpaceSystemJoinsHutlar()
    {
        var wildSpace = StationTuning.Anchors.Where(anchor => anchor.IsWildSpace).ToList();

        wildSpace.Should().NotBeEmpty();
        foreach (var anchor in wildSpace)
        {
            var expected = anchor.SystemName.Contains("Vesh Pulsar") ? PlanetType.Dathomir : PlanetType.Hutlar;

            PlanetaryCouncilRules.GetCouncilPlanetOfAnchor(anchor).Should().Be(expected, anchor.Id);
            PlanetaryCouncilRules.GetPlanetOfExchange(StationRules.GetExchange(anchor.Id)).Should().Be(expected, anchor.Id);
        }
    }

    [Test]
    public void EveryOrbitAnchorJoinsTheCouncilOfItsOrbitsPlanet()
    {
        foreach (var anchor in StationTuning.Anchors.Where(anchor => !anchor.IsWildSpace))
        {
            PlanetaryCouncilRules.GetCouncilPlanetOfAnchor(anchor).Should().Be(anchor.HomePlanet, anchor.Id);
            PlanetaryCouncilRules.GetCouncilPlanetOfAnchor(anchor).Should().NotBe(PlanetType.Invalid, anchor.Id);
        }
    }

    [Test]
    public void AnUnknownAnchorJoinsNoCouncil()
    {
        PlanetaryCouncilRules.GetCouncilPlanetOfAnchor(null).Should().Be(PlanetType.Invalid);
    }

    #endregion

    #region Who is summoned

    [Test]
    public void TheMayorTheOfficersAndTheActingMayorAreSummonedOnce()
    {
        var city = new WorldProperty { OwnerPlayerId = "mayor", ActingMayorPlayerId = "acting", ActingMayorEndsUtc = Now.AddDays(5) };
        city.Officers["officer"] = new CityOfficer();
        city.Officers["acting"] = new CityOfficer();

        PlanetaryCouncilRules.GetRecipients(city, Now).Should().BeEquivalentTo(new[] { "mayor", "officer", "acting" });
    }

    [Test]
    public void AnActingMayorWhoseTermRanOutIsNotSummoned()
    {
        var city = new WorldProperty { OwnerPlayerId = "mayor", ActingMayorPlayerId = "acting", ActingMayorEndsUtc = Now.AddDays(-1) };

        PlanetaryCouncilRules.GetRecipients(city, Now).Should().BeEquivalentTo(new[] { "mayor" });
    }

    [Test]
    public void AfterAStepDown_TheInterimHolderIsSummonedAlongsideTheMayorOfRecord()
    {
        var city = new WorldProperty
        {
            OwnerPlayerId = "mayor",
            StepDown = new CityStepDown { InterimHolderPlayerId = "interim", Stage = CityStepDownStage.Accepted }
        };

        PlanetaryCouncilRules.GetRecipients(city, Now).Should().BeEquivalentTo(new[] { "mayor", "interim" });
    }

    [Test]
    public void APlayerIsARecipientOnlyOfTheirOwnCity()
    {
        var city = new WorldProperty { OwnerPlayerId = "mayor" };
        city.Officers["officer"] = new CityOfficer();

        PlanetaryCouncilRules.IsRecipient(city, "officer", Now).Should().BeTrue();
        PlanetaryCouncilRules.IsRecipient(city, "citizen", Now).Should().BeFalse();
        PlanetaryCouncilRules.IsRecipient(city, string.Empty, Now).Should().BeFalse();
    }

    #endregion

    #region The notice

    [Test]
    public void ASummonsReadsOnceAtLogin_ForTwoWeeks()
    {
        var summons = new CityCouncilSummons { Id = "s1", Planet = PlanetType.Korriban, Text = "Gather.", PostedUtc = Now };

        PlanetaryCouncilRules.IsUnread(summons, null, Now.AddDays(1)).Should().BeTrue();
        PlanetaryCouncilRules.IsUnread(summons, "older", Now.AddDays(13)).Should().BeTrue();
        PlanetaryCouncilRules.IsUnread(summons, "s1", Now.AddDays(1)).Should().BeFalse("they have read it");
        PlanetaryCouncilRules.IsUnread(summons, null, Now.AddDays(14)).Should().BeFalse("it is too old");
        PlanetaryCouncilRules.IsUnread(null, null, Now).Should().BeFalse();
        PlanetaryCouncilRules.IsUnread(new CityCouncilSummons(), null, Now).Should().BeFalse();
    }

    [Test]
    public void TheSummonsNamesThePlanetAndAddsTheDMsText()
    {
        PlanetaryCouncilRules.BuildSummonsText("Korriban", string.Empty).Should().Contain("Korriban").And.Contain("main settlement");
        PlanetaryCouncilRules.BuildSummonsText("Korriban", "The tombs stir.").Should().EndWith("The tombs stir.");
        PlanetaryCouncilRules.BuildSummonsText("Korriban", "x").Should().MatchRegex("^[\\x20-\\x7E]+$");
    }

    [Test]
    public void TheDMsTextIsCutToThreeHundredCharacters()
    {
        PlanetaryCouncilRules.MessageMaxLength.Should().Be(300);
        PlanetaryCouncilRules.NormalizeMessage(new string('b', 500)).Should().HaveLength(300);
        PlanetaryCouncilRules.NormalizeMessage("  hello\u0007 ").Should().Be("hello");
        PlanetaryCouncilRules.NormalizeMessage(null).Should().BeEmpty();
    }

    [Test]
    public void TheGazetteAndTheMessageSayTheSameThing()
    {
        PlanetaryCouncilRules.BuildGazetteText("Korriban", "Come.").Should().Be(PlanetaryCouncilRules.BuildSummonsText("Korriban", "Come."));
    }

    #endregion
}
