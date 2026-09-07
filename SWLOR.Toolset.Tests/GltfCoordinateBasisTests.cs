using System.Numerics;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Animation;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests;

public class GltfCoordinateBasisTests
{
    private string _folder = null!;
    [SetUp] public void Setup()
    {
        _folder = Path.Combine(Path.GetTempPath(), "swlor-gltf-basis-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
    }
    [TearDown] public void Teardown() => Directory.Delete(_folder, true);

    [TestCase(0)] [TestCase(1)] [TestCase(2)]
    public void SampleConvertsRotationAxesAndInheritedTranslationsToNwnCoordinates(int axis)
    {
        var source = Source(axis);
        var rest = source.Sample(0, 0);
        foreach (var matrix in rest)
            AssertDirections(matrix, Matrix4x4.Identity);
        Near(rest[0].Translation, new(0, 0, 1));
        Near(rest[1].Translation, new(1, 0, 1));

        var expectedRotation = Matrix4x4.CreateFromAxisAngle(NwnAxis(axis), MathF.PI / 2);
        var moved = source.Sample(0, 1);
        foreach (var matrix in moved)
            AssertDirections(matrix, expectedRotation);
        Near(moved[0].Translation, new(0, 0, 3));
        Near(moved[1].Translation, new Vector3(0, 0, 3) + Vector3.TransformNormal(Vector3.UnitX, expectedRotation));
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)]
    public void CalibratedBakeRotatesAroundConvertedAxesAndPreservesTargetBoneOffsets(int axis)
    {
        var source = Source(axis);
        var pose = new[]
        {
            new PosedNode(new(0, 0, 1), Quaternion.CreateFromAxisAngle(Vector3.UnitX, .4f), 1),
            new PosedNode(Vector3.UnitX, Quaternion.CreateFromAxisAngle(Vector3.UnitY, .3f), 1)
        };
        var rig = new AnimationProject { AnimationRoot = "root", Joints =
            [new("root", -1, pose[0]), new("child", 0, pose[1])] };
        var calibration = new AnimationRetarget(rig, pose, source, 0, 0,
            [new("root", "Root"), new("child", "Child")]);
        var baked = calibration.Bake(source, 0, 20, 1);
        var initial = AnimationRig.World(rig.Joints, pose);
        var start = AnimationRig.World(baked.Joints, baked.Sample(0));
        var finish = AnimationRig.World(baked.Joints, baked.Sample(1));
        var turn = Matrix4x4.CreateFromAxisAngle(NwnAxis(axis), MathF.PI / 2);
        for (var i = 0; i < pose.Length; i++)
        {
            AssertDirections(start[i], initial[i]);
            Near(start[i].Translation, initial[i].Translation);
            AssertDirections(finish[i], initial[i] * turn);
        }
        Near(finish[0].Translation, new(0, 0, 3));
        Near(finish[1].Translation, finish[0].Translation +
            Vector3.TransformNormal(initial[1].Translation - initial[0].Translation, turn));
    }

    private static Vector3 NwnAxis(int axis) => axis switch
    {
        0 => Vector3.UnitX, 1 => Vector3.UnitZ, _ => -Vector3.UnitY
    };

    private static void AssertDirections(Matrix4x4 actual, Matrix4x4 expected)
    {
        foreach (var direction in new[] { Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ })
            Near(Vector3.TransformNormal(direction, actual), Vector3.TransformNormal(direction, expected));
    }

    private static void Near(Vector3 actual, Vector3 expected) =>
        Vector3.Distance(actual, expected).Should().BeLessThan(1e-5f, $"expected {expected}, received {actual}");

    [TestCase(0f)] [TestCase(5f)]
    public void PositiveClipStartsSampleAndBakeOnAZeroBasedTimeline(float startTime)
    {
        var source = Source(0, startTime);
        source.Animations[0].Duration.Should().Be(1);
        source.Animations[0].Tracks[0].Times.Should().Equal(startTime, startTime + 1);
        Near(source.Sample(0, 0)[0].Translation, new(0, 0, 1));
        Near(source.Sample(0, .5f)[0].Translation, new(0, 0, 2));
        Near(source.Sample(0, 1)[0].Translation, new(0, 0, 3));
        var rest = new PosedNode(new(0, 0, 1), Quaternion.Identity, 1);
        var rig = new AnimationProject { AnimationRoot = "root", Joints = [new("root", -1, rest)] };
        var calibration = new AnimationRetarget(rig, [rest], source, 0, 0, [new("root", "Root")]);
        var baked = calibration.Bake(source, 0, 20, 1);
        baked.Duration.Should().Be(1);
        Near(baked.Sample(.5f)[0].Position, new(0, 0, 2));
        Near(baked.Sample(1)[0].Position, new(0, 0, 3));
    }

    [Test]
    public void SingleKeyAtAPositiveTimestampIsAOneSecondHeldPose()
    {
        var source = Source(0, 5, singleKey: true);
        source.Animations[0].Duration.Should().Be(0);
        source.GetPlaybackDuration(0).Should().Be(1);
        Near(source.Sample(0, 0)[0].Translation, new(0, 0, 1));
        Near(source.Sample(0, 1)[0].Translation, new(0, 0, 1));
    }

    private GltfAnimationSource Source(int axis, float startTime = 0, bool singleKey = false)
    {
        var rotation = Quaternion.CreateFromAxisAngle(axis switch
            { 0 => Vector3.UnitX, 1 => Vector3.UnitY, _ => Vector3.UnitZ }, MathF.PI / 2);
        using var payload = new MemoryStream();
        using (var writer = new BinaryWriter(payload, System.Text.Encoding.UTF8, leaveOpen: true))
            foreach (var value in new[] { startTime, startTime + 1, 0, 0, 0, 1, rotation.X, rotation.Y, rotation.Z, rotation.W, 0, 1, 0, 0, 3, 0 })
                writer.Write(value);
        File.WriteAllBytes(Path.Combine(_folder, "motion.bin"), payload.ToArray());
        var path = Path.Combine(_folder, "motion.gltf");
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            asset = new { version = "2.0" },
            buffers = new[] { new { byteLength = payload.Length, uri = "motion.bin" } },
            bufferViews = new[]
            {
                new { buffer = 0, byteOffset = 0, byteLength = 8 },
                new { buffer = 0, byteOffset = 8, byteLength = 32 },
                new { buffer = 0, byteOffset = 40, byteLength = 24 }
            },
            accessors = new[]
            {
                new { bufferView = 0, componentType = 5126, count = singleKey ? 1 : 2, type = "SCALAR" },
                new { bufferView = 1, componentType = 5126, count = singleKey ? 1 : 2, type = "VEC4" },
                new { bufferView = 2, componentType = 5126, count = singleKey ? 1 : 2, type = "VEC3" }
            },
            nodes = new object[]
            {
                new { name = "Root", children = new[] { 1 }, translation = new[] { 0, 1, 0 } },
                new { name = "Child", translation = new[] { 1, 0, 0 } }
            },
            animations = new[] { new
            {
                samplers = new[] { new { input = 0, output = 1 }, new { input = 0, output = 2 } },
                channels = new[]
                {
                    new { sampler = 0, target = new { node = 0, path = "rotation" } },
                    new { sampler = 1, target = new { node = 0, path = "translation" } }
                }
            } }
        }));
        return GltfAnimationSource.Load(path);
    }
}
