using System;
using SWLOR.Game.Server.Service.FactionService;

namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>
    /// The faction standing a trade event changes. Getting caught smuggling, running from customs, attacking a lawful
    /// ship and raiding a convoy cost the authority's faction standing; a delivered freight contract earns standing with
    /// the destination exchange's faction. Standing (-5000 to 5000) sorts into tiers that cut customs scans, improve
    /// prices and unlock larger freight loads; Hostile standing sends the authority's patrols after the pilot.
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

        /// <summary>Hostile at or below <see cref="TradeTuning.StandingHostile"/>; otherwise the highest tier whose minimum is met.</summary>
        public static TradeStandingTier GetTier(int standing)
        {
            if (standing <= TradeTuning.StandingHostile)
                return TradeStandingTier.Hostile;

            var tier = TradeStandingTier.Neutral;
            for (var index = 0; index < TradeTuning.StandingTierMinimums.Length; index++)
            {
                if (standing >= TradeTuning.StandingTierMinimums[index])
                    tier = (TradeStandingTier)(index + 1);
            }

            return tier;
        }

        /// <summary>The share (percent) of a customs scan chance this tier removes.</summary>
        public static int GetScanCutPercent(TradeStandingTier tier)
        {
            return tier > TradeStandingTier.Neutral ? TradeTuning.StandingScanCutPercents[(int)tier - 1] : 0;
        }

        /// <summary>The fraction this tier adds to Social's price improvement, on both sides of a trade.</summary>
        public static double GetPriceBonus(TradeStandingTier tier)
        {
            return tier > TradeStandingTier.Neutral ? TradeTuning.StandingPriceBonuses[(int)tier - 1] : 0;
        }

        /// <summary>The standing a pilot needs to take a freight load of this volume (0 for the small loads).</summary>
        public static int GetFreightMinimumStanding(int volume)
        {
            var index = Array.IndexOf(TradeTuning.FreightOfferVolumes, volume);
            return index < 0 ? 0 : TradeTuning.FreightOfferMinimumStandings[index];
        }

        /// <summary>
        /// Whether an authority's patrol attacks a pilot who enters its system: their standing is Hostile and no patrol
        /// came for them within the cooldown.
        /// </summary>
        public static bool ShouldPatrolAttack(TradeStandingTier tier, DateTime? lastAttack, DateTime now)
        {
            return tier == TradeStandingTier.Hostile &&
                   (lastAttack == null || (now - lastAttack.Value).TotalSeconds >= TradeTuning.HostilePatrolCooldownSeconds);
        }

        /// <summary>The tier's display name.</summary>
        public static string GetTierName(TradeStandingTier tier) => tier.ToString();

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
