namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>What a performer chose for the show, as the cue windows display it. Changes only when the performer changes it.</summary>
    public sealed record PerformerSetup
    {
        public string RoutineText { get; init; } = string.Empty;
        public string SetText { get; init; } = string.Empty;
        public string SongText { get; init; } = string.Empty;
        public string InstrumentText { get; init; } = string.Empty;
        public string TempoText { get; init; } = string.Empty;
    }
}
