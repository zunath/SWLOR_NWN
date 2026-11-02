namespace SWLOR.Game.Server.Service.FishingService
{
    /// <summary>What a fight needs to know about the angler to carry on: whether they are still there, still, safe, holding the rod and watching.</summary>
    /// <param name="IsValid">False once the angler has logged out.</param>
    /// <param name="IsDead">True while the angler is dead.</param>
    /// <param name="IsInCombat">True while the angler is in combat.</param>
    /// <param name="Position">Where the angler stands.</param>
    /// <param name="HasWindowOpen">True while the fight window is open on their screen.</param>
    /// <param name="EquippedRod">The item in the angler's right hand, which must still be the rod that hooked the fish.</param>
    /// <param name="OwnsActivity">True while the angler's busy state is still the fishing one the fight set, not a rest or another activity that replaced it.</param>
    public readonly record struct FishingPlayerState(
        bool IsValid,
        bool IsDead,
        bool IsInCombat,
        System.Numerics.Vector3 Position,
        bool HasWindowOpen,
        uint EquippedRod,
        bool OwnsActivity);
}
