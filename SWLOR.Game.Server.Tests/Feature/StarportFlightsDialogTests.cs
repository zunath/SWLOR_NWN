using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.ShuttleService;

namespace SWLOR.Game.Server.Tests.Feature;

public class StarportFlightsDialogTests
{
    [OneTimeSetUp]
    public void CachePlanets()
    {
        typeof(Planet).GetMethod("CachePlanets", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, null);
    }

    [TestCase(PlanetType.Viscara)]
    [TestCase(PlanetType.Tatooine)]
    [TestCase(PlanetType.Dantooine)]
    public void UnboardedTicketOffersRefundAtOriginalDestinationAndOtherStarports(PlanetType terminalPlanet)
    {
        var ride = new ShuttleRide("passenger")
        {
            Status = ShuttleRideStatus.Ticketed,
            Origin = PlanetType.Viscara,
            Destination = PlanetType.Tatooine,
            FarePaid = 500,
            TaxPaid = 50
        };
        var page = new ConversationMenuPage(_ => { });

        typeof(StarportFlightsDialog).GetMethod("BuildTicketedPage", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(new StarportFlightsDialog(), new object[] { page, 1u, terminalPlanet, ride });

        page.Responses.Should().ContainSingle();
        page.Responses[0].Text.Should().Be("Refund Ticket (500 cr)", "only the original fare is refundable");
        page.Responses[0].IsActive.Should().BeTrue();
        page.Header.Should().Contain("Ticketed Flight: " + Planet.GetPlanetByType(ride.Destination).Name);
        page.Header.Should().NotContain("Refunds are only available");
        if (terminalPlanet != ride.Origin)
            page.Header.Should().Contain("This ticket departs from " + Planet.GetPlanetByType(ride.Origin).Name);
    }
}
