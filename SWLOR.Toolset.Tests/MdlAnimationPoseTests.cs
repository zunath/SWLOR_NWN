using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Sampling an MDL animation into per-node local transforms, so a model stands the way the game
    /// stands it rather than in the bind pose its geometry is stored in.
    /// </summary>
    public class MdlAnimationPoseTests
    {
        private static MdlAnimation Animation(string name, MdlNode root) =>
            new() { Name = name, GeometryRoot = root, Length = 1f };

        private static MdlNode Bone(string name, params (float Time, Vector3 Position)[] keys)
        {
            var node = new MdlNode { Name = name, Position = new Vector3(99f, 99f, 99f) };
            node.PositionTimes = keys.Select(k => k.Time).ToArray();
            node.PositionValues = keys.Select(k => k.Position).ToArray();
            return node;
        }

        private static MdlModel ModelWith(params MdlAnimation[] animations)
        {
            var model = new MdlModel();
            foreach (var animation in animations)
                model.Animations.Add(animation);
            return model;
        }

        [Test]
        public void APlainIdleIsPreferredOverOtherStances()
        {
            var model = ModelWith(
                Animation("walk", new MdlNode()),
                Animation("cpause1", new MdlNode()),
                Animation("pause1", new MdlNode()));

            MdlAnimationPose.FindIdle(model)!.Name.Should().Be("pause1");
        }

        [Test]
        public void AnyPauseWillDoWhenThereIsNoNamedIdle()
        {
            var model = ModelWith(Animation("walk", new MdlNode()), Animation("pausetired", new MdlNode()));

            MdlAnimationPose.FindIdle(model)!.Name.Should().Be("pausetired");
        }

        /// <summary>
        /// No idle means no pose. An arbitrary animation would be worse than the bind pose - a walk or
        /// an attack frame reads as a broken model rather than an unposed one.
        /// </summary>
        [Test]
        public void AModelWithNoIdleGetsNoPose()
        {
            MdlAnimationPose.FindIdle(ModelWith(Animation("walk", new MdlNode()))).Should().BeNull();
            MdlAnimationPose.FindIdle(ModelWith()).Should().BeNull();
            MdlAnimationPose.FindIdle(null).Should().BeNull();
        }

        [Test]
        public void AKeyframedNodeIsSampledAndAnUntouchedOneIsAbsent()
        {
            var root = new MdlNode { Name = "root" };
            root.Children.Add(Bone("hand", (0f, new Vector3(1f, 2f, 3f))));

            var posed = MdlAnimationPose.Sample(Animation("pause1", root), 0f);

            posed.Should().ContainKey("hand");
            posed["hand"].Position.Should().Be(new Vector3(1f, 2f, 3f));
            posed.Should().NotContainKey("root", "a node with no keyframes keeps its static transform");
        }

        [Test]
        public void ValuesAreInterpolatedBetweenKeyframes()
        {
            var root = new MdlNode { Name = "root" };
            root.Children.Add(Bone("hand", (0f, Vector3.Zero), (2f, new Vector3(10f, 0f, 0f))));

            MdlAnimationPose.Sample(Animation("pause1", root), 1f)["hand"]
                .Position.X.Should().BeApproximately(5f, 0.001f);
        }

        /// <summary>
        /// Past either end the nearest keyframe is held rather than wrapped - asking beyond the track
        /// wants the final pose, not the first one snapped back to.
        /// </summary>
        [TestCase(-5f, 0f)]
        [TestCase(99f, 10f)]
        public void SamplingOutsideTheTrackClampsRatherThanWrapping(float seconds, float expectedX)
        {
            var root = new MdlNode { Name = "root" };
            root.Children.Add(Bone("hand", (0f, Vector3.Zero), (2f, new Vector3(10f, 0f, 0f))));

            MdlAnimationPose.Sample(Animation("pause1", root), seconds)["hand"]
                .Position.X.Should().BeApproximately(expectedX, 0.001f);
        }

        [Test]
        public void SamplingHandlesAnAnimationWithNoNodes()
        {
            MdlAnimationPose.Sample(null, 0f).Should().BeEmpty();
            MdlAnimationPose.Sample(new MdlAnimation { Name = "pause1" }, 0f).Should().BeEmpty();
        }

        /// <summary>
        /// The pose replaces a node's own transform when composing, and a bone's pose carries its
        /// descendants - which is what lets one skeleton's idle pose a whole composed body.
        /// </summary>
        [Test]
        public void APosedBoneMovesEverythingBelowIt()
        {
            var upper = new MdlNode { Name = "upper", Position = Vector3.Zero };
            var lower = new MdlNode { Name = "lower", Position = new Vector3(0f, 0f, 1f), Parent = upper };
            upper.Children.Add(lower);

            var pose = new Dictionary<string, PosedNode>(StringComparer.OrdinalIgnoreCase)
            {
                ["upper"] = new PosedNode(new Vector3(5f, 0f, 0f), Quaternion.Identity, 1f)
            };

            var unposed = Vector3.Transform(Vector3.Zero, MdlMeshBuilder.ComposeNodeTransform(lower));
            var posed = Vector3.Transform(Vector3.Zero, MdlMeshBuilder.ComposeNodeTransform(lower, pose));

            unposed.Should().Be(new Vector3(0f, 0f, 1f));
            posed.Should().Be(new Vector3(5f, 0f, 1f), "the child rides its parent's posed transform");
        }

        [Test]
        public void AnimationScaleAppliesToInheritedTranslationsOnly()
        {
            var inheritedRoot = new MdlNode { Name = "root" };
            inheritedRoot.Children.Add(Bone(
                "hand",
                (0f, new Vector3(2f, 0f, 0f))));
            var superModel = ModelWith(Animation("pause1", inheritedRoot));
            var model = new MdlModel
            {
                SuperModel = "shared_idle",
                Scale = 3f
            };

            var posed = MdlAnimationPose.SampleIdle(
                model,
                resRef => resRef == "shared_idle" ? superModel : null);

            posed["hand"].Position.Should().Be(new Vector3(6f, 0f, 0f));
        }

        [Test]
        public void CreaturePreviewClipsResolveIdleWalkAndAttackFromTheSupermodelChain()
        {
            static MdlNode AnimatedRoot(float endX)
            {
                var root = new MdlNode { Name = "root" };
                root.Children.Add(Bone(
                    "hand",
                    (0f, Vector3.Zero),
                    (1f, new Vector3(endX, 0f, 0f))));
                return root;
            }

            var superModel = ModelWith(
                Animation("pause1", AnimatedRoot(1f)),
                Animation("walk", AnimatedRoot(2f)),
                Animation("1hslashl", AnimatedRoot(3f)));
            var model = new MdlModel { SuperModel = "a_ba" };

            var clips = MdlAnimationPose.SampleCreaturePreviewAnimations(
                model,
                name => name == "a_ba" ? superModel : null,
                framesPerSecond: 2);

            clips.Select(clip => clip.Name).Should().Equal("pause1", "walk", "1hslashl");
            clips.Should().OnlyContain(clip => clip.Frames.Count > 1);
        }
    }
}
