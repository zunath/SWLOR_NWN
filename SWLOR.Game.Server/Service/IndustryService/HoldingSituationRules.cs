using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.HoldingService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Service.IndustryService
{
    /// <summary>
    /// Holding events that reach City Affairs (Player City Revamp, "Planetary industry"): a bandit raid or a customs inspection at a
    /// citizen's holding on the city's planet can raise a situation. The city can send help against a raid or pay a customs fine
    /// from the treasury. An event the owner has already settled closes its situation. Situation texts name no player.
    /// </summary>
    public static class HoldingSituationRules
    {
        public const int RaidHelpCost = 5000;

        /// <summary>The situation a holding event raises, or Invalid for an event that raises none.</summary>
        public static SituationType GetSituationType(HoldingEventType eventType)
        {
            return eventType switch
            {
                HoldingEventType.Raid => SituationType.HoldingRaid,
                HoldingEventType.Customs => SituationType.HoldingCustoms,
                _ => SituationType.Invalid,
            };
        }

        /// <summary>The situation raised by what a holding advance left pending: a raid that began, or a fine that was handed out.</summary>
        public static List<SituationType> GetNewSituations(bool hadRaid, bool hadFine, PlanetaryHolding holding)
        {
            var types = new List<SituationType>();
            if (holding.RaidPending && !hadRaid)
                types.Add(SituationType.HoldingRaid);
            if (holding.CustomsFine > 0 && !hadFine)
                types.Add(SituationType.HoldingCustoms);

            return types;
        }

        /// <summary>
        /// When the situation runs out: the usual 72 hours, or the holding's own deadline if that comes first, since the event
        /// resolves itself then.
        /// </summary>
        public static DateTime GetExpiryUtc(DateTime drawnUtc, DateTime holdingDeadlineUtc)
        {
            var usual = SituationRules.GetExpiryUtc(drawnUtc);
            return holdingDeadlineUtc < usual ? holdingDeadlineUtc : usual;
        }

        /// <summary>The credits of a customs fine the treasury can pay: the fine, up to the cost limit of a situation.</summary>
        public static int GetFineRelief(int customsFine)
        {
            return Math.Clamp(customsFine, 0, SituationRules.MaxCost);
        }

        /// <summary>Whether the event a situation is about is still going: a raid that holds the site, or a fine that is owed.</summary>
        public static bool IsStillPending(SituationType type, PlanetaryHolding holding)
        {
            if (holding == null)
                return false;

            return type switch
            {
                SituationType.HoldingRaid => holding.RaidPending,
                SituationType.HoldingCustoms => holding.CustomsFine > 0,
                _ => false,
            };
        }

        /// <summary>What the officials are told about the holding, without naming its owner.</summary>
        public static string BuildNote(SituationType type, HoldingDetail detail, string planetName, PlanetaryHolding holding)
        {
            var place = $"The holding is a level {holding.Level} {detail.Name} on {planetName}.";
            return type switch
            {
                SituationType.HoldingRaid => $"{place} The raiders hold the site until {holding.RaidDeadline:yyyy-MM-dd HH:mm} UTC.",
                SituationType.HoldingCustoms =>
                    $"{place} The fine is {SituationRules.FormatCredits(holding.CustomsFine)}, due {holding.CustomsDeadline:yyyy-MM-dd HH:mm} UTC.",
                _ => place,
            };
        }

        /// <summary>The deadline of the pending event the situation is about.</summary>
        public static DateTime GetDeadlineUtc(SituationType type, PlanetaryHolding holding)
        {
            return type == SituationType.HoldingRaid ? holding.RaidDeadline : holding.CustomsDeadline;
        }

        /// <summary>The holding log line when the city drives raiders off.</summary>
        public static string BuildHelpLog(string cityName)
        {
            return $"Bandit raid: the city of {cityName} sent help and drove the raiders off. The warehouse is safe.";
        }

        /// <summary>The holding log line when the city pays part or all of a fine.</summary>
        public static string BuildFineLog(string cityName, int credits, int remaining)
        {
            return remaining > 0
                ? $"Customs fine: the city of {cityName} paid {credits} credits of it. {remaining} credits are still owed."
                : $"Customs fine: the city of {cityName} paid the {credits} credit fine. The inspectors leave your stock alone.";
        }

        /// <summary>Takes a share of a fine off a holding. Returns the credits taken off.</summary>
        public static int ApplyFineRelief(PlanetaryHolding holding, int credits)
        {
            var relieved = Math.Clamp(credits, 0, Math.Max(0, holding.CustomsFine));
            holding.CustomsFine -= relieved;
            return relieved;
        }
    }
}
