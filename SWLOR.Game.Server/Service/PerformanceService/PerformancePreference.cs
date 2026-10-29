namespace SWLOR.Game.Server.Service.PerformanceService
{
    public sealed class PerformancePreference
    {
        public PerformanceRoutineType Routine { get; set; } = PerformanceRoutineType.CantinaShuffle;
        public PerformanceSetType Set { get; set; } = PerformanceSetType.CantinaStandards;
        public PerformanceTempo Tempo { get; set; } = PerformanceTempo.Relaxed;
    }
}
