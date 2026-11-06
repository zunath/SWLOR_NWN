namespace SWLOR.Game.Server.Service.SlicingService
{
    /// <summary>The board and trace as they were before an action, kept so a tool can rewind it.</summary>
    public sealed class SlicingSessionSnapshot
    {
        public SlicingBoard Board { get; init; }
        public int TraceRemaining { get; init; }
    }
}
