using System.Text;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Legacy;
using SWLOR.CraftSim.Legacy.Crafting;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Report
{
    /// <summary>Writes the old craft minigame's baseline as markdown.</summary>
    public static class CraftingReport
    {
        public static string Build(CraftingBaselineResults results, RecipeCatalog recipes, string command)
        {
            var text = new StringBuilder();
            text.AppendLine("# Crafting revamp: legacy baseline of the old craft minigame");
            text.AppendLine();
            text.AppendLine("The old minigame (`CraftViewModel.cs`) played at every recipe level, gear level and policy, with today's rules. It is the yardstick the");
            text.AppendLine("new minigames' economy-parity gate compares against. No new minigame is measured here.");
            text.AppendLine();
            AppendInputs(text, results, recipes, command);
            AppendGear(text, results);
            AppendSkillTables(text, results);
            AppendSensitivity(text, results);
            AppendLimitations(text);
            return text.ToString();
        }

        private static void AppendInputs(StringBuilder text, CraftingBaselineResults results, RecipeCatalog recipes, string command)
        {
            text.AppendLine("## Inputs");
            text.AppendLine();
            text.AppendLine($"- Command: `{command}`");
            text.AppendLine($"- Seed {results.Seed}; {results.Runs} crafts per recipe level, skill, gear level and policy. Each craft's dice come from its own stream of the seed, so a run repeats exactly.");
            text.AppendLine("- Recipes: every active recipe in the game server's recipe definitions, grouped by level. Level, skill and component lines are read from the definitions; durability, progress and quality come from `RecipeLevelChart` through `Craft.GetRecipeLevelDetail`.");
            text.AppendLine("  " + string.Join("; ", recipes.Recipes.GroupBy(r => r.Skill).OrderBy(g => (int)g.Key)
                .Select(g => $"{g.Key} {g.Count()} (levels {g.Min(r => r.Level)}-{g.Max(r => r.Level)})")) + ".");
            text.AppendLine("- Crafter rank: the recipe's level, at most 50 (the rank cap), so a crafter works at their own level. The sensitivity table also plays " +
                            string.Join(" and ", results.SensitivityOffsets.Select(o => $"{Signed(o)} levels")) +
                            " (at least the recipe's required rank, level - 3).");
            text.AppendLine("- Gear levels: none, the middle tier and the best tier of each crafting skill's gear (next section). The meters use gear only: no food or status bonuses.");
            text.AppendLine("- Policies:");
            text.AppendLine("  - **Dominant rotation**: Waste Not, then Basic Touch repeated, repaired with Master's Mend, then synthesis to finish. It keeps back the durability the finish needs, counting the repairs its remaining CP can buy, and finishes with the affordable synthesis of highest expected progress while keeping one repair affordable. It never uses Steady Hand, Muscle Memory, Veneration, Standard Touch or Precise Touch.");
            text.AppendLine("  - **Lookahead**: at every step it tries each usable action against 8 sampled futures (the same dice for every candidate), finishes each future with the dominant rotation, and takes the action with the best mean score (0 for a failed craft, 0.5 plus half the quality fraction for a success). It is one step of policy improvement over the rotation, not an optimal player.");
            text.AppendLine("- Material use: a success consumes every committed component stack once, and each failure consumes each stack at 65% (`CraftViewModel.cs:1601-1641`), so stacks per success = stacks x (1 + 0.65 x (1 - p) / p) for a success chance p. A stack is one distinct component line of the recipe; enhancements are not committed.");
            text.AppendLine();
            text.AppendLine("Rules ported, each checked against the source by `LegacySourceTests`:");
            text.AppendLine();
            var rules = new MarkdownTable("Rule", "Source");
            foreach (var source in LegacySource.All.Where(s => s.File.EndsWith("CraftViewModel.cs") || s.File.EndsWith("RecipeLevelChart.cs") || s.File.EndsWith("Craft.cs") || s.File.EndsWith("EquipmentStats.cs")))
                rules.Add(source.Name, $"`{source.Citation}`");
            text.AppendLine(rules.ToString());
        }

        private static void AppendGear(StringBuilder text, CraftingBaselineResults results)
        {
            text.AppendLine("## Gear levels");
            text.AppendLine();
            text.AppendLine("A skill's gear is every item blueprint with that skill's Control (item property 88), Craftsmanship (89) or CPBonus (115) that a Smithery recipe makes. Within a body slot the recipes are ordered by level, so the Nth is tier N, and a gear level is one item of the same tier in each of armor, helmet, necklace, belt, boots, bracer and cloak plus two rings. \"Mid\" is tier 3 and \"best\" is tier 5, of five. Max CP is gear CP + 0.75 x rank (+31 from rank 25) at the shown rank.");
            text.AppendLine();
            var table = new MarkdownTable("Skill", "Gear level", "Tier", "Control", "Craftsmanship", "Gear CP", "Max CP at rank 25", "Max CP at rank 50");
            foreach (var gear in results.GearLevels)
            {
                table.Add(
                    gear.Skill.ToString(),
                    gear.Name,
                    gear.Tier == 0 ? "-" : gear.Tier.ToString(),
                    gear.Control.ToString(),
                    gear.Craftsmanship.ToString(),
                    gear.CpBonus.ToString(),
                    LegacyCraftRules.MaxCp(gear.CpBonus, 25).ToString(),
                    LegacyCraftRules.MaxCp(gear.CpBonus, 50).ToString());
            }

            text.AppendLine(table.ToString());
        }

        private static void AppendSkillTables(StringBuilder text, CraftingBaselineResults results)
        {
            text.AppendLine("## Results by skill");
            text.AppendLine();
            text.AppendLine("Crafter at the recipe's level. Success is the share of crafts that finish the item. Quality is the mean final quality of successful crafts (maximum quality is 100%); Full is the share of those that reached it. Actions counts every craft, failed or not. Stacks is component stacks consumed per successful item when the band's recipes are attempted equally often; recipes that never succeeded in these runs are left out of it, and the percentage after it is their share of the band.");
            text.AppendLine();

            var policies = results.Policies;
            foreach (var skill in results.Bands.Select(b => b.Skill).Distinct().OrderBy(s => (int)s))
            {
                text.AppendLine($"### {skill}");
                text.AppendLine();
                var header = new List<string> { "Levels", "Recipes", "Gear" };
                foreach (var policy in policies)
                    header.AddRange(new[] { $"{policy}: success", "quality", "full", "actions", "stacks" });

                var table = new MarkdownTable(header.ToArray());
                var rows = results.Bands.Where(b => b.Skill == skill && b.RankOffset == 0).ToList();
                foreach (var band in rows.Select(r => r.Band).Distinct())
                {
                    foreach (var gear in rows.Where(r => r.Band == band).Select(r => r.Gear).Distinct())
                    {
                        var cells = new List<string>();
                        var first = rows.First(r => r.Band == band && r.Gear == gear);
                        cells.Add(band);
                        cells.Add(first.Recipes.ToString());
                        cells.Add(gear);
                        foreach (var policy in policies)
                        {
                            var row = rows.FirstOrDefault(r => r.Band == band && r.Gear == gear && r.Policy == policy);
                            if (row == null)
                            {
                                cells.AddRange(new[] { "-", "-", "-", "-", "-" });
                                continue;
                            }

                            cells.Add(Percent(row.SuccessRate));
                            cells.Add(Percent(row.QualityFraction));
                            cells.Add(Percent(row.FullQualityShare));
                            cells.Add(Number(row.ActionsPerCraft));
                            cells.Add(Stacks(row));
                        }

                        table.Add(cells.ToArray());
                    }
                }

                text.AppendLine(table.ToString());
            }
        }

        private static void AppendSensitivity(StringBuilder text, CraftingBaselineResults results)
        {
            if (results.SensitivityOffsets.Count == 0)
                return;

            var policy = results.Policies.First();
            text.AppendLine("## Rank sensitivity");
            text.AppendLine();
            text.AppendLine($"The {policy.ToLowerInvariant()} with the crafter's rank at the recipe's level plus the offset shown (kept between the recipe's required rank and 50). Each level below the recipe raises the progress needed by 25% and cuts progress and quality per action by 5%. Each level above raises progress per action by 5% and, in the old code, also raises the progress needed by 5%, up to 25%.");
            text.AppendLine();
            var offsets = new[] { 0 }.Concat(results.SensitivityOffsets).OrderBy(o => o).ToList();
            var header = new List<string> { "Skill", "Levels", "Gear" };
            foreach (var offset in offsets)
                header.AddRange(new[] { $"{Signed(offset)}: success", "quality", "actions" });

            var table = new MarkdownTable(header.ToArray());
            foreach (var skill in results.Bands.Select(b => b.Skill).Distinct().OrderBy(s => (int)s))
            {
                var rows = results.Bands.Where(b => b.Skill == skill && b.Policy == policy).ToList();
                foreach (var band in rows.Select(r => r.Band).Distinct())
                {
                    foreach (var gear in rows.Where(r => r.Band == band).Select(r => r.Gear).Distinct())
                    {
                        var cells = new List<string> { skill.ToString(), band, gear };
                        foreach (var offset in offsets)
                        {
                            var row = rows.FirstOrDefault(r => r.Band == band && r.Gear == gear && r.RankOffset == offset);
                            if (row == null)
                                cells.AddRange(new[] { "-", "-", "-" });
                            else
                                cells.AddRange(new[] { Percent(row.SuccessRate), Percent(row.QualityFraction), Number(row.ActionsPerCraft) });
                        }

                        table.Add(cells.ToArray());
                    }
                }
            }

            text.AppendLine(table.ToString());
        }

        private static void AppendLimitations(StringBuilder text)
        {
            text.AppendLine("## Limitations");
            text.AppendLine();
            text.AppendLine("- Only the two policies above are played. Neither is optimal, so a result is evidence of what these plays reach, not a ceiling. The lookahead uses 8 futures per action, so its choices carry sampling noise.");
            text.AppendLine("- A craft is played at one rank. Real crafters pass through ranks, and rank thresholds (5, 8, 10, 15, 20, 25, 30, 35, 40) change the actions available.");
            text.AppendLine("- Gear is the sets derived above. Mixed or partial sets, food and other status bonuses to Control, Craftsmanship and CP are not played.");
            text.AppendLine("- Components are counted as one stack per component line. The real count depends on how the player's inventory splits each line (`AggregateComponents`), and a stack can be partly committed. Enhancements, blueprints and licensed runs are not played, and the 65% loss on failure also applies to selected enhancements.");
            text.AppendLine("- Quality is the final quality fraction. The old minigame uses it for enhancement transfer chance, food duration, XP and vendor value, none of which are valued here.");
            text.AppendLine("- Recipes at one level share a result, because the old minigame reads only the recipe's level and skill. Components only weight the material columns.");
            text.AppendLine("- Max progress is computed as the old code does, including its sign errors (the 5% bonus above the recipe raises the progress needed), because the baseline must match today's play.");
        }

        private static string Stacks(CraftBandRow row)
        {
            if (row.StacksPerSuccess == null)
                return "n/a (never)";

            var stacks = Number(row.StacksPerSuccess.Value);
            return row.NeverCraftedShare > 0 ? $"{stacks} ({Percent(row.NeverCraftedShare)} never)" : stacks;
        }
    }
}
