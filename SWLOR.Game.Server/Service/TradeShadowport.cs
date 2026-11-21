using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.IndustryService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Shadowport and the lawful port at a city door of the Trade Exchange (Player City Revamp, "Planetary industry"). The shared
    /// market's goods, pressure and buy prices never change: a door only prices what it buys (<see cref="ShadowportRules"/>) and
    /// rolls the customs bust on a contraband sale.
    /// </summary>
    public static partial class Trade
    {
        /// <summary>How the door treats banned goods: an open port, a lawful port, or a Shadowport.</summary>
        public static ShadowportStance GetShadowportStance(TradeDock dock, TradeExchangeType exchange)
        {
            if (dock == null || !dock.IsCity)
                return ShadowportStance.OpenPort;

            return ShadowportRules.GetStance(TradeLookup.GetExchange(exchange)?.Authority ?? TradeAuthority.None, dock.ShadowportActive);
        }

        /// <summary>
        /// The market a door buys a banned good by when the shared market does not trade it, or null when the door buys nothing of the
        /// kind. A sale of such a good is a contraband sale.
        /// </summary>
        public static TradeMarketDetail GetContrabandSaleMarket(TradeDock dock, TradeExchangeType exchange, CommodityType commodity)
        {
            var stance = GetShadowportStance(dock, exchange);
            return ShadowportRules.IsContrabandSale(TradeLookup.GetExchange(exchange), commodity, stance)
                ? ShadowportRules.GetContrabandMarket(commodity, stance)
                : null;
        }

        /// <summary>The market a sale of the good is priced by: normal goods sell 15% lower at a Shadowport, and the rest is unchanged.</summary>
        public static TradeMarketDetail GetSaleMarket(TradeDock dock, TradeExchangeType exchange, CommodityType commodity, TradeMarketDetail market)
        {
            if (market == null || TradeLookup.GetExchange(exchange)?.FindMarket(commodity) == null)
                return market;

            return TradePricing.WithPriceMultiplier(market, ShadowportRules.GetNormalGoodsSaleFactor(GetShadowportStance(dock, exchange)));
        }

        /// <summary>
        /// Rolls the customs bust of a contraband sale at a city door. A bust is the customs result: the fine, the banned cargo
        /// confiscated, and lawful standing lost. The sale does not happen. Returns true when the sale was busted.
        /// </summary>
        private static bool TryBustContrabandSale(uint player, PlayerShip ship, TradeDock dock, TradeExchangeType exchange, CommodityType commodity)
        {
            var detail = TradeLookup.GetExchange(exchange);
            var stance = GetShadowportStance(dock, exchange);
            if (!ShadowportRules.IsContrabandSale(detail, commodity, stance))
                return false;

            var authority = detail.Authority;
            var bannedValue = CustomsRules.GetBannedValue(ship, Space.GetDockedShips(ship.Id), authority);
            var tier = GetStandingTier(player, TradeStandingRules.GetFaction(authority));
            var chance = ShadowportRules.GetBustChance(
                bannedValue,
                CustomsRules.GetCompartmentPercent(GetFittedModuleTags(ship)),
                TradeStandingRules.GetScanCutPercent(tier),
                stance);
            if (Random.NextFloat() >= chance)
                return false;

            var shipId = ship.Id;
            var fineDue = CustomsRules.GetFine(bannedValue);
            AssignCommand(player, () =>
            {
                var (fine, taken) = CollectCustomsFine(player, shipId, authority, fineDue, true);
                RecordStanding(player, authority, TradeStandingEvent.CaughtSmuggling);
                SendMessageToPC(player, ColorToken.Red(taken.Count > 0
                    ? $"{dock.CityName} customs bust the sale! You pay a {fine} credit fine and they confiscate {CustomsRules.Describe(taken)}."
                    : $"{dock.CityName} customs bust the sale! You pay a {fine} credit fine."));
                Log.Write(LogGroup.Space, $"SHADOWPORT BUST - {PlayerName.GetAuditName(player)} - {stance} {dock.CityName} ({dock.CityId}) {authority}: fined {fine}, confiscated {CustomsRules.Describe(taken)} - chance {chance:0.###}");
            });
            return true;
        }
    }
}
