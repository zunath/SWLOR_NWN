namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// A pilot's current run of consecutive weapon hits against one target (see <see cref="SpaceRepeatedHits"/>).
    /// </summary>
    public class SpaceRepeatedHitState
    {
        public uint Target { get; init; }
        public int Stacks { get; init; }
        public DateTime LastHit { get; init; }
    }
}
