namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>One beat of the cue strip: a single cue, a two-lane chord, or a held rest that scores neither hit nor miss.</summary>
    public readonly record struct PerformanceBeat(PerformanceCue Primary, PerformanceCue? Second, bool IsRest)
    {
        public static PerformanceBeat Rest => new(PerformanceCue.Left, null, true);

        public static PerformanceBeat Single(PerformanceCue cue) => new(cue, null, false);

        public static PerformanceBeat Chord(PerformanceCue first, PerformanceCue second) => new(first, second, false);

        public bool IsChord => Second.HasValue;

        public int LaneMask
        {
            get
            {
                if (IsRest)
                    return 0;

                var mask = 1 << (int)Primary;
                return Second.HasValue ? mask | (1 << (int)Second.Value) : mask;
            }
        }

        public bool Requires(PerformanceCue cue) => (LaneMask & (1 << (int)cue)) != 0;
    }
}
