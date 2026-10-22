using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Fishing;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.FishingService;

namespace SWLOR.CraftSim.Tests.Fishing
{
    [TestFixture]
    public class RuleOfThumbPolicyTests
    {
        private static string Choose(FishMove move, int cap = 20)
        {
            var setup = new LineSetup(cap, 6, 16, 30, 16, 4, LineMovePattern.For(FishRarity.Common, 20));
            var engine = new LineEngine(new FixedRandom(), setup, move);
            return new RuleOfThumbPolicy().Choose(engine);
        }

        [Test]
        public void ATire_IsAFreeReel_AndAThrash_IsForHold()
        {
            Choose(FishMove.Tire).Should().Be(LineActionIds.Reel);
            Choose(FishMove.Thrash).Should().Be(LineActionIds.Hold);
        }

        [Test]
        public void AgainstARunOrADive_ItReelsWhileTheLineHasRoomAndGivesLineWhenItHasNot()
        {
            Choose(FishMove.Run).Should().Be(LineActionIds.Reel, "a Reel against a Run adds 10 of the 20 Tension, leaving half");
            Choose(FishMove.Run, cap: 12).Should().Be(LineActionIds.GiveLine, "the same Reel would leave under a fifth of a 12 Tension line");
            Choose(FishMove.Dive, cap: 9).Should().Be(LineActionIds.GiveLine, "a Reel against a Dive adds 8 Tension");
            Choose(FishMove.Dive).Should().Be(LineActionIds.Reel);
        }

        [Test]
        public void AReelThatLandsTheFish_IsAlwaysTaken()
        {
            var setup = new LineSetup(10, 6, 6, 16, 16, 4, LineMovePattern.For(FishRarity.Common, 20));
            var engine = new LineEngine(new FixedRandom(), setup, FishMove.Run);

            new RuleOfThumbPolicy().Choose(engine).Should().Be(LineActionIds.Reel);
        }

        [Test]
        public void ItFishesOnlyWithTheLine()
        {
            var act = () => new RuleOfThumbPolicy().Choose(new NotTheLine());

            act.Should().Throw<ArgumentException>();
        }

        private sealed class FixedRandom : IMinigameRandom
        {
            public int D100() => 1;

            public int Next(int minInclusive, int maxExclusive) => minInclusive;
        }

        private sealed class NotTheLine : ICraftMinigameEngine
        {
            public CraftMinigameType Type => CraftMinigameType.Forge;
            public MinigameStatus Status => MinigameStatus.InProgress;
            public MinigameMeters Meters => MinigameMeters.Start(1, 1, 1, 1);
            public int Turn => 0;
            public MinigameOpportunity NextOpportunity => null;
            public IReadOnlyList<MinigameActionInfo> Actions => Array.Empty<MinigameActionInfo>();
            public IReadOnlyList<MinigameMeterView> DescribeMeters() => Array.Empty<MinigameMeterView>();
            public MinigameActionPreview Preview(string actionId) => throw new NotSupportedException();
            public IReadOnlyList<MinigameActionPreview> PreviewAll() => Array.Empty<MinigameActionPreview>();
            public MinigameActionResult Perform(string actionId) => throw new NotSupportedException();
            public ICraftMinigameEngine Fork(IMinigameRandom random) => this;
        }
    }
}
