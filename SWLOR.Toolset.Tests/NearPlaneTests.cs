using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The near clip plane, which is what decides whether a decal can be told from the surface it
    /// sits on. A depth buffer spends almost all of its precision between the near plane and a few
    /// multiples of it, so a near plane fixed at 0.1m leaves a painting hung on a wall fighting the
    /// wall for the same depth values - visible as flicker whenever the camera moves.
    /// </summary>
    public class NearPlaneTests
    {
        [Test]
        public void ItScalesWithTheOrbitDistance()
        {
            // Constant ratio: the depth precision at what is being looked at does not change with zoom.
            (AreaCameraMath.NearPlaneFor(200f) / AreaCameraMath.NearPlaneFor(20f))
                .Should().BeApproximately(10f, 0.001f);
        }

        [Test]
        public void AtATypicalInteriorFramingItIsFarLooserThanTheOldFixedPlane()
        {
            AreaCameraMath.NearPlaneFor(20f).Should().BeApproximately(1f, 0.001f);
            AreaCameraMath.NearPlaneFor(20f).Should().BeGreaterThan(0.1f * 5f,
                "the fixed 0.1m plane is what left the paintings flickering against the wall");
        }

        /// <summary>
        /// Never nearer than a tenth of the closest the camera can be brought, so raising it can
        /// never clip geometry the builder is looking at.
        /// </summary>
        [TestCase(0f)]
        [TestCase(0.5f)]
        [TestCase(AreaCameraMath.MinDistance)]
        public void ItNeverExceedsAFractionOfTheClosestApproach(float distance)
        {
            AreaCameraMath.NearPlaneFor(distance)
                .Should().BeLessThanOrEqualTo(AreaCameraMath.MinDistance / 10f + 0.001f);
        }

        [Test]
        public void ItIsAlwaysPositive()
        {
            AreaCameraMath.NearPlaneFor(0f).Should().BeGreaterThan(0f);
            AreaCameraMath.NearPlaneFor(-50f).Should().BeGreaterThan(0f);
        }
    }
}
