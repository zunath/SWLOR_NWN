using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.FishingService;

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
        /// The Willpower score Quick Reel rolls with (<see cref="FishingQuickReel.ReferenceWillpowerScore"/>): 10, a character with no
        /// attribute investment (owner decision, October 2026), which is the low end of the corridor.
        /// </summary>
        public int QuickReelWillpowerScore { get; init; } = FishingQuickReel.ReferenceWillpowerScore;

        /// <summary>
        /// The bound on a skilled fight's catch rate (owner decision, October 2026; a starting value): it may be this share above Quick Reel's
        /// chance for the same fish and angler, 0.15 being 15% more.
        /// </summary>
        public double FightBoundRelative { get; init; } = 0.15;

        /// <summary>
        /// Points added to the bound, so that a fish Quick Reel can hardly land is not held to a few hundredths of a point, and to cover the
        /// sampling error of a case of 1000 fights (about 1.5 points at its widest, so 3 points is two standard errors).
        /// </summary>
        public double FightBoundSlackPoints { get; init; } = 3;

        /// <summary>The most a fight may land, in percent, when Quick Reel lands <paramref name="quickReelPercent"/>.</summary>
        public double FightBound(double quickReelPercent) => quickReelPercent * (1 + FightBoundRelative) + FightBoundSlackPoints;

        /// <summary>Quick Reel's chance, in percent, for an angler of the rank and a fish of the level.</summary>
        public int QuickReel(int rank, int fishLevel) => FishingQuickReel.CatchChancePercent(rank, fishLevel, QuickReelWillpowerScore);

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
