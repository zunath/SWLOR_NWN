using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PlayerListService;
using SWLOR.NWN.API.NWNX;
using NWNXLib = NWN.Native.API.NWNXLib;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// The player list: the logic and state that used to be static in <c>Service.PlayerList</c>, moved here unchanged.
    /// The facade still carries the event handlers and forwards every call to the container's instance.
    /// </summary>
    /// <remarks>
    /// The two places that read the engine's native party and PvP data (<see cref="IsHostileTo"/> and
    /// <see cref="GetPartyFactionId"/>) are <c>virtual</c> so a unit test can answer them without a running server;
    /// the implementations here are the original reads.
    /// </remarks>
    public class PlayerListService : IPlayerListService
    {
        /// <summary>The descriptor shown for a DM. <c>Service.PlayerList.DMDescriptor</c> repeats it.</summary>
        public const string DMDescriptor = "Dungeon Master";

        private readonly IDBService _db;
        private readonly IPartyService _party;
        private readonly PlayerListFlagType[] _knownFlags = Enum.GetValues<PlayerListFlagType>();
        private readonly HashSet<uint> _exitingPlayers = new();
        private bool _isRefreshQueued;

        public PlayerListService(IDBService db, IPartyService party)
        {
            _db = db;
            _party = party;
        }

        public IReadOnlyCollection<PlayerListFlagType> GetFlags(uint player)
        {
            if (!GetIsObjectValid(player) || DMPresence.IsControlledByDM(player))
                return Array.Empty<PlayerListFlagType>();

            var dbPlayer = _db.Get<Player>(GetObjectUUID(player));
            return GetFlags(dbPlayer, Disguise.GetActiveDisguise(dbPlayer));
        }

        public void SetFlag(uint player, PlayerListFlagType flag, bool isEnabled)
        {
            if (!GetIsPC(player) || DMPresence.IsControlledByDM(player) || !_knownFlags.Contains(flag))
                return;

            var dbPlayer = _db.Get<Player>(GetObjectUUID(player));
            if (dbPlayer == null)
                return;

            var disguise = Disguise.GetActiveDisguise(dbPlayer);
            var flags = disguise != null
                ? disguise.PlayerListFlags ??= new List<PlayerListFlagType>()
                : dbPlayer.PlayerListFlags ??= new List<PlayerListFlagType>();

            if (isEnabled == flags.Contains(flag))
                return;

            if (isEnabled)
                flags.Add(flag);
            else
                flags.RemoveAll(existing => existing == flag);

            if (disguise != null)
                _db.Set(disguise);
            else
                _db.Set(dbPlayer);

            QueueRefresh();
        }

        public List<PlayerListSubject> BuildSubjects()
        {
            var subjects = new List<PlayerListSubject>();
            var partyLeaders = GetPartyLeaders();
            var partySizes = CountPartySizes(partyLeaders);

            foreach (var player in GetOnlinePlayers())
            {
                var subject = new PlayerListSubject
                {
                    Player = player,
                    CanonicalName = GetName(player),
                    IsAfk = PlayerName.IsAfk(player),
                };

                if (DMPresence.IsControlledByDM(player))
                {
                    subject.IsDM = true;
                    subject.Descriptor = DMDescriptor;
                    subject.Flags = Array.Empty<PlayerListFlagType>();
                    subject.PartyLeader = player;
                    subject.PartyPlayerCount = 1;
                }
                else
                {
                    var dbPlayer = _db.Get<Player>(GetObjectUUID(player));
                    var disguise = Disguise.GetActiveDisguise(dbPlayer);
                    subject.Descriptor = Disguise.GetDisplayDescriptor(player);
                    subject.IsDisguised = disguise != null;
                    subject.Flags = GetFlags(dbPlayer, disguise);
                    subject.PartyLeader = partyLeaders[player];
                    subject.PartyPlayerCount = partySizes[subject.PartyLeader];
                }

                subjects.Add(subject);
            }

            return subjects;
        }

        public List<PlayerListEntry> BuildEntries(uint viewer, IReadOnlyList<PlayerListSubject> subjects)
        {
            // A possessing DM's window belongs to the creature, but the roster lists their avatar.
            viewer = DMPresence.GetDM(viewer);
            var viewerIsDM = DMPresence.IsControlledByDM(viewer);
            var viewerSubject = subjects.FirstOrDefault(subject => subject.Player == viewer);
            var showOwnDescriptor = viewerIsDM || PlayerName.ShouldShowOwnDescriptor(viewer);
            var showNamedDescriptors = viewerIsDM || PlayerName.ShouldShowDescriptorForNamedPlayers(viewer);
            var entries = new List<PlayerListEntry>();

            foreach (var subject in subjects)
            {
                if (!PlayerListRules.IsVisibleTo(viewerIsDM, subject.IsDM))
                    continue;

                var relation = BuildRelation(viewer, viewerIsDM, viewerSubject, subject);
                var name = ResolveName(viewer, viewerIsDM, subject);
                var showsDescriptor = PlayerListRules.ShowsDescriptor(viewerIsDM, relation.TargetIsSelf,
                    !string.IsNullOrEmpty(name), showOwnDescriptor, showNamedDescriptors);
                entries.Add(new PlayerListEntry
                {
                    Player = subject.Player,
                    Name = name,
                    Descriptor = showsDescriptor ? subject.Descriptor : string.Empty,
                    IsDisguised = viewerIsDM && subject.IsDisguised,
                    IsAfk = subject.IsAfk,
                    Flags = subject.Flags,
                    Relation = relation,
                    Status = PlayerListRules.GetStatus(relation),
                });
            }

            return entries
                .OrderBy(entry => GetSortGroup(entry))
                .ThenBy(entry => string.IsNullOrEmpty(entry.Name) ? entry.Descriptor : entry.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public PlayerListRelation GetRelation(uint viewer, uint target)
        {
            viewer = DMPresence.GetDM(viewer);
            target = DMPresence.GetDM(target);
            var viewerIsDM = DMPresence.IsControlledByDM(viewer);
            var online = GetOnlinePlayers();
            if (!online.Contains(target) || !PlayerListRules.IsVisibleTo(viewerIsDM, DMPresence.IsControlledByDM(target)))
                return new PlayerListRelation { ViewerIsDM = viewerIsDM };

            var partyLeaders = GetPartyLeaders();
            var partySizes = CountPartySizes(partyLeaders);
            return BuildRelation(viewer,
                viewerIsDM,
                ToPartySubject(viewer, partyLeaders, partySizes),
                ToPartySubject(target, partyLeaders, partySizes));
        }

        public (uint Leader, int PlayerCount) GetParty(uint player)
        {
            var partyLeaders = GetPartyLeaders();
            if (!partyLeaders.TryGetValue(player, out var leader))
                return (player, 1);

            return (leader, CountPartySizes(partyLeaders)[leader]);
        }

        public virtual bool IsHostileTo(uint viewer, uint target)
        {
            var creature = NWNXLib.g_pAppManager.m_pServerExoApp.GetCreatureByGameObjectID(target);
            return creature != null && creature.GetPVPPlayerLikesMe(viewer) == 0;
        }

        public void SetHostile(uint viewer, uint target, bool isHostile)
        {
            if (isHostile)
                SetPCDislike(viewer, target);
            else
                SetPCLike(viewer, target);

            QueueRefresh();
        }

        public string SendTell(uint sender, uint target, string message)
        {
            message = string.Join(' ', (message ?? string.Empty)
                .Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
            if (string.IsNullOrEmpty(message))
                return "Enter a message to send.";

            var senderIsDM = DMPresence.IsControlledByDM(sender);
            // A hidden DM gets the same answer as an absent player, so the reply reveals nothing.
            if (!GetOnlinePlayers().Contains(target) ||
                !PlayerListRules.IsVisibleTo(senderIsDM, DMPresence.IsControlledByDM(target)))
                return "That player is no longer online.";

            var speaker = senderIsDM ? DMPresence.GetDM(sender) : sender;
            var channel = senderIsDM ? ChatChannel.DMTell : ChatChannel.PlayerTell;

            SetLocalBool(speaker, PlayerName.DirectTellVariable, true);
            try
            {
                ChatPlugin.SendMessage(channel, message, speaker, DMPresence.GetControlledCreature(target));
            }
            finally
            {
                DeleteLocalBool(speaker, PlayerName.DirectTellVariable);
            }

            // Matches the DM log entry the Player Search window writes for its private messages.
            if (senderIsDM)
                Log.Write(LogGroup.DM, $"{DMPresence.DescribeDM(sender)} messaged {GetName(target)} ({GetObjectUUID(target)}): {message}");

            return string.Empty;
        }

        public void JumpTo(uint dm, uint target)
        {
            DMPresence.JumpTo(dm, GetLocation(DMPresence.GetControlledCreature(target)),
                $"{GetName(target)} ({GetObjectUUID(target)})");
        }

        public void Invite(uint inviter, uint target)
        {
            PlayerPlugin.SendPartyInvite(target, inviter);
        }

        public void RemoveFromParty(uint member)
        {
            _party.RemoveFromCurrentParty(member);
            QueueRefresh();
        }

        public bool MakeLeader(uint leader, uint member)
        {
            var transferred = _party.TransferLeadership(leader, member);
            QueueRefresh();
            return transferred;
        }

        public void QueueRefresh()
        {
            if (_isRefreshQueued)
                return;

            _isRefreshQueued = true;
            Scheduler.Schedule(RefreshOpenWindows, TimeSpan.Zero);
        }

        public void RefreshOnEnter()
        {
            QueueRefresh();
        }

        public void RefreshOnExit()
        {
            // The leaving player is still listed by GetFirstPC until the event finishes.
            _exitingPlayers.Add(GetExitingObject());
            QueueRefresh();
        }

        public void MoveWindowOnPossession()
        {
            // Possessing starts on the avatar; returning to it starts on the possessed creature.
            var current = OBJECT_SELF;
            if (!GetIsDM(current) && !GetIsDMPossessed(current))
                return;

            if (!Gui.IsWindowOpen(current, GuiWindowType.PlayerList))
                return;

            var dm = DMPresence.GetDM(current);
            // Possession has not settled while the event runs, so follow it on the next tick.
            DelayCommand(0.1f, () =>
            {
                DMPresence.CloseWindow(dm, GuiWindowType.PlayerList);
                DMPresence.ShowWindow(dm, GuiWindowType.PlayerList);
            });
        }

        public void RefreshOnPartyChange()
        {
            QueueRefresh();
        }

        private void RefreshOpenWindows()
        {
            _isRefreshQueued = false;

            var windows = new List<(uint Owner, uint Target)>();
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (_exitingPlayers.Contains(player))
                    continue;

                if (DMPresence.IsControlledByDM(player))
                {
                    var dm = DMPresence.GetDM(player);
                    foreach (var target in DMPresence.GetShownOn(DMPresence.GetControlledCreature(dm), GuiWindowType.PlayerList))
                    {
                        if (Gui.IsWindowOpen(target, GuiWindowType.PlayerList))
                            windows.Add((dm, target));
                    }
                }
                else if (Gui.IsWindowOpen(player, GuiWindowType.PlayerList))
                {
                    windows.Add((player, player));
                }
            }

            if (windows.Count > 0)
            {
                var payload = new PlayerListRefreshEvent(BuildSubjects());
                foreach (var (owner, target) in windows)
                    Gui.PublishRefreshEvent(owner, payload, target);
            }

            _exitingPlayers.Clear();
        }

        private IReadOnlyCollection<PlayerListFlagType> GetFlags(Player dbPlayer, PlayerDisguise disguise)
        {
            var flags = disguise != null ? disguise.PlayerListFlags : dbPlayer?.PlayerListFlags;
            if (flags == null || flags.Count == 0)
                return Array.Empty<PlayerListFlagType>();

            return _knownFlags.Where(flags.Contains).ToList();
        }

        private List<uint> GetOnlinePlayers()
        {
            var players = new List<uint>();
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (!_exitingPlayers.Contains(player))
                    players.Add(player);
            }

            return players;
        }

        // Player characters by their party leader. A player with no party leads their own party of one.
        private Dictionary<uint, uint> GetPartyLeaders()
        {
            var memberships = GetOnlinePlayers()
                .Select(player => new PlayerListPartyMembership
                {
                    Player = player,
                    IsDM = DMPresence.IsControlledByDM(player),
                    PartyFactionId = GetPartyFactionId(player),
                    FactionLeader = GetFactionLeader(player),
                })
                .ToList();

            return PlayerListRules.GetPartyLeaders(memberships);
        }

        // The player-party faction whose member list holds the character. NPC factions are not parties, and
        // a DM's Set Faction rewrites a creature's faction id without moving it between member lists.
        protected virtual int? GetPartyFactionId(uint creature)
        {
            var faction = NWNXLib.g_pAppManager.m_pServerExoApp.GetCreatureByGameObjectID(creature)?.GetFaction();
            if (faction == null ||
                faction.GetFactionManager().GetIsNPCFaction(faction.m_nFactionId) != 0 ||
                !faction.m_listFactionMembers.Contains(creature))
                return null;

            return faction.m_nFactionId;
        }

        private static PlayerListSubject ToPartySubject(
            uint player,
            Dictionary<uint, uint> partyLeaders,
            Dictionary<uint, int> partySizes)
        {
            var isDM = !partyLeaders.TryGetValue(player, out var leader);
            return new PlayerListSubject
            {
                Player = player,
                IsDM = isDM,
                PartyLeader = isDM ? player : leader,
                PartyPlayerCount = isDM ? 1 : partySizes[leader],
            };
        }

        private static Dictionary<uint, int> CountPartySizes(Dictionary<uint, uint> partyLeaders)
        {
            var sizes = new Dictionary<uint, int>();
            foreach (var leader in partyLeaders.Values)
                sizes[leader] = sizes.TryGetValue(leader, out var count) ? count + 1 : 1;

            return sizes;
        }

        private PlayerListRelation BuildRelation(
            uint viewer,
            bool viewerIsDM,
            PlayerListSubject viewerSubject,
            PlayerListSubject subject)
        {
            var isSelf = subject.Player == viewer;
            var viewerIsInParty = !viewerIsDM && viewerSubject is { PartyPlayerCount: >= 2 };
            var isInViewerParty = !isSelf &&
                                  viewerIsInParty &&
                                  !subject.IsDM &&
                                  subject.PartyLeader == viewerSubject.PartyLeader;

            return new PlayerListRelation
            {
                ViewerIsDM = viewerIsDM,
                ViewerIsInParty = viewerIsInParty,
                ViewerIsPartyLeader = viewerIsInParty && viewerSubject.PartyLeader == viewer,
                ViewerIsInSpace = Service.Space.IsPlayerInSpaceMode(viewer),
                ViewerCallState = viewerIsDM ? PlayerListCallState.None : HoloCom.GetCallState(viewer),
                HasTarget = true,
                TargetIsSelf = isSelf,
                TargetIsDM = subject.IsDM,
                TargetIsInViewerParty = isInViewerParty,
                TargetIsInOtherParty = !isSelf && !isInViewerParty && !subject.IsDM && subject.PartyPlayerCount >= 2,
                TargetIsPartyLeader = !subject.IsDM && subject.PartyPlayerCount >= 2 && subject.PartyLeader == subject.Player,
                ViewerDislikesTarget = !isSelf && !viewerIsDM && !subject.IsDM && IsHostileTo(viewer, subject.Player),
                TargetIsInSpace = !subject.IsDM && Service.Space.IsPlayerInSpaceMode(subject.Player),
                TargetIsInCall = !subject.IsDM && HoloCom.GetCallState(subject.Player) != PlayerListCallState.None,
            };
        }

        // Staff see canonical names. Players see their own name and the names they assigned, and nothing else.
        private static string ResolveName(uint viewer, bool viewerIsDM, PlayerListSubject subject)
        {
            if (viewerIsDM || subject.Player == viewer)
                return subject.CanonicalName;

            return PlayerName.TryGetKnownName(viewer, subject.Player, out var knownName)
                ? PlayerName.SanitizeKnownName(knownName)
                : string.Empty;
        }

        private static int GetSortGroup(PlayerListEntry entry)
        {
            if (entry.Relation.TargetIsSelf)
                return 0;
            if (entry.Relation.TargetIsInViewerParty)
                return 1;
            if (entry.Relation.TargetIsDM)
                return 4;

            return string.IsNullOrEmpty(entry.Name) ? 3 : 2;
        }
    }
}
