using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;
using SWLOR.Game.Server.Service.ProspectingService;

namespace SWLOR.Game.Server.Domain.Crafting
{
    /// <summary>
    /// The entry point the prospecting code and its window use to begin, play and end a visit. It runs <see cref="ProspectingLifecycle"/>
    /// against the game engine. The static facade <c>Service.ProspectingService.ProspectingSessions</c> forwards to it and keeps the event handlers.
    /// </summary>
    public interface IProspectingSessionsService
    {
        /// <summary>Begins a session: opens the window on the node and holds the node for the player.</summary>
        ProspectingBegin Begin(ProspectingRequest request);

        /// <summary>Starts an action; the report arrives once it has taken its seconds and resolved.</summary>
        ProspectingActionStart StartAction(ProspectingSession session, string actionId, Action<ProspectingActionReport> onResolved);

        /// <summary>Rebuilds the session with the player's perks and style as they are now. False once the session has acted or ended.</summary>
        bool RefreshEffects(ProspectingSession session, ProspectingEffects effects);

        /// <summary>Starts a Quick Harvest of the node.</summary>
        ProspectingActionStart StartQuickHarvest(ProspectingSession session, Action<QuickHarvestReport> onProgress);

        /// <summary>Ends a session the player left unfinished. Does nothing once it is settled.</summary>
        void Abandon(ProspectingSession session, ProspectingEnd reason);

        /// <summary>True while the session is the player's session in progress.</summary>
        bool IsActive(ProspectingSession session);

        /// <summary>Checks the player now and ends the session if they moved, entered combat, died or closed the window. Returns true while it can go on.</summary>
        bool Revalidate(ProspectingSession session);

        /// <summary>True when another player is working the node.</summary>
        bool IsInUseByAnother(uint node, uint player);

        /// <summary>The XP one item pays this session.</summary>
        int XpPerItem(ProspectingSession session);

        /// <summary>A player who dies stops at once.</summary>
        void AbandonOnDeath();

        /// <summary>A player who logs out stops at once.</summary>
        void AbandonOnExit();
    }
}
