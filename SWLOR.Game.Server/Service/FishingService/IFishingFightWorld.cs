namespace SWLOR.Game.Server.Service.FishingService
{
    /// <summary>
    /// Everything a fishing bite and its fight need from the game engine. <see cref="FishingFightLifecycle"/> holds the rules and
    /// calls only this, so they run without the engine; <c>NwnFishingFightWorld</c> is the engine implementation.
    /// </summary>
    public interface IFishingFightWorld
    {
        /// <summary>Sets the bait left on the rod.</summary>
        void SetRemainingBait(uint rod, int remaining);

        /// <summary>Sets the bites the fishing point has left.</summary>
        void SetFishingPointAttempts(uint fishingPoint, int remaining);

        /// <summary>Removes a fishing point that has no bites left.</summary>
        void ExhaustFishingPoint(uint fishingPoint);

        void GiveAgricultureXp(uint player, int xp);

        void Message(uint player, string text);

        /// <summary>Puts the landed fish in the angler's inventory.</summary>
        void CreateFish(uint player, string resref);

        /// <summary>Marks the angler busy fishing, so they cannot cast again until the fight is over.</summary>
        void BeginFight(uint player);

        /// <summary>Clears the busy mark the fight set.</summary>
        void EndFight(uint player);

        /// <summary>Opens the fight window. Returns false when it could not be opened.</summary>
        bool OpenWindow(uint player, FishingFight fight);

        void CloseWindow(uint player);

        FishingPlayerState GetPlayerState(uint player);

        /// <summary>The current time, which paces the turns of a fight.</summary>
        DateTime UtcNow { get; }

        /// <summary>Runs <paramref name="action"/> after the given seconds, whether or not the angler is still in the game.</summary>
        void Schedule(float seconds, Action action);
    }
}
