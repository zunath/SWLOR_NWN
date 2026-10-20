using System;
using System.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using Vector2 = System.Numerics.Vector2;
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

        [TestCase(2.5f)]
        [TestCase(3.5f)]
        [TestCase(4.5f)]
        [TestCase(5.2f)]
        public void AiRushEndpointsPassBesideTheTargetAndStrikeIt(float distance)
        {
            var origin = new Vector2(10, 10);
            var target = origin + Vector2.Normalize(new Vector2(3, 1)) * distance;
            var endpoints = TwinPistolAbility.RushEndpointsPast(origin, target, 2f, 6f, 4f).ToList();
            Assert.That(endpoints, Is.Not.Empty, "a target within reach yields a dash past it");
            foreach (var endpoint in endpoints)
            {
                var length = Vector2.Distance(origin, endpoint);
                Assert.That(length, Is.InRange(2f, 6f), "the dash stays within its movement limits");
                Assert.That(TwinPistolAbility.IsInTravelLine(new Vector3(target, 0), new Vector3(origin, 0),
                    new Vector3(endpoint, 0), 4f), Is.True, "the target is inside the four-meter travel line");
                var segment = endpoint - origin;
                var along = Vector2.Dot(target - origin, segment) / segment.LengthSquared();
                var closest = origin + segment * Math.Clamp(along, 0f, 1f);
                Assert.That(Vector2.Distance(closest, target), Is.GreaterThanOrEqualTo(1.19f),
                    "the path clears the target's body instead of running into it");
            }
        }

        [Test]
        public void AiRushHasNoEndpointWhenTheTargetIsOutOfReach()
        {
            Assert.That(TwinPistolAbility.RushEndpointsPast(Vector2.Zero, new Vector2(7, 0), 2f, 6f, 4f), Is.Empty);
            Assert.That(TwinPistolAbility.RushEndpointsPast(Vector2.Zero, Vector2.Zero, 2f, 6f, 4f), Is.Empty);
        }
    }
}
