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

        /// <summary>The Cantina song a musician plays, as an ambientmusic.2da track; -1 lets the first collected song play.</summary>
        public int SongTrackId { get; set; } = PerformanceSongs.AutomaticSong;

        /// <summary>
        /// A watcher's choice to hear the followed band's song. Unchecking it in the Watching window keeps their own music.
        /// </summary>
        public bool PlayPerformerSong { get; set; } = true;
    }
}
