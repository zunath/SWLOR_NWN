using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Rng;

namespace SWLOR.CraftSim.Tests.Rng
{
    [TestFixture]
    public class SimMinigameRandomTests
    {
        [Test]
        public void ASeedAndStreamRepeatTheSameRolls()
        {
            var first = SimMinigameRandom.ForRun(7, 1, 2, 3);
            var second = SimMinigameRandom.ForRun(7, 1, 2, 3);

            Enumerable.Range(0, 50).Select(_ => first.D100()).Should().Equal(Enumerable.Range(0, 50).Select(_ => second.D100()));
        }

        [Test]
        public void DifferentStreamsOfOneSeedDoNotShareDice()
        {
            var first = SimMinigameRandom.ForRun(7, 1, 2, 3);
            var second = SimMinigameRandom.ForRun(7, 1, 2, 4);

            Enumerable.Range(0, 50).Select(_ => first.D100()).Should().NotEqual(Enumerable.Range(0, 50).Select(_ => second.D100()));
        }

        [Test]
        public void D100StaysFromOneToOneHundredAndNextInsideItsRange()
        {
            var random = SimMinigameRandom.ForRun(3, 9);

            for (var i = 0; i < 2000; i++)
            {
                random.D100().Should().BeInRange(1, 100);
                random.Next(-3, 4).Should().BeInRange(-3, 3);
                random.Next(5, 6).Should().Be(5);
            }
        }

        [Test]
        public void NextRejectsAnEmptyRange()
        {
            var act = () => SimMinigameRandom.ForRun(1).Next(4, 4);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void CallsCountEveryRoll()
        {
            var random = SimMinigameRandom.ForRun(1);

            random.D100();
            random.Next(0, 10);
            random.D100();

            random.Calls.Should().Be(3);
        }
    }
}
