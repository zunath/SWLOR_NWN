using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>The exchange trade journal that keeps a trade's gold and cargo change together across a restart.</summary>
public class PendingTradeTests
{
    [Test]
    public void APendingTrade_RoundTripsThroughTheCharacterFileLocal()
    {
        var trade = new PendingTrade { Id = "t1", ShipId = "s1", Commodity = CommodityType.Grain, Units = -12, Credits = 0 };

        PendingTrade.TryParse(trade.Serialize(), out var parsed).Should().BeTrue();
        parsed.Should().BeEquivalentTo(trade);
        PendingTrade.TryParse(string.Empty, out _).Should().BeFalse();
        PendingTrade.TryParse("t1|s1|8|x|0", out _).Should().BeFalse();
        PendingTrade.TryParse(null, out _).Should().BeFalse();
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
        trade.Should().Contain("ObjectPlugin.SetString(player, PendingTradeVariable, trade.Serialize(), true);\n            ExportSingleCharacter(player);")
            .And.Contain("[NWNEventHandler(ScriptName.OnModuleEnter)]\n        public static void ApplyPendingTrade()")
            .And.NotContain("AssignCommand(player, () => TakeGoldFromCreature(order.Credits, player, true));",
                "the payment must be in the character file before it is exported");

        foreach (var method in new[] { "public static void Buy(", "public static void Sell(" })
        {
            var body = trade[trade.IndexOf(method, StringComparison.Ordinal)..];
            body.IndexOf("SaveTradeGold(player, trade);", StringComparison.Ordinal)
                .Should().BeLessThan(body.IndexOf("DB.Set(ship);", StringComparison.Ordinal), method);
        }
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
