using SWLOR.Game.Server.Service.FishingService;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// The fights a bite opens. The static facade <c>Service.FishingService.FishingFights</c> forwards to the container
    /// instance of this interface and keeps the death and logout handlers.
    /// </summary>
    public interface IFishingFightsService
    {
        /// <summary>Handles a bite: spends the bait, pays the XP and opens the fight. Returns null when the fight could not open.</summary>
        FishingFight Bite(FishingBite bite);

        /// <summary>Settles a fight whose engine has ended. Returns null when it was already settled.</summary>
        FishingFightResult Settle(FishingFight fight);

        /// <summary>True while the angler may still choose Quick Reel: the fight is theirs and they have made no move in it.</summary>
        bool CanQuickReel(FishingFight fight);

        /// <summary>The chance, in percent, that Quick Reel lands the fish of the fight.</summary>
        int QuickReelChance(FishingFight fight);

        /// <summary>Ends the fight at once by the catch roll. Returns null when it was refused.</summary>
        FishingFightResult QuickReel(FishingFight fight);

        /// <summary>Loses the fish because the angler left the fight unfinished. Does nothing once the fight is settled.</summary>
        void Abandon(FishingFight fight, FishingFightEnd reason);

        /// <summary>True while the fight is the angler fight in progress.</summary>
        bool IsActive(FishingFight fight);

        /// <summary>Checks the angler now and loses the fish if they moved, entered combat, died or closed the window. Returns true while the fight can go on.</summary>
        bool Revalidate(FishingFight fight);

        /// <summary>Records that the angler acted, once an action has resolved, which restarts the idle limit.</summary>
        void TurnTaken(FishingFight fight);

        /// <summary>Plays the clip of an action that resolved while the fight goes on.</summary>
        void PlayAction(FishingFight fight, string actionId);

        /// <summary>An angler who dies loses the fish at once.</summary>
        void AbandonOnDeath();

        /// <summary>An angler who logs out loses the fish at once.</summary>
        void AbandonOnExit();
    }
}
