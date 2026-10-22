using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Fishing;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Scenario;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.FishingService;

namespace SWLOR.CraftSim.Tests.Fishing
{
    /// <summary>
    /// Covers the bound on the fight (owner decision, October 2026): a skilled fight's expected catch rate stays within +15% (starting value)
    /// of Quick Reel's, for the angler with no perks, the whole Angler line and the special rare fish with Trophy Catch.
    /// </summary>
    [TestFixture]
    public class FishingFightBoundGateTests
    {
        private static readonly FishingParityOptions Small = FishingParityOptions.Default with
        {
            ReferenceRanks = new[] { 30 },
            PooledRanks = new[] { 0, 30 },
            LowestDelta = -2,
            HighestDelta = 2
        };

        // The shipped bound is judged at the report's runs and seed, which take about two minutes to play; the tests below share one played set.
        private static readonly Lazy<FishingParity> ReportParity = new(() => new FishingParity(1000, 1));

        private static GateContext Context() => new(null, null, 30, 1, AttributeSweep.Default);

        [Test]
        public void TheBound_IsFifteenPercentAboveQuickReelsChancePlusThreePoints()
        {
            var options = FishingParityOptions.Default;

            options.FightBound(64).Should().BeApproximately(76.6, 1e-9);
            options.FightBound(44).Should().BeApproximately(53.6, 1e-9);
            options.FightBound(0).Should().Be(3, "a fish Quick Reel cannot land is still held to a floor, not to nothing");
            (options with { FightBoundRelative = 0.25, FightBoundSlackPoints = 0 }).FightBound(40).Should().BeApproximately(50, 1e-9);
        }

        [Test]
        public void ALooseBound_PassesAndTheTablesShowEveryRow()
        {
            var gate = new FishingFightBoundGate(new FishingParity(30, 1), Small with { FightBoundRelative = 10 });

            var result = gate.Evaluate(Context());

            result.Gate.Should().Be(FishingFightBoundGate.GateName);
            result.Verdict.Should().Be(GateVerdict.Passed, result.Summary);
            result.Tables.Should().HaveCount(4);
            result.Tables[0].Rows.Select(row => row[0]).Should().Equal("-2", "-1", "0", "+1", "+2");
            result.Tables[0].Header.Should().Contain("Quick Reel (Willpower 12)").And.Contain("Bound").And.Contain("Fight, adaptive");
            result.Tables[1].Rows.Select(row => row[0]).Should().Equal("0", "30");
            result.Tables[2].Header.Should().Contain("Whole line, Patient Angler").And.Contain("Whole line, Aggressive Angler");
            result.Tables[3].Rows.Select(row => row[0]).Should().Contain(name => name.Contains("Trophy Catch"));
            result.Tables[0].Rows.Select(row => row.Last()).Should().OnlyContain(verdict => verdict == "Within");
        }

        [Test]
        public void ABoundNoFightCanMeet_FailsAndNamesTheRowsOver()
        {
            var gate = new FishingFightBoundGate(new FishingParity(30, 1), Small with { FightBoundRelative = -1, FightBoundSlackPoints = 0 });

            var result = gate.Evaluate(Context());

            result.Verdict.Should().Be(GateVerdict.Failed);
            result.Summary.Should().Contain("more than").And.Contain("row(s)").And.Contain("with no perks");
            result.Tables[0].Rows.Select(row => row.Last()).Should().Contain("Over");
        }

        [Test]
        public void TheShippedFight_IsWithinTheBound_WithNoPerksWithTheWholeLineAndOnRareFishWithTrophyCatch()
        {
            var gate = new FishingFightBoundGate(ReportParity.Value);

            var result = gate.Evaluate(new GateContext(null, null, 1000, 1, AttributeSweep.Default));

            result.Verdict.Should().Be(GateVerdict.Passed, result.Summary);
        }

        [Test]
        public void EveryPerkTheDraftProposes_IsWithinTheBound_AndTheFirstDraftsWholeLineIsOver()
        {
            var options = FishingParityOptions.Default;
            new FishingFightBoundGate(ReportParity.Value).Evaluate(new GateContext(null, null, 1000, 1, AttributeSweep.Default));

            var table = FishingAnglerEvidence.PerkMagnitudes(ReportParity.Value, options);

            foreach (var variant in FishingAnglerEvidence.Variants)
                table.Rows.Single(row => row[0] == variant.Name).Last().Should().Be("Yes", variant.Name);

            FishingAnglerEvidence.RejectedVariants.Should().HaveCount(1);
            foreach (var variant in FishingAnglerEvidence.RejectedVariants)
                table.Rows.Single(row => row[0] == variant.Name).Last().Should().StartWith("No: over by", variant.Name);
        }

        [Test]
        public void TrophyCatch_StaysWithinTheBound_OnTheSpecialFishAtRankFifty_AloneAndWithTheWholeLine()
        {
            var options = FishingParityOptions.Default;
            new FishingFightBoundGate(ReportParity.Value).Evaluate(new GateContext(null, null, 1000, 1, AttributeSweep.Default));

            var verdicts = FishingAnglerEvidence.RareFishVerdicts(ReportParity.Value, options);

            var bound = options.FightBound(options.QuickReel(FishingAnglerEvidence.RareFishRank, FishingAnglerEvidence.RareFishLevel));
            bound.Should().BeApproximately(53.6, 1e-9, "Quick Reel lands a level 52 fish at rank 50 44% of the time at Willpower 12");
            var gated = verdicts.Where(verdict => verdict.Case.Gated).ToList();
            gated.Should().Contain(verdict => verdict.Case.Effects.RareFishRungReduction == FishingAnglerEvidence.TrophyCatchRungs);
            gated.Should().OnlyContain(verdict => verdict.Within, "Trophy Catch and the whole line together still land the fish within the bound");
            gated.Select(verdict => verdict.Landed).Max().Should().BeGreaterThan(
                verdicts.Single(verdict => verdict.Case.Name.StartsWith("Fight, a rare fish (chained moves)")).Landed,
                "Trophy Catch is worth something");
        }

        [Test]
        public void ARareFishTakesNoPerkButTrophyCatch_SoTheWholeLineAddsNothingToIt()
        {
            var parity = ReportParity.Value;
            var plain = parity.Catch(FishingAnglerEvidence.RareFishLevel, FishRarity.Rare, 50, 4, LinePlayer.Adaptive, LineAnglerEffects.None).LandedPercent;
            var withLine = parity.Catch(FishingAnglerEvidence.RareFishLevel, FishRarity.Rare, 50, 4, LinePlayer.Adaptive, FishingAnglerEvidence.WholeLinePatient).LandedPercent;

            Math.Abs(withLine - plain).Should().BeLessThan(5, "the line is ignored; the two differ only by the dice of two samples of 1000 fights");
        }
    }
}
