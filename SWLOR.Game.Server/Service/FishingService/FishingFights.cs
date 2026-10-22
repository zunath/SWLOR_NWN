using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.Game.Server.Service.FishingService
{
    /// <summary>The entry point fishing and the Line window use to start, settle and abandon a fight. It runs <see cref="FishingFightLifecycle"/> against the game engine.</summary>
    public static class FishingFights
    {
        private static readonly FishingFightLifecycle Lifecycle =
            new(NwnFishingFightWorld.Instance, ServiceMinigameRandom.Instance);

        /// <summary>Handles a bite: spends the bait, pays the XP and opens the fight. Returns null when the fight could not open.</summary>
        public static FishingFight Bite(FishingBite bite) => Lifecycle.Bite(bite);

        /// <summary>Settles a fight whose engine has ended. Returns null when it was already settled.</summary>
        public static FishingFightResult Settle(FishingFight fight) => Lifecycle.Settle(fight);

        /// <summary>Loses the fish because the angler left the fight unfinished. Does nothing once the fight is settled.</summary>
        public static void Abandon(FishingFight fight, FishingFightEnd reason) => Lifecycle.Abandon(fight, reason);

        /// <summary>True while the fight is the angler's fight in progress.</summary>
        public static bool IsActive(FishingFight fight) => Lifecycle.IsActive(fight);

        /// <summary>Checks the angler now and loses the fish if they moved, entered combat, died or closed the window. Returns true while the fight can go on.</summary>
        public static bool Revalidate(FishingFight fight) => Lifecycle.Revalidate(fight);

        /// <summary>An angler who logs out loses the fish at once.</summary>
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void AbandonOnExit()
        {
            var player = GetExitingObject();
            Lifecycle.Abandon(Lifecycle.GetActive(player), FishingFightEnd.Left);
        }
    }
}
