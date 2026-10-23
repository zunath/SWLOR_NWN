using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Harvesting;
using SWLOR.CraftSim.Scenario;
using SWLOR.CraftSim.Search;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service.CraftService.Minigame.Gathering;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.CraftSim.Tests.Harvesting
{
    /// <summary>The Prospector and Treasure Seeker lines in the simulator: the loadouts, the caps every one stays under, and the study's exports.</summary>
    [TestFixture]
    public class GatheringPerkStudyTests
    {
        private static readonly IReadOnlyDictionary<PerkType, PerkDetail> Perks = new GatheringPerkDefinition().BuildPerks();

        private static GateContext Context(int runs) => new(null, null, runs, 1, AttributeSweep.Default);

        [Test]
        public void NoPerks_PlaysTheEffectsNoneEveryGateAlreadyJudges()
        {
            var options = HarvestingParityOptions.Default;
            var search = SearchParityOptions.Default;

            foreach (var rank in new[] { 0, 20, 50 })
            {
                options.EffectsAt(rank).Should().Be(ProspectingEffects.None);
                search.EffectsAt(rank).Should().Be(SearchEffects.None);
            }
        }

        [Test]
        public void TheLoadoutsAreEveryLineAndStyleCombinationTheGatesRun()
        {
            GatheringLoadout.All.Select(loadout => loadout.Slug).Should().OnlyHaveUniqueItems().And.HaveCount(8);
            GatheringLoadout.All.First().Should().Be(GatheringLoadout.None);
            GatheringLoadout.Find("both-lines-careful-hands").Should().NotBeNull();
            GatheringLoadout.Find("nope").Should().BeNull();
        }

        [Test]
        public void AProspectorSessionAtRankFiftyIsPlayedWithTheLinesEffects()
        {
            var effects = GatheringLoadout.Find("prospector-strip-mining")!.ProspectingAt(Perks, 50);

            effects.StrikeTimeReductionTenths.Should().Be(ProspectingPerkRules.EfficientStrikesStrikeTenthsRank2 + ProspectingPerkRules.StripMiningStrikeTimeTenths);
            effects.SeamWorkTimeReductionTenths.Should().Be(-ProspectingPerkRules.StripMiningSeamWorkTimeTenths);
            effects.StrikeStabilityReduction.Should().Be(-ProspectingPerkRules.StripMiningStrikeStability);
            effects.RichExtractTimeReductionTenths.Should().Be(ProspectingPerkRules.RichVeinsTimeTenthsRank2);
            effects.MotherLode.Should().BeTrue();
            effects.SeesSeamTypes.Should().BeTrue();
        }

        [Test]
        public void AnOverrideReplacesTheLoadoutForAStudyOfOnePurchase()
        {
            var effects = ProspectorPerkStudy.OfLevel(Perks, PerkType.RichVeins, 2);
            var options = HarvestingParityOptions.Default with { EffectsOverride = _ => effects };

            options.EffectsAt(10).Should().Be(effects);
            effects.RichExtractTimeReductionTenths.Should().Be(ProspectingPerkRules.RichVeinsTimeTenthsRank2);
        }

        [Test]
        public void TheProspectorLineLeavesTheSkilledSessionsItemsGemsAndCaps_AndSavesTime()
        {
            var nodes = HarvestingFixture.Nodes;
            var ranks = new[] { 25, 50 };
            var tiers = new[] { 3 };
            var probes = ProspectorPerkStudy.Probes(Perks)
                .Where(probe => probe.Name is "No perks" or "Prospector, no style (every purchase the rank allows)").ToList();

            var rows = ProspectorPerkStudy.Play(nodes, Perks, 6000, 1, probes, ranks, tiers);

            var none = rows.Single(row => row.Name == "No perks");
            var line = rows.Single(row => row.Name.StartsWith("Prospector, no style", StringComparison.Ordinal));
            line.SkilledItems.Should().BeApproximately(none.SkilledItems, 1e-9, "the line adds no item and saves no Stability");
            line.SkilledCharges.Should().BeApproximately(none.SkilledCharges, 1e-9);
            line.SkilledSeconds.Should().BeLessThan(none.SkilledSeconds * 0.9);
            line.SkilledActions.Should().BeGreaterThanOrEqualTo(3);
        }

        [TestCase("prospector-strip-mining")]
        [TestCase("both-lines-careful-hands")]
        public void EveryLoadoutPassesTheHarvestingGatesAtTheRanksTheCapsBite(string slug)
        {
            var loadout = GatheringLoadout.Find(slug)!;
            var options = new HarvestingParityOptions { Ranks = new[] { 25, 50 }, Loadout = loadout, Perks = Perks };
            var parity = new HarvestingParity(20000, 1, HarvestingFixture.Nodes, options);

            var results = HarvestingReport.Run(Context(20000), parity, GateThresholds.Default);

            results.Where(result => result.Gate is GateNames.EconomyParity or GateNames.Length or GateNames.XpRate)
                .Should().OnlyContain(result => result.Verdict == GateVerdict.Passed, string.Join(" ", results.Select(result => result.Summary)));
        }

        [TestCase("treasure-seeker-no-style")]
        [TestCase("treasure-seeker-careful-hands")]
        public void EveryLoadoutPassesTheSearchGatesAtTheRanksTheCapsBite(string slug)
        {
            var loadout = GatheringLoadout.Find(slug)!;
            var options = SearchParityOptions.Default with { Ranks = new[] { 20, 50 }, SiteLevels = new[] { 2 }, Loadout = loadout, Perks = Perks };
            var parity = new SearchParity(8000, 1);

            var results = SearchReport.Run(Context(8000), parity, options, GateThresholds.Default);

            results.Where(result => result.Gate is GateNames.EconomyParity or GateNames.Length or GateNames.XpRate)
                .Should().OnlyContain(result => result.Verdict == GateVerdict.Passed, string.Join(" ", results.Select(result => result.Summary)));
        }

        private static GateResult SearchYield(string slug)
        {
            var options = SearchParityOptions.Default with { Ranks = new[] { 20, 50 }, SiteLevels = new[] { 2 }, Loadout = GatheringLoadout.Find(slug)!, Perks = Perks };
            return SearchReport.Run(Context(4000), new SearchParity(4000, 1), options, GateThresholds.Default)
                .Single(result => result.Gate == GateNames.EconomyParity);
        }

        [TestCase("prospector-no-style")]
        [TestCase("prospector-strip-mining")]
        public void AProspectorOnlyLoadoutKeepsTheUnperkedSearchLimits_BecauseItChangesNothingOnTheBoard(string slug)
        {
            var result = SearchYield(slug);

            result.Verdict.Should().Be(GateVerdict.Passed, result.Summary);
            result.Summary.Should().Contain("a careless board finds at least 0.85x").And.Contain("to 1.08x").And.NotContain("no floor");
        }

        [Test]
        public void OnlyCarefulHandsLiftsTheCarelessFloor_AndOnlyTheTreasureSeekerLineOrCarefulHandsLiftTheSkilledLimit()
        {
            var seeker = SearchYield("treasure-seeker-no-style").Summary;
            seeker.Should().Contain("a careless board finds at least 0.85x").And.Contain("to 1.15x");

            var careful = SearchYield("treasure-seeker-careful-hands").Summary;
            careful.Should().Contain("a careless board has no floor").And.Contain("to 1.15x");
        }

        [Test]
        public void ThePartOptionTakesOnlyThePartsTheReportHas()
        {
            var act = () => SWLOR.CraftSim.Cli.GatheringPerksCommand.Run(new[] { "--part", "bibel" }, new StringWriter());

            act.Should().Throw<ArgumentException>().WithMessage("*bibel*prospecting*bible*");
        }

        [Test]
        public void StripMiningAgainstNoStyleIsJudgedOnNineKindsOfNodeAtEveryRankAndPooledByTheGate()
        {
            var rows = ProspectorStyleBalance.Compare(
                _ => ProspectorPerkStudy.OfStyle(GatheringStyle.StripMining),
                _ => ProspectingEffects.None,
                0.05,
                4000,
                1,
                GateThresholds.Default,
                ProspectorStyleBalance.OverheadSeconds);

            rows.Where(row => !row.IsGate).Select(row => row.Rank).Should().Equal(ProspectorStyleBalance.Ranks);
            rows.Where(row => !row.IsGate).Should().OnlyContain(row => row.Cases == ProspectorStyleBalance.Kinds.Count);
            var gate = rows.Single(row => row.IsGate);
            gate.Cases.Should().Be(ProspectorStyleBalance.Ranks.Count * ProspectorStyleBalance.Kinds.Count);
            gate.FirstWins.Should().BeGreaterThanOrEqualTo(GateThresholds.Default.StyleMinimumWinShare);
            gate.SecondWins.Should().BeGreaterThanOrEqualTo(GateThresholds.Default.StyleMinimumWinShare);
            Math.Abs(gate.MeanGap).Should().BeLessThanOrEqualTo(GateThresholds.Default.StyleMaximumMeanDifferencePoints);
            gate.Passed.Should().BeTrue();
        }

        [Test]
        public void TheBibleExportHasTwentyRowsOfFiftyPointsInRankOrder_FromTheGamesOwnDefinitions()
        {
            var rows = GatheringPerkBibleRows.Read(Perks);

            rows.Should().HaveCount(20);
            rows.Sum(row => row.Price).Should().Be(50);
            rows.Where(row => row.Line == "Prospector").Select(row => row.Rank).Should().Equal(5, 10, 15, 20, 25, 30, 35, 40, 45, 50);
            rows.Where(row => row.Line == "Treasure Seeker").Select(row => row.Rank).Should().Equal(5, 10, 15, 20, 25, 30, 35, 40, 45, 50);
            rows.Where(row => row.Type == "Style").Select(row => row.Name).Should().BeEquivalentTo("Strip Mining", "Careful Hands");
            rows.Select(row => row.Name).Should().OnlyHaveUniqueItems();
            rows.Should().OnlyContain(row => row.Description.All(character => character < 128));

            var json = JObject.Parse(GatheringPerkBibleRows.ToJson(rows));
            json["rows"]!.Count().Should().Be(20);
        }

        [Test]
        public void ARunOfTheCommandWritesEveryPartOfTheReport()
        {
            var directory = Directory.CreateTempSubdirectory("craftsim-gathering-perks-").FullName;
            try
            {
                var report = Path.Combine(directory, "gathering-perks.md");
                var writer = new StringWriter();

                var exit = SWLOR.CraftSim.Cli.GatheringPerksCommand.Run(new[] { "--runs", "400", "--seed", "1", "--out", report }, writer);

                exit.Should().Be(0);
                var text = File.ReadAllText(report);
                text.Should().Contain("## Inputs").And.Contain("## Prospecting").And.Contain("## Search").And.Contain("## Gate 2: style balance");
                text.Should().Contain("## The parity gates for every loadout").And.Contain("## Bulk Refining").And.Contain("## Limitations");
                text.Should().Contain("Both lines, Careful Hands");
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
