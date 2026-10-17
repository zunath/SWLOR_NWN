using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PlayerListService;
using SWLOR.NWN.API.NWNX;
using NWNXLib = NWN.Native.API.NWNXLib;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Data and actions behind the player list window, which replaces the native player list:
    /// per-identity flags, rows resolved for each viewer, party controls, hostility, and tells.
    /// </summary>
    public static class PlayerList
    {
        public const string DMDescriptor = "Dungeon Master";

        private static readonly PlayerListFlagType[] KnownFlags = Enum.GetValues<PlayerListFlagType>();
        private static readonly HashSet<uint> ExitingPlayers = new();
        private static bool _isRefreshQueued;

        /// <summary>
        /// Returns the flags of the identity a player is showing: their active disguise's, or their own.
        /// </summary>
        public static IReadOnlyCollection<PlayerListFlagType> GetFlags(uint player)
        {
            if (!GetIsObjectValid(player) || DMPresence.IsControlledByDM(player))
                return Array.Empty<PlayerListFlagType>();

            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            return GetFlags(dbPlayer, Disguise.GetActiveDisguise(dbPlayer));
        }

        /// <summary>
        /// Turns a flag on or off for the identity a player is showing. Disguises keep their own flags so a
        /// distinctive set cannot link a disguise to its owner.
        /// </summary>
        public static void SetFlag(uint player, PlayerListFlagType flag, bool isEnabled)
        {
            if (!GetIsPC(player) || DMPresence.IsControlledByDM(player) || !KnownFlags.Contains(flag))
                return;

            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
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
                DB.Set(disguise);
            else
                DB.Set(dbPlayer);

            QueueRefresh();
        }

        /// <summary>
        /// Returns everyone online with the details every viewer shares. Party sizes count player characters only.
        /// </summary>
        public static List<PlayerListSubject> BuildSubjects()
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
                    var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
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

        /// <summary>
        /// Resolves the shared subjects into rows for one viewer: names they may see, party status, and hostility.
        /// Non-DM viewers never see DMs, canonical names of others, or anything ordered by them.
        /// </summary>
        public static List<PlayerListEntry> BuildEntries(uint viewer, IReadOnlyList<PlayerListSubject> subjects)
        {
            var viewerIsDM = DMPresence.IsControlledByDM(viewer);
            var viewerSubject = subjects.FirstOrDefault(subject => subject.Player == viewer);
            var entries = new List<PlayerListEntry>();

            foreach (var subject in subjects)
            {
                if (!PlayerListRules.IsVisibleTo(viewerIsDM, subject.IsDM))
                    continue;

                var relation = BuildRelation(viewer, viewerIsDM, viewerSubject, subject);
                entries.Add(new PlayerListEntry
                {
                    Player = subject.Player,
                    Name = ResolveName(viewer, viewerIsDM, subject),
                    Descriptor = subject.Descriptor,
                    IsDisguised = viewerIsDM && subject.IsDisguised,
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

        /// <summary>
        /// Reads how a viewer currently relates to one target, straight from the engine, for checking an action
        /// at the moment it is used.
        /// </summary>
        public static PlayerListRelation GetRelation(uint viewer, uint target)
        {
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

        /// <summary>
        /// Whether the viewer has marked the target hostile through the player list or the native PvP attitude.
        /// </summary>
        public static bool IsHostileTo(uint viewer, uint target)
        {
            var creature = NWNXLib.g_pAppManager.m_pServerExoApp.GetCreatureByGameObjectID(target);
            return creature != null && creature.GetPVPPlayerLikesMe(viewer) == 0;
        }

        public static void SetHostile(uint viewer, uint target, bool isHostile)
        {
            if (isHostile)
                SetPCDislike(viewer, target);
            else
                SetPCLike(viewer, target);

            QueueRefresh();
        }

        /// <summary>
        /// Sends a tell to the character the sender picked. The target is exact, so a display name shared with
        /// another online character does not block it the way a typed tell would be.
        /// </summary>
        /// <returns>An error to show the sender, or an empty string when the tell was sent.</returns>
        public static string SendTell(uint sender, uint target, string message)
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

            return string.Empty;
        }

        /// <summary>
        /// Moves a DM, or the creature they possess, to the selected player. Written to the DM log.
        /// </summary>
        public static void JumpTo(uint dm, uint target)
        {
            DMPresence.JumpTo(dm, GetLocation(DMPresence.GetControlledCreature(target)),
                $"{GetName(target)} ({GetObjectUUID(target)})");
        }

        public static void Invite(uint inviter, uint target)
        {
            PlayerPlugin.SendPartyInvite(target, inviter);
        }

        public static void RemoveFromParty(uint member)
        {
            Party.RemoveFromCurrentParty(member);
            QueueRefresh();
        }

        public static bool MakeLeader(uint leader, uint member)
        {
            var transferred = Party.TransferLeadership(leader, member);
            QueueRefresh();
            return transferred;
        }

        /// <summary>
        /// Refreshes every open player list on the next frame. Repeated requests in one frame refresh once.
        /// </summary>
        public static void QueueRefresh()
        {
            if (_isRefreshQueued)
                return;

            _isRefreshQueued = true;
            Scheduler.Schedule(RefreshOpenWindows, TimeSpan.Zero);
        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void RefreshOnEnter()
        {
            QueueRefresh();
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void RefreshOnExit()
        {
            // The leaving player is still listed by GetFirstPC until the event finishes.
            ExitingPlayers.Add(GetExitingObject());
            QueueRefresh();
        }

        [NWNEventHandler(ScriptName.OnPartyAcceptInvitationAfter)]
        [NWNEventHandler(ScriptName.OnPartyLeaveAfter)]
        [NWNEventHandler(ScriptName.OnPartyKickAfter)]
        [NWNEventHandler(ScriptName.OnPartyTransferLeadershipAfter)]
        public static void RefreshOnPartyChange()
        {
            QueueRefresh();
        }

        private static void RefreshOpenWindows()
        {
            _isRefreshQueued = false;

            var windows = new List<(uint Owner, uint Target)>();
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (ExitingPlayers.Contains(player))
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

            ExitingPlayers.Clear();
        }

        private static IReadOnlyCollection<PlayerListFlagType> GetFlags(Player dbPlayer, PlayerDisguise disguise)
        {
            var flags = disguise != null ? disguise.PlayerListFlags : dbPlayer?.PlayerListFlags;
            if (flags == null || flags.Count == 0)
                return Array.Empty<PlayerListFlagType>();

            return KnownFlags.Where(flags.Contains).ToList();
        }

        private static List<uint> GetOnlinePlayers()
        {
            var players = new List<uint>();
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (!ExitingPlayers.Contains(player))
                    players.Add(player);
            }

            return players;
        }

        // Player characters by their party leader. A player with no party leads their own party of one.
        private static Dictionary<uint, uint> GetPartyLeaders()
        {
            var leaders = new Dictionary<uint, uint>();
            foreach (var player in GetOnlinePlayers())
            {
                if (DMPresence.IsControlledByDM(player))
                    continue;

                var leader = GetFactionLeader(player);
                leaders[player] = GetIsObjectValid(leader) ? leader : player;
            }

            return leaders;
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

        private static PlayerListRelation BuildRelation(
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
                HasTarget = true,
                TargetIsSelf = isSelf,
                TargetIsDM = subject.IsDM,
                TargetIsInViewerParty = isInViewerParty,
                TargetIsInOtherParty = !isSelf && !isInViewerParty && !subject.IsDM && subject.PartyPlayerCount >= 2,
                TargetIsPartyLeader = !subject.IsDM && subject.PartyPlayerCount >= 2 && subject.PartyLeader == subject.Player,
                ViewerDislikesTarget = !isSelf && !viewerIsDM && !subject.IsDM && IsHostileTo(viewer, subject.Player),
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
