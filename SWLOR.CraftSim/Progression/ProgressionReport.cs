using System.Text;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Legacy;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;
using SWLOR.Game.Server.Service.CraftService.Research;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.CraftSim.Report.MarkdownTable;
using SWLOR.CraftSim.Report;

namespace SWLOR.CraftSim.Progression
{
    /// <summary>Writes the progression-pace baseline, its sensitivities and the proposals for the owner as markdown.</summary>
    public static class ProgressionReport
    {
        /// <summary>The guild task XP awards the report prices, as a share of the craft's base XP per item handed in.</summary>
        public static readonly IReadOnlyList<double> GuildXpShares = new[] { 0.25, 0.5, 1.0 };

        /// <summary>How far above today's rate a played gathering session may train: the plan's gate 5 starting value, +15%.</summary>
        public const double PlayedSessionParity = 1.15;

        /// <summary>The share the report recommends, and the one the XP target options assume.</summary>
        public const double RecommendedGuildXpShare = 0.5;

        private static readonly (int First, int End)[] Bands = { (0, 10), (10, 20), (20, 30), (30, 40), (40, 50) };

        public static string Build(ProgressionModel model, ProgressionAssumptions a, string command)
        {
            var text = new StringBuilder();
            var gearNames = new[] { CraftingGearCatalog.NoGearName, CraftingGearCatalog.MidGearName, CraftingGearCatalog.BestGearName };
            var skills = model.Skills.ToList();
            var paths = skills.ToDictionary(s => s, s => gearNames.ToDictionary(g => g, g => model.Path(s, g, a)));
            var rpHours = (double)Enumerable.Range(0, ProgressionAssumptions.MaximumRank).Sum(Skill.GetRequiredXP) / ProgressionAssumptions.RoleplayXpPerHour;

            text.AppendLine("# Crafting revamp: progression pace baseline (workstream 11)");
            text.AppendLine();
            text.AppendLine("How long a new crafter takes today to reach rank 50 in each crafting skill and in Gathering, with the component gathering that supplies the crafts, " +
                            "measured with the legacy craft model (`design/testing/crafting-revamp/legacy-crafting-baseline.md`), and what the owner's October 2026 decisions change. " +
                            "The tables below the decisions are today's pace; the decisions are applied in code in workstream 11b and their resulting hours are in \"Decisions applied\". " +
                            "The XP-rate gate (gate 6) measures new minigames against today's XP first; the decisions apply on top of that result.");
            text.AppendLine();

            var after = a with { CraftXpMultiplier = CraftMinigameXp.CraftingPaceMultiplier };
            AppendSummary(text, model, a, paths, rpHours, gearNames);
            AppendDecisions(text, model, a, after, skills);
            AppendInputs(text, model, a, command, skills);
            AppendBands(text, model, a, paths, gearNames, skills);
            AppendGathering(text, model, a);
            AppendComponents(text, model, a, paths, skills);
            AppendSensitivity(text, model, a, skills);
            AppendGuildCohort(text, model, a, skills);
            if (skills.Contains(SkillType.Fabrication))
                AppendResearchCohort(text, model, after);
            AppendProposals(text, model, a, skills, rpHours);
            AppendLimitations(text);
            return text.ToString();
        }

        // ---------------------------------------------------------------------------------------------------------------------------

        private static void AppendSummary(
            StringBuilder text, ProgressionModel model, ProgressionAssumptions a,
            Dictionary<SkillType, Dictionary<string, SkillPath>> paths, double rpHours, string[] gearNames)
        {
            var mid = CraftingGearCatalog.MidGearName;
            text.AppendLine("## Hours to rank 50 today");
            text.AppendLine();
            text.AppendLine($"A new crafter (rank 0, nothing handed in to a guild) who always crafts recipes of their own level with the legacy lookahead player. " +
                            $"Hours are craft window time ({Number(a.SecondsPerAction)} s per action plus {Number(a.OverheadSecondsPerCraft, 0)} s per craft) plus the time to gather and refine the components " +
                            "(node travel is an unmeasured assumption, below). Mid gear is the headline; the band-by-band tables give all three gear levels.");
            text.AppendLine();
            var table = new MarkdownTable(
                "Skill", "Crafts to 50 (mid gear)", "Craft window h", "Gathering h", "Total h: no gear", "Total h: mid gear", "Total h: best gear",
                "XP per hour (mid gear)", "Component units no node supplies (mid gear)");
            foreach (var (skill, byGear) in paths)
            {
                var m = byGear[mid];
                table.Add(
                    skill.ToString(), Number(m.Crafts, 0), Number(m.CraftHours, 1), Number(m.GatherHours, 1),
                    Number(byGear[gearNames[0]].TotalHours, 1), Number(m.TotalHours, 1), Number(byGear[gearNames[2]].TotalHours, 1),
                    Whole(m.XpPerHour), Number(m.UnmodelledUnits, 0));
            }

            var gathering = model.Gathering(GatheringStrategy.Best, a);
            var harvest = model.Gathering(GatheringStrategy.HarvestAndRefine, a);
            var scavenge = model.Gathering(GatheringStrategy.Scavenge, a);
            table.Add(
                "Gathering (the skill)", "-", "-", Number(gathering.TotalHours, 1), Number(gathering.TotalHours, 1), Number(gathering.TotalHours, 1),
                Number(gathering.TotalHours, 1), Whole(gathering.XpPerHour), "-");
            text.AppendLine(table.ToString());
            var requiredXp = paths.Values.First()[mid].RequiredXp;
            text.AppendLine($"Rank 50 costs {Whole(requiredXp)} XP in every skill (`Skill.GetRequiredXP`, ranks 0-49). RP XP pays {Whole(ProgressionAssumptions.RoleplayXpPerHour)} XP per hour with no materials " +
                            $"(1,500 base XP every 30 minutes; the public mechanism in `RoleplayXP.cs`), which is {Number(rpHours, 0)} hours for the same XP. Gathering takes " +
                            $"{Number(harvest.TotalHours, 1)} h by harvesting and refining alone, {Number(scavenge.TotalHours, 1)} h by scavenging alone and {Number(gathering.TotalHours, 1)} h taking the better of the two at each rank.");
            text.AppendLine();

            var fastest = paths.OrderBy(p => p.Value[mid].TotalHours).First();
            var slowest = paths.OrderByDescending(p => p.Value[mid].TotalHours).First();
            text.AppendLine("What the numbers say:");
            text.AppendLine();
            text.AppendLine($"- **Crafts, not just components.** Mid gear needs about {Number(paths.Values.Average(p => p[mid].Crafts), 0)} at-level crafts to reach rank 50, not the 340-420 in the plan's research: " +
                            "that range assumes near-full quality, and the lookahead player's quality at mid gear is " +
                            $"{Percent(paths.Values.Average(p => p[mid].Steps.Sum(st => st.Crafts * st.SuccessRate * st.QualityFraction) / p[mid].Steps.Sum(st => st.Crafts * st.SuccessRate)))} on average (quality adds up to 100% to a craft's XP).");
            text.AppendLine($"- **Pace.** The craft loop pays {Whole(slowest.Value[mid].XpPerHour)}-{Whole(fastest.Value[mid].XpPerHour)} XP per hour with its gathering, " +
                            $"{Number(slowest.Value[mid].XpPerHour / ProgressionAssumptions.RoleplayXpPerHour, 1)}-{Number(fastest.Value[mid].XpPerHour / ProgressionAssumptions.RoleplayXpPerHour, 1)} times what RP XP pays. " +
                            "Players who bypass crafting with RP XP are buying out the materials and gathering, not a faster rate.");
            foreach (var (skill, byGear) in paths.OrderByDescending(p => p.Value[mid].GatherHours / p.Value[mid].TotalHours).Take(1))
            {
                var path = byGear[mid];
                var byComponent = model.GatherHoursByComponent(path, a).Where(c => c.Value > 0).OrderByDescending(c => c.Value).Take(3).ToList();
                var perUnit = byComponent.Select(c => model.Supply.SecondsPerUnit(c.Key, a) / 60).ToList();
                text.AppendLine($"- **Gathering dominates {skill}.** {Percent(path.GatherHours / path.TotalHours)} of its hours are gathering. The three components that cost the most are " +
                                string.Join(", ", byComponent.Select(c => $"`{c.Key}` ({Number(c.Value, 1)} h)")) +
                                $"; each drops on a small share of rolls (gems 2-9%, crate goods 15%), so one unit costs {Number(perUnit.Min(), 1)}-{Number(perUnit.Max(), 1)} minutes.");
            }

            text.AppendLine("- **Gathering is quick for the skill itself.** Each harvested item pays up to 1,200 XP and refining pays 25-150 XP per unit at the ore's level (the model counts that at every rank), so Gathering rank 50 takes hours, not days, if nodes are free. Node respawns and contention are not modelled.");
            text.AppendLine("- **Agriculture has a faster path than the one modelled here.** Fishing pays the same chart per bite (600-1,200 XP); see the catch task table in the guild section.");
            text.AppendLine();
        }

        private static void AppendDecisions(
            StringBuilder text, ProgressionModel model, ProgressionAssumptions today, ProgressionAssumptions after, List<SkillType> skills)
        {
            var mid = CraftingGearCatalog.MidGearName;
            text.AppendLine("## Decisions applied (owner, October 2026)");
            text.AppendLine();
            text.AppendLine($"1. **Guild hand-ins give XP.** The Smithery, Engineering, Fabrication and Agriculture guilds pay {GuildTaskXp.XpPercent}% of `Skill.GetDeltaXP(task level - rank)` for every item handed in, in the guild's skill " +
                            "(`GuildTaskXp`), with the task level the recipe level for a crafted item and the `FishType` level for a catch task. It goes through the crafting XP path, so Social and food bonuses apply, and the chart's cutoff stays: " +
                            "a hand-in five or more levels below the rank pays nothing, as crafting and quick craft (rank at least level + 5) do. The Hunter's Guild gives none.");
            text.AppendLine($"2. **Crafting XP x{Number(CraftMinigameXp.CraftingPaceMultiplier, 2)}.** `CraftMinigameXp.CraftingPaceMultiplier` multiplies the base XP of crafts in Smithery, Engineering, Fabrication and Agriculture's cooking, on top of each minigame's gate-6 band multiplier. " +
                            "It does not apply to Gathering, Espionage, fishing, guild hand-ins, research, combat or Piloting. Gate 6 is still measured against today's XP, before this multiplier.");
            text.AppendLine($"3. **Research XP scaled.** `ResearchXpRules` now pays {ResearchXpRules.ExperimentXpPercent}% (experiment) and {ResearchXpRules.CompletionXpPercent}% (completion) of the chart XP, +{ResearchXpRules.LevelBonusPercentPerLevel}% per research level, " +
                            "down from 100% and 50%, so research XP per real hour stays under at-level Fabrication crafting XP per hour (see the research cohort).");
            text.AppendLine();
            text.AppendLine("Hours to rank 50 at mid gear, today against with decisions 1 and 2 (hand-ins at the recommended award, crafts at the pace multiplier; the same unmeasured timings as the rest of this report):");
            text.AppendLine();
            var table = new MarkdownTable(
                "Skill", "Today h", "With the decisions h", "Change", "Crafts to 50 today", "Crafts to 50 with the decisions", "Hand-in XP share of the climb", "At-level hand-ins",
                "Catch-up hand-ins (quick craft)", "Catch-up h (in the total)", "Guild rank at rank 50");
            var todayHours = new List<double>();
            var afterHours = new List<double>();
            foreach (var skill in skills)
            {
                var before = model.Path(skill, mid, today);
                var plan = GuildHandInPlan.For(model.Guilds, GuildTaskCatalog.GuildOf(skill), GuildTaskXp.XpPercent / 100.0);
                var path = model.Path(skill, mid, after, plan);
                todayHours.Add(before.TotalHours);
                afterHours.Add(path.TotalHours);
                table.Add(
                    skill.ToString(), Number(before.TotalHours, 1), Number(path.TotalHours, 1), Percent(path.TotalHours / before.TotalHours - 1), Number(before.Crafts, 0),
                    Number(path.Crafts, 0), Percent(path.GuildXp / path.RequiredXp), Number(path.HandIns, 0), Number(path.CatchUpHandIns, 0), Number(path.CatchUpHours, 1),
                    path.GuildRankReached.ToString());
            }

            double Median(List<double> values)
            {
                var ordered = values.OrderBy(v => v).ToList();
                return (ordered[(ordered.Count - 1) / 2] + ordered[ordered.Count / 2]) / 2;
            }

            table.Add("Median skill", Number(Median(todayHours), 1), Number(Median(afterHours), 1), Percent(Median(afterHours) / Median(todayHours) - 1), "-", "-", "-", "-", "-", "-", "-");
            table.Add("Slowest skill", Number(todayHours.Max(), 1), Number(afterHours.Max(), 1), Percent(afterHours.Max() / todayHours.Max() - 1), "-", "-", "-", "-", "-", "-", "-");
            text.AppendLine(table.ToString());
            text.AppendLine($"The proposed target was the median skill at {Number(TargetHours, 0)} h or fewer. With the decisions it is {Number(Median(afterHours), 1)} h" +
                            (Median(afterHours) <= TargetHours ? ", which meets it." : $", {Number(Median(afterHours) - TargetHours, 1)} h over it.") +
                            " An at-level hand-in is one of the crafter's own crafts of a task recipe, which the model makes whenever the ten-task board holds one, so it adds no craft. " +
                            "A catch-up hand-in is a quick-crafted item that raises the guild rank when the rank stands in the way; its quick-craft and turn-in time is in the hours above and its XP is not paid (quick-crafted items sit five or more levels below the rank).");
            text.AppendLine();

            if (skills.Contains(SkillType.Fabrication))
            {
                var rows = ResearchCohort.Rows(model, mid, after).Where(r => r.BaseXp > 0).ToList();
                if (rows.Count > 0)
                {
                    var worst = rows.OrderByDescending(r => r.StartingToCraftingRatio).First();
                    text.AppendLine($"Research under the new values: the highest research XP per elapsed hour against at-level crafting XP per hour (with the x{Number(CraftMinigameXp.CraftingPaceMultiplier, 2)}) is " +
                                    $"{Percent(worst.StartingToCraftingRatio.Value)} at Fabrication {worst.FabricationRank}" +
                                    (worst.StartingToCraftingRatio <= 1 ? ", under the plan's ceiling at every rank." : ", **above the ceiling**.") +
                                    " Before the change the starting values (100% and 50%) paid 167% at Fabrication 10 in the baseline.");
                    text.AppendLine();
                }
            }
        }

        private static void AppendInputs(StringBuilder text, ProgressionModel model, ProgressionAssumptions a, string command, List<SkillType> skills)
        {
            text.AppendLine("## Inputs and assumptions");
            text.AppendLine();
            text.AppendLine($"- Command: `{command}`");
            text.AppendLine($"- Seed {model.Options.Seed}; {model.Options.Runs} lookahead crafts per recipe level, skill and gear level ({model.Options.Rollouts} sampled futures per action), each from its own stream of the seed, so a run repeats exactly. " +
                            "The hours themselves are expected values, not sampled.");
            text.AppendLine("- Recipes: the active recipes in the server's definitions that a new crafter can make without learning them first, of all the active ones (" +
                            string.Join("; ", skills.Select(s => $"{s} {model.Recipes.ForNewCrafter(s).Count()} of {model.Recipes.ForSkill(s).Count()}")) + "). The crafter's rank steps from 0 to 49 and each step crafts recipes of level max(1, rank): the `Skill.GetDeltaXP` price for the level equals the rank is 600 XP (676 at rank 0). Each of a level's recipes counts equally, as in the gates' cohort.");
            text.AppendLine("- Gear: none, tier 3 (mid) and tier 5 (best) of the skill's own crafting gear (`legacy-crafting-baseline.md`, \"Gear levels\"). Food and status bonuses are not played.");
            text.AppendLine("- Craft player and XP: the legacy lookahead (`RolloutPolicy`) at the recipe's level and rank. A success pays the base XP plus the quality share; a failure pays 15% of the base (`CraftOutcomeRules`). No blueprint, no enhancements, no XP bonuses from Social or food, and no first-craft bonus unless the variety row below says so.");
            text.AppendLine($"- Time in the craft window: {Number(a.SecondsPerAction)} s per action (the legacy window's estimate) plus **{Number(a.OverheadSecondsPerCraft, 0)} s per craft** to open the station, pick the recipe, commit the components and collect the item. The overhead is an unmeasured estimate; the gates' own overhead defaults to 0. The sensitivity table shows 15 s and 60 s.");
            text.AppendLine($"- Components: a craft commits every component stack of the recipe and a failure loses each stack on 65% of rolls (`CraftViewModel.cs:1601-1641`), so a craft consumes success + 0.65 x failure of the committed units. " +
                            "A unit is priced at the cheapest loot table in `HarvestingLootTableDefinition` that drops it, worked the way the game works that table. " +
                            $"Ore veins and trees are harvested (`res_used` on the placeable): each harvester use takes {Number(a.SecondsPerHarvest)} s (the item's `Delay(5f)`) plus its share of {Number(a.NodeTravelSeconds, 0)} s node travel over the node's 2.5 average harvests, and rolls the table once (plus the Might bonus roll; Might modifier {a.MightModifier}). " +
                            $"Bushes, herbs, vegetable patches and supply crates are scavenge sites (`scav_opened`, `SCAVENGE_POINT_LOOT_TABLE_NAME`): a visit costs the {Number(a.NodeTravelSeconds, 0)} s walk plus {Number(a.ScavengeVisitSeconds)} s for the one search animation of the visit (a second attempt adds no time), the site can be searched once, and each successful search (`d20 + Perception >= 6`, then 6 + d3 on the second attempt that Hard Look gives) is one roll of the table, giving 1 to the item's maximum quantity. " +
                            "Hard Look follows the gatherer's Gathering tier (`CraftRankMilestones.GetHardLookLevel`), which the model sets to the tier of the crafter's own crafting rank at each step (tier 1 below rank 10, one more every 10 ranks) and never below the site's level, so a higher-rank crafter searches low-level sites twice more often; the Perception modifier is 0. " +
                            "A roll yields one item of the table, so a recipe that needs two items of one table is priced at the expected visits until both counts are met (the expected maximum of the items' independent arrival times, checked against an exact multinomial calculation in the tests), which lies between the scarcer item's time and the sum of both.");
            text.AppendLine($"- Refined ore (`ref_*`) costs the raw ore plus refining: {Number(a.RefineSecondsPerBatch, 0)} s per operation (`RefiningDelaySeconds`) over a batch of {a.RefineBatchUnits} units (a full 99-unit stack; the refinery accepts several stacks per operation, so this is cautious). The sensitivity table also prices a batch of one power core (4 units). Power cores cost credits, not time.");
            text.AppendLine("- Asteroid ores (`ore_*`, the Engineering recipes' `ref_tilarium` and the like) are priced like a ground node of the same tier. Space mining is not modelled.");
            text.AppendLine($"- Node travel: **{Number(a.NodeTravelSeconds, 0)} s** between nodes, unmeasured. Respawn waits and other players at the node are not modelled, so gathering hours are a lower bound. Gathering rank is not a gate: the crafter is assumed to reach every node tier the recipe needs.");
            text.AppendLine("- Components no node supplies (enemy and crate drops of leather and electronics, meat, vendor goods, parts another recipe makes) are left out of the hours and counted in the summary column. The sensitivity table prices them at 20 s a unit, about a harvested unit.");
            text.AppendLine($"- Gathering the skill: each rank uses the better of harvesting and refining the best ground vein the tier allows (`CraftRankMilestones.TierForRank`) and searching the best scavenge site, at {Number(a.NodeTravelSeconds, 0)} s between nodes or sites and {Number(a.ScavengeVisitSeconds)} s of search animation a visit. Perception modifier {a.PerceptionModifier}.");
            text.AppendLine();
            text.AppendLine("Rules copied from the server (the model calls everything else directly), each checked against its source by `ProgressionSourceTests` or `LegacySourceTests`, "
                            + "except the loss rule of the deleted `CraftViewModel.cs`, which is frozen at commit `" + LegacySource.CraftViewModelCommit + "`:");
            text.AppendLine();
            var rules = new MarkdownTable("Rule", "Source");
            foreach (var source in ProgressionSource.All.Concat(LegacySource.All.Where(s =>
                         s.File.EndsWith("ProspectingNode.cs") || s.File.EndsWith("GatheringLevels.cs") || s.File.EndsWith("ScavengeQuickSearch.cs") || s.File.EndsWith("ScavengePoint.cs") ||
                         s.File.EndsWith("RefineryViewModel.cs") ||
                         s == LegacySource.FailureLoss || s == LegacySource.RecipeUnlockOffset)))
            {
                rules.Add(source.Name, $"`{source.Citation}`");
            }

            text.AppendLine(rules.ToString());
        }

        private static void AppendBands(
            StringBuilder text, ProgressionModel model, ProgressionAssumptions a,
            Dictionary<SkillType, Dictionary<string, SkillPath>> paths, string[] gearNames, List<SkillType> skills)
        {
            var mid = CraftingGearCatalog.MidGearName;
            text.AppendLine("## Hours per 10-rank band");
            text.AppendLine();
            text.AppendLine("A band is the hours to climb from its first rank to the next band's first rank (0-10 is rank 0 to rank 10). Crafts, craft window and gathering hours are mid gear; the three total columns give each gear level. " +
                            "\"RP XP h\" is the hours RP XP needs for the band's XP.");
            text.AppendLine();
            foreach (var skill in skills)
            {
                text.AppendLine($"### {skill}");
                text.AppendLine();
                var table = new MarkdownTable(
                    "Ranks", "XP", "Crafts (mid)", "Craft window h", "Gathering h", "Total h: no gear", "Total h: mid gear", "Total h: best gear", "XP per hour (mid)", "RP XP h");
                foreach (var (first, end) in Bands)
                {
                    var m = paths[skill][mid].Band(first, end).ToList();
                    var xp = m.Sum(s => s.RequiredXp);
                    var totals = gearNames.Select(g => paths[skill][g].Band(first, end).Sum(s => s.TotalHours)).ToList();
                    table.Add(
                        $"{first}-{end}", Whole(xp), Number(m.Sum(s => s.Crafts), 0), Number(m.Sum(s => s.CraftHours), 1), Number(m.Sum(s => s.GatherHours), 1),
                        Number(totals[0], 1), Number(totals[1], 1), Number(totals[2], 1), Whole(xp / totals[1]), Number((double)xp / ProgressionAssumptions.RoleplayXpPerHour, 1));
                }

                var all = paths[skill][mid];
                table.Add(
                    "0-50", Whole(all.RequiredXp), Number(all.Crafts, 0), Number(all.CraftHours, 1), Number(all.GatherHours, 1),
                    Number(paths[skill][gearNames[0]].TotalHours, 1), Number(all.TotalHours, 1), Number(paths[skill][gearNames[2]].TotalHours, 1),
                    Whole(all.XpPerHour), Number((double)all.RequiredXp / ProgressionAssumptions.RoleplayXpPerHour, 1));
                text.AppendLine(table.ToString());
            }
        }

        private static void AppendGathering(StringBuilder text, ProgressionModel model, ProgressionAssumptions a)
        {
            text.AppendLine("## Gathering rank 50");
            text.AppendLine();
            text.AppendLine("Hours per band for a new gatherer. Harvesting pays `GetDeltaXP(vein level - rank)` for every item, and a vein two tiers below the rank pays nothing, so the gatherer always works the top vein the tier allows; refining pays 25-150 XP per raw unit, counted at every rank.");
            text.AppendLine();
            var strategies = new[] { GatheringStrategy.HarvestAndRefine, GatheringStrategy.Scavenge, GatheringStrategy.Best };
            var paths = strategies.ToDictionary(s => s, s => model.Gathering(s, a));
            var table = new MarkdownTable("Ranks", "XP", "Harvest and refine h", "Scavenge h", "Better of the two h", "XP per hour (better)", "RP XP h");
            foreach (var (first, end) in Bands)
            {
                var best = paths[GatheringStrategy.Best].Band(first, end).ToList();
                var xp = best.Sum(s => s.RequiredXp);
                table.Add(
                    $"{first}-{end}", Whole(xp),
                    Number(paths[GatheringStrategy.HarvestAndRefine].Band(first, end).Sum(s => s.Hours), 2),
                    Number(paths[GatheringStrategy.Scavenge].Band(first, end).Sum(s => s.Hours), 2),
                    Number(best.Sum(s => s.Hours), 2), Whole(xp / best.Sum(s => s.Hours)), Number((double)xp / ProgressionAssumptions.RoleplayXpPerHour, 1));
            }

            var total = paths[GatheringStrategy.Best];
            table.Add(
                "0-50", Whole(total.RequiredXp), Number(paths[GatheringStrategy.HarvestAndRefine].TotalHours, 2),
                Number(paths[GatheringStrategy.Scavenge].TotalHours, 2), Number(total.TotalHours, 2), Whole(total.XpPerHour),
                Number((double)total.RequiredXp / ProgressionAssumptions.RoleplayXpPerHour, 1));
            text.AppendLine(table.ToString());

            var harvestPath = paths[GatheringStrategy.HarvestAndRefine];
            var scavengePath = paths[GatheringStrategy.Scavenge];
            text.AppendLine("The plan's quick gathering and the minigame sessions that replace the harvester and the search, for the same climb. **Quick Harvest** resolves a node at today's expected units and XP, one harvest delay per unit, and **Quick Search** resolves a site at today's expected rolls and XP, taking as long as today's attempts, " +
                            "so each is today's loop by construction and neither can train Gathering faster than the rows below. A played session may exceed it by the gate's starting value of " +
                            $"{Percent(PlayedSessionParity - 1)}; the minigames are not built yet, so that row is the cap the parity gate enforces, not a measurement.");
            text.AppendLine();
            var quick = new MarkdownTable("Cohort", "Gathering XP per hour (rank 0 to 50)", "Hours to rank 50", "Ceiling the plan sets");
            quick.Add("Quick Harvest (today's harvesting and refining)", Whole(harvestPath.XpPerHour), Number(harvestPath.TotalHours, 2), "today's rate, never above it");
            quick.Add("Quick Search (today's scavenging)", Whole(scavengePath.XpPerHour), Number(scavengePath.TotalHours, 2), "today's rate, never above it");
            quick.Add("Played Prospecting session at the parity cap", Whole(harvestPath.XpPerHour * PlayedSessionParity), Number(harvestPath.TotalHours / PlayedSessionParity, 2), $"at most {Percent(PlayedSessionParity - 1)} above today's");
            quick.Add("Played Search session at the parity cap", Whole(scavengePath.XpPerHour * PlayedSessionParity), Number(scavengePath.TotalHours / PlayedSessionParity, 2), $"at most {Percent(PlayedSessionParity - 1)} above today's");
            text.AppendLine(quick.ToString());

            text.AppendLine("Sensitivity of the better-of-two total to the node travel assumption, the only unmeasured input that moves it:");
            text.AppendLine();
            var travel = new MarkdownTable("Seconds between nodes", "Harvest and refine h", "Scavenge h", "Better of the two h");
            foreach (var seconds in new[] { 15.0, 30.0, 60.0, 120.0, 300.0 })
            {
                var scenario = a with { NodeTravelSeconds = seconds };
                travel.Add(
                    Number(seconds, 0), Number(model.Gathering(GatheringStrategy.HarvestAndRefine, scenario).TotalHours, 1),
                    Number(model.Gathering(GatheringStrategy.Scavenge, scenario).TotalHours, 1), Number(model.Gathering(GatheringStrategy.Best, scenario).TotalHours, 1));
            }

            text.AppendLine(travel.ToString());
        }

        private static void AppendComponents(
            StringBuilder text, ProgressionModel model, ProgressionAssumptions a,
            Dictionary<SkillType, Dictionary<string, SkillPath>> paths, List<SkillType> skills)
        {
            var mid = CraftingGearCatalog.MidGearName;
            text.AppendLine("## What supplies the components");
            text.AppendLine();
            text.AppendLine("Share of component units the at-level recipes commit, by where the model says they come from, over the whole climb. A unit only counts as gathered when a gathering loot table in `HarvestingLootTableDefinition` drops it (or its raw ore does).");
            text.AppendLine();
            var shares = new MarkdownTable("Skill", "Units per climb (mid gear)", "Refined ore", "Harvested items", "Made by another recipe", "Other (drops, vendor, quest)");
            foreach (var skill in skills)
            {
                var totals = Enum.GetValues<ComponentSource>().ToDictionary(source => source, _ => 0.0);
                foreach (var step in paths[skill][mid].Steps)
                {
                    var consumed = step.SuccessRate + 0.65 * (1 - step.SuccessRate);
                    foreach (var (source, units) in model.Demand(skill, step.RecipeLevel, a).UnitsBySource)
                        totals[source] += step.Crafts * consumed * units;
                }

                var sum = totals.Values.Sum();
                shares.Add(
                    skill.ToString(), Number(sum, 0), Percent(totals[ComponentSource.Refined] / sum), Percent(totals[ComponentSource.Harvested] / sum),
                    Percent(totals[ComponentSource.Crafted] / sum), Percent(totals[ComponentSource.Other] / sum));
            }

            text.AppendLine(shares.ToString());

            text.AppendLine("Gathering hours of the mid gear climb by kind of node. Each component is priced at the cheapest table that drops it. Ore veins and trees are harvested; bushes, herbs, vegetable patches and supply crates are scavenge sites; asteroid tables are mined from a ship and priced like a harvest node:");
            text.AppendLine();
            var families = skills.SelectMany(s => model.GatherHoursByFamily(paths[s][mid], a).Keys).Distinct().Where(f => f != ComponentSupply.NoNodeFamily).OrderBy(f => f, StringComparer.Ordinal).ToList();
            var header = new[] { "Skill" }.Concat(families).ToArray();
            var family = new MarkdownTable(header);
            foreach (var skill in skills)
            {
                var hours = model.GatherHoursByFamily(paths[skill][mid], a);
                family.Add(new[] { skill.ToString() }.Concat(families.Select(f => Number(hours.GetValueOrDefault(f), 1))).ToArray());
            }

            text.AppendLine(family.ToString());

            text.AppendLine("The five components that cost the most gathering hours in each skill's mid gear climb:");
            text.AppendLine();
            var top = new MarkdownTable("Skill", "Component", "Gathering h", "Share of the skill's gathering");
            foreach (var skill in skills)
            {
                var path = paths[skill][mid];
                foreach (var (component, hours) in model.GatherHoursByComponent(path, a).Where(c => c.Value > 0).OrderByDescending(c => c.Value).Take(5))
                    top.Add(skill.ToString(), $"`{component}`", Number(hours, 1), Percent(hours / path.GatherHours));
            }

            text.AppendLine(top.ToString());
        }

        private static void AppendSensitivity(StringBuilder text, ProgressionModel model, ProgressionAssumptions a, List<SkillType> skills)
        {
            var mid = CraftingGearCatalog.MidGearName;
            text.AppendLine("## Sensitivity");
            text.AppendLine();
            text.AppendLine("Total hours to rank 50 at mid gear when one unmeasured input or play choice changes. Every other row is the baseline above.");
            text.AppendLine();
            var scenarios = new (string Name, ProgressionAssumptions Assumptions)[]
            {
                ("Baseline", a),
                ("Overhead 15 s per craft", a with { OverheadSecondsPerCraft = 15 }),
                ("Overhead 60 s per craft", a with { OverheadSecondsPerCraft = 60 }),
                ("Node travel 15 s", a with { NodeTravelSeconds = 15 }),
                ("Node travel 60 s", a with { NodeTravelSeconds = 60 }),
                ("Node travel 120 s", a with { NodeTravelSeconds = 120 }),
                ("Might modifier +2 (half the harvests drop a second item)", a with { MightModifier = 2 }),
                ("Refining one power core (4 units) per operation", a with { RefineBatchUnits = 4 }),
                ("Components no node supplies cost 20 s a unit", a with { UnmodelledUnitSeconds = 20 }),
                ("Crafts the cheapest quarter of each level's recipes", a with { CheapestQuartileRecipes = true }),
                ("Rotates through each level's recipes (20% first-craft bonus on each)", a with { VarietyPlay = true })
            };
            var table = new MarkdownTable(new[] { "Scenario" }.Concat(skills.Select(s => s.ToString())).ToArray());
            foreach (var (name, scenario) in scenarios)
                table.Add(new[] { name }.Concat(skills.Select(s => Number(model.Path(s, mid, scenario).TotalHours, 1))).ToArray());

            text.AppendLine(table.ToString());
        }

        // ---------------------------------------------------------------------------------------------------------------------------

        private static void AppendGuildCohort(StringBuilder text, ProgressionModel model, ProgressionAssumptions a, List<SkillType> skills)
        {
            var mid = CraftingGearCatalog.MidGearName;
            var guilds = model.Guilds;
            text.AppendLine("## Guild task cohort");
            text.AppendLine();
            text.AppendLine("With the task cap lifted, a completed task goes back on the board at once for the Smithery, Engineering, Fabrication and Agriculture guilds, so hand-ins are a repeatable activity. " +
                            "A task wants one item (four raw fish for a catch task) and pays credits and guild points by its tier, which is the guild rank that unlocks it. Credits, guild points and the rank thresholds are unchanged.");
            text.AppendLine();
            var catalog = new MarkdownTable("Guild", "Tasks", "Crafted", "Caught", "Other", "Tier 0-4 recipe or fish levels", "Credits per task by tier", "Base guild points by tier");
            foreach (var guild in new[] { GuildType.SmitheryGuild, GuildType.EngineeringGuild, GuildType.FabricationGuild, GuildType.AgricultureGuild })
            {
                var tasks = guilds.For(guild).ToList();
                var tiers = tasks.Select(t => t.Tier).Distinct().OrderBy(t => t).ToList();
                catalog.Add(
                    guild.ToString(), tasks.Count.ToString(), tasks.Count(t => t.Kind == GuildTaskKind.Crafted).ToString(), tasks.Count(t => t.Kind == GuildTaskKind.Caught).ToString(),
                    tasks.Count(t => t.Kind == GuildTaskKind.Other).ToString(),
                    string.Join(", ", tiers.Select(t => { var levels = tasks.Where(x => x.Tier == t && x.Level.HasValue).Select(x => x.Level.Value).ToList(); return $"{levels.Min()}-{levels.Max()}"; })),
                    string.Join(" / ", tiers.Select(t => Whole(guilds.RewardOfTier(guild, t).Credits))),
                    string.Join(" / ", tiers.Select(t => guilds.RewardOfTier(guild, t).GuildPoints.ToString())));
            }

            text.AppendLine(catalog.ToString());
            text.AppendLine("Every crafted task asks for a player-produced item, which includes quick-crafted ones (`NwnCraftSessionWorld.CreateCraftedItem` stamps both). Tier k holds the recipes of levels 10k+1 to 10k+10. " +
                            "Guild rank 5 needs 45,000 GP, about 238 hand-ins when each is handed in at the tier of the rank it climbs from.");
            text.AppendLine();

            text.AppendLine("### Cohort: hand in what you craft");
            text.AppendLine();
            text.AppendLine("The cohort crafts at level as before and, whenever the board has a task for that recipe level and the guild rank allows it, hands the item in. " +
                            $"The board is the catch: `Guild.RefreshGuildTasks` keeps only {GuildBoard.TasksPerTier} random tasks of a tier at each server start (all of them when the tier has {GuildBoard.TasksPerTier} or fewer), the same for every player, and the cap change does not touch that. " +
                            "A level's task is therefore on the board with a chance, and the model weights every hand-in, its XP, points and credits by it (the chance that at least one of the level's tasks is among the ten drawn; Agriculture's catch tasks take board places too). " +
                            "A crafter who finds no task for their level on this boot's board hands nothing in, so a climb over many server starts gets the expected share. " +
                            "A hand-in asks for one exact recipe (the quest collector matches the item's resref), so when the board holds the level's task the crafter makes that recipe, not an arbitrary one of the level: the craft's components are the level's task recipes' mean, not the level's average over all recipes (a crafter who picks recipes by cost can do so with or without a guild; see the sensitivity table). " +
                            $"Each hand-in costs {Number(a.HandInSeconds, 0)} s (unmeasured). When the guild rank is what stands in the way, the crafter quick-crafts the preferred task item of their current tier that is on the board and that quick craft allows " +
                            $"(the cheapest to gather; rank at least level + {QuickCraftRules.RankMargin}, after one craft by hand; {QuickCraftBatch.SecondsPerItem:0} s per item, components gathered as above) and hands it in until the rank rises; the cost is the expected cost over which tasks the board holds, given that it holds one. A board with no eligible item makes the crafter wait for the next server start. The wait is not play time and is not charged, but the crafts made meanwhile cannot be handed in, so they are made without the award for the expected number of missed boards ({Number(a.PlayHoursPerServerStart, 0)} play hours per server start, unmeasured); the table gives the lowest chance of such a board and the expected server starts waited. Those items pay no XP. " +
                            "The award is a share of the craft's base XP (`GetDeltaXP(task level - rank)`, the same cutoff as crafting), so a hand-in five or more levels below the rank pays nothing and quick-crafted items never do.");
            text.AppendLine();
            var cohort = new MarkdownTable(
                "Skill", "Award share", "Total h (mid gear)", "Change vs no guild", "Change vs the 0% award", "At-level hand-ins", "Catch-up hand-ins", "Catch-up h",
                "Lowest chance a board held a catch-up item", "Server starts waited", "Guild rank at rank 50", "Hand-in XP", "Share of the climb's XP", "Credits earned");
            foreach (var skill in skills)
            {
                var guild = GuildTaskCatalog.GuildOf(skill);
                var baseline = model.Path(skill, mid, a);
                var zero = model.Path(skill, mid, a, GuildHandInPlan.For(guilds, guild, 0));
                cohort.Add(skill.ToString(), "no guild", Number(baseline.TotalHours, 1), "-", "-", "-", "-", "-", "-", "-", "-", "-", "-", "-");
                foreach (var share in new[] { 0.0 }.Concat(GuildXpShares))
                {
                    var path = model.Path(skill, mid, a, GuildHandInPlan.For(guilds, guild, share));
                    cohort.Add(
                        skill.ToString(), share == 0 ? "0% (hand-ins for credits and points only)" : Percent(share), Number(path.TotalHours, 1),
                        Percent(path.TotalHours / baseline.TotalHours - 1), share == 0 ? "-" : Percent(path.TotalHours / zero.TotalHours - 1),
                        Number(path.HandIns, 0), Number(path.CatchUpHandIns, 0), Number(path.CatchUpHours, 1), Percent(path.LowestCatchUpBoardChance), Number(path.ExpectedRestartsWaited, 2),
                        path.GuildRankReached.ToString(), Whole(path.GuildXp), Percent(path.GuildXp / path.RequiredXp), Whole(path.Credits));
                }
            }

            text.AppendLine(cohort.ToString());
            text.AppendLine("Read the award's effect against the 0% row, not against no guild: handing in the task recipes already changes the hours at a 0% award, because their components differ from the level's average recipe. " +
                            "Hand-in XP's share of the climb is the figure to read for the award: the proposal should land it between a fifth and a third, so it neither dominates nor vanishes.");
            text.AppendLine();

            text.AppendLine($"The same cohort by rank band at the recommended {Percent(RecommendedGuildXpShare)} award. The task tier a crafter can hand in at is capped by the guild rank, which the catch-up hand-ins raise; one item per task, taken from the crafter's own craft or, for the catch-up, from quick craft.");
            text.AppendLine();
            var bands = new MarkdownTable(
                "Skill", "Ranks", "Task tier at level", "Items per task", "At-level hand-ins", "Quick-crafted catch-up hand-ins", "Hand-in hours (turn-in and catch-up)", "Hand-in XP", "Share of the band's XP");
            foreach (var skill in skills)
            {
                var guild = GuildTaskCatalog.GuildOf(skill);
                var path = model.Path(skill, mid, a, GuildHandInPlan.For(guilds, guild, RecommendedGuildXpShare));
                foreach (var (first, end) in Bands)
                {
                    var steps = path.Band(first, end).ToList();
                    var xp = steps.Sum(s => s.RequiredXp);
                    bands.Add(
                        skill.ToString(), $"{first}-{end}", (first / 10).ToString(), "1",
                        Number(steps.Sum(s => s.HandIns), 0), Number(steps.Sum(s => s.CatchUpHandIns), 0),
                        Number(steps.Sum(s => s.HandInHours + s.CatchUpHours), 2), Whole(steps.Sum(s => s.GuildXp)), Percent(steps.Sum(s => s.GuildXp) / xp));
                }
            }

            text.AppendLine(bands.ToString());

            text.AppendLine("### The guild rank chore");
            text.AppendLine();
            text.AppendLine("What raising a guild from rank 0 to 5 costs a rank-50 crafter who quick-crafts the cheapest eligible task item of each tier that is on the board and hands it in (a board with none makes them wait for the next server start, which is not charged). Before the change the board held ten tasks a tier per server start, so rank 5 took at least 14-21 days.");
            text.AppendLine();
            var chore = new MarkdownTable("Guild", "Hand-ins", "Hours", "Credits earned", "Credits per hour", "Lowest chance a board holds an eligible item", "Server starts waited (not charged)");
            var choreDetail = new MarkdownTable("Guild", "Tier", "Item", "Item level", "Guild points each", "Hand-ins", "Hours", "Chance a board holds an eligible item");
            foreach (var guild in new[] { GuildType.SmitheryGuild, GuildType.EngineeringGuild, GuildType.FabricationGuild, GuildType.AgricultureGuild })
            {
                var rows = GuildCohorts.Chore(guilds, model.Supply, guild, a, ResultsOrNull(model, GuildTaskCatalog.SkillOf(guild), mid));
                chore.Add(
                    guild.ToString(), Number(rows.Sum(r => r.HandIns), 0), Number(rows.Sum(r => r.Hours), 1), Whole(rows.Sum(r => r.Credits)),
                    Whole(rows.Sum(r => r.Credits) / rows.Sum(r => r.Hours)), Percent(rows.Min(r => r.BoardChance)), Number(rows.Sum(r => r.ExpectedRestartsWaited), 2));
                foreach (var row in rows)
                    choreDetail.Add(guild.ToString(), row.Tier.ToString(), $"`{row.Resref}`", row.ItemLevel.ToString(), row.GuildPointsEach.ToString(), Number(row.HandIns, 0), Number(row.Hours, 2), Percent(row.BoardChance));
            }

            text.AppendLine(chore.ToString());
            text.AppendLine(choreDetail.ToString());

            text.AppendLine("### Catch tasks (Agriculture)");
            text.AppendLine();
            text.AppendLine("A catch task wants four raw fish of one kind (for example `agr_tsk_017`, four moat carp). The XP basis is the fish's `FishType` level against the Agriculture rank. " +
                            $"Fishing already pays `GetDeltaXP(fish level - rank)` on every bite, landed or not, at an unmeasured {Number(a.FishingSecondsPerBite, 0)} s per bite and the catch chance `40 + 10 x (rank - level) + 2 x Willpower` (Willpower score {a.WillpowerScore}). " +
                            "The table sets the rank equal to the tier's median fish level.");
            text.AppendLine();
            var catches = new MarkdownTable(
                "Tier", "Fish level", "Fish per task", "Catch chance", "Bites per task", "Bite XP each", "Seconds per task", "Bite XP per hour",
                "Hand-in XP at 50% per task", "Hand-in XP at 50% per fish", "Share of the task's XP: per task", "Share: per fish");
            foreach (var row in GuildCohorts.CatchTasks(guilds, a))
            {
                var perTask = row.HandInXp(0.5, false);
                var perFish = row.HandInXp(0.5, true);
                catches.Add(
                    row.Tier.ToString(), row.FishLevel.ToString(), row.Quantity.ToString(), Percent(row.CatchChance), Number(row.BitesPerTask, 1), row.BiteXp.ToString(),
                    Number(row.SecondsPerTask, 0), Whole(row.XpPerHour(0)), Number(perTask, 0), Number(perFish, 0),
                    Percent(perTask / (row.BiteXpPerTask + perTask)), Percent(perFish / (row.BiteXpPerTask + perFish)));
            }

            text.AppendLine(catches.ToString());
            text.AppendLine("Fishing pays several times what the cooking loop does per hour at this bite time, so Agriculture's pace is set by fishing for anyone who fishes. The table assumes a fish of the tier's level is available to the angler; location, rod and bait pools are not modelled.");
            text.AppendLine();

            text.AppendLine("### What limits the hand-ins");
            text.AppendLine();
            var rate = GuildCohorts.Chore(guilds, model.Supply, GuildType.SmitheryGuild, a, ResultsOrNull(model, SkillType.Smithery, mid));
            text.AppendLine("Every hand-in consumes one real player-crafted item (or four caught fish), which already limits the rate: the components. Nothing else limits it once the cap is lifted: the board's ten tasks per tier no longer bound it, because a completed task returns at once and one task can be repeated. " +
                            $"Quick craft makes a base item in {QuickCraftBatch.SecondsPerItem:0} s, so for a high-rank crafter the real bounds are the components and the turn-in time. " +
                            $"Credits are the lever that matters: a task pays {Whole(guilds.RewardOfTier(GuildType.SmitheryGuild, 0).Credits)} to {Whole(guilds.RewardOfTier(GuildType.SmitheryGuild, 4).Credits)} credits plus the Guild Relations and Social bonuses, and the Smithery rank climb alone pays " +
                            $"{Whole(rate.Sum(r => r.Credits))} credits for {Number(rate.Sum(r => r.Hours), 1)} hours of work. Nothing was added in this PR; the owner decides whether credits need a limit (see the proposals).");
            text.AppendLine();
        }

        private static void AppendResearchCohort(StringBuilder text, ProgressionModel model, ProgressionAssumptions a)
        {
            var rows = ResearchCohort.Rows(model, CraftingGearCatalog.MidGearName, a);
            text.AppendLine("## Research cohort");
            text.AppendLine();
            text.AppendLine("Research is a Fabrication XP source in the revamp. The Research workstream set starting values in `ResearchXpRules`: the experiment pays " +
                            $"{ResearchXpRules.ExperimentXpPercent}% of the chart XP at full effort and completing the job {ResearchXpRules.CompletionXpPercent}%, each raised {ResearchXpRules.LevelBonusPercentPerLevel}% per research level reached, " +
                            "where the chart XP is `GetDeltaXP(recipe level - Fabrication rank)`. This cohort is a Fabrication researcher at the tier their rank opens, doing the job loop that pays the most XP per hour, and it calls those rules directly. " +
                            "A job's length in seconds and its credit cost are one formula, `200 x tier^2.2 x level^2` (tier = recipe level / 10 + 1, at most 5; `Craft.CalculateResearchCost`), where level is the blueprint's new research level 1-10. " +
                            "A recipe's blueprint is first acquired by starting its level-1 job with no blueprint, so \"blueprint acquisition\" is that job. The tier gate is Fabrication rank / 10 and the job slots are 1, 2 from rank 25 and 3 from rank 50 (`CraftRankMilestones`). " +
                            $"The experiment cuts up to {ExperimentRules.TimeCutCapPercent}% of a job's time (`ExperimentRules.TimeCutCapPercent`), which the cohort takes as the best case.");
            text.AppendLine();
            text.AppendLine($"Time at the terminal per job: an experiment of {a.ResearchTrials} trials at {Number(a.ResearchSecondsPerTrial, 0)} s, plus {Number(a.ResearchVisitSeconds, 0)} s to start the job and {Number(a.ResearchVisitSeconds, 0)} s to collect it " +
                            $"({Number(a.ResearchTrials * a.ResearchSecondsPerTrial + 2 * a.ResearchVisitSeconds, 0)} s; unmeasured). The recipe researched is the one the research window lists (`Craft.IsResearchableRecipe`: weapons, armor and food) that needs no learning and whose level pays the most against the rank within the tier. " +
                            "\"Fast loop\" keeps every slot on level-1 jobs for new blueprints; \"climb\" runs each slot up one blueprint from level 1 to 10. Base XP is the chart XP for the recipe level against the rank.");
            text.AppendLine();
            var table = new MarkdownTable(
                "Fabrication rank", "Tier", "Recipe level", "Base XP", "Job slots", "Level-1 job", "Level-10 job", "Level-1 job credits", "Fast loop jobs per h", "Climb jobs per h",
                "Crafting XP per h (incl. gathering and the pace multiplier)", "Research XP per h at a 100% award");
            foreach (var row in rows.Where(r => r.FabricationRank % 5 == 0 || r.FabricationRank == ProgressionAssumptions.MaximumRank - 1))
            {
                table.Add(
                    row.FabricationRank.ToString(), row.Tier.ToString(), row.RecipeLevel.ToString(), row.BaseXp.ToString(), row.ConcurrentJobs.ToString(),
                    Hours(row.FirstJobSeconds), Hours(row.TenthJobSeconds), Whole(row.FirstJobCredits), Number(row.FastLoopJobsPerHour, 1), Number(row.ClimbJobsPerHour, 2),
                    Whole(row.CraftingXpPerHour), Whole(row.ResearchXpPerHourAtFullShare));
            }

            text.AppendLine(table.ToString());
            var paying = rows.Where(r => r.BaseXp > 0).ToList();
            var zero = rows.Count(r => r.BaseXp == 0);
            text.AppendLine($"The tier gate lags the rank, so the best recipe a researcher may start is between 1 and 10 levels below their rank, and the XP chart pays nothing for a recipe five or more levels below. " +
                            $"{zero} of the {rows.Count} Fabrication ranks 10-49 have no researchable recipe that pays XP at all under the starting values; research XP arrives in bursts in the first four ranks of each tier. " +
                            "The third job slot (Fabrication 50) arrives when the skill stops paying XP, so it matters for blueprints only.");
            text.AppendLine();
            if (paying.Count > 0)
            {
                text.AppendLine("The starting values against at-level Fabrication crafting at the same rank (the hours-to-50 model, gathering and the per-craft overhead included). A ratio above 100% breaks the plan's ceiling that research XP per real hour stays below crafting XP per hour.");
                text.AppendLine();
                var starting = new MarkdownTable(
                    "Fabrication rank", "Base XP", "Level-1 job XP (starting values)", "Starting values as a share of base XP", "Research XP per elapsed h", "Crafting XP per h",
                    "Research / crafting, elapsed", "Research / crafting, player's own time");
                foreach (var row in paying)
                {
                    starting.Add(
                        row.FabricationRank.ToString(), row.BaseXp.ToString(), row.StartingLevel1XpPerJob.ToString(), Percent(row.StartingShare.Value), Whole(row.StartingXpPerHour),
                        Whole(row.CraftingXpPerHour), Percent(row.StartingToCraftingRatio.Value), Percent(row.StartingActiveToCraftingRatio.Value));
                }

                text.AppendLine(starting.ToString());
                var brokenElapsed = paying.Where(r => r.StartingToCraftingRatio > 1).Select(r => r.FabricationRank).ToList();
                var brokenActive = paying.Where(r => r.StartingActiveToCraftingRatio > 1).Select(r => r.FabricationRank).ToList();
                text.AppendLine($"Per elapsed hour with every slot busy, the starting values stay under crafting at {paying.Count - brokenElapsed.Count} of the {paying.Count} paying ranks" +
                                (brokenElapsed.Count > 0 ? $" and break it at rank {string.Join(", ", brokenElapsed)}" : "") + ". " +
                                $"Per hour of the player's own time at the terminal ({Number(paying[0].ActiveSecondsPerJob, 0)} s a job) they break it at {brokenActive.Count} of the {paying.Count} ranks (the lowest ratio is {Percent(paying.Min(r => r.StartingActiveToCraftingRatio.Value))}), " +
                                "because a researcher who plays only the experiments and lets the jobs run earns a level-1 job's XP for about a minute and a half of attention.");
                text.AppendLine();
                text.AppendLine("Ceilings in terms of an award, as a share of base XP per job: the largest award that keeps research XP per hour under at-level Fabrication crafting XP per hour at that rank, by two yardsticks.");
                text.AppendLine();
                var ceil = new MarkdownTable("Yardstick", "Lowest ceiling over the paying ranks", "At Fabrication rank");
                var elapsed = paying.OrderBy(r => r.CeilingShare).First();
                var active = paying.OrderBy(r => r.ActiveCeilingShare).First();
                ceil.Add("Per elapsed hour with every job slot busy (research runs while the player does other things)", Percent(elapsed.CeilingShare.Value), elapsed.FabricationRank.ToString());
                ceil.Add("Per hour of the player's own time at the terminal", Percent(active.ActiveCeilingShare.Value), active.FabricationRank.ToString());
                text.AppendLine(ceil.ToString());
                text.AppendLine("The Research workstream's own report compares its best row with crafting at 24,000 XP per hour (15 actions at 6 s, no overhead, no gathering); this report's crafting rate includes the per-craft overhead and the gathering the crafts need, so it is lower and the ceiling tighter.");
                text.AppendLine();
            }
        }

        // ---------------------------------------------------------------------------------------------------------------------------

        private static void AppendProposals(StringBuilder text, ProgressionModel model, ProgressionAssumptions a, List<SkillType> skills, double rpHours)
        {
            var mid = CraftingGearCatalog.MidGearName;
            var solver = new XpTargetSolver(model, RecommendedGuildXpShare);
            var today = skills.ToDictionary(s => s, s => model.Path(s, mid, a).TotalHours);
            var slowest = today.OrderByDescending(t => t.Value).First().Key;
            text.AppendLine("## Proposals for the owner");
            text.AppendLine();
            text.AppendLine("These are options with their measured effect, not code. All of them apply to the four crafting skills and their guilds only: combat and Piloting keep `GetRequiredXP` and `GetDeltaXP` as they are, and Espionage is excluded " +
                            "(`Craft.GetBaseRecipeXP`'s practice XP, `EspionageInfiltration.CalculateXp` and slicing's `Skill.CalculateEspionageXP` are untouched and pinned by tests). " +
                            "None moves the XP chart's cutoff (a craft five or more levels below the rank pays nothing), so quick craft stays at rank >= recipe level + 5 and never costs a player XP they could still earn.");
            text.AppendLine();

            // XP target.
            text.AppendLine("### XP target");
            text.AppendLine();
            var todaySolver = new XpTargetSolver(model, 0);
            var medianToday = todaySolver.MedianHours(a);
            text.AppendLine($"Today the four skills take {string.Join(", ", skills.Select(s => $"{s} {Number(today[s], 0)} h"))} at mid gear with the model's unmeasured timings: a median of {Number(medianToday, 0)} h and a slowest of {Number(today.Values.Max(), 0)} h, against {Number(rpHours, 0)} h of RP XP. " +
                            $"**Proposed target: the median crafting skill reaches rank 50 in {Number(TargetHours, 0)} focused hours or fewer** ({Percent(TargetHours / rpHours)} of the RP XP time), with the {Percent(RecommendedGuildXpShare)} guild award below. " +
                            $"The target is set on the median skill because the slowest ({slowest}) is slow from gathering, not from XP: {Percent(model.Path(slowest, mid, a).GatherHours / today[slowest])} of its hours are spent gathering. " +
                            "Hours scale with 1 / multiplier, because the components and the craft time scale with the crafts, so one crafting-only multiplier moves every skill together and cannot close that gap.");
            text.AppendLine();
            var targets = new MarkdownTable(
                new[] { "Median skill target (h)", "Multiplier needed", "Rank cost cap needed (XP)" }.Concat(skills.Select(s => $"{s} h at that multiplier")).ToArray());
            foreach (var hours in new[] { TargetHours, 35.0, 25.0 })
            {
                var multiplier = solver.SolveMultiplier(a, hours, XpTargetMetric.MedianSkill);
                var cap = solver.SolveCap(a, hours, XpTargetMetric.MedianSkill);
                targets.Add(new[]
                {
                    Number(hours, 0),
                    multiplier.HasValue ? "x" + Number(multiplier.Value, 2) : "none up to x64",
                    cap.HasValue ? (cap.Value >= NoCap ? "none needed" : Whole(cap.Value)) : "none can"
                }.Concat(skills.Select(s => multiplier.HasValue ? Number(solver.Path(s, a with { CraftXpMultiplier = multiplier.Value }).TotalHours, 1) : "-")).ToArray());
            }

            text.AppendLine("What each target needs on top of the guild award. A multiplier of x1.00 means the award alone meets it:");
            text.AppendLine();
            text.AppendLine(targets.ToString());

            var medianA = solver.MedianHours(a);
            var multiplierForTarget = solver.SolveMultiplier(a, TargetHours, XpTargetMetric.MedianSkill);
            var needsMultiplier = multiplierForTarget.HasValue && multiplierForTarget.Value > 1.0;
            var optionB = multiplierForTarget.HasValue ? Math.Ceiling(multiplierForTarget.Value * 20) / 20 : 1.0;
            var medianB = solver.MedianHours(a with { CraftXpMultiplier = optionB });
            var capForB = solver.SolveCap(a, medianB, XpTargetMetric.MedianSkill);
            var optionC = capForB.HasValue ? capForB.Value / 100 * 100 : NoCap;
            var optionCTotal = Enumerable.Range(0, ProgressionAssumptions.MaximumRank).Sum(r => Math.Min(Skill.GetRequiredXP(r), optionC));
            var options = new (string Name, string Change, ProgressionAssumptions Assumptions, double? GuildShare, string Migration)[]
            {
                ("Today", "nothing", a, null, "no"),
                ("0", "hand in the guild's task recipes for credits and points, no XP award (isolates the recipe choice)", a, 0.0, "no"),
                ("A", "guild task XP only", a, RecommendedGuildXpShare, "no"),
                ("B", $"A, and crafting XP x{Number(optionB, 2)} (crafts only)", a with { CraftXpMultiplier = optionB }, RecommendedGuildXpShare, "no"),
                ("C", $"A, and no rank costs more than {Whole(optionC)} XP", a with { RequiredXpCap = optionC }, RecommendedGuildXpShare, "yes")
            };
            var option = new MarkdownTable(new[] { "Option", "Change" }.Concat(skills.Select(s => $"{s} h")).Concat(new[] { "Median skill", "Slowest skill", "Migration of stored XP" }).ToArray());
            foreach (var (name, change, scenario, guildShare, migration) in options)
            {
                var hours = skills.ToDictionary(
                    skill => skill,
                    skill => model.Path(skill, mid, scenario, guildShare.HasValue ? GuildHandInPlan.For(model.Guilds, GuildTaskCatalog.GuildOf(skill), guildShare.Value) : null).TotalHours);
                var ordered = hours.Values.OrderBy(h => h).ToList();
                var median = (ordered[(ordered.Count - 1) / 2] + ordered[ordered.Count / 2]) / 2;
                option.Add(new[] { name, change }.Concat(skills.Select(s => Number(hours[s], 1))).Concat(new[] { Number(median, 1), Number(ordered[^1], 1), migration }).ToArray());
            }

            text.AppendLine(option.ToString());
            text.AppendLine("- **Row 0 is not an option.** It shows that handing in the guild's task recipes, with no award at all, already changes the hours, because the task recipes' components differ from the level's average recipe; the award's own effect is the difference between rows 0 and A.");
            text.AppendLine($"- **A: guild task XP only.** No craft XP change; the smallest change, with no stored-data conversion. It takes the median skill to {Number(medianA, 1)} h, " +
                            (medianA <= TargetHours ? "which meets the target." : $"which is {Number(medianA - TargetHours, 1)} h short of the {Number(TargetHours, 0)} h target."));
            text.AppendLine($"- **B: A plus one crafting-only multiplier, x{Number(optionB, 2)}.** The smallest round multiplier that brings the median skill to the target ({Number(medianB, 1)} h). One parameter on what crafts pay, retuned from the client timings later. It changes gains only, so stored XP and ranks need no conversion.");
            text.AppendLine($"- **C: A plus a cap of {Whole(optionC)} XP on any rank's cost.** The same median as B by a different route: it cuts the climb from {Whole(Enumerable.Range(0, ProgressionAssumptions.MaximumRank).Sum(Skill.GetRequiredXP))} to {Whole(optionCTotal)} XP and spares the late ranks that cost up to 16,000 XP and need 15-25 crafts each, while leaving the early ranks alone. " +
                            "It changes required XP per rank for the crafting skills, so it needs a crafting-specific table and the plan's migration (convert stored XP and ranks so nobody loses a rank).");
            text.AppendLine();
            text.AppendLine((needsMultiplier
                                ? $"**Recommendation: B.** The guild award alone leaves the median skill at {Number(medianA, 1)} h, so the target needs a crafting-only multiplier of about x{Number(optionB, 2)} on top; B reaches it with no migration. "
                                : $"**Recommendation: A.** The guild award alone meets the {Number(TargetHours, 0)} h target for the median skill without touching craft XP. ") +
                            $"Treat the multiplier as a starting point: the model's hours move more with the node travel time ({slowest} runs from {Number(model.Path(slowest, mid, a with { NodeTravelSeconds = 15 }).TotalHours, 0)} h at 15 s to {Number(model.Path(slowest, mid, a with { NodeTravelSeconds = 120 }).TotalHours, 0)} h at 120 s) than any option moves them, " +
                            "so measure it, re-run `progression`, and retune. " +
                            $"{slowest} stays far above the median under every option here because its components are scarce drops (gems and crate goods); that is a supply question for the gathering workstream, not an XP one.");
            text.AppendLine();

            // Guild XP.
            text.AppendLine("### Guild task XP");
            text.AppendLine();
            text.AppendLine("Each crafting-guild hand-in awards XP in the guild's skill. **Formula:** award = share x `Skill.GetDeltaXP(level - rank)` x item quantity, with `level` the recipe level of a crafted task and the fish's `FishType` level of a catch task (such as `agr_tsk_017`), " +
                            "and `rank` the player's rank in the guild's skill. The chart's cutoff makes a hand-in five or more levels below the rank worth nothing, so the quick-crafted items that raise the guild rank pay no XP, and a task never pays more than crafting the same recipe would.");
            text.AppendLine();
            var guildOptions = new MarkdownTable(new[] { "Share of base XP" }.Concat(skills.SelectMany(s => new[] { $"{s} h", $"{s} XP share" })).ToArray());
            guildOptions.Add(new[] { "none" }.Concat(skills.SelectMany(s => new[] { Number(today[s], 1), "-" })).ToArray());
            var shareOfClimb = new Dictionary<double, double>();
            foreach (var share in GuildXpShares)
            {
                var results = skills.Select(skill => model.Path(skill, mid, a, GuildHandInPlan.For(model.Guilds, GuildTaskCatalog.GuildOf(skill), share))).ToList();
                shareOfClimb[share] = results.Average(p => p.GuildXp / p.RequiredXp);
                guildOptions.Add(new[] { Percent(share) }.Concat(results.SelectMany(path => new[] { Number(path.TotalHours, 1), Percent(path.GuildXp / path.RequiredXp) })).ToArray());
            }

            text.AppendLine(guildOptions.ToString());
            var catches = GuildCohorts.CatchTasks(model.Guilds, a);
            var perTask = catches.Count > 0 ? catches[0].HandInXp(RecommendedGuildXpShare, false) / (catches[0].BiteXpPerTask + catches[0].HandInXp(RecommendedGuildXpShare, false)) : 0;
            var perFish = catches.Count > 0 ? catches[0].HandInXp(RecommendedGuildXpShare, true) / (catches[0].BiteXpPerTask + catches[0].HandInXp(RecommendedGuildXpShare, true)) : 0;
            text.AppendLine($"- **Option 1, {Percent(GuildXpShares[0])}.** Hand-ins supply {Percent(shareOfClimb[GuildXpShares[0]])} of the climb's XP: a small bonus that is easy to ignore.");
            text.AppendLine($"- **Option 2, {Percent(GuildXpShares[1])} (recommended).** Hand-ins supply {Percent(shareOfClimb[GuildXpShares[1]])}: a real part of the pace that does not replace crafting. " +
                            $"Quantity matters for catch tasks: at this share a four-fish task's hand-in XP is {Percent(perFish)} of the task's XP when the award is per fish and {Percent(perTask)} when it is per task, so the award is per item.");
            text.AppendLine($"- **Option 3, {Percent(GuildXpShares[2])}.** Hand-ins supply {Percent(shareOfClimb[GuildXpShares[2]])} of the climb, and a crafter who hands in every craft effectively doubles their XP per craft. That is the largest change and should be a deliberate choice.");
            text.AppendLine();
            text.AppendLine("The credit faucet is the risk to weigh with this: every hand-in pays 1,000-7,200 credits and quick craft makes the items in seconds. If the owner wants a limit, the least invasive option is a per-day cap on guild task credits (not points or XP) per character; " +
                            "nothing here adds one.");
            text.AppendLine();

            // Research ceiling.
            text.AppendLine("### Research XP ceiling");
            text.AppendLine();
            var rows = skills.Contains(SkillType.Fabrication)
                ? ResearchCohort.Rows(model, mid, a with { CraftXpMultiplier = CraftMinigameXp.CraftingPaceMultiplier }).Where(r => r.BaseXp > 0).ToList()
                : new List<ResearchRow>();
            if (rows.Count > 0)
            {
                var startingShare = rows.Max(r => r.StartingShare.Value);
                var scale = rows.Min(r => r.CeilingShare.Value / r.StartingShare.Value);
                var binding = rows.OrderBy(r => r.CeilingShare.Value / r.StartingShare.Value).First();
                var experiment = (int)Math.Floor(ResearchXpRules.ExperimentXpPercent * scale);
                var completion = (int)Math.Floor(ResearchXpRules.CompletionXpPercent * scale);
                var ownScale = rows.Min(r => r.ActiveCeilingShare.Value / r.StartingShare.Value);
                var ownBroken = rows.Count(r => r.StartingActiveToCraftingRatio > 1);
                text.AppendLine($"The Research workstream's starting values pay {Percent(startingShare)} of the chart XP for a level-1 job (`ResearchXpRules`: {ResearchXpRules.ExperimentXpPercent}% experiment, {ResearchXpRules.CompletionXpPercent}% completion, +{ResearchXpRules.LevelBonusPercentPerLevel}% per level). " +
                                "**Ceiling, as the plan words it: research XP per real hour, with every job slot busy, stays below at-level Fabrication crafting XP per hour at the same rank** (the whole crafting loop, gathering and overhead included). " +
                                (scale >= 1
                                    ? $"The values in `ResearchXpRules` ({ResearchXpRules.ExperimentXpPercent}% and {ResearchXpRules.CompletionXpPercent}%) already sit under it. "
                                    : $"They break it at Fabrication {binding.FabricationRank}, where a level-1 job on a tier-1 recipe takes minutes and the lone slot restarts it all hour. " +
                                      $"**Proposal: scale the starting values to {Percent(scale)} of themselves, `ExperimentXpPercent` {experiment} and `CompletionXpPercent` {completion}**, which keeps research under crafting at every rank. ") +
                                $"Counted per hour of the player's own time at the terminal ({Number(binding.ActiveSecondsPerJob, 0)} s a job, because the jobs run offline), the starting values exceed crafting at {ownBroken} of the {rows.Count} paying ranks and would need {Percent(ownScale)} of themselves; " +
                                "the plan's wording is the elapsed hour, so that is shown as the owner's call, not proposed. " +
                                "Keep a rule that a research award never exceeds the base XP of a craft of the same recipe level. " +
                                "Credits give a second bound: the first job on a new blueprint costs 200-4,200 credits and the level-10 job of a tier-4 recipe takes about five days. " +
                                "Because the chart pays nothing for recipes five or more levels below the rank, the owner may want a floor for research (like `GetPracticeXP`'s 150) so researchers are not paid only in the first four ranks of each tier; a floor must sit under the same ceiling.");
            }

            text.AppendLine();
        }

        public const double TargetHours = 45.0;

        private const int NoCap = 20_000;

        private static void AppendLimitations(StringBuilder text)
        {
            text.AppendLine("## Limitations");
            text.AppendLine();
            text.AppendLine("- Seconds per action, craft overhead, node travel, turn-in time, research visits and seconds per fishing bite are unmeasured estimates. The client evidence replaces them; the command re-runs with the new values.");
            text.AppendLine("- Respawns, node contention, inventory trips, selling, restocking power cores and every other hour of real play are not modelled. These hours are a floor on a focused crafter's time, not a forecast of calendar days.");
            text.AppendLine("- Components no node supplies, and parts another recipe makes, are not timed; the sensitivity row prices them at a flat 20 s. Gems are priced as if self-gathered, although players trade them.");
            text.AppendLine("- One player policy (the legacy lookahead) at three gear sets. A better player or a different recipe choice changes quality, hence XP per craft. Stretch play (recipes up to three levels above the rank) and blueprints are not played.");
            text.AppendLine("- The crafter's Gathering rank is not a gate, XP bonuses (Social, food, DM) are not applied, and the total skill-rank cap and XP debt are ignored.");
            text.AppendLine("- Guild points follow the server's formula without Guild Relations or Social, and the cohort's catch-up uses one task item per tier. Credits are base rewards without the Guild Relations and Social bonuses.");
            text.AppendLine("- Fishing is shown for catch tasks only; Agriculture's fishing path is not part of the hours above, and fish availability by location is not modelled.");
            text.AppendLine("- The research cohort assumes the award follows `GetDeltaXP`; the Research workstream sets the real amount, and the ceiling is the bound on it.");
        }

        /// <summary>The craft results of a skill the run played, or null for one a <c>--skills</c> subset left out.</summary>
        private static ICraftResults ResultsOrNull(ProgressionModel model, SkillType skill, string gear) =>
            model.Skills.Contains(skill) ? model.Results(skill, gear) : null;

        private static string Whole(double value) => value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);

        private static string Hours(double seconds) =>
            seconds >= 3600 ? Number(seconds / 3600, 1) + " h" : Number(seconds / 60, 1) + " min";
    }
}
