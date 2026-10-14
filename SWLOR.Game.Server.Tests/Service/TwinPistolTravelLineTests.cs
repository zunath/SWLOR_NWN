using NUnit.Framework;
using SWLOR.Game.Server.Service;
using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Tests.Service
{
    [TestFixture]
    public class TwinPistolTravelLineTests
    {
        [TestCase(3, 2, 0, true)]
        [TestCase(3, -2, 0, true)]
        [TestCase(3, 2.01f, 0, false)]
        [TestCase(-.01f, 0, 0, false)]
        [TestCase(6.01f, 0, 0, false)]
        [TestCase(6, 0, 0, true)]
        [TestCase(3, 0, 3, false)]
        public void RushUsesTheActualTravelSegmentAndFourMeterTotalWidth(float x, float y, float z, bool expected)
        {
            Assert.That(TwinPistolAbility.IsInTravelLine(new Vector3(x, y, z), Vector3.Zero,
                new Vector3(6, 0, 0), 4), Is.EqualTo(expected));
        }

        [Test]
        public void EmptyOrInvalidTravelCannotCreateAnAttackArea()
        {
            Assert.That(TwinPistolAbility.IsInTravelLine(Vector3.Zero, Vector3.Zero, Vector3.Zero, 4), Is.False);
            Assert.That(TwinPistolAbility.IsInTravelLine(Vector3.Zero, Vector3.Zero, new Vector3(6, 0, 0), 0), Is.False);
            Assert.That(TwinPistolAbility.IsInTravelLine(new Vector3(float.NaN, 0, 0), Vector3.Zero,
                new Vector3(6, 0, 0), 4), Is.False);
        }
    }
}
