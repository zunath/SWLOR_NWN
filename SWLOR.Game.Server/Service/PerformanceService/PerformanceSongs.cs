namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>
    /// The song choice constants. Which song a musician plays is decided by <c>IPerformanceSongService</c>.
    /// </summary>
    public static class PerformanceSongs
    {
        /// <summary>The preference value that leaves the choice to the first collected Cantina song.</summary>
        public const int AutomaticSong = -1;

        /// <summary>
        /// A single song leaves nothing to choose, and the engine's one-row combo popup draws its scroll buttons stacked,
        /// so the picker only opens with two or more songs.
        /// </summary>
        public static bool CanChooseSong(int availableSongs) => availableSongs > 1;
    }
}
