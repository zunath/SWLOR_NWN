using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SlicingService
{
    /// <summary>A slicing attempt in progress: one player working one target's board.</summary>
    public sealed class ActiveSlicingSession
    {
        public uint Player { get; init; }
        public uint Target { get; init; }
        public SlicingSourceType Source { get; init; }
        public int Tier { get; init; }
        public SlicingBoard Board { get; set; }
        public int TraceRemaining { get; set; }
        public int SelectedIndex { get; set; } = -1;
        public bool HasCommitted { get; set; }
        public bool HasUsedTool { get; set; }
        public SlicingToolType PrimedTool { get; set; }
        public uint PrimedToolItem { get; set; }
        public int FreeActionsRemaining { get; set; }
        public List<SlicingSessionSnapshot> History { get; } = new();
    }
}
