using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Rng;

namespace SWLOR.CraftSim.Tests.Rng
{
    [TestFixture]
    public class SimRandomTests
    {
        [Test]
        public void ASeedRepeatsTheSameSequence()
        {
            var first = new SimRandom(7);
            var second = new SimRandom(7);

            Enumerable.Range(0, 50).Select(_ => first.D100()).Should().Equal(Enumerable.Range(0, 50).Select(_ => second.D100()));
        }

        [Test]
        public void SequenceIsPinnedSoAReportRepeatsOnAnyRuntime()
        {
            var random = new SimRandom(1);

            Enumerable.Range(0, 5).Select(_ => random.NextUInt64()).Should().Equal(
                13757245211066428519UL, 17911839290282890590UL, 8196980753821780235UL, 8195237237126968761UL, 14072917602864530048UL);
        }

        [Test]
        public void DifferentSeedsGiveDifferentSequences()
        {
            var first = new SimRandom(1);
            var second = new SimRandom(2);

            Enumerable.Range(0, 20).Select(_ => first.D100()).Should().NotEqual(Enumerable.Range(0, 20).Select(_ => second.D100()));
        }

        [TestCase(3)]
        [TestCase(4)]
        [TestCase(20)]
        [TestCase(100)]
        public void Dice_StayInRangeAndReachBothEnds(int sides)
        {
            var random = new SimRandom(5);
            var rolls = Enumerable.Range(0, 20_000).Select(_ => random.Roll(sides)).ToList();

            rolls.Min().Should().Be(1);
            rolls.Max().Should().Be(sides);
        }

        [Test]
        public void ADieIsUniform()
        {
            var random = new SimRandom(11);
            var counts = new int[4];
            const int Rolls = 100_000;
            for (var roll = 0; roll < Rolls; roll++)
                counts[random.D4() - 1]++;

            foreach (var count in counts)
                count.Should().BeInRange((int)(Rolls / 4.0 * 0.97), (int)(Rolls / 4.0 * 1.03));
        }

        [Test]
        public void ADieNeedsASide()
        {
            var act = () => new SimRandom(1).Roll(0);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void ForRun_GivesEachStreamItsOwnSequence()
        {
            var first = SimRandom.ForRun(1, 2, 3);
            var same = SimRandom.ForRun(1, 2, 3);
            var other = SimRandom.ForRun(1, 2, 4);
            var otherSeed = SimRandom.ForRun(2, 2, 3);

            var firstRolls = Enumerable.Range(0, 10).Select(_ => first.NextUInt64()).ToList();
            firstRolls.Should().Equal(Enumerable.Range(0, 10).Select(_ => same.NextUInt64()));
            firstRolls.Should().NotEqual(Enumerable.Range(0, 10).Select(_ => other.NextUInt64()));
            firstRolls.Should().NotEqual(Enumerable.Range(0, 10).Select(_ => otherSeed.NextUInt64()));
        }

        [Test]
        public void NextDouble_IsInTheUnitInterval()
        {
            var random = new SimRandom(3);

            Enumerable.Range(0, 1000).Select(_ => random.NextDouble()).Should().OnlyContain(v => v >= 0 && v < 1);
        }
    }
}
