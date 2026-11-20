using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.InitiativeTrackerService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Initiative trackers players start in an area and others join, opened with /tracker or the dice bag.
    /// A tracker lives only in memory: it ends when its last member leaves the area or logs out.
    /// Anyone on the list can move the turn; the host and DMs also add named entries, remove entries, and reset.
    /// </summary>
    public static class InitiativeTracker
    {
        private static readonly Dictionary<int, InitiativeTrackerSession> _sessions = new();
        private static readonly Dictionary<uint, int> _sessionIdByMember = new();
        private static readonly HashSet<uint> _exitingPlayers = new();
        private static int _nextSessionId = 1;
        private static bool _isRefreshQueued;

        /// <summary>
        /// Returns who acts for a window or command: the player, or the DM avatar behind a possessed creature.
        /// </summary>
        public static uint GetMember(uint actor) => DMPresence.GetDM(actor);

        public static bool IsDM(uint member) => DMPresence.IsControlledByDM(member);

        /// <summary>
        /// Returns the tracker a member has joined. A member no longer acting in its area is taken off it first:
        /// a DM can possess a creature elsewhere without either object leaving the tracker's area.
        /// </summary>
        public static InitiativeTrackerSession GetSession(uint member)
        {
            var session = FindSession(member);
            if (session == null || session.Area == GetActingArea(member))
                return session;

            Leave(member);
            return null;
        }

        // The joined tracker as recorded, without the area check GetSession makes.
        private static InitiativeTrackerSession FindSession(uint member)
        {
            return _sessionIdByMember.TryGetValue(member, out var sessionId) &&
                   _sessions.TryGetValue(sessionId, out var session)
                ? session
                : null;
        }

        public static IReadOnlyList<InitiativeTrackerSession> GetSessionsInArea(uint area)
        {
            return _sessions.Values
                .Where(session => session.Area == area)
                .OrderBy(session => session.Id)
                .ToList();
        }

        /// <summary>
        /// The area a member is acting in: a DM's possessed creature's area, otherwise their own.
        /// </summary>
        public static uint GetActingArea(uint member) => GetArea(DMPresence.GetControlledCreature(member));

        public static bool IsManager(InitiativeTrackerSession session, uint member)
        {
            return session != null && (session.Host == member || IsDM(member));
        }

        public static bool CanMoveTurn(InitiativeTrackerSession session, uint member)
        {
            return IsManager(session, member) || session?.GetEntryOf(member) != null;
        }

        /// <summary>
        /// Opens or closes a tracker window for a player, or on the creature a DM controls.
        /// </summary>
        public static void ToggleWindow(uint actor, GuiWindowType type)
        {
            if (DMPresence.IsControlledByDM(actor))
                DMPresence.ToggleWindow(actor, type);
            else
                Gui.TogglePlayerWindow(actor, type);
        }

        public static void Start(uint member)
        {
            var area = GetActingArea(member);
            if (!GetIsObjectValid(area))
                return;

            LeaveCurrent(member);
            var session = new InitiativeTrackerSession(_nextSessionId++, area, member);
            _sessions[session.Id] = session;
            _sessionIdByMember[member] = session.Id;
            QueueRefresh();
        }

        public static void Join(uint member, int sessionId)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
            {
                Notify(member, ColorToken.Red("That tracker has ended."));
                QueueRefresh();
                return;
            }

            if (session.Area != GetActingArea(member))
            {
                Notify(member, ColorToken.Red("You can only join a tracker in the area you are in."));
                return;
            }

            if (session.IsMember(member))
                return;

            LeaveCurrent(member);
            session.Join(member);
            _sessionIdByMember[member] = session.Id;
            QueueRefresh();
        }

        public static void Leave(uint member)
        {
            if (LeaveCurrent(member))
                QueueRefresh();
        }

        public static void SetOwnEntry(uint member, int value)
        {
            var session = GetSession(member);
            if (session == null)
                return;

            if (IsDM(member))
            {
                Notify(member, ColorToken.Red("DMs add named entries instead of their own."));
                return;
            }

            if (session.SetMemberEntry(member, value))
                QueueRefresh();
        }

        /// <summary>
        /// Rolls a d20 for a member's own entry and says the result aloud, the way the dice bag does.
        /// </summary>
        public static void RollOwnEntry(uint member)
        {
            var session = GetSession(member);
            if (session == null || IsDM(member))
                return;

            var roll = Random.D20(1);
            var message = ColorToken.SkillCheck("Initiative Roll: ") + "1d20: " + roll;
            var speaker = DMPresence.GetControlledCreature(member);
            AssignCommand(speaker, () => SpeakString(message));
            SetOwnEntry(member, roll);
        }

        public static void RemoveOwnEntry(uint member)
        {
            var session = GetSession(member);
            var entry = session?.GetEntryOf(member);
            if (entry != null && session.RemoveEntry(entry.Id))
                QueueRefresh();
        }

        public static void AddNamedEntry(uint member, string name, int value)
        {
            var session = GetSession(member);
            if (!IsManager(session, member))
                return;

            name = UtilPlugin.StripColors(name ?? string.Empty);
            if (session.AddNamedEntry(name, value) == null)
            {
                Notify(member, ColorToken.Red("Enter a name for the entry."));
                return;
            }

            QueueRefresh();
        }

        /// <summary>
        /// Removes any entry. Managers only; everyone else removes their own entry with <see cref="RemoveOwnEntry"/>.
        /// </summary>
        public static void RemoveEntry(uint member, int entryId)
        {
            var session = GetSession(member);
            if (!IsManager(session, member))
                return;

            if (session.RemoveEntry(entryId))
                QueueRefresh();
        }

        public static void Reset(uint member)
        {
            var session = GetSession(member);
            if (!IsManager(session, member))
                return;

            session.Reset();
            QueueRefresh();
        }

        public static void MoveTurn(uint member, bool forward)
        {
            var session = GetSession(member);
            if (!CanMoveTurn(session, member))
                return;

            session.MoveTurn(forward);
            QueueRefresh();
        }

        public static void ChangeStep(uint member, int delta)
        {
            var session = GetSession(member);
            if (!CanMoveTurn(session, member))
                return;

            var step = session.Step;
            session.Step = step + delta;
            if (session.Step != step)
                QueueRefresh();
        }

        /// <summary>
        /// Updates every open tracker window on the next frame. Repeated requests in one frame refresh once.
        /// </summary>
        public static void QueueRefresh()
        {
            if (_isRefreshQueued)
                return;

            _isRefreshQueued = true;
            Scheduler.Schedule(RefreshOpenWindows, TimeSpan.Zero);
        }

        /// <summary>
        /// Leaving the area takes a member off its tracker, along with their entry. Tracker windows close on
        /// area change like other windows, so only the members left behind need a refresh.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAreaExit)]
        public static void LeaveOnAreaExit()
        {
            var exiting = GetExitingObject();
            if (!GetIsPC(exiting) && !GetIsDMPossessed(exiting))
                return;

            var member = GetMember(exiting);
            var session = FindSession(member);
            if (session == null || session.Area != OBJECT_SELF)
                return;

            // A DM's avatar can stay behind while they possess a creature elsewhere, and the reverse.
            if (exiting != member && exiting != DMPresence.GetControlledCreature(member))
                return;

            Leave(member);
        }

        /// <summary>
        /// Takes a DM off a tracker when possession moves them to another area, so its other members see them leave.
        /// </summary>
        [NWNEventHandler(ScriptName.OnDMPossessBefore)]
        [NWNEventHandler(ScriptName.OnDMPossessFullPowerBefore)]
        public static void CheckAreaOnPossession()
        {
            var member = GetMember(OBJECT_SELF);
            if (!_sessionIdByMember.ContainsKey(member))
                return;

            // Possession has not settled while the event runs, so check on the next tick.
            Scheduler.Schedule(() => GetSession(member), TimeSpan.FromSeconds(0.1));
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void LeaveOnModuleExit()
        {
            var exiting = GetExitingObject();
            var member = GetMember(exiting);
            if (FindSession(member) == null)
                return;

            // The leaving player is still listed by GetFirstPC until the event finishes.
            _exitingPlayers.Add(exiting);
            Leave(member);
        }

        private static bool LeaveCurrent(uint member)
        {
            var session = FindSession(member);
            _sessionIdByMember.Remove(member);
            if (session == null)
                return false;

            session.Leave(member);
            if (session.IsEmpty)
                _sessions.Remove(session.Id);

            return true;
        }

        private static void Notify(uint member, string message)
        {
            DMPresence.SendMessage(member, message);
        }

        private static void RefreshOpenWindows()
        {
            _isRefreshQueued = false;

            var payload = new InitiativeTrackerRefreshEvent();
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (_exitingPlayers.Contains(player))
                    continue;

                if (DMPresence.IsControlledByDM(player))
                {
                    // One publish refreshes every tracker window shown on that target.
                    var dm = DMPresence.GetDM(player);
                    var controlled = DMPresence.GetControlledCreature(dm);
                    var targets = DMPresence.GetShownOn(controlled, GuiWindowType.InitiativeTracker)
                        .Concat(DMPresence.GetShownOn(controlled, GuiWindowType.InitiativeTrackerEdit))
                        .Distinct()
                        .Where(IsTrackerWindowOpen);
                    foreach (var target in targets)
                        Gui.PublishRefreshEvent(dm, payload, target);
                }
                else if (IsTrackerWindowOpen(player))
                {
                    Gui.PublishRefreshEvent(player, payload);
                }
            }

            _exitingPlayers.Clear();
        }

        private static bool IsTrackerWindowOpen(uint target)
        {
            return Gui.IsWindowOpen(target, GuiWindowType.InitiativeTracker) ||
                   Gui.IsWindowOpen(target, GuiWindowType.InitiativeTrackerEdit);
        }
    }
}
