using System.Reflection;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Legacy.Gathering;
using SWLOR.CraftSim.Report;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Gathering;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;
using SWLOR.Game.Server.Service.ScavengeService;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Search
{
    /// <summary>
    /// The scavenging study: the economy-parity gate's yield check, the length gate and the XP-rate gate over the scavenging cohort, Quick
    /// Search's own XP rate, plus the evidence the Treasure Seeker perk draft quotes, written as one markdown report in the style of the
    /// other balance reports.
    /// </summary>
    public static class SearchReport
    {
        public const string Title = "Crafting revamp: the Search board and Quick Search against today's scavenging";

        public const string QuickSearchXpGate = "Quick Search XP rate";

        /// <summary>Plays every case in parallel, runs the gates and the evidence, and returns the results the report shows.</summary>
        public static IReadOnlyList<GateResult> Run(GateContext context, SearchParity parity, SearchParityOptions options, GateThresholds thresholds)
        {
            var cohort = new SearchXpRateCohort(parity, options);
            var yield = new SearchYieldGate(parity, options);
            parity.Prepare(yield.Keys().Concat(cohort.Keys()).Concat(SearchPerkEvidence.Keys(options))
                .Concat(SearchPerkEvidence.Ranks.SelectMany(rank => new[]
                {
                    new SearchKey(rank, 1, SearchPlayerKind.Careless, SearchEffects.None), new SearchKey(rank, 1, SearchPlayerKind.Skilled, SearchEffects.None)
                })));

            var results = new List<GateResult>
            {
                yield.Evaluate(context),
                new SearchLengthGate(parity, options).Evaluate(context),
                new XpRateGate(cohort, thresholds).Evaluate(context),
                QuickSearchXp(options, thresholds),
                new GateResult(
                    "Evidence: XP rate by player",
                    GateVerdict.NotApplicable,
                    "Not a gate. The XP per hour of each simulated scavenger against today's; the gate judges the lower of the two.",
                    new[] { XpByPlayer(cohort, context, options) })
            };

            var rank = 30;
            results.Add(new GateResult(
                "Evidence: rare supply",
                GateVerdict.NotApplicable,
                "Not a gate. How the board changes the rare items real scavenge sites give: Dig Deeper's finds roll the table with its rare entries weighted x" + ScavengeLoot.DeeperWeightMultiplier + ", and they replace " +
                "searches that would mostly have rolled the common items. The plan caps the rolls, not what they roll; the owner decides whether the rare supply needs a cap of its own.",
                new[]
                {
                    SearchRareSupply.Build(parity, options, rank, 0),
                    SearchRareSupply.Build(parity, options, rank, 3),
                    SearchRareSupply.Build(parity, options, rank, 0, SearchPerkEvidence.WithCarefulHands(options.TreasureSeekerLine), "the whole Treasure Seeker line and Careful Hands")
                }));
            results.Add(new GateResult(
                "Evidence: Treasure Seeker magnitudes",
                GateVerdict.NotApplicable,
                "Not a gate. What each proposed Treasure Seeker purchase adds to the skilled player's finds; the draft quotes it. The cap above holds for the whole line.",
                new[] { SearchPerkEvidence.PerkMagnitudes(parity, options) }));
            return results;
        }

        /// <summary>
        /// Quick Search takes today's time and pays today's XP, so its XP per hour is today's, not above it (owner decision, October 2026): the
        /// expected XP of a Quick Search at each site level and the attempts it takes, from the server's rules, against the legacy model.
        /// </summary>
        public static GateResult QuickSearchXp(SearchParityOptions options, GateThresholds thresholds)
        {
            var table = new EvidenceTable(
                "Site level", "Gathering rank", "Today's XP per site", "Quick Search XP per site", "Today's attempts", "Quick Search attempts",
                "Today's XP per hour", "Quick Search XP per hour", "Ratio", "Verdict");
            var failures = new List<string>();
            foreach (var level in options.SiteLevels)
            {
                var rank = SearchXpRateCohort.RankOf(level);
                var tier = CraftRankMilestones.HardLookLevelForRank(rank);
                var delta = ServerXp.ForDelta(10 * level - rank);
                var today = ScavengeModel.ExpectedXpPerSite(level, rank, tier, ScavengeQuickSearch.ReferencePerceptionModifier);
                var quick = ScavengeQuickSearch.ExpectedXp(tier, delta);
                var todayAttempts = ScavengeModel.ExpectedAttemptsPerSite(tier, ScavengeQuickSearch.ReferencePerceptionModifier);
                var quickAttempts = ScavengeQuickSearch.ExpectedAttempts(tier);
                var todayRate = today / (options.LegacySeconds + options.SiteSeconds) * 3600;
                var quickRate = quick / (SearchRules.QuickSearchSeconds + options.SiteSeconds) * 3600;
                var ratio = quickRate / todayRate;
                var equal = Math.Abs(ratio - 1) <= 1e-9 && ratio <= thresholds.XpRateMinimumRatio + 1e-9;
                if (!equal)
                    failures.Add($"site level {level} ({Number(ratio, 3)}x)");

                table.Add(
                    level.ToString(), rank.ToString(), Number(today, 1), Number(quick, 1), Number(todayAttempts, 2), Number(quickAttempts, 2),
                    Number(todayRate, 0), Number(quickRate, 0), Number(ratio, 3), equal ? "Equal" : "Different");
            }

            var gateTable = table.ToGateTable("Quick Search against today's search, per site level at the Gathering rank 10 times the level");
            return failures.Count == 0
                ? new GateResult(QuickSearchXpGate, GateVerdict.Passed, "Quick Search pays today's XP in today's time at every site level, so its XP per hour is today's and not above it.", new[] { gateTable })
                : new GateResult(QuickSearchXpGate, GateVerdict.Failed, $"Quick Search's XP per hour differs from today's: {string.Join("; ", failures)}.", new[] { gateTable });
        }

        private static GateTable XpByPlayer(SearchXpRateCohort cohort, GateContext context, SearchParityOptions options)
        {
            var table = new EvidenceTable(
                "Site level", "Gathering rank", "Today's XP per hour", "Careless XP per board", "Careless XP per hour", "Careless / today",
                "Skilled XP per board", "Skilled XP per hour", "Skilled / today", "Actions per board");
            foreach (var xpCase in cohort.Cases(context))
            {
                var level = int.Parse(xpCase.Group.Replace("Site level ", string.Empty));
                var rank = SearchXpRateCohort.RankOf(level);
                var legacy = cohort.PlayLegacy(xpCase, context).XpPerHour(cohort.LegacyTiming);
                var careless = cohort.Play(level, rank, SearchPlayerKind.Careless);
                var skilled = cohort.Play(level, rank, SearchPlayerKind.Skilled);
                table.Add(
                    level.ToString(),
                    rank.ToString(),
                    Number(legacy, 0),
                    Number(careless.XpPerRun, 0),
                    Number(careless.XpPerHour(cohort.ReplacementTiming), 0),
                    Number(careless.XpPerHour(cohort.ReplacementTiming) / legacy, 2) + "x",
                    Number(skilled.XpPerRun, 0),
                    Number(skilled.XpPerHour(cohort.ReplacementTiming), 0),
                    Number(skilled.XpPerHour(cohort.ReplacementTiming) / legacy, 2) + "x",
                    Number(careless.ActionsPerRun, 1));
            }

            return table.ToGateTable("Gathering XP per hour of a board against today's search, by player");
        }

        public static string Build(
            GateContext context,
            SearchParity parity,
            SearchParityOptions options,
            GateThresholds thresholds,
            IReadOnlyList<GateResult> results,
            string command) =>
            GateReport.Build(Title, context, results, command, Inputs(context, parity, options, thresholds), Limitations(options));

        public static IReadOnlyList<string> Inputs(GateContext context, SearchParity parity, SearchParityOptions options, GateThresholds thresholds)
        {
            var lines = new List<string>
            {
                "- Today (what Quick Search keeps): one attempt, plus a second at `Hard Look level x 10 + Perception modifier x 5` percent. An attempt succeeds on `d20 + Perception modifier` against a DC of 6, rising by a d3 " +
                "after each attempt; a success rolls the site's loot table once and pays the site's XP, a failure pays a quarter of it. Hard Look level is the Gathering rank's tier (`CraftRankMilestones`). " +
                $"The Perception modifier is the parity reference of a character with no investment, {ScavengeQuickSearch.ReferencePerceptionModifier} (owner decision, October 22 2026); no code reads an attribute.",
                $"- The board: the Search engine, {parity.Runs} boards per case, each with its own dice from seed {parity.Seed}. A 3x3 board; the Gathering rank sets the searches (`CraftRankMilestones.SearchesForRank`, replacing Hard Look), the promising spots " +
                $"(`PromisingSpotsForRank`, replacing Perception) and the chance a Search finds on promising ground (`PromisingChanceForRank`): {string.Join("; ", CraftRankMilestones.SearchSchedule().Select(step => $"from rank {step.Rank}: {step.Searches} searches, {step.PromisingSpots} promising spots, {step.PromisingChance}% on promising ground"))}. " +
                $"Searches find {SearchRules.OrdinaryChance}% on ordinary ground. A Dig Deeper risks {SearchRules.DigRiskBase}% of ruining the spot, {SearchRules.DigRiskStep} points more for each dig before it, " +
                $"and finds {SearchRules.DigFindChance}% of the time when it does not, rolling the loot table with its rare entries weighted x{ScavengeLoot.DeeperWeightMultiplier}. A Sift shows the four spots next to a spot. Every action costs one search.",
                "- Players: **Careless** searches the spots in reading order and never sifts or digs. **Skilled** plays from what the window shows (it does not read the hidden layout): every spot is a chain of actions, the best split of the searches among the chains " +
                "is a small knapsack, and it Sifts when the ground a Sift would show raises that value by more than the search it costs. It is a floor on skilled play, not an optimal player. **Oracle** is told the whole layout for free; no real player can reach it.",
                $"- Yield is the loot-table rolls a site gives (a find is one roll). Limits: Quick Search equals today's expected rolls exactly; careless at least {Number(options.CarelessMinimumRatio, 2)}x of it; skilled {Number(options.SkilledMinimumRatio, 2)}x to {Number(options.UnperkedSkilledMaximumRatio, 2)}x with no perks; " +
                $"oracle at most {Number(options.OracleMaximumRatio, 2)}x; each within {Number(options.Tolerance, 2)} for sampling error. With the whole Treasure Seeker line the skilled player stays within the plan's +15% ({Number(options.SkilledMaximumRatio, 2)}x), the line being sized to stay inside the cap with its value in the rare finds ({Describe(options.TreasureSeekerLine)}).",
                $"- Time: {Number(options.SiteSeconds)} s to reach and open each site in both loops (the progression model's estimate, unmeasured); today's search takes {Number(options.LegacySeconds)} s once the site is open (one get-low animation before its one or two attempts, so a second attempt adds no time) and a board action {Number(options.SecondsPerAction)} s " +
                $"(`SearchRules.ActionSeconds`, which the game enforces as the least time between two actions, the first free). Both are estimates until the client run measures them.",
                $"- XP: every action pays Gathering XP. A Search or Dig Deeper attempt pays what the site pays against the Gathering rank (`Skill.GetDeltaXP(10 x site level - rank)`), a quarter on failure, as scavenging always did, times the multiplier by site level the game applies (`CraftMinigameXp`): " +
                string.Join(", ", Enumerable.Range(1, 5).Select(level => $"level {level} {SearchRules.XpMultiplierForLevel(level).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}x")) +
                ". A Sift finds nothing, so it pays the quarter an attempt that finds nothing pays.",
                "- XP cohort (gate 6): site levels 1 to 5, the Gathering rank 10 times the level, no tool. The gate judges the lower XP per hour of the careless and the skilled player.",
                "- Named starting values of the board (`SearchRules`), all tunable:"
            };

            foreach (var (name, value) in StartingValues())
                lines.Add($"  - `{name}` = {value}");

            if (options.Loadout != GatheringLoadout.None)
            {
                lines.Insert(0,
                    $"- Gatherer: **{options.Loadout.Name}**. Every board is played by someone who owns every purchase their Gathering rank allows of the Prospector and Treasure Seeker lines (`GatheringPerkDefinition`), with the style active from rank {GatheringStyles.StyleRank}. " +
                    "Quick Search reads no perk, and the skilled player may find up to the plan's +15% of its rolls.");
            }

            return lines;
        }

        /// <summary>Every starting value of the board, by name, as the report records them.</summary>
        public static IReadOnlyList<(string Name, string Value)> StartingValues() =>
            typeof(SearchRules).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsLiteral)
                .Select(field => (field.Name, Convert.ToString(field.GetRawConstantValue(), System.Globalization.CultureInfo.InvariantCulture)))
                .ToList();

        private static string Describe(SearchEffects effects) =>
            $"{(effects.SearchChanceDelta >= 0 ? "+" : string.Empty)}{effects.SearchChanceDelta} points on every Search, {effects.DigRiskPercent}% of the dig risk" +
            (effects.ExtraPromisingSpots != 0 ? $", +{effects.ExtraPromisingSpots} promising spot" : string.Empty) +
            (effects.DigFindBonus != 0 ? $", +{effects.DigFindBonus} points of dig find chance" : string.Empty) +
            (effects.DeeperWeightBonus > 0 ? $", rare entries weigh {effects.DeeperWeightBonus} more in a dig" : string.Empty) +
            (effects.FindWeightBonus > 0 ? $", rare entries weigh {effects.FindWeightBonus} more in an ordinary find" : string.Empty) +
            (effects.JackpotWeightBonus > 0 ? $", rare entries weigh {effects.JackpotWeightBonus} more in the first dig find of a board" : string.Empty);

        public static IReadOnlyList<string> Limitations(SearchParityOptions options) => new[]
        {
            "- The skilled player is a one-step value-of-information player, not an optimal one, and real players are less skilled; the careless player marks the other end. The oracle bounds what any play could find. The study does not measure how real players play.",
            "- Yield counts loot-table rolls, not their value: a find from Dig Deeper is one roll like any other. Sites' extra loot tables (`LOOT_TABLE_n`), which roll once for every search that finds, and Creditfinder are not modelled. The rare-supply tables show what the rolls land on for real tables.",
            $"- Gate 6 weighs the time a board adds ({Number(options.SecondsPerAction)} s an action, and {Number(options.SiteSeconds)} s to reach a site, both unmeasured) against the XP of its attempts, so the result is a starting value: it grows or shrinks with the real seconds. " +
            "The game keeps the pace from below (each action after the first waits the action time), so a fast player earns about the modelled rate and no more; a slower one earns less.",
            "- Respawn, node density and contention are not modelled, so the study compares one site against one site, not sites per hour.",
            "- Perks other than the Treasure Seeker effects the Search board reads are not modelled. The client was not run."
        };
    }
}
