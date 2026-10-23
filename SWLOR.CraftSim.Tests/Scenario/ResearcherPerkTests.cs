using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Play;
using SWLOR.CraftSim.Rng;
using SWLOR.CraftSim.Scenario.Experiment;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;
using SWLOR.Game.Server.Service.CraftService.Research;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Tests.Scenario
{
    [TestFixture]
    public class ResearcherPerkTests
    {
        private static readonly GearLevel Mid = GearLevel.Fixed("mid gear", SkillType.Fabrication, 13, 12, 26);

        [Test]
        public void ABuildTakesItsNumbersFromThePerkDefinitionsAtTheRankItIsPlayedAt()
        {
            ResearcherBuild.Unperked.TuningAt(50).Should().Be(ExperimentTuning.Default);
            ResearcherBuild.MethodicalAlone.TuningAt(19).Should().Be(ExperimentTuning.Default, "the style opens at Fabrication 20");
            ResearcherBuild.MethodicalAlone.TuningAt(20).Should().Be(ExperimentTuning.Default with { Trials = 8, TestChanceBonusPercent = -15 });
            ResearcherBuild.IntuitiveAlone.TuningAt(50).Should().Be(ExperimentTuning.Default with { Trials = 5, TestChanceBonusPercent = 20 });
            ResearcherBuild.MethodicalLine.TuningAt(50).Should().Be(new ExperimentTuning(9, -3, 20, 25, 7, 25, true, true));
            ResearcherBuild.IntuitiveLine.TuningAt(50).Should().Be(new ExperimentTuning(6, 32, 20, 25, 7, 25, true, true));
            ResearcherBuild.Alone(PerkType.FieldNotes).TuningAt(39).TestChanceBonusPercent.Should().Be(6);
            ResearcherBuild.Alone(PerkType.FieldNotes).TuningAt(40).TestChanceBonusPercent.Should().Be(12);
            ResearcherBuild.AllPerks.Should().HaveCount(9);
        }

        [Test]
        public void AFullLineOwnsOneStyleOnly()
        {
            ResearcherBuild.MethodicalLine.Perks.Should().Contain(PerkType.Methodical).And.NotContain(PerkType.Intuitive);
            ResearcherBuild.IntuitiveLine.Perks.Should().Contain(PerkType.Intuitive).And.NotContain(PerkType.Methodical);
            ResearcherBuild.MethodicalLine.Perks.Should().HaveCount(ResearcherBuild.AllPerks.Count - 1);
        }

        [Test]
        public void TheFocusedPerkedPolicySpendsBreakthroughFirst_ThenRefinesAndTests()
        {
            var setup = new ExperimentSetup(3, 50, 50, 200, false, ExperimentTuning.Default with { Breakthrough = true });
            var engine = new ExperimentEngine(new SeededMinigameRandom(1), setup);
            var policy = new ExperimentPerkedPolicy(ResearchCandidate.CreditReduction);

            policy.Choose(engine).Should().Be(ExperimentActionIds.Breakthrough);
            engine.Perform(ExperimentActionIds.Breakthrough);
            policy.Choose(engine).Should().Be(ExperimentActionIds.TestCreditReduction, "Refine Method is unavailable while Breakthrough waits");
            engine.Perform(ExperimentActionIds.TestCreditReduction);
            policy.Choose(engine).Should().Be(ExperimentActionIds.RefineMethod);
        }

        [Test]
        public void TheUndecidedPolicyTestsBothThenBacksTheLeaderAndPoolsWithPeerReview()
        {
            var tuning = ExperimentTuning.Default with { PeerReview = true };
            var setup = new ExperimentSetup(3, 50, 50, 0, false, tuning);
            var engine = new ExperimentEngine(new ScriptedRandom(1), setup);
            var policy = new ExperimentPerkedPolicy(ResearchCandidate.CreditReduction, spread: true);

            var chosen = new List<string>();
            while (engine.Status == MinigameStatus.InProgress)
            {
                var action = policy.Choose(engine);
                chosen.Add(action);
                engine.Perform(action).Performed.Should().BeTrue(action);
            }

            chosen.Take(3).Should().Equal(
                ExperimentActionIds.TestCreditReduction, ExperimentActionIds.TestTimeReduction, ExperimentActionIds.TestCreditReduction);
            chosen.Should().Contain(ExperimentActionIds.PeerReviewTimeToCredit, "the evidence behind the leader is pooled into it");
        }

        [Test]
        public void AnUndecidedPlayerWithPeerReviewKeepsItsCpBackFromRefineMethod()
        {
            // 62 CP: one Refine Method (30) would leave 32, which pays for Peer Review (25); a second would not.
            var tuning = ExperimentTuning.Default with { PeerReview = true };
            var engine = new ExperimentEngine(new ScriptedRandom(1), new ExperimentSetup(3, 50, 50, 62 - 37, false, tuning));
            var policy = new ExperimentPerkedPolicy(ResearchCandidate.CreditReduction, spread: true);

            engine.Meters.Cp.Max.Should().Be(62);
            policy.Choose(engine).Should().Be(ExperimentActionIds.RefineMethod);
            engine.Perform(ExperimentActionIds.RefineMethod);
            engine.Perform(ExperimentActionIds.TestCreditReduction);
            policy.Choose(engine).Should().NotBe(ExperimentActionIds.RefineMethod, "32 CP less 30 would leave too little for Peer Review");
        }

        [Test]
        public void ASampleIsRepeatableFromItsSeed_AndTheBuildIsLeftOutOfTheDice()
        {
            var experimentCase = new ExperimentCase(3, 5, 50, Mid);

            var a = ResearcherPerkStudy.Play(experimentCase, ExperimentTuning.Default, _ => new ExperimentPerkedPolicy(ResearchCandidate.CreditReduction), 50, 3);
            var b = ResearcherPerkStudy.Play(experimentCase, ExperimentTuning.Default, _ => new ExperimentPerkedPolicy(ResearchCandidate.CreditReduction), 50, 3);
            var other = ResearcherPerkStudy.Play(experimentCase, ExperimentTuning.Default, _ => new ExperimentPerkedPolicy(ResearchCandidate.CreditReduction), 50, 4);

            b.Should().Be(a);
            other.Should().NotBe(a);
            a.Trials.Should().Be(6);
        }

        [Test]
        public void ABreakthroughBuildMeetsTheSameDiceAtTheSameTrialAsAnUnperkedOne()
        {
            // Breakthrough's guaranteed Test takes no roll; every later Test must still read its own trial's roll.
            var experimentCase = new ExperimentCase(3, 5, 50, Mid);
            var rolls = new Dictionary<bool, List<int>> { [false] = new(), [true] = new() };
            foreach (var breakthrough in new[] { false, true })
            {
                var tuning = ExperimentTuning.Default with { Breakthrough = breakthrough, Trials = 6 };
                ExperimentEngine engine = null;
                var random = new RecordingRandom(new TrialPairedRandom(
                    () => engine?.TrialsUsed ?? 0,
                    trial => SimMinigameRandom.ForRun(1, ResearcherPerkStudy.StudyStream, 3, 5, 50, Mid.Tier, 0, 0, trial)), rolls[breakthrough]);
                engine = new ExperimentEngine(random, experimentCase.CreateSetup(tuning));
                CraftPlayer.Play(engine, new ExperimentPerkedPolicy(ResearchCandidate.CreditReduction));
            }

            // The unperked researcher's Tests at trials 0..5 and the Breakthrough researcher's Tests at trials 1..5 read the same dice.
            rolls[true].Should().Equal(rolls[false].Skip(rolls[false].Count - rolls[true].Count));
        }

        /// <summary>Records every D100 an engine draws.</summary>
        private sealed class RecordingRandom : IMinigameRandom
        {
            private readonly IMinigameRandom _inner;
            private readonly List<int> _rolls;

            public RecordingRandom(IMinigameRandom inner, List<int> rolls)
            {
                _inner = inner;
                _rolls = rolls;
            }

            public int D100()
            {
                var roll = _inner.D100();
                _rolls.Add(roll);
                return roll;
            }

            public int Next(int minInclusive, int maxExclusive) => _inner.Next(minInclusive, maxExclusive);
        }

        [Test]
        public void TheScoreIsTheShareOfTheMaximumTheExperimentIsFor()
        {
            var fixedCase = new ExperimentCase(1, 1, 10, Mid);
            var contested = new ExperimentCase(3, 1, 10, Mid);
            var sample = new ResearcherPerkStudy.Sample(7, 6, 6, 12.5, 5, 0.1, 0.1, 0);

            ResearcherPerkStudy.Score(fixedCase, sample).Should().BeApproximately(50, 1e-9, "12.5 of the 25% cap");
            ResearcherPerkStudy.Score(contested, sample).Should().BeApproximately(50, 1e-9, "5 of today's 10%");
        }

        [Test]
        public void TheReportHasEverySectionAndIsRepeatable()
        {
            var options = new ResearcherPerkStudyOptions { Runs = 4, Seed = 1, Gear = new[] { GearLevel.None(SkillType.Fabrication), Mid } };

            var report = ResearcherPerkStudy.Build(options, "test");

            report.Should().Contain("## Style balance: Methodical against Intuitive");
            report.Should().Contain("## The styles' magnitudes");
            report.Should().Contain("## Each purchase alone, and the whole line");
            report.Should().Contain("## Peer Review for a player who has not decided");
            report.Should().Contain("## Fabrication XP per real hour with the line");
            report.Should().Contain("Methodical +2 trials, -15 points");
            ResearcherPerkStudy.Build(options, "test").Should().Be(report);
            report.Should().MatchRegex("^[\\x20-\\x7E\\r\\n]*$", "a report is ASCII");
        }

        [Test]
        public void TheStyleSearchJudgesEachPairingFromOnePlayOfEachStyle_AndIsRepeatable()
        {
            var options = new ResearcherPerkStudyOptions { Runs = 4, Seed = 1, Gear = new[] { GearLevel.None(SkillType.Fabrication), Mid } };
            var methodical = new[] { new ResearcherStyleSearch.StyleNumbers(2, -15), new ResearcherStyleSearch.StyleNumbers(3, -20) };
            var intuitive = new[] { ResearcherStyleSearch.ApprovedIntuitive };

            var results = ResearcherStyleSearch.Search(options, methodical, intuitive);

            results.Should().HaveCount(2);
            results.Select(result => result.Distance).Should().Equal(new[] { 5, 15 }, "the distance counts points of chance and 5 points a trial");
            results.Should().OnlyContain(result => result.AloneFailed >= 0 && result.AloneFailed <= 4 && result.LineFailed >= 0 && result.LineFailed <= 4);
            ResearcherStyleSearch.Search(options, methodical, intuitive).Should().Equal(results);
            ResearcherStyleSearch.Distance(ResearcherStyleSearch.ApprovedMethodical, ResearcherStyleSearch.ApprovedIntuitive).Should().Be(0);
        }

        [Test]
        public void TheStyleSearchReportNamesTheClosestPassingPairOrTheBestOfThoseThatFail()
        {
            var options = new ResearcherPerkStudyOptions { Runs = 2, Seed = 1, Gear = new[] { GearLevel.None(SkillType.Fabrication), Mid } };

            var report = ResearcherStyleSearch.Build(options, "test");

            report.Should().Contain("## Result");
            report.Should().Contain("Pairs that pass both:");
            report.Should().Contain("## The implemented pair, case by case");
            report.Should().Contain("1024 pairs");
            report.Should().MatchRegex("^[\x20-\x7E\r\n]*$", "a report is ASCII");
        }

        [Test]
        public void TheLineAblationDescribesTheLineUnderTestAndNamesEachSharedPurchase()
        {
            ResearcherLineAblation.DescribeLine().Should().Be(
                "Broader Hypotheses (+1 trial) from rank 35, Second Opinion from rank 15, Field Notes +6 from rank 5 then +12 from rank 40");

            var options = new ResearcherPerkStudyOptions { Runs = 2, Seed = 1, Gear = new[] { GearLevel.None(SkillType.Fabrication), Mid } };
            var report = ResearcherLineAblation.Build(options, "test");

            report.Should().Contain("## The whole line, one purchase taken out");
            report.Should().Contain("Whole line without Broader Hypotheses");
            report.Should().Contain("Styles with Efficient Analysis");
            report.Should().Contain("## Candidate rebalances of the line");
            report.Should().Contain("## Grid: where Broader Hypotheses sits and how large Field Notes is");
            report.Should().MatchRegex("^[\x20-\x7E\r\n]*$", "a report is ASCII");
            ResearcherLineAblation.SharedPerks.Should().NotContain(new[] { PerkType.Methodical, PerkType.Intuitive }).And.HaveCount(7);
        }

        /// <summary>A source of chance whose every roll is the same.</summary>
        private sealed class ScriptedRandom : IMinigameRandom
        {
            private readonly int _roll;

            public ScriptedRandom(int roll)
            {
                _roll = roll;
            }

            public int D100() => _roll;

            public int Next(int minInclusive, int maxExclusive) => minInclusive;
        }
    }
}
