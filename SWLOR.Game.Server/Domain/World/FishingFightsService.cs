using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Crafting.Minigame;
using SWLOR.Game.Server.Domain.World;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// The fights a bite opens: the logic that used to be a static class, moved here unchanged. The death and logout
    /// handlers run on the container instance; static callers reach it through <c>GameServices.FishingFights</c>. The
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

        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public void AbandonOnDeath()
        {
            var player = GetLastPlayerDied();
            _lifecycle.Abandon(_lifecycle.GetActive(player), FishingFightEnd.Died);
        }

        [PlayerSession(PlayerReload.Quiesce,
            "A fishing fight cannot be carried, so it ends as at logout and the busy flag clears instead of sticking.",
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
            _lifecycle.Abandon(_lifecycle.GetActive(player), FishingFightEnd.Left);
        }
    }
}
