using System.Text;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>
    /// A city's story request for the DMs (Player City Revamp, "Story hooks"): who may post one, how long it can be, what
    /// the DM feed shows, and how a DM changes its status. Pure, so each rule can be tested.
    /// </summary>
    public static class StoryRequestRules
    {
        /// <summary>The longest request, matching the HoloNet message limit.</summary>
        public const int MaxLength = 600;

        /// <summary>
        /// Trims a request, drops control characters other than line breaks, and cuts it to <see cref="MaxLength"/>
        /// characters.
        /// </summary>
        public static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var cleaned = new StringBuilder();
            foreach (var character in text.Trim())
            {
                if (character == '\r')
                    continue;

                if (char.IsControl(character) && character != '\n')
                    continue;

                cleaned.Append(character);
            }

            var result = cleaned.ToString().Trim();
            return result.Length <= MaxLength
                ? result
                : result.Substring(0, MaxLength).TrimEnd();
        }

        /// <summary>Why the player may not post a request, or an empty string when they may.</summary>
        /// <param name="role">The poster's role. The mayor, an Acting Mayor, and an officer may post.</param>
        /// <param name="text">The request as typed.</param>
        public static string CheckPost(CityRole role, string text)
        {
            if (!CityPowerRules.RoleHoldsPower(role, CityPower.PostRequests))
                return "Only the mayor, an Acting Mayor, or an officer can post a story request.";

            return Normalize(text).Length == 0
                ? "Write what you want the DMs to know."
                : string.Empty;
        }

        /// <summary>A request waiting for a DM.</summary>
        public static bool IsOpen(CityStoryRequest request)
        {
            return request != null && request.Status == CityStoryRequestStatus.Open;
        }

        /// <summary>A DM can take a request only while it is open.</summary>
        public static bool CanTake(CityStoryRequest request)
        {
            return IsOpen(request);
        }

        /// <summary>A DM can mark a request done while it is open or taken.</summary>
        public static bool CanFinish(CityStoryRequest request)
        {
            return request != null &&
                   (request.Status == CityStoryRequestStatus.Open || request.Status == CityStoryRequestStatus.Taken);
        }

        public static string DescribeStatus(CityStoryRequestStatus status)
        {
            switch (status)
            {
                case CityStoryRequestStatus.Open:
                    return "Open";
                case CityStoryRequestStatus.Taken:
                    return "Taken";
                case CityStoryRequestStatus.Done:
                    return "Done";
                default:
                    return "Unknown";
            }
        }

        /// <summary>
        /// The DM feed description for a posted request, in the layout of the story hooks. The feed is staff only, so it names
        /// the poster's character. Every argument must already be safe for Discord.
        /// </summary>
        public static string BuildFeedDescription(
            string cityName,
            string levelName,
            int level,
            string posterName,
            string posterTitle,
            string text)
        {
            var description = new StringBuilder();
            description.Append($"**City**: {cityName}\n");
            description.Append($"**City Level**: {levelName} (Lvl. {level})\n");

            if (!string.IsNullOrWhiteSpace(posterName))
            {
                var title = string.IsNullOrWhiteSpace(posterTitle) ? string.Empty : $" ({posterTitle})";
                description.Append($"**Posted by**: {posterName}{title}\n");
            }
            else if (!string.IsNullOrWhiteSpace(posterTitle))
            {
                description.Append($"**Posted by**: {posterTitle}\n");
            }

            description.Append('\n').Append(text);
            return description.ToString();
        }
    }
}
