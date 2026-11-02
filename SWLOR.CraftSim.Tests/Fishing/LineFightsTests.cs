using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Fishing;
using SWLOR.CraftSim.Play;
using SWLOR.CraftSim.Rng;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.FishingService;

namespace SWLOR.CraftSim.Tests.Fishing
{
    [TestFixture]
    public class LineFightsTests
    {
        private sealed class QuitsImmediately : ICraftPolicy
        {
            public string Name => "Quits";

            public string Choose(ICraftMinigameEngine engine) => null;
        }

        [Test]
        public void EveryFightIsCountedOnce_AndTheSameSeedRepeatsTheSample()
        {
            var setup = LineSetup.ForFish(25, FishRarity.Common, 25, 2);

            var first = LineFights.Sample(setup, LineFights.Adaptive, 30, 7, 3);
            var again = LineFights.Sample(setup, LineFights.Adaptive, 30, 7, 3);

            first.Should().Be(again);
            (first.Landed + first.Snapped + first.Escaped).Should().Be(first.Runs).And.Be(30);
            first.MeanTurns.Should().BeGreaterThan(0);
            first.LandedPercent.Should().BeInRange(0, 100);
        }

        [Test]
        public void AFishFarBelowTheRankIsLandedEveryTime_AndOneFarAboveItAlmostNever()
        {
            var easy = LineFights.Sample(LineSetup.ForFish(0, FishRarity.Common, 50, 4), LineFights.Adaptive, 40, 1, 1);
            var hopeless = LineFights.Sample(LineSetup.ForFish(80, FishRarity.Common, 0, 0), LineFights.Adaptive, 40, 1, 2);

            easy.LandedPercent.Should().Be(100);
            hopeless.LandedPercent.Should().BeLessThan(5);
            hopeless.Snapped.Should().BeGreaterThan(hopeless.Escaped);
        }

        [Test]
        public void APlayerWhoStopsPlaying_HasLostTheFish()
        {
            var setup = LineSetup.ForFish(25, FishRarity.Common, 25, 2);

            var sample = LineFights.Sample(setup, _ => new QuitsImmediately(), 5, 1, 1);

            sample.Escaped.Should().Be(5);
            sample.Landed.Should().Be(0);
            sample.Turns.Should().Be(0);
        }

        [Test]
        public void ThePlayedEngine_IsTheEngineTheGameRuns()
        {
            var setup = LineSetup.ForFish(25, FishRarity.Common, 25, 2);

            var engine = LineFights.Play(setup, LineFights.Adaptive(0), SimMinigameRandom.ForRun(1, SimStreams.Adaptive, 1));

            engine.Type.Should().Be(CraftMinigameType.Line);
            engine.Status.Should().NotBe(MinigameStatus.InProgress);
            engine.Turn.Should().BeInRange(1, LineFights.ActionLimit);
        }

        [Test]
        public void SkilledPlayAtTheAnglersOwnLevel_BeatsTheRuleOfThumb()
        {
            var setup = LineSetup.ForFish(30, FishRarity.Common, 30, 3);

            var skilled = LineFights.Sample(setup, LineFights.Adaptive, 150, 1, 1);
            var casual = LineFights.Sample(setup, LineFights.RuleOfThumb, 150, 1, 1);

            skilled.LandedPercent.Should().BeGreaterThan(casual.LandedPercent);
        }
    }
}
