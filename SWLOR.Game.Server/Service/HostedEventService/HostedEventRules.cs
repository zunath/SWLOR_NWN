using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWLOR.Game.Server.Entity;

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
        public static string ValidateDetails(string name, string hostName, string description, string startText, out DateTime startUtc)
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
            if (!TryParseStartTime(startText, out startUtc))
                return $"Enter the start time in UTC as {StartTimeFormat}, for example 2026-11-20 19:30.";

            return string.Empty;
        }

        /// <summary>
        /// Reads a UTC start time typed as yyyy-MM-dd HH:mm.
        /// </summary>
        public static bool TryParseStartTime(string text, out DateTime startUtc)
        {
            var parsed = DateTime.TryParseExact(
                (text ?? string.Empty).Trim(),
                StartTimeFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out startUtc);
            if (parsed)
                startUtc = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc);
            return parsed;
        }

        public static string FormatStartTime(DateTime utc)
        {
            return utc.ToString(StartTimeFormat, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// The next whole hour at least 30 minutes away, used as a new event's start time.
        /// </summary>
        public static DateTime GetDefaultStartTime(DateTime now)
        {
            var hour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc).AddHours(1);
            return hour - now < TimeSpan.FromMinutes(30) ? hour.AddHours(1) : hour;
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
        public static string DescribeTiming(HostedEvent hostedEvent, DateTime now)
        {
            if (hostedEvent.Status == HostedEventStatus.Running)
            {
                var started = hostedEvent.StartedUtc ?? hostedEvent.ScheduledStartUtc;
                return $"Running since {FormatStartTime(started)} UTC ({FormatDuration(now - started)} ago).";
            }

            var scheduled = FormatStartTime(hostedEvent.ScheduledStartUtc);
            return hostedEvent.ScheduledStartUtc > now
                ? $"Starts {scheduled} UTC (in {FormatDuration(hostedEvent.ScheduledStartUtc - now)})."
                : $"Scheduled for {scheduled} UTC. Waiting for the host to start it.";
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
