namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// How many area ability hits one ship has taken from one pilot inside the current window
    /// (see <see cref="SpaceHitSequences"/>).
    /// </summary>
    public class SpaceHitSequenceState
    {
        public int Count { get; init; }
        public DateTime LastHit { get; init; }
    }
}
