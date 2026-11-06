using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;

namespace SWLOR.Game.Server.Service.ScavengeService
{
    /// <summary>
    /// The entry point scavenge code and its window use to begin, play and end a visit. It runs <see cref="ScavengeLifecycle"/>
    /// against the game engine.
    /// </summary>
    public static class ScavengeSessions
    {
        private static IScavengeSessionsService Instance => FacadeAccessor.Resolve<IScavengeSessionsService>();

        /// <summary>Begins a visit. Returns the session, or null when another scavenger is at the site.</summary>
        public static ScavengeSession Start(ScavengeRequest request)
        {
            return Instance.Start(request);
        }

        /// <summary>True while the site has a visit in progress.</summary>
        public static bool IsInUse(uint site)
        {
            return Instance.IsInUse(site);
        }

        /// <summary>The visit in progress at a site, or null.</summary>
        public static ScavengeSession GetActiveAt(uint site)
        {
            return Instance.GetActiveAt(site);
        }

        public static bool IsActive(ScavengeSession session)
        {
            return Instance.IsActive(session);
        }

        /// <summary>True while the scavenger may still choose Quick Search.</summary>
        public static bool CanQuickSearch(ScavengeSession session)
        {
            return Instance.CanQuickSearch(session);
        }

        /// <summary>Resolves the site by the rules scavenging had before the board. Returns null when it was refused.</summary>
        public static ScavengeResult QuickSearch(ScavengeSession session)
        {
            return Instance.QuickSearch(session);
        }

        /// <summary>Rebuilds the board's rules with the scavenger's perks and style as they are now. False once the visit has acted or ended.</summary>
        public static bool RefreshEffects(ScavengeSession session, SearchEffects effects)
        {
            return Instance.RefreshEffects(session, effects);
        }

        /// <summary>Performs one action of the board.</summary>
        public static ScavengeStep Act(ScavengeSession session, string actionId)
        {
            return Instance.Act(session, actionId);
        }

        /// <summary>Ends the visit because the scavenger left it. Does nothing once the visit is settled.</summary>
        public static void Abandon(ScavengeSession session, ScavengeEnd reason)
        {
            Instance.Abandon(session, reason);
        }

        /// <summary>Checks the scavenger now and ends the visit if they moved, entered combat, died or closed the window.</summary>
        public static bool Revalidate(ScavengeSession session)
        {
            return Instance.Revalidate(session);
        }

        /// <summary>The seconds the scavenger must wait before their next action may resolve; 0 when it may now.</summary>
        public static double SecondsUntilNextAction(ScavengeSession session)
        {
            return Instance.SecondsUntilNextAction(session);
        }

        /// <summary>A scavenger who dies stops searching at once; the poll cannot be trusted to see it, because a subdued death revives them first.</summary>
        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void AbandonOnDeath()
        {
            Instance.AbandonOnDeath();
        }

        /// <summary>A scavenger who logs out stops searching at once.</summary>
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void AbandonOnExit()
        {
            Instance.AbandonOnExit();
        }
    }
}
