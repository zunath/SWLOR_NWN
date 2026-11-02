namespace SWLOR.Game.Server.Service.ProspectingService
{
    /// <summary>What a session needs to know about the player to carry on: whether they are still there, still, safe, holding the harvester and watching.</summary>
    /// <param name="IsValid">False once the player has logged out.</param>
    /// <param name="IsDead">True while the player is dead.</param>
    /// <param name="IsInCombat">True while the player is in combat.</param>
    /// <param name="Position">Where the player stands.</param>
    /// <param name="HasWindowOpen">True while the Prospecting window is open on their screen.</param>
    /// <param name="HasHarvester">True while the harvester is still in the player's inventory.</param>
    /// <param name="OwnsActivity">True while the player's busy state is still the prospecting one the session set, not a rest or another activity that replaced it.</param>
    public readonly record struct ProspectingPlayerState(
        bool IsValid,
        bool IsDead,
        bool IsInCombat,
        System.Numerics.Vector3 Position,
        bool HasWindowOpen,
        bool HasHarvester,
        bool OwnsActivity);
}
