using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.HostedEventService
{
    /// <summary>
    /// Engine-free rules for listing the Discord server's scheduled events in game: reading the snapshot the
    /// background service stores, making Discord text safe for the game, and working out what a sync changes.
    /// </summary>
    public static class DiscordEventImport
    {
        private const int ScheduledStatus = 1;
        private const int ActiveStatus = 2;
        private const int CompletedStatus = 3;
        private const int CanceledStatus = 4;
        public const string DefaultName = "Discord event";
        public const string DefaultHost = "Discord";

        private static readonly Regex Mention = new(@"<(?:@[!&]?|#)\d+>", RegexOptions.Compiled);
        private static readonly Regex CustomEmoji = new(@"<a?:\w+:\d+>", RegexOptions.Compiled);
        private static readonly Regex Timestamp = new(@"<t:(-?\d+)(?::[tTdDfFR])?>", RegexOptions.Compiled);
        private static readonly Regex MarkdownLink = new(@"\[([^\]]*)\]\((?:https?://[^)\s]+)\)", RegexOptions.Compiled);
        private static readonly Regex Heading = new(@"^#{1,3}\s+", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex Emphasis = new(@"\*\*|__|~~|\|\|", RegexOptions.Compiled);
        private static readonly Regex Spaces = new(@"[ ]{2,}", RegexOptions.Compiled);
        private static readonly Regex BlankLines = new(@"\n{3,}", RegexOptions.Compiled);

        /// <summary>
        /// Reads the events in a snapshot that are scheduled or running on Discord.
        /// Returns null when there is no snapshot or any listed event cannot be read, so nothing is removed on a bad read.
        /// </summary>
        public static List<DiscordScheduledEvent> Parse(string snapshotJson)
        {
            if (string.IsNullOrWhiteSpace(snapshotJson))
                return null;

            JToken root;
            try
            {
                // Dates stay text so they are read as UTC below, not in the server's local zone.
                using var reader = new JsonTextReader(new StringReader(snapshotJson)) { DateParseHandling = DateParseHandling.None };
                root = JToken.ReadFrom(reader);
            }
            catch (JsonException)
            {
                return null;
            }

            if (root is not JObject snapshot || snapshot["events"] is not JArray items)
                return null;

            var events = new List<DiscordScheduledEvent>();
            var seen = new HashSet<string>();
            foreach (var token in items)
            {
                // An entry that cannot be read could be an event a DM set up, so the whole snapshot is ignored.
                if (token is not JObject item)
                    return null;

                var id = ReadString(item, "id");
                var status = item["status"]?.Type == JTokenType.Integer ? item.Value<int>("status") : 0;
                if (string.IsNullOrWhiteSpace(id) || status is < ScheduledStatus or > CanceledStatus)
                    return null;

                if (status is CompletedStatus or CanceledStatus || !seen.Add(id))
                    continue;

                if (!DateTime.TryParse(
                        ReadString(item, "scheduled_start_time"),
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                        out var startUtc))
                    return null;

                var creator = item["creator"] as JObject;
                var host = ToGameText(ReadString(creator, "global_name"), HostedEventRules.MaxHostLength, singleLine: true);
                if (host.Length == 0)
                    host = ToGameText(ReadString(creator, "username"), HostedEventRules.MaxHostLength, singleLine: true);

                var name = ToGameText(ReadString(item, "name"), HostedEventRules.MaxNameLength, singleLine: true);
                events.Add(new DiscordScheduledEvent
                {
                    Id = id,
                    Name = name.Length > 0 ? name : DefaultName,
                    Description = ToGameText(ReadString(item, "description"), HostedEventRules.MaxDescriptionLength, singleLine: false),
                    HostName = host.Length > 0 ? host : DefaultHost,
                    StartUtc = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc),
                });
            }

            return events;
        }

        /// <summary>
        /// Works out what a sync changes. Events a DM has started belong to the game and are left alone.
        /// </summary>
        public static DiscordSyncPlan Plan(
            IReadOnlyList<DiscordScheduledEvent> discordEvents,
            IEnumerable<HostedEvent> events,
            IReadOnlyDictionary<string, DateTime> dismissals)
        {
            var plan = new DiscordSyncPlan();
            var listed = discordEvents.ToDictionary(e => e.Id);
            var imported = new Dictionary<string, HostedEvent>();
            foreach (var hostedEvent in events.Where(e => !string.IsNullOrEmpty(e.DiscordEventId)))
                imported.TryAdd(hostedEvent.DiscordEventId, hostedEvent);

            foreach (var discord in discordEvents)
            {
                if (imported.TryGetValue(discord.Id, out var hostedEvent))
                {
                    if (hostedEvent.Status == HostedEventStatus.Scheduled && Differs(hostedEvent, discord))
                        plan.Update.Add((hostedEvent, discord));
                    continue;
                }

                if (dismissals.TryGetValue(discord.Id, out var dismissedStart) && dismissedStart == discord.StartUtc)
                    continue;

                plan.Create.Add(discord);
            }

            foreach (var hostedEvent in imported.Values)
            {
                if (hostedEvent.Status == HostedEventStatus.Scheduled && !listed.ContainsKey(hostedEvent.DiscordEventId))
                    plan.Remove.Add(hostedEvent);
            }

            foreach (var (discordId, dismissedStart) in dismissals)
            {
                if (!listed.TryGetValue(discordId, out var discord) || discord.StartUtc != dismissedStart)
                    plan.DropDismissals.Add(discordId);
            }

            return plan;
        }

        /// <summary>
        /// Copies Discord's details onto an imported event.
        /// </summary>
        public static void Apply(HostedEvent hostedEvent, DiscordScheduledEvent discord)
        {
            hostedEvent.DiscordEventId = discord.Id;
            hostedEvent.Name = discord.Name;
            hostedEvent.HostName = discord.HostName;
            hostedEvent.Description = discord.Description;
            hostedEvent.ScheduledStartUtc = discord.StartUtc;
            hostedEvent.DiscordStartUtc = discord.StartUtc;
        }

        /// <summary>
        /// The Discord start time that identifies an imported event's occurrence, even after a DM edits the event's time.
        /// </summary>
        public static DateTime GetOccurrenceStart(HostedEvent hostedEvent)
        {
            return hostedEvent.DiscordStartUtc ?? hostedEvent.ScheduledStartUtc;
        }

        /// <summary>
        /// Whether an event's name, host, time and description come from Discord: it was imported and no DM has started it.
        /// </summary>
        public static bool IsSyncedFromDiscord(HostedEvent hostedEvent)
        {
            return hostedEvent is { Status: HostedEventStatus.Scheduled } && !string.IsNullOrEmpty(hostedEvent.DiscordEventId);
        }

        /// <summary>
        /// Turns Discord text into game text: Discord markup removed, punctuation made ASCII, accented letters kept,
        /// and anything the game cannot show (emoji, other scripts) dropped. Cut to the length limit with "...".
        /// </summary>
        public static string ToGameText(string text, int maxLength, bool singleLine)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            text = text.Replace("\r\n", "\n").Replace('\r', '\n');
            text = Timestamp.Replace(text, match => FormatTimestamp(match.Groups[1].Value));
            text = Mention.Replace(text, string.Empty);
            text = CustomEmoji.Replace(text, string.Empty);
            text = MarkdownLink.Replace(text, "$1");
            text = Heading.Replace(text, string.Empty);
            text = Emphasis.Replace(text, string.Empty);

            var builder = new StringBuilder(text.Length);
            foreach (var character in text)
                AppendGameCharacter(builder, character, singleLine);

            var lines = builder.ToString()
                .Split('\n')
                .Select(line => Spaces.Replace(line, " ").Trim());
            var result = BlankLines.Replace(string.Join("\n", lines), "\n\n").Trim();
            if (singleLine)
                result = Spaces.Replace(result, " ");

            return result.Length <= maxLength
                ? result
                : result[..(maxLength - 3)].TrimEnd() + "...";
        }

        private static bool Differs(HostedEvent hostedEvent, DiscordScheduledEvent discord)
        {
            return hostedEvent.Name != discord.Name ||
                   hostedEvent.HostName != discord.HostName ||
                   hostedEvent.Description != discord.Description ||
                   hostedEvent.ScheduledStartUtc != discord.StartUtc;
        }

        private static string FormatTimestamp(string seconds)
        {
            return long.TryParse(seconds, out var value) && value is > -62135596800 and < 253402300800
                ? DateTimeOffset.FromUnixTimeSeconds(value).UtcDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + " UTC"
                : string.Empty;
        }

        // Code points, not literals: game text in C# may not hold these characters (GameTextEncodingTests).
        private static void AppendGameCharacter(StringBuilder builder, char character, bool singleLine)
        {
            switch ((int)character)
            {
                case '\n':
                    builder.Append(singleLine ? ' ' : '\n');
                    return;
                case '\t':
                case 0x00A0: // no-break space
                case 0x2002:
                case 0x2003:
                case 0x2009:
                case 0x202F:
                    builder.Append(' ');
                    return;
                case 0x2018: // single quotes and primes
                case 0x2019:
                case 0x201A:
                case 0x201B:
                case 0x2032:
                    builder.Append('\'');
                    return;
                case 0x201C: // double quotes
                case 0x201D:
                case 0x201E:
                case 0x201F:
                case 0x2033:
                    builder.Append('"');
                    return;
                case 0x2010: // hyphens, dashes and minus
                case 0x2011:
                case 0x2012:
                case 0x2013:
                case 0x2014:
                case 0x2015:
                case 0x2212:
                case 0x2022: // bullet
                case 0x00B7: // middle dot
                    builder.Append('-');
                    return;
                case 0x2026: // ellipsis
                    builder.Append("...");
                    return;
            }

            if (character is >= ' ' and <= '~' || IsWindows1252Letter(character))
            {
                builder.Append(character);
                return;
            }

            // An accented letter outside Windows-1252 (such as o with a double acute) keeps its base letter.
            if (char.IsLetter(character))
            {
                var baseLetter = character.ToString().Normalize(NormalizationForm.FormD)[0];
                if (baseLetter is >= 'A' and <= 'Z' or >= 'a' and <= 'z')
                    builder.Append(baseLetter);
            }
        }

        private static bool IsWindows1252Letter(char character)
        {
            return character is >= (char)0xC0 and <= (char)0xFF && character != (char)0xD7 && character != (char)0xF7 ||
                   character is (char)0x152 or (char)0x153 or (char)0x160 or (char)0x161 or (char)0x17D or (char)0x17E or (char)0x178;
        }

        private static string ReadString(JObject item, string property)
        {
            return item?[property]?.Type == JTokenType.String ? item.Value<string>(property) : string.Empty;
        }
    }
}
