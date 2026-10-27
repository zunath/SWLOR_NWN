using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Service.PlayerListService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Data and actions behind the player list window, which replaces the native player list:
    /// per-identity flags, rows resolved for each viewer, party controls, hostility, and tells.
    /// The logic and state live in <see cref="IPlayerListService"/>; every member here forwards to the container's
    /// instance, and the event handlers stay here so their names in the handler order manifest do not change.
    /// </summary>
    public static class PlayerList
    {
        // Compile-time constant: callers get the value inlined, as before.
        public const string DMDescriptor = Domain.Social.PlayerListService.DMDescriptor;

        private static IPlayerListService Instance => FacadeAccessor.Resolve<IPlayerListService>();

        /// <summary>
        /// Returns the flags of the identity a player is showing: their active disguise's, or their own.
        /// </summary>
        public static IReadOnlyCollection<PlayerListFlagType> GetFlags(uint player)
        {
            return Instance.GetFlags(player);
        }

        /// <summary>
        /// Turns a flag on or off for the identity a player is showing. Disguises keep their own flags so a
        /// distinctive set cannot link a disguise to its owner.
        /// </summary>
        public static void SetFlag(uint player, PlayerListFlagType flag, bool isEnabled)
        {
            Instance.SetFlag(player, flag, isEnabled);
        }

        /// <summary>
        /// Returns everyone online with the details every viewer shares. Party sizes count player characters only.
        /// </summary>
        public static List<PlayerListSubject> BuildSubjects()
        {
            return Instance.BuildSubjects();
        }

        /// <summary>
        /// Resolves the shared subjects into rows for one viewer: names they may see, party status, and hostility.
        /// Non-DM viewers never see DMs, canonical names of others, or anything ordered by them.
        /// </summary>
        public static List<PlayerListEntry> BuildEntries(uint viewer, IReadOnlyList<PlayerListSubject> subjects)
        {
            return Instance.BuildEntries(viewer, subjects);
        }

        /// <summary>
        /// Reads how a viewer currently relates to one target, straight from the engine, for checking an action
        /// at the moment it is used.
        /// </summary>
        public static PlayerListRelation GetRelation(uint viewer, uint target)
        {
            return Instance.GetRelation(viewer, target);
        }

        /// <summary>
        /// Returns a character's party leader and how many player characters the party holds. A character
        /// outside a party, or a DM, leads a party of one.
        /// </summary>
        public static (uint Leader, int PlayerCount) GetParty(uint player)
        {
            return Instance.GetParty(player);
        }

        /// <summary>
        /// Whether the viewer has marked the target hostile through the player list or the native PvP attitude.
        /// </summary>
        public static bool IsHostileTo(uint viewer, uint target)
        {
            return Instance.IsHostileTo(viewer, target);
        }

        public static void SetHostile(uint viewer, uint target, bool isHostile)
        {
            Instance.SetHostile(viewer, target, isHostile);
        }

        /// <summary>
        /// Sends a tell to the character the sender picked. The target is exact, so a display name shared with
        /// another online character does not block it the way a typed tell would be.
        /// </summary>
        /// <returns>An error to show the sender, or an empty string when the tell was sent.</returns>
        public static string SendTell(uint sender, uint target, string message)
        {
            return Instance.SendTell(sender, target, message);
        }

        /// <summary>
        /// Moves a DM, or the creature they possess, to the selected player. Written to the DM log.
        /// </summary>
        public static void JumpTo(uint dm, uint target)
        {
            Instance.JumpTo(dm, target);
        }

        public static void Invite(uint inviter, uint target)
        {
            Instance.Invite(inviter, target);
        }

        public static void RemoveFromParty(uint member)
        {
            Instance.RemoveFromParty(member);
        }

        public static bool MakeLeader(uint leader, uint member)
        {
            return Instance.MakeLeader(leader, member);
        }

        /// <summary>
        /// Refreshes every open player list on the next frame. Repeated requests in one frame refresh once.
        /// </summary>
        public static void QueueRefresh()
        {
            Instance.QueueRefresh();
        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void RefreshOnEnter()
        {
            Instance.RefreshOnEnter();
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void RefreshOnExit()
        {
            Instance.RefreshOnExit();
        }

        /// <summary>
        /// Reopens an open player list on whatever a DM controls next. The client keeps the window open across
        /// possession, but its view model stays bound to the object it opened on.
        /// </summary>
        [NWNEventHandler(ScriptName.OnDMPossessBefore)]
        [NWNEventHandler(ScriptName.OnDMPossessFullPowerBefore)]
        public static void MoveWindowOnPossession()
        {
            Instance.MoveWindowOnPossession();
        }

        [NWNEventHandler(ScriptName.OnPartyAcceptInvitationAfter)]
        [NWNEventHandler(ScriptName.OnPartyLeaveAfter)]
        [NWNEventHandler(ScriptName.OnPartyKickAfter)]
        [NWNEventHandler(ScriptName.OnPartyTransferLeadershipAfter)]
        public static void RefreshOnPartyChange()
        {
            Instance.RefreshOnPartyChange();
        }
    }
}
