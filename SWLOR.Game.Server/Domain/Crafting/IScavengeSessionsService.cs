using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;
using SWLOR.Game.Server.Service.ScavengeService;

namespace SWLOR.Game.Server.Domain.Crafting
{
    /// <summary>
    /// The entry point the scavenge code and its window use to begin, play and end a visit. It runs <see cref="ScavengeLifecycle"/>
    /// against the game engine. The static facade <c>Service.ScavengeService.ScavengeSessions</c> forwards to it and keeps the event handlers.
    /// </summary>
    public interface IScavengeSessionsService
    {
        /// <summary>Begins a visit. Returns the session, or null when another scavenger is at the site.</summary>
        ScavengeSession Start(ScavengeRequest request);

        /// <summary>True while the site has a visit in progress.</summary>
        bool IsInUse(uint site);

        /// <summary>The visit in progress at a site, or null.</summary>
        ScavengeSession GetActiveAt(uint site);

        bool IsActive(ScavengeSession session);

        /// <summary>True while the scavenger may still choose Quick Search.</summary>
        bool CanQuickSearch(ScavengeSession session);

        /// <summary>Resolves the site by the rules scavenging had before the board. Returns null when it was refused.</summary>
        ScavengeResult QuickSearch(ScavengeSession session);

        /// <summary>Rebuilds the board's rules with the scavenger's perks and style as they are now. False once the visit has acted or ended.</summary>
        bool RefreshEffects(ScavengeSession session, SearchEffects effects);

        /// <summary>Performs one action of the board.</summary>
        ScavengeStep Act(ScavengeSession session, string actionId);

        /// <summary>Ends the visit because the scavenger left it. Does nothing once the visit is settled.</summary>
        void Abandon(ScavengeSession session, ScavengeEnd reason);

        /// <summary>Checks the scavenger now and ends the visit if they moved, entered combat, died or closed the window.</summary>
        bool Revalidate(ScavengeSession session);

        /// <summary>The seconds the scavenger must wait before their next action may resolve; 0 when it may now.</summary>
        double SecondsUntilNextAction(ScavengeSession session);

        /// <summary>A player who dies stops at once.</summary>
        void AbandonOnDeath();

        /// <summary>A player who logs out stops at once.</summary>
        void AbandonOnExit();
    }
}
