using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The transform gizmo's handles have to be grabbable on their own geometry.
    /// </summary>
    /// <remarks>
    /// Both arms and ring are drawn well outside most objects' bounds, so a press was only ever
    /// tested against the object body: the handles were visible but inert, and the press fell through
    /// to camera panning instead.
    /// </remarks>
    [TestFixture]
    public class GizmoPickingTests
    {
        private const float ArmLength = 2.2f;
        private const float RingRadius = 1.8f;
        private const float Tolerance = 0.3f;

        private static readonly Vector3 Origin = new(10f, 20f, 0f);

        /// <summary>A ray straight down from high above <paramref name="target"/>.</summary>
        private static PickRay FromAbove(Vector3 target) =>
            new(new Vector3(target.X, target.Y, target.Z + 50f), new Vector3(0, 0, -1));

        private static GizmoHandle Pick(PickRay ray) =>
            GizmoPicking.Pick(ray, Origin, ArmLength, RingRadius, Tolerance);

        [Test]
        public void A_Press_On_The_X_Arm_Grabs_An_Axis()
        {
            Pick(FromAbove(Origin + new Vector3(ArmLength * 0.6f, 0, 0)))
                .Should().Be(GizmoHandle.Axis);
        }

        [Test]
        public void A_Press_On_The_Y_Arm_Grabs_An_Axis()
        {
            Pick(FromAbove(Origin + new Vector3(0, ArmLength * 0.6f, 0)))
                .Should().Be(GizmoHandle.Axis);
        }

        /// <summary>
        /// The Z arm points straight up, so a top-down ray runs along it rather than across it. Tested
        /// from the side, which is the angle the orbit camera actually presents it at.
        /// </summary>
        [Test]
        public void A_Press_On_The_Z_Arm_Grabs_An_Axis()
        {
            var target = Origin + new Vector3(0, 0, ArmLength * 0.6f);
            var ray = new PickRay(target + new Vector3(50f, 0, 0), new Vector3(-1, 0, 0));

            Pick(ray).Should().Be(GizmoHandle.Axis);
        }

        [Test]
        public void A_Press_On_The_Ring_Grabs_The_Ring()
        {
            var onRing = Origin + new Vector3(
                MathF.Cos(MathF.PI / 3f) * RingRadius,
                MathF.Sin(MathF.PI / 3f) * RingRadius,
                GizmoPicking.RingGroundOffset);

            Pick(FromAbove(onRing)).Should().Be(GizmoHandle.Ring);
        }

        /// <summary>
        /// Empty space between the ring and the arms must stay a miss, or the gizmo would swallow
        /// presses meant for the camera or for another object.
        /// </summary>
        [Test]
        public void A_Press_In_Open_Space_Grabs_Nothing()
        {
            Pick(FromAbove(Origin + new Vector3(RingRadius * 3f, RingRadius * 3f, 0)))
                .Should().Be(GizmoHandle.None);
        }

        /// <summary>Past the end of an arm is a miss - the arm is a segment, not an infinite line.</summary>
        [Test]
        public void A_Press_Beyond_The_End_Of_An_Arm_Grabs_Nothing()
        {
            Pick(FromAbove(Origin + new Vector3(ArmLength * 3f, 0, 0)))
                .Should().Be(GizmoHandle.None);
        }

        /// <summary>
        /// A ray pointing away from the gizmo must miss. The camera can sit anywhere, and a ray-line
        /// distance that ignored direction would report a hit behind the viewer.
        /// </summary>
        [Test]
        public void A_Ray_Pointing_Away_Grabs_Nothing()
        {
            var ray = new PickRay(Origin + new Vector3(0, 0, 50f), new Vector3(0, 0, 1));

            Pick(ray).Should().Be(GizmoHandle.None);
        }

        /// <summary>
        /// Tolerance is what the caller scales with camera distance, so it has to actually widen the
        /// grab - at a wide zoom an arm covers only a pixel or two.
        /// </summary>
        [Test]
        public void A_Wider_Tolerance_Grabs_A_Near_Miss()
        {
            // Near the inner end of the X arm, where the ring is a long way off and cannot win the
            // comparison instead.
            var nearMiss = FromAbove(Origin + new Vector3(0.3f, 0.5f, 0));

            GizmoPicking.Pick(nearMiss, Origin, ArmLength, RingRadius, 0.2f)
                .Should().Be(GizmoHandle.None);
            GizmoPicking.Pick(nearMiss, Origin, ArmLength, RingRadius, 0.8f)
                .Should().Be(GizmoHandle.Axis);
        }
    }
}
