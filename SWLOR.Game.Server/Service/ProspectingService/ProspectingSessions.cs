using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;

namespace SWLOR.Game.Server.Service.ProspectingService
{
    /// <summary>
    /// The entry point prospecting code and its window use to begin, play and end a visit. It runs <see cref="ProspectingLifecycle"/>
    /// against the game engine.
    /// </summary>
    public static class ProspectingSessions
    {
        private static IProspectingSessionsService Instance => FacadeAccessor.Resolve<IProspectingSessionsService>();

        /// <summary>Begins a session: opens the window on the node and holds the node for the player.</summary>
        public static ProspectingBegin Begin(ProspectingRequest request)
        {
            return Instance.Begin(request);
        }

        /// <summary>Starts an action; the report arrives once it has taken its seconds and resolved.</summary>
        public static ProspectingActionStart StartAction(ProspectingSession session, string actionId, Action<ProspectingActionReport> onResolved)
        {
            return Instance.StartAction(session, actionId, onResolved);
        }

        /// <summary>Rebuilds the session with the player's perks and style as they are now. False once the session has acted or ended.</summary>
        public static bool RefreshEffects(ProspectingSession session, ProspectingEffects effects)
        {
            return Instance.RefreshEffects(session, effects);
        }

        /// <summary>Starts a Quick Harvest of the node.</summary>
        public static ProspectingActionStart StartQuickHarvest(ProspectingSession session, Action<QuickHarvestReport> onProgress)
        {
            return Instance.StartQuickHarvest(session, onProgress);
        }

        /// <summary>Ends a session the player left unfinished. Does nothing once it is settled.</summary>
        public static void Abandon(ProspectingSession session, ProspectingEnd reason)
        {
            Instance.Abandon(session, reason);
        }

        /// <summary>True while the session is the player's session in progress.</summary>
        public static bool IsActive(ProspectingSession session)
        {
            return Instance.IsActive(session);
        }

        /// <summary>Checks the player now and ends the session if they moved, entered combat, died or closed the window. Returns true while it can go on.</summary>
        public static bool Revalidate(ProspectingSession session)
        {
            return Instance.Revalidate(session);
        }

        /// <summary>True when another player is working the node.</summary>
        public static bool IsInUseByAnother(uint node, uint player)
        {
            return Instance.IsInUseByAnother(node, player);
        }

        /// <summary>The XP one item pays this session.</summary>
        public static int XpPerItem(ProspectingSession session)
        {
            return Instance.XpPerItem(session);
        }

        /// <summary>A player who dies stops harvesting at once; the poll cannot be trusted to see it, because a subdued death revives them first.</summary>
        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void AbandonOnDeath()
        {
            Instance.AbandonOnDeath();
        }

        /// <summary>A player who logs out stops harvesting at once.</summary>
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void AbandonOnExit()
        {
            Instance.AbandonOnExit();
        }
    }
}
