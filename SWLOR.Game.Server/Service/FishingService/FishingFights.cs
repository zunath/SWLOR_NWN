using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.World;

namespace SWLOR.Game.Server.Service.FishingService
{
    /// <summary>
    /// The entry point fishing and the Line window use to start, settle and abandon a fight. The logic lives in
    /// <see cref="IFishingFightsService"/>; every member here forwards to the container instance, and the death and logout
    /// handlers stay here so their names in the handler order manifest do not change. This is the extra facade of the
    /// <c>Fishing</c> service (<c>ConvertedServices.json</c>).
    /// </summary>
    public static class FishingFights
    {
        private static IFishingFightsService Instance => FacadeAccessor.Resolve<IFishingFightsService>();

        /// <summary>Handles a bite: spends the bait, pays the XP and opens the fight. Returns null when the fight could not open.</summary>
        public static FishingFight Bite(FishingBite bite) => Instance.Bite(bite);

        /// <summary>Settles a fight whose engine has ended. Returns null when it was already settled.</summary>
        public static FishingFightResult Settle(FishingFight fight) => Instance.Settle(fight);

        /// <summary>True while the angler may still choose Quick Reel: the fight is theirs and they have made no move in it.</summary>
        public static bool CanQuickReel(FishingFight fight) => Instance.CanQuickReel(fight);

        /// <summary>The chance, in percent, that Quick Reel lands the fish of the fight.</summary>
        public static int QuickReelChance(FishingFight fight) => Instance.QuickReelChance(fight);

        /// <summary>Ends the fight at once by the catch roll. Returns null when it was refused.</summary>
        public static FishingFightResult QuickReel(FishingFight fight) => Instance.QuickReel(fight);

        /// <summary>Loses the fish because the angler left the fight unfinished. Does nothing once the fight is settled.</summary>
        public static void Abandon(FishingFight fight, FishingFightEnd reason) => Instance.Abandon(fight, reason);

        /// <summary>True while the fight is the angler fight in progress.</summary>
        public static bool IsActive(FishingFight fight) => Instance.IsActive(fight);

        /// <summary>Checks the angler now and loses the fish if they moved, entered combat, died or closed the window. Returns true while the fight can go on.</summary>
        public static bool Revalidate(FishingFight fight) => Instance.Revalidate(fight);

        /// <summary>Records that the angler acted, once an action has resolved, which restarts the idle limit.</summary>
        public static void TurnTaken(FishingFight fight) => Instance.TurnTaken(fight);

        /// <summary>Plays the clip of an action that resolved while the fight goes on. Landing the fish plays its own clip when the fight settles.</summary>
        public static void PlayAction(FishingFight fight, string actionId) => Instance.PlayAction(fight, actionId);

        /// <summary>
        /// An angler who dies loses the fish at once. The poll cannot be trusted to see it: a death that is subdued revives the angler
        /// before the next check.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void AbandonOnDeath()
        {
            Instance.AbandonOnDeath();
        }

        /// <summary>An angler who logs out loses the fish at once.</summary>
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void AbandonOnExit()
        {
            Instance.AbandonOnExit();
        }
    }
}
