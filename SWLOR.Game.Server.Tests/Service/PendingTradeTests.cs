using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>The exchange trade journal that keeps a trade's gold and cargo change together across a restart.</summary>
public class PendingTradeTests
{
    [Test]
    public void APendingTrade_RoundTripsThroughTheCharacterFileLocal()
    {
        var trade = new PendingTrade
        {
            Id = "t1", ShipId = "s1", Commodity = CommodityType.Grain, Units = -12, Credits = 0,
            Exchange = TradeExchangeType.Viscara, CityId = "city1", Tariff = 45
        };

        PendingTrade.TryParse(trade.Serialize(), out var parsed).Should().BeTrue();
        parsed.Should().BeEquivalentTo(trade);
        var npc = new PendingTrade { Id = "t2", ShipId = "s1", Commodity = CommodityType.Grain, Units = 3, Credits = 90, Exchange = TradeExchangeType.Viscara };
        PendingTrade.TryParse(npc.Serialize(), out var parsedNpc).Should().BeTrue();
        parsedNpc.Should().BeEquivalentTo(npc, "an NPC dock's mark has no city and no tariff");
        PendingTrade.TryParse(string.Empty, out _).Should().BeFalse();
        PendingTrade.TryParse("t1|s1|8|x|0|1||0", out _).Should().BeFalse();
        PendingTrade.TryParse("t1|s1|8|-12|0", out _).Should().BeFalse("a mark without its market and tariff cannot be replayed");
        PendingTrade.TryParse(null, out _).Should().BeFalse();
    }

    [Test]
    public void ATradesPressure_IsPutOnItsMarketOncePerPlayer()
    {
        var now = new DateTime(2026, 11, 23, 12, 0, 0, DateTimeKind.Utc);
        var state = new TradeExchangeState { Exchange = TradeExchangeType.Viscara };
        var sale = new PendingTrade { Id = "sell", ShipId = "s1", Commodity = CommodityType.Grain, Units = -8, Exchange = TradeExchangeType.Viscara };

        sale.ApplyPressureTo(state, "p1", now).Should().BeTrue();
        sale.ApplyPressureTo(state, "p1", now).Should().BeFalse("the exchange records the trade with its pressure");
        state.Markets[CommodityType.Grain].Pressure.Should().Be(8, "a sale floods the market");

        var purchase = new PendingTrade { Id = "buy", ShipId = "s1", Commodity = CommodityType.Grain, Units = 3, Credits = 90, Exchange = TradeExchangeType.Viscara };
        purchase.ApplyPressureTo(state, "p2", now).Should().BeTrue("another player's trade is kept apart");
        state.Markets[CommodityType.Grain].Pressure.Should().Be(5, "a purchase drains the market");

        new TradeExchangeState { Exchange = TradeExchangeType.Viscara, LastTradeIds = null }
            .Invoking(older => sale.ApplyPressureTo(older, "p1", now)).Should().NotThrow("older records load without the trade ids");
        sale.ApplyPressureTo(new TradeExchangeState { Exchange = TradeExchangeType.Tatooine }, "p1", now)
            .Should().BeFalse("a mark presses only its own exchange");
    }

    [Test]
    public void ATradesTariff_IsPaidToItsCityOnce()
    {
        var city = new WorldProperty { Id = "city1", Treasury = 1000, LastTariffTradeIds = null };
        var trade = new PendingTrade { Id = "buy", ShipId = "s1", Commodity = CommodityType.Grain, Units = 3, Credits = 90, CityId = "city1", Tariff = 12 };

        trade.PayTariffTo(city, "p1").Should().BeTrue();
        trade.PayTariffTo(city, "p1").Should().BeFalse("the city records the trade with the credit");
        city.Treasury.Should().Be(1012);

        trade.PayTariffTo(new WorldProperty { Id = "city2" }, "p1").Should().BeFalse("only the trade's own city is paid");
        new PendingTrade { Id = "npc", ShipId = "s1", Units = 3, Tariff = 0 }.PayTariffTo(city, "p1").Should().BeFalse("an NPC dock takes no tariff");
        city.Treasury.Should().Be(1012);
    }

    [Test]
    public void LoginRecovery_ReplaysTheMarketPressureAndTariffOfAnInterruptedTrade()
    {
        var trade = ReadServerSource("Service", "Trade.cs");
        var login = trade[trade.IndexOf("public static void ApplyPendingTrade()", StringComparison.Ordinal)..];
        login = login[..login.IndexOf("foreach (var reserved", StringComparison.Ordinal)];
        login.Should().Contain("ApplyTradeMark(player, playerId, trade);")
            .And.Contain("if (PressMarket(trade, playerId))")
            .And.Contain("if (PayTariff(trade, player))");

        var buy = trade[trade.IndexOf("private static void CompleteBuy(", StringComparison.Ordinal)..];
        buy = buy[..buy.IndexOf("public static void Sell(", StringComparison.Ordinal)];
        buy.IndexOf("DB.Set(ship);", StringComparison.Ordinal)
            .Should().BeLessThan(buy.IndexOf("PressMarket(trade, GetObjectUUID(player));", StringComparison.Ordinal));
        buy.Should().NotContain("AddPressure(", "the market changes only through the mark, which records it once");

        var sell = trade[trade.IndexOf("public static void Sell(", StringComparison.Ordinal)..];
        sell.Should().Contain("if (trade.ApplyPressureTo(state, GetObjectUUID(player), now))\n                DB.Set(state);");

        var city = ReadServerSource("Service", "TradeCity.cs");
        city.Should().Contain("private static bool PayTariff(PendingTrade trade, uint player)")
            .And.Contain("if (!trade.PayTariffTo(city, GetObjectUUID(player)))\n                return false;\n\n            DB.Set(city);");
    }

    [Test]
    public void APurchase_IsLoadedOnce()
    {
        var cargo = new Dictionary<CommodityType, CargoLot>();
        var trade = new PendingTrade { Id = "buy", ShipId = "s1", Commodity = CommodityType.Grain, Units = 10, Credits = 600 };

        trade.ApplyTo(cargo, string.Empty).Should().BeTrue();
        trade.ApplyTo(cargo, "buy").Should().BeFalse("the ship already records the trade");
        CargoHoldRules.GetUnits(cargo, CommodityType.Grain).Should().Be(10);
        cargo[CommodityType.Grain].CostBasis.Should().Be(600);
    }

    [Test]
    public void ASale_UnloadsWhatTheBayStillHolds()
    {
        var cargo = new Dictionary<CommodityType, CargoLot>();
        CargoHoldRules.AddUnits(cargo, CommodityType.Grain, 5, 300);
        var trade = new PendingTrade { Id = "sell", ShipId = "s1", Commodity = CommodityType.Grain, Units = -8 };

        trade.ApplyTo(cargo, "earlier").Should().BeTrue();
        CargoHoldRules.GetUnits(cargo, CommodityType.Grain).Should().Be(0);
    }

    [Test]
    public void BuyAndSell_SaveTheGoldWithTheMarkBeforeTheCargo()
    {
        var trade = ReadServerSource("Service", "Trade.cs");
        trade.Should().Contain("ObjectPlugin.SetString(player, PendingTradeVariable, trade.Serialize(), true);\n                    return true;\n                },\n                () => CharacterCheckpoint.Save(player),",
                "the gold and the mark are saved at once, before the cargo changes")
            .And.Contain("[NWNEventHandler(ScriptName.OnModuleEnter)]\n        public static void ApplyPendingTrade()")
            .And.Contain("AssignCommand(player, () => CompleteBuy(player, shipId, exchangeType, dockId, commodity, units));",
                "TakeGoldFromCreature does nothing from a NUI handler, so the purchase completes on the player's queue");

        var complete = trade[trade.IndexOf("private static void CompleteBuy(", StringComparison.Ordinal)..];
        var save = complete.IndexOf("if (!SaveTradeGold(player, trade,", StringComparison.Ordinal);
        save.Should().BeGreaterThan(0);
        complete.IndexOf("if (!TryPlanBuy(player, shipId, exchangeType, dockId, commodity, units, out var ship, out var detail, out var dock, out var order))",
                StringComparison.Ordinal)
            .Should().BeInRange(0, save, "purchases queued together are planned against the cargo and credits as they are when each runs");
        complete.IndexOf("() => TakeGoldFromCreature(order.Credits, player, true),", StringComparison.Ordinal)
            .Should().BeGreaterThan(save, "the payment is taken inside the save, which puts it back when the file cannot be saved");

        trade.Should().Contain("if (ship == null || ship.OwnerPlayerId != playerId)",
                "a former owner's mark never changes a ship that has changed owners")
            .And.Contain("ship.LastTradeIds[playerId] = trade.Id;", "each player's last trade is kept apart");

        foreach (var method in new[] { "public static void Buy(", "public static void Sell(" })
        {
            var body = trade[trade.IndexOf(method, StringComparison.Ordinal)..];
            body.IndexOf("if (!SaveTradeGold(player, trade,", StringComparison.Ordinal)
                .Should().BeLessThan(body.IndexOf("RecordTrade(ship, player, trade);", StringComparison.Ordinal), method);
            System.Text.RegularExpressions.Regex.IsMatch(body, @"if \(!SaveTradeGold\(player, trade,[\s\S]*?\)\)\)\n\s+(\{[\s\S]*?)?return;\n(\s+\}\n)?\n\s+RecordTrade\(")
                .Should().BeTrue($"{method} stops before the cargo changes when the character could not be saved");
        }
    }

    [Test]
    public void ASaleIsReservedOnTheShip_SoNoOtherPilotSpillsUnitsAlreadyPaidFor()
    {
        var trade = ReadServerSource("Service", "Trade.cs");
        var sell = trade[trade.IndexOf("public static void Sell(", StringComparison.Ordinal)..];
        var reserve = sell.IndexOf("ship.PendingSaleTradeId = trade.Id;\n            DB.Set(ship);", StringComparison.Ordinal);
        var save = sell.IndexOf("if (!SaveTradeGold(player, trade,", StringComparison.Ordinal);
        var record = sell.IndexOf("RecordTrade(ship, player, trade);", StringComparison.Ordinal);
        reserve.Should().BeInRange(0, save, "the reservation is saved before the proceeds");
        sell[save..record].Should().Contain("ship.PendingSaleTradeId = string.Empty;", "a failed save gives the ship back");
        var saved = sell[record..];
        saved.IndexOf("ship.PendingSaleTradeId = string.Empty;", StringComparison.Ordinal)
            .Should().BeInRange(0, saved.IndexOf("DB.Set(ship);", StringComparison.Ordinal), "the reservation goes with the bay change");

        var login = trade[trade.IndexOf("public static void ApplyPendingTrade()", StringComparison.Ordinal)..];
        login.IndexOf("ApplyTradeMark(player, playerId, trade);", StringComparison.Ordinal)
            .Should().BeInRange(0, login.IndexOf("reserved.PendingSaleTradeId = string.Empty;", StringComparison.Ordinal),
                "the owner's login applies the mark, then lifts the reservation, with or without a mark");

        ReadServerSource("Service", "Space.cs").Should().Contain("if (!string.IsNullOrWhiteSpace(dbShip.PendingSaleTradeId))",
            "no pilot launches a ship whose sale a restart interrupted");
    }

    [Test]
    public void TheExchangeWindow_RefreshesAgainAfterAPurchaseCompletesOnThePlayersQueue()
    {
        var window = ReadServerSource("Feature", "GuiDefinition", "ViewModel", "TradeExchangeViewModel.cs");
        var placeOrder = window[window.IndexOf("private void PlaceOrder(bool buying)", StringComparison.Ordinal)..];
        placeOrder = placeOrder[..placeOrder.IndexOf("public Action OnClickBuy()", StringComparison.Ordinal)];

        var buy = placeOrder.IndexOf("Trade.Buy(Player, shipId, _exchange, _dockId, _rows[index], GetOrderUnits());", StringComparison.Ordinal);
        var queued = placeOrder.IndexOf("AssignCommand(Player, Refresh);", StringComparison.Ordinal);
        buy.Should().BeGreaterThanOrEqualTo(0);
        queued.Should().BeGreaterThan(buy, "the refresh is queued behind the purchase Trade.Buy queued, so it reads the new credits and cargo");
        placeOrder.Should().NotContain("if (!buying)\n                AssignCommand(Player, Refresh);",
            "purchases refresh on the queue as sales do, so Sell is enabled once the first unit is bought");
    }

    private static string ReadServerSource(params string[] path)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        var full = Path.Combine(new[] { directory!.FullName, "SWLOR.Game.Server" }.Concat(path).ToArray());
        return File.ReadAllText(full).Replace("\r\n", "\n");
    }
}
