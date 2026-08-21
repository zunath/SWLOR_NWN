using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Updating one instance's transform without rebuilding the scene around it.
    /// </summary>
    /// <remarks>
    /// Moving or rotating an object used to reserialise both documents, reparse them and reassemble
    /// every tile and instance - once per repeat tick of a held rotate button, with a "Building
    /// scene..." banner flashing over the viewport throughout. These cover the cheap path that
    /// replaced it, including the two things it has to get right: a trigger's world-space polygon
    /// travels with a move, and the tile list is carried forward by reference so the renderer can
    /// tell the grid did not change.
    /// </remarks>
    [TestFixture]
    public class AreaSceneInstanceUpdateTests
    {
        private static InstanceMarker Marker(
            InstanceMarkerKind kind = InstanceMarkerKind.Placeable,
            Vector3? position = null,
            IReadOnlyList<Vector3>? geometry = null) =>
            new()
            {
                Kind = kind,
                Tag = "crate",
                TemplateResRef = "plc_crate",
                Position = position ?? new Vector3(10, 20, 0),
                Orientation = new Vector2(1, 0),
                Geometry = geometry
            };

        private static AreaScene SceneWith(params InstanceMarker[] instances) =>
            new()
            {
                Tileset = "tde01",
                Width = 4,
                Height = 4,
                Tiles = new[]
                {
                    new TilePlacement
                    {
                        TileIndex = 0, Column = 0, Row = 0, TileId = 1, Orientation = 0,
                        HeightLevel = 0, CenterX = 5, CenterY = 5, HeightOffset = 0,
                        Transform = Matrix4x4.Identity, IsFallback = false
                    }
                },
                Instances = instances,
                Diagnostics = new AreaSceneDiagnostics(),
                IsInteriorTileset = true
            };

        /// <summary>
        /// Doors belong in a tile's doorway. The placement ghost always snapped; moving an already
        /// placed door did not, so it could be dragged off its tile frame and walkmesh opening. Both
        /// paths now share this one rule.
        /// </summary>
        [Test]
        public void TheNearestDoorwayIsChosenOnTheFloorPlane()
        {
            var scene = new AreaScene
            {
                Tileset = "tde01", Width = 4, Height = 4,
                Tiles = Array.Empty<TilePlacement>(),
                Instances = Array.Empty<InstanceMarker>(),
                Diagnostics = new AreaSceneDiagnostics(),
                DoorAnchors = new[]
                {
                    new TileDoorAnchor
                    {
                        TileIndex = 0, DoorIndex = 0, Type = 0,
                        Position = new Vector3(5, 5, 0), Orientation = new Vector2(1, 0)
                    },
                    new TileDoorAnchor
                    {
                        TileIndex = 1, DoorIndex = 0, Type = 0,
                        // Far away across the floor, so the ground-floor doorway wins despite the click
                        // being level with this one.
                        Position = new Vector3(60, 5, 40), Orientation = new Vector2(0, 1)
                    }
                }
            };

            scene.NearestEmptyDoorway(new Vector3(7, 5, 0))!.Position.Should().Be(new Vector3(5, 5, 0));
        }

        /// <summary>
        /// Doorways in a multi-storey area stack almost exactly above one another, so ignoring Z meant a
        /// click upstairs took whichever lower-floor anchor was marginally nearer across the floor.
        /// </summary>
        [Test]
        public void AClickUpstairsTakesTheUpstairsDoorway()
        {
            var scene = new AreaScene
            {
                Tileset = "tde01", Width = 4, Height = 4,
                Tiles = Array.Empty<TilePlacement>(),
                Instances = Array.Empty<InstanceMarker>(),
                Diagnostics = new AreaSceneDiagnostics(),
                DoorAnchors = new[]
                {
                    new TileDoorAnchor
                    {
                        TileIndex = 0, DoorIndex = 0, Type = 0,
                        // Marginally nearer across the floor, but a storey down.
                        Position = new Vector3(5, 5, 0), Orientation = new Vector2(1, 0)
                    },
                    new TileDoorAnchor
                    {
                        TileIndex = 1, DoorIndex = 0, Type = 0,
                        Position = new Vector3(7, 5, 10), Orientation = new Vector2(0, 1)
                    }
                }
            };

            scene.NearestEmptyDoorway(new Vector3(6, 5, 10))!.Position.Should().Be(new Vector3(7, 5, 10));
        }

        [Test]
        public void OnTheSameStoreyTheNearerDoorwayStillWins()
        {
            // Height must separate floors without dominating: two doorways on one floor are chosen
            // between by where the builder actually clicked. Both are inside the snap radius, so it is
            // the choice between them that is under test rather than which one is in reach.
            var scene = new AreaScene
            {
                Tileset = "tde01", Width = 4, Height = 4,
                Tiles = Array.Empty<TilePlacement>(),
                Instances = Array.Empty<InstanceMarker>(),
                Diagnostics = new AreaSceneDiagnostics(),
                DoorAnchors = new[]
                {
                    new TileDoorAnchor
                    {
                        TileIndex = 0, DoorIndex = 0, Type = 0,
                        Position = new Vector3(28, 5, 10), Orientation = new Vector2(1, 0)
                    },
                    new TileDoorAnchor
                    {
                        TileIndex = 1, DoorIndex = 0, Type = 0,
                        Position = new Vector3(33, 5, 10), Orientation = new Vector2(0, 1)
                    }
                }
            };

            scene.NearestEmptyDoorway(new Vector3(32, 5, 10))!.Position.Should().Be(new Vector3(33, 5, 10));
        }

        [Test]
        public void AnAreaWithNoDoorwaysOffersNone()
        {
            SceneWith(Marker()).NearestEmptyDoorway(Vector3.Zero).Should().BeNull();
        }

        [Test]
        public void RotatingAnInstanceKeepsEverythingElseAboutIt()
        {
            var marker = Marker();

            var turned = marker.WithTransform(marker.Position, new Vector2(0, 1));

            turned.Orientation.Should().Be(new Vector2(0, 1));
            turned.Position.Should().Be(marker.Position);
            turned.Kind.Should().Be(marker.Kind);
            turned.Tag.Should().Be(marker.Tag);
            turned.TemplateResRef.Should().Be(marker.TemplateResRef);
            turned.VisualTransform.Should().Be(marker.VisualTransform);
        }

        [Test]
        public void MovingATriggerCarriesItsVolumeAlong()
        {
            // Geometry is world-space in the scene but authored as offsets from the instance
            // position, so a trigger left behind by its own polygon would be a real bug.
            var marker = Marker(
                InstanceMarkerKind.Trigger,
                new Vector3(10, 20, 0),
                new[] { new Vector3(10, 20, 0), new Vector3(12, 20, 0), new Vector3(12, 23, 0) });

            var moved = marker.WithTransform(new Vector3(30, 25, 1), marker.Orientation);

            moved.Geometry.Should().Equal(
                new Vector3(30, 25, 1), new Vector3(32, 25, 1), new Vector3(32, 28, 1));
        }

        [Test]
        public void RotatingATriggerLeavesItsVolumeWhereItWas()
        {
            // A trigger's volume is stored unrotated; the engine does not turn it with the
            // instance's heading, so neither does the toolset.
            var geometry = new[] { new Vector3(10, 20, 0), new Vector3(12, 20, 0) };
            var marker = Marker(InstanceMarkerKind.Trigger, new Vector3(10, 20, 0), geometry);

            var turned = marker.WithTransform(marker.Position, new Vector2(0, 1));

            turned.Geometry.Should().Equal(geometry);
        }

        [Test]
        public void AnInstanceWithNoVolumeStaysWithoutOne()
        {
            Marker().WithTransform(new Vector3(1, 2, 3), new Vector2(0, 1)).Geometry.Should().BeNull();
        }

        [Test]
        public void ReplacingAnInstanceLeavesTheRestOfTheSceneAlone()
        {
            var first = Marker();
            var second = Marker(position: new Vector3(40, 40, 0));
            var scene = SceneWith(first, second);

            var updated = scene.WithInstanceReplaced(second, second.WithTransform(new Vector3(50, 40, 0), second.Orientation));

            updated.Should().NotBeNull();
            updated!.Instances.Should().HaveCount(2);
            updated.Instances[0].Should().BeSameAs(first, "an untouched instance is not rebuilt");
            updated.Instances[1].Position.Should().Be(new Vector3(50, 40, 0));
        }

        [Test]
        public void ReplacingAnInstanceCarriesTheTileListByReference()
        {
            // This is what lets the renderer skip re-uploading every tile's walkmesh and regrouping
            // every draw batch. A copied list here would silently undo the whole optimisation.
            var marker = Marker();
            var scene = SceneWith(marker);

            var updated = scene.WithInstanceReplaced(marker, marker.WithTransform(marker.Position, new Vector2(0, 1)));

            updated!.Tiles.Should().BeSameAs(scene.Tiles);
            updated.DoorAnchors.Should().BeSameAs(scene.DoorAnchors);
            updated.Lighting.Should().BeSameAs(scene.Lighting);
            updated.IsInteriorTileset.Should().BeTrue();
        }

        [Test]
        public void AddingAnInstanceLeavesAreaWideSceneInputsAlone()
        {
            var existing = Marker(InstanceMarkerKind.Placeable);
            var added = Marker(InstanceMarkerKind.Waypoint, new Vector3(50, 60, 0));
            var scene = SceneWith(existing);

            var updated = scene.WithInstanceAdded(added);

            updated.Should().NotBeSameAs(scene);
            updated.Instances.Should().Equal(existing, added);
            updated.Tiles.Should().BeSameAs(scene.Tiles);
            updated.DoorAnchors.Should().BeSameAs(scene.DoorAnchors);
            updated.Diagnostics.Should().BeSameAs(scene.Diagnostics);
            updated.Lighting.Should().BeSameAs(scene.Lighting);
            updated.IsInteriorTileset.Should().BeTrue();
            scene.Instances.Should().ContainSingle().Which.Should().BeSameAs(
                existing, "the published scene is immutable");
        }

        [Test]
        public void AddingAnInstanceKeepsTheFullBuilderKindOrder()
        {
            var creature = Marker(InstanceMarkerKind.Creature);
            var placeable = Marker(InstanceMarkerKind.Placeable);
            var waypoint = Marker(InstanceMarkerKind.Waypoint);
            var door = Marker(InstanceMarkerKind.Door);
            var scene = SceneWith(creature, placeable, waypoint);

            var updated = scene.WithInstanceAdded(door);

            updated.Instances.Select(instance => instance.Kind).Should().Equal(
                InstanceMarkerKind.Creature,
                InstanceMarkerKind.Door,
                InstanceMarkerKind.Placeable,
                InstanceMarkerKind.Waypoint);
        }

        [Test]
        public void TheOriginalSceneIsNotMutated()
        {
            var marker = Marker();
            var scene = SceneWith(marker);

            scene.WithInstanceReplaced(marker, marker.WithTransform(new Vector3(99, 99, 0), marker.Orientation));

            scene.Instances[0].Should().BeSameAs(marker, "the published scene must not change under the renderer");
            scene.Instances[0].Position.Should().Be(new Vector3(10, 20, 0));
        }

        [Test]
        public void AnInstanceFromASupersededSceneIsRejected()
        {
            // The caller falls back to a full rebuild on null. Silently doing nothing here would
            // leave the viewport showing the object at its old transform.
            var scene = SceneWith(Marker());

            scene.WithInstanceReplaced(Marker(), Marker()).Should().BeNull();
        }

        [Test]
        public void SceneDimensionsSurviveTheUpdate()
        {
            var marker = Marker();
            var scene = SceneWith(marker);

            var updated = scene.WithInstanceReplaced(marker, marker.WithTransform(marker.Position, new Vector2(0, 1)));

            updated!.Tileset.Should().Be("tde01");
            updated.Width.Should().Be(4);
            updated.Height.Should().Be(4);
        }
    }
}
