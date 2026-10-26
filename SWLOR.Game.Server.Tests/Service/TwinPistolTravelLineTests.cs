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

        [TestCase(9f)]
        [TestCase(20f)]
        [TestCase(6.01f)]
        public void RushClickPastReachDashesTheFullDistance(float clicked)
        {
            Assert.That(TwinPistolAbility.RushEndpointLengths(clicked, 2f, 6f).First(), Is.EqualTo(6f).Within(.001f));
        }

        [Test]
        public void RushClickWithinReachKeepsItsDistance()
        {
            Assert.That(TwinPistolAbility.RushEndpointLengths(4.2f, 2f, 6f).First(), Is.EqualTo(4.2f).Within(.001f));
        }

        [Test]
        public void RushClickShortOfTheMinimumIsNotLengthened()
        {
            Assert.That(TwinPistolAbility.RushEndpointLengths(1f, 2f, 6f), Is.EqualTo(new[] { 1f }),
                "movement validation still refuses a dash shorter than two meters");
        }

        [Test]
        public void RushEndpointOnACreatureShiftsFartherThenNearerWithinLimits()
        {
            var lengths = TwinPistolAbility.RushEndpointLengths(4f, 2f, 6f).ToArray();
            Assert.That(lengths.Take(5), Is.EqualTo(new[] { 4f, 4.25f, 3.75f, 4.5f, 3.5f }).Within(.001f));
            Assert.That(lengths, Is.All.InRange(2f, 6f));
            var atReach = TwinPistolAbility.RushEndpointLengths(12f, 2f, 6f).ToArray();
            Assert.That(atReach.Take(3), Is.EqualTo(new[] { 6f, 5.75f, 5.5f }).Within(.001f),
                "a clamped endpoint can only shift nearer");
            Assert.That(atReach.Min(), Is.EqualTo(4f).Within(.001f), "the search stays within two meters of the click");
        }

        [Test]
        public void RushHasNoEndpointWithoutADistanceOrValidLimits()
        {
            Assert.That(TwinPistolAbility.RushEndpointLengths(0f, 2f, 6f), Is.Empty);
            Assert.That(TwinPistolAbility.RushEndpointLengths(float.NaN, 2f, 6f), Is.Empty);
            Assert.That(TwinPistolAbility.RushEndpointLengths(4f, 6f, 2f), Is.Empty);
        }

        [Test]
        public void AiRushHasNoEndpointWhenTheTargetIsOutOfReach()
        {
            Assert.That(TwinPistolAbility.RushEndpointsPast(Vector2.Zero, new Vector2(7, 0), 2f, 6f, 4f), Is.Empty);
            Assert.That(TwinPistolAbility.RushEndpointsPast(Vector2.Zero, Vector2.Zero, 2f, 6f, 4f), Is.Empty);
        }

        [Test]
        public void RetreatTriesStraightBackFirstThenShorterThenAngledLanes()
        {
            var origin = new Vector2(10, 10);
            var endpoints = TwinPistolAbility.RetreatEndpoints(origin, new Vector2(-2, 0), 2f, 4f).ToList();
            Assert.That(endpoints, Has.Count.EqualTo(15), "five lanes, each at 4, 3 and 2.5 meters");
            var expected = new (float Degrees, float Length)[]
            {
                (0, 4), (0, 3), (0, 2.5f), (30, 4), (30, 3), (30, 2.5f), (-30, 4), (-30, 3), (-30, 2.5f),
                (60, 4), (60, 3), (60, 2.5f), (-60, 4), (-60, 3), (-60, 2.5f),
            };
            for (var index = 0; index < expected.Length; index++)
            {
                var offset = endpoints[index] - origin;
                Assert.That(offset.Length(), Is.EqualTo(expected[index].Length).Within(.001f), $"endpoint {index} length");
                var degrees = MathF.Atan2(offset.Y, -offset.X) * 180f / MathF.PI;
                Assert.That(Math.Abs(degrees), Is.EqualTo(Math.Abs(expected[index].Degrees)).Within(.01f), $"endpoint {index} angle");
                Assert.That(Vector2.Dot(offset, new Vector2(-1, 0)), Is.GreaterThan(0), "every lane moves away from the target");
            }
            Assert.That(endpoints[3].Y, Is.Not.EqualTo(endpoints[6].Y).Within(.001f), "angled lanes try both sides");
        }

        [Test]
        public void RetreatShortestEndpointKeepsArrivalHeadroomAboveTheMinimum()
        {
            var lengths = TwinPistolAbility.RetreatEndpoints(Vector2.Zero, new Vector2(1, 0), 2f, 4.2f)
                .Take(3).Select(endpoint => endpoint.Length()).ToArray();
            Assert.That(lengths, Is.EqualTo(new[] { 4.2f, 3.2f, 2.5f }).Within(.001f),
                "an early native arrival still travels the two meters slide rewards require");
            var narrow = TwinPistolAbility.RetreatEndpoints(Vector2.Zero, new Vector2(1, 0), 2f, 2.2f)
                .Take(2).Select(endpoint => endpoint.Length()).ToArray();
            Assert.That(narrow[0], Is.EqualTo(2.2f).Within(.001f), "the shortest endpoint never exceeds the maximum");
            Assert.That(narrow[1], Is.EqualTo(2.2f).Within(.001f), "the next lane starts at the same capped distance");
        }

        [Test]
        public void RetreatHasNoEndpointWithoutADirectionOrValidLimits()
        {
            Assert.That(TwinPistolAbility.RetreatEndpoints(Vector2.Zero, Vector2.Zero, 2f, 4f), Is.Empty);
            Assert.That(TwinPistolAbility.RetreatEndpoints(Vector2.Zero, new Vector2(1, 0), 5f, 4f), Is.Empty);
        }
    }
}
