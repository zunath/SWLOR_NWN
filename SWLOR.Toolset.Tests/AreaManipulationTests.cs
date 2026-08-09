using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Coverage for the WP5.2 <see cref="AreaManipulation"/> move/rotate gizmo math:
    /// ray/horizontal-plane intersection (the move gizmo tracks the drag on the plane through the
    /// instance's current Z; place-from-palette tracks it on the Z=0 ground plane) and grid-snap
    /// rounding. No GL/UI/document dependency - GlAreaControl drives its live drag preview with
    /// this, and AreaEditorViewModel commits the final values through InstanceFieldMap only once
    /// the drag releases.
    /// </summary>
    public class AreaManipulationTests
    {
        // ----- IntersectRayWithHorizontalPlane -----

        [Test]
        public void IntersectRayWithHorizontalPlane_StraightDownRay_HitsExpectedPoint()
        {
            var ray = new PickRay(new Vector3(3f, 4f, 10f), new Vector3(0f, 0f, -1f));

            var hit = AreaManipulation.IntersectRayWithHorizontalPlane(ray, 2f);

            hit.Should().NotBeNull();
            hit!.Value.Should().Be(new Vector3(3f, 4f, 2f));
        }

        [Test]
        public void IntersectRayWithHorizontalPlane_AngledRay_HitsExpectedPoint()
        {
            // Direction (2,0,-1) normalized: descends 1 unit in Z for every 2 units of +X.
            // Starting at the origin and intersecting the plane 4 units below must travel 8 units in X.
            var ray = new PickRay(Vector3.Zero, Vector3.Normalize(new Vector3(2f, 0f, -1f)));

            var hit = AreaManipulation.IntersectRayWithHorizontalPlane(ray, -4f);

            hit.Should().NotBeNull();
            hit!.Value.X.Should().BeApproximately(8f, 0.0001f);
            hit.Value.Y.Should().BeApproximately(0f, 0.0001f);
            hit.Value.Z.Should().BeApproximately(-4f, 0.0001f);
        }

        [Test]
        public void IntersectRayWithHorizontalPlane_ParallelToPlane_ReturnsNull()
        {
            var ray = new PickRay(new Vector3(0f, 0f, 5f), new Vector3(1f, 0f, 0f));

            AreaManipulation.IntersectRayWithHorizontalPlane(ray, 0f).Should().BeNull();
        }

        [Test]
        public void IntersectRayWithHorizontalPlane_PlaneBehindRayOrigin_ReturnsNull()
        {
            // The ray descends away from a plane that sits above its origin - never reaches it.
            var ray = new PickRay(new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, -1f));

            AreaManipulation.IntersectRayWithHorizontalPlane(ray, 5f).Should().BeNull();
        }

        [Test]
        public void IntersectRayWithHorizontalPlane_RayAlreadyOnPlane_ReturnsOrigin()
        {
            var ray = new PickRay(new Vector3(1f, 2f, 0f), new Vector3(0f, 0f, -1f));

            var hit = AreaManipulation.IntersectRayWithHorizontalPlane(ray, 0f);

            hit.Should().Be(new Vector3(1f, 2f, 0f));
        }

        // ----- SnapToGridXy -----

        [TestCase(0.3f, 0.3f, 0.5f, 0.5f)]
        [TestCase(0.7f, 0.7f, 0.5f, 0.5f)]
        [TestCase(-0.3f, -0.3f, -0.5f, -0.5f)]
        [TestCase(1.24f, -1.26f, 1.0f, -1.5f)]
        public void SnapToGridXy_RoundsXyToNearestCell(float x, float y, float expectedX, float expectedY)
        {
            var snapped = AreaManipulation.SnapToGridXy(new Vector3(x, y, 7f), 0.5f);

            snapped.X.Should().BeApproximately(expectedX, 0.0001f);
            snapped.Y.Should().BeApproximately(expectedY, 0.0001f);
        }

        [Test]
        public void SnapToGridXy_NeverChangesZ()
        {
            var snapped = AreaManipulation.SnapToGridXy(new Vector3(1.23f, -4.56f, 7.891f), 0.5f);

            snapped.Z.Should().Be(7.891f);
        }

        [Test]
        public void SnapToGridXy_ZeroCellSize_ReturnsPositionUnchanged()
        {
            var position = new Vector3(1.23f, -4.56f, 7f);

            AreaManipulation.SnapToGridXy(position, 0f).Should().Be(position);
        }

        [Test]
        public void SnapToGridXy_NegativeCellSize_ReturnsPositionUnchanged()
        {
            var position = new Vector3(1.23f, -4.56f, 7f);

            AreaManipulation.SnapToGridXy(position, -1f).Should().Be(position);
        }

        // ----- HeadingToOrientation -----

        [Test]
        public void HeadingToOrientation_Zero_ReturnsUnitX()
        {
            AreaManipulation.HeadingToOrientation(0f).Should().Be(new Vector2(1f, 0f));
        }

        [Test]
        public void HeadingToOrientation_QuarterTurn_ReturnsUnitY()
        {
            var orientation = AreaManipulation.HeadingToOrientation(1.5707964f); // pi/2

            orientation.X.Should().BeApproximately(0f, 0.0001f);
            orientation.Y.Should().BeApproximately(1f, 0.0001f);
        }

        [Test]
        public void HeadingToOrientation_HalfTurn_ReturnsNegativeUnitX()
        {
            var orientation = AreaManipulation.HeadingToOrientation(3.1415927f); // pi

            orientation.X.Should().BeApproximately(-1f, 0.0001f);
            orientation.Y.Should().BeApproximately(0f, 0.0001f);
        }

        [Test]
        public void HeadingToOrientation_RoundTripsThroughAtan2()
        {
            const float heading = 0.7f;
            var orientation = AreaManipulation.HeadingToOrientation(heading);

            MathF.Atan2(orientation.Y, orientation.X).Should().BeApproximately(heading, 0.0001f);
        }

        [Test]
        public void ManipulationPreviewPreservesPaletteAndCustomTintState()
        {
            var palette = new Dictionary<int, int> { [4] = 73 };
            var tints = new Dictionary<string, int> { ["TM_pmh0_chest189_4"] = 123456 };
            var source = new InstanceMarker
            {
                Kind = InstanceMarkerKind.Creature,
                Position = Vector3.Zero,
                Orientation = Vector2.UnitX,
                LayerColorIndices = palette,
                TintMapOverrides = tints
            };
            var clonePreview = typeof(SWLOR.Toolset.Viewport.GlAreaControl).GetMethod(
                "ClonePreview",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

            var preview = (InstanceMarker)clonePreview.Invoke(
                null,
                [source, Vector3.One, Vector2.UnitY])!;

            preview.LayerColorIndices.Should().BeSameAs(palette);
            preview.TintMapOverrides.Should().BeSameAs(tints);
        }
    }
}
