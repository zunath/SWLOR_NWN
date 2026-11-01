using System.Collections.Concurrent;
using SWLOR.CraftSim.Legacy.Crafting;

namespace SWLOR.CraftSim.Progression
{
    /// <summary>The legacy craft results the progression walk reads: a recipe level crafted at a skill rank.</summary>
    public interface ICraftResults
    {
        /// <summary>The crafts of a recipe at <paramref name="level"/> by a crafter at <paramref name="rank"/> (rank 0 to 50).</summary>
        CraftCell At(int level, int rank);
    }

    /// <summary>
    /// Results played once for every recipe level at the crafter's own level (the at-level climb), and any other pairing of recipe level and
    /// rank played on demand from the same seed and remembered. The old window's CP, progress and unlocked actions all depend on the rank, so a
    /// rank-0 craft of a level-1 recipe, or a craft of a low-level recipe by a higher-rank crafter, is a different result from the at-level one.
    /// </summary>
    public sealed class CraftResults : ICraftResults
    {
        private readonly IReadOnlyDictionary<int, CraftCell> _atLevel;
        private readonly Func<int, int, CraftCell> _play;
        private readonly ConcurrentDictionary<(int Level, int Rank), Lazy<CraftCell>> _played = new();

        /// <param name="atLevel">Results for each recipe level crafted at a rank equal to the level, keyed by level.</param>
        /// <param name="play">Plays a recipe level at a rank when it is not an at-level pair; called at most once per pair.</param>
        public CraftResults(IReadOnlyDictionary<int, CraftCell> atLevel, Func<int, int, CraftCell> play)
        {
            _atLevel = atLevel;
            _play = play;
        }

        /// <summary>Pairs played on demand so far.</summary>
        public int PlayedOnDemand => _played.Count;

        public CraftCell At(int level, int rank)
        {
            if (rank == level)
            {
                return _atLevel.TryGetValue(level, out var cell)
                    ? cell
                    : throw new InvalidDataException($"No craft results for recipe level {level}.");
            }

            return _played.GetOrAdd((level, rank), key => new Lazy<CraftCell>(() => _play(key.Level, key.Rank))).Value;
        }
    }
}
