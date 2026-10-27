using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.TradeService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Dock exchanges (plan section 13): a ship docked at an NPC starport buys and sells commodities there. Prices
    /// come from <see cref="TradePricing"/>, flooding is saved per exchange in <see cref="TradeExchangeState"/>, and a
    /// profitable sale gives Piloting XP. Cargo is units on <see cref="PlayerShip.Cargo"/>, never items.
    /// </summary>
    public static partial class Trade
    {
        /// <summary>The cargo bay of a ship: its hull's Cargo stat. Ships without a revamp hull have none.</summary>
        public static int GetCargoCapacity(PlayerShip ship)
        {
            return Space.FindShipDetail(ship?.Status?.ItemTag)?.Hull?.Cargo ?? 0;
        }

        /// <summary>The ship the player is flying, or null.</summary>
        public static PlayerShip GetActiveShip(uint player)
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            if (dbPlayer == null || string.IsNullOrWhiteSpace(dbPlayer.ActiveShipId) || dbPlayer.ActiveShipId == Guid.Empty.ToString())
                return null;

            return DB.Get<PlayerShip>(dbPlayer.ActiveShipId);
        }

        /// <summary>Whether the ship sits at the NPC dock whose landing area this is (not in space, not aboard a host).</summary>
        public static bool IsDockedAt(PlayerShip ship, string landingAreaResref)
        {
            if (ship == null || string.IsNullOrWhiteSpace(landingAreaResref) || Space.IsDockedAboardHost(ship, out _))
                return false;

            var property = DB.Get<WorldProperty>(ship.PropertyId);
            if (property == null || property.Positions.ContainsKey(PropertyLocationType.CurrentPosition))
                return false;

            return property.Positions.TryGetValue(PropertyLocationType.DockPosition, out var dock) &&
                   dock.AreaResref == landingAreaResref;
        }

        /// <summary>The player's own ships docked at this NPC dock. Cargo and its profit stay with the owner.</summary>
        public static List<PlayerShip> GetShipsDockedAt(uint player, string landingAreaResref)
        {
            return PlayerShipAccess.GetOwnedShips(GetObjectUUID(player))
                .Where(ship => IsDockedAt(ship, landingAreaResref))
                .ToList();
        }

        /// <summary>The exchange's flooding brought up to now. A new exchange starts unflooded.</summary>
        public static TradeExchangeState GetExchangeState(TradeExchangeType exchange, DateTime now)
        {
            var state = DB.Get<TradeExchangeState>(TradeExchangeState.GetId(exchange)) ?? new TradeExchangeState
            {
                Id = TradeExchangeState.GetId(exchange),
                Exchange = exchange
            };

            foreach (var market in state.Markets.Values)
            {
                TradePricing.Advance(market, now);
            }

            return state;
        }

        /// <summary>The current pressure on a commodity at an exchange.</summary>
        public static double GetPressure(TradeExchangeState state, CommodityType commodity)
        {
            return state.Markets.TryGetValue(commodity, out var market) ? market.Pressure : 0;
        }

        /// <summary>Buys units for the player's ship docked here. An order for more than fits or is affordable buys what it can.</summary>
        public static void Buy(uint player, string shipId, TradeExchangeType exchangeType, string landingAreaResref, CommodityType commodity, int units)
        {
            if (!TryGetTrade(player, shipId, exchangeType, landingAreaResref, commodity, out var ship, out var detail, out var market))
                return;

            var now = DateTime.UtcNow;
            var state = GetExchangeState(exchangeType, now);
            var capacity = GetCargoCapacity(ship);
            var order = TradeRules.PlanBuy(
                detail,
                market,
                GetPressure(state, commodity),
                GetAbilityScore(player, AbilityType.Social),
                units,
                capacity,
                CargoHoldRules.GetFreeVolume(capacity, ship.Cargo),
                GetGold(player));

            if (order.Refusal != TradeRefusal.None)
            {
                SendMessageToPC(player, ColorToken.Red(TradeRules.GetRefusalMessage(order.Refusal)));
                return;
            }

            TakeGoldFromCreature(order.Credits, player, true);
            CargoHoldRules.AddUnits(ship.Cargo, commodity, order.Units, order.Credits);
            AddPressure(state, commodity, -order.Units, now);
            DB.Set(ship);
            DB.Set(state);

            var reduced = order.WasReduced ? " (all that fits and you can afford)" : string.Empty;
            SendMessageToPC(player, ColorToken.Green($"Bought {order.Units} {detail.Name} for {order.Credits} credits{reduced}."));
            Log.Write(LogGroup.Space, $"TRADE BUY - {PlayerName.GetAuditName(player)} - {order.Units} {commodity} for {order.Credits} at {exchangeType} - ship {ship.Id}");
        }

        /// <summary>Sells units from the player's ship docked here and gives Piloting XP for the profit.</summary>
        public static void Sell(uint player, string shipId, TradeExchangeType exchangeType, string landingAreaResref, CommodityType commodity, int units)
        {
            if (!TryGetTrade(player, shipId, exchangeType, landingAreaResref, commodity, out var ship, out var detail, out var market))
                return;

            var now = DateTime.UtcNow;
            var state = GetExchangeState(exchangeType, now);
            var order = TradeRules.PlanSell(
                detail,
                market,
                GetPressure(state, commodity),
                GetAbilityScore(player, AbilityType.Social),
                units,
                CargoHoldRules.GetUnits(ship.Cargo, commodity));

            if (order.Refusal != TradeRefusal.None)
            {
                SendMessageToPC(player, ColorToken.Red(TradeRules.GetRefusalMessage(order.Refusal)));
                return;
            }

            var cost = CargoHoldRules.RemoveUnits(ship.Cargo, commodity, order.Units);
            GiveGoldToCreature(player, order.Credits);
            AddPressure(state, commodity, order.Units, now);
            DB.Set(ship);
            DB.Set(state);

            var profit = order.Credits - cost;
            var profitText = profit >= 0 ? $"a profit of {profit}" : $"a loss of {-profit}";
            SendMessageToPC(player, ColorToken.Green($"Sold {order.Units} {detail.Name} for {order.Credits} credits, {profitText}."));
            Log.Write(LogGroup.Space, $"TRADE SELL - {PlayerName.GetAuditName(player)} - {order.Units} {commodity} for {order.Credits} (cost {cost}) at {exchangeType} - ship {ship.Id}");

            var xp = TradeRules.GetSaleXp(profit);
            if (xp > 0)
                Skill.GiveSkillXP(player, SkillType.Piloting, xp);
        }

        private static void AddPressure(TradeExchangeState state, CommodityType commodity, int units, DateTime now)
        {
            if (!state.Markets.TryGetValue(commodity, out var market))
            {
                market = new TradeMarketState { UpdatedAt = now };
                state.Markets[commodity] = market;
            }

            market.Pressure += units;
            market.UpdatedAt = now;
        }

        private static bool TryGetTrade(
            uint player,
            string shipId,
            TradeExchangeType exchangeType,
            string landingAreaResref,
            CommodityType commodity,
            out PlayerShip ship,
            out CommodityDetail detail,
            out TradeMarketDetail market)
        {
            ship = string.IsNullOrWhiteSpace(shipId) ? null : DB.Get<PlayerShip>(shipId);
            detail = TradeLookup.GetCommodity(commodity);
            market = TradeLookup.GetExchange(exchangeType)?.FindMarket(commodity);

            if (ship == null || ship.OwnerPlayerId != GetObjectUUID(player) || !IsDockedAt(ship, landingAreaResref))
            {
                SendMessageToPC(player, ColorToken.Red(TradeRules.GetRefusalMessage(TradeRefusal.ShipNotDocked)));
                return false;
            }

            if (detail == null || market == null)
            {
                SendMessageToPC(player, ColorToken.Red(TradeRules.GetRefusalMessage(TradeRefusal.NotTradedHere)));
                return false;
            }

            return true;
        }
    }
}
