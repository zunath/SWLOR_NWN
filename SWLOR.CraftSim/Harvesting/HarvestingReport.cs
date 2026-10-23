using System.Globalization;
using System.Reflection;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Legacy.Gathering;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Harvesting
{
    /// <summary>
    /// The harvesting study: Quick Harvest and Prospecting sessions against today's harvests. It runs the economy-parity gate (yield per
    /// node), the length gate and the XP-rate gate over the harvesting cohort, plus the evidence the plan and the Prospector draft quote,
    /// and writes one markdown report in the style of the other balance reports.
    /// </summary>
    public static class HarvestingReport
    {
        public const string Title = "Crafting revamp: Prospecting (harvesting) against today's harvests";

        public const string QuickHarvestCapGate = "Quick Harvest XP rate cap";

        /// <summary>Plays every case in parallel, runs the three gates and the evidence, and returns the results the report shows.</summary>
        public static IReadOnlyList<GateResult> Run(GateContext context, HarvestingParity parity, GateThresholds thresholds)
        {
            parity.Prepare();

            var quickCohort = new HarvestingXpRateCohort(parity, HarvestingLoop.QuickHarvest);
            var skilledCohort = new HarvestingXpRateCohort(parity, HarvestingLoop.SkilledSession);
            var casualCohort = new HarvestingXpRateCohort(parity, HarvestingLoop.StrikeOnlySession);
            var quick = new XpRateGate(quickCohort, thresholds).Evaluate(context);
            var skilled = new XpRateGate(skilledCohort, thresholds).Evaluate(context);
            var casual = new XpRateGate(casualCohort, thresholds).Evaluate(context);

            var xpVerdict = Worst(quick.Verdict, skilled.Verdict);
            var results = new List<GateResult>
            {
                new HarvestingYieldGate(parity).Evaluate(context),
                new HarvestingLengthGate(parity).Evaluate(context),
                new GateResult(
                    GateNames.XpRate,
                    xpVerdict,
                    $"{quick.Summary} {skilled.Summary}",
                    quick.Tables.Concat(skilled.Tables).ToList()),
                QuickHarvestCap(quickCohort, context),
                new GateResult(
                    "Evidence: strike-only session XP rate",
                    GateVerdict.NotApplicable,
                    "Not a gate. A player who only strikes gets today's yield (the whole pool, through the strikes that break into seams) in less time and fewer charges than Quick Harvest, so the session pays at least today's XP per hour.",
                    casual.Tables),
                CostsEvidence(parity),
                FastEvidence(parity),
                RankEvidence(parity),
                RecklessEvidence(parity),
                RefiningEvidence()
            };
            return results;
        }

        public static string Build(
            GateContext context,
            HarvestingParity parity,
            GateThresholds thresholds,
            IReadOnlyList<GateResult> results,
            string command) =>
            GateReport.Build(Title, context, results, command, Inputs(parity, thresholds), Limitations(parity));

        private static GateVerdict Worst(params GateVerdict[] verdicts) =>
            verdicts.Contains(GateVerdict.Failed) ? GateVerdict.Failed
            : verdicts.Contains(GateVerdict.NotRun) ? GateVerdict.NotRun
            : GateVerdict.Passed;

        /// <summary>Quick Harvest "never trains Gathering faster than today": its XP per hour is today's, no more.</summary>
        private static GateResult QuickHarvestCap(HarvestingXpRateCohort cohort, GateContext context)
        {
            var table = new EvidenceTable("Group", "Today, XP per hour", "Quick Harvest, XP per hour", "Ratio", "Verdict");
            var failures = new List<string>();
            foreach (var xpCase in cohort.Cases(context))
            {
                var today = cohort.PlayLegacy(xpCase, context).XpPerHour(cohort.LegacyTiming);
                var quick = cohort.PlayReplacement(xpCase, context).XpPerHour(cohort.ReplacementTiming);
                var ratio = quick / today;
                var passed = ratio <= 1 + 1e-9;
                if (!passed)
                    failures.Add($"{xpCase.Group} ({Number(ratio, 3)}x)");

                table.Add(xpCase.Group, Number(today, 0), Number(quick, 0), Number(ratio, 3), passed ? "Passed" : "Failed");
            }

            var tables = new[] { table.ToGateTable("Quick Harvest XP per hour against today's (rank = the vein's level)") };
            return failures.Count > 0
                ? new GateResult(QuickHarvestCapGate, GateVerdict.Failed, $"Quick Harvest trains Gathering faster than today in {failures.Count} case(s): {string.Join("; ", failures)}.", tables)
                : new GateResult(
                    QuickHarvestCapGate,
                    GateVerdict.Passed,
                    "Quick Harvest's XP per hour is exactly today's in every case: the same units, the same XP for each, one harvest delay for each.",
                    tables);
        }

        /// <summary>Charges, time and gems per node: the cost of playing and what the upside is made of.</summary>
        private static GateResult CostsEvidence(HarvestingParity parity)
        {
            var table = new EvidenceTable(
                "Node tier", "Rank", "Charges per node, today and Quick Harvest", "Charges per node, strike only", "Charges per node, skilled",
                "Charges per item, skilled", "Seconds per node, today and Quick Harvest", "Seconds per node, strike only", "Seconds per node, skilled",
                "Gems per node, today", "Gems per node, skilled", "Gems, skilled / today");
            foreach (var tier in parity.Tiers)
            {
                var rank = Math.Min(50, GatheringLevels.ForTier(tier));
                var casual = parity.Mean(tier, rank, ProspectorPlan.Casual);
                var skilled = parity.Skilled(tier, rank).Mean;
                var today = HarvestingParity.TodayItemsPerNode;
                var todayGems = parity.TodayGemsPerNode(tier);
                table.Add(
                    tier.ToString(),
                    rank.ToString(),
                    Number(today, 2),
                    Number(casual.Charges, 2),
                    Number(skilled.Charges, 2),
                    Number(skilled.Charges / skilled.Items, 2),
                    Number(today * parity.Options.SecondsPerHarvest, 1),
                    Number(casual.Seconds, 1),
                    Number(skilled.Seconds, 1),
                    Number(todayGems, 3),
                    Number(skilled.Gems, 3),
                    todayGems <= 0 ? "n/a" : Number(skilled.Gems / todayGems, 1) + "x");
            }

            return new GateResult(
                "Evidence: charges, time and gems",
                GateVerdict.NotApplicable,
                "Not a gate. What a node costs and what the skilled player's extra items are made of. Today a unit costs one charge and 5 seconds, and its roll of the table is a gem with the table's gem weight over its whole weight.",
                new[] { table.ToGateTable("Charges, seconds and gems per node at rank = the vein's level (a Might modifier of 0 for today)") });
        }

        /// <summary>The player in a hurry: Heavy Strike plans take the same items in fewer actions and less time.</summary>
        private static GateResult FastEvidence(HarvestingParity parity)
        {
            var table = new EvidenceTable(
                "Node tier", "Rank", "Careful, items per node", "Careful, actions", "Careful, XP per hour / today", "Heavy Strikes, items per node",
                "Heavy Strikes, actions", "Heavy Strikes, XP per hour / today");
            foreach (var tier in parity.Tiers)
            {
                var rank = Math.Min(50, GatheringLevels.ForTier(tier));
                var careful = parity.Skilled(tier, rank).Mean;
                var fast = parity.Mean(tier, rank, ProspectorPlan.Fast[0]);
                var todayRate = HarvestModel.ExpectedXpPerNode(tier, rank, 0) / (HarvestModel.ExpectedHarvestsPerNode * parity.Options.SecondsPerHarvest);
                var xp = HarvestingParity.SessionXpPerItem(tier, rank);
                table.Add(
                    tier.ToString(),
                    rank.ToString(),
                    Number(careful.Items, 2),
                    Number(careful.Actions, 2),
                    Number(careful.Items * xp / careful.Seconds / todayRate, 2),
                    Number(fast.Items, 2),
                    Number(fast.Actions, 2),
                    Number(fast.Items * xp / fast.Seconds / todayRate, 2));
            }

            return new GateResult(
                "Evidence: Heavy Strike plans",
                GateVerdict.NotApplicable,
                "Not a gate. A Heavy Strike is a faster Strike, not a bigger one: the plans that use it take the same items as the careful plans in fewer actions and less time, so their XP per hour is higher. They count toward the ceiling the +15% cap is checked against.",
                new[] { table.ToGateTable("Careful play against Heavy Strike play at rank = the vein's level") });
        }

        /// <summary>What the Gathering rank milestones do, rank by rank, at a tier-1 vein that every rank can work.</summary>
        private static GateResult RankEvidence(HarvestingParity parity)
        {
            var table = new EvidenceTable(
                "Rank", "Seams one Survey reveals", "Tier of yield", "Units of a Strike (mean)", "Units of a Heavy Strike (mean)", "Skilled items per node",
                "Skilled actions per node", "Strike-only actions per node");
            var tier = parity.Tiers.Min();
            foreach (var rank in parity.RanksFor(tier))
            {
                var setup = ProspectingSetup.ForNode(new ProspectingNode(4, ProspectingRules.MaxStability, default), 10, rank, tier);
                var skilled = parity.Skilled(tier, rank).Mean;
                var casual = parity.Mean(tier, rank, ProspectorPlan.Casual);
                table.Add(
                    rank.ToString(),
                    setup.SurveyReveals.ToString(),
                    setup.YieldTier.ToString(),
                    Number(ProspectingRules.ExpectedUnits(setup.StrikeYield), 2),
                    Number(ProspectingRules.ExpectedUnits(setup.HeavyStrikeYield), 2),
                    Number(skilled.Items, 3),
                    Number(skilled.Actions, 2),
                    Number(casual.Actions, 2));
            }

            return new GateResult(
                "Evidence: Gathering rank milestones",
                GateVerdict.NotApplicable,
                "Not a gate. The milestones replace Might and Perception: they reveal more seams and take more units per action. The pool still ends the node, so they shorten a session and raise no yield on their own.",
                new[] { table.ToGateTable($"Rank milestones at a tier {tier} node (the same milestones apply at every tier)") });
        }

        /// <summary>The player who spends Stability on seams without keeping enough to mine the pool.</summary>
        private static GateResult RecklessEvidence(HarvestingParity parity)
        {
            var table = new EvidenceTable(
                "Node tier", "Rank", "Seams first, no reserve: items per node", "Share of nodes collapsed", "Units lost with the node", "Skilled items per node");
            foreach (var tier in parity.Tiers)
            {
                var rank = Math.Min(50, GatheringLevels.ForTier(tier));
                var reckless = parity.Mean(tier, rank, ProspectorPlan.Reckless);
                var skilled = parity.Skilled(tier, rank).Mean;
                var sample = parity.Play(new HarvestCase(tier, rank, true), ProspectorPlan.Reckless);
                table.Add(
                    tier.ToString(),
                    rank.ToString(),
                    Number(reckless.Items, 2),
                    Percent(reckless.Collapsed, 1),
                    Number(sample.PoolLeftPerNode, 2),
                    Number(skilled.Items, 2));
            }

            return new GateResult(
                "Evidence: collapse",
                GateVerdict.NotApplicable,
                "Not a gate. Stability is the node's budget: a player who surveys and extracts without leaving enough to mine the pool can collapse the node and lose the units left in it. The window warns before the click.",
                new[] { table.ToGateTable("A player who does not keep a Stability reserve (gem-bearing node kinds)") });
        }

        /// <summary>Refining's XP per unit now, against what harvesting pays for the same tier and what refining paid before.</summary>
        private static GateResult RefiningEvidence()
        {
            var table = new EvidenceTable(
                "Ore", "Ore tier", "Ore activity level", "Refining XP per unit before", "Refining XP per unit at rank = level", "At level + 5 ranks",
                "At level + 10 ranks", "At rank 0", "At rank 50", "Harvest XP per item of the same tier at rank = level");
            foreach (var ore in RefiningModel.Ores())
            {
                var level = GatheringLevels.ForTier(ore.RequiredLevel);
                table.Add(
                    ore.Ore,
                    ore.RequiredLevel.ToString(),
                    level.ToString(),
                    ore.XpPerUnit.ToString(),
                    RefiningModel.CurrentXpPerUnit(ore, level).ToString(),
                    RefiningModel.CurrentXpPerUnit(ore, level + 5).ToString(),
                    RefiningModel.CurrentXpPerUnit(ore, level + 10).ToString(),
                    RefiningModel.CurrentXpPerUnit(ore, 0).ToString(),
                    RefiningModel.CurrentXpPerUnit(ore, 50).ToString(),
                    HarvestModel.XpPerItem(ore.RequiredLevel, level).ToString());
            }

            return new GateResult(
                "Evidence: refining XP",
                GateVerdict.NotApplicable,
                "Not a gate: refining has no minigame and no legacy rate to match. Refining pays GetDeltaXP(ore activity level - rank) for each unit, where an ore's activity level is the level of the vein of its tier (10 x (tier - 1) + 5, so 5, 15, 25, 35 and 45). The ore of the veins' tier pays the same per unit as harvesting that vein, so a unit that is harvested and then refined pays twice at the same rank.",
                new[] { table.ToGateTable("Refining XP per unit by ore, before and now") });
        }

        public static IReadOnlyList<string> Inputs(HarvestingParity parity, GateThresholds thresholds)
        {
            var options = parity.Options;
            var lines = new List<string>
            {
                "- Today (the loop Quick Harvest keeps): a node holds d4 harvests, set when it is first worked. Each harvest spends one charge and one harvest delay " +
                $"({Number(options.SecondsPerHarvest, 1)} s) for one roll of the node's loot table, and pays `GetDeltaXP(10 x (tier - 1) + 5 - rank)` XP. " +
                "It also gave a bonus item at 25% per Might modifier point; the owner's attribute parity reference is a Might modifier of 0, so today's yield is " +
                $"{Number(HarvestingParity.TodayItemsPerNode, 2)} items, charges and {Number(HarvestingParity.TodayItemsPerNode * options.SecondsPerHarvest, 1)} s per node, and no code reads an attribute now.",
                "- Quick Harvest: `ProspectingQuickHarvest`, the rule the game runs. It takes the units the node has left, as far as the harvester's charges go, a charge and a harvest delay for each, and pays today's XP for each, with no multiplier and no rank milestone.",
                $"- A session: the Prospecting engine, {parity.Runs} nodes per case, each with its own dice from seed {parity.Seed}. A node is made as the game makes one (`ProspectingNode.Create`): a pool of 1-4 units, Stability 100, one to three hidden seams that each seal a unit, " +
                $"a gem pocket with {ProspectingRules.GemPocketPercent}% chance on a node whose table has a gem. Harvester: {options.Charges} charges (a fresh harvester).",
                "- Players: the skilled player is the best of three careful plans (survey once, twice or until every seam is found, extracting what is found and mining with Strikes), chosen for each tier and rank by the most items per node; every plan reads only what the window shows and keeps enough Stability and charges to mine the pool. " +
                "The length and XP-rate gates use that player. The +15% cap is checked against every plan, three more that use Heavy Strikes included. " +
                "The casual player strikes the node until it is spent. The reckless player surveys and extracts without a reserve.",
                $"- Parity: Quick Harvest equals today's expected items, XP, seconds and charges per node; a skilled session stays within {Percent(options.SkilledCapShare)} above and not more than {Percent(1 - options.SkilledFloorShare)} below today's items per node (the plan's +15%; the floor is the sampling noise).",
                $"- Length: the skilled player averages {Number(options.LeastActions, 0)}-{Number(options.MostActions, 0)} actions per node (the plan's target for a gathering node).",
                "- XP-rate cohort (gate 6): per node tier, the rank equal to the vein's level (5, 15, 25, 35, 45) with the highest harvester tier that rank allows (`CraftRankMilestones`); XP per item, at the attribute reference. " +
                $"Time is the sum of the seconds of the actions taken ({Number(ProspectingRules.SurveySeconds, 1)} s a Survey, {Number(ProspectingRules.StrikeSeconds, 1)} s a Strike, {Number(ProspectingRules.HeavyStrikeSeconds, 1)} s a Heavy Strike, {Number(ProspectingRules.ExtractSeconds, 1)} s an Extract, Leave immediate); " +
                "the game enforces it, because an action resolves only after its seconds and the window cannot be clicked meanwhile. Both loops are in units of one harvest delay. Unmeasured estimates until the client run.",
                $"- The session's XP multiplier by the vein's level (`CraftMinigameXp`): {string.Join(", ", Enumerable.Range(1, 5).Select(tier => $"tier {tier} {Number(CraftMinigameXp.MultiplierFor(CraftMinigameType.Prospecting, GatheringLevels.ForTier(tier)), 2)}x"))}. 1.00x is today's XP per item; Quick Harvest never takes a multiplier.",
                "- Rank milestones (`CraftRankMilestones`): Strike and Heavy Strike yield more at Gathering " +
                $"{string.Join(", ", CraftRankMilestones.ProspectingYieldMilestoneRanks)} (replacing Might), and a Survey reveals one more seam at {string.Join(" and ", CraftRankMilestones.ProspectingSurveyMilestoneRanks)} (replacing Perception).",
                "- Node kinds, read from the module's placeables and the harvesting loot tables:"
            };

            foreach (var tier in parity.Tiers)
            {
                var nodes = parity.Nodes.Where(node => node.Tier == tier).ToList();
                lines.Add($"  - Tier {tier} (level {GatheringLevels.ForTier(tier)}, harvester tier {tier}): " +
                          string.Join(", ", nodes.Select(node => $"`{node.Resref}` ({(node.HasGems ? "gem " + Percent(node.GemChance, 1) + " of a roll" : "no gem")})")));
            }

            lines.Add("- Named starting values of the session (`ProspectingRules`), all tunable:");
            foreach (var (name, value) in StartingValues())
                lines.Add($"  - `{name}` = {value}");

            return lines;
        }

        /// <summary>Every starting value of Prospecting, by name, as the report records them.</summary>
        public static IReadOnlyList<(string Name, string Value)> StartingValues() =>
            typeof(ProspectingRules).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsLiteral)
                .Select(field => (field.Name, Convert.ToString(field.GetRawConstantValue(), CultureInfo.InvariantCulture)))
                .ToList();

        public static IReadOnlyList<string> Limitations(HarvestingParity parity) => new[]
        {
            "- The skilled player is the best of a set of plans a person can follow from the window, not an optimal player. A better plan could take more items per node, so the +15% cap is checked against these plans, and the margin below it is the room for one.",
            "- Items are counted, not priced. A gem pocket gives a gem in place of a unit, so it adds no item but moves value into gems; the evidence table shows the gem supply against today's. The extra ore roll of a rich seam is the only way a session yields more than the pool.",
            "- Respawn, node density and travel are not modelled, so these are yields per node, not per hour. The XP-rate gate counts the seconds of the actions only; walking between nodes costs both loops the same.",
            $"- Seconds per action are estimates until the client run measures them. They are the cohort's only timing input: a session whose actions take longer than modelled earns less per hour than the gate shows, and one that takes less earns more.",
            "- A skilled session spends more charges per item than today (the evidence table), because Survey and Extract cost charges and yield no unit of their own. A harvester lasts fewer skilled sessions than Quick Harvests; a strike-only session spends fewer charges than either.",
            "- Perks are not modelled; the Prospector draft's magnitudes are estimates against these sessions, and no perk code exists. The client was not run."
        };
    }
}
