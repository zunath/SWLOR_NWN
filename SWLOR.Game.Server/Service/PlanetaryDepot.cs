using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.HoldingService;
using SWLOR.Game.Server.Service.IndustryService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The Planetary Depot (Player City Revamp, "Planetary industry"): a citizen standing at their city's Starport of level 2 or
    /// higher can bring a holding of the same planet there from the NPC dock, and from there back to a dock, once every 7 days. The
    /// rules are in <see cref="PlanetaryDepotRules"/>. Only the dock the holding's output waits at changes.
    /// </summary>
    public static class PlanetaryDepot
    {
        /// <summary>Why the holding cannot move to the dock the player stands at, or none.</summary>
        public static DepotRefusal GetRefusal(uint player, PlanetaryHolding holding, TradeExchangeType exchange, string dockId, DateTime nowUtc)
        {
            var detail = holding == null ? null : HoldingRules.GetDetail(holding.Type);
            if (detail == null)
                return DepotRefusal.NotYourHolding;

            var cityDock = HoldingRules.IsCityDockId(dockId) ? Trade.TryGetCityDock(dockId) : null;
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            return PlanetaryDepotRules.GetMoveRefusal(new DepotMoveRequest
            {
                OwnerOwnsHolding = holding.OwnerPlayerId == GetObjectUUID(player),
                HoldingExchange = detail.Exchange,
                DockExchange = exchange,
                HoldingDockId = holding.DockId,
                DockId = dockId,
                HoldingAtCityStarport = HoldingRules.IsCityDockId(holding.DockId),
                DockIsCityStarport = cityDock != null,
                StarportLevel = cityDock?.StarportLevel ?? 0,
                OwnerIsCitizen = cityDock != null && dbPlayer != null && dbPlayer.CitizenPropertyId == cityDock.CityId,
                LastMoveUtc = holding.LastDepotMoveUtc,
                NowUtc = nowUtc,
                PaymentPending = HoldingPaymentRules.IsPending(holding),
            });
        }

        /// <summary>Moves the holding to the dock the player stands at. Returns false, telling the player why, when it cannot move.</summary>
        public static bool MoveHere(uint player, string holdingId, TradeExchangeType exchange, string dockId)
        {
            var holding = PlanetaryHoldings.LoadHolding(player, holdingId);
            var now = DateTime.UtcNow;
            var refusal = GetRefusal(player, holding, exchange, dockId, now);
            if (refusal != DepotRefusal.None)
            {
                SendMessageToPC(player, ColorToken.Red(PlanetaryDepotRules.GetRefusalMessage(
                    refusal, PlanetaryDepotRules.GetDaysLeft(holding?.LastDepotMoveUtc ?? default, now))));
                return false;
            }

            var cityDock = HoldingRules.IsCityDockId(dockId) ? Trade.TryGetCityDock(dockId) : null;
            var place = cityDock == null ? "this dock" : $"the {cityDock.CityName} Starport";
            holding.DockId = dockId;
            holding.LastDepotMoveUtc = now;
            HoldingRules.AddLog(holding, now, $"The Planetary Depot moved the holding to {place}. Its output waits there now.");
            DB.Set(holding);

            SendMessageToPC(player, ColorToken.Green(
                $"Your {PlanetaryHoldings.GetHoldingName(holding)} now ships from {place}. It can move again in {PlanetaryDepotRules.CooldownDays} days."));
            Log.Write(LogGroup.Space, $"HOLDING DEPOT - {PlayerName.GetAuditName(player)} - {holding.Type} {holding.Id} moved to {dockId}");
            return true;
        }
    }
}
