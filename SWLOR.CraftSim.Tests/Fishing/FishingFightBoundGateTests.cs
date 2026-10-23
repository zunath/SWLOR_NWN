using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Fishing;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Scenario;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.FishingService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.CraftSim.Tests.Fishing
{
    /// <summary>
    /// Covers the bound on the fight (owner decision, October 2026): an unperked skilled fight's expected catch rate stays within +15% (starting
    /// value) of Quick Reel's, the whole Angler line lifts it to about 90% of the fish at the angler's own level, and the special rare fish
    /// stay within the bound with and without Trophy Catch.
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

        // The shipped bound is judged at the report's runs and seed, which take a few minutes to play; the tests below share one played set.
        private static readonly Lazy<FishingParity> ReportParity = new(() => new FishingParity(1000, 1));

        private static GateContext Context() => new(null, null, 30, 1, AttributeSweep.Default);

        [Test]
        public void TheBound_IsFifteenPercentAboveQuickReelsChancePlusThreePoints()
        {
            var options = FishingParityOptions.Default;

            options.FightBound(60).Should().BeApproximately(72.0, 1e-9);
            options.FightBound(40).Should().BeApproximately(49.0, 1e-9);
            options.FightBound(0).Should().Be(3, "a fish Quick Reel cannot land is still held to a floor, not to nothing");
            (options with { FightBoundRelative = 0.25, FightBoundSlackPoints = 0 }).FightBound(40).Should().BeApproximately(50, 1e-9);
        }

        [Test]
        public void TheWholeLinesTarget_IsNinetyPercentWithinFivePoints()
        {
            FishingParityOptions.Default.PerkedTargetPercent.Should().Be(90);
            FishingParityOptions.Default.PerkedTolerancePoints.Should().Be(5);
        }

        [Test]
        public void ALooseBound_PassesAndTheTablesShowEveryRow()
        {
            var gate = new FishingFightBoundGate(new FishingParity(30, 1), Small with { FightBoundRelative = 10, PerkedTolerancePoints = 1000 });

            var result = gate.Evaluate(Context());

            result.Gate.Should().Be(FishingFightBoundGate.GateName);
            result.Verdict.Should().Be(GateVerdict.Passed, result.Summary);
            result.Tables.Should().HaveCount(4);
            result.Tables[0].Rows.Select(row => row[0]).Should().Equal("-2", "-1", "0", "+1", "+2");
            result.Tables[0].Header.Should().Contain("Quick Reel (Willpower 10)").And.Contain("Bound").And.Contain("Fight, adaptive");
            result.Tables[1].Rows.Select(row => row[0]).Should().Equal("0", "30");
            result.Tables[2].Header.Should().Contain("Patient Angler line").And.Contain("Aggressive Angler line").And.Contain("Target");
            result.Tables[3].Rows.Select(row => row[0]).Should().Contain(name => name.Contains("Trophy Catch"));
            result.Tables[0].Rows.Select(row => row.Last()).Should().OnlyContain(verdict => verdict == "Within");
        }

        [Test]
        public void ABoundNoFightCanMeet_FailsAndNamesTheRowsOver()
        {
            var gate = new FishingFightBoundGate(new FishingParity(30, 1), Small with { FightBoundRelative = -1, FightBoundSlackPoints = 0 });

            var result = gate.Evaluate(Context());

            result.Verdict.Should().Be(GateVerdict.Failed);
            result.Summary.Should().Contain("out of bounds").And.Contain("row(s)").And.Contain("with no perks");
            result.Tables[0].Rows.Select(row => row.Last()).Should().Contain("Over");
        }

        [Test]
        public void TheShippedFight_IsWithinTheBound_WithNoPerks_TheWholeLineIsOnTarget_AndRareFishStayInside()
        {
            var gate = new FishingFightBoundGate(ReportParity.Value);

            var result = gate.Evaluate(new GateContext(null, null, 1000, 1, AttributeSweep.Default));

            result.Verdict.Should().Be(GateVerdict.Passed, result.Summary);
        }

        [Test]
        public void TheWholeLineAtRankFifty_LandsAboutNinetyPercentOfTheFishAtItsOwnLevel_InEitherStyle()
        {
            var options = FishingParityOptions.Default;
            new FishingFightBoundGate(ReportParity.Value).Evaluate(new GateContext(null, null, 1000, 1, AttributeSweep.Default));

            var line = FishingAnglerEvidence.PerkedLine(ReportParity.Value, options);

            line.Outside.Should().BeEmpty();
            line.Table.Rows.Single(row => row[0] == "50").Last().Should().Be("Within");
            foreach (var build in FishingAnglerEvidence.Lines)
            {
                var landed = ReportParity.Value.Catch(50, FishRarity.Common, 50, 4, LinePlayer.Adaptive, build.EffectsAt(50)).LandedPercent;
                landed.Should().BeInRange(options.PerkedTargetPercent - options.PerkedTolerancePoints, options.PerkedTargetPercent + options.PerkedTolerancePoints, build.Name);
            }
        }

        [Test]
        public void TheLineDoesNotHelpAnAnglerWhoQuickReels_AndTheBoundForTheUnperkedFightHolds()
        {
            var options = FishingParityOptions.Default;

            options.QuickReel(50, 50).Should().Be(60);
            options.FightBound(options.QuickReel(50, 50)).Should().BeApproximately(72.0, 1e-9);
            FishingQuickReel.CatchChancePercent(50, 50).Should().Be(60, "no perk reads the roll");
        }

        [Test]
        public void AnUnperkedSkilledFight_LandsSlightlyMoreThanQuickReelAtTheFishsLevel()
        {
            var options = FishingParityOptions.Default;
            var parity = ReportParity.Value;
            new FishingFightBoundGate(parity).Evaluate(new GateContext(null, null, 1000, 1, AttributeSweep.Default));

            var fight = options.ReferenceRanks.Average(rank =>
                parity.Catch(rank, FishRarity.Common, rank, FishingParity.NominalRodTier(rank), LinePlayer.Adaptive, LineAnglerEffects.None).LandedPercent);
            var quickReel = options.QuickReel(0, 0);

            (fight / quickReel).Should().BeInRange(1.02, 1.08, "fighting is optional, for a slightly better catch: about 1.03-1.08 times Quick Reel's at the fish's own level");
        }

        [Test]
        public void TrophyCatch_AloneStaysWithinTheBound_OnTheSpecialFishAtRankFifty_AndTheWholeLineIsShownNotJudged()
        {
            var options = FishingParityOptions.Default;
            new FishingFightBoundGate(ReportParity.Value).Evaluate(new GateContext(null, null, 1000, 1, AttributeSweep.Default));

            var verdicts = FishingAnglerEvidence.RareFishVerdicts(ReportParity.Value, options);

            var bound = options.FightBound(options.QuickReel(FishingAnglerEvidence.RareFishRank, FishingAnglerEvidence.RareFishLevel));
            bound.Should().BeApproximately(49.0, 1e-9, "Quick Reel lands a level 52 fish at rank 50 40% of the time at Willpower 10");
            var gated = verdicts.Where(verdict => verdict.Case.Gated).ToList();
            gated.Should().Contain(verdict => verdict.Case.Effects.RareFishRungReduction == FishingAnglerEvidence.TrophyCatchRungs);
            gated.Should().OnlyContain(verdict => verdict.Within, "no perk but Trophy Catch touches the fish a gated row plays");
            gated.Should().OnlyContain(verdict => verdict.Case.Effects.TensionCapBonus == 0 && verdict.Case.Effects.SetHookDrainBonus == 0);
            verdicts.Where(verdict => !verdict.Case.Gated && verdict.Case.Rarity == FishRarity.Rare)
                .Should().NotBeEmpty("the rows with the fight perks at half strength are evidence, not judged");
            verdicts.Single(verdict => verdict.Case.Name == "Fight, a rare fish: Trophy Catch alone").Landed.Should().BeGreaterThan(
                verdicts.Single(verdict => verdict.Case.Name == "Fight, a rare fish: No perks").Landed, "Trophy Catch is worth something");
        }

        [Test]
        public void ARareFishTakesTheWholeLineAtHalfStrength()
        {
            var plain = LineSetup.ForFish(FishingAnglerEvidence.RareFishLevel, FishRarity.Rare, 50, 4);

            foreach (var line in new[] { FishingAnglerEvidence.WholeLinePatient, FishingAnglerEvidence.WholeLineAggressive })
            {
                var withLine = LineSetup.ForFish(FishingAnglerEvidence.RareFishLevel, FishRarity.Rare, 50, 4, line);

                withLine.TensionCap.Should().Be(plain.TensionCap + 4, "half of Strong Line III's 8");
                withLine.SetHookDrain.Should().Be(plain.SetHookDrain + 1, "half of Hard Set III's 3, rounded down");
                withLine.GiveLineDistance.Should().Be(plain.GiveLineDistance, "a style's one point is halved to nothing");
                withLine.ReelDistance.Should().Be(plain.ReelDistance);
                withLine.ForecastDepth.Should().Be(1, "Fish Sense does not work on a rare fish");
                withLine.Pull.Should().Be(plain.Pull);
                withLine.Stamina.Should().Be(plain.Stamina);
            }

            var withTrophy = LineSetup.ForFish(
                FishingAnglerEvidence.RareFishLevel, FishRarity.Rare, 50, 4, AnglerBuild.PatientLine.EffectsAt(50));
            withTrophy.Pull.Should().BeLessThan(plain.Pull);
            withTrophy.Stamina.Should().BeLessThan(plain.Stamina);
            FishingAnglerEvidence.TrophyCatchRungs.Should().Be(4);
        }

        [Test]
        public void TheEvidenceKeysCoverEveryCaseTheTablesRead()
        {
            var keys = FishingAnglerEvidence.Keys().Distinct().ToList();

            keys.Should().Contain(key => key.Rarity == FishRarity.Rare && key.Effects.RareFishRungReduction == FishingAnglerEvidence.TrophyCatchRungs);
            keys.Should().Contain(key => key.Rank == 50 && key.Effects.TensionCapBonus == 8 && key.Effects.GiveLineDistanceReduction == 1);
            keys.Should().Contain(key => key.Rank == 50 && key.Effects.TensionCapBonus == 8 && key.Effects.ReelDistanceBonus == 1);
            PerkType.TrophyCatch.Should().Be(AnglerBuild.Line.Single(pair => pair.Value.Name == "Trophy Catch").Key);
        }
    }
}
