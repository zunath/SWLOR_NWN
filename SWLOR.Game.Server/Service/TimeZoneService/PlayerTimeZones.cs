using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.TimeZoneService
{
    /// <summary>
    /// The time zones players can pick in Settings, and conversions between UTC and a player's time.
    /// The server cannot see a client's clock, so a player's zone is whatever they picked (UTC until they pick one).
    /// </summary>
    public static class PlayerTimeZones
    {
        public const string UtcId = "UTC";

        // UTC first (the default), then by standard offset. Labels show the standard offset; daylight saving applies on its own.
        public static readonly IReadOnlyList<PlayerTimeZone> All = new List<PlayerTimeZone>
        {
            new(UtcId, "UTC", "UTC"),
            new("Pacific/Honolulu", "UTC-10 Hawaii", "HST"),
            new("America/Anchorage", "UTC-9 Alaska", "AKST", "AKDT"),
            new("America/Los_Angeles", "UTC-8 US Pacific", "PST", "PDT"),
            new("America/Denver", "UTC-7 US Mountain", "MST", "MDT"),
            new("America/Phoenix", "UTC-7 Arizona", "MST"),
            new("America/Chicago", "UTC-6 US Central", "CST", "CDT"),
            new("America/Mexico_City", "UTC-6 Mexico City"),
            new("America/New_York", "UTC-5 US Eastern", "EST", "EDT"),
            new("America/Halifax", "UTC-4 Atlantic Canada", "AST", "ADT"),
            new("America/St_Johns", "UTC-3:30 Newfoundland", "NST", "NDT"),
            new("America/Sao_Paulo", "UTC-3 Brazil (Sao Paulo)"),
            new("America/Argentina/Buenos_Aires", "UTC-3 Argentina"),
            new("Europe/London", "UTC+0 UK, Ireland, Portugal"),
            new("Europe/Paris", "UTC+1 Central Europe", "CET", "CEST"),
            new("Europe/Athens", "UTC+2 Eastern Europe", "EET", "EEST"),
            new("Africa/Johannesburg", "UTC+2 South Africa", "SAST"),
            new("Europe/Moscow", "UTC+3 Moscow, Istanbul", "MSK"),
            new("Asia/Dubai", "UTC+4 Dubai"),
            new("Asia/Kolkata", "UTC+5:30 India", "IST"),
            new("Asia/Bangkok", "UTC+7 Bangkok, Jakarta"),
            new("Asia/Shanghai", "UTC+8 China, Singapore"),
            new("Australia/Perth", "UTC+8 Australia West", "AWST"),
            new("Asia/Tokyo", "UTC+9 Japan, Korea"),
            new("Australia/Adelaide", "UTC+9:30 Australia Central", "ACST", "ACDT"),
            new("Australia/Sydney", "UTC+10 Australia East", "AEST", "AEDT"),
            new("Australia/Brisbane", "UTC+10 Queensland", "AEST"),
            new("Pacific/Auckland", "UTC+12 New Zealand", "NZST", "NZDT"),
        };

        public static PlayerTimeZone Utc => All[0];

        private static readonly Dictionary<string, TimeZoneInfo> Resolved = new();

        /// <summary>
        /// The zone with this id, or UTC when the id is empty or not in the list.
        /// </summary>
        public static PlayerTimeZone Get(string id)
        {
            return All.FirstOrDefault(zone => zone.Id == id) ?? Utc;
        }

        public static int IndexOf(string id)
        {
            var index = All.ToList().FindIndex(zone => zone.Id == id);
            return index < 0 ? 0 : index;
        }

        public static PlayerTimeZone GetByIndex(int index)
        {
            return index >= 0 && index < All.Count ? All[index] : Utc;
        }

        /// <summary>
        /// The zone a player picked in Settings, or a DM in DM Settings, or UTC.
        /// </summary>
        public static PlayerTimeZone ForPlayer(uint player)
        {
            // DM avatars and possessed creatures have no Player record; a DM's choice follows their CD key.
            if (DMPresence.IsControlledByDM(player))
                return Get(DMPreference.Get(player).TimeZoneId);

            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            return Get(dbPlayer?.Settings?.TimeZoneId);
        }

        public static TimeZoneInfo GetInfo(PlayerTimeZone zone)
        {
            if (zone == null || zone.Id == UtcId)
                return TimeZoneInfo.Utc;

            lock (Resolved)
            {
                if (!Resolved.TryGetValue(zone.Id, out var info))
                {
                    // A host without the zone in its time zone database shows UTC rather than failing the window.
                    try
                    {
                        info = TimeZoneInfo.FindSystemTimeZoneById(zone.Id);
                    }
                    catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
                    {
                        info = TimeZoneInfo.Utc;
                    }

                    Resolved[zone.Id] = info;
                }

                return info;
            }
        }

        public static DateTime ToLocal(DateTime utc, PlayerTimeZone zone)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), GetInfo(zone));
        }

        /// <summary>
        /// Converts a wall-clock time in the zone to UTC. Returns false for a time that daylight saving skips.
        /// A time that happens twice when clocks go back is read as the first one.
        /// </summary>
        public static bool TryToUtc(DateTime local, PlayerTimeZone zone, out DateTime utc)
        {
            var info = GetInfo(zone);
            var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
            if (info.IsInvalidTime(unspecified))
            {
                utc = default;
                return false;
            }

            var offset = info.IsAmbiguousTime(unspecified)
                ? info.GetAmbiguousTimeOffsets(unspecified).Max()
                : info.GetUtcOffset(unspecified);
            utc = DateTime.SpecifyKind(unspecified - offset, DateTimeKind.Utc);
            return true;
        }

        /// <summary>
        /// The short name shown after a time: "EST", "CEST", or the offset ("UTC+8", "UTC-3:30") for zones without one.
        /// </summary>
        public static string GetAbbreviation(DateTime utc, PlayerTimeZone zone)
        {
            zone ??= Utc;
            var info = GetInfo(zone);
            // A zone this host could not resolve shows UTC times, so its own names would be wrong.
            if (info.Equals(TimeZoneInfo.Utc))
                return "UTC";

            var abbreviation = info.IsDaylightSavingTime(DateTime.SpecifyKind(utc, DateTimeKind.Utc))
                ? zone.DaylightAbbreviation
                : zone.StandardAbbreviation;
            return abbreviation ?? FormatOffset(info.GetUtcOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)));
        }

        public static string FormatOffset(TimeSpan offset)
        {
            if (offset == TimeSpan.Zero)
                return "UTC";

            var sign = offset < TimeSpan.Zero ? "-" : "+";
            var absolute = offset.Duration();
            return absolute.Minutes == 0
                ? $"UTC{sign}{absolute.Hours.ToString(CultureInfo.InvariantCulture)}"
                : $"UTC{sign}{absolute.Hours.ToString(CultureInfo.InvariantCulture)}:{absolute.Minutes:00}";
        }
    }
}
