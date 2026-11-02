using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityGazetteService;
using SWLOR.Game.Server.Service.EdictService;

namespace SWLOR.Game.Server.Service.PropertyService
{
    /// <summary>
    /// The rules of a candidate's platform (Player City Revamp, "Election changes"): which Edicts a platform may hold,
    /// what it costs, the warning when the candidate's City Management rank would lower the city, and the words the
    /// election window uses to show a platform and the incumbent's record. Pure, so each rule can be tested without the
    /// database or the engine.
    /// </summary>
    public static class ElectionPlatformRules
    {
        /// <summary>How many recent Gazette decisions the incumbent's row shows.</summary>
        public const int RecentDecisionCount = 5;

        #region Choosing Edicts

        /// <summary>
        /// The first reason the Edicts cannot make up a platform for the city, or none. A platform holds at most the city's
        /// slot count, one of each Edict, each at or below the city's level, none that Wild Space stations lack, and a
        /// valid city for every Alliance and Embargo, never an Alliance and an Embargo against the same city.
        /// </summary>
        /// <param name="edicts">The Edicts, in the order they were chosen.</param>
        /// <param name="cityId">The city the candidate stands in.</param>
        /// <param name="cityLevel">The city's level.</param>
        /// <param name="isWildSpaceStation">Whether the city is a Wild Space station.</param>
        /// <param name="isValidTarget">Whether a named city exists and is not being removed.</param>
        public static (EdictRefusal Refusal, EdictType Type) GetRefusal(
            IEnumerable<ElectionPlatformEdict> edicts,
            string cityId,
            int cityLevel,
            bool isWildSpaceStation,
            Func<string, bool> isValidTarget)
        {
            var held = new Dictionary<EdictType, CityEdict>();
            foreach (var edict in edicts ?? Enumerable.Empty<ElectionPlatformEdict>())
            {
                if (edict == null)
                    return (EdictRefusal.UnknownEdict, EdictType.Invalid);

                var targetId = edict.TargetCityId ?? string.Empty;
                var refusal = EdictRules.GetEnactRefusal(
                    edict.Type,
                    cityId,
                    targetId,
                    !string.IsNullOrWhiteSpace(targetId) && isValidTarget(targetId),
                    cityLevel,
                    isWildSpaceStation,
                    CityWideCondition.None,
                    held);
                if (refusal != EdictRefusal.None)
                    return (refusal, edict.Type);

                held[edict.Type] = new CityEdict { TargetCityId = targetId };
            }

            return (EdictRefusal.None, EdictType.Invalid);
        }

        /// <summary>The player-facing reason an Edict cannot join the platform.</summary>
        public static string DescribeRefusal(EdictRefusal refusal, EdictType type, int cityLevel)
        {
            return refusal == EdictRefusal.NoFreeSlot
                ? $"A platform holds at most {EdictRules.GetSlots(cityLevel)} Edict(s) at this city level."
                : EdictRules.DescribeRefusal(refusal, type);
        }

        /// <summary>
        /// The Edicts as a platform stores them: in Edict order, one of each type, and a city only on the Edicts that name one.
        /// </summary>
        public static List<ElectionPlatformEdict> Normalize(IEnumerable<ElectionPlatformEdict> edicts)
        {
            var result = new List<ElectionPlatformEdict>();
            foreach (var edict in (edicts ?? Enumerable.Empty<ElectionPlatformEdict>())
                         .Where(x => x != null)
                         .OrderBy(x => (int)x.Type))
            {
                var namesTarget = EdictCatalog.Get(edict.Type)?.NamesTargetCity ?? false;
                result.Add(new ElectionPlatformEdict
                {
                    Type = edict.Type,
                    TargetCityId = namesTarget ? (edict.TargetCityId ?? string.Empty).Trim() : string.Empty
                });
            }

            return result;
        }

        /// <summary>What the platform adds to the city's weekly bill once every Edict is enacted and working.</summary>
        public static int GetWeeklyCost(IEnumerable<ElectionPlatformEdict> edicts)
        {
            return (edicts ?? Enumerable.Empty<ElectionPlatformEdict>()).Sum(x => EdictRules.GetWeeklyCost(x.Type));
        }

        /// <summary>
        /// The platform to enact for the winner, or null when they made none. A candidate with no entry on the election
        /// ran before platforms existed, so winning leaves the city's Edicts as they are.
        /// </summary>
        public static ElectionPlatform GetPlatformToEnact(IReadOnlyDictionary<string, ElectionPlatform> platforms, string winnerPlayerId)
        {
            if (platforms == null || string.IsNullOrWhiteSpace(winnerPlayerId))
                return null;

            return platforms.TryGetValue(winnerPlayerId, out var platform) ? platform : null;
        }

        #endregion

        #region Cost preview and warning

        /// <summary>
        /// The platform step's cost preview: the platform's weekly cost beside the city's upkeep now, the treasury (both
        /// balances), and the weekly citizenship tax.
        /// </summary>
        public static string BuildCostPreview(ElectionPlatformContext context, IReadOnlyCollection<ElectionPlatformEdict> edicts)
        {
            var platformCost = GetWeeklyCost(edicts);
            var upkeepNow = (long)context.BaseUpkeep + context.CurrentEdictUpkeep;
            var upkeepWith = (long)context.BaseUpkeep + platformCost;
            var taxIncome = (long)context.CitizenshipTax * context.CitizenCount;

            var text = new StringBuilder();
            text.Append($"Your platform: {platformCost:N0} cr a week for {edicts.Count} Edict(s).\n");
            text.Append($"Upkeep now: {upkeepNow:N0} cr a week ({context.BaseUpkeep:N0} base and facilities, {context.CurrentEdictUpkeep:N0} Edicts).\n");
            text.Append($"Upkeep with your platform: {upkeepWith:N0} cr a week.\n");
            text.Append($"Treasury: {context.Treasury:N0} cr");
            if (context.RestrictedTreasury > 0)
                text.Append($", plus {context.RestrictedTreasury:N0} cr that cannot be withdrawn");

            text.Append(".\n");
            text.Append($"Citizenship tax: {context.CitizenshipTax:N0} cr from {context.CitizenCount:N0} citizens is {taxIncome:N0} cr a week when everyone pays. Transportation and tariff income depends on traffic.");

            if (edicts.Any(x => x.Type == EdictType.Alliance))
                text.Append("\nAn Alliance costs nothing until the other city also enacts one.");
            if (edicts.Count > 0)
                text.Append("\nWinning adds the first part-week of each Edict to the upkeep bill.");

            return text.ToString();
        }

        /// <summary>
        /// The warning when the candidate's City Management rank would lower the city level, and with it the Edict slots,
        /// at the next weekly upkeep check. Empty when the city would keep the level the citizen count gives it anyway.
        /// </summary>
        /// <param name="context">What the platform step knows about the city and the candidate.</param>
        /// <param name="platformEdictCount">The Edicts in the platform now.</param>
        /// <param name="levelName">A city level's name, such as Township.</param>
        public static string BuildCityLevelWarning(ElectionPlatformContext context, int platformEdictCount, Func<int, string> levelName)
        {
            if (context.CandidateSupportedLevel >= context.BaselineSupportedLevel)
                return string.Empty;

            var slotsNow = EdictRules.GetSlots(context.CityLevel);
            var slotsAfter = EdictRules.GetSlots(context.CandidateSupportedLevel);
            var text = new StringBuilder();
            text.Append($"Warning: your City Management rank supports city level {context.CandidateMayorLevel} ({levelName(context.CandidateMayorLevel)}). ");
            text.Append($"If you win, the weekly upkeep check lowers the city from level {context.CityLevel} ({levelName(context.CityLevel)}) ");
            text.Append($"to level {context.CandidateSupportedLevel} ({levelName(context.CandidateSupportedLevel)})");
            text.Append(slotsAfter < slotsNow
                ? $" and its Edict slots from {slotsNow} to {slotsAfter}."
                : ".");
            text.Append(" Edicts the lower level cannot hold pause until the level returns; a paused Edict is not a broken promise.");
            if (platformEdictCount > slotsAfter)
                text.Append($" Your platform has {platformEdictCount} Edicts, and only {slotsAfter} slot(s) would remain.");

            return text.ToString();
        }

        #endregion

        #region Election window text

        /// <summary>The Edicts of a platform as lines, with the city an Alliance or Embargo names and each weekly cost.</summary>
        public static string BuildEdictLines(IEnumerable<ElectionPlatformEdict> edicts, Func<string, string> getCityName)
        {
            var lines = (edicts ?? Enumerable.Empty<ElectionPlatformEdict>())
                .Select(x => $"- {DescribeEdict(x, getCityName)} - {EdictRules.GetWeeklyCost(x.Type):N0} cr a week")
                .ToList();
            return lines.Count == 0 ? "None" : string.Join("\n", lines);
        }

        /// <summary>An Edict's name, with the city it names.</summary>
        public static string DescribeEdict(ElectionPlatformEdict edict, Func<string, string> getCityName)
        {
            var name = EdictRules.GetName(edict.Type);
            if (string.IsNullOrWhiteSpace(edict.TargetCityId))
                return name;

            var cityName = getCityName(edict.TargetCityId);
            return $"{name} naming {(string.IsNullOrWhiteSpace(cityName) ? "a city that is gone" : cityName)}";
        }

        /// <summary>One row of the candidate list: the name, whether they hold the seat, and what their platform costs.</summary>
        public static string BuildCandidateRowText(string name, bool isIncumbent, ElectionPlatform platform)
        {
            var label = isIncumbent ? $"{name} (incumbent)" : name;
            if (platform == null)
                return $"{label} - no platform";

            var count = platform.Edicts.Count;
            return count == 0
                ? $"{label} - no Edicts"
                : $"{label} - {count} Edict(s), {GetWeeklyCost(platform.Edicts):N0} cr a week";
        }

        /// <summary>
        /// What the window shows for the selected candidate: their Edicts, the weekly cost, the statement, and for the
        /// incumbent their record and recent decisions.
        /// </summary>
        /// <param name="platform">The candidate's platform, or null if they made none.</param>
        /// <param name="getCityName">A city's name by id, or empty when it is gone.</param>
        /// <param name="noStatementText">What to show when there is no statement.</param>
        /// <param name="incumbentSection">The incumbent's record and decisions, or empty for a challenger.</param>
        public static string BuildCandidateDetails(
            ElectionPlatform platform,
            Func<string, string> getCityName,
            string noStatementText,
            string incumbentSection)
        {
            var text = new StringBuilder();
            if (platform == null)
            {
                text.Append("This candidate made no platform.");
            }
            else
            {
                text.Append("Edicts promised:\n");
                text.Append(BuildEdictLines(platform.Edicts, getCityName));
                text.Append($"\nWeekly cost: {GetWeeklyCost(platform.Edicts):N0} cr\n\nStatement:\n");
                text.Append(string.IsNullOrWhiteSpace(platform.Statement) ? noStatementText : platform.Statement);
            }

            if (!string.IsNullOrWhiteSpace(incumbentSection))
                text.Append("\n\n").Append(incumbentSection);

            return text.ToString();
        }

        /// <summary>The incumbent's record, such as "Promises kept: 2 of 3".</summary>
        public static string BuildRecordText(MayorRecord record)
        {
            if (record == null || record.Promised <= 0)
                return "Promises kept: no platform promises on record";

            var kept = Math.Max(0, record.Promised - record.Broken);
            return $"Promises kept: {kept} of {record.Promised}";
        }

        /// <summary>The newest Gazette decisions and Edict changes, public ones only, newest first.</summary>
        public static List<CityGazetteEntry> SelectRecentDecisions(IEnumerable<CityGazetteEntry> entries, int count = RecentDecisionCount)
        {
            return (entries ?? Enumerable.Empty<CityGazetteEntry>())
                .Where(x => x != null &&
                            !x.IsSecret &&
                            (x.EntryType == CityGazetteEntryType.Decision || x.EntryType == CityGazetteEntryType.Edict))
                .OrderByDescending(x => x.TimeUtc)
                .Take(count)
                .ToList();
        }

        /// <summary>The incumbent's part of the candidate details: their record and recent decisions.</summary>
        public static string BuildIncumbentSection(MayorRecord record, IReadOnlyCollection<CityGazetteEntry> decisions)
        {
            var text = new StringBuilder(BuildRecordText(record));
            text.Append("\nRecent decisions:");
            if (decisions.Count == 0)
            {
                text.Append(" none on the Gazette");
            }
            else
            {
                foreach (var entry in decisions)
                {
                    text.Append($"\n- {entry.TimeUtc:yyyy-MM-dd}: {entry.Text}");
                }
            }

            return text.ToString();
        }

        #endregion
    }
}
