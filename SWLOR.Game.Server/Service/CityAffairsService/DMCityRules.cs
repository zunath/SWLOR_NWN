using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>
    /// What the DM city window shows (Player City Revamp, "Story hooks"): which citizens count as lived-in, which cities a
    /// planet filter keeps, how the list is ordered, and how each line reads. Pure, so each rule can be tested.
    /// </summary>
    public static class DMCityRules
    {
        /// <summary>A citizen counts as active when their last login falls within this many days.</summary>
        public const int ActiveWindowDays = 7;

        /// <summary>How many of a city's newest Gazette entries the window lists.</summary>
        public const int RecentGazetteEntries = 6;

        /// <summary>The citizens whose last login is within <see cref="ActiveWindowDays"/> days of now.</summary>
        public static int CountActiveCitizens(IEnumerable<DateTime?> lastLogins, DateTime nowUtc)
        {
            var cutoff = nowUtc.AddDays(-ActiveWindowDays);
            return (lastLogins ?? Enumerable.Empty<DateTime?>()).Count(login => login.HasValue && login.Value >= cutoff);
        }

        /// <summary>Whether a city is listed under the planet filter. No planet selected lists every city.</summary>
        public static bool MatchesPlanet(PlanetType filter, PlanetType councilPlanet)
        {
            return filter == PlanetType.Invalid || filter == councilPlanet;
        }

        /// <summary>
        /// Orders the list: cities with an open story request first, since those are waiting for a DM, then by planet and
        /// name.
        /// </summary>
        public static List<DMCitySummary> Sort(IEnumerable<DMCitySummary> cities)
        {
            return (cities ?? Enumerable.Empty<DMCitySummary>())
                .OrderByDescending(city => StoryRequestRules.IsOpen(city.StoryRequest))
                .ThenBy(city => city.PlanetName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(city => city.CityName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(city => city.CityId, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>The line a city has in the list.</summary>
        public static string BuildListLabel(DMCitySummary city)
        {
            var label = $"{city.CityName} ({city.PlanetName}) - {city.ActiveCitizenCount} of {city.CitizenCount} active";
            return StoryRequestRules.IsOpen(city.StoryRequest) ? $"{label} - REQUEST" : label;
        }

        /// <summary>The details of the selected city: people, Edicts, and recent Gazette entries.</summary>
        public static string BuildDetails(DMCitySummary city)
        {
            var text = new StringBuilder();
            text.AppendLine($"{city.CityName}, {city.PlanetName}{(city.IsStation ? " (station)" : string.Empty)}, level {city.Level}");
            text.AppendLine(city.MayorSteppedDown
                ? $"Mayor of record: {city.MayorName} (stepped down, no powers until the snap election ends)"
                : $"Mayor: {city.MayorName}");

            if (!string.IsNullOrWhiteSpace(city.ActingMayorName))
                text.AppendLine($"{(city.MayorSteppedDown ? "Interim holder" : "Acting Mayor")}: {city.ActingMayorName}");

            text.AppendLine(city.OfficerLines.Count == 0
                ? "Officers: none"
                : "Officers: " + string.Join("; ", city.OfficerLines));
            text.AppendLine($"Citizens: {city.CitizenCount}, {city.ActiveCitizenCount} logged in within {ActiveWindowDays} days");
            text.AppendLine(city.EdictNames.Count == 0
                ? "Active Edicts: none"
                : "Active Edicts: " + string.Join(", ", city.EdictNames));
            text.AppendLine();
            text.AppendLine("Recent Gazette entries:");
            if (city.GazetteLines.Count == 0)
                text.Append("  none");
            else
                text.Append(string.Join("\n", city.GazetteLines.Select(line => "  " + line)));

            return text.ToString();
        }

        /// <summary>One Gazette entry as a line: its date, text, and the official's title. Secret entries are marked, since DMs see them.</summary>
        public static string BuildGazetteLine(CityGazetteEntry entry)
        {
            var title = string.IsNullOrWhiteSpace(entry.OfficialTitle) ? string.Empty : $" ({entry.OfficialTitle})";
            var secret = entry.IsSecret ? "[secret] " : string.Empty;
            return $"{entry.TimeUtc:yyyy-MM-dd} {secret}{entry.Text}{title}";
        }

        /// <summary>The story request of the selected city, with who posted it and where it stands.</summary>
        public static string BuildRequestText(DMCitySummary city)
        {
            var request = city.StoryRequest;
            if (request == null)
                return "This city has no story request.";

            var poster = string.IsNullOrWhiteSpace(city.StoryPosterName)
                ? request.PosterTitle
                : $"{city.StoryPosterName} ({request.PosterTitle})";
            return $"{StoryRequestRules.DescribeStatus(request.Status)} - posted {request.PostedUtc:yyyy-MM-dd} UTC by {poster}\n\n{request.Text}";
        }

        /// <summary>The line an officer or mayor has in the details: their name, title, and whether they are online.</summary>
        public static string BuildPersonLine(string name, string title, bool isOnline)
        {
            var titleText = string.IsNullOrWhiteSpace(title) ? string.Empty : $" ({title})";
            return $"{name}{titleText}{(isOnline ? " [online]" : string.Empty)}";
        }

        /// <summary>The recipients of a council summons for a planet, as the summons feedback reports them.</summary>
        public static string BuildSummonsFeedback(string planetName, int cityCount, int recipientCount, int onlineCount)
        {
            if (cityCount == 0)
                return $"No cities are on {planetName}'s council.";

            return $"Council called for {planetName}: {cityCount} cities, {recipientCount} mayors and officers, {onlineCount} online now. " +
                   "The rest read it at their next login.";
        }
    }
}
