using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityGazetteService;

namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>
    /// The rules of a city's officers (Player City Revamp, "City Affairs"): how many there can be, who may appoint whom,
    /// what a title looks like, and who loses their office when the city shrinks. Pure, so each rule can be tested.
    /// </summary>
    public static class OfficerRules
    {
        public const int TitleMaxLength = 32;

        /// <summary>The title an officer gets when the mayor gives none.</summary>
        public const string DefaultTitle = "Officer";

        /// <summary>A city has 1 officer plus 1 per city level, so 2 to 6.</summary>
        public static int GetOfficerLimit(int cityLevel)
        {
            return 1 + Math.Clamp(cityLevel, 1, 5);
        }

        /// <summary>
        /// Cleans a title the mayor typed: no control characters or markup, single spaces, at most
        /// <see cref="TitleMaxLength"/> characters. An empty result is the default title.
        /// </summary>
        public static string NormalizeTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return DefaultTitle;

            var cleaned = new StringBuilder();
            var previousWasSpace = false;
            foreach (var character in title.Trim())
            {
                if (char.IsControl(character) || character == '<' || character == '>')
                    continue;

                if (char.IsWhiteSpace(character))
                {
                    if (previousWasSpace)
                        continue;

                    cleaned.Append(' ');
                    previousWasSpace = true;
                    continue;
                }

                cleaned.Append(character);
                previousWasSpace = false;
            }

            var result = cleaned.ToString().Trim();
            if (result.Length > TitleMaxLength)
                result = result.Substring(0, TitleMaxLength).TrimEnd();

            return result.Length == 0 ? DefaultTitle : result;
        }

        /// <summary>
        /// Whether the mayor may appoint the character. Returns the reason it is refused, or an empty string when it is allowed.
        /// </summary>
        /// <param name="appointerId">The player making the appointment.</param>
        /// <param name="mayorId">The city's mayor.</param>
        /// <param name="targetId">The player to appoint.</param>
        /// <param name="targetIsCitizen">The target is a citizen of this city and not deleted.</param>
        /// <param name="officerIds">The current officers.</param>
        /// <param name="cityLevel">The city's level.</param>
        public static string CheckAppointment(
            string appointerId,
            string mayorId,
            string targetId,
            bool targetIsCitizen,
            IReadOnlyCollection<string> officerIds,
            int cityLevel)
        {
            if (string.IsNullOrWhiteSpace(appointerId) || appointerId != mayorId)
                return "Only the mayor can appoint officers.";

            if (string.IsNullOrWhiteSpace(targetId))
                return "Pick a citizen to appoint.";

            if (targetId == mayorId)
                return "The mayor already handles City Affairs.";

            if (!targetIsCitizen)
                return "Only citizens of this city can be officers.";

            if (officerIds.Contains(targetId))
                return "That citizen is already an officer.";

            if (officerIds.Count >= GetOfficerLimit(cityLevel))
                return $"A level {cityLevel} city can have {GetOfficerLimit(cityLevel)} officers.";

            return string.Empty;
        }

        /// <summary>Whether the player may decide the city's situations: the mayor, or an officer who is a citizen.</summary>
        public static bool CanHandleCityAffairs(
            string playerId,
            string mayorId,
            bool isCitizen,
            IReadOnlyDictionary<string, CityOfficer> officers,
            bool holdsPermission)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return false;

            if (playerId == mayorId)
                return true;

            return isCitizen && holdsPermission && officers != null && officers.ContainsKey(playerId);
        }

        /// <summary>The title an official acts under: Mayor for the mayor, otherwise the officer's title.</summary>
        public static string GetTitle(string playerId, string mayorId, IReadOnlyDictionary<string, CityOfficer> officers)
        {
            if (playerId == mayorId)
                return CityGazetteRules.MayorTitle;

            return officers != null && officers.TryGetValue(playerId, out var officer) && !string.IsNullOrWhiteSpace(officer.Title)
                ? officer.Title
                : DefaultTitle;
        }

        /// <summary>
        /// The officers whose appointments end because the city dropped a level: the newest ones above the limit.
        /// </summary>
        public static List<string> SelectAppointmentsAboveLimit(IReadOnlyDictionary<string, CityOfficer> officers, int cityLevel)
        {
            if (officers == null)
                return new List<string>();

            var excess = officers.Count - GetOfficerLimit(cityLevel);
            if (excess <= 0)
                return new List<string>();

            return officers
                .OrderByDescending(pair => pair.Value.AppointedUtc)
                .ThenBy(pair => pair.Key, StringComparer.Ordinal)
                .Take(excess)
                .Select(pair => pair.Key)
                .ToList();
        }
    }
}
