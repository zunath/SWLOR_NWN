using SWLOR.Game.Server.Service.FactionService;

namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>
    /// The faction standing a trade event changes. Getting caught smuggling, running from customs, attacking a lawful
    /// ship and raiding a convoy cost the authority's faction standing; a delivered freight contract earns standing with
    /// the destination exchange's faction. The faction-standing trade unlocks (prices, contracts) come with W7c.
    /// </summary>
    public static class TradeStandingRules
    {
        /// <summary>The ground faction of a lawful authority, or Invalid for None.</summary>
        public static FactionType GetFaction(TradeAuthority authority)
        {
            return authority switch
            {
                TradeAuthority.Republic => FactionType.RepublicMilitary,
                TradeAuthority.Czerka => FactionType.Czerka,
                TradeAuthority.Sith => FactionType.SithMilitary,
                _ => FactionType.Invalid,
            };
        }

        /// <summary>The standing change of an event (negative for offenses).</summary>
        public static int GetAmount(TradeStandingEvent standingEvent)
        {
            return standingEvent switch
            {
                TradeStandingEvent.CaughtSmuggling => TradeTuning.StandingCaughtSmuggling,
                TradeStandingEvent.RanFromCustoms => TradeTuning.StandingRanFromCustoms,
                TradeStandingEvent.AttackedLawfulShip => TradeTuning.StandingAttackedLawfulShip,
                TradeStandingEvent.RaidedConvoy => TradeTuning.StandingRaidedConvoy,
                TradeStandingEvent.DeliveredFreight => TradeTuning.StandingDeliveredFreight,
                _ => 0,
            };
        }
    }
}
