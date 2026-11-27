using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Crafting.Minigame;
using SWLOR.Game.Server.Domain.Crafting.Minigame.Search;
using SWLOR.Game.Server.Domain.Crafting;

namespace SWLOR.Game.Server.Domain.Crafting
{
    /// <inheritdoc cref="IScavengeSessionsService" />
    public class ScavengeSessionsService : IScavengeSessionsService
    {
        private readonly IScavengeService _scavenge;
        private ScavengeLifecycle _lifecycle;

        public ScavengeSessionsService(IScavengeService scavenge)
        {
            _scavenge = scavenge;
        }

        /// <summary>The rules of a visit, run against the engine world. It keeps the visits in progress, so there is one.</summary>
        protected ScavengeLifecycle Lifecycle => _lifecycle ??= CreateScavengeLifecycle(_scavenge.World);

        /// <summary>Builds the rules over the world and the server's dice.</summary>
        protected virtual ScavengeLifecycle CreateScavengeLifecycle(NwnScavengeWorld world) =>
            new(world, ServiceMinigameRandom.Instance);

        /// <inheritdoc />
        public ScavengeSession Start(ScavengeRequest request) => Lifecycle.Start(request);

        /// <inheritdoc />
        public bool IsInUse(uint site) => Lifecycle.IsInUse(site);

        /// <inheritdoc />
        public ScavengeSession GetActiveAt(uint site) => Lifecycle.GetActiveAt(site);

        /// <inheritdoc />
        public bool IsActive(ScavengeSession session) => Lifecycle.IsActive(session);

        /// <inheritdoc />
        public bool CanQuickSearch(ScavengeSession session) => Lifecycle.CanQuickSearch(session);

        /// <inheritdoc />
        public ScavengeResult QuickSearch(ScavengeSession session) => Lifecycle.QuickSearch(session);

        /// <inheritdoc />
        public bool RefreshEffects(ScavengeSession session, SearchEffects effects) => Lifecycle.RefreshEffects(session, effects);

        /// <inheritdoc />
        public ScavengeStep Act(ScavengeSession session, string actionId) => Lifecycle.Act(session, actionId);

        /// <inheritdoc />
        public void Abandon(ScavengeSession session, ScavengeEnd reason) => Lifecycle.Abandon(session, reason);

        /// <inheritdoc />
        public bool Revalidate(ScavengeSession session) => Lifecycle.Revalidate(session);

        /// <inheritdoc />
        public double SecondsUntilNextAction(ScavengeSession session) => Lifecycle.SecondsUntilNextAction(session);

        /// <inheritdoc />
        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public void AbandonOnDeath()
        {
            var player = GetLastPlayerDied();
            Lifecycle.Abandon(Lifecycle.GetActive(player), ScavengeEnd.Died);
        }

        /// <inheritdoc />
        [PlayerSession(PlayerReload.Quiesce,
            "A search cannot be carried, so it ends as at logout (finds saved, site cleared) instead of leaving the site claimed.",
            Step = nameof(AbandonFor))]
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public void AbandonOnExit()
        {
            AbandonFor(GetExitingObject());
        }

        /// <summary>
        /// The work of <see cref="AbandonOnExit"/> for <paramref name="player"/>: the logout's handler, and a hot reload's quiescence step for a player who stays online.
        /// </summary>
        public void AbandonFor(uint player)
        {
            Lifecycle.Abandon(Lifecycle.GetActive(player), ScavengeEnd.Left);
        }
    }
}
