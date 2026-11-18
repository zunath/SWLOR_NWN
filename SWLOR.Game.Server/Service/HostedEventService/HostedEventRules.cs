using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.TimeZoneService;

namespace SWLOR.Game.Server.Service.HostedEventService
{
    /// <summary>
    /// Engine-free event rules: travel refusals, start time parsing, expiry, ordering and display text.
    /// </summary>
    public static class HostedEventRules
    {
        public const int MaxNameLength = 64;
        public const int MaxHostLength = 64;
        public const int MaxDescriptionLength = 1000;
        public const string StartTimeFormat = "yyyy-MM-dd HH:mm";

        // An event its host never started stops being listed this long after its start time.
        public static readonly TimeSpan UnstartedExpiry = TimeSpan.FromHours(12);
        // A running event nobody ended ends itself this long after it started.
        public static readonly TimeSpan RunningExpiry = TimeSpan.FromHours(24);

        public const string EventNotFound = "That event has ended.";
        public const string DMsUseDMTools = "DMs travel to events with Go to Arrival in the DM Events window.";
        public const string NotStarted = "This event has not started yet.";
        public const string JoinClosed = "The host has not opened this event for joining.";
        public const string NoArrivalPoint = "The host has not set an arrival point for this event.";
        public const string AlreadyAtEvent = "You are already at this event.";
        public const string AtAnotherEvent = "You are at another event. Leave it first with /leaveevent.";
        public const string DeadCannotJoin = "You cannot travel to an event while dead.";
        public const string SpaceCannotJoin = "Land your ship before traveling to an event.";
        public const string CombatCannotJoin = "You cannot travel to an event during combat.";
        public const string OutOfCharacterCannotJoin = "You cannot travel to an event from here.";
        public const string NotAtEvent = "You have not joined an event. Use /events to find one.";
        public const string DeadCannotLeave = "You cannot leave the event while dead.";
        public const string SpaceCannotLeave = "Land your ship before leaving the event.";
        public const string CombatCannotLeave = "You cannot leave the event during combat.";

        /// <summary>
        /// Returns why a player cannot join an event, or an empty string when they can.
        /// </summary>
        public static string GetJoinRefusal(HostedEvent hostedEvent, HostedEventTravelState state)
        {
            if (state.IsDM)
                return DMsUseDMTools;
            if (hostedEvent == null)
                return EventNotFound;
            if (hostedEvent.Status != HostedEventStatus.Running)
                return NotStarted;
            if (!hostedEvent.IsJoinOpen)
                return JoinClosed;
            if (!HasArrivalPoint(hostedEvent))
                return NoArrivalPoint;
            if (state.CurrentEventId == hostedEvent.Id)
                return AlreadyAtEvent;
            if (!string.IsNullOrEmpty(state.CurrentEventId))
                return AtAnotherEvent;
            if (state.IsDead)
                return DeadCannotJoin;
            if (state.IsInSpace)
                return SpaceCannotJoin;
            if (state.IsInCombat)
                return CombatCannotJoin;
            if (state.IsInOutOfCharacterArea)
                return OutOfCharacterCannotJoin;

            return string.Empty;
        }

        /// <summary>
        /// Returns why a player cannot leave their event, or an empty string when they can.
        /// A DM sending everyone back skips the combat check.
        /// </summary>
        public static string GetLeaveRefusal(HostedEventTravelState state, bool ignoreCombat = false)
        {
            if (state.IsDM)
                return DMsUseDMTools;
            if (string.IsNullOrEmpty(state.CurrentEventId))
                return NotAtEvent;
            if (state.IsDead)
                return DeadCannotLeave;
            if (state.IsInSpace)
                return SpaceCannotLeave;
            if (state.IsInCombat && !ignoreCombat)
                return CombatCannotLeave;

            return string.Empty;
        }

        /// <summary>
        /// Whether players could join the event right now, ignoring their own state.
        /// </summary>
        public static bool IsJoinable(HostedEvent hostedEvent)
        {
            return hostedEvent is { Status: HostedEventStatus.Running } && IsOpenToJoin(hostedEvent);
        }

        /// <summary>
        /// Whether players will be able to join while the event runs: joining is allowed and an arrival point is set.
        /// </summary>
        public static bool IsOpenToJoin(HostedEvent hostedEvent)
        {
            return hostedEvent is { IsJoinOpen: true } && HasArrivalPoint(hostedEvent);
        }

        public static bool HasArrivalPoint(HostedEvent hostedEvent)
        {
            return !string.IsNullOrWhiteSpace(hostedEvent?.ArrivalAreaResref);
        }

        /// <summary>
        /// Checks the details a DM entered. Returns an error, or an empty string when they can be saved.
        /// </summary>
        public static string ValidateDetails(
            string name,
            string hostName,
            string description,
            string startText,
            PlayerTimeZone zone,
            out DateTime startUtc)
        {
            startUtc = default;
            if (string.IsNullOrWhiteSpace(name))
                return "Enter a name for the event.";
            if (name.Trim().Length > MaxNameLength)
                return $"Event names are limited to {MaxNameLength} characters.";
            if ((hostName ?? string.Empty).Trim().Length > MaxHostLength)
                return $"Host names are limited to {MaxHostLength} characters.";
            if ((description ?? string.Empty).Length > MaxDescriptionLength)
                return $"Descriptions are limited to {MaxDescriptionLength} characters.";
            zone ??= PlayerTimeZones.Utc;
            if (!TryParseLocalTime(startText, out var local))
                return $"Enter the start time in your time zone ({zone.Label}) as {StartTimeFormat}, for example 2026-11-20 19:30.";
            if (!PlayerTimeZones.TryToUtc(local, zone, out startUtc))
                return $"{FormatLocal(local)} is skipped by the daylight saving change in your time zone ({zone.Label}). Pick another time.";

            return string.Empty;
        }

        /// <summary>
        /// The start to save: the stored one while the DM left the start text as shown, otherwise the one they typed.
        /// Re-reading unchanged text would move an event in the repeated hour when clocks go back to the first one.
        /// </summary>
        public static DateTime GetStartToSave(DateTime? storedUtc, string startText, PlayerTimeZone zone, DateTime typedUtc)
        {
            zone ??= PlayerTimeZones.Utc;
            return storedUtc.HasValue && FormatStartTime(storedUtc.Value, zone) == (startText ?? string.Empty).Trim()
                ? storedUtc.Value
                : typedUtc;
        }

        /// <summary>
        /// Reads a wall-clock time typed as yyyy-MM-dd HH:mm.
        /// </summary>
        public static bool TryParseLocalTime(string text, out DateTime local)
        {
            var parsed = DateTime.TryParseExact(
                (text ?? string.Empty).Trim(),
                StartTimeFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out local);
            if (parsed)
                local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
            return parsed;
        }

        /// <summary>
        /// A UTC time as the zone's wall clock, in the format DMs type: "2026-11-20 19:30".
        /// </summary>
        public static string FormatStartTime(DateTime utc, PlayerTimeZone zone)
        {
            return FormatLocal(PlayerTimeZones.ToLocal(utc, zone));
        }

        /// <summary>
        /// A UTC time as the zone's wall clock with the zone's short name: "2026-11-20 19:30 EST".
        /// </summary>
        public static string FormatZoneTime(DateTime utc, PlayerTimeZone zone)
        {
            return $"{FormatStartTime(utc, zone)} {PlayerTimeZones.GetAbbreviation(utc, zone)}";
        }

        private static string FormatLocal(DateTime local)
        {
            return local.ToString(StartTimeFormat, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// The next whole hour on the zone's clock at least 30 minutes away, used as a new event's start time (UTC).
        /// </summary>
        public static DateTime GetDefaultStartTime(DateTime now, PlayerTimeZone zone)
        {
            var local = PlayerTimeZones.ToLocal(now, zone);
            var hour = new DateTime(local.Year, local.Month, local.Day, local.Hour, 0, 0, DateTimeKind.Unspecified);
            for (var step = 1; step <= 4; step++)
            {
                if (PlayerTimeZones.TryToUtc(hour.AddHours(step), zone, out var utc) && utc - now >= TimeSpan.FromMinutes(30))
                    return utc;
            }

            return now.AddHours(1);
        }

        /// <summary>
        /// Whether an event should be removed: never started long after its start time, or running far too long.
        /// </summary>
        public static bool IsExpired(HostedEvent hostedEvent, DateTime now)
        {
            if (hostedEvent.Status == HostedEventStatus.Running)
                return now - (hostedEvent.StartedUtc ?? hostedEvent.ScheduledStartUtc) >= RunningExpiry;

            return now - hostedEvent.ScheduledStartUtc >= UnstartedExpiry;
        }

        /// <summary>
        /// Running events first, longest running first, then scheduled events soonest first.
        /// </summary>
        public static List<HostedEvent> OrderForDisplay(IEnumerable<HostedEvent> events)
        {
            return events
                .OrderBy(e => e.Status == HostedEventStatus.Running ? 0 : 1)
                .ThenBy(e => e.Status == HostedEventStatus.Running ? e.StartedUtc ?? e.ScheduledStartUtc : e.ScheduledStartUtc)
                .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// The short status shown beside an event in the list: "Live", "in 3h 20m", or "Delayed".
        /// </summary>
        public static string GetListStatus(HostedEvent hostedEvent, DateTime now)
        {
            if (hostedEvent.Status == HostedEventStatus.Running)
                return "Live";

            return hostedEvent.ScheduledStartUtc > now
                ? "in " + FormatDuration(hostedEvent.ScheduledStartUtc - now)
                : "Delayed";
        }

        /// <summary>
        /// The full timing line shown in the details.
        /// </summary>
        public static string DescribeTiming(HostedEvent hostedEvent, DateTime now, PlayerTimeZone zone)
        {
            if (hostedEvent.Status == HostedEventStatus.Running)
            {
                var started = hostedEvent.StartedUtc ?? hostedEvent.ScheduledStartUtc;
                return $"Running since {FormatZoneTime(started, zone)} ({FormatDuration(now - started)} ago).";
            }

            var scheduled = FormatZoneTime(hostedEvent.ScheduledStartUtc, zone);
            return hostedEvent.ScheduledStartUtc > now
                ? $"Starts {scheduled} (in {FormatDuration(hostedEvent.ScheduledStartUtc - now)})."
                : $"Scheduled for {scheduled}. Waiting for the host to start it.";
        }

        /// <summary>
        /// A rounded duration: "2d 3h", "3h 20m", "45m", or "1m" for anything shorter.
        /// </summary>
        public static string FormatDuration(TimeSpan duration)
        {
            if (duration < TimeSpan.Zero)
                duration = TimeSpan.Zero;

            if (duration.TotalDays >= 1)
                return duration.Hours > 0 ? $"{(int)duration.TotalDays}d {duration.Hours}h" : $"{(int)duration.TotalDays}d";
            if (duration.TotalHours >= 1)
                return duration.Minutes > 0 ? $"{(int)duration.TotalHours}h {duration.Minutes}m" : $"{(int)duration.TotalHours}h";

            return $"{Math.Max(1, (int)duration.TotalMinutes)}m";
        }
    }
}
