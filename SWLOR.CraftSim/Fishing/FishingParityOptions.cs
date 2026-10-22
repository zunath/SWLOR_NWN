using SWLOR.Game.Server.Service.CraftService.Minigame.Line;

namespace SWLOR.CraftSim.Fishing
{
    /// <summary>
    /// What the fishing parity study plays and how it judges: the starting values the plan leaves to the simulator
    /// ("Fish caught per bite at each rank stays near today's catch rate"). Each is recorded in the report's inputs.
    /// </summary>
    public sealed record FishingParityOptions
    {
        public static readonly FishingParityOptions Default = new();

        /// <summary>The Agriculture ranks the table by rank above the fish's level plays, each with the best rod it can use.</summary>
        public IReadOnlyList<int> ReferenceRanks { get; init; } = new[] { 10, 20, 30, 40, 50 };

        /// <summary>The lowest rank above the fish's level in the table (negative: the fish is above the rank).</summary>
        public int LowestDelta { get; init; } = -8;

        public int HighestDelta { get; init; } = 6;

        /// <summary>The ranks the pooled table plays, over every rod and bait pool the angler can use.</summary>
        public IReadOnlyList<int> PooledRanks { get; init; } = Enumerable.Range(0, 11).Select(step => step * 5).ToList();

        /// <summary>The Willpower scores whose catch chances bound the corridor the fight is judged against.</summary>
        public int CorridorLowScore { get; init; } = 10;

        public int CorridorHighScore { get; init; } = 14;

        /// <summary>The adaptive player's catch rate may sit this many points outside the corridor and still count as near today's.</summary>
        public double TolerancePoints { get; init; } = 10;

        /// <summary>
        /// The seconds one turn of the fight takes the player, reading the previews included: <c>LineRules.TurnSeconds</c>, which the game also
        /// enforces as the least time between two actions. An estimate until the client run measures it.
        /// </summary>
        public double SecondsPerTurn { get; init; } = LineRules.TurnSeconds;

        /// <summary>
        /// Whether the XP cohort's player takes the first action of a fight at once, as the game allows (each later action waits the turn).
        /// False models the typical player, who reads the first move for a turn like every other; true models the fastest allowed pace, a
        /// fight of n actions lasting n - 1 turns.
        /// </summary>
        public bool FreeOpeningTurn { get; init; }

        /// <summary>The seconds of one cast and wait for a bite: 6, 7 or 8 in the game, so 7 on average. Both loops spend it, once per bite.</summary>
        public double CastSeconds { get; init; } = 7;

        public IReadOnlyList<int> Deltas => Enumerable.Range(LowestDelta, HighestDelta - LowestDelta + 1).ToList();
    }
}
