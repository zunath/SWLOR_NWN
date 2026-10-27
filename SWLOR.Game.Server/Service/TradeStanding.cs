using SWLOR.Game.Server.Service.FactionService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Lawful standing from trade (plan section 13, "Factions"): the one place trade changes faction standing. Customs,
    /// convoy raids and freight contracts call it; W7c adds what standing unlocks (prices and contracts).
    /// </summary>
    public static partial class Trade
    {
        /// <summary>Changes the player's standing with a lawful authority's faction.</summary>
        public static void RecordStanding(uint player, TradeAuthority authority, TradeStandingEvent standingEvent)
        {
            RecordStanding(player, TradeStandingRules.GetFaction(authority), standingEvent);
        }

        /// <summary>Changes the player's standing with a faction; nothing for Invalid.</summary>
        public static void RecordStanding(uint player, FactionType faction, TradeStandingEvent standingEvent)
        {
            var amount = TradeStandingRules.GetAmount(standingEvent);
            if (!GetIsPC(player) || faction == FactionType.Invalid || amount == 0)
                return;

            Faction.AdjustPlayerFactionStanding(player, faction, amount);
            Log.Write(LogGroup.Space, $"TRADE STANDING - {PlayerName.GetAuditName(player)} - {standingEvent} {faction} {amount}");
        }
    }
}
