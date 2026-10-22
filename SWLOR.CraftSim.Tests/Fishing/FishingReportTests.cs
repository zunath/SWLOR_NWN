using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Cli;
using SWLOR.CraftSim.Fishing;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Scenario;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;

namespace SWLOR.CraftSim.Tests.Fishing
{
    [TestFixture]
    public class FishingReportTests
    {
        private static readonly FishingParityOptions Small = FishingParityOptions.Default with
        {
            ReferenceRanks = new[] { 30 },
            PooledRanks = new[] { 0, 30 },
            LowestDelta = -2,
            HighestDelta = 2
        };

        private static string WriteReport(out IReadOnlyList<GateResult> results)
        {
            var parity = new FishingParity(8, 3);
            var context = new GateContext(null, null, 8, 3, AttributeSweep.Default);
            results = FishingReport.Run(context, parity, Small, GateThresholds.Default);
            return FishingReport.Build(context, parity, Small, GateThresholds.Default, results, "SWLOR.CraftSim fishing --runs 8");
        }

        [Test]
        public void TheReportHoldsBothGatesAndTheEvidenceTheDraftQuotes()
        {
            var report = WriteReport(out var results);

            results.Select(result => result.Gate).Should().Equal(
                GateNames.EconomyParity,
                FishingFightBoundGate.GateName,
                GateNames.Length,
                GateNames.XpRate,
                "Evidence: XP rate at the fastest allowed pace",
                "Evidence: Angler perk magnitudes",
                "Evidence: style balance");
            report.Should().StartWith("# " + FishingReport.Title);
            report.Should().Contain("## Inputs").And.Contain("## Verdicts").And.Contain("## Economy parity").And.Contain("## Fight bound").And.Contain("## Length").And.Contain("## XP rate")
                .And.Contain("## Evidence: XP rate at the fastest allowed pace").And.Contain("## Evidence: Angler perk magnitudes").And.Contain("## Evidence: style balance")
                .And.Contain("## Limitations");
            report.Should().Contain("Willpower scores 8, 10, 12, 14, 16, 18, 20");
            report.Should().Contain("Rank - fish level").And.Contain("Agriculture rank").And.Contain("Trophy Catch");
            report.Should().Contain("Quick Reel").And.Contain("no attribute investment");
        }

        [Test]
        public void TheReportRecordsEveryStartingValueOfTheFightByName()
        {
            var values = FishingReport.StartingValues();

            values.Should().Contain(("RungAtRank", LineRules.RungAtRank.ToString()));
            values.Should().Contain(("TensionCapBase", LineRules.TensionCapBase.ToString()));
            values.Should().Contain(("PullPercentPerRung", "0.8"));
            values.Select(value => value.Name).Should().OnlyHaveUniqueItems();
            WriteReport(out _).Should().Contain($"`RungAtRank` = {LineRules.RungAtRank}");
        }

        [Test]
        public void TheReportIsAsciiAndNamesThePlayersAndTheTimeItAssumes()
        {
            var report = WriteReport(out _);

            report.Where(character => character > 127).Should().BeEmpty();
            report.Should().Contain("Adaptive player").And.Contain("Rule-of-thumb player").And.Contain("8.0 s for each turn");
        }

        [Test]
        public void TheReportRecordsTheXpMultiplierOfEveryBand()
        {
            var report = WriteReport(out _);

            string Text(double value) => value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            report.Should().Contain($"`XpMultiplierLevels0To10` = {Text(LineRules.XpMultiplierLevels0To10)}");
            report.Should().Contain($"`XpMultiplierLevels41Up` = {Text(LineRules.XpMultiplierLevels41Up)}");
            report.Should().Contain("CraftMinigameXp");
            report.Should().Contain($"0-10 {Text(LineRules.XpMultiplierLevels0To10)}x").And.Contain($"41-52 {Text(LineRules.XpMultiplierLevels41Up)}x");
            FishingReport.XpBands().Select(band => band.Name).Should().Equal("0-10", "11-20", "21-30", "31-40", "41-52");
            FishingReport.XpBands().Select(band => band.Multiplier).Should().Equal(
                LineRules.XpMultiplierLevels0To10,
                LineRules.XpMultiplierLevels11To20,
                LineRules.XpMultiplierLevels21To30,
                LineRules.XpMultiplierLevels31To40,
                LineRules.XpMultiplierLevels41Up);
        }

        [Test]
        public void TheGatesSayWhatTheyFound()
        {
            WriteReport(out var results);

            results.Single(result => result.Gate == GateNames.XpRate).Tables.Single().Rows.Should().HaveCount(5, "one case for each band of fish levels");
            results.Single(result => result.Gate == "Evidence: XP rate at the fastest allowed pace").Tables.Single().Rows.Should().HaveCount(5);
            results.Single(result => result.Gate == GateNames.EconomyParity).Tables.Should().HaveCount(3, "by rank above the fish's level, by rank over the pools, and per hour");
            results.Single(result => result.Gate == FishingFightBoundGate.GateName).Tables.Should().HaveCount(4, "no perks, over the pools, the Angler line, and the rare fish");
            results.Where(result => result.Gate.StartsWith("Evidence")).Should().OnlyContain(result => result.Verdict == GateVerdict.NotApplicable);
        }

        [Test]
        public void ThePerkEvidence_PlaysTheStylesAndTheCapstoneAndEveryProposedPurchase()
        {
            var parity = new FishingParity(8, 3);

            var perks = FishingAnglerEvidence.PerkMagnitudes(parity);
            var style = FishingAnglerEvidence.StyleBalance(parity);
            var rare = FishingAnglerEvidence.RareFish(parity, new[] { 10, 14 });

            perks.Rows.Should().HaveCount(FishingAnglerEvidence.Variants.Count + FishingAnglerEvidence.RejectedVariants.Count + 1);
            style.Table.Rows.Should().HaveCount(FishingAnglerEvidence.Ranks.Count * FishingAnglerEvidence.StyleDeltas.Count);
            (style.PatientShare + style.AggressiveShare).Should().BeApproximately(1.0, 1e-9);
            rare.Rows.Select(row => row[0]).Should().Contain(name => name.Contains("Trophy Catch"));
            FishingAnglerEvidence.Variants.Concat(FishingAnglerEvidence.RejectedVariants).Select(variant => variant.Name).Should().OnlyHaveUniqueItems();
        }

        [Test]
        public void TheCommandWritesTheReportAndTheJson()
        {
            var directory = Path.Combine(Path.GetTempPath(), "craftsim-fishing-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var report = Path.Combine(directory, "line.md");
                var json = Path.Combine(directory, "line.json");
                var output = new StringWriter();

                var code = FishingCommand.Run(new[] { "--runs", "6", "--seed", "2", "--out", report, "--json", json }, output, Small);

                code.Should().Be(0);
                File.ReadAllText(report).Should().StartWith("# " + FishingReport.Title);
                File.ReadAllText(json).Should().Contain("\"seed\": 2").And.Contain("\"runs\": 6");
                output.ToString().Should().Contain("Economy parity:").And.Contain("XP rate:");
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Test]
        public void TheStrictCommandExitsWithTwoWhenAGateFailed()
        {
            var directory = Path.Combine(Path.GetTempPath(), "craftsim-fishing-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var code = FishingCommand.Run(
                    new[] { "--runs", "6", "--out", Path.Combine(directory, "line.md"), "--strict" }, new StringWriter(), Small);

                code.Should().Be(2, "the XP-rate gate fails for a fight that adds turns to a bite");
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Test]
        public void TheCommandRefusesWhatItDoesNotTake()
        {
            var output = new StringWriter();

            ((Action)(() => FishingCommand.Run(new[] { "--bogus", "1" }, output))).Should().Throw<ArgumentException>();
            ((Action)(() => FishingCommand.Run(new[] { "--runs", "0" }, output))).Should().Throw<ArgumentException>();
            ((Action)(() => FishingCommand.Run(new[] { "--out", "a.md", "--json", "a.md" }, output))).Should().Throw<ArgumentException>();
        }
    }
}
