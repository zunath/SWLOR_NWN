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

        /// <summary>The city drives the raiders off the holding the situation is about. Returns false when nothing was left to do.</summary>
        public static bool SendHelp(WorldProperty city, CitySituation situation, DateTime nowUtc)
        {
            var holding = string.IsNullOrWhiteSpace(situation.SubjectId) ? null : DB.Get<PlanetaryHolding>(situation.SubjectId);
            if (holding == null || !holding.RaidPending)
                return false;

            holding.RaidPending = false;
            var text = HoldingSituationRules.BuildHelpLog(city.CustomName);
            HoldingRules.AddLog(holding, nowUtc, text);
            DB.Set(holding);
            TellOwner(holding.OwnerPlayerId, $"{PlanetaryHoldings.GetHoldingName(holding)}: {text}");
            Log.Write(LogGroup.Space, $"HOLDING HELP - city {city.CustomName} ({city.Id}) drove the raiders off holding {holding.Id} of {holding.OwnerPlayerId}");
            return true;
        }

        /// <summary>The treasury pays off <paramref name="credits"/> of the fine of the holding the situation is about.</summary>
        public static int PayFine(WorldProperty city, CitySituation situation, int credits, DateTime nowUtc)
        {
            var holding = string.IsNullOrWhiteSpace(situation.SubjectId) ? null : DB.Get<PlanetaryHolding>(situation.SubjectId);
            if (holding == null || credits <= 0)
                return 0;

            var relieved = HoldingSituationRules.ApplyFineRelief(holding, credits);
            if (relieved <= 0)
                return 0;

            var text = HoldingSituationRules.BuildFineLog(city.CustomName, relieved, holding.CustomsFine);
            HoldingRules.AddLog(holding, nowUtc, text);
            DB.Set(holding);
            TellOwner(holding.OwnerPlayerId, $"{PlanetaryHoldings.GetHoldingName(holding)}: {text}");
            Log.Write(LogGroup.Space, $"HOLDING FINE - city {city.CustomName} ({city.Id}) paid {relieved} of the fine of holding {holding.Id} of {holding.OwnerPlayerId}");
            return relieved;
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
