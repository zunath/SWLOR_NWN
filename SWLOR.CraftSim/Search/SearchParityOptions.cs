using SWLOR.CraftSim.Progression;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service.CraftService.Minigame.Gathering;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;
using SWLOR.Game.Server.Service.PerkService;

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
        /// The most a skilled session finds with no perks, as a multiple of Quick Search's expected rolls (owner rule: playing the board
        /// adds bounded upside over Quick Search, about 1.03x to 1.08x). The Treasure Seeker line then stays inside
        /// <see cref="SkilledMaximumRatio"/> with its value in the rare finds.
        /// </summary>
        public double UnperkedSkilledMaximumRatio { get; init; } = 1.08;

        /// <summary>The least a skilled session finds with no perks: the board is worth playing, so it beats Quick Search.</summary>
        public double SkilledMinimumRatio { get; init; } = 1.03;

        /// <summary>The least a careless session finds: clicking Search on every spot in turn stays near today's yield.</summary>
        public double CarelessMinimumRatio { get; init; } = 0.85;

        /// <summary>
        /// The most that even a scavenger with no perks who is told the whole layout may find. No real player reaches it; it shows that
        /// the cap does not depend on how well the skilled player plays, with room for a cleverer player than the skilled one.
        /// </summary>
        public double OracleMaximumRatio { get; init; } = 1.40;

        /// <summary>The sampling error the ratios may sit outside their limits by, since each is the mean of a few thousand boards.</summary>
        public double Tolerance { get; init; } = 0.02;

        /// <summary>
        /// Seconds from finishing one scavenge site to searching the next (walking to it and opening it), the same in both loops. The
        /// progression model's estimate (<see cref="ProgressionAssumptions.NodeTravelSeconds"/>), unmeasured until the client run.
        /// </summary>
        public double SiteSeconds { get; init; } = new ProgressionAssumptions().NodeTravelSeconds;

        /// <summary>Seconds one action of the board takes the player, reading the previews included: <see cref="SearchRules.ActionSeconds"/>.</summary>
        public double SecondsPerAction { get; init; } = SearchRules.ActionSeconds;

        /// <summary>
        /// Seconds a visit to a site took in the old code, after reaching it: one get-low animation before its one or two attempts, so a
        /// second attempt added nothing. A Quick Search takes the same: <see cref="SearchRules.QuickSearchSeconds"/>.
        /// </summary>
        public double LegacySeconds { get; init; } = SearchRules.QuickSearchSeconds;

        /// <summary>The site levels of the XP cohort: each at the Gathering rank 10 times the level, the plan's scavenging cohort.</summary>
        public IReadOnlyList<int> SiteLevels { get; init; } = new[] { 1, 2, 3, 4, 5 };

        /// <summary>The gatherer's perks and style: every board is played by someone who owns what the Gathering rank allows of them.</summary>
        public GatheringLoadout Loadout { get; init; } = GatheringLoadout.None;

        /// <summary>The perk definitions the loadout is read from.</summary>
        public IReadOnlyDictionary<PerkType, PerkDetail> Perks { get; init; } = new GatheringPerkDefinition().BuildPerks();

        /// <summary>Effects to play instead of the loadout's, by rank: a study of one purchase at a time sets it. Null plays the loadout.</summary>
        public Func<int, SearchEffects> EffectsOverride { get; init; }

        /// <summary>What the loadout's perks and style change on the board at a Gathering rank. Nothing for no perks.</summary>
        public SearchEffects EffectsAt(int rank) => EffectsOverride?.Invoke(rank) ?? Loadout.SearchAt(Perks, rank);

        /// <summary>
        /// The whole Treasure Seeker line as the Search board reads it with no style, for the check that no perk takes a skilled session
        /// past the cap: every purchase the line has (the game's own perk definitions, at the highest Gathering rank).
        /// </summary>
        public SearchEffects TreasureSeekerLine { get; init; } =
            new GatheringLoadout("Treasure Seeker, no style", false, true, GatheringStyle.None)
                .SearchAt(new GatheringPerkDefinition().BuildPerks(), 50);

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
