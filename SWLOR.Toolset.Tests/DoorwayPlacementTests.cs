using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The two rules that decide where a door may hang: it goes in a doorway the cursor actually
    /// reaches, and only in one that is still empty.
    /// </summary>
    /// <remarks>
    /// Both replaced looser behaviour that shipped. The snap was unbounded, so the ghost teleported to
    /// the nearest doorway anywhere in the area and a click landed somewhere the builder was not
    /// pointing at; and nothing stopped a second door being hung in a doorway that already had one,
    /// which the engine draws as two leaves z-fighting inside one frame.
    /// </remarks>
    [TestFixture]
    public class DoorwayPlacementTests
    {
        private static TileDoorAnchor Anchor(float x, float y, int doorIndex = 0, int tileIndex = 0) =>
            new()
            {
                TileIndex = tileIndex,
                DoorIndex = doorIndex,
                Type = 0,
                Position = new Vector3(x, y, 0),
                Orientation = new Vector2(1, 0)
            };

        private static InstanceMarker Door(float x, float y, float z = 0) =>
            new()
            {
                Kind = InstanceMarkerKind.Door,
                Tag = "door",
                TemplateResRef = "dt_door",
                Position = new Vector3(x, y, z),
                Orientation = new Vector2(1, 0)
            };

        private static AreaScene Scene(TileDoorAnchor[] anchors, params InstanceMarker[] instances) =>
            new()
            {
                Tileset = "tde01",
                Width = 4,
                Height = 4,
                Tiles = Array.Empty<TilePlacement>(),
                Instances = instances,
                DoorAnchors = anchors,
                Diagnostics = new AreaSceneDiagnostics()
            };

        [Test]
        public void ADoorStandingInADoorwayFillsIt()
        {
            var anchor = Anchor(10, 10);

            Scene(new[] { anchor }, Door(10, 10)).IsDoorwayFilled(anchor).Should().BeTrue();
        }

        /// <summary>
        /// A door elsewhere on the same tile leaves the doorway empty - the corpus has doors standing on
        /// doorway tiles without being in the doorway, and blanking the slot for those would lock
        /// builders out of doorways that are genuinely free.
        /// </summary>
        [Test]
        public void ADoorAwayFromTheDoorwayDoesNotFillIt()
        {
            var anchor = Anchor(10, 10);

            Scene(new[] { anchor }, Door(12, 10)).IsDoorwayFilled(anchor).Should().BeFalse();
        }

        /// <summary>Only doors fill doorways: a placeable parked in one does not.</summary>
        [Test]
        public void ANonDoorInstanceDoesNotFillADoorway()
        {
            var anchor = Anchor(10, 10);
            var crate = new InstanceMarker
            {
                Kind = InstanceMarkerKind.Placeable,
                Position = new Vector3(10, 10, 0),
                Orientation = new Vector2(1, 0)
            };

            Scene(new[] { anchor }, crate).IsDoorwayFilled(anchor).Should().BeFalse();
        }

        /// <summary>
        /// The door being dragged does not fill the doorway it is being dragged out of, or nudging one
        /// inside its own frame would find that frame occupied by itself and jump it somewhere else.
        /// </summary>
        [Test]
        public void TheDoorBeingMovedDoesNotFillItsOwnDoorway()
        {
            var anchor = Anchor(10, 10);
            var door = Door(10, 10);
            var scene = Scene(new[] { anchor }, door);

            scene.IsDoorwayFilled(anchor, ignore: door).Should().BeFalse();
            scene.NearestEmptyDoorway(new Vector3(10.5f, 10, 0), ignore: door).Should().BeSameAs(anchor);
        }

        [Test]
        public void ADoorwayWithinReachIsOffered()
        {
            var near = Anchor(10, 10);
            var far = Anchor(40, 40, doorIndex: 1);

            Scene(new[] { near, far }).NearestEmptyDoorway(new Vector3(11, 10, 0)).Should().BeSameAs(near);
        }

        [Test]
        public void ADoorwayOutOfReachIsNotOffered()
        {
            var scene = Scene(new[] { Anchor(10, 10) });
            var justTooFar = new Vector3(10 + AreaScene.DoorSnapRadius + 0.5f, 10, 0);

            scene.NearestEmptyDoorway(justTooFar).Should().BeNull();
        }

        /// <summary>
        /// The filled doorway is nearer, but a second leaf cannot go in it - the empty one further off is
        /// the answer, as long as it is itself in reach.
        /// </summary>
        [Test]
        public void AFilledDoorwayIsPassedOverForAnEmptyOneStillInReach()
        {
            var filled = Anchor(10, 10);
            var empty = Anchor(13, 10, doorIndex: 1);
            var scene = Scene(new[] { filled, empty }, Door(10, 10));

            scene.NearestEmptyDoorway(new Vector3(11, 10, 0)).Should().BeSameAs(empty);
        }

        [Test]
        public void EveryDoorwayFilledMeansNowhereToPlace()
        {
            var scene = Scene(new[] { Anchor(10, 10) }, Door(10, 10));

            scene.NearestEmptyDoorway(new Vector3(10, 10, 0)).Should().BeNull();
            scene.HasEmptyDoorway().Should().BeFalse();
        }

        [Test]
        public void AnAreaWithAnEmptyDoorwayHasSomewhereToPlace()
        {
            var scene = Scene(new[] { Anchor(10, 10), Anchor(20, 10, doorIndex: 1) }, Door(10, 10));

            scene.HasEmptyDoorway().Should().BeTrue();
        }

        [Test]
        public void AnAreaWithNoDoorwaysHasNowhereToPlace()
        {
            Scene(Array.Empty<TileDoorAnchor>()).HasEmptyDoorway().Should().BeFalse();
        }

        /// <summary>
        /// Storeys count here as they do for the snap: doorways in a multi-storey area stack almost
        /// exactly above one another, and the door hanging in the one upstairs leaves the one below it
        /// empty.
        /// </summary>
        [Test]
        public void ADoorAStoreyUpDoesNotFillTheDoorwayBelowIt()
        {
            var anchor = Anchor(10, 10);

            Scene(new[] { anchor }, Door(10, 10, z: 10)).IsDoorwayFilled(anchor).Should().BeFalse();
        }
    }
}
