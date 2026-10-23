namespace SWLOR.Game.Server.Service.FishingService
{
    /// <summary>
    /// A bite as the fishing code knows it once a cast has picked a fish: who hooked what with which rod and bait at which point.
    /// The fight that follows is built from it.
    /// </summary>
    /// <param name="Player">The angler.</param>
    /// <param name="Rod">The fishing rod item.</param>
    /// <param name="FishingPoint">The fishing point that was cast into.</param>
    /// <param name="Fish">The fish that bit.</param>
    /// <param name="RodType">The kind of rod, which sets the tier of the line.</param>
    /// <param name="Rank">The angler's Agriculture rank when the fish bit.</param>
    /// <param name="BaitRemaining">The bait on the rod before this bite spends one.</param>
    /// <param name="BaitName">The name of the loaded bait, for the "Bait Remaining" line.</param>
    /// <param name="PointAttemptsRemaining">The bites the fishing point had left before this one.</param>
    /// <param name="Position">Where the angler stood, which they must not leave during the fight.</param>
    public sealed record FishingBite(
        uint Player,
        uint Rod,
        uint FishingPoint,
        FishAttribute Fish,
        FishingRodType RodType,
        int Rank,
        int BaitRemaining,
        string BaitName,
        int PointAttemptsRemaining,
        System.Numerics.Vector3 Position);
}
