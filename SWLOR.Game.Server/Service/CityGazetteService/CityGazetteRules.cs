using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service.CityGazetteService
{
    /// <summary>
    /// The City Gazette's rules, with no database or engine calls: which events become entries, how an entry reads,
    /// which entries a reader sees, and which old entries are dropped.
    /// </summary>
    public static class CityGazetteRules
    {
        /// <summary>A city keeps this many entries, the newest first.</summary>
        public const int MaxEntriesPerCity = 200;

        public const string MayorTitle = "Mayor";

        /// <summary>The title for someone other than the mayor who holds the permission for the action.</summary>
        public const string DelegateTitle = "Delegate";

        /// <summary>Whether a property broadcast also becomes a Gazette entry: every city and station, nothing else.</summary>
        public static bool ShouldMirrorBroadcast(WorldProperty property)
        {
            return property != null &&
                   property.PropertyType == PropertyType.City &&
                   !string.IsNullOrWhiteSpace(property.Id);
        }

        /// <summary>The entry type for a property broadcast, by its title. Titles it does not know are notices.</summary>
        public static CityGazetteEntryType GetEntryTypeForEvent(string title)
        {
            var name = title?.Trim() ?? string.Empty;

            if (name.StartsWith("City Election", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("New Mayor Elected", StringComparison.OrdinalIgnoreCase))
                return CityGazetteEntryType.Election;

            if (name.StartsWith("City Upkeep", StringComparison.OrdinalIgnoreCase))
                return CityGazetteEntryType.Upkeep;

            if (name.Equals("City Citizen Warning", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("City Expiration", StringComparison.OrdinalIgnoreCase))
                return CityGazetteEntryType.Citizens;

            if (name.Equals("New Player City Founded", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("New Player Station Founded", StringComparison.OrdinalIgnoreCase))
                return CityGazetteEntryType.Founding;

            return CityGazetteEntryType.Notice;
        }

        /// <summary>
        /// Removes Discord markdown (bold, italics, underline, strikethrough, spoilers, code) so a broadcast reads as plain
        /// text in the Gazette.
        /// </summary>
        public static string StripMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return text
                .Replace("*", string.Empty)
                .Replace("__", string.Empty)
                .Replace("~~", string.Empty)
                .Replace("||", string.Empty)
                .Replace("`", string.Empty);
        }

        /// <summary>
        /// The Gazette text for a property broadcast: its title, its first line, and the lines after it as facts in
        /// brackets. The Discord message itself does not change.
        /// </summary>
        public static string BuildBroadcastText(string title, string details)
        {
            var lines = StripMarkdown(details)
                .Split('\n')
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .ToList();
            var heading = StripMarkdown(title).Trim();

            if (lines.Count == 0)
                return heading;

            var text = heading.Length == 0 ? lines[0] : $"{heading}: {lines[0]}";
            if (lines.Count > 1)
                text += $" ({string.Join("; ", lines.Skip(1))})";

            return text;
        }

        /// <summary>The text of a tax entry from the values that changed, or an empty string when nothing did.</summary>
        public static string BuildTaxChangeText(IEnumerable<(string Name, string Before, string After)> changes)
        {
            var changed = (changes ?? Enumerable.Empty<(string Name, string Before, string After)>())
                .Where(change => change.Before != change.After)
                .Select(change => $"{change.Name} {change.Before} -> {change.After}")
                .ToList();

            return changed.Count == 0
                ? string.Empty
                : $"Taxes changed: {string.Join("; ", changed)}.";
        }

        /// <summary>The title an official acts under in a tax entry.</summary>
        public static string GetTaxOfficialTitle(bool isMayor) => isMayor ? MayorTitle : DelegateTitle;

        /// <summary>Whether a post has what an entry needs: a city and some text.</summary>
        public static bool IsPostable(string cityId, string text)
        {
            return !string.IsNullOrWhiteSpace(cityId) && !string.IsNullOrWhiteSpace(text);
        }

        public static CityGazetteEntry CreateEntry(
            string cityId,
            CityGazetteEntryType type,
            string text,
            string officialPlayerId,
            string officialTitle,
            bool isSecret,
            bool isHook,
            DateTime nowUtc)
        {
            return new CityGazetteEntry
            {
                CityId = cityId,
                TimeUtc = nowUtc,
                EntryType = type,
                Text = text.Trim(),
                OfficialPlayerId = officialPlayerId ?? string.Empty,
                OfficialTitle = officialTitle?.Trim() ?? string.Empty,
                IsSecret = isSecret,
                IsHook = isHook
            };
        }

        /// <summary>Whether an entry goes to the property Discord feed: every entry that is not secret.</summary>
        public static bool ShouldPostToFeed(CityGazetteEntry entry) => entry != null && !entry.IsSecret;

        /// <summary>Whether an entry goes to the DM feed as a story hook: every hook, secret or not, since the feed is staff only.</summary>
        public static bool ShouldPostToDMFeed(CityGazetteEntry entry) => entry != null && entry.IsHook;

        /// <summary>
        /// The entries a reader sees, newest first, at most <see cref="MaxEntriesPerCity"/>. Secret entries stay out
        /// unless <paramref name="includeSecret"/> is set.
        /// </summary>
        public static List<CityGazetteEntry> SelectVisibleEntries(IEnumerable<CityGazetteEntry> entries, bool includeSecret)
        {
            return OrderNewestFirst(entries)
                .Where(entry => includeSecret || !entry.IsSecret)
                .Take(MaxEntriesPerCity)
                .ToList();
        }

        /// <summary>The entries to delete so the city keeps only its newest <paramref name="keep"/>.</summary>
        public static List<CityGazetteEntry> SelectEntriesToPrune(IEnumerable<CityGazetteEntry> entries, int keep = MaxEntriesPerCity)
        {
            return OrderNewestFirst(entries)
                .Skip(Math.Max(0, keep))
                .ToList();
        }

        public static string GetFeedTitle(CityGazetteEntryType type)
        {
            switch (type)
            {
                case CityGazetteEntryType.Election: return "City Gazette: Election";
                case CityGazetteEntryType.Upkeep: return "City Gazette: Upkeep";
                case CityGazetteEntryType.Citizens: return "City Gazette: Citizens";
                case CityGazetteEntryType.Founding: return "City Gazette: Founding";
                case CityGazetteEntryType.Tax: return "City Gazette: Taxes";
                case CityGazetteEntryType.Decision: return "City Gazette: Decision";
                case CityGazetteEntryType.Edict: return "City Gazette: Edict";
                case CityGazetteEntryType.Project: return "City Gazette: Project";
                default: return "City Gazette";
            }
        }

        /// <summary>
        /// The Discord description for an entry, in the layout of the property broadcasts. Every argument must already be
        /// safe for Discord. It names the official's title, never the player.
        /// </summary>
        public static string BuildFeedDescription(string cityName, string levelName, int level, string officialTitle, string text)
        {
            var description = new StringBuilder();
            description.Append($"**City**: {cityName}\n");
            description.Append($"**City Level**: {levelName} (Lvl. {level})\n");
            if (!string.IsNullOrWhiteSpace(officialTitle))
                description.Append($"**Official**: {officialTitle}\n");

            description.Append('\n').Append(text);
            return description.ToString();
        }

        /// <summary>
        /// The description of a story hook for the DM feed. The feed is staff only, so it names the official's character.
        /// Every argument must already be safe for Discord.
        /// </summary>
        public static string BuildHookDescription(
            string cityName,
            string levelName,
            int level,
            string officialName,
            string officialTitle,
            bool isSecret,
            string text)
        {
            var description = new StringBuilder();
            description.Append($"**City**: {cityName}\n");
            description.Append($"**City Level**: {levelName} (Lvl. {level})\n");

            if (!string.IsNullOrWhiteSpace(officialName))
            {
                var title = string.IsNullOrWhiteSpace(officialTitle) ? string.Empty : $" ({officialTitle})";
                description.Append($"**Official**: {officialName}{title}\n");
            }
            else if (!string.IsNullOrWhiteSpace(officialTitle))
            {
                description.Append($"**Official**: {officialTitle}\n");
            }

            if (isSecret)
                description.Append("**Secret**: Hidden from the public Gazette\n");

            description.Append('\n').Append(text);
            return description.ToString();
        }

        private static IEnumerable<CityGazetteEntry> OrderNewestFirst(IEnumerable<CityGazetteEntry> entries)
        {
            return (entries ?? Enumerable.Empty<CityGazetteEntry>())
                .Where(entry => entry != null)
                .OrderByDescending(entry => entry.TimeUtc)
                .ThenByDescending(entry => entry.DateCreated)
                .ThenBy(entry => entry.Id, StringComparer.Ordinal);
        }
    }
}
