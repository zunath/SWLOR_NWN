namespace SWLOR.Game.Server.Service.FishingService
{
    /// <summary>What a fight needs to know about the angler to carry on: whether they are still there, still, safe, and watching.</summary>
    /// <param name="IsValid">False once the angler has logged out.</param>
    /// <param name="IsDead">True while the angler is dead.</param>
    /// <param name="IsInCombat">True while the angler is in combat.</param>
    /// <param name="Position">Where the angler stands.</param>
    /// <param name="HasWindowOpen">True while the fight window is open on their screen.</param>
    public readonly record struct FishingPlayerState(
        bool IsValid,
        bool IsDead,
        bool IsInCombat,
        System.Numerics.Vector3 Position,
        bool HasWindowOpen);
}
