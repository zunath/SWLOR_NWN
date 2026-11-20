using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.Game.Server.Service.HostedEventService;
using SWLOR.Game.Server.Service.TimeZoneService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// DM-hosted events: DMs schedule, start and end them; players see them with /events, join them when
    /// the host allows it, and go back to where they joined from with /leaveevent.
    /// </summary>
    public static class HostedEvents
    {
        // The exact spot a player joined from. Kept for the session so a player who joined from an
        // instance (an apartment, a starship) returns there while it still exists.
        private const string ReturnLocationVariable = "HOSTED_EVENT_RETURN_LOCATION";
        private const string ReturnEventIdVariable = "HOSTED_EVENT_RETURN_EVENT_ID";
        private const float ReturnArrivalRadius = 5f;
        private static readonly TimeSpan ReturnCheckInterval = TimeSpan.FromSeconds(1);
        private const int ReturnChecks = 120;

        private static readonly HashSet<string> OutOfCharacterAreaResrefs = new() { "ooc_area", "char_migration", "spending_area" };
        // Written by the background service (DiscordEventSync.SnapshotKey): the Discord server's Events tab.
        private const string DiscordSnapshotKey = "swlor:discord:scheduled-events";
        private const string DiscordCreatedBy = "Discord";
        private const int DiscordPostColor = 3447003;

        private static readonly Dictionary<string, HostedEvent> Events = new();
        // Discord event ID -> the start time of the occurrence a DM cancelled or ended in game.
        private static readonly Dictionary<string, DateTime> DiscordDismissals = new();
        private static bool _sweepScheduled;

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void LoadEvents()
        {
            Events.Clear();
            foreach (var hostedEvent in DB.Search(new DBQuery<HostedEvent>().AddPaging(500, 0)))
                Events[hostedEvent.Id] = hostedEvent;

            DiscordDismissals.Clear();
            foreach (var dismissal in DB.Search(new DBQuery<DiscordEventDismissal>().AddPaging(500, 0)))
                DiscordDismissals[dismissal.Id] = dismissal.ScheduledStartUtc;

            SyncFromDiscord();

            if (!_sweepScheduled)
            {
                Scheduler.ScheduleRepeating(SweepEvents, TimeSpan.FromMinutes(1));
                _sweepScheduled = true;
            }
        }

        /// <summary>
        /// Every event that is scheduled or running, running events first.
        /// </summary>
        public static List<HostedEvent> GetEvents()
        {
            return HostedEventRules.OrderForDisplay(Events.Values);
        }

        public static HostedEvent GetEvent(string eventId)
        {
            return !string.IsNullOrEmpty(eventId) && Events.TryGetValue(eventId, out var hostedEvent)
                ? hostedEvent
                : null;
        }

        /// <summary>
        /// The return point of the event a player is at, or null when they are not at one.
        /// </summary>
        public static HostedEventReturnPoint GetReturnPoint(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return null;

            return DB.Get<Player>(GetObjectUUID(player))?.EventReturnPoint;
        }

        /// <summary>
        /// Creates an event, or updates one, from the DM Events window. Returns an error, or an empty string.
        /// </summary>
        public static string SaveDetails(
            uint dm,
            string eventId,
            string name,
            string hostName,
            string description,
            string startText,
            DateTime? shownStartUtc,
            PlayerTimeZone zone,
            bool isJoinOpen,
            bool announceOnStart,
            out HostedEvent saved)
        {
            saved = null;
            var synced = GetEvent(eventId);
            if (DiscordEventImport.IsSyncedFromDiscord(synced))
            {
                // Discord owns the name, host, time and description until a DM starts the event.
                synced.IsJoinOpen = isJoinOpen;
                synced.AnnounceOnStart = announceOnStart;
                Store(synced);
                Log.Write(LogGroup.DM, $"{DMPresence.DescribeDM(dm)} updated the options of Discord event '{synced.Name}' ({synced.Id}).");
                saved = synced;
                return string.Empty;
            }

            var error = HostedEventRules.ValidateDetails(name, hostName, description, startText, zone, out var startUtc);
            if (!string.IsNullOrEmpty(error))
                return error;

            var isNew = string.IsNullOrEmpty(eventId);
            var hostedEvent = isNew ? new HostedEvent { CreatedBy = DMPresence.DescribeDM(dm) } : GetEvent(eventId);
            if (hostedEvent == null)
                return HostedEventRules.EventNotFound;

            hostedEvent.Name = name.Trim();
            hostedEvent.HostName = string.IsNullOrWhiteSpace(hostName) ? GetName(DMPresence.GetDM(dm)) : hostName.Trim();
            hostedEvent.Description = description ?? string.Empty;
            hostedEvent.ScheduledStartUtc = HostedEventRules.GetStartToSave(
                shownStartUtc ?? (isNew ? null : hostedEvent.ScheduledStartUtc), startText, zone, startUtc);
            hostedEvent.IsJoinOpen = isJoinOpen;
            hostedEvent.AnnounceOnStart = announceOnStart;

            Store(hostedEvent);
            Log.Write(LogGroup.DM, $"{DMPresence.DescribeDM(dm)} {(isNew ? "created" : "updated")} event '{hostedEvent.Name}' ({hostedEvent.Id}).");
            saved = hostedEvent;
            return string.Empty;
        }

        /// <summary>
        /// Sets an event's arrival point to where the DM, or the creature they possess, is standing.
        /// </summary>
        public static string SetArrivalPoint(uint dm, string eventId)
        {
            var hostedEvent = GetEvent(eventId);
            if (hostedEvent == null)
                return HostedEventRules.EventNotFound;

            var controlled = DMPresence.GetControlledCreature(dm);
            var area = GetArea(controlled);
            if (!IsPersistentArea(area))
                return "Players cannot be sent here. Stand in a regular area, not an instance, space, or an out-of-character area.";

            var position = GetPosition(controlled);
            hostedEvent.ArrivalAreaResref = GetResRef(area);
            hostedEvent.ArrivalX = position.X;
            hostedEvent.ArrivalY = position.Y;
            hostedEvent.ArrivalZ = position.Z;
            hostedEvent.ArrivalFacing = GetFacing(controlled);
            Store(hostedEvent);
            Log.Write(LogGroup.DM, $"{DMPresence.DescribeDM(dm)} set the arrival point of event '{hostedEvent.Name}' ({hostedEvent.Id}) to {hostedEvent.ArrivalAreaResref}.");
            return string.Empty;
        }

        public static string ClearArrivalPoint(uint dm, string eventId)
        {
            var hostedEvent = GetEvent(eventId);
            if (hostedEvent == null)
                return HostedEventRules.EventNotFound;

            hostedEvent.ArrivalAreaResref = string.Empty;
            Store(hostedEvent);
            Log.Write(LogGroup.DM, $"{DMPresence.DescribeDM(dm)} cleared the arrival point of event '{hostedEvent.Name}' ({hostedEvent.Id}).");
            return string.Empty;
        }

        /// <summary>
        /// The arrival point's area name, or an empty string when none is set or its area is gone.
        /// </summary>
        public static string GetArrivalAreaName(HostedEvent hostedEvent)
        {
            var area = GetArrivalArea(hostedEvent);
            return GetIsObjectValid(area) ? GetName(area) : string.Empty;
        }

        /// <summary>
        /// Moves the DM, or the creature they possess, to an event's arrival point.
        /// </summary>
        public static string GoToArrivalPoint(uint dm, string eventId)
        {
            var hostedEvent = GetEvent(eventId);
            if (hostedEvent == null)
                return HostedEventRules.EventNotFound;
            if (!GetIsObjectValid(GetArrivalArea(hostedEvent)))
                return HostedEventRules.NoArrivalPoint;

            DMPresence.JumpTo(dm, GetArrivalLocation(hostedEvent), $"the arrival point of event '{hostedEvent.Name}'");
            return string.Empty;
        }

        /// <summary>
        /// Starts a scheduled event and, when the host chose to, tells every player who wants event announcements.
        /// </summary>
        public static string Start(uint dm, string eventId)
        {
            var hostedEvent = GetEvent(eventId);
            if (hostedEvent == null)
                return HostedEventRules.EventNotFound;
            if (hostedEvent.Status == HostedEventStatus.Running)
                return "This event is already running.";

            hostedEvent.Status = HostedEventStatus.Running;
            hostedEvent.StartedUtc = DateTime.UtcNow;
            Store(hostedEvent);
            Log.Write(LogGroup.DM, $"{DMPresence.DescribeDM(dm)} started event '{hostedEvent.Name}' ({hostedEvent.Id}).");

            if (hostedEvent.AnnounceOnStart)
            {
                Announce(hostedEvent);
                PostStartToDiscord(hostedEvent);
            }

            return string.Empty;
        }

        /// <summary>
        /// Ends a running event or cancels a scheduled one. Players at the event stay where they are
        /// and keep /leaveevent until they use it.
        /// </summary>
        public static string End(uint dm, string eventId)
        {
            var hostedEvent = GetEvent(eventId);
            if (hostedEvent == null)
                return HostedEventRules.EventNotFound;

            var wasRunning = hostedEvent.Status == HostedEventStatus.Running;
            Remove(hostedEvent);
            DismissDiscordEvent(hostedEvent);
            Log.Write(LogGroup.DM, $"{DMPresence.DescribeDM(dm)} {(wasRunning ? "ended" : "cancelled")} event '{hostedEvent.Name}' ({hostedEvent.Id}).");

            if (wasRunning)
                TellParticipantsEnded(hostedEvent);

            return string.Empty;
        }

        /// <summary>
        /// Sends every online player at an event back to where they joined from, even in combat.
        /// Returns a summary for the DM.
        /// </summary>
        public static string SendEveryoneBack(uint dm, string eventId)
        {
            var hostedEvent = GetEvent(eventId);
            if (hostedEvent == null)
                return HostedEventRules.EventNotFound;

            // Players sent back stay participants until they arrive, so count the offline ones first.
            var online = GetOnlineParticipants(hostedEvent);
            var offline = hostedEvent.ParticipantPlayerIds.Count - online.Count;
            var sent = 0;
            var refused = new List<string>();
            foreach (var player in online)
            {
                var error = Leave(player, ignoreCombat: true);
                if (string.IsNullOrEmpty(error))
                {
                    SendMessageToPC(player, ColorToken.Green($"The host sent you back from {hostedEvent.Name}."));
                    sent++;
                }
                else
                {
                    refused.Add($"{GetName(player)} ({error})");
                }
            }

            Log.Write(LogGroup.DM, $"{DMPresence.DescribeDM(dm)} sent {sent} players back from event '{hostedEvent.Name}' ({hostedEvent.Id}).");

            var summary = $"Sent {sent} {(sent == 1 ? "player" : "players")} back.";
            if (refused.Count > 0)
                summary += " Not sent: " + string.Join(", ", refused) + ".";
            if (offline > 0)
                summary += $" {offline} offline {(offline == 1 ? "player keeps" : "players keep")} /leaveevent.";
            return summary;
        }

        /// <summary>
        /// The number of players at an event and how many of them are online.
        /// </summary>
        public static (int Total, int Online) CountParticipants(HostedEvent hostedEvent)
        {
            return (hostedEvent.ParticipantPlayerIds.Count, GetOnlineParticipants(hostedEvent).Count);
        }

        /// <summary>
        /// Sends a player to an event's arrival point, remembering where they came from.
        /// </summary>
        public static string Join(uint player, string eventId)
        {
            var hostedEvent = GetEvent(eventId);
            var error = HostedEventRules.GetJoinRefusal(hostedEvent, GetTravelState(player));
            if (!string.IsNullOrEmpty(error))
                return error;

            if (!GetIsObjectValid(GetArrivalArea(hostedEvent)))
                return "The arrival point no longer exists. Let the host know.";

            var landing = FindLanding(player, GetArrivalLocation(hostedEvent));
            if (landing == null)
                return "The arrival point is blocked. Let the host know.";

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            dbPlayer.EventReturnPoint = BuildReturnPoint(player, dbPlayer, hostedEvent);
            DB.Set(dbPlayer);
            SetLocalLocation(player, ReturnLocationVariable, GetLocation(player));
            SetLocalString(player, ReturnEventIdVariable, hostedEvent.Id);

            hostedEvent.ParticipantPlayerIds.Add(playerId);
            Store(hostedEvent);

            AssignCommand(player, () =>
            {
                ClearAllActions();
                JumpToLocation(landing);
            });
            SendMessageToPC(player, ColorToken.Green($"Traveling to {hostedEvent.Name}. Use /leaveevent or the Events window to go back."));
            return string.Empty;
        }

        /// <summary>
        /// Sends a player back to where they joined their event from.
        /// </summary>
        public static string Leave(uint player, bool ignoreCombat = false)
        {
            var error = HostedEventRules.GetLeaveRefusal(GetTravelState(player), ignoreCombat);
            if (!string.IsNullOrEmpty(error))
                return error;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var returnPoint = dbPlayer.EventReturnPoint;
            var destination = FindLanding(player, GetReturnLocation(player, dbPlayer, returnPoint))
                              ?? FindLanding(player, Death.GetRespawnLocation(dbPlayer));
            if (destination == null)
                return "Your way back is blocked right now. Try again shortly, or ask a DM for help.";

            // The return point stays until the player arrives, so a restart or disconnect mid-jump leaves /leaveevent working.
            AssignCommand(player, () =>
            {
                ClearAllActions();
                JumpToLocation(destination);
            });
            ConfirmReturn(player, playerId, returnPoint.EventId, destination, ReturnChecks);
            return string.Empty;
        }

        // Clears the return point once the player stands at their destination. A player who never gets there keeps it.
        private static void ConfirmReturn(uint player, string playerId, string eventId, Location destination, int checksLeft)
        {
            if (!GetIsObjectValid(player) || GetObjectUUID(player) != playerId)
                return;

            var arrived = GetArea(player) == GetAreaFromLocation(destination) &&
                          GetDistanceBetweenLocations(GetLocation(player), destination) <= ReturnArrivalRadius;
            if (!arrived)
            {
                if (checksLeft > 0)
                    Scheduler.Schedule(() => ConfirmReturn(player, playerId, eventId, destination, checksLeft - 1), ReturnCheckInterval);
                return;
            }

            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer?.EventReturnPoint?.EventId != eventId)
                return;

            dbPlayer.EventReturnPoint = null;
            DB.Set(dbPlayer);
            DeleteLocalLocation(player, ReturnLocationVariable);
            DeleteLocalString(player, ReturnEventIdVariable);

            var hostedEvent = GetEvent(eventId);
            if (hostedEvent != null && hostedEvent.ParticipantPlayerIds.Remove(playerId))
                Store(hostedEvent);
            else
                RefreshWindows();
        }

        /// <summary>
        /// Reads what blocks a player from traveling to or from an event.
        /// </summary>
        public static HostedEventTravelState GetTravelState(uint player)
        {
            var isDM = GetIsDM(player) || GetIsDMPossessed(player) || !GetIsPC(player);
            return new HostedEventTravelState
            {
                IsDM = isDM,
                IsDead = GetIsDead(player),
                IsInCombat = GetIsInCombat(player) || Enmity.HasEnmity(player),
                IsInSpace = !isDM && Space.IsPlayerInSpaceMode(player),
                IsInOutOfCharacterArea = OutOfCharacterAreaResrefs.Contains(GetResRef(GetArea(player))),
                CurrentEventId = isDM ? string.Empty : GetReturnPoint(player)?.EventId ?? string.Empty,
            };
        }

        private static void Store(HostedEvent hostedEvent)
        {
            Events[hostedEvent.Id] = hostedEvent;
            DB.Set(hostedEvent);
            RefreshWindows();
        }

        private static void Remove(HostedEvent hostedEvent)
        {
            Events.Remove(hostedEvent.Id);
            DB.Delete<HostedEvent>(hostedEvent.Id);
            RefreshWindows();
        }

        private static void SweepEvents()
        {
            SyncFromDiscord();

            var now = DateTime.UtcNow;
            foreach (var hostedEvent in Events.Values.Where(e => HostedEventRules.IsExpired(e, now)).ToList())
            {
                Remove(hostedEvent);
                DismissDiscordEvent(hostedEvent);
                Log.Write(LogGroup.DM, $"Event '{hostedEvent.Name}' ({hostedEvent.Id}) expired and was removed.");
                if (hostedEvent.Status == HostedEventStatus.Running)
                    TellParticipantsEnded(hostedEvent);
            }

            // Keeps the start countdowns in open windows current.
            RefreshWindows();
        }

        /// <summary>
        /// Lists new Discord events, copies changes to the ones no DM has started, and removes those Discord no longer lists.
        /// Does nothing until the background service has stored a snapshot.
        /// </summary>
        private static void SyncFromDiscord()
        {
            List<DiscordScheduledEvent> discordEvents;
            try
            {
                discordEvents = DiscordEventImport.Parse(DB.StringGet(DiscordSnapshotKey));
            }
            catch (Exception ex)
            {
                Log.WriteError(ex, "Could not read the Discord event snapshot.");
                return;
            }

            if (discordEvents == null)
                return;

            var plan = DiscordEventImport.Plan(discordEvents, Events.Values, DiscordDismissals);
            foreach (var discord in plan.Create)
            {
                var hostedEvent = new HostedEvent { CreatedBy = DiscordCreatedBy };
                DiscordEventImport.Apply(hostedEvent, discord);
                DB.Set(hostedEvent);
                Events[hostedEvent.Id] = hostedEvent;
                Log.Write(LogGroup.DM, $"Listed Discord event '{hostedEvent.Name}' ({hostedEvent.Id}, Discord {discord.Id}).");
            }

            // Each change reaches the cache only after it is saved, so a failed save is planned again on the next sync.
            foreach (var (hostedEvent, discord) in plan.Update)
            {
                var updated = hostedEvent.Copy();
                DiscordEventImport.Apply(updated, discord);
                DB.Set(updated);
                Events[updated.Id] = updated;
            }

            foreach (var hostedEvent in plan.Remove)
            {
                DB.Delete<HostedEvent>(hostedEvent.Id);
                Events.Remove(hostedEvent.Id);
                Log.Write(LogGroup.DM, $"Removed Discord event '{hostedEvent.Name}' ({hostedEvent.Id}): Discord no longer lists it.");
            }

            foreach (var discordId in plan.DropDismissals)
            {
                DB.Delete<DiscordEventDismissal>(discordId);
                DiscordDismissals.Remove(discordId);
            }

            if (plan.Create.Count > 0 || plan.Update.Count > 0 || plan.Remove.Count > 0)
                RefreshWindows();
        }

        // A Discord event a DM cancelled or ended stays off the list until Discord moves it to another start time.
        private static void DismissDiscordEvent(HostedEvent hostedEvent)
        {
            if (string.IsNullOrEmpty(hostedEvent.DiscordEventId))
                return;

            var occurrenceStart = DiscordEventImport.GetOccurrenceStart(hostedEvent);
            DB.Set(new DiscordEventDismissal
            {
                Id = hostedEvent.DiscordEventId,
                ScheduledStartUtc = occurrenceStart,
            });
            DiscordDismissals[hostedEvent.DiscordEventId] = occurrenceStart;
        }

        private static void PostStartToDiscord(HostedEvent hostedEvent)
        {
            var url = ApplicationSettings.Get().EventWebhookUrl;
            if (string.IsNullOrWhiteSpace(url))
                return;

            var action = HostedEventRules.IsJoinable(hostedEvent) ? "join" : "see the details";
            var message = $"Event started: {hostedEvent.Name}, hosted by {hostedEvent.HostName}. Type /events in game to {action}.";
            _ = PostToDiscordAsync(url, message);
        }

        private static async Task PostToDiscordAsync(string url, string message)
        {
            try
            {
                if (!await BackgroundJob.EnqueueDiscordWebhook(url, "SWLOR Events", message, DiscordPostColor))
                    Log.Write(LogGroup.Error, $"Could not queue the Discord post: {message}");
            }
            catch (Exception ex)
            {
                Log.WriteError(ex, "Could not queue the Discord post: {Message}", message);
            }
        }

        private static void Announce(HostedEvent hostedEvent)
        {
            var action = HostedEventRules.IsJoinable(hostedEvent) ? "join" : "see the details";
            var message = ColorToken.Cyan($"Event started: {hostedEvent.Name}, hosted by {hostedEvent.HostName}. Use /events to {action}.");
            foreach (var player in GetOnlinePCs())
            {
                if (!DMPresence.IsControlledByDM(player) &&
                    !(DB.Get<Player>(GetObjectUUID(player))?.Settings?.DisplayEventAnnouncements ?? true))
                    continue;

                SendMessageToPC(DMPresence.GetControlledCreature(player), message);
            }
        }

        private static void TellParticipantsEnded(HostedEvent hostedEvent)
        {
            foreach (var player in GetOnlineParticipants(hostedEvent))
                SendMessageToPC(player, ColorToken.Cyan($"{hostedEvent.Name} has ended. Use /leaveevent when you are ready to go back."));
        }

        // Returned as a list: callers leave or refresh players, which walks the same native PC cursor.
        private static List<uint> GetOnlineParticipants(HostedEvent hostedEvent)
        {
            return GetOnlinePCs()
                .Where(player => !DMPresence.IsControlledByDM(player) &&
                                 hostedEvent.ParticipantPlayerIds.Contains(GetObjectUUID(player)))
                .ToList();
        }

        private static List<uint> GetOnlinePCs()
        {
            var players = new List<uint>();
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
                players.Add(player);
            return players;
        }

        /// <summary>
        /// Redraws one player's open event windows, for example after they pick another time zone.
        /// </summary>
        public static void RefreshWindows(uint player)
        {
            Gui.PublishRefreshEvent(player, new HostedEventsChangedRefreshEvent(), DMPresence.GetControlledCreature(player));
        }

        private static void RefreshWindows()
        {
            var refresh = new HostedEventsChangedRefreshEvent();
            foreach (var player in GetOnlinePCs())
                Gui.PublishRefreshEvent(player, refresh, DMPresence.GetControlledCreature(player));
        }

        // Regular areas only: instances and space areas are not found by resref after a restart.
        private static bool IsPersistentArea(uint area)
        {
            if (!GetIsObjectValid(area))
                return false;

            var resref = GetResRef(area);
            var isSpace = GetLocalBool(area, "SPACE") || GetName(area).StartsWith("Space -");
            return !isSpace && !OutOfCharacterAreaResrefs.Contains(resref) && Area.GetAreaByResref(resref) == area;
        }

        private static uint GetArrivalArea(HostedEvent hostedEvent)
        {
            return HostedEventRules.HasArrivalPoint(hostedEvent)
                ? Area.GetAreaByResref(hostedEvent.ArrivalAreaResref)
                : OBJECT_INVALID;
        }

        private static Location GetArrivalLocation(HostedEvent hostedEvent)
        {
            return Location(
                GetArrivalArea(hostedEvent),
                Vector3(hostedEvent.ArrivalX, hostedEvent.ArrivalY, hostedEvent.ArrivalZ),
                hostedEvent.ArrivalFacing);
        }

        // Players spread out around the arrival point instead of stacking, and never into a spot the engine refuses.
        private static Location FindLanding(uint player, Location arrival)
        {
            var area = GetAreaFromLocation(arrival);
            var position = GetPositionFromLocation(arrival);
            var facing = GetFacingFromLocation(arrival);
            var nearby = DMJumpSpots
                .GetSpotsAround(new Vector2(position.X, position.Y))
                .Select(spot => Location(area, new Vector3(spot.X, spot.Y, position.Z), facing));

            // The engine's own landing spot, so the jump and the arrival check agree on where the player ends up.
            foreach (var candidate in nearby.Prepend(arrival))
            {
                if (DMPresence.TryGetLanding(player, candidate, out var landing))
                    return landing;
            }

            return null;
        }

        // Instances and space are not found again after a restart, so their return point is the player's last saved
        // regular location; the exact spot is kept for this session in a local variable.
        private static HostedEventReturnPoint BuildReturnPoint(uint player, Player dbPlayer, HostedEvent hostedEvent)
        {
            var returnPoint = new HostedEventReturnPoint
            {
                EventId = hostedEvent.Id,
                EventName = hostedEvent.Name,
            };

            var area = GetArea(player);
            if (IsPersistentArea(area))
            {
                var position = GetPosition(player);
                returnPoint.AreaResref = GetResRef(area);
                returnPoint.X = position.X;
                returnPoint.Y = position.Y;
                returnPoint.Z = position.Z;
                returnPoint.Facing = GetFacing(player);
            }
            else if (!string.IsNullOrWhiteSpace(dbPlayer.LocationAreaResref))
            {
                returnPoint.AreaResref = dbPlayer.LocationAreaResref;
                returnPoint.X = dbPlayer.LocationX;
                returnPoint.Y = dbPlayer.LocationY;
                returnPoint.Z = dbPlayer.LocationZ;
                returnPoint.Facing = dbPlayer.LocationOrientation;
            }

            return returnPoint;
        }

        private static Location GetReturnLocation(uint player, Player dbPlayer, HostedEventReturnPoint returnPoint)
        {
            if (GetLocalString(player, ReturnEventIdVariable) == returnPoint.EventId)
            {
                var exact = GetLocalLocation(player, ReturnLocationVariable);
                if (GetIsObjectValid(GetAreaFromLocation(exact)))
                    return exact;
            }

            var area = string.IsNullOrWhiteSpace(returnPoint.AreaResref)
                ? OBJECT_INVALID
                : Area.GetAreaByResref(returnPoint.AreaResref);
            if (GetIsObjectValid(area))
                return Location(area, Vector3(returnPoint.X, returnPoint.Y, returnPoint.Z), returnPoint.Facing);

            return Death.GetRespawnLocation(dbPlayer);
        }
    }
}
