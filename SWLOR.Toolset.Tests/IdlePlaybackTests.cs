using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Sampling an idle across its length so it can be played once and left to settle, the way Aurora
    /// plays a creature's idle when an area opens and then leaves it standing where it finished.
    /// </summary>
    public class IdlePlaybackTests
    {
        private static string RepoRoot
        {
            get
            {
                var current = new DirectoryInfo(AppContext.BaseDirectory);
                while (current != null)
                {
                    if (File.Exists(Path.Combine(current.FullName, "Build", "hakbuilder.json")) &&
                        Directory.Exists(Path.Combine(current.FullName, "SWLOR_Haks")))
                    {
                        return current.FullName;
                    }

                    current = current.Parent;
                }

                throw new DirectoryNotFoundException("Could not locate the repository root from the test context.");
            }
        }

        private static MdlModel AnimatedModel(float length)
        {
            var root = new MdlNode { Name = "root" };
            var bone = new MdlNode
            {
                Name = "bone",
                PositionTimes = new[] { 0f, length },
                PositionValues = new[] { Vector3.Zero, new Vector3(10f, 0f, 0f) }
            };
            root.Children.Add(bone);

            var model = new MdlModel();
            model.Animations.Add(new MdlAnimation { Name = "pause1", Length = length, GeometryRoot = root });
            return model;
        }

        [Test]
        public void FramesSpanTheWholeAnimation()
        {
            var frames = MdlAnimationPose.SampleIdleFrames(AnimatedModel(2f), _ => null, framesPerSecond: 10);

            frames.Should().HaveCountGreaterThan(1);
            frames[0].Seconds.Should().BeApproximately(0f, 0.001f);
            frames[^1].Seconds.Should().BeApproximately(2f, 0.001f);
            frames[0].Pose["bone"].Position.X.Should().BeApproximately(0f, 0.01f);
            frames[^1].Pose["bone"].Position.X.Should().BeApproximately(10f, 0.01f);
        }

        /// <summary>Frame count is bounded, so a long idle cannot cost unbounded memory per model.</summary>
        [Test]
        public void FrameCountIsCapped()
        {
            MdlAnimationPose.SampleIdleFrames(AnimatedModel(600f), _ => null, framesPerSecond: 30, maxFrames: 24)
                .Should().HaveCount(24);
        }

        [Test]
        public void AModelWithNoIdleYieldsNoFrames()
        {
            MdlAnimationPose.SampleIdleFrames(new MdlModel(), _ => null).Should().BeEmpty();
            MdlAnimationPose.SampleIdleFrames(null, _ => null).Should().BeEmpty();
        }

        /// <summary>
        /// The resting transform is the last frame, not the first. The animation stops where it ends
        /// and stays there, so picking, bounds and a still thumbnail all want that pose.
        /// </summary>
        [Test]
        public void TheBuiltMeshRestsOnTheFinalFrame()
        {
            var model = AnimatedModel(2f);
            var mesh = new MdlTrimeshNode
            {
                Name = "bone",
                Vertices = new[] { Vector3.Zero },
                Faces = new[] { new MdlFace { VertexIndex0 = 0, VertexIndex1 = 0, VertexIndex2 = 0 } },
                Bitmap = "t"
            };
            var root = new MdlNode { Name = "root" };
            root.Children.Add(mesh);
            mesh.Parent = root;
            model.GeometryRoot = root;

            var frames = MdlAnimationPose.SampleIdleFrames(model, _ => null, framesPerSecond: 10)
                .Select(f => f.Pose).ToList();
            var built = MdlMeshBuilder.Build(model, frames);

            var rendered = built.Meshes.Single();
            rendered.PoseFrames.Should().HaveCount(frames.Count);
            rendered.Transform.Should().Be(rendered.PoseFrames[^1], "the idle comes to rest on its last frame");
            rendered.PoseFrames[0].Should().NotBe(rendered.PoseFrames[^1], "the frames must actually differ");
        }

        /// <summary>A real creature yields a run of frames that move, not one repeated pose.</summary>
        [Test]
        public void ARealCreatureYieldsFramesThatDiffer()
        {
            var installPath = NwnInstallLocator.Locate();
            if (installPath == null)
            {
                Assert.Ignore("No local NWN:EE installation was found; skipping.");
                return;
            }

            var index = ResourceIndex.FromHakBuilderConfig(
                Path.Combine(RepoRoot, "Build", "hakbuilder.json"),
                Path.Combine(RepoRoot, "SWLOR_Haks"),
                KeyBifCatalog.Load(Path.Combine(installPath, "data")));
            var reader = new MdlReader();

            MdlModel? Load(string name)
            {
                var identity = new ResourceIdentity(name, ResourceIdentity.TypeFromExtension("mdl"));
                return index.TryLookup(identity, out var handle) ? reader.Parse(handle.GetBytes()) : null;
            }

            var model = Load("pmh0");
            if (model == null)
            {
                Assert.Ignore("pmh0 did not resolve; skipping.");
                return;
            }

            var frames = MdlAnimationPose.SampleIdleFrames(model, Load);

            frames.Should().HaveCountGreaterThan(1, "an idle with a length should sample to several frames");
            frames.Should().OnlyContain(f => f.Pose.Count > 0);

            var first = frames[0].Pose;
            var last = frames[^1].Pose;
            first.Keys.Should().IntersectWith(last.Keys);
            first.Any(kv => last.TryGetValue(kv.Key, out var end) && end != kv.Value)
                .Should().BeTrue("a breathing idle has to move something between its ends");
        }
    }
}
