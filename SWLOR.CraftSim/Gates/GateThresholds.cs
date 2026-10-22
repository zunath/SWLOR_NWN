using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Play;

namespace SWLOR.CraftSim.Gates
{
    /// <summary>
    /// Every threshold and sampling size the crafting gates use, with its starting value, in one place. Each is a first guess the plan
    /// marks as tunable (<c>CraftingRevampPlan.md</c>, "Balance gates and tooling"); a change here changes every minigame's gates and
    /// is recorded in the report's inputs.
    /// </summary>
    public sealed record GateThresholds
    {
        public static readonly GateThresholds Default = new();

        // Gate 1: no fixed rotation.

        /// <summary>Adaptive play must beat the best fixed rotation by at least this many quality points (percentage points of the quality meter) in every band and gear tier.</summary>
        public double RotationMarginPoints { get; init; } = 10;

        // Gate 2: style balance.

        /// <summary>Each style of a pair must be the better choice on at least this share of recipes.</summary>
        public double StyleMinimumWinShare { get; init; } = 0.30;

        /// <summary>Neither style of a pair may beat the other by more than this many quality points on average ("about 5").</summary>
        public double StyleMaximumMeanDifferencePoints { get; init; } = 5;

        /// <summary>Two styles within this many quality points on a recipe tie, and each takes half a win.</summary>
        public double StyleTieMarginPoints { get; init; } = 0.5;

        // Gate 3: perks optional.

        /// <summary>The rank of the unperked crafter.</summary>
        public int PerksOptionalRank { get; init; } = 50;

        /// <summary>At least this share of attempts must reach the maximum quality, in each at-level band, by skilled play with the best gear and no perks.</summary>
        public double PerksOptionalMinimumFullQualityShare { get; init; } = 0.50;

        /// <summary>The recipe levels a rank-50 crafter works at, from just below their rank to the highest recipe in the game.</summary>
        public IReadOnlyList<RecipeBand> PerksOptionalBands { get; init; } = new[]
        {
            new RecipeBand("41-45", 41, 45),
            new RecipeBand("46-50", 46, 50),
            new RecipeBand("51-53", 51, int.MaxValue)
        };

        // Gate 5: material parity.

        /// <summary>Expected component stacks consumed per item must be within this share of the legacy baseline (10%).</summary>
        public double MaterialTolerance { get; init; } = 0.10;

        /// <summary>The share of a band's recipes the new minigame may never make beyond the legacy share; recipes never made are left out of the stack figure, so this stops them hiding there.</summary>
        public double MaterialExtraNeverCraftedShare { get; init; } = 0.10;

        // Gate 6: XP rate.

        /// <summary>New XP per hour divided by the legacy rate must be at least this: "at least today's".</summary>
        public double XpRateMinimumRatio { get; init; } = 1.0;

        /// <summary>
        /// The player's seconds per action in the old craft window. The plan sizes a craft at 10-20 actions in about 1-2 minutes, which is
        /// 6 seconds an action. An estimate until the client evidence measures it.
        /// </summary>
        public double LegacySecondsPerAction { get; init; } = 6.0;

        /// <summary>Seconds a craft costs besides its actions (opening the recipe, collecting the item), the same for both loops. Unmeasured; 0 until it is.</summary>
        public double OverheadSecondsPerAttempt { get; init; } = 0.0;

        // Sampling.

        /// <summary>The fixed-rotation solver's effort for every scenario; null leaves each scenario's own <see cref="Scenario.ICraftScenario.SolverOptions"/>.</summary>
        public FixedRotationSolverOptions SolverOptions { get; init; }

        /// <summary>Recipes played per band: an even sample across the band, repeatable. Null plays every recipe.</summary>
        public int? RecipesPerBand { get; init; } = 6;

        /// <summary>An attempt that takes more actions than this counts as stalled, which is no item.</summary>
        public int ActionLimit { get; init; } = 100;

        public IReadOnlyList<(string Name, string Value, string Meaning)> Describe() => new[]
        {
            ("RotationMarginPoints", Number(RotationMarginPoints), "gate 1: adaptive beats the best fixed rotation by at least this many quality points"),
            ("StyleMinimumWinShare", Percent(StyleMinimumWinShare), "gate 2: each style is the better choice on at least this share of recipes"),
            ("StyleMaximumMeanDifferencePoints", Number(StyleMaximumMeanDifferencePoints), "gate 2: neither style beats the other by more on average"),
            ("StyleTieMarginPoints", Number(StyleTieMarginPoints), "gate 2: a recipe within this many points is a tie, half a win each"),
            ("PerksOptionalRank", PerksOptionalRank.ToString(), "gate 3: rank of the unperked crafter"),
            ("PerksOptionalMinimumFullQualityShare", Percent(PerksOptionalMinimumFullQualityShare), "gate 3: share of crafts that reach maximum quality, per at-level band"),
            ("PerksOptionalBands", string.Join(", ", PerksOptionalBands.Select(b => b.Name)), "gate 3: the at-level recipe bands"),
            ("MaterialTolerance", Percent(MaterialTolerance), "gate 5: component stacks per item within this share of the legacy baseline"),
            ("MaterialExtraNeverCraftedShare", Percent(MaterialExtraNeverCraftedShare), "gate 5: recipes never made may exceed the legacy share by at most this"),
            ("XpRateMinimumRatio", Number(XpRateMinimumRatio), "gate 6: XP per hour divided by the legacy rate"),
            ("LegacySecondsPerAction", Number(LegacySecondsPerAction), "gate 6: seconds per action in the old craft window (unmeasured estimate)"),
            ("OverheadSecondsPerAttempt", Number(OverheadSecondsPerAttempt), "gate 6: seconds per craft besides actions, both loops (unmeasured estimate)"),
            ("RecipesPerBand", RecipesPerBand?.ToString() ?? "all", "recipes played per band"),
            ("SolverOptions", SolverOptions == null ? "the scenario's" : "overridden", "effort of the fixed-rotation solver"),
            ("ActionLimit", ActionLimit.ToString(), "actions after which an attempt counts as stalled")
        };

        private static string Number(double value) => value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

        private static string Percent(double fraction) => (fraction * 100).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "%";
    }
}
