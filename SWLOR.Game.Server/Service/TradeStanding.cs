using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.FactionService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Lawful standing from trade (plan section 13, "Factions"): the one place trade changes faction standing. Customs,
    /// convoy raids and freight contracts call it. What standing unlocks: better prices at the faction's exchanges, fewer
    /// customs scans, larger freight loads, and a hostile authority's patrols attack on sight (W7c).
    /// </summary>
    public static partial class Trade
    {
        /// <summary>The player's standing with a faction (0 with none, and for Invalid).</summary>
        public static int GetStanding(uint player, FactionType faction)
        {
            if (!GetIsPC(player) || faction == FactionType.Invalid)
                return 0;

            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            return dbPlayer != null && dbPlayer.Factions.TryGetValue(faction, out var standing) ? standing.Standing : 0;
        }

        /// <summary>How a faction regards the player.</summary>
        public static TradeStandingTier GetStandingTier(uint player, FactionType faction)
        {
            return TradeStandingRules.GetTier(GetStanding(player, faction));
        }

        /// <summary>The price bonus the player's standing earns at an exchange: its faction's tier (Dathomir has none).</summary>
        public static double GetPriceBonus(uint player, TradeExchangeType exchange)
        {
            var faction = TradeLookup.GetExchange(exchange)?.StandingFaction ?? FactionType.Invalid;
            return TradeStandingRules.GetPriceBonus(GetStandingTier(player, faction));
        }

        /// <summary>
        /// The standing that gates the exchange's freight offers: the player's standing with its faction, or
        /// <see cref="int.MaxValue"/> when the exchange has no faction and offers every load to everyone.
        /// </summary>
        public static int GetFreightStanding(uint player, TradeExchangeType exchange)
        {
            var faction = TradeLookup.GetExchange(exchange)?.StandingFaction ?? FactionType.Invalid;
            return faction == FactionType.Invalid ? int.MaxValue : GetStanding(player, faction);
        }

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
