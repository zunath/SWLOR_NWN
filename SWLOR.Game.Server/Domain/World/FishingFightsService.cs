using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.FishingService;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// The fights a bite opens: the logic that used to be static in <c>Service.FishingService.FishingFights</c>, moved here
    /// unchanged. The facade keeps the death and logout handlers and forwards every call to the container instance. The
    /// lifecycle (and the active fights it keeps) is the own state of this instance, running against the game engine.
    /// </summary>
    public class FishingFightsService : IFishingFightsService
    {
        private readonly FishingFightLifecycle _lifecycle;

        public FishingFightsService()
            : this(new NwnFishingFightWorld(), ServiceMinigameRandom.Instance)
        {
        }

        /// <summary>For a unit test: the lifecycle runs against the world and the random the test gives.</summary>
        protected FishingFightsService(IFishingFightWorld world, IMinigameRandom random)
        {
            _lifecycle = new FishingFightLifecycle(world, random);
        }

        public FishingFight Bite(FishingBite bite) => _lifecycle.Bite(bite);

        public FishingFightResult Settle(FishingFight fight) => _lifecycle.Settle(fight);

        public bool CanQuickReel(FishingFight fight) => _lifecycle.CanQuickReel(fight);

        public int QuickReelChance(FishingFight fight) => FishingFightLifecycle.QuickReelChance(fight);

        public FishingFightResult QuickReel(FishingFight fight) => _lifecycle.QuickReel(fight);

        public void Abandon(FishingFight fight, FishingFightEnd reason) => _lifecycle.Abandon(fight, reason);

        public bool IsActive(FishingFight fight) => _lifecycle.IsActive(fight);

        public bool Revalidate(FishingFight fight) => _lifecycle.Revalidate(fight);

        public void TurnTaken(FishingFight fight) => _lifecycle.TurnTaken(fight);

        public void PlayAction(FishingFight fight, string actionId) => _lifecycle.PlayAction(fight, actionId);

        public void AbandonOnDeath()
        {
            var player = GetLastPlayerDied();
            _lifecycle.Abandon(_lifecycle.GetActive(player), FishingFightEnd.Died);
        }

        public void AbandonOnExit()
        {
            var player = GetExitingObject();
            _lifecycle.Abandon(_lifecycle.GetActive(player), FishingFightEnd.Left);
        }
    }
}
