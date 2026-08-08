using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Coverage for the WP5.1 <see cref="AreaPicking"/> instance hit-testing: ray/AABB and
    /// ray-triangle math against hand-built meshes, closest-of-several selection, and the
    /// marker-vs-model display rule (a placeable in marker mode must pick by its marker box even
    /// when it carries a resolved model, mirroring GlAreaControl.DrawsAsModel).
    /// </summary>
    public class AreaPickingTests
    {
        private static InstanceMarker MakeMarkerInstance(
            InstanceMarkerKind kind, Vector3 position, string tag, RenderModel? model = null,
            Matrix4x4? visualTransform = null, bool isDoorTransition = false) => new()
        {
            Kind = kind,
            TemplateResRef = tag,
            Tag = tag,
            Position = position,
            Orientation = new Vector2(1f, 0f),
            VisualTransform = visualTransform ?? Matrix4x4.Identity,
            Model = model,
            IsDoorTransition = isDoorTransition
        };

        private static RenderMesh MakeMesh(float[] positions, int[] indices) => new()
        {
            NodeName = "test_mesh",
            TextureName = string.Empty,
            Positions = positions,
            Normals = Array.Empty<float>(),
            TexCoords = Array.Empty<float>(),
            Indices = indices,
            Transform = Matrix4x4.Identity
        };

        /// <summary>A flat 10x10 quad in the local XY plane centered on the origin (X,Y in [-5,5], Z=0), as two triangles.</summary>
        private static RenderModel MakeFlatQuadModel() => new()
        {
            Name = "quad",
            Meshes = new[]
            {
                MakeMesh(
                    positions: new float[]
                    {
                        -5f, -5f, 0f,
                         5f, -5f, 0f,
                         5f,  5f, 0f,
                        -5f,  5f, 0f
                    },
                    indices: new[] { 0, 1, 2, 0, 2, 3 })
            }
        };

        /// <summary>A single right triangle in the local XY plane: (0,0,0), (10,0,0), (0,10,0) - its AABB is [0,10]x[0,10]x[0,0], but the triangle itself only covers the region x+y &lt;= 10.</summary>
        private static RenderModel MakeRightTriangleModel() => new()
        {
            Name = "right_triangle",
            Meshes = new[]
            {
                MakeMesh(
                    positions: new float[] { 0f, 0f, 0f, 10f, 0f, 0f, 0f, 10f, 0f },
                    indices: new[] { 0, 1, 2 })
            }
        };

        private static AreaScene MakeScene(params InstanceMarker[] instances) => new()
        {
            Tileset = "test",
            Width = 1,
            Height = 1,
            Tiles = Array.Empty<TilePlacement>(),
            Instances = instances,
            Diagnostics = new AreaSceneDiagnostics()
        };

        private static PickRay DownwardRayAt(float x, float y, float startZ = 100f) =>
            new(new Vector3(x, y, startZ), new Vector3(0f, 0f, -1f));

        // ----- Marker (AABB-only) hit-testing -----

        [Test]
        public void PickClosestInstance_RayThroughMarkerBox_Hits()
        {
            var instance = MakeMarkerInstance(InstanceMarkerKind.Creature, Vector3.Zero, "npc");
            var scene = MakeScene(instance);

            var hit = AreaPicking.PickClosestInstance(DownwardRayAt(0f, 0f), scene, showPlaceableModels: true);

            hit.Should().BeSameAs(instance);
        }

        [Test]
        public void PickClosestInstance_RayOutsideMarkerBox_Misses()
        {
            // Marker half-width is 0.4 - a ray 2m off to the side should clear the box entirely.
            var instance = MakeMarkerInstance(InstanceMarkerKind.Creature, Vector3.Zero, "npc");
            var scene = MakeScene(instance);

            var hit = AreaPicking.PickClosestInstance(DownwardRayAt(2f, 0f), scene, showPlaceableModels: true);

            hit.Should().BeNull();
        }

        [Test]
        public void PickClosestInstance_ClosestOfSeveralMarkers_ReturnsNearestToRayOrigin()
        {
            // Both markers sit under the same (x=0,y=0) column; the ray descends from above, so the
            // one with the higher Z (closer to the ray's start) must win.
            var near = MakeMarkerInstance(InstanceMarkerKind.Creature, new Vector3(0f, 0f, 0f), "near");
            var far = MakeMarkerInstance(InstanceMarkerKind.Creature, new Vector3(0f, 0f, -5f), "far");
            var scene = MakeScene(far, near); // deliberately out of distance order

            var hit = AreaPicking.PickClosestInstance(DownwardRayAt(0f, 0f), scene, showPlaceableModels: true);

            hit.Should().BeSameAs(near);
        }

        // ----- Model (AABB + triangle) hit-testing -----

        [Test]
        public void PickClosestInstance_RayThroughModelAabbAndInsideTriangle_Hits()
        {
            // (2,2) is well within both the triangle's AABB [0,10]x[0,10] and the triangle itself (x+y=4 <= 10).
            var instance = MakeMarkerInstance(InstanceMarkerKind.Placeable, Vector3.Zero, "prop", MakeRightTriangleModel());
            var scene = MakeScene(instance);

            var hit = AreaPicking.PickClosestInstance(DownwardRayAt(2f, 2f), scene, showPlaceableModels: true);

            hit.Should().BeSameAs(instance);
        }

        [Test]
        public void PickClosestInstance_RayInsideModelAabbButOutsideTriangle_Misses()
        {
            // (9,9) is inside the triangle's AABB [0,10]x[0,10] but outside the triangle itself
            // (x+y=18 > 10) - an AABB overlap alone must not count as a hit.
            var instance = MakeMarkerInstance(InstanceMarkerKind.Placeable, Vector3.Zero, "prop", MakeRightTriangleModel());
            var scene = MakeScene(instance);

            var hit = AreaPicking.PickClosestInstance(DownwardRayAt(9f, 9f), scene, showPlaceableModels: true);

            hit.Should().BeNull();
        }

        [Test]
        public void PickClosestInstance_RayOutsideModelAabbEntirely_MissesWithoutTriangleTest()
        {
            var instance = MakeMarkerInstance(InstanceMarkerKind.Placeable, Vector3.Zero, "prop", MakeRightTriangleModel());
            var scene = MakeScene(instance);

            var hit = AreaPicking.PickClosestInstance(DownwardRayAt(100f, 100f), scene, showPlaceableModels: true);

            hit.Should().BeNull();
        }

        [Test]
        public void PickClosestInstance_ClosestOfSeveralModels_ReturnsNearestToRayOrigin()
        {
            var nearer = MakeMarkerInstance(InstanceMarkerKind.Placeable, new Vector3(0f, 0f, 5f), "nearer", MakeFlatQuadModel());
            var farther = MakeMarkerInstance(InstanceMarkerKind.Placeable, new Vector3(0f, 0f, 0f), "farther", MakeFlatQuadModel());
            var scene = MakeScene(farther, nearer);

            var hit = AreaPicking.PickClosestInstance(DownwardRayAt(1f, 1f), scene, showPlaceableModels: true);

            hit.Should().BeSameAs(nearer);
        }

        // ----- Marker-vs-model display rule -----

        [Test]
        public void PickClosestInstance_ShowPlaceableModelsTrue_PicksThroughModelBeyondMarkerBox()
        {
            // The quad model spans local X/Y in [-5,5] - far larger than the 0.4-half-width marker
            // box - so a ray at x=2 (outside the marker box, inside the model) only hits when the
            // instance is drawn/picked as its model.
            var instance = MakeMarkerInstance(InstanceMarkerKind.Placeable, Vector3.Zero, "prop", MakeFlatQuadModel());
            var scene = MakeScene(instance);

            var hit = AreaPicking.PickClosestInstance(DownwardRayAt(2f, 0f), scene, showPlaceableModels: true);

            hit.Should().BeSameAs(instance, "showPlaceableModels=true draws/picks placeables as their model");
        }

        [Test]
        public void PickClosestInstance_ShowPlaceableModelsFalse_PlaceableWithModelPicksByMarkerBoxOnly()
        {
            // Same setup as above, but with placeable models toggled off: DrawsAsModel becomes
            // false for this Placeable regardless of its resolved Model, so the ray at x=2 (well
            // outside the marker's 0.4 half-width, even though it's inside the model mesh) must miss.
            var instance = MakeMarkerInstance(InstanceMarkerKind.Placeable, Vector3.Zero, "prop", MakeFlatQuadModel());
            var scene = MakeScene(instance);

            var hit = AreaPicking.PickClosestInstance(DownwardRayAt(2f, 0f), scene, showPlaceableModels: false);

            hit.Should().BeNull("with placeable models hidden, picking must use the marker box, not the model mesh");
        }

        [Test]
        public void PickClosestInstance_ShowPlaceableModelsFalse_StillHitsWithinMarkerBox()
        {
            var instance = MakeMarkerInstance(InstanceMarkerKind.Placeable, Vector3.Zero, "prop", MakeFlatQuadModel());
            var scene = MakeScene(instance);

            var hit = AreaPicking.PickClosestInstance(DownwardRayAt(0f, 0f), scene, showPlaceableModels: false);

            hit.Should().BeSameAs(instance);
        }

        [Test]
        public void PickClosestInstance_NonPlaceableKindWithModel_AlwaysDrawnAsModelRegardlessOfToggle()
        {
            // The showPlaceableModels toggle only affects Placeable kinds (per GlAreaControl.DrawsAsModel) -
            // a Door with a resolved model must still pick through its model mesh either way.
            var instance = MakeMarkerInstance(InstanceMarkerKind.Door, Vector3.Zero, "door", MakeFlatQuadModel());
            var scene = MakeScene(instance);

            var hit = AreaPicking.PickClosestInstance(DownwardRayAt(2f, 0f), scene, showPlaceableModels: false);

            hit.Should().BeSameAs(instance);
        }

        // ----- Bounds helpers used by GlAreaControl's selection highlight -----

        [Test]
        public void ComputeMarkerWorldBounds_IsCenteredOnInstancePosition()
        {
            var instance = MakeMarkerInstance(InstanceMarkerKind.Waypoint, new Vector3(10f, 20f, 3f), "wp");

            var (min, max) = AreaPicking.ComputeMarkerWorldBounds(instance);

            min.X.Should().BeApproximately(9.6f, 0.0001f);
            max.X.Should().BeApproximately(10.4f, 0.0001f);
            min.Z.Should().BeApproximately(3.05f, 0.0001f);
            max.Z.Should().BeApproximately(4.25f, 0.0001f);
        }

        [Test]
        public void ComputeModelWorldBounds_TranslatesWithInstancePosition()
        {
            var instance = MakeMarkerInstance(InstanceMarkerKind.Placeable, new Vector3(100f, 0f, 0f), "prop", MakeFlatQuadModel());

            var bounds = AreaPicking.ComputeModelWorldBounds(instance);

            bounds.Should().NotBeNull();
            bounds!.Value.Min.X.Should().BeApproximately(95f, 0.0001f);
            bounds.Value.Max.X.Should().BeApproximately(105f, 0.0001f);
        }

        [Test]
        public void VisualTransform_AffectsModelBoundsAndPicking()
        {
            var visual = Matrix4x4.CreateScale(0.5f) * Matrix4x4.CreateTranslation(10f, 0f, 4f);
            var instance = MakeMarkerInstance(
                InstanceMarkerKind.Placeable,
                new Vector3(100f, 0f, 0f),
                "transformed",
                MakeRightTriangleModel(),
                visual);

            var bounds = AreaPicking.ComputeModelWorldBounds(instance);
            var hit = AreaPicking.PickInstance(
                DownwardRayAt(111f, 1f), instance, drawsAsModel: true);

            bounds.Should().NotBeNull();
            bounds!.Value.Min.X.Should().BeApproximately(110f, 0.0001f);
            bounds.Value.Max.X.Should().BeApproximately(115f, 0.0001f);
            bounds.Value.Min.Z.Should().BeApproximately(4f, 0.0001f);
            hit.Should().NotBeNull("picking must use the same visual transform as rendering");
        }

        [Test]
        public void ComputeModelWorldBounds_NoModel_ReturnsNull()
        {
            var instance = MakeMarkerInstance(InstanceMarkerKind.Creature, Vector3.Zero, "npc");

            AreaPicking.ComputeModelWorldBounds(instance).Should().BeNull();
        }

        [Test]
        public void ComputeInstanceWorldBounds_DrawsAsModelTrue_UsesModelBounds()
        {
            var instance = MakeMarkerInstance(InstanceMarkerKind.Placeable, Vector3.Zero, "prop", MakeFlatQuadModel());

            var (min, max) = AreaPicking.ComputeInstanceWorldBounds(instance, drawsAsModel: true);

            // The quad spans [-5,5] locally - far wider than the 0.8-wide marker box.
            (max.X - min.X).Should().BeApproximately(10f, 0.0001f);
        }

        [Test]
        public void ComputeInstanceWorldBounds_DrawsAsModelFalse_UsesMarkerBounds()
        {
            var instance = MakeMarkerInstance(InstanceMarkerKind.Placeable, Vector3.Zero, "prop", MakeFlatQuadModel());

            var (min, max) = AreaPicking.ComputeInstanceWorldBounds(instance, drawsAsModel: false);

            (max.X - min.X).Should().BeApproximately(0.8f, 0.0001f);
        }

        [Test]
        public void DoorTransitionWithoutResolvedGeometry_UsesTheVisibleDoorwayPlaneForBoundsAndPicking()
        {
            var instance = MakeMarkerInstance(
                InstanceMarkerKind.Door,
                new Vector3(10f, 20f, 4f),
                "transition",
                isDoorTransition: true);

            var (min, max) = AreaPicking.ComputeInstanceWorldBounds(instance, drawsAsModel: false);
            var hit = AreaPicking.PickClosestInstance(
                DownwardRayAt(10f, 20f), MakeScene(instance), showPlaceableModels: true);

            (max.X - min.X).Should().BeApproximately(2f, 0.0001f);
            (max.Z - min.Z).Should().BeApproximately(3f, 0.0001f);
            hit.Should().BeSameAs(instance,
                "the fallback transition plane must be selectable instead of an invisible marker");
            AreaPicking.DrawsAsModel(instance, showPlaceableModels: true).Should().BeFalse(
                "transition geometry has its own translucent render pass");
        }

        // ----- PickInstance (WP5.2: single-instance hit-test for the move/rotate gizmo's press check) -----

        [Test]
        public void PickInstance_DrawsAsModelFalse_UsesMarkerBounds()
        {
            var instance = MakeMarkerInstance(InstanceMarkerKind.Placeable, Vector3.Zero, "prop", MakeFlatQuadModel());

            AreaPicking.PickInstance(DownwardRayAt(2f, 0f), instance, drawsAsModel: false).Should().BeNull(
                "with drawsAsModel=false the gizmo's press check must use the marker box, not the model mesh");
            AreaPicking.PickInstance(DownwardRayAt(0f, 0f), instance, drawsAsModel: false).Should().NotBeNull();
        }

        [Test]
        public void PickInstance_DrawsAsModelTrue_UsesModelTriangles()
        {
            var instance = MakeMarkerInstance(InstanceMarkerKind.Placeable, Vector3.Zero, "prop", MakeRightTriangleModel());

            AreaPicking.PickInstance(DownwardRayAt(2f, 2f), instance, drawsAsModel: true).Should().NotBeNull(
                "(2,2) is inside both the triangle's AABB and the triangle itself");
            AreaPicking.PickInstance(DownwardRayAt(9f, 9f), instance, drawsAsModel: true).Should().BeNull(
                "(9,9) is inside the triangle's AABB but outside the triangle itself");
        }

        [Test]
        public void PickInstance_RayMisses_ReturnsNull()
        {
            var instance = MakeMarkerInstance(InstanceMarkerKind.Creature, Vector3.Zero, "npc");

            AreaPicking.PickInstance(DownwardRayAt(100f, 100f), instance, drawsAsModel: false).Should().BeNull();
        }

        // ----- DrawsAsModel rule itself -----

        [TestCase(InstanceMarkerKind.Placeable, false, false)]
        [TestCase(InstanceMarkerKind.Placeable, true, true)]
        [TestCase(InstanceMarkerKind.Door, false, true)]
        [TestCase(InstanceMarkerKind.Door, true, true)]
        public void DrawsAsModel_MatchesGlAreaControlRule(InstanceMarkerKind kind, bool showPlaceableModels, bool expected)
        {
            var instance = MakeMarkerInstance(kind, Vector3.Zero, "x", MakeFlatQuadModel());

            AreaPicking.DrawsAsModel(instance, showPlaceableModels).Should().Be(expected);
        }

        [Test]
        public void DrawsAsModel_NoModel_AlwaysFalse()
        {
            var instance = MakeMarkerInstance(InstanceMarkerKind.Door, Vector3.Zero, "door");

            AreaPicking.DrawsAsModel(instance, showPlaceableModels: true).Should().BeFalse();
        }
    }
}
