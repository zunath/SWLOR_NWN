using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.HoldingService;
using SWLOR.Game.Server.Service.IndustryService;
using SWLOR.Game.Server.Service.LogService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Holding events as City Affairs (Player City Revamp, "Planetary industry"). When a holding of a citizen, on the city's planet,
    /// is raided or fined by customs, the city can get a situation about it: send help against the raid, or pay the fine from the
    /// treasury. The rules are in <see cref="HoldingSituationRules"/>; this class reads and writes the records.
    /// </summary>
    public static class HoldingCityAffairs
    {
        /// <summary>
        /// Raises the situations a holding advance left pending: a raid or a customs fine that began during it. A city that cannot take
        /// another situation, or that the holding's owner does not belong to, raises none and the owner deals with the event alone.
        /// </summary>
        public static void RaiseSituations(PlanetaryHolding holding, HoldingDetail detail, bool hadRaid, bool hadFine, DateTime nowUtc)
        {
            var types = HoldingSituationRules.GetNewSituations(hadRaid, hadFine, holding);
            if (types.Count == 0)
                return;

            var owner = string.IsNullOrWhiteSpace(holding.OwnerPlayerId) ? null : DB.Get<Player>(holding.OwnerPlayerId);
            var city = owner == null || owner.IsDeleted ? null : Edict.GetLiveCity(owner.CitizenPropertyId);
            if (city == null || !IndustrialCharterRules.IsOnCityPlanet(detail.Exchange, EdictEffects.GetCityExchange(city), Edict.IsWildSpaceStation(city)))
                return;

            var planet = PlanetaryHoldings.GetPlanetName(detail);
            foreach (var type in types)
            {
                CityAffairs.RaiseEvent(
                    city.Id,
                    type,
                    holding.Id,
                    HoldingSituationRules.BuildNote(type, detail, planet, holding),
                    HoldingSituationRules.GetDeadlineUtc(type, holding),
                    nowUtc);
            }
        }

        /// <summary>
        /// The credits a situation's subject owes that its choice would pay: a holding's customs fine, up to the cost limit. 0 for a
        /// situation about anything else, and for a holding that is gone or owes nothing.
        /// </summary>
        public static int GetSubjectCredits(CitySituation situation)
        {
            if (situation == null || situation.Type != SituationType.HoldingCustoms || string.IsNullOrWhiteSpace(situation.SubjectId))
                return 0;

            var holding = DB.Get<PlanetaryHolding>(situation.SubjectId);
            return HoldingSituationRules.GetFineRelief(holding?.CustomsFine ?? 0);
        }

        /// <summary>
        /// Whether the event the situation is about is over, because the owner settled it or the holding is gone. Such a situation is
        /// closed without a decision, which costs the city nothing and counts as no neglect.
        /// </summary>
        public static bool CloseIfSettled(CitySituation situation, DateTime nowUtc)
        {
            if (situation == null ||
                situation.State != CitySituationState.Open ||
                string.IsNullOrWhiteSpace(situation.SubjectId) ||
                HoldingSituationRules.IsStillPending(situation.Type, DB.Get<PlanetaryHolding>(situation.SubjectId)))
            {
                return false;
            }

            situation.State = CitySituationState.Decided;
            situation.ResolvedUtc = nowUtc;
            DB.Set(situation);
            return true;
        }

        /// <summary>
        /// Applies a situation outcome's changes to its holding once, after the city record holds the outcome that pays for them:
        /// the city drives the raiders off, and the treasury pays off part of the customs fine. The holding records the outcome's
        /// situation in the same write, so an outcome a restart left on the city changes the holding once. True when it changed now.
        /// </summary>
        public static bool ApplyOutcome(WorldProperty city, CitySituationOutcome outcome, DateTime nowUtc)
        {
            if (outcome == null || !outcome.ChangesHolding)
                return false;

            var holding = DB.Get<PlanetaryHolding>(outcome.HoldingId);
            if (holding == null || string.Equals(holding.LastCitySituationId, outcome.SituationId, StringComparison.Ordinal))
                return false;

            var texts = new List<string>();
            if (outcome.HoldingHelp && holding.RaidPending)
            {
                holding.RaidPending = false;
                texts.Add(HoldingSituationRules.BuildHelpLog(city.CustomName));
                Log.Write(LogGroup.Space, $"HOLDING HELP - city {city.CustomName} ({city.Id}) drove the raiders off holding {holding.Id} of {holding.OwnerPlayerId}");
            }

            var relieved = outcome.HoldingFineCredits > 0 ? HoldingSituationRules.ApplyFineRelief(holding, outcome.HoldingFineCredits) : 0;
            if (relieved > 0)
            {
                texts.Add(HoldingSituationRules.BuildFineLog(city.CustomName, relieved, holding.CustomsFine));
                Log.Write(LogGroup.Space, $"HOLDING FINE - city {city.CustomName} ({city.Id}) paid {relieved} of the fine of holding {holding.Id} of {holding.OwnerPlayerId}");
            }

            foreach (var text in texts)
                HoldingRules.AddLog(holding, nowUtc, text);
            holding.LastCitySituationId = outcome.SituationId;
            DB.Set(holding);

            foreach (var text in texts)
                TellOwner(holding.OwnerPlayerId, $"{PlanetaryHoldings.GetHoldingName(holding)}: {text}");
            return texts.Count > 0;
        }

        private static void TellOwner(string playerId, string text)
        {
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (!GetIsDM(player) && GetObjectUUID(player) == playerId)
                {
                    SendMessageToPC(player, ColorToken.Green(text));
                    return;
                }
            }
        }
    }
}
