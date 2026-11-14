using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.TravelerService
{
    /// <summary>
    /// What players read about the travelers: the Gazette lines and the login notice. Plain ASCII, and pure, so the wording
    /// can be tested.
    /// </summary>
    public static class TravelerText
    {
        /// <summary>The title with "a" or "an" in front of it.</summary>
        public static string WithArticle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return string.Empty;

            var vowel = "aeiouAEIOU".IndexOf(title.Trim()[0]) >= 0;
            return (vowel ? "an " : "a ") + title.Trim();
        }

        /// <summary>The Gazette line for a traveler's arrival in a city.</summary>
        public static string BuildArrival(TravelerDetail traveler, string cityName)
        {
            return $"{traveler.Name}, {WithArticle(traveler.Title)}, has arrived at the Traveler's Lodge in {NameOf(cityName)} for the week.";
        }

        /// <summary>The Gazette line for a traveler that left a city because the Lodge was taken down.</summary>
        public static string BuildLodgeLost(TravelerDetail traveler)
        {
            return $"{traveler.Name}, {WithArticle(traveler.Title)}, has left the city now that its Traveler's Lodge is gone, and waits at {HubName(string.Empty)} until next week.";
        }

        /// <summary>Where a traveler is, in one line.</summary>
        public static string DescribeLocation(TravelerLocation location)
        {
            var traveler = TravelerCatalog.Get(location.Type);
            if (traveler == null)
                return string.Empty;

            var where = string.IsNullOrWhiteSpace(location.CityName)
                ? $"waiting at {HubName(location.AreaName)}"
                : string.IsNullOrWhiteSpace(location.AreaName)
                    ? $"visiting {location.CityName}"
                    : $"visiting {location.CityName} ({location.AreaName})";

            return $"{traveler.Name}, {traveler.Title}: {where}";
        }

        /// <summary>The login notice listing where every traveler is this week, or an empty string when there are none.</summary>
        public static string BuildLoginNotice(IEnumerable<TravelerLocation> locations)
        {
            var lines = (locations ?? Enumerable.Empty<TravelerLocation>())
                .Select(DescribeLocation)
                .Where(line => !string.IsNullOrEmpty(line))
                .ToList();

            return lines.Count == 0 ? string.Empty : "Travelers this week:\n" + string.Join("\n", lines);
        }

        /// <summary>What the information broker says about where the other travelers are.</summary>
        public static string BuildOthersNews(TravelerType asking, IEnumerable<TravelerLocation> locations)
        {
            var lines = (locations ?? Enumerable.Empty<TravelerLocation>())
                .Where(location => location.Type != asking)
                .Select(DescribeLocation)
                .Where(line => !string.IsNullOrEmpty(line))
                .ToList();

            return lines.Count == 0
                ? "I have not heard from the other travelers this week."
                : "The other travelers, as I hear it:\n" + string.Join("\n", lines);
        }

        private static string HubName(string areaName)
        {
            return string.IsNullOrWhiteSpace(areaName) ? TravelerRules.HubName : areaName.Trim();
        }

        private static string NameOf(string cityName)
        {
            return string.IsNullOrWhiteSpace(cityName) ? "the city" : cityName.Trim();
        }
    }
}
