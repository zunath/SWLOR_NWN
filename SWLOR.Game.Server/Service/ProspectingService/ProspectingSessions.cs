using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.Game.Server.Service.ProspectingService
{
    /// <summary>
    /// The entry point harvesters and the Prospecting window use to begin, run and end a session. It runs <see cref="ProspectingLifecycle"/>
    /// against the game engine.
    /// </summary>
    public static class ProspectingSessions
    {
        private static readonly ProspectingLifecycle Lifecycle =
            new(NwnProspectingWorld.Instance, ServiceMinigameRandom.Instance);

        /// <summary>Begins a session: opens the window on the node and holds the node for the player.</summary>
        public static ProspectingBegin Begin(ProspectingRequest request) => Lifecycle.Begin(request);

        /// <summary>Starts an action; the report arrives once it has taken its seconds and resolved.</summary>
        public static ProspectingActionStart StartAction(
            ProspectingSession session, string actionId, Action<ProspectingActionReport> onResolved) =>
            Lifecycle.StartAction(session, actionId, onResolved);

        /// <summary>Starts a Quick Harvest of the node.</summary>
        public static ProspectingActionStart StartQuickHarvest(ProspectingSession session, Action<QuickHarvestReport> onProgress) =>
            Lifecycle.StartQuickHarvest(session, onProgress);

        /// <summary>Ends a session the player left unfinished. Does nothing once it is settled.</summary>
        public static void Abandon(ProspectingSession session, ProspectingEnd reason) => Lifecycle.Abandon(session, reason);

        /// <summary>True while the session is the player's session in progress.</summary>
        public static bool IsActive(ProspectingSession session) => Lifecycle.IsActive(session);

        /// <summary>Checks the player now and ends the session if they moved, entered combat, died or closed the window. Returns true while it can go on.</summary>
        public static bool Revalidate(ProspectingSession session) => Lifecycle.Revalidate(session);

        /// <summary>True when another player is working the node.</summary>
        public static bool IsInUseByAnother(uint node, uint player) => Lifecycle.IsInUseByAnother(node, player);

        /// <summary>The XP one item pays this session.</summary>
        public static int XpPerItem(ProspectingSession session) => Lifecycle.XpPerItem(session);

        /// <summary>A player who dies stops harvesting at once. The poll cannot be trusted to see it: a death that is subdued revives the player before the next check.</summary>
        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void AbandonOnDeath()
        {
            var player = GetLastPlayerDied();
            Lifecycle.Abandon(Lifecycle.GetActive(player), ProspectingEnd.Died);
        }

        /// <summary>A player who logs out stops harvesting at once, and the node is released.</summary>
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void AbandonOnExit()
        {
            var player = GetExitingObject();
            Lifecycle.Abandon(Lifecycle.GetActive(player), ProspectingEnd.Left);
        }
    }
}
