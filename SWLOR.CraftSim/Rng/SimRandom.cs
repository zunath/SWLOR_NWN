namespace SWLOR.CraftSim.Rng
{
    /// <summary>A seeded SplitMix64 so a run repeats on any machine; dice are 1-based and inclusive like the game's d100().</summary>
    public sealed class SimRandom
    {
        private ulong _state;

        public SimRandom(long seed)
        {
            _state = unchecked((ulong)seed);
            // Scramble the seed so neighbouring seeds (run 0, run 1, ...) start from unrelated states.
            NextUInt64();
        }

        /// <summary>A generator for one run of a batch: runs with different indexes and the same seed never share a stream.</summary>
        public static SimRandom ForRun(int seed, params int[] stream)
        {
            var mixed = unchecked((ulong)(uint)seed * 0x9E3779B97F4A7C15UL);
            foreach (var part in stream)
                mixed = unchecked((mixed ^ (ulong)(uint)part) * 0xBF58476D1CE4E5B9UL + 0x94D049BB133111EBUL);

            return new SimRandom(unchecked((long)mixed));
        }

        public ulong NextUInt64()
        {
            unchecked
            {
                _state += 0x9E3779B97F4A7C15UL;
                var z = _state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        /// <summary>A whole number from 1 to <paramref name="sides"/>.</summary>
        public int Roll(int sides)
        {
            if (sides < 1)
                throw new ArgumentOutOfRangeException(nameof(sides), "A die needs at least one side.");

            return (int)(((NextUInt64() >> 32) * (ulong)sides) >> 32) + 1;
        }

        public int D3() => Roll(3);
        public int D4() => Roll(4);
        public int D20() => Roll(20);
        public int D100() => Roll(100);

        /// <summary>A number from 0 up to but not including 1.</summary>
        public double NextDouble() => (NextUInt64() >> 11) * (1.0 / (1UL << 53));
    }
}
