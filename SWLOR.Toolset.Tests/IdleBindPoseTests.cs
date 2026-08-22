using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// An animation node only carries the channels it animates. Everything else has to come from the
    /// skeleton's authored bind pose.
    /// </summary>
    /// <remarks>
    /// NWN's <c>a_ba</c> idle rotates the body's bones but never moves them, so every bone in it has
    /// an orientation track and no position track. Reading the missing position off the animation
    /// stub yields &lt;0,0,0&gt;, which put every bone of the skeleton at the origin: an armor preview
    /// drew as a single blob with the shoulders inside the chest, and it did it to creature previews
    /// too. The composer was never at fault - it attached every part to the right bone, and then the
    /// pose folded the bones together.
    /// </remarks>
    [TestFixture]
    public class IdleBindPoseTests
    {
        [Test]
        public void AnUntrackedChannelKeepsTheSkeletonsAuthoredValue()
        {
            var shoulder = new MdlNode { Name = "shoulder_g", Position = new Vector3(0.2f, 0f, 1.5f), Scale = 1f };
            var root = new MdlNode { Name = "rootdummy", Scale = 1f };
            root.Children.Add(shoulder);
            shoulder.Parent = root;
            var model = new MdlModel { GeometryRoot = root };

            // The animation's stub for that bone: an orientation track, no position track, and - as
            // real animation stubs do - a blank Position of its own.
            var animatedShoulder = new MdlNode
            {
                Name = "shoulder_g",
                Position = Vector3.Zero,
                Scale = 1f,
                OrientationTimes = [0f],
                OrientationValues = [Quaternion.Identity],
            };
            var animationRoot = new MdlNode { Name = "rootdummy", Scale = 1f };
            animationRoot.Children.Add(animatedShoulder);
            var animation = new MdlAnimation { Name = "pause1", GeometryRoot = animationRoot };

            var posed = MdlAnimationPose.Sample(animation, 0f, MdlAnimationPose.BindPose(model));

            posed.Should().ContainKey("shoulder_g");
            posed["shoulder_g"].Position.Should().Be(new Vector3(0.2f, 0f, 1.5f),
                "an untracked position comes from the skeleton, not from the animation stub's blank");
        }

        [Test]
        public void WithoutABindPoseTheStubsOwnValueIsStillUsed()
        {
            // The no-bind-pose overload has to keep behaving as before for callers that pose a model
            // against its own animations rather than a supermodel's.
            var node = new MdlNode { Name = "bone", Position = new Vector3(1f, 2f, 3f), Scale = 1f };
            var root = new MdlNode { Name = "rootdummy", Scale = 1f };
            root.Children.Add(node);
            node.ScaleTimes = [0f];
            node.ScaleValues = [1f];

            var posed = MdlAnimationPose.Sample(
                new MdlAnimation { Name = "pause1", GeometryRoot = root }, 0f);

            posed["bone"].Position.Should().Be(new Vector3(1f, 2f, 3f));
        }
    }
}
