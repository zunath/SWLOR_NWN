namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>The icon and hot bar slot a cue is shown with in the performance windows. Pure.</summary>
    public static class PerformanceCueIcons
    {
        public const string StepLeft = "ife_pf_stepl";
        public const string StepUp = "ife_pf_stepu";
        public const string StepDown = "ife_pf_stepd";
        public const string StepRight = "ife_pf_stepr";
        public const string NoteLow = "ife_pf_notelo";
        public const string NoteMid = "ife_pf_notemid";
        public const string NoteHigh = "ife_pf_notehi";

        /// <summary>The icon resref of the mode feat that answers the cue; the same art as the hot bar button.</summary>
        public static string GetIcon(PerformanceCue cue)
        {
            return cue switch
            {
                PerformanceCue.Left => StepLeft,
                PerformanceCue.Up => StepUp,
                PerformanceCue.Down => StepDown,
                PerformanceCue.Right => StepRight,
                PerformanceCue.Low => NoteLow,
                PerformanceCue.Mid => NoteMid,
                PerformanceCue.High => NoteHigh,
                _ => string.Empty
            };
        }

        /// <summary>The 1-based hot bar slot the cue's mode feat sits in while performing.</summary>
        public static int GetHotbarSlot(PerformanceCue cue)
        {
            return cue switch
            {
                PerformanceCue.Left => 1,
                PerformanceCue.Up => 2,
                PerformanceCue.Down => 3,
                PerformanceCue.Right => 4,
                PerformanceCue.Low => 1,
                PerformanceCue.Mid => 2,
                PerformanceCue.High => 3,
                _ => 0
            };
        }

        public static string GetSlotText(PerformanceCue cue)
        {
            var slot = GetHotbarSlot(cue);
            return slot > 0 ? slot.ToString() : string.Empty;
        }
    }
}
