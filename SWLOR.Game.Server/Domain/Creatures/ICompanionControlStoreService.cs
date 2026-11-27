namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// The registered companions of the companion control and what is known about each: whether a creature is a droid or
    /// player beast under a player's control, its mode and whether its abilities are on.
    /// </summary>
    public interface ICompanionControlStoreService
    {
        /// <summary>True when the companion control holds state for the creature.</summary>
        bool IsRegisteredCompanion(uint creature);

        /// <summary>True when the creature is a droid or a player beast that a player is the master of.</summary>
        bool IsControlledCompanion(uint creature);

        CompanionMode GetMode(uint companion);

        bool AreAbilitiesEnabled(uint companion);

        /// <summary>The state of a companion; a creature that is not registered gets a fresh state.</summary>
        CompanionControlState GetState(uint companion);

        /// <summary>Starts a fresh state for a companion, in the area of its master.</summary>
        void Register(uint companion);

        void Remove(uint companion);

        bool TryGetState(uint companion, out CompanionControlState state);
    }
}
