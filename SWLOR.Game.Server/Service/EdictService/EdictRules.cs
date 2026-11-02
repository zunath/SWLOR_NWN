using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityGazetteService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service.EdictService
{
    /// <summary>
    /// The rules of a city's Edicts (Player City Revamp, "Edict rules"): slots, locks, what is in effect, what is billed,
    /// what an enactment costs, and which Edicts pause when the city's level drops. Pure, so every rule can be tested
    /// without the database or the engine.
    /// </summary>
    public static class EdictRules
    {
        /// <summary>Days an Edict stays locked after it takes effect.</summary>
        public const int LockDays = 7;

        /// <summary>Days of citizenship before a citizen gets Edict benefits.</summary>
        public const int CitizenDelayDays = 7;

        private const int DaysPerBillingWeek = 7;

        #region Slots and levels

        /// <summary>Edict slots: 1 at levels 1 and 2, 2 at levels 3 and 4, 3 at level 5.</summary>
        public static int GetSlots(int cityLevel)
        {
            if (cityLevel >= 5)
                return 3;

            return cityLevel >= 3 ? 2 : 1;
        }

        public static bool MeetsMinimumLevel(EdictType type, int cityLevel)
        {
            var detail = EdictCatalog.Get(type);
            return detail != null && cityLevel >= detail.MinimumCityLevel;
        }

        /// <summary>Whether a city can hold the Edict at all. Planet-based Edicts are hidden for Wild Space stations.</summary>
        public static bool IsAvailableTo(EdictType type, bool isWildSpaceStation)
        {
            var detail = EdictCatalog.Get(type);
            return detail != null && !(detail.IsPlanetBased && isWildSpaceStation);
        }

        /// <summary>The Edicts a city sees in its Edicts tab, in type order.</summary>
        public static IReadOnlyList<EdictDetail> GetOfferedEdicts(bool isWildSpaceStation)
        {
            return EdictCatalog.Ordered.Where(x => IsAvailableTo(x.Type, isWildSpaceStation)).ToList();
        }

        /// <summary>
        /// The Edicts to pause because of the city's level: first those whose minimum level is now too high, then the
        /// newest of the rest above the slot count. A held Edict that is not in the catalog is paused too.
        /// </summary>
        public static HashSet<EdictType> GetCityLevelPauses(IReadOnlyDictionary<EdictType, CityEdict> edicts, int cityLevel)
        {
            var paused = new HashSet<EdictType>();
            var remaining = new List<KeyValuePair<EdictType, CityEdict>>();
            foreach (var pair in edicts)
            {
                if (MeetsMinimumLevel(pair.Key, cityLevel))
                    remaining.Add(pair);
                else
                    paused.Add(pair.Key);
            }

            var excess = remaining.Count - GetSlots(cityLevel);
            if (excess > 0)
            {
                foreach (var pair in remaining
                             .OrderByDescending(x => x.Value.EnactedUtc)
                             .ThenByDescending(x => (int)x.Key)
                             .Take(excess))
                {
                    paused.Add(pair.Key);
                }
            }

            return paused;
        }

        #endregion

        #region Conditions and pauses

        /// <summary>
        /// The city-wide conditions that hold. Disrepair is a bill still unpaid at a weekly check: the check that finds
        /// one stamps a destruction date, and paying the bill clears it.
        /// </summary>
        public static CityWideCondition GetConditions(
            int upkeepOwed,
            bool hasDisrepairDate,
            DateTime? noConfidenceSinceUtc,
            DateTime? neglectSinceUtc)
        {
            var conditions = CityWideCondition.None;
            if (upkeepOwed > 0 && hasDisrepairDate)
                conditions |= CityWideCondition.Disrepair;
            if (noConfidenceSinceUtc.HasValue)
                conditions |= CityWideCondition.NoConfidence;
            if (neglectSinceUtc.HasValue)
                conditions |= CityWideCondition.Neglect;

            return conditions;
        }

        public static bool IsPausedBySituation(CityEdict edict, DateTime nowUtc)
        {
            return edict.SituationPauseEndsUtc.HasValue && nowUtc < edict.SituationPauseEndsUtc.Value;
        }

        /// <summary>
        /// Whether the Edict works: it is enacted, and neither a city-wide condition nor any single-Edict reason covers it.
        /// A pending Alliance does not work.
        /// </summary>
        public static bool IsInEffect(CityEdict edict, CityWideCondition conditions, DateTime nowUtc)
        {
            return IsBilled(edict, conditions) && !IsPausedBySituation(edict, nowUtc);
        }

        /// <summary>
        /// Whether the weekly bill includes the Edict: it is in effect, or paused only by a situation outcome, which is
        /// still billed because the pause is the outcome.
        /// </summary>
        public static bool IsBilled(CityEdict edict, CityWideCondition conditions)
        {
            return edict.State == EdictState.Active &&
                   conditions == CityWideCondition.None &&
                   !edict.IsPausedByCityLevel;
        }

        public static HashSet<EdictType> GetBilledTypes(IReadOnlyDictionary<EdictType, CityEdict> edicts, CityWideCondition conditions)
        {
            return edicts.Where(x => IsBilled(x.Value, conditions)).Select(x => x.Key).ToHashSet();
        }

        /// <summary>The weekly check's Edict charges, one line for each billed Edict, in type order.</summary>
        public static IReadOnlyList<EdictBillLine> GetBillLines(IReadOnlyDictionary<EdictType, CityEdict> edicts, CityWideCondition conditions)
        {
            return edicts
                .Where(x => IsBilled(x.Value, conditions))
                .OrderBy(x => (int)x.Key)
                .Select(x => new EdictBillLine(x.Key, GetName(x.Key), GetWeeklyCost(x.Key)))
                .ToList();
        }

        /// <summary>The held Edicts the weekly check does not bill, each with its reason, in type order.</summary>
        public static IReadOnlyList<string> GetUnbilledDescriptions(
            IReadOnlyDictionary<EdictType, CityEdict> edicts,
            CityWideCondition conditions,
            DateTime nowUtc)
        {
            return edicts
                .Where(x => !IsBilled(x.Value, conditions))
                .OrderBy(x => (int)x.Key)
                .Select(x => $"{GetName(x.Key)} ({DescribePause(x.Value, conditions, nowUtc)})")
                .ToList();
        }

        /// <summary>Why a held Edict is not working, or an empty string when it is.</summary>
        public static string DescribePause(CityEdict edict, CityWideCondition conditions, DateTime nowUtc, bool pausedBySituationModifier = false)
        {
            if (edict.State == EdictState.Pending)
                return "waiting for the other city";
            if (conditions != CityWideCondition.None)
                return "city " + DescribeConditions(conditions);
            if (edict.IsPausedByCityLevel)
                return "city level too low";
            if (pausedBySituationModifier || IsPausedBySituation(edict, nowUtc))
                return "a situation outcome";

            return string.Empty;
        }

        public static string DescribeConditions(CityWideCondition conditions)
        {
            var parts = new List<string>();
            if (conditions.HasFlag(CityWideCondition.Disrepair))
                parts.Add("in disrepair");
            if (conditions.HasFlag(CityWideCondition.NoConfidence))
                parts.Add("under No confidence");
            if (conditions.HasFlag(CityWideCondition.Neglect))
                parts.Add("under Neglect");

            return string.Join(" and ", parts);
        }

        public static int GetWeeklyCost(EdictType type)
        {
            return EdictCatalog.Get(type)?.WeeklyCost ?? 0;
        }

        public static string GetName(EdictType type)
        {
            return EdictCatalog.Get(type)?.Name ?? type.ToString();
        }

        #endregion

        #region Cost

        /// <summary>
        /// What enacting an Edict mid-term (or resuming one between weekly checks) pays at once: its weekly cost times the
        /// days left until the next check, over 7, rounded down. A check that is already overdue counts as no days, and
        /// one more than a week away counts as 7.
        /// </summary>
        public static int GetProration(int weeklyCost, DateTime nowUtc, DateTime nextCheckUtc)
        {
            if (weeklyCost <= 0)
                return 0;

            var week = TimeSpan.FromDays(DaysPerBillingWeek);
            var remaining = nextCheckUtc - nowUtc;
            if (remaining <= TimeSpan.Zero)
                return 0;
            if (remaining > week)
                remaining = week;

            return (int)Math.Floor((decimal)weeklyCost * remaining.Ticks / week.Ticks);
        }

        /// <summary>
        /// What enacting costs right now. An Alliance costs nothing until the other city has one waiting that names this
        /// city, so the pair takes effect together.
        /// </summary>
        public static int GetEnactCharge(EdictType type, bool partnerAllianceWaiting, DateTime nowUtc, DateTime nextCheckUtc)
        {
            if (type == EdictType.Alliance && !partnerAllianceWaiting)
                return 0;

            return GetProration(GetWeeklyCost(type), nowUtc, nextCheckUtc);
        }

        #endregion

        #region Enacting and repealing

        public static DateTime GetLockEndUtc(DateTime takesEffectUtc)
        {
            return takesEffectUtc.AddDays(LockDays);
        }

        /// <summary>
        /// The mayor can repeal only after the lock ends. A pending Alliance has no lock, since its lock starts when both
        /// cities enact.
        /// </summary>
        public static bool CanRepeal(CityEdict edict, DateTime nowUtc)
        {
            return !edict.LockEndsUtc.HasValue || nowUtc >= edict.LockEndsUtc.Value;
        }

        /// <summary>An Alliance and an Embargo cannot name the same city.</summary>
        public static bool IsExclusivePair(EdictType type, string targetCityId, IReadOnlyDictionary<EdictType, CityEdict> held)
        {
            EdictType opposite;
            switch (type)
            {
                case EdictType.Alliance:
                    opposite = EdictType.Embargo;
                    break;
                case EdictType.Embargo:
                    opposite = EdictType.Alliance;
                    break;
                default:
                    return false;
            }

            return !string.IsNullOrWhiteSpace(targetCityId) &&
                   held.TryGetValue(opposite, out var other) &&
                   other.TargetCityId == targetCityId;
        }

        /// <summary>
        /// The first reason a mayor cannot enact the Edict, or none. The funds check comes after, since an Alliance's cost
        /// depends on the other city.
        /// </summary>
        /// <param name="type">The Edict.</param>
        /// <param name="cityId">The enacting city.</param>
        /// <param name="targetCityId">The city an Alliance or Embargo names.</param>
        /// <param name="targetIsValid">Whether the named city exists and is not being removed.</param>
        /// <param name="cityLevel">The enacting city's level.</param>
        /// <param name="isWildSpaceStation">Whether the enacting city is a Wild Space station.</param>
        /// <param name="conditions">The enacting city's city-wide conditions.</param>
        /// <param name="held">The Edicts the city already holds, whatever their state.</param>
        public static EdictRefusal GetEnactRefusal(
            EdictType type,
            string cityId,
            string targetCityId,
            bool targetIsValid,
            int cityLevel,
            bool isWildSpaceStation,
            CityWideCondition conditions,
            IReadOnlyDictionary<EdictType, CityEdict> held)
        {
            var detail = EdictCatalog.Get(type);
            if (detail == null)
                return EdictRefusal.UnknownEdict;
            if (!IsAvailableTo(type, isWildSpaceStation))
                return EdictRefusal.UnavailableHere;
            if (cityLevel < detail.MinimumCityLevel)
                return EdictRefusal.LevelTooLow;
            if (held.ContainsKey(type))
                return EdictRefusal.AlreadyHeld;
            if (conditions != CityWideCondition.None)
                return EdictRefusal.CityWideCondition;
            if (held.Count >= GetSlots(cityLevel))
                return EdictRefusal.NoFreeSlot;

            if (detail.NamesTargetCity)
            {
                if (string.IsNullOrWhiteSpace(targetCityId))
                    return EdictRefusal.TargetRequired;
                if (targetCityId == cityId || !targetIsValid)
                    return EdictRefusal.InvalidTarget;
                if (IsExclusivePair(type, targetCityId, held))
                    return EdictRefusal.ExclusivePair;
            }

            return EdictRefusal.None;
        }

        /// <summary>The player-facing reason for a refusal.</summary>
        public static string DescribeRefusal(EdictRefusal refusal, EdictType type, CityWideCondition conditions = CityWideCondition.None)
        {
            var detail = EdictCatalog.Get(type);
            var name = detail?.Name ?? "That Edict";

            switch (refusal)
            {
                case EdictRefusal.None:
                    return string.Empty;
                case EdictRefusal.UnknownEdict:
                    return "Choose an Edict first.";
                case EdictRefusal.CityUnavailable:
                    return "This city is no longer available.";
                case EdictRefusal.UnavailableHere:
                    return $"{name} is not available to a station in Wild Space.";
                case EdictRefusal.LevelTooLow:
                    return $"{name} needs city level {detail?.MinimumCityLevel ?? 1}.";
                case EdictRefusal.AlreadyHeld:
                    return $"{name} is already enacted.";
                case EdictRefusal.CityWideCondition:
                    return $"No Edict can be enacted while the city is {DescribeConditions(conditions)}.";
                case EdictRefusal.NoFreeSlot:
                    return "There is no free Edict slot. Repeal an Edict first.";
                case EdictRefusal.TargetRequired:
                    return $"{name} must name a city.";
                case EdictRefusal.InvalidTarget:
                    return "That city cannot be named.";
                case EdictRefusal.ExclusivePair:
                    return "A city cannot hold an Alliance and an Embargo against the same city.";
                case EdictRefusal.InsufficientFunds:
                    return "The treasury cannot pay for the days left until the next upkeep check.";
                case EdictRefusal.NotHeld:
                    return $"{name} is not enacted.";
                case EdictRefusal.StillLocked:
                    return $"{name} is locked for {LockDays} days after it takes effect.";
                default:
                    return "That Edict cannot be changed right now.";
            }
        }

        /// <summary>The title an official acts under in an Edict's Gazette entry.</summary>
        public static string GetOfficialTitle(bool isMayor)
        {
            return isMayor ? CityGazetteRules.MayorTitle : CityGazetteRules.DelegateTitle;
        }

        #endregion

        #region Text

        /// <summary>A time left as days and hours, hours and minutes, or minutes.</summary>
        public static string FormatRemaining(TimeSpan remaining)
        {
            if (remaining.TotalDays >= 1)
                return $"{(int)remaining.TotalDays}d {remaining.Hours}h";
            if (remaining.TotalHours >= 1)
                return $"{(int)remaining.TotalHours}h {remaining.Minutes}m";

            return $"{Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes))}m";
        }

        /// <summary>What the Edicts tab says about the treasury, the conditions that stop enacting, and the proration.</summary>
        public static string BuildFundsText(long treasury, long restrictedTreasury, CityWideCondition conditions, DateTime nowUtc, DateTime nextCheckUtc)
        {
            var text = $"Treasury: {treasury:N0} cr";
            if (restrictedTreasury > 0)
                text += $", plus {restrictedTreasury:N0} cr that cannot be withdrawn, which upkeep and Edicts spend first";

            var daysLeft = Math.Clamp((nextCheckUtc - nowUtc).TotalDays, 0d, DaysPerBillingWeek);
            text += $". Enacting an Edict pays at once for the {daysLeft:0.0} days left until the next upkeep check.";

            if (conditions != CityWideCondition.None)
                text += $" No Edict can be enacted while the city is {DescribeConditions(conditions)}.";

            return text;
        }

        /// <summary>What the Edicts tab says about an Edict before it is enacted.</summary>
        public static string BuildOfferText(EdictDetail detail, int prorationNow)
        {
            var text = $"{detail.Name}: {detail.Description}\nCosts {detail.WeeklyCost:N0} cr a week. Needs city level {detail.MinimumCityLevel}.";
            text += detail.Type == EdictType.Alliance
                ? " An Alliance costs nothing until the other city also enacts one naming this city."
                : $" Enacting now pays {prorationNow:N0} cr.";

            return text;
        }

        /// <summary>
        /// A held Edict as a row of the Edicts tab: its name, the city it names, its weekly cost, then its state, lock and origin.
        /// </summary>
        /// <param name="type">The Edict.</param>
        /// <param name="edict">The held Edict.</param>
        /// <param name="targetCityName">The named city's name, or null when that city is gone.</param>
        /// <param name="conditions">The city-wide conditions.</param>
        /// <param name="nowUtc">The time.</param>
        /// <param name="pausedBySituationModifier">True when a city modifier from a situation pauses the Edict.</param>
        public static string BuildHeldText(
            EdictType type,
            CityEdict edict,
            string targetCityName,
            CityWideCondition conditions,
            DateTime nowUtc,
            bool pausedBySituationModifier = false)
        {
            var title = GetName(type);
            if (!string.IsNullOrWhiteSpace(edict.TargetCityId))
                title += $" ({(string.IsNullOrWhiteSpace(targetCityName) ? "a city that is gone" : targetCityName)})";
            title += $" - {GetWeeklyCost(type):N0} cr a week";

            var parts = new List<string>();
            if (edict.State == EdictState.Pending)
            {
                parts.Add("Pending: costs nothing until that city's mayor enacts an Alliance naming this city");
            }
            else
            {
                var pause = DescribePause(edict, conditions, nowUtc, pausedBySituationModifier);
                parts.Add(pause.Length == 0 ? "In effect" : "Paused: " + pause);
                parts.Add(CanRepeal(edict, nowUtc)
                    ? "can be repealed"
                    : $"locked for {FormatRemaining(edict.LockEndsUtc.Value - nowUtc)}");
            }

            if (edict.FromPlatform)
                parts.Add("from the mayor's platform");

            return title + "\n" + string.Join(" - ", parts);
        }

        #endregion

        #region Gazette text

        /// <summary>The Gazette line for an enactment. A pending Alliance says what it is waiting for.</summary>
        public static string BuildEnactedText(EdictType type, string targetCityName, bool takesEffect)
        {
            var text = $"The {GetName(type)} Edict was enacted{DescribeTarget(targetCityName, ", naming ")}.";
            return takesEffect ? text : text + " It takes effect when that city's mayor enacts an Alliance naming this city.";
        }

        public static string BuildRepealedText(EdictType type, string targetCityName)
        {
            return $"The {GetName(type)} Edict{DescribeTarget(targetCityName, " naming ")} was repealed.";
        }

        public static string BuildAllianceFormedText(string otherCityName)
        {
            return $"The Alliance with {NameOrUnknown(otherCityName)} took effect.";
        }

        public static string BuildAllianceEndedText(string otherCityName, string reason)
        {
            return $"The Alliance with {NameOrUnknown(otherCityName)} ended because that city {reason}.";
        }

        private static string DescribeTarget(string targetCityName, string lead)
        {
            return string.IsNullOrWhiteSpace(targetCityName) ? string.Empty : lead + targetCityName;
        }

        private static string NameOrUnknown(string cityName)
        {
            return string.IsNullOrWhiteSpace(cityName) ? "another city" : cityName;
        }

        #endregion

        #region Citizens

        /// <summary>
        /// Whether a character gets its own city's citizen Edicts: a citizen who owes no taxes and has been one for at
        /// least 7 days. A citizen with no citizen-since time never qualifies.
        /// </summary>
        public static bool IsEligibleCitizen(
            string citizenPropertyId,
            string cityId,
            bool isDeleted,
            int owedTaxes,
            DateTime? citizenSinceUtc,
            DateTime nowUtc)
        {
            return ElectionRules.IsCitizenOf(citizenPropertyId, cityId, isDeleted) &&
                   owedTaxes <= 0 &&
                   citizenSinceUtc.HasValue &&
                   nowUtc >= citizenSinceUtc.Value.AddDays(CitizenDelayDays);
        }

        #endregion
    }
}
