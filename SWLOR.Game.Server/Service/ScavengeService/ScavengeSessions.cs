using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;

namespace SWLOR.Game.Server.Service.ScavengeService
{
    /// <summary>
    /// The entry point scavenge sites and the Search window use to start, play and end a visit. It runs <see cref="ScavengeLifecycle"/>
    /// against the game engine.
    /// </summary>
    public static class ScavengeSessions
    {
        private static readonly ScavengeLifecycle Lifecycle =
            new(NwnScavengeWorld.Instance, ServiceMinigameRandom.Instance);

        /// <summary>Begins a visit. Returns the session, or null when another scavenger is at the site.</summary>
        public static ScavengeSession Start(ScavengeRequest request) => Lifecycle.Start(request);

        /// <summary>True while the site has a visit in progress.</summary>
        public static bool IsInUse(uint site) => Lifecycle.IsInUse(site);

        /// <summary>The visit in progress at a site, or null.</summary>
        public static ScavengeSession GetActiveAt(uint site) => Lifecycle.GetActiveAt(site);

        public static bool IsActive(ScavengeSession session) => Lifecycle.IsActive(session);

        /// <summary>True while the scavenger may still choose Quick Search.</summary>
        public static bool CanQuickSearch(ScavengeSession session) => Lifecycle.CanQuickSearch(session);

        /// <summary>Resolves the site by the rules scavenging had before the board. Returns null when it was refused.</summary>
        public static ScavengeResult QuickSearch(ScavengeSession session) => Lifecycle.QuickSearch(session);

        /// <summary>Rebuilds the board's rules with the scavenger's perks and style as they are now. False once the visit has acted or ended.</summary>
        public static bool RefreshEffects(ScavengeSession session, SearchEffects effects) => Lifecycle.RefreshEffects(session, effects);

        /// <summary>Performs one action of the board.</summary>
        public static ScavengeStep Act(ScavengeSession session, string actionId) => Lifecycle.Act(session, actionId);

        /// <summary>Ends the visit because the scavenger left it. Does nothing once the visit is settled.</summary>
        public static void Abandon(ScavengeSession session, ScavengeEnd reason) => Lifecycle.Abandon(session, reason);

        /// <summary>Checks the scavenger now and ends the visit if they moved, entered combat, died or closed the window.</summary>
        public static bool Revalidate(ScavengeSession session) => Lifecycle.Revalidate(session);

        /// <summary>The seconds the scavenger must wait before their next action may resolve; 0 when it may now.</summary>
        public static double SecondsUntilNextAction(ScavengeSession session) => Lifecycle.SecondsUntilNextAction(session);

        /// <summary>A scavenger who dies stops searching at once; the poll cannot be trusted to see it, because a subdued death revives them first.</summary>
        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void AbandonOnDeath()
        {
            var player = GetLastPlayerDied();
            Lifecycle.Abandon(Lifecycle.GetActive(player), ScavengeEnd.Died);
        }

        /// <summary>A scavenger who logs out stops searching at once.</summary>
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void AbandonOnExit()
        {
            var player = GetExitingObject();
            Lifecycle.Abandon(Lifecycle.GetActive(player), ScavengeEnd.Left);
        }
    }
}
