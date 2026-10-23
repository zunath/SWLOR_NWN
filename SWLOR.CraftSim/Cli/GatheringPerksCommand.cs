using System.Text;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Harvesting;
using SWLOR.CraftSim.Report;
using SWLOR.CraftSim.Scenario;
using SWLOR.CraftSim.Search;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame.Gathering;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;
using SWLOR.Game.Server.Service.PerkService;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Cli
{
    /// <summary>
    /// The <c>gathering-perks</c> command: plays the Prospector and Treasure Seeker lines and writes their evidence to
    /// <c>design/testing/crafting-revamp/gathering-perks.md</c> unless <c>--out</c> says otherwise: what each purchase does, the caps
    /// every loadout must stay under (the harvesting and search parity gates run for each loadout), the styles against each other and the
    /// line against the unperked gatherer, and Bulk Refining.
    /// </summary>
    public static class GatheringPerksCommand
    {
        public const int MaximumRuns = 200_000;
        public const int DefaultRuns = 4000;

        private static readonly string[] ValueOptions = { "--runs", "--seed", "--out", "--repo", "--part" };

        public static int Run(string[] args, TextWriter output)
        {
            var options = CommandLineOptions.Parse(args, ValueOptions, Array.Empty<string>());
            var runs = CommandLineOptions.GetInt(options, "--runs", DefaultRuns, 1, MaximumRuns);
            var seed = CommandLineOptions.GetInt(options, "--seed", 1, int.MinValue);
            var root = options.TryGetValue("--repo", out var repo) ? RepositoryPaths.FindRoot(repo[0]) : RepositoryPaths.FindRoot();
            var outPath = options.TryGetValue("--out", out var outValues)
                ? outValues[0]
                : Path.Combine(root, "design", "testing", "crafting-revamp", "gathering-perks.md");
            var part = options.TryGetValue("--part", out var partValues) ? partValues[0] : "all";
            if (part == "bible" && !options.ContainsKey("--out"))
                outPath = Path.Combine(root, "design", "testing", "crafting-revamp", "gathering-perk-bible-rows.json");

            var perks = new GatheringPerkDefinition().BuildPerks();
            if (part == "bible")
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
                OutputFile.WriteAllText(outPath, GatheringPerkBibleRows.ToJson(GatheringPerkBibleRows.Read(perks)));
                output.WriteLine($"Wrote {outPath}");
                return 0;
            }

            var nodes = HarvestNodeCatalog.Read(Path.Combine(root, "Module", "utp"));
            var thresholds = GateThresholds.Default;
            var context = new GateContext(null, null, runs, seed, AttributeSweep.Default);
            var text = new StringBuilder();
            text.AppendLine("# Crafting revamp: the Gathering perk lines (Prospector and Treasure Seeker)");
            text.AppendLine();
            text.AppendLine($"Command: `SWLOR.CraftSim gathering-perks {string.Join(" ", args)}`");
            text.AppendLine();
            if (part == "all")
                AppendInputs(text, runs, seed);

            if (part is "all" or "prospecting")
            {
                output.WriteLine("Playing the nodes...");
                var tiers = new[] { 1, 3, 5 };
                var ranks = ProspectorPerkStudy.Ranks;
                var rows = ProspectorPerkStudy.Play(nodes, perks, runs, seed, ProspectorPerkStudy.Probes(perks), ranks, tiers);
                text.AppendLine("## Prospecting");
                text.AppendLine();
                AppendTable(text, ProspectorPerkStudy.Table(rows, runs, ranks, tiers));
            }

            if (part is "all" or "search")
            {
                output.WriteLine("Playing the boards...");
                var searchOptions = SearchParityOptions.Default with { Perks = perks };
                var parity = new SearchParity(runs, seed);
                var probes = TreasureSeekerPerkStudy.Probes(perks);
                parity.Prepare(TreasureSeekerPerkStudy.Keys(probes));
                text.AppendLine("## Search");
                text.AppendLine();
                AppendTable(text, TreasureSeekerPerkStudy.Table(parity, searchOptions, probes));
            }

            if (part is "all" or "styles")
            {
                output.WriteLine("Comparing the styles...");
                AppendStyleBalance(text, nodes, perks, runs, seed, thresholds);
            }

            if (part is "all" or "gates")
            {
                output.WriteLine("Running the gates for every loadout...");
                AppendLoadoutGates(text, output, nodes, perks, runs, seed, context, thresholds);
            }

            if (part is "all" or "refining")
                AppendRefining(text, perks);

            if (part == "all")
                AppendLimitations(text);

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
            OutputFile.WriteAllText(outPath, text.ToString());
            output.WriteLine($"Wrote {outPath}");
            return 0;
        }

        private static void AppendInputs(StringBuilder text, int runs, int seed)
        {
            text.AppendLine("## Inputs");
            text.AppendLine();
            text.AppendLine($"- Seed {seed}; {runs} nodes or boards per case and plan. The unperked gatherer is the one `harvesting-prospecting-parity.md` and `scavenging-search-parity.md` describe, and Quick Harvest and Quick Search read no perk, so their numbers do not move.");
            text.AppendLine("- The perks are read from the game's own definitions (`GatheringPerkDefinition`) and a gatherer owns every purchase their Gathering rank allows; a style adds what its status effect carries. The loadouts are the lines and the styles in every combination the gates run.");
            text.AppendLine("- Economy bounds (the coordinator's, October 23): a skilled session of any plan stays within 1.15x of today's items and gems per node and of Quick Search's rolls per board, and spends at most about 1.5x of today's harvester charges per node. The Treasure Seeker line's value is its rare finds, which no cap limits.");
            text.AppendLine("- Why the Prospector line spends time and not Stability: a skilled session already claims nearly every seam its Stability allows, so it already sits at the cap (1.136x, and 1.145x for the best of nine plans). Any Stability a perk saves lets it claim more and lifts it past 1.15x (Efficient Strikes at -2 and -4 Stability alone took it to 1.151x and 1.163x; a cheaper Extract of a rich seam to 1.165x). A perk that adds an item, an extra ore roll or a gem fails the cap by construction. Time and power cores are not what decides a seam, so the line saves those.");
            text.AppendLine();
        }

        private static void AppendStyleBalance(
            StringBuilder text,
            IReadOnlyList<HarvestNodeType> nodes,
            IReadOnlyDictionary<PerkType, PerkDetail> perks,
            int runs,
            int seed,
            GateThresholds thresholds)
        {
            var tierThree = nodes.Where(node => node.Tier == 3).ToList();
            var gemChance = tierThree.Count == 0 ? 0.05 : tierThree.Average(node => node.GemChance);
            var overhead = ProspectorStyleBalance.OverheadSeconds;
            var line = GatheringLoadout.Perked.First(loadout => loadout.Slug == "prospector-strip-mining");
            var plain = GatheringLoadout.Perked.First(loadout => loadout.Slug == "prospector-no-style");
            var alone = ProspectorStyleBalance.Compare(
                _ => ProspectorPerkStudy.OfStyle(GatheringStyle.StripMining), _ => ProspectingEffects.None, gemChance, runs, seed, thresholds, overhead);
            var withLine = ProspectorStyleBalance.Compare(
                rank => line.ProspectingAt(perks, rank), rank => plain.ProspectingAt(perks, rank), gemChance, runs, seed, thresholds, overhead);

            text.AppendLine("## Gate 2: style balance");
            text.AppendLine();
            text.AppendLine(
                "The two styles never compete inside one window. Strip Mining changes only the Prospecting session and Careful Hands only the Search board, so a harvester chooses between Strip Mining and the plain rules, and a scavenger between Careful Hands and the plain rules. " +
                $"Prospecting is judged on the nine kinds of node (a pool of 1 to 4 units and 1 to 3 seams) at the ranks a style is open at: both players are played on the same nodes and dice, each by the best of the skilled plans for the kind, and scored by items per second of play with {overhead:0} seconds to reach the next node. " +
                "A point is one percent of the better score. The verdict is the pooled row; the rows of single ranks show how the balance moves. " +
                "Careful Hands has no partner on the Search board, so it has no pair to balance: the table in the Search section shows what it trades (finds for rare items), and a scavenger who wants common materials plays the plain board.");
            text.AppendLine();
            AppendTable(text, ProspectorStyleBalance.Table("Prospecting: Strip Mining against no style, no other perks", "Strip Mining", "No style", alone));
            AppendTable(text, ProspectorStyleBalance.Table("Prospecting: Strip Mining against no style, with the whole Prospector line", "Strip Mining", "No style", withLine));
        }

        private static void AppendLoadoutGates(
            StringBuilder text,
            TextWriter output,
            IReadOnlyList<HarvestNodeType> nodes,
            IReadOnlyDictionary<PerkType, PerkDetail> perks,
            int runs,
            int seed,
            GateContext context,
            GateThresholds thresholds)
        {
            text.AppendLine("## The parity gates for every loadout");
            text.AppendLine();
            text.AppendLine(
                "Each loadout plays the harvesting study (`HarvestingReport`: economy parity with the +15% item and gem caps and the 1.5x charge cap, length, XP rate) and the search study (`SearchReport`: the +15% cap on rolls, length, XP rate) as a gatherer who owns every purchase the Gathering rank allows. " +
                "`SWLOR.CraftSim harvesting --loadout <name>` and `search --loadout <name>` write any of them in full.");
            text.AppendLine();

            var summary = new EvidenceTable("Loadout", "Harvesting: economy parity", "Harvesting: length", "Harvesting: XP rate", "Search: economy parity", "Search: length", "Search: XP rate");
            var details = new StringBuilder();
            var searchParity = new SearchParity(runs, seed);
            foreach (var loadout in GatheringLoadout.All)
            {
                output.WriteLine($"  {loadout.Name}");
                var harvestOptions = HarvestingParityOptions.Default with { Loadout = loadout, Perks = perks };
                var harvest = HarvestingReport.Run(context, new HarvestingParity(runs, seed, nodes, harvestOptions), thresholds);
                var searchOptions = SearchParityOptions.Default with { Loadout = loadout, Perks = perks };
                var search = SearchReport.Run(context, searchParity, searchOptions, thresholds);

                GateResult Find(IReadOnlyList<GateResult> results, string gate) => results.First(result => result.Gate == gate);
                var hy = Find(harvest, GateNames.EconomyParity);
                var hl = Find(harvest, GateNames.Length);
                var hx = Find(harvest, GateNames.XpRate);
                var sy = Find(search, GateNames.EconomyParity);
                var sl = Find(search, GateNames.Length);
                var sx = Find(search, GateNames.XpRate);
                summary.Add(loadout.Name, hy.Verdict.ToString(), hl.Verdict.ToString(), hx.Verdict.ToString(), sy.Verdict.ToString(), sl.Verdict.ToString(), sx.Verdict.ToString());

                details.AppendLine($"### {loadout.Name}");
                details.AppendLine();
                details.AppendLine($"- Harvesting, economy parity: {hy.Summary}");
                details.AppendLine($"- Harvesting, length: {hl.Summary}");
                details.AppendLine($"- Harvesting, XP rate: {hx.Summary}");
                details.AppendLine($"- Search, economy parity: {sy.Summary}");
                details.AppendLine($"- Search, length: {sl.Summary}");
                details.AppendLine($"- Search, XP rate: {sx.Summary}");
                details.AppendLine();
            }

            AppendTable(text, summary.ToGateTable("Verdicts"));
            text.Append(details);
        }

        private static void AppendRefining(StringBuilder text, IReadOnlyDictionary<PerkType, PerkDetail> perks)
        {
            var loadout = GatheringLoadout.Perked.First(entry => entry.Slug == "prospector-no-style");
            var table = new EvidenceTable(
                "Gathering rank", "Items per power core, rank only", "Bulk Refining", "Items per power core, with it", "Power cores for 100 ore", "With Bulk Refining", "Power cores saved");
            foreach (var rank in new[] { 0, 10, 20, 30, 40, 45, 50 })
            {
                var milestone = CraftRankMilestones.RefineryManagementLevelForRank(rank);
                var bulk = loadout.RefineBatchAt(perks, rank);
                var before = RefineryViewModel.ItemsPerCore(milestone, 0);
                var after = RefineryViewModel.ItemsPerCore(milestone, bulk);
                var coresBefore = RefineryViewModel.PowerCoresFor(100, before);
                var coresAfter = RefineryViewModel.PowerCoresFor(100, after);
                table.Add(
                    rank.ToString(), before.ToString(), $"+{bulk}", after.ToString(), coresBefore.ToString(), coresAfter.ToString(),
                    Percent(1.0 - (double)coresAfter / coresBefore, 0));
            }

            text.AppendLine("## Bulk Refining");
            text.AppendLine();
            text.AppendLine(
                "Refining converts ore one for one, so Bulk Refining changes only the power cores a batch costs, which are bought with credits: it is a credit saving and no supply. A core refined 3 items before the revamp, and 9 with Refinery Management VI at Gathering 45 (the plan gives those six to everyone by rank). The line adds 1, 2 and 3 on top: 12 at most, 33% over the old maximum of 9 and 25% fewer cores than the plan's batch at Gathering 45. Refining XP does not depend on the batch.");
            text.AppendLine();
            AppendTable(text, table.ToGateTable("Power cores for a batch of ore, by Gathering rank, with the Prospector line"));
        }

        private static void AppendLimitations(StringBuilder text)
        {
            text.AppendLine("## Limitations");
            text.AppendLine();
            text.AppendLine("- No client run: the windows, the style lists and the timing of the quicker actions are unverified in the game. Seconds per action are the engine's, and 30 seconds to reach the next node is the progression model's unmeasured estimate.");
            text.AppendLine("- The skilled player is the study's best careful plan; real players are neither as consistent nor as unaware of what is hidden. Sampling noise on the highest of nine plans is about half a point.");
            text.AppendLine("- Gate 2's score is items per second, and gems are counted as items.");
            text.AppendLine("- Gem pockets are about 2% of tier 1 nodes and 0.3% of tier 5 nodes (a pocket is `GemPocketShare` of a table's gem chance), so the pocket perks (Sharp Chisel, Deep Scan's pocket order, Core Sample's pocket warning) rarely matter in play.");
        }

        internal static void AppendTable(StringBuilder text, GateTable table)
        {
            text.AppendLine($"### {table.Title}");
            text.AppendLine();
            var markdown = new MarkdownTable(table.Header.ToArray());
            foreach (var row in table.Rows)
                markdown.Add(row.ToArray());

            text.AppendLine(markdown.ToString());
        }
    }
}
