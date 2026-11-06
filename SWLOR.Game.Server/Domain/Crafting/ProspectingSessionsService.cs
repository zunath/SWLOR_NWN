using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;
using SWLOR.Game.Server.Service.ProspectingService;

namespace SWLOR.Game.Server.Domain.Crafting
{
    /// <inheritdoc cref="IProspectingSessionsService" />
    public class ProspectingSessionsService : IProspectingSessionsService
    {
        private readonly IProspectingService _prospecting;
        private ProspectingLifecycle _lifecycle;

        public ProspectingSessionsService(IProspectingService prospecting)
        {
            _prospecting = prospecting;
        }

        /// <summary>The rules of a visit, run against the engine world. It keeps the visits in progress, so there is one.</summary>
        protected ProspectingLifecycle Lifecycle => _lifecycle ??= CreateProspectingLifecycle(_prospecting.World);

        /// <summary>Builds the rules over the world and the server's dice.</summary>
        protected virtual ProspectingLifecycle CreateProspectingLifecycle(NwnProspectingWorld world) =>
            new(world, ServiceMinigameRandom.Instance);

        /// <inheritdoc />
        public ProspectingBegin Begin(ProspectingRequest request) => Lifecycle.Begin(request);

        /// <inheritdoc />
        public ProspectingActionStart StartAction(ProspectingSession session, string actionId, Action<ProspectingActionReport> onResolved) => Lifecycle.StartAction(session, actionId, onResolved);

        /// <inheritdoc />
        public bool RefreshEffects(ProspectingSession session, ProspectingEffects effects) => Lifecycle.RefreshEffects(session, effects);

        /// <inheritdoc />
        public ProspectingActionStart StartQuickHarvest(ProspectingSession session, Action<QuickHarvestReport> onProgress) => Lifecycle.StartQuickHarvest(session, onProgress);

        /// <inheritdoc />
        public void Abandon(ProspectingSession session, ProspectingEnd reason) => Lifecycle.Abandon(session, reason);

        /// <inheritdoc />
        public bool IsActive(ProspectingSession session) => Lifecycle.IsActive(session);

        /// <inheritdoc />
        public bool Revalidate(ProspectingSession session) => Lifecycle.Revalidate(session);

        /// <inheritdoc />
        public bool IsInUseByAnother(uint node, uint player) => Lifecycle.IsInUseByAnother(node, player);

        /// <inheritdoc />
        public int XpPerItem(ProspectingSession session) => Lifecycle.XpPerItem(session);

        /// <inheritdoc />
        public void AbandonOnDeath()
        {
            var player = GetLastPlayerDied();
            Lifecycle.Abandon(Lifecycle.GetActive(player), ProspectingEnd.Died);
        }

        /// <inheritdoc />
        public void AbandonOnExit()
        {
            var player = GetExitingObject();
            Lifecycle.Abandon(Lifecycle.GetActive(player), ProspectingEnd.Left);
        }
    }
}
