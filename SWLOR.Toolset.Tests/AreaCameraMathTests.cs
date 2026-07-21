using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Coverage for the WP4.5 <see cref="AreaCameraMath"/> pure orbit-camera helpers: initial
    /// framing from an area's tile-grid bounds, elevation/distance clamping, orbit eye-offset
    /// trigonometric identities, and pan/zoom pixel-to-world conversions.
    /// </summary>
    public class AreaCameraMathTests
    {
        [Test]
        public void ComputeInitialFraming_SquareArea_TargetsGroundPlaneCenter()
        {
            var (target, _) = AreaCameraMath.ComputeInitialFraming(
                areaWidthTiles: 8, areaHeightTiles: 8, tileSize: 10f,
                verticalFovRadians: MathF.PI / 4f, aspectRatio: 1f);

            // 8 tiles * 10m = 80m footprint; center is (40, 40), ground plane (Z=0).
            target.Should().Be(new Vector3(40f, 40f, 0f));
        }

        [Test]
        public void ComputeInitialFraming_LargerArea_ProducesLargerDistance()
        {
            var (_, smallDistance) = AreaCameraMath.ComputeInitialFraming(4, 4, 10f, MathF.PI / 4f, 1f);
            var (_, largeDistance) = AreaCameraMath.ComputeInitialFraming(32, 32, 10f, MathF.PI / 4f, 1f);

            largeDistance.Should().BeGreaterThan(smallDistance);
        }

        [Test]
        public void ComputeInitialFraming_WideAspectRatio_AccountsForHorizontalFit()
        {
            // A very wide (non-square) area needs a horizontal fit, not just vertical, once the
            // viewport aspect ratio is narrow enough that width becomes the binding constraint.
            var (_, distanceNarrowViewport) = AreaCameraMath.ComputeInitialFraming(
                areaWidthTiles: 40, areaHeightTiles: 4, tileSize: 10f,
                verticalFovRadians: MathF.PI / 4f, aspectRatio: 1f);

            var (_, distanceWideViewport) = AreaCameraMath.ComputeInitialFraming(
                areaWidthTiles: 40, areaHeightTiles: 4, tileSize: 10f,
                verticalFovRadians: MathF.PI / 4f, aspectRatio: 3f);

            // A wider viewport (bigger aspect ratio) can fit the same horizontal extent from
            // closer in, so the required distance should shrink (or at least not grow).
            distanceWideViewport.Should().BeLessThanOrEqualTo(distanceNarrowViewport);
        }

        [Test]
        public void ComputeInitialFraming_ZeroSizedArea_NeverProducesDegenerateDistance()
        {
            var (_, distance) = AreaCameraMath.ComputeInitialFraming(0, 0, 10f, MathF.PI / 4f, 1f);

            distance.Should().BeGreaterThanOrEqualTo(AreaCameraMath.MinDistance);
            float.IsNaN(distance).Should().BeFalse();
            float.IsInfinity(distance).Should().BeFalse();
        }

        [TestCase(-10f, AreaCameraMath.MinElevationRadians)]
        [TestCase(10f, AreaCameraMath.MaxElevationRadians)]
        public void ClampElevation_OutOfRange_ClampsToBounds(float input, float expected)
        {
            AreaCameraMath.ClampElevation(input).Should().Be(expected);
        }

        [Test]
        public void ClampElevation_WithinRange_PassesThroughUnchanged()
        {
            AreaCameraMath.ClampElevation(0.7f).Should().Be(0.7f);
        }

        [Test]
        public void ClampDistance_BelowMinimum_ClampsToMinDistance()
        {
            AreaCameraMath.ClampDistance(-5f, initialDistance: 50f).Should().Be(AreaCameraMath.MinDistance);
        }

        [Test]
        public void ClampDistance_AboveMaxMultiplier_ClampsToInitialDistanceTimesMultiplier()
        {
            AreaCameraMath.ClampDistance(100_000f, initialDistance: 50f)
                .Should().Be(50f * AreaCameraMath.MaxDistanceMultiplier);
        }

        [Test]
        public void OrbitEyeOffset_ZeroAzimuthZeroElevation_PointsAlongPositiveX()
        {
            var offset = AreaCameraMath.OrbitEyeOffset(azimuthRadians: 0f, elevationRadians: 0f, distance: 10f);

            offset.X.Should().BeApproximately(10f, 0.0001f);
            offset.Y.Should().BeApproximately(0f, 0.0001f);
            offset.Z.Should().BeApproximately(0f, 0.0001f);
        }

        [Test]
        public void OrbitEyeOffset_StraightUpElevation_PointsAlongPositiveZ()
        {
            var offset = AreaCameraMath.OrbitEyeOffset(azimuthRadians: 0f, elevationRadians: MathF.PI / 2f, distance: 10f);

            offset.X.Should().BeApproximately(0f, 0.001f);
            offset.Y.Should().BeApproximately(0f, 0.001f);
            offset.Z.Should().BeApproximately(10f, 0.001f);
        }

        [Test]
        public void OrbitEyeOffset_AlwaysMatchesRequestedDistance()
        {
            var offset = AreaCameraMath.OrbitEyeOffset(1.234f, 0.456f, distance: 25f);

            offset.Length().Should().BeApproximately(25f, 0.001f);
        }

        [Test]
        public void PanDelta_ZeroAzimuth_RightDragMovesAlongWorldMinusY()
        {
            // At azimuth 0 the eye sits along +X from the target (OrbitEyeOffset), so the view
            // direction is -X; the camera's screen-right axis is cross(forward, up) = cross((-1,0,0),
            // (0,0,1)) = (0,1,0). A rightward drag should slide the scene right under the cursor,
            // which means the target moves in the opposite (-right) world direction: -Y.
            var delta = AreaCameraMath.PanDelta(azimuthRadians: 0f, dxPixels: 10f, dyPixels: 0f, worldPerPixel: 1f);

            delta.X.Should().BeApproximately(0f, 0.0001f);
            delta.Y.Should().BeApproximately(-10f, 0.0001f);
            delta.Z.Should().BeApproximately(0f, 0.0001f);
        }

        [Test]
        public void PanDelta_VerticalDragMovesAlongWorldUp()
        {
            var delta = AreaCameraMath.PanDelta(azimuthRadians: 0.9f, dxPixels: 0f, dyPixels: 10f, worldPerPixel: 2f);

            delta.Should().Be(new Vector3(0f, 0f, 20f));
        }

        [Test]
        public void WorldUnitsPerPixel_ZeroViewportHeight_ReturnsZero()
        {
            AreaCameraMath.WorldUnitsPerPixel(distance: 50f, verticalFovRadians: MathF.PI / 4f, viewportHeightPixels: 0)
                .Should().Be(0f);
        }

        [Test]
        public void WorldUnitsPerPixel_LargerDistance_CoversMoreWorldPerPixel()
        {
            var near = AreaCameraMath.WorldUnitsPerPixel(10f, MathF.PI / 4f, 800);
            var far = AreaCameraMath.WorldUnitsPerPixel(100f, MathF.PI / 4f, 800);

            far.Should().BeGreaterThan(near);
        }
    }
}
