using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.CraftSim.Rng
{
    /// <summary>
    /// The source of chance a simulated engine uses: SplitMix64 streams of a seed, so every run repeats exactly on any machine.
    /// It counts its calls so a search can tell whether a forecast used chance at all.
    /// </summary>
    public sealed class SimMinigameRandom : IMinigameRandom
    {
        private readonly SimRandom _random;

        public SimMinigameRandom(SimRandom random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public static SimMinigameRandom ForRun(int seed, params int[] stream) => new(SimRandom.ForRun(seed, stream));

        /// <summary>How many rolls were taken.</summary>
        public int Calls { get; private set; }

        public int D100()
        {
            Calls++;
            return _random.D100();
        }

        public int Next(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "The range needs at least one value.");

            Calls++;
            return minInclusive + _random.Roll(maxExclusive - minInclusive) - 1;
        }
    }
}
