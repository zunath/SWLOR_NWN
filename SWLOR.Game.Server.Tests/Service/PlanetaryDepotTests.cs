using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.HoldingService;
using SWLOR.Game.Server.Service.IndustryService;
using SWLOR.Game.Server.Service.StationService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The Planetary Depot (Player City Revamp, "Planetary industry"): a citizen moves a holding between an NPC dock and their city's
/// Starport of level 2 or higher, once every 7 days. It changes where the output waits, never how much.
/// </summary>
public class PlanetaryDepotTests
{
    private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);
    private const string NpcDock = "korriban_landing";
    private const string CityDock = "6b3f6ac5-0000-4000-8000-000000000001";

    private static DepotMoveRequest ToCity(Func<DepotMoveRequest, DepotMoveRequest> change = null)
    {
        var request = new DepotMoveRequest
        {
            HoldingExchange = TradeExchangeType.Korriban,
            DockExchange = TradeExchangeType.Korriban,
            HoldingDockId = NpcDock,
            HoldingAtCityStarport = false,
            DockId = CityDock,
            DockIsCityStarport = true,
            StarportLevel = 2,
            OwnerIsCitizen = true,
            NowUtc = Now,
        };
        return change == null ? request : change(request);
    }

    private static DepotMoveRequest Copy(DepotMoveRequest request, bool? owns = null, TradeExchangeType? dockExchange = null, int? level = null,
        bool? citizen = null, DateTime? last = null, bool? payment = null) => new()
    {
        OwnerOwnsHolding = owns ?? request.OwnerOwnsHolding,
        HoldingExchange = request.HoldingExchange,
        DockExchange = dockExchange ?? request.DockExchange,
        HoldingDockId = request.HoldingDockId,
        HoldingAtCityStarport = request.HoldingAtCityStarport,
        DockId = request.DockId,
        DockIsCityStarport = request.DockIsCityStarport,
        StarportLevel = level ?? request.StarportLevel,
        OwnerIsCitizen = citizen ?? request.OwnerIsCitizen,
        LastMoveUtc = last ?? request.LastMoveUtc,
        NowUtc = request.NowUtc,
        PaymentPending = payment ?? request.PaymentPending,
    };

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException();
        }
    }

    private static string Source(params string[] relative) =>
        File.ReadAllText(Path.Combine(new[] { Root, "SWLOR.Game.Server" }.Concat(relative).ToArray()));

    private static string Method(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"{signature} should exist");
        var open = source.IndexOf('{', start);
        var depth = 0;
        for (var index = open; index < source.Length; index++)
        {
            if (source[index] == '{')
                depth++;
            else if (source[index] == '}' && --depth == 0)
                return source[start..(index + 1)];
        }

        throw new InvalidOperationException($"{signature} has no closing brace.");
    }

    // The rules

    [Test]
    public void ACitizenAtTheirStarportOfLevelTwo_MovesAHoldingFromTheNpcDock()
    {
        PlanetaryDepotRules.MinimumStarportLevel.Should().Be(2);
        PlanetaryDepotRules.CooldownDays.Should().Be(7);
        PlanetaryDepotRules.GetMoveRefusal(ToCity()).Should().Be(DepotRefusal.None);
    }

    [Test]
    public void TheStarportNeedsLevelTwo()
    {
        PlanetaryDepotRules.GetMoveRefusal(Copy(ToCity(), level: 1)).Should().Be(DepotRefusal.StarportTooLow);
        PlanetaryDepotRules.GetMoveRefusal(Copy(ToCity(), level: 0)).Should().Be(DepotRefusal.StarportTooLow);
        PlanetaryDepotRules.GetMoveRefusal(Copy(ToCity(), level: 5)).Should().Be(DepotRefusal.None);
    }

    [Test]
    public void OnlyACitizenOfTheCity_UsesItsDepot()
    {
        PlanetaryDepotRules.GetMoveRefusal(Copy(ToCity(), citizen: false)).Should().Be(DepotRefusal.NotACitizen);
    }

    [Test]
    public void ItIsOnceEverySevenDays_FromTheLastMove()
    {
        var yesterday = Now.AddDays(-1);
        PlanetaryDepotRules.GetMoveRefusal(Copy(ToCity(), last: yesterday)).Should().Be(DepotRefusal.TooSoon);
        PlanetaryDepotRules.GetMoveRefusal(Copy(ToCity(), last: Now.AddDays(-7).AddSeconds(1))).Should().Be(DepotRefusal.TooSoon);
        PlanetaryDepotRules.GetMoveRefusal(Copy(ToCity(), last: Now.AddDays(-7))).Should().Be(DepotRefusal.None);
        PlanetaryDepotRules.GetMoveRefusal(Copy(ToCity(), last: Now.AddDays(-30))).Should().Be(DepotRefusal.None);

        PlanetaryDepotRules.GetDaysLeft(yesterday, Now).Should().Be(6);
        PlanetaryDepotRules.GetDaysLeft(Now.AddDays(-7).AddSeconds(1), Now).Should().Be(1);
        PlanetaryDepotRules.GetDaysLeft(Now.AddDays(-8), Now).Should().Be(0);
        PlanetaryDepotRules.GetDaysLeft(default, Now).Should().Be(0);
    }

    [Test]
    public void AHoldingThatNeverMoved_MovesAtOnce()
    {
        PlanetaryDepotRules.GetNextMoveUtc(default).Should().Be(DateTime.MinValue);
        PlanetaryDepotRules.GetMoveRefusal(Copy(ToCity(), last: default(DateTime))).Should().Be(DepotRefusal.None);
        new PlanetaryHolding().LastDepotMoveUtc.Should().Be(default(DateTime));
    }

    [Test]
    public void AHoldingMovesBackToANpcDock_AtAnyStarportLevel_UnderTheSameLimit()
    {
        var back = new DepotMoveRequest
        {
            HoldingExchange = TradeExchangeType.Korriban,
            DockExchange = TradeExchangeType.Korriban,
            HoldingDockId = CityDock,
            HoldingAtCityStarport = true,
            DockId = NpcDock,
            DockIsCityStarport = false,
            NowUtc = Now,
        };

        PlanetaryDepotRules.GetMoveRefusal(back).Should().Be(DepotRefusal.None);

        var recent = new DepotMoveRequest
        {
            HoldingExchange = back.HoldingExchange, DockExchange = back.DockExchange, HoldingDockId = back.HoldingDockId,
            HoldingAtCityStarport = true, DockId = back.DockId, DockIsCityStarport = false, NowUtc = Now, LastMoveUtc = Now.AddDays(-2),
        };
        PlanetaryDepotRules.GetMoveRefusal(recent).Should().Be(DepotRefusal.TooSoon);
    }

    [Test]
    public void TheDepotServesOnlyTheHoldingsOfItsOwnPlanet()
    {
        PlanetaryDepotRules.GetMoveRefusal(Copy(ToCity(), dockExchange: TradeExchangeType.Viscara)).Should().Be(DepotRefusal.WrongPlanet);
    }

    [Test]
    public void OnlyTheOwnerMovesAHolding_AndNotWhileAPaymentIsPending()
    {
        PlanetaryDepotRules.GetMoveRefusal(Copy(ToCity(), owns: false)).Should().Be(DepotRefusal.NotYourHolding);
        PlanetaryDepotRules.GetMoveRefusal(Copy(ToCity(), payment: true)).Should().Be(DepotRefusal.PaymentPending);
    }

    [Test]
    public void AHoldingAlreadyAtTheDock_HasNothingToMove()
    {
        var here = ToCity();
        var request = new DepotMoveRequest
        {
            HoldingExchange = here.HoldingExchange, DockExchange = here.DockExchange, HoldingDockId = CityDock, HoldingAtCityStarport = true,
            DockId = CityDock, DockIsCityStarport = true, StarportLevel = 3, OwnerIsCitizen = true, NowUtc = Now,
        };
        PlanetaryDepotRules.GetMoveRefusal(request).Should().Be(DepotRefusal.AlreadyHere);
    }

    [Test]
    public void TheDepotMovesToOrFromACityStarport_NeverBetweenTwoNpcDocks()
    {
        var npcToNpc = new DepotMoveRequest
        {
            HoldingExchange = TradeExchangeType.SmugglersMoon, DockExchange = TradeExchangeType.SmugglersMoon,
            HoldingDockId = "smugglersmoon_landing", HoldingAtCityStarport = false,
            DockId = "smugglersmoon_station", DockIsCityStarport = false, NowUtc = Now,
        };
        PlanetaryDepotRules.GetMoveRefusal(npcToNpc).Should().Be(DepotRefusal.NotACityMove);
    }

    [Test]
    public void EveryRefusalHasAMessage_AndTheCooldownOneNamesTheDaysLeft()
    {
        foreach (var refusal in Enum.GetValues<DepotRefusal>().Where(x => x != DepotRefusal.None))
        {
            PlanetaryDepotRules.GetRefusalMessage(refusal, 3).Should().NotBeNullOrWhiteSpace(refusal.ToString());
            PlanetaryDepotRules.GetRefusalMessage(refusal, 3).All(c => c >= ' ' && c <= '~').Should().BeTrue(refusal.ToString());
        }

        PlanetaryDepotRules.GetRefusalMessage(DepotRefusal.TooSoon, 3).Should().Contain("3 days");
        PlanetaryDepotRules.GetRefusalMessage(DepotRefusal.TooSoon, 1).Should().Contain("1 day.");
        PlanetaryDepotRules.GetRefusalMessage(DepotRefusal.None).Should().BeEmpty();
    }

    // Stations

    [Test]
    public void AnOrbitStationsDepot_ServesTheHoldingsOnItsOrbitsPlanet()
    {
        foreach (var anchor in StationTuning.Anchors.Where(x => !x.IsWildSpace))
        {
            var holdingsExchange = HoldingRules.GetCityDockHoldingsExchange(anchor.Exchange, false);
            if (holdingsExchange == TradeExchangeType.Invalid)
                continue;

            holdingsExchange.Should().Be(anchor.Exchange, anchor.Id);
            var onPlanet = Enum.GetValues<HoldingType>().Select(HoldingRules.GetDetail).First(x => x != null && x.Exchange == holdingsExchange);
            var elsewhere = Enum.GetValues<HoldingType>().Select(HoldingRules.GetDetail).First(x => x != null && x.Exchange != holdingsExchange);

            PlanetaryDepotRules.GetMoveRefusal(new DepotMoveRequest
            {
                HoldingExchange = onPlanet.Exchange, DockExchange = holdingsExchange, HoldingDockId = "npc", DockId = CityDock,
                DockIsCityStarport = true, StarportLevel = 2, OwnerIsCitizen = true, NowUtc = Now,
            }).Should().Be(DepotRefusal.None, anchor.Id);

            PlanetaryDepotRules.GetMoveRefusal(new DepotMoveRequest
            {
                HoldingExchange = elsewhere.Exchange, DockExchange = holdingsExchange, HoldingDockId = "npc", DockId = CityDock,
                DockIsCityStarport = true, StarportLevel = 2, OwnerIsCitizen = true, NowUtc = Now,
            }).Should().Be(DepotRefusal.WrongPlanet, anchor.Id);
        }
    }

    [Test]
    public void AWildSpaceStation_HasNoDepot_BecauseItSellsNoHoldings()
    {
        foreach (var anchor in StationTuning.Anchors.Where(x => x.IsWildSpace))
        {
            HoldingRules.GetCityDockHoldingsExchange(anchor.Exchange, true).Should().Be(TradeExchangeType.Invalid, anchor.Id);
        }
    }

    // Output and the lost Starport

    [Test]
    public void MovingAHolding_ChangesOnlyWhereItsOutputWaits()
    {
        var move = Method(Source("Service", "PlanetaryDepot.cs"), "public static bool MoveHere(");

        move.Should().Contain("holding.DockId = dockId;");
        move.Should().Contain("holding.LastDepotMoveUtc = now;");
        move.Should().NotContain("Warehouse");
        move.Should().NotContain("Level");
        move.Should().NotContain("LastProcessedAt");
        move.Should().NotContain("UpkeepOwed");
    }

    [Test]
    public void ALostStarport_SendsTheHoldingToTheNpcDockWithoutUsingTheLimit()
    {
        var rehome = Method(Source("Service", "PlanetaryHoldings.cs"), "public static void RehomeStrandedHoldings(");

        rehome.Should().Contain("holding.DockId = npcDockId;");
        rehome.Should().NotContain("LastDepotMoveUtc");
    }

    [Test]
    public void TheHoldingsWindow_OffersTheMoveToTheDockItIsOpenedAt()
    {
        var model = Source("Feature", "GuiDefinition", "ViewModel", "PlanetaryHoldingsViewModel.cs");
        model.Should().Contain("PlanetaryDepot.MoveHere(Player, id, _exchange, _dockId)");
        model.Should().Contain("PlanetaryDepot.GetRefusal(Player, holding, _exchange, _dockId, now)");

        Source("Feature", "GuiDefinition", "PlanetaryHoldingsDefinition.cs").Should().Contain("model.OnClickMoveHere()");
    }
}
