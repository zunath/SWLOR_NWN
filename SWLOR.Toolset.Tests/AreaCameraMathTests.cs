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
        public void ComputeModelFraming_CentresOnTheModelAndScalesWithIt()
        {
            var (target, distance) = AreaCameraMath.ComputeModelFraming(
                new Vector3(-0.05f, -0.2f, 0f), new Vector3(0.05f, 0.9f, 0.1f),
                verticalFovRadians: MathF.PI / 4f, aspectRatio: 1f);

            target.Should().Be(new Vector3(0f, 0.35f, 0.05f), "the camera looks at the model's own centre");

            // A sword is roughly a metre of geometry, so the camera belongs about a metre away -
            // not the ~18m a one-tile area footprint used to produce.
            distance.Should().BeLessThan(3f);

            var (_, bigger) = AreaCameraMath.ComputeModelFraming(
                new Vector3(-1f, -1f, 0f), new Vector3(1f, 1f, 2f),
                verticalFovRadians: MathF.PI / 4f, aspectRatio: 1f);
            bigger.Should().BeGreaterThan(distance, "a mannequin needs more room than a sword");
        }

        [Test]
        public void ComputeSceneFraming_AimsAtWhereThePreviewInstanceActuallyStands()
        {
            // A preview scene parks its one instance at the centre of its nominal tile, so framing
            // the model's LOCAL bounds pointed the camera metres away from the geometry and the
            // preview box rendered empty - a 1.9m mannequin drawn at (5,5,0) with the camera
            // looking at (0,0,0.95).
            var model = new RenderModel
            {
                Meshes = new[]
                {
                    new RenderMesh
                    {
                        NodeName = "body",
                        TextureName = string.Empty,
                        Positions = new[] { -0.3f, -0.15f, 0f, 0.3f, 0.15f, 1.9f },
                        Normals = new[] { 0f, 0f, 1f, 0f, 0f, 1f },
                        TexCoords = new[] { 0f, 0f, 1f, 1f },
                        Indices = new[] { 0, 1, 0 },
                        Transform = Matrix4x4.Identity
                    }
                }
            };
            var scene = new AreaScene
            {
                Tileset = string.Empty,
                Width = 1,
                Height = 1,
                Tiles = Array.Empty<TilePlacement>(),
                Instances = new[]
                {
                    new InstanceMarker
                    {
                        Kind = InstanceMarkerKind.Item,
                        TemplateResRef = "armor",
                        Tag = "armor",
                        Position = new Vector3(5f, 5f, 0f),
                        Orientation = new Vector2(1f, 0f),
                        Model = model
                    }
                },
                Diagnostics = new AreaSceneDiagnostics()
            };

            var (target, distance) = AreaCameraMath.ComputeSceneFraming(
                scene, tileSize: 10f, verticalFovRadians: MathF.PI / 4f, aspectRatio: 1.4f);

            target.X.Should().BeApproximately(5f, 0.01f, "the model is drawn at the instance position");
            target.Y.Should().BeApproximately(5f, 0.01f);
            target.Z.Should().BeApproximately(0.95f, 0.01f, "and the camera looks at its middle height");
            distance.Should().BeLessThan(6f, "a mannequin is framed close, not from across the tile");
        }

        [Test]
        public void ComputeSceneFraming_FallsBackToTheGridForARealArea()
        {
            var scene = new AreaScene
            {
                Tileset = "tcn01",
                Width = 8,
                Height = 8,
                // No single instance carrying geometry, so this is an ordinary area rather than a
                // model preview - which is the only distinction the framing choice turns on.
                Tiles = Array.Empty<TilePlacement>(),
                Instances = Array.Empty<InstanceMarker>(),
                Diagnostics = new AreaSceneDiagnostics()
            };

            var (target, _) = AreaCameraMath.ComputeSceneFraming(
                scene, tileSize: 10f, verticalFovRadians: MathF.PI / 4f, aspectRatio: 1f);

            target.Should().Be(new Vector3(40f, 40f, 0f), "an area still frames its whole footprint");
        }

        [Test]
        public void ComputeModelFraming_NarrowViewportPullsBackSoNothingIsClipped()
        {
            var (_, square) = AreaCameraMath.ComputeModelFraming(
                new Vector3(-1f), new Vector3(1f), MathF.PI / 4f, aspectRatio: 1f);
            var (_, narrow) = AreaCameraMath.ComputeModelFraming(
                new Vector3(-1f), new Vector3(1f), MathF.PI / 4f, aspectRatio: 0.4f);

            narrow.Should().BeGreaterThan(square, "the horizontal axis is the tight one when the box is narrow");
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

        /// <summary>
        /// Vertical panning travels forward across the ground, never up into the air.
        /// </summary>
        /// <remarks>
        /// Raising the camera shifts the view vertically too, so the two look alike for a moment - but
        /// altitude changes how much of the map is in shot and eventually flies the camera off the
        /// scene, whereas a builder panning up means "show me further on". This test exists because
        /// the pan really did move along world +Z until it was corrected.
        /// </remarks>
        [Test]
        public void PanDelta_VerticalDragTravelsForwardOnTheGround_NotUpwards()
        {
            var delta = AreaCameraMath.PanDelta(azimuthRadians: 0.9f, dxPixels: 0f, dyPixels: 10f, worldPerPixel: 2f);

            delta.Z.Should().Be(0f, because: "panning must never change the camera's altitude");

            // At azimuth 0.9 the eye sits along (cos, sin) from the target, so the camera looks along
            // (-cos, -sin) - and a positive dy carries it forward along exactly that.
            var forward = new Vector3(-MathF.Cos(0.9f), -MathF.Sin(0.9f), 0f);
            delta.X.Should().BeApproximately(forward.X * 20f, 0.0001f);
            delta.Y.Should().BeApproximately(forward.Y * 20f, 0.0001f);
        }

        /// <summary>The two pan axes stay independent: neither leaks into the other.</summary>
        [Test]
        public void PanDelta_HorizontalAndVerticalAxesArePerpendicular()
        {
            var across = AreaCameraMath.PanDelta(0.9f, dxPixels: 10f, dyPixels: 0f, worldPerPixel: 1f);
            var along = AreaCameraMath.PanDelta(0.9f, dxPixels: 0f, dyPixels: 10f, worldPerPixel: 1f);

            Vector3.Dot(across, along).Should().BeApproximately(0f, 0.0001f);
        }

        [Test]
        public void ScreenPanDelta_FrontPreview_VerticalDragMovesVertically()
        {
            var delta = AreaCameraMath.ScreenPanDelta(
                azimuthRadians: MathF.PI * 1.5f,
                elevationRadians: 0f,
                dxPixels: 0f,
                dyPixels: 25f,
                worldPerPixel: 0.01f);

            delta.X.Should().BeApproximately(0f, 0.0001f);
            delta.Y.Should().BeApproximately(0f, 0.0001f);
            delta.Z.Should().BeApproximately(0.25f, 0.0001f);
        }

        [Test]
        public void ScreenPanDelta_AtElevation_UsesPerpendicularScreenAxes()
        {
            var across = AreaCameraMath.ScreenPanDelta(
                azimuthRadians: 0.9f,
                elevationRadians: 0.55f,
                dxPixels: 10f,
                dyPixels: 0f,
                worldPerPixel: 1f);
            var vertical = AreaCameraMath.ScreenPanDelta(
                azimuthRadians: 0.9f,
                elevationRadians: 0.55f,
                dxPixels: 0f,
                dyPixels: 10f,
                worldPerPixel: 1f);

            across.Length().Should().BeApproximately(10f, 0.0001f);
            vertical.Length().Should().BeApproximately(10f, 0.0001f);
            Vector3.Dot(across, vertical).Should().BeApproximately(0f, 0.0001f);
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

        [Test]
        public void CreateProjection_ModelPreviewUsesOrthographicLensAtEquivalentFramingScale()
        {
            const float distance = 10f;
            const float verticalFov = MathF.PI / 4f;
            const float aspect = 1.5f;
            var projection = AreaCameraMath.CreateProjection(
                isSingleModelPreview: true,
                distance,
                verticalFov,
                aspect,
                nearPlane: 0.5f,
                farPlane: 100f);

            projection.M34.Should().Be(0f, "orthographic projection does not divide X/Y by depth");
            projection.M44.Should().Be(1f);

            var equivalentHalfHeight = distance * MathF.Tan(verticalFov / 2f);
            var topAtTargetScale = Vector4.Transform(
                new Vector4(0f, equivalentHalfHeight, -distance, 1f),
                projection);
            (topAtTargetScale.Y / topAtTargetScale.W).Should().BeApproximately(
                1f,
                0.0001f,
                "switching lenses must not change the existing preview framing or zoom scale");
        }

        [Test]
        public void CreateProjection_AreaViewRetainsPerspectiveLens()
        {
            var projection = AreaCameraMath.CreateProjection(
                isSingleModelPreview: false,
                distance: 10f,
                verticalFovRadians: MathF.PI / 4f,
                aspectRatio: 1.5f,
                nearPlane: 0.5f,
                farPlane: 100f);

            projection.M34.Should().Be(-1f);
            projection.M44.Should().Be(0f);
        }

        // ----- WP5.1: ScreenPointToRay (picking) -----

        private static (Matrix4x4 View, Matrix4x4 Projection) BuildTestCamera()
        {
            var eye = new Vector3(20f, -10f, 15f);
            var target = new Vector3(20f, 20f, 0f);
            var view = Matrix4x4.CreateLookAt(eye, target, Vector3.UnitZ);
            var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4f, 16f / 9f, 0.1f, 500f);
            return (view, projection);
        }

        /// <summary>Projects a world point to a logical screen coordinate the same way the GL pipeline (and any mouse pointer) would, for round-tripping through ScreenPointToRay.</summary>
        private static Vector2 ProjectToScreen(Vector3 worldPoint, Matrix4x4 view, Matrix4x4 projection, int width, int height)
        {
            var clip = Vector4.Transform(new Vector4(worldPoint, 1f), view * projection);
            clip /= clip.W;

            var screenX = (clip.X + 1f) / 2f * width;
            var screenY = (1f - clip.Y) / 2f * height;
            return new Vector2(screenX, screenY);
        }

        private static float DistanceFromRay(PickRay ray, Vector3 point)
        {
            var toPoint = point - ray.Origin;
            var alongRay = Vector3.Dot(toPoint, ray.Direction);
            var closestOnRay = ray.Origin + ray.Direction * alongRay;
            return Vector3.Distance(closestOnRay, point);
        }

        [Test]
        public void ScreenPointToRay_ProjectedWorldPoint_LiesOnResultingRay()
        {
            var (view, projection) = BuildTestCamera();
            var worldPoint = new Vector3(22f, 18f, 3f);
            const int width = 1280;
            const int height = 720;

            var screenPoint = ProjectToScreen(worldPoint, view, projection, width, height);
            var ray = AreaCameraMath.ScreenPointToRay(screenPoint, width, height, view, projection);

            DistanceFromRay(ray, worldPoint).Should().BeLessThan(0.01f);
        }

        [Test]
        public void ScreenPointToRay_OrthographicModelPreview_LiesOnProjectedWorldPoint()
        {
            const float distance = 30f;
            const int width = 900;
            const int height = 600;
            var target = new Vector3(20f, 20f, 3f);
            var eye = target + new Vector3(0f, distance, 0f);
            var view = Matrix4x4.CreateLookAt(eye, target, Vector3.UnitZ);
            var projection = AreaCameraMath.CreateProjection(
                isSingleModelPreview: true,
                distance,
                verticalFovRadians: MathF.PI / 4f,
                aspectRatio: (float)width / height,
                nearPlane: 0.1f,
                farPlane: 500f);
            var worldPoint = new Vector3(22f, 18f, 4f);

            var screenPoint = ProjectToScreen(worldPoint, view, projection, width, height);
            var ray = AreaCameraMath.ScreenPointToRay(
                screenPoint,
                width,
                height,
                view,
                projection);

            DistanceFromRay(ray, worldPoint).Should().BeLessThan(0.01f,
                "model picking and drag controls use the same projection as the rendered preview");
        }

        [Test]
        public void ScreenPointToRay_ScreenCenter_ProducesRayPassingThroughOrbitTarget()
        {
            var (view, projection) = BuildTestCamera();
            const int width = 1000;
            const int height = 800;

            // The orbit target used to build the view matrix always projects to the screen center.
            var target = new Vector3(20f, 20f, 0f);
            var ray = AreaCameraMath.ScreenPointToRay(new Vector2(width / 2f, height / 2f), width, height, view, projection);

            DistanceFromRay(ray, target).Should().BeLessThan(0.01f);
        }

        [Test]
        public void ScreenPointToRay_DirectionIsNormalized()
        {
            var (view, projection) = BuildTestCamera();
            var ray = AreaCameraMath.ScreenPointToRay(new Vector2(640f, 200f), 1280, 720, view, projection);

            ray.Direction.Length().Should().BeApproximately(1f, 0.0001f);
        }

        [Test]
        public void ScreenPointToRay_ZeroSizedViewport_ReturnsDegenerateRayWithoutThrowing()
        {
            var (view, projection) = BuildTestCamera();

            Action act = () => AreaCameraMath.ScreenPointToRay(new Vector2(10f, 10f), 0, 0, view, projection);

            act.Should().NotThrow();
            var ray = AreaCameraMath.ScreenPointToRay(new Vector2(10f, 10f), 0, 0, view, projection);
            float.IsNaN(ray.Direction.X).Should().BeFalse();
        }
    }
}
