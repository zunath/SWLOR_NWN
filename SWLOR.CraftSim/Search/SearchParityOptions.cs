using SWLOR.CraftSim.Progression;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;

namespace SWLOR.CraftSim.Search
{
    /// <summary>
    /// What the Search study plays and how it judges: the starting values the plan leaves to the simulator ("Gathering yield per scavenge
    /// site at each rank stays near today's expected yield. Quick Search equals it, and a skilled session stays within +15% of it").
    /// Each is recorded in the report's inputs.
    /// </summary>
    public sealed record SearchParityOptions
    {
        public static readonly SearchParityOptions Default = new();

        /// <summary>The Gathering ranks the yield tables play.</summary>
        public IReadOnlyList<int> Ranks { get; init; } = Enumerable.Range(0, 11).Select(step => step * 5).ToList();

        /// <summary>
        /// The most a skilled session may find with every perk, as a multiple of Quick Search's expected rolls: +15% (the plan's starting
        /// value). The skilled player is a floor on skilled play, as the adaptive player is for the Line.
        /// </summary>
        public double SkilledMaximumRatio { get; init; } = 1.15;

        /// <summary>
        /// The most a skilled session finds with no perks, as a multiple of Quick Search's expected rolls: Quick Search is the choice for
        /// yield and the board the choice for XP and the rare finds, so an unperked board does not beat it. The Treasure Seeker line is
        /// sized to lift a skilled scavenger up to <see cref="SkilledMaximumRatio"/> and no further (the Quick Reel precedent for fishing).
        /// </summary>
        public double UnperkedSkilledMaximumRatio { get; init; } = 1.00;

        /// <summary>The least a skilled session finds with no perks: the board stays near today's yield.</summary>
        public double SkilledMinimumRatio { get; init; } = 0.80;

        /// <summary>The least a careless session finds: clicking Search on every spot in turn stays near today's yield.</summary>
        public double CarelessMinimumRatio { get; init; } = 0.70;

        /// <summary>
        /// The most that even a scavenger with no perks who is told the whole layout may find. No real player reaches it; it shows that
        /// the cap holds for anyone, with room for a cleverer player than the skilled one.
        /// </summary>
        public double OracleMaximumRatio { get; init; } = 1.30;

        /// <summary>The sampling error the ratios may sit outside their limits by, since each is the mean of a few thousand boards.</summary>
        public double Tolerance { get; init; } = 0.02;

        /// <summary>
        /// Seconds from finishing one scavenge site to searching the next (walking to it and opening it), the same in both loops. The
        /// progression model's estimate (<see cref="ProgressionAssumptions.NodeTravelSeconds"/>), unmeasured until the client run.
        /// </summary>
        public double SiteSeconds { get; init; } = new ProgressionAssumptions().NodeTravelSeconds;

        /// <summary>Seconds one action of the board takes the player, reading the previews included: <see cref="SearchRules.ActionSeconds"/>.</summary>
        public double SecondsPerAction { get; init; } = SearchRules.ActionSeconds;

        /// <summary>Seconds one of today's attempts takes (a Quick Search attempt takes the same): <see cref="SearchRules.QuickSearchSecondsPerAttempt"/>.</summary>
        public double LegacySecondsPerAttempt { get; init; } = SearchRules.QuickSearchSecondsPerAttempt;

        /// <summary>The site levels of the XP cohort: each at the Gathering rank 10 times the level, the plan's scavenging cohort.</summary>
        public IReadOnlyList<int> SiteLevels { get; init; } = new[] { 1, 2, 3, 4, 5 };

        /// <summary>
        /// The whole Treasure Seeker line as the Search board reads it, for the check that no perk takes a skilled session past the cap:
        /// the perk draft's magnitudes (<c>gathering-treasure-seeker-perk-line-draft.md</c>).
        /// </summary>
        public SearchEffects TreasureSeekerLine { get; init; } = new(
            ExtraPromisingSpots: 1,
            DigRiskPercent: 50,
            DeeperWeightBonus: 2);

        /// <summary>Boards of the XP cohort's Quick Search check and the Monte Carlo cross-check of the exact expected rolls.</summary>
        public int QuickSearchRuns { get; init; } = 100_000;

        /// <summary>Plays each player the XP-rate gate judges, worst first: the gate judges the lowest XP per hour among them.</summary>
        public IReadOnlyList<SearchPlayerKind> XpPlayers { get; init; } = new[] { SearchPlayerKind.Careless, SearchPlayerKind.Skilled };

        /// <summary>The scavenge tables the rare-supply evidence rolls.</summary>
        public IReadOnlyList<string> RareSupplyTables { get; init; } = new[]
        {
            "SCAVENGING_BUSH_1", "SCAVENGING_BUSH_3", "SCAVENGING_BUSH_5", "VEGETABLES_PATCH_3", "SUPPLY_CRATE_3", "SUPPLY_CRATE_5",
            "KORRIBAN_SITH_CRATE_1", "KORRIBAN_SITH_CRATE_3", "TATOOINE_TUSKEN_CRATE", "VISCARA_COXXIAN_CRATE", "VISCARA_MANDALORIAN_CRATE", "QIONHIVE_RESOURCES"
        };
    }
}
