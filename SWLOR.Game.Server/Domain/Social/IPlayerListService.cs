using System.Collections.Generic;
using SWLOR.Game.Server.Service.PlayerListService;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// Data and actions behind the player list window, which replaces the native player list: per-identity flags,
    /// rows resolved for each viewer, party controls, hostility, and tells. The static facade <c>Service.PlayerList</c>
    /// forwards to the container's instance of this interface and keeps the event handlers.
    /// </summary>
    public interface IPlayerListService
    {
        /// <summary>
        /// Returns the flags of the identity a player is showing: their active disguise's, or their own.
        /// </summary>
        IReadOnlyCollection<PlayerListFlagType> GetFlags(uint player);

        /// <summary>
        /// Turns a flag on or off for the identity a player is showing. Disguises keep their own flags so a
        /// distinctive set cannot link a disguise to its owner.
        /// </summary>
        void SetFlag(uint player, PlayerListFlagType flag, bool isEnabled);

        /// <summary>
        /// Returns everyone online with the details every viewer shares. Party sizes count player characters only.
        /// </summary>
        List<PlayerListSubject> BuildSubjects();

        /// <summary>
        /// Resolves the shared subjects into rows for one viewer: names they may see, party status, and hostility.
        /// Non-DM viewers never see DMs, canonical names of others, or anything ordered by them.
        /// </summary>
        List<PlayerListEntry> BuildEntries(uint viewer, IReadOnlyList<PlayerListSubject> subjects);

        /// <summary>
        /// Reads how a viewer currently relates to one target, straight from the engine, for checking an action
        /// at the moment it is used.
        /// </summary>
        PlayerListRelation GetRelation(uint viewer, uint target);

        /// <summary>
        /// Returns a character's party leader and how many player characters the party holds. A character
        /// outside a party, or a DM, leads a party of one.
        /// </summary>
        (uint Leader, int PlayerCount) GetParty(uint player);

        /// <summary>
        /// Whether the viewer has marked the target hostile through the player list or the native PvP attitude.
        /// </summary>
        bool IsHostileTo(uint viewer, uint target);

        void SetHostile(uint viewer, uint target, bool isHostile);

        /// <summary>
        /// Sends a tell to the character the sender picked. The target is exact, so a display name shared with
        /// another online character does not block it the way a typed tell would be.
        /// </summary>
        /// <returns>An error to show the sender, or an empty string when the tell was sent.</returns>
        string SendTell(uint sender, uint target, string message);

        /// <summary>
        /// Moves a DM, or the creature they possess, to the selected player. Written to the DM log.
        /// </summary>
        void JumpTo(uint dm, uint target);

        void Invite(uint inviter, uint target);

        void RemoveFromParty(uint member);

        bool MakeLeader(uint leader, uint member);

        /// <summary>
        /// Refreshes every open player list on the next frame. Repeated requests in one frame refresh once.
        /// </summary>
        void QueueRefresh();

        void RefreshOnEnter();

        void RefreshOnExit();

        /// <summary>
        /// Reopens an open player list on whatever a DM controls next. The client keeps the window open across
        /// possession, but its view model stays bound to the object it opened on.
        /// </summary>
        void MoveWindowOnPossession();

        void RefreshOnPartyChange();
    }
}
