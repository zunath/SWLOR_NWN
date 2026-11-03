using System.Text;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Legacy;
using SWLOR.CraftSim.Legacy.Crafting;
using SWLOR.CraftSim.Scenario;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Report
{
    /// <summary>Writes today's expected transferred enhancements, the yardstick of balance gate 7, as markdown.</summary>
    public static class LegacyEnhancementReport
    {
        /// <summary>The averages of a set of samples: each sample (a recipe's cell) counts equally.</summary>
        private readonly record struct Average(int Samples, double MadeShare, double MeanQuality, double MeanChancePercent, double Transfers)
        {
            public static Average Of(IReadOnlyCollection<LegacyEnhancementSample> samples)
            {
                if (samples.Count == 0)
                    return default;

                var made = samples.Sum(s => s.Cell.Successes);
                return new Average(
                    samples.Count,
                    samples.Average(s => s.Cell.SuccessRate),
                    made == 0 ? 0 : samples.Sum(s => s.Cell.QualitySum) / made,
                    made == 0 ? 0 : samples.Sum(s => (double)s.Cell.TransferChanceSum) / made,
                    samples.Average(s => s.Cell.ExpectedTransfersPerAttempt));
            }
        }

        public static string Build(
            LegacyEnhancementResults results, RecipeCatalog recipes, EnhancementCatalog enhancements, string command)
        {
            var text = new StringBuilder();
            text.AppendLine("# Crafting revamp: legacy baseline of enhancement transfer");
            text.AppendLine();
            text.AppendLine("The old minigame (`CraftViewModel.cs`) played with one, two and three enhancements slotted, with today's rules: each enhancement raises the progress the");
            text.AppendLine("piece needs by its progress penalty, and on a success each enhancement property transfers at a chance equal to the quality percentage. It is the yardstick of");
            text.AppendLine("balance gate 7 (enhancement parity). No new minigame is measured here.");
            text.AppendLine();
            AppendInputs(text, results, recipes, enhancements, command);
            AppendRepresentatives(text, results, enhancements);
            AppendGateCohort(text, results);
            AppendSweep(text, results);
            AppendLimitations(text);
            return text.ToString();
        }

        private static void AppendInputs(
            StringBuilder text, LegacyEnhancementResults results, RecipeCatalog recipes, EnhancementCatalog enhancements, string command)
        {
            var options = results.Options;
            text.AppendLine("## Inputs");
            text.AppendLine();
            text.AppendLine($"- Command: `{command}`");
            text.AppendLine($"- Seed {options.Seed}; {options.Runs} crafts per recipe level, skill, gear level, policy, enhancement penalty and slot count. Each craft's dice come from its own stream of the seed, so a run repeats exactly.");
            text.AppendLine($"- Enhancement items: the {enhancements.Items.Count} items in `Module/uti` that carry a progress penalty (item property 95) and one enhancement property for a recipe kind, with an enhancement level (104): " +
                            string.Join(", ", enhancements.Kinds.Select(kind => $"{enhancements.Items.Count(i => i.Type == kind)} {kind.ToString().ToLowerInvariant()}")) +
                            $". {enhancements.Skipped.Count} item(s) with a penalty are left out: {string.Join("; ", enhancements.Skipped.Take(6))}" +
                            (enhancements.Skipped.Count > 6 ? "; ..." : string.Empty) + ".");
            text.AppendLine("- Representative enhancement: a recipe is played with the real item of its enhancement kind at the best level it accepts (the old window refuses an enhancement more than 5 levels above the recipe), taking the item whose progress penalty is the lower median of that level, ties by resref. The sweep plays every level the recipe accepts.");
            text.AppendLine("- Slots: an item with 1, 2 or 3 enhancements, every slot holding the same representative item. Recipes have 1 or 2 slots; research and blueprints add more, so every enhanceable recipe is played at each count.");
            text.AppendLine("- Recipes: an even sample of " + options.RecipesPerBand + " active recipes per skill and band that have an enhancement slot, each counting equally. " +
                            string.Join("; ", results.Samples.GroupBy(s => s.Skill).OrderBy(g => (int)g.Key).Select(g =>
                                $"{g.Key}: {recipes.Recipes.Count(r => r.Skill == g.Key && r.EnhancementSlots > 0 && r.EnhancementType != RecipeEnhancementType.None)} enhanceable of {recipes.Recipes.Count(r => r.Skill == g.Key)}")) + ".");
            text.AppendLine($"- Gate cohort: rank {options.GateRank}, the best gear, the at-level bands {string.Join(", ", options.GateBands.Select(b => b.Name))}, both policies. Sweep: rank equal to the recipe's level (at most 50) in the bands {string.Join(", ", options.SweepBands.Select(b => b.Name))}, " +
                            (options.SweepEveryGearLevel ? "no gear, mid gear and best gear" : "best gear") + ", the lookahead policy.");
            text.AppendLine("- Gear levels: none, the middle tier and the best tier of each crafting skill's gear, as in `legacy-crafting-baseline.md`. The meters use gear only: no food or status bonuses.");
            text.AppendLine("- Policies: **Dominant rotation** is the rotation players report (Waste Not, Basic Touch repeated, Master's Mend, then synthesis to finish); **Lookahead** tries each action against 8 sampled futures finished by the rotation (see `legacy-crafting-baseline.md`). The gate compares against the lookahead player, which is the stricter yardstick.");
            text.AppendLine("- Expected transfers: a craft that is made transfers each enhancement with probability `(int)(quality / max quality * 100) / 100` (truncated as the old window does), so it adds slots x chance; a craft that breaks the piece adds none. No transfer dice are drawn, which carries the same mean with less noise. Figures are per enhanced craft attempt.");
            text.AppendLine("- Extra progress: the enhancements' total penalty over the progress the recipe needs without them in the old window (rank-adjusted), averaged over the recipes.");
            text.AppendLine();
            text.AppendLine(LegacySource.RulesPortedNote);
            text.AppendLine();
            var rules = new MarkdownTable("Rule", "Source");
            foreach (var source in new[]
                     {
                         LegacySource.EnhancementSlots, LegacySource.EnhancementKinds, LegacySource.EnhancementProgressPenalty,
                         LegacySource.EnhancementTransfer, LegacySource.MaxProgress, LegacySource.LevelChart
                     })
            {
                rules.Add(source.Name, $"`{source.Citation}`");
            }

            text.AppendLine(rules.ToString());
        }

        private static void AppendRepresentatives(StringBuilder text, LegacyEnhancementResults results, EnhancementCatalog enhancements)
        {
            text.AppendLine("## Representative enhancements");
            text.AppendLine();
            text.AppendLine("Every enhancement kind and level the cohorts below use, with the spread of the real items and the one chosen. Penalties are in the old window's progress units.");
            text.AppendLine();
            var used = results.Samples.Select(s => (s.Item.Type, s.Item.Level)).Distinct().OrderBy(k => k.Type).ThenBy(k => k.Level).ToList();
            var table = new MarkdownTable("Kind", "Level", "Tier", "Items", "Penalty min / median / max", "Representative item", "Its penalty");
            foreach (var (type, level) in used)
            {
                var summary = enhancements.Summarize(type, level);
                var item = enhancements.Representative(type, level);
                table.Add(
                    type.ToString(),
                    level.ToString(),
                    EnhancementItem.TierName(EnhancementItem.TierOf(level)),
                    summary.Count.ToString(),
                    $"{summary.Minimum} / {Number(summary.Median, 1)} / {summary.Maximum}",
                    $"{item.Name} (`{item.Resref}`)",
                    item.ProgressPenalty.ToString());
            }

            text.AppendLine(table.ToString());
        }

        private static void AppendGateCohort(StringBuilder text, LegacyEnhancementResults results)
        {
            var options = results.Options;
            var cohort = results.Samples.Where(s => s.IsGateCohort).ToList();
            text.AppendLine($"## Gate 7 cohort: rank {options.GateRank}, best gear, at level");
            text.AppendLine();
            text.AppendLine("Expected enhancements transferred per enhanced craft. This is the figure the new minigames' thresholds must reach or beat. \"Made\" is the share of crafts that finish the item; \"avg quality\" and \"chance\" are over the crafts that were made.");
            text.AppendLine();

            foreach (var skill in cohort.Select(s => s.Skill).Distinct().OrderBy(s => (int)s))
            {
                var gear = results.GearLevels.FirstOrDefault(g => g.Skill == skill && cohort.Any(s => s.Skill == skill && s.Gear == g.Name));
                text.AppendLine($"### {skill}" + (gear == null ? string.Empty : $" ({gear})"));
                text.AppendLine();
                var table = new MarkdownTable(
                    "Band", "Recipes", "Penalty each", "Slots", "Extra progress",
                    "Lookahead: made", "avg quality", "chance", "enhancements transferred",
                    "Dominant: made", "avg quality", "chance", "enhancements transferred");
                foreach (var band in cohort.Where(s => s.Skill == skill).Select(s => s.Band).Distinct())
                {
                    foreach (var slots in options.SlotCounts)
                    {
                        var look = cohort.Where(s => s.Skill == skill && s.Band == band && s.Slots == slots && s.Policy == "Lookahead").ToList();
                        var dominant = cohort.Where(s => s.Skill == skill && s.Band == band && s.Slots == slots && s.Policy == "Dominant rotation").ToList();
                        if (look.Count == 0)
                            continue;

                        var a = Average.Of(look);
                        var b = Average.Of(dominant);
                        table.Add(
                            band,
                            look.Count.ToString(),
                            PenaltyRange(look),
                            slots.ToString(),
                            Percent(ExtraProgress(look)),
                            Percent(a.MadeShare), Percent(a.MeanQuality), Number(a.MeanChancePercent, 0) + "%", Number(a.Transfers, 2),
                            Percent(b.MadeShare), Percent(b.MeanQuality), Number(b.MeanChancePercent, 0) + "%", Number(b.Transfers, 2));
                    }
                }

                text.AppendLine(table.ToString());
            }

            var most = options.SlotCounts.Max();
            var triple = cohort.Where(s => s.Slots == most).ToList();
            if (triple.Count > 0)
            {
                var perBand = cohort.Select(s => s.Band).Distinct().Select(band =>
                {
                    var look = Average.Of(triple.Where(s => s.Band == band && s.Policy == "Lookahead").ToList());
                    var dominant = Average.Of(triple.Where(s => s.Band == band && s.Policy == "Dominant rotation").ToList());
                    return $"{band}: {Percent(look.MeanQuality)} lookahead, {Percent(dominant.MeanQuality)} rotation";
                });
                text.AppendLine($"Check against player reports: crafters report about 80% quality on triple-enhanced endgame crafts with the best gear. The model's average quality of the items made with {most} enhancements, over every skill: {string.Join("; ", perBand)}.");
                text.AppendLine();
            }
        }

        private static void AppendSweep(StringBuilder text, LegacyEnhancementResults results)
        {
            var options = results.Options;
            var sweep = results.Samples.Where(s => !s.IsGateCohort).ToList();
            text.AppendLine("## By band, enhancement tier and slot count");
            text.AppendLine();
            text.AppendLine($"The lookahead player at rank equal to the recipe's level (at most {ScenarioCohort.MaximumRank}). A tier is the enhancement level's group (5 is I, 15 is II, 25 is III, 35 is IV, 45 is V); a recipe only accepts a tier whose level is at most 5 above its own. Each cell is the expected enhancements transferred per craft, with the share of crafts made in brackets.");
            text.AppendLine();

            foreach (var skill in sweep.Select(s => s.Skill).Distinct().OrderBy(s => (int)s))
            {
                foreach (var gearName in sweep.Where(s => s.Skill == skill).Select(s => s.Gear).Distinct().OrderBy(name => GearOrder(name)))
                {
                    var rows = sweep.Where(s => s.Skill == skill && s.Gear == gearName).ToList();
                    var gear = results.GearLevels.FirstOrDefault(g => g.Skill == skill && g.Name == gearName);
                    text.AppendLine($"### {skill}, {gearName}" + (gear == null || gear.Tier == 0 ? string.Empty : $" (Control {gear.Control}, Craftsmanship {gear.Craftsmanship}, CP {gear.CpBonus})"));
                    text.AppendLine();
                    var header = new List<string> { "Band", "Tier (level)", "Recipes", "Penalty each", "Extra progress at most slots" };
                    header.AddRange(options.SlotCounts.Select(c => $"{c} slot" + (c == 1 ? string.Empty : "s")));
                    header.Add("Avg quality at fewest / most slots");
                    var table = new MarkdownTable(header.ToArray());

                    foreach (var band in rows.Select(s => s.Band).Distinct())
                    {
                        foreach (var level in rows.Where(s => s.Band == band).Select(s => s.Item.Level).Distinct().OrderBy(l => l))
                        {
                            var cell = rows.Where(s => s.Band == band && s.Item.Level == level).ToList();
                            var cells = new List<string>
                            {
                                band,
                                $"{EnhancementItem.TierName(EnhancementItem.TierOf(level))} ({level})",
                                cell.Select(s => s.Recipe.Type).Distinct().Count().ToString(),
                                PenaltyRange(cell.Where(s => s.Slots == options.SlotCounts[0]).ToList()),
                                Percent(ExtraProgress(cell.Where(s => s.Slots == options.SlotCounts.Max()).ToList()))
                            };
                            foreach (var slots in options.SlotCounts)
                            {
                                var average = Average.Of(cell.Where(s => s.Slots == slots).ToList());
                                cells.Add($"{Number(average.Transfers, 2)} ({Percent(average.MadeShare)})");
                            }

                            var fewest = Average.Of(cell.Where(s => s.Slots == options.SlotCounts.Min()).ToList());
                            var most = Average.Of(cell.Where(s => s.Slots == options.SlotCounts.Max()).ToList());
                            cells.Add($"{Percent(fewest.MeanQuality)} / {Percent(most.MeanQuality)}");
                            table.Add(cells.ToArray());
                        }
                    }

                    text.AppendLine(table.ToString());
                }
            }
        }

        private static int GearOrder(string name) => name switch
        {
            CraftingGearCatalog.NoGearName => 0,
            CraftingGearCatalog.MidGearName => 1,
            _ => 2
        };

        private static string PenaltyRange(IReadOnlyCollection<LegacyEnhancementSample> samples)
        {
            var penalties = samples.Select(s => s.Item.ProgressPenalty).ToList();
            return penalties.Count == 0 ? "-" : penalties.Min() == penalties.Max() ? penalties[0].ToString() : $"{penalties.Min()}-{penalties.Max()}";
        }

        /// <summary>The share of extra progress the enhancements ask for in the old window, averaged over the samples' recipes.</summary>
        private static double ExtraProgress(IReadOnlyCollection<LegacyEnhancementSample> samples)
        {
            if (samples.Count == 0)
                return 0;

            return samples.Average(s =>
            {
                var plain = new LegacyCraftContext(s.Recipe.Level, s.Rank, 0, 0, 0);
                return plain.MaxProgress <= 0 ? 0 : s.Cell.Enhancements.TotalProgressPenalty / (double)plain.MaxProgress;
            });
        }

        private static void AppendLimitations(StringBuilder text)
        {
            text.AppendLine("## Limitations");
            text.AppendLine();
            text.AppendLine("- Enhancement items differ in progress penalty within a level; the figures are for the lower-median item. A harder item transfers less, and the sweep shows how much the level matters.");
            text.AppendLine("- Every slot holds the same item, and every enhancement is assumed to be one the recipe accepts. Players mix enhancements, and an enhancement of the wrong kind is refused by the old window.");
            text.AppendLine("- The crafting policies are not real players. The lookahead is one step of policy improvement over the rotation players report, so it is the stricter yardstick; neither plays Steady Hand, Muscle Memory or Veneration the way a skilled player might in a long craft.");
            text.AppendLine("- Recipes are an even sample of each band, weighted equally; a recipe the sample skipped can differ. Recipe levels fix the progress, quality and durability the old window asks for.");
            text.AppendLine("- No attributes, food, status effects, blueprints (their bonus slots and fees), first-craft bonus or quick craft are modeled, and the client was not run.");
            text.AppendLine("- Failed crafts transfer nothing and also lose each enhancement at 65%; only the transfer is counted here, the loss belongs to the economy-parity gate.");
        }
    }
}
