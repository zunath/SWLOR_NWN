namespace SWLOR.Game.Server.Service.PerformanceService
{
    public sealed class PerformancePreference
    {
        public PerformanceRoutineType Routine { get; set; } = PerformanceRoutineType.CantinaShuffle;
        public PerformanceSetType Set { get; set; } = PerformanceSetType.CantinaStandards;

        /// <summary>The blended second routine; used only by a dancer with the capstone. Cantina Shuffle means none.</summary>
        public PerformanceRoutineType SecondRoutine { get; set; } = PerformanceRoutineType.CantinaShuffle;

        /// <summary>The woven second set; used only by a musician with the capstone. Cantina Standards means none.</summary>
        public PerformanceSetType SecondSet { get; set; } = PerformanceSetType.CantinaStandards;
        public PerformanceTempo Tempo { get; set; } = PerformanceTempo.Relaxed;
    }
}
