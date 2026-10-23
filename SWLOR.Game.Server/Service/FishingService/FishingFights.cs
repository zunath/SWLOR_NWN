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

        /// <summary>True while the angler may still choose Quick Reel: the fight is theirs and they have made no move in it.</summary>
        public static bool CanQuickReel(FishingFight fight) => Lifecycle.CanQuickReel(fight);

        /// <summary>The chance, in percent, that Quick Reel lands the fight's fish.</summary>
        public static int QuickReelChance(FishingFight fight) => FishingFightLifecycle.QuickReelChance(fight);

        /// <summary>Ends the fight at once by today's catch roll. Returns null when it was refused.</summary>
        public static FishingFightResult QuickReel(FishingFight fight) => Lifecycle.QuickReel(fight);

        /// <summary>Loses the fish because the angler left the fight unfinished. Does nothing once the fight is settled.</summary>
        public static void Abandon(FishingFight fight, FishingFightEnd reason) => Lifecycle.Abandon(fight, reason);

        /// <summary>True while the fight is the angler's fight in progress.</summary>
        public static bool IsActive(FishingFight fight) => Lifecycle.IsActive(fight);

        /// <summary>Checks the angler now and loses the fish if they moved, entered combat, died or closed the window. Returns true while the fight can go on.</summary>
        public static bool Revalidate(FishingFight fight) => Lifecycle.Revalidate(fight);

        /// <summary>The seconds the angler must wait before their next action may resolve; 0 when it may now.</summary>
        public static double SecondsUntilNextTurn(FishingFight fight) => Lifecycle.SecondsUntilNextTurn(fight);

        /// <summary>Starts the wait for the next action, once an action has resolved.</summary>
        public static void TurnTaken(FishingFight fight) => Lifecycle.TurnTaken(fight);

        /// <summary>Plays the clip of an action that resolved while the fight goes on. Landing the fish plays its own clip when the fight settles.</summary>
        public static void PlayAction(FishingFight fight, string actionId) => Lifecycle.PlayAction(fight, actionId);

        /// <summary>
        /// An angler who dies loses the fish at once. The poll cannot be trusted to see it: a death that is subdued revives the angler
        /// before the next check.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void AbandonOnDeath()
        {
            var player = GetLastPlayerDied();
            Lifecycle.Abandon(Lifecycle.GetActive(player), FishingFightEnd.Died);
        }

        /// <summary>An angler who logs out loses the fish at once.</summary>
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void AbandonOnExit()
        {
            var player = GetExitingObject();
            Lifecycle.Abandon(Lifecycle.GetActive(player), FishingFightEnd.Left);
        }
    }
}
