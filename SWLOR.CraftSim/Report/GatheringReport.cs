using System.Text;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Legacy;
using SWLOR.CraftSim.Legacy.Gathering;
using SWLOR.CraftSim.Scenario;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Report
{
    /// <summary>Writes the harvesting, scavenging, refining and fishing baselines as markdown.</summary>
    public static class GatheringReport
    {
        private static readonly int[] Ranks = Enumerable.Range(0, 11).Select(i => i * 5).ToArray();

        public static string Build(GatheringBaselineResults results, AttributeSweep attributes, string command)
        {
            var text = new StringBuilder();
            text.AppendLine("# Crafting revamp: legacy baseline of gathering, refining and fishing");
            text.AppendLine();
            text.AppendLine("Today's harvesting, scavenging, refining and fishing-catch rules, the yardstick for the new gathering and fishing minigames' economy-parity gate.");
            text.AppendLine("The rules depend on Might (harvest bonus item), Perception (scavenge attempts and success) and Willpower (catch chance). Until the median attributes of");
            text.AppendLine("active characters are measured, the tables sweep attribute modifiers " +
                            $"{Signed(attributes.Minimum)} to {Signed(attributes.Maximum)} (a Willpower score of 10 + 2 x modifier).");
            text.AppendLine();
            AppendInputs(text, results, command);
            AppendHarvest(text, results);
            AppendScavenge(text, results);
            AppendRefining(text);
            AppendFishing(text, attributes);
            AppendLimitations(text);
            return text.ToString();
        }

        private static void AppendInputs(StringBuilder text, GatheringBaselineResults results, string command)
        {
            text.AppendLine("## Inputs");
            text.AppendLine();
            text.AppendLine($"- Command: `{command}`");
            text.AppendLine($"- Seed {results.Seed}. Every table gives the exact expected value from the ported rule; harvesting and scavenging also play {results.Trials} nodes or sites per row from the seed as a cross-check.");
            text.AppendLine("- XP uses the server's own `Skill.GetDeltaXP`. Item counts are loot-table rolls, not priced.");
            text.AppendLine();
            text.AppendLine("Rules ported, each checked against the source by `LegacySourceTests`:");
            text.AppendLine();
            var rules = new MarkdownTable("Rule", "Source");
            foreach (var source in LegacySource.All.Where(s =>
                         s.File.EndsWith("HarvesterItemDefinition.cs") || s.File.EndsWith("ScavengePoint.cs") ||
                         s.File.EndsWith("RefineryViewModel.cs") || s.File.Contains("Fishing")))
            {
                rules.Add(source.Name, $"`{source.Citation}`");
            }

            text.AppendLine(rules.ToString());
            text.AppendLine("The fishing catch roll (`40 + 10 x (rank - fish level) + 2 x Willpower`, kept from 0 to 95) was in `FinishFishing` until the Line fight replaced it. It is ported here from `Service/Fishing.cs` at commit d5f1c0466, where it was checked against the source, and is no longer checked.");
            text.AppendLine();
            text.AppendLine("The server's XP chart (`GetDeltaXP`), by the activity's level minus the rank:");
            text.AppendLine();
            var chart = new MarkdownTable(new[] { "Level - rank" }.Concat(Enumerable.Range(-5, 13).Reverse().Select(Signed)).ToArray());
            chart.Add(new[] { "XP" }.Concat(Enumerable.Range(-5, 13).Reverse().Select(d => ServerXp.ForDelta(d).ToString())).ToArray());
            text.AppendLine(chart.ToString());
        }

        private static void AppendHarvest(StringBuilder text, GatheringBaselineResults results)
        {
            text.AppendLine("## Harvesting");
            text.AppendLine();
            text.AppendLine("A node holds d4 harvests, set by its first harvest. Each harvest spends one harvester charge for one loot-table item, plus a free second item at d100 <= 25 x Might modifier that does not reduce the count.");
            text.AppendLine();
            var table = new MarkdownTable("Might modifier", "Bonus item chance", "Harvests (charges) per node", "Items per node", "Items per node, played", "Items per charge");
            foreach (var row in results.Harvest)
            {
                table.Add(
                    Signed(row.MightModifier),
                    Percent(row.BonusItemChance),
                    Number(row.ExpectedHarvests, 2),
                    Number(row.ExpectedItems, 2),
                    Number(row.SimulatedItems, 2),
                    Number(row.ExpectedItems / row.ExpectedHarvests, 2));
            }

            text.AppendLine(table.ToString());
            text.AppendLine("XP per item gathered, by node level and Gathering rank (`GetDeltaXP(10 x (node level - 1) + 5 - rank)`). A harvest pays this once per item gathered:");
            text.AppendLine();
            var xp = new MarkdownTable(new[] { "Node level (vein level)" }.Concat(Ranks.Select(r => $"Rank {r}")).ToArray());
            for (var level = HarvestModel.FirstNodeLevel; level <= HarvestModel.LastNodeLevel; level++)
                xp.Add(new[] { $"{level} ({HarvestModel.VeinLevel(level)})" }.Concat(Ranks.Select(r => HarvestModel.XpPerItem(level, r).ToString())).ToArray());

            text.AppendLine(xp.ToString());
        }

        private static void AppendScavenge(StringBuilder text, GatheringBaselineResults results)
        {
            text.AppendLine("## Scavenging");
            text.AppendLine();
            text.AppendLine("One attempt, plus a second at Hard Look x 10 + Perception modifier x 5 percent. An attempt succeeds on d20 + Perception modifier against a DC of 6, then 6 + d3 for the second attempt. A site can be searched once.");
            text.AppendLine();
            var perceptions = results.Scavenge.Select(r => r.PerceptionModifier).Distinct().OrderBy(p => p).ToList();
            var hardLooks = results.Scavenge.Select(r => r.HardLook).Distinct().OrderBy(h => h).ToList();

            text.AppendLine("Successful rolls per site (exact), by Perception modifier and Hard Look level:");
            text.AppendLine();
            text.AppendLine(ScavengeMatrix(results, perceptions, hardLooks, r => r.ExpectedSuccesses).ToString());
            text.AppendLine("Attempts per site (exact):");
            text.AppendLine();
            text.AppendLine(ScavengeMatrix(results, perceptions, hardLooks, r => r.ExpectedAttempts).ToString());
            var worst = results.Scavenge.Max(r => Math.Abs(r.SimulatedSuccesses - r.ExpectedSuccesses));
            text.AppendLine($"Cross-check: {results.Trials} played sites per cell differ from the exact successful rolls by at most {Number(worst, 3)}.");
            text.AppendLine();
            text.AppendLine("XP per site at Hard Look 0 and Perception modifier 0 (`GetDeltaXP(10 x site level - rank)` per success, a quarter per failure), by site level and Gathering rank:");
            text.AppendLine();
            var xp = new MarkdownTable(new[] { "Site level" }.Concat(Ranks.Select(r => $"Rank {r}")).ToArray());
            for (var level = ScavengeModel.FirstSiteLevel; level <= ScavengeModel.LastSiteLevel; level++)
            {
                xp.Add(new[] { level.ToString() }.Concat(Ranks.Select(r => Number(ScavengeModel.ExpectedXpPerSite(level, r, 0, 0)))).ToArray());
            }

            text.AppendLine(xp.ToString());
        }

        private static MarkdownTable ScavengeMatrix(
            GatheringBaselineResults results, IReadOnlyList<int> perceptions, IReadOnlyList<int> hardLooks, Func<ScavengeRow, double> value)
        {
            var table = new MarkdownTable(new[] { "Perception modifier" }.Concat(hardLooks.Select(h => $"Hard Look {h}")).ToArray());
            foreach (var perception in perceptions)
            {
                table.Add(new[] { Signed(perception) }
                    .Concat(hardLooks.Select(h => Number(value(results.Scavenge.First(r => r.HardLook == h && r.PerceptionModifier == perception)), 2)))
                    .ToArray());
            }

            return table;
        }

        private static void AppendRefining(StringBuilder text)
        {
            var ores = RefiningModel.Ores();
            text.AppendLine("## Refining");
            text.AppendLine();
            text.AppendLine("Each power core refines 3 + Refinery Management units. Every unit grants a flat XP set by its ore, whatever the refiner's rank. The ores the game does not have (Ochne, Croknor, Hemorgite, Bisteiss) are left out.");
            text.AppendLine();
            var cores = new MarkdownTable("Refinery Management", "Units per core", "Cores for 100 units", "Cores for 9 units");
            for (var level = 0; level <= RefiningModel.MaximumRefineryManagement; level++)
            {
                cores.Add(
                    level.ToString(),
                    RefiningModel.ItemsPerCore(level).ToString(),
                    RefiningModel.CoresRequired(100, level).ToString(),
                    RefiningModel.CoresRequired(9, level).ToString());
            }

            text.AppendLine(cores.ToString());
            text.AppendLine("XP per refined unit (the same at every rank), against what harvesting a node of the same tier pays per item at the tier's unlock rank (10 x (tier - 1)), five ranks later and ten ranks later:");
            text.AppendLine();
            var xp = new MarkdownTable("Ore", "Ore tier", "Refining XP per unit", "XP per core at 3 units", "XP per core at 9 units", "Harvest XP per item at unlock rank", "+5 ranks", "+10 ranks");
            foreach (var ore in ores)
            {
                var unlock = 10 * (ore.RequiredLevel - 1);
                xp.Add(
                    ore.Ore,
                    ore.RequiredLevel.ToString(),
                    ore.XpPerUnit.ToString(),
                    RefiningModel.Xp(ore, 3).ToString(),
                    RefiningModel.Xp(ore, 9).ToString(),
                    HarvestModel.XpPerItem(ore.RequiredLevel, unlock).ToString(),
                    HarvestModel.XpPerItem(ore.RequiredLevel, unlock + 5).ToString(),
                    HarvestModel.XpPerItem(ore.RequiredLevel, unlock + 10).ToString());
            }

            text.AppendLine(xp.ToString());
        }

        private static void AppendFishing(StringBuilder text, AttributeSweep attributes)
        {
            var modifiers = attributes.Modifiers.ToList();
            text.AppendLine("## Fishing");
            text.AppendLine();
            text.AppendLine("A bite spends one bait, then lands the fish at `40 + 10 x (Agriculture rank - fish level) + 2 x Willpower score` percent, kept between 0 and 95. XP is paid on every bite, caught or not.");
            text.AppendLine();
            text.AppendLine("Catch chance by rank above the fish's level and Willpower modifier (score in brackets):");
            text.AppendLine();
            var header = new[] { "Rank - fish level" }.Concat(modifiers.Select(m => $"WIL {Signed(m)} ({AttributeSweep.ScoreFor(m)})")).ToArray();
            var chance = new MarkdownTable(header);
            for (var delta = -5; delta <= 6; delta++)
                chance.Add(new[] { Signed(delta) }.Concat(modifiers.Select(m => FishingCatchModel.CatchChancePercent(delta, 0, AttributeSweep.ScoreFor(m)) + "%")).ToArray());

            text.AppendLine(chance.ToString());

            var pools = FishingCatchModel.Pools();
            text.AppendLine($"Catch chance per bite by Agriculture rank, averaged over every rod and bait pool at every fishing location ({pools.Count} pools: each location, rod and bait, daytime and nighttime counted separately; pools weighted equally, fish within a pool by frequency). The game-wide average mixes pools no single angler fishes, so use it to compare rules, not as a rank's experience:");
            text.AppendLine();
            var pooled = new MarkdownTable(new[] { "Agriculture rank" }.Concat(modifiers.Select(m => $"WIL {Signed(m)}")).ToArray());
            for (var rank = 0; rank <= 50; rank += 5)
                pooled.Add(new[] { rank.ToString() }.Concat(modifiers.Select(m => Number(pools.Average(p => FishingCatchModel.PoolCatchChance(p, rank, AttributeSweep.ScoreFor(m)))) + "%")).ToArray());

            text.AppendLine(pooled.ToString());

            var fish = FishingCatchModel.Fish();
            text.AppendLine($"{fish.Count} fish, levels {fish.Min(f => f.Level)} to {fish.Max(f => f.Level)}. XP per bite is `GetDeltaXP(fish level - rank)`, caught or not:");
            text.AppendLine();
            var xp = new MarkdownTable(new[] { "Fish level" }.Concat(Ranks.Select(r => $"Rank {r}")).ToArray());
            foreach (var level in new[] { 0, 10, 20, 30, 40, 50 })
                xp.Add(new[] { level.ToString() }.Concat(Ranks.Select(r => FishingCatchModel.XpPerBite(r, level).ToString())).ToArray());

            text.AppendLine(xp.ToString());
        }

        private static void AppendLimitations(StringBuilder text)
        {
            text.AppendLine("## Limitations");
            text.AppendLine();
            text.AppendLine("- The attribute modifiers are a sweep, not measured medians. A Willpower score of 10 + 2 x modifier assumes the engine's ability modifier, floor((score - 10) / 2).");
            text.AppendLine("- Harvesting counts items, not their value: the loot-table roll behind each item, and the extra `LOOT_TABLE_` rolls some nodes add through `Loot.SpawnLoot`, are not played. Scavenging counts successful rolls, not the loot they return, and ignores Treasure Hunter and Creditfinder.");
            text.AppendLine("- Respawn, node density and travel time are not modelled, so these are yields per node or site, not per hour.");
            text.AppendLine("- The fishing pool average ignores that most pools are unreachable for a given rod, and the 80% chance of no bite when only the default fish is available. Real anglers choose their location, rod and bait.");
            text.AppendLine("- Refining XP does not depend on rank today, so its rows are the same at every rank.");
        }
    }
}
