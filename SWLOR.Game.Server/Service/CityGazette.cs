using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service.CityGazetteService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The City Gazette: each city's public log. Entries are saved per city (the newest
    /// <see cref="CityGazetteRules.MaxEntriesPerCity"/> stay), non-secret entries go to the property Discord feed, and hook
    /// entries go to the DM feed as story hooks.
    /// </summary>
    public static class CityGazette
    {
        private const int FeedColor = 5763719;
        private const int HookColor = 15105570;
        private const string FeedAuthor = "SWLOR Property Broadcast";
        private const string HookAuthor = "SWLOR City Story Hook";

        private static readonly ApplicationSettings _appSettings = ApplicationSettings.Get();

        /// <summary>
        /// Writes an entry. A non-secret entry is posted to the property Discord feed, and a hook entry also to the DM feed.
        /// A failure is logged and never reaches the caller.
        /// </summary>
        /// <returns>The entry, or null when it was not written.</returns>
        public static CityGazetteEntry Post(
            string cityId,
            CityGazetteEntryType type,
            string text,
            string officialPlayerId = null,
            string officialTitle = null,
            bool isSecret = false,
            bool isHook = false)
        {
            var entry = Write(cityId, type, text, officialPlayerId, officialTitle, isSecret, isHook);
            if (entry == null)
                return null;

            var city = DB.Get<WorldProperty>(cityId);
            if (CityGazetteRules.ShouldPostToFeed(entry))
                PostToFeed(city, entry);
            if (CityGazetteRules.ShouldPostToDMFeed(entry))
                PostToDMFeed(city, entry);

            return entry;
        }

        /// <summary>
        /// Writes the entry for an event whose Discord message was already sent, so nothing is posted again.
        /// </summary>
        public static CityGazetteEntry Record(
            string cityId,
            CityGazetteEntryType type,
            string text,
            string officialPlayerId = null,
            string officialTitle = null)
        {
            return Write(cityId, type, text, officialPlayerId, officialTitle, false, false);
        }

        /// <summary>
        /// Writes the Gazette entry for a property broadcast of a city or station. The broadcast's Discord message is
        /// unchanged; any other property is ignored.
        /// </summary>
        public static void RecordPropertyBroadcast(WorldProperty property, string title, string details)
        {
            if (!CityGazetteRules.ShouldMirrorBroadcast(property))
                return;

            Record(
                property.Id,
                CityGazetteRules.GetEntryTypeForEvent(title),
                CityGazetteRules.BuildBroadcastText(title, details));
        }

        /// <summary>
        /// A city's entries, newest first. Secret entries are left out unless <paramref name="includeSecret"/> is set.
        /// </summary>
        public static List<CityGazetteEntry> GetEntries(string cityId, bool includeSecret)
        {
            if (string.IsNullOrWhiteSpace(cityId))
                return new List<CityGazetteEntry>();

            return CityGazetteRules.SelectVisibleEntries(LoadEntries(cityId), includeSecret);
        }

        /// <summary>
        /// Makes a secret entry public and posts it to the property Discord feed. Its time stays the time it was decided.
        /// </summary>
        /// <returns>False when the entry does not exist or is not secret.</returns>
        public static bool Reveal(string entryId)
        {
            if (string.IsNullOrWhiteSpace(entryId))
                return false;

            var entry = DB.Get<CityGazetteEntry>(entryId);
            if (entry == null || !entry.IsSecret)
                return false;

            entry.IsSecret = false;
            DB.Set(entry);

            PostToFeed(DB.Get<WorldProperty>(entry.CityId), entry);
            return true;
        }

        /// <summary>Opens the Gazette window on a city's log. Anyone can read it.</summary>
        /// <param name="player">The player who reads.</param>
        /// <param name="cityId">The property id of the city or station.</param>
        /// <param name="tetherObject">The terminal the window follows, or invalid for none.</param>
        public static void OpenWindow(uint player, string cityId, uint tetherObject = OBJECT_INVALID)
        {
            Gui.TogglePlayerWindow(player, GuiWindowType.CityGazette, new CityGazettePayload(cityId), tetherObject);
        }

        /// <summary>Deletes a city's whole log. A deleted city has no readers.</summary>
        public static void DeleteAll(string cityId)
        {
            if (string.IsNullOrWhiteSpace(cityId))
                return;

            foreach (var entry in LoadEntries(cityId))
            {
                DB.Delete<CityGazetteEntry>(entry.Id);
            }
        }

        private static CityGazetteEntry Write(
            string cityId,
            CityGazetteEntryType type,
            string text,
            string officialPlayerId,
            string officialTitle,
            bool isSecret,
            bool isHook)
        {
            if (!CityGazetteRules.IsPostable(cityId, text))
                return null;

            try
            {
                var entry = CityGazetteRules.CreateEntry(
                    cityId, type, text, officialPlayerId, officialTitle, isSecret, isHook, DateTime.UtcNow);
                DB.Set(entry);
                Prune(cityId);
                return entry;
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, $"Failed to write a Gazette entry for city '{cityId}': {ex}");
                return null;
            }
        }

        private static DBQuery<CityGazetteEntry> BuildQuery(string cityId)
        {
            return new DBQuery<CityGazetteEntry>()
                .AddFieldSearch(nameof(CityGazetteEntry.CityId), cityId, false);
        }

        private static List<CityGazetteEntry> LoadEntries(string cityId)
        {
            var query = BuildQuery(cityId);
            var count = (int)DB.SearchCount(query);
            if (count <= 0)
                return new List<CityGazetteEntry>();

            return DB.Search(query.AddPaging(count, 0))
                .Where(entry => entry != null)
                .ToList();
        }

        private static void Prune(string cityId)
        {
            if (DB.SearchCount(BuildQuery(cityId)) <= CityGazetteRules.MaxEntriesPerCity)
                return;

            foreach (var old in CityGazetteRules.SelectEntriesToPrune(LoadEntries(cityId)))
            {
                DB.Delete<CityGazetteEntry>(old.Id);
            }
        }

        private static void PostToFeed(WorldProperty city, CityGazetteEntry entry)
        {
            var url = _appSettings.PropertyBroadcastWebhookUrl;
            if (city == null || string.IsNullOrWhiteSpace(url))
                return;

            var level = GetCityLevel(city);
            var description = CityGazetteRules.BuildFeedDescription(
                Property.SanitizeDiscordText(city.CustomName),
                Property.GetCityLevelName(level),
                level,
                Property.SanitizeDiscordText(entry.OfficialTitle),
                Property.SanitizeDiscordText(entry.Text));

            QueueDiscord(url, FeedAuthor, CityGazetteRules.GetFeedTitle(entry.EntryType), description, FeedColor, city);
        }

        private static void PostToDMFeed(WorldProperty city, CityGazetteEntry entry)
        {
            var url = _appSettings.DMStoryHookWebhookUrl;
            if (city == null || string.IsNullOrWhiteSpace(url))
                return;

            var officialName = string.IsNullOrWhiteSpace(entry.OfficialPlayerId)
                ? string.Empty
                : DB.Get<Player>(entry.OfficialPlayerId)?.Name ?? string.Empty;
            var level = GetCityLevel(city);
            var description = CityGazetteRules.BuildHookDescription(
                Property.SanitizeDiscordText(city.CustomName),
                Property.GetCityLevelName(level),
                level,
                Property.SanitizeDiscordText(officialName),
                Property.SanitizeDiscordText(entry.OfficialTitle),
                entry.IsSecret,
                Property.SanitizeDiscordText(entry.Text));

            QueueDiscord(url, HookAuthor, "Story Hook", description, HookColor, city);
        }

        private static int GetCityLevel(WorldProperty city)
        {
            return city.Upgrades.TryGetValue(PropertyUpgradeType.CityLevel, out var level) ? level : 1;
        }

        private static void QueueDiscord(string url, string author, string title, string description, int color, WorldProperty city)
        {
            try
            {
                var enqueued = BackgroundJob.EnqueueDiscordWebhook(url, author, description, color, title)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

                if (!enqueued)
                {
                    Log.Write(LogGroup.Error, $"Failed to queue Gazette post '{title}' for city '{city.CustomName}' ({city.Id}).");
                }
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, $"Failed to queue Gazette post '{title}' for city '{city.CustomName}' ({city.Id}): {ex}");
            }
        }
    }
}
