using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Animation;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Editors.Animation;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests;

public class AnimationEditorTests
{
    private string _folder = null!;
    [SetUp] public void Setup() { _folder = Path.Combine(Path.GetTempPath(), "swlor-animation-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(_folder); }
    [TearDown] public void Teardown() { Directory.Delete(_folder, true); }
    private static AnimationProject Rig() => new()
    {
        Name = "Wave", ModelName = "hero", AnimationRoot = "rootdummy",
        Joints =
        [
            new("hero", -1, new(Vector3.Zero, Quaternion.Identity, 1)),
            new("rootdummy", 0, new(Vector3.Zero, Quaternion.Identity, 1)),
            new("upper", 1, new(Vector3.Zero, Quaternion.Identity, 1)),
            new("lower", 2, new(Vector3.UnitX, Quaternion.Identity, 1)),
            new("hand", 3, new(Vector3.UnitX, Quaternion.Identity, 1))
        ]
    };
    private string Write(string relative, string text)
    {
        var path = Path.Combine(_folder, relative); Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, text); return path;
    }

    [Test] public void ProjectAndMdlRoundTripPreservePosesEventsAndCulture()
    {
        var rig = Rig(); var first = rig.Sample(0); var last = rig.Sample(0);
        last[2] = last[2] with { Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2), Position = new(.25f, 0, 0) };
        rig.SetKey(0, first); rig.SetKey(1, last); rig.Events.Add(new(.4f, "cast"));
        var old = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var copy = AnimationProject.Deserialize(rig.Serialize());
            var mdl = AnimationMdl.Export(copy); mdl.Should().Contain("event 0.4");
            var imported = AnimationMdl.Import(mdl, Rig());
            imported.Events.Should().Equal(rig.Events);
            for (var i = 0; i <= 20; i++)
            {
                var a = rig.Sample(i / 20f); var b = imported.Sample(i / 20f);
                for (var j = 0; j < a.Length; j++)
                {
                    Vector3.Distance(a[j].Position, b[j].Position).Should().BeLessThan(1e-5f);
                    Math.Abs(Quaternion.Dot(a[j].Orientation, b[j].Orientation)).Should().BeApproximately(1, 1e-5f);
                }
            }
        }
        finally { CultureInfo.CurrentCulture = old; }
    }
    [Test] public void MdlImportRejectsUnsupportedControllersRatherThanDroppingThem()
    {
        var block = AnimationMdl.Export(Rig()).Replace("  endnode", "    alphakey 1\n      0 0.5\n  endnode");
        Action act = () => AnimationMdl.Import(block, Rig()); act.Should().Throw<InvalidDataException>().WithMessage("*Unsupported*");
    }
    [TestCase(0)] [TestCase(1)]
    public void MdlImportUsesSourceDefaultsIndependentlyOfThePreviousClip(int explicitLength)
    {
        var rig = Rig(); rig.Duration = 5; rig.Transition = 2; rig.AnimationRoot = "hand";
        var length = explicitLength == 0 ? "" : "length 0.7\n";
        var imported = AnimationMdl.Import("newanim Pose hero\n" + length +
            "node dummy hand\nparent lower\nposition 0 0 0\nendnode\ndoneanim Pose hero", rig);
        imported.Duration.Should().Be(explicitLength == 0 ? 1 : .7f);
        imported.Transition.Should().Be(0);
        imported.AnimationRoot.Should().Be("hero");
        imported.Keys.Last().Time.Should().Be(imported.Duration);
        rig.Duration.Should().Be(5); rig.Transition.Should().Be(2); rig.AnimationRoot.Should().Be("hand");
    }

    [TestCase(1)] [TestCase(10)]
    public void SparseSkinMeshesCannotBypassThePlaybackMemoryBudget(int meshCount)
    {
        var model = new MdlModel { GeometryRoot = new MdlNode { Name = "root" } };
        for (var i = 0; i < meshCount; i++)
            model.GeometryRoot.Children.Add(new MdlSkinmeshNode
            {
                Name = "skin" + i, Vertices = new Vector3[65535],
                Faces = [new MdlFace { VertexIndex0 = 0, VertexIndex1 = 1, VertexIndex2 = 2 }]
            });
        var count = AnimationEditorDocumentViewModel.PreviewFrameCount(model, 10);
        if (meshCount == 1) count.Should().BeInRange(2, 8);
        else count.Should().Be(0, "even two frames exceed the budget, so playback must stay disabled");
    }
    [TestCase("Pose", "hero")]
    [TestCase("pose", "HERO")]
    [TestCase("POSE", "Hero")]
    public void StaticAnimationValuesOverrideTheRigBindPose(string endAnimation, string endModel)
    {
        var block = "newanim Pose hero\nlength 1\nnode dummy hand\nparent lower\nposition 0 0 0\norientation 0 0 1 0.5\nendnode\n" +
            $"doneanim {endAnimation} {endModel}\n";
        var pose = AnimationMdl.Import(block, Rig()).Sample(.5f);
        pose[4].Position.Should().Be(Vector3.Zero);
        Math.Abs(Quaternion.Dot(pose[4].Orientation, Quaternion.CreateFromAxisAngle(Vector3.UnitZ, .5f))).Should().BeApproximately(1, 1e-5f);
    }
    [TestCase("Other", "hero")]
    [TestCase("Pose", "other")]
    public void MdlImportRejectsMismatchedAnimationTerminators(string endAnimation, string endModel)
    {
        var block = $"newanim Pose hero\nlength 1\ndoneanim {endAnimation} {endModel}\n";
        Action import = () => AnimationMdl.Import(block, Rig());
        import.Should().Throw<InvalidDataException>().WithMessage("*Mismatched animation terminator*");
    }
    [TestCase("newanim")] [TestCase("NEWANIM")]
    public void MdlImportRejectsASecondBlockEvenAfterACompleteFirstAnimation(string declaration)
    {
        var rig = Rig();
        var model = "newmodel hero\n" + AnimationMdl.ExportGeometry(rig) + AnimationMdl.Export(rig) +
            "# another clip follows\n" + AnimationMdl.Export(rig, "Other").Replace("newanim", declaration) + "donemodel hero\n";
        Action import = () => AnimationMdl.Import(model, rig);
        import.Should().Throw<InvalidDataException>().WithMessage("*one newanim/doneanim block*");
    }
    [Test] public void MdlImportSupportsEndlistTracksAndPreservesTheirInterpolatedTransforms()
    {
        var block = "newanim Pose hero\nlength 1\nnode dummy hand\nparent lower\n" +
            "positionkey\n0 0 0 0\n1 2 0 0\nendlist\n" +
            "orientationkey\n0 0 0 1 0\n1 0 0 1 1.57079633\nendlist\n" +
            "scalekey\n0 1\n1 2\nendlist\nendnode\ndoneanim Pose hero\n";
        var pose = AnimationMdl.Import(block, Rig()).Sample(.5f)[4];
        pose.Position.Should().Be(Vector3.UnitX); pose.Scale.Should().Be(1.5f);
        Math.Abs(Quaternion.Dot(pose.Orientation, Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 4))).Should().BeApproximately(1, 1e-5f);
        Action missingEndlist = () => AnimationMdl.Import(block.Replace("endlist\nendnode", "endnode"), Rig());
        missingEndlist.Should().Throw<InvalidDataException>();
        Action duplicateTime = () => AnimationMdl.Import(block.Replace("1 2 0 0", "0 2 0 0"), Rig());
        duplicateTime.Should().Throw<InvalidDataException>().WithMessage("*times must increase*");
        var oversized = "newanim Pose hero\nlength 600\nnode dummy hand\nparent lower\nscalekey\n" +
            string.Join('\n', Enumerable.Range(0, AnimationProject.MaxKeyframes + 1).Select(i => $"{i} 1")) + "\nendlist\nendnode\ndoneanim Pose hero";
        Action tooManyKeys = () => AnimationMdl.Import(oversized, Rig());
        tooManyKeys.Should().Throw<InvalidDataException>().WithMessage("*too many keys*");
    }
    [TestCase("rootdummy", "rhand_g")] [TestCase("ROOTDUMMY", "RHAND_G")]
    [TestCase("PELVIS_G", "LFOOT_G")] [TestCase("Pelvis_G", "Lhand_G")] [TestCase("RootDummy", "Rfoot_G")]
    public void MovingTheBodyAnchorsTheHandPositionAndOrientation(string body, string end)
    {
        var rig = Rig(); rig.Joints[1] = rig.Joints[1] with { Name = body }; rig.Joints[4] = rig.Joints[4] with { Name = end };
        var pose = rig.Sample(0); var original = AnimationRig.World(rig.Joints, pose)[4];
        var changed = AnimationRig.SetJoint(rig.Joints, pose, 1,
            pose[1] with { Position = new(.3f, 0, 0), Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, .3f) }, true);
        var result = AnimationRig.World(rig.Joints, changed)[4];
        Vector3.Distance(original.Translation, result.Translation).Should().BeLessThan(1e-4f);
        Matrix4x4.Decompose(original, out _, out var a, out _); Matrix4x4.Decompose(result, out _, out var b, out _);
        Math.Abs(Quaternion.Dot(a, b)).Should().BeApproximately(1, 1e-5f);
    }
    [Test] public void MdlImportRejectsOutOfRangeAndDuplicateTimes()
    {
        var rig = Rig(); rig.SetKey(0, rig.Sample(0));
        var moved = rig.Sample(0); moved[1] = moved[1] with { Position = Vector3.UnitX };
        rig.SetKey(1, moved); // A real changing track must retain both timestamps after compaction.
        var text = AnimationMdl.Export(rig).Replace("length 1", "length 0.5");
        Action act = () => AnimationMdl.Import(text, Rig()); act.Should().Throw<InvalidDataException>();
        text = AnimationMdl.Export(rig).Replace("      1 ", "      0 ");
        act.Should().Throw<InvalidDataException>();
    }
    [Test] public void MdlImportRejectsConflictingControllersAndHierarchy()
    {
        var text = AnimationMdl.Export(Rig());
        Action duplicate = () => AnimationMdl.Import(text.Replace("    positionkey", "    position 0 0 0\n    positionkey"), Rig());
        duplicate.Should().Throw<InvalidDataException>().WithMessage("*Duplicate*");
        Action parent = () => AnimationMdl.Import(text.Replace("parent lower", "parent rootdummy"), Rig());
        parent.Should().Throw<InvalidDataException>().WithMessage("*hierarchy*");
    }
    [Test] public void IkReachesTargetAndPreservesLengthsAndHandWorldOrientation()
    {
        var rig = Rig(); var pose = rig.Sample(0);
        pose[1] = pose[1] with { Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, .3f) };
        var before = AnimationRig.World(rig.Joints, pose);
        var target = new Vector3(1.2f, .8f, .3f);
        var solved = AnimationRig.SolveLimb(rig.Joints, pose, 4, target, new(0, 0, 2));
        var world = AnimationRig.World(rig.Joints, solved);
        Vector3.Distance(world[4].Translation, target).Should().BeLessThan(1e-4f);
        Vector3.Distance(world[2].Translation, world[3].Translation).Should().BeApproximately(1, 1e-5f);
        Vector3.Distance(world[3].Translation, world[4].Translation).Should().BeApproximately(1, 1e-5f);
        Matrix4x4.Decompose(before[4], out _, out var a, out _); Matrix4x4.Decompose(world[4], out _, out var b, out _);
        Math.Abs(Quaternion.Dot(a, b)).Should().BeApproximately(1, 1e-5f);
    }
    [Test] public void IkClampsUnreachableTargetWithoutNaNs()
    {
        var rig = Rig(); var result = AnimationRig.SolveLimb(rig.Joints, rig.Sample(0), 4, new(100, 0, 0), Vector3.UnitX);
        foreach (var pose in result) AnimationProject.ValidatePose(pose);
        AnimationRig.World(rig.Joints, result)[4].Translation.Length().Should().BeApproximately(2, 1e-4f);
    }

    private string Gltf(string interpolation = "LINEAR", bool badView = false, float duration = 1)
    {
        var floats = interpolation == "CUBICSPLINE"
            ? new float[] { 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0 }
            : new float[] { 0, 1, 0, 0, 0, 1, 0, 0 };
        floats[1] = duration;
        var bytes = floats.SelectMany(BitConverter.GetBytes).ToArray();
        var data = new
        {
            asset = new { version = "2.0" },
            buffers = new[] { new { byteLength = bytes.Length, uri = "data:application/octet-stream;base64," + Convert.ToBase64String(bytes) } },
            bufferViews = new[] { new { buffer = 0, byteOffset = 0, byteLength = 8 }, new { buffer = 0, byteOffset = 8, byteLength = badView ? 4 : bytes.Length - 8 } },
            accessors = new object[] { new { bufferView = 0, componentType = 5126, count = 2, type = "SCALAR" },
                new { bufferView = 1, componentType = 5126, count = interpolation == "CUBICSPLINE" ? 6 : 2, type = "VEC3" } },
            nodes = new object[] { new { name = "Root", children = new[] { 1 } }, new { name = "Hand", translation = new[] { 0, 1, 0 } } },
            animations = new[] { new { name = "Step", samplers = new[] { new { input = 0, output = 1, interpolation } },
                channels = new[] { new { sampler = 0, target = new { node = 0, path = "translation" } } } } }
        };
        return Write("source.gltf", JsonSerializer.Serialize(data));
    }
    [TestCase("LINEAR", .5f)] [TestCase("STEP", 0f)] [TestCase("CUBICSPLINE", .5f)]
    public void GltfSamplesInterpolationAndConvertsYUpToZUp(string interpolation, float x)
    {
        var source = GltfAnimationSource.Load(Gltf(interpolation)); var pose = source.Sample(0, .5f);
        pose[0].Translation.X.Should().BeApproximately(x, 1e-5f);
        pose[1].Translation.Z.Should().BeApproximately(1, 1e-5f);
    }
    [Test] public void GltfRejectsAccessorEscapingItsView()
    {
        var path = Gltf(badView: true); Action act = () => GltfAnimationSource.Load(path); act.Should().Throw<InvalidDataException>();
    }
    [TestCase(false)] [TestCase(true)]
    public void GltfEmbeddedBuffersRetainSamplesWithWhitespaceOrGlbStorage(bool glb)
    {
        var path = Gltf(); var root = JsonNode.Parse(File.ReadAllText(path))!;
        var buffer = root["buffers"]![0]!.AsObject();
        var uri = buffer["uri"]!.GetValue<string>();
        var payload = uri[(uri.IndexOf(',') + 1)..];
        if (glb)
        {
            buffer.Remove("uri");
            var json = Encoding.UTF8.GetBytes(root.ToJsonString());
            var padding = (4 - json.Length % 4) % 4;
            var bin = Convert.FromBase64String(payload);
            using var contents = new MemoryStream();
            using (var writer = new BinaryWriter(contents, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(0x46546c67u); writer.Write(2u); writer.Write((uint)(28 + json.Length + padding + bin.Length));
                writer.Write(json.Length + padding); writer.Write(0x4e4f534au); writer.Write(json);
                for (var i = 0; i < padding; i++) writer.Write((byte)' ');
                writer.Write(bin.Length); writer.Write(0x004e4942u); writer.Write(bin);
            }
            path = Path.Combine(_folder, "source.glb"); File.WriteAllBytes(path, contents.ToArray());
        }
        else
        {
            buffer["uri"] = "data:application/octet-stream;base64,\n" + payload[..4] + " \t" + payload[4..] + "\r\n";
            File.WriteAllText(path, root.ToJsonString());
        }
        GltfAnimationSource.Load(path).Sample(0, .5f)[0].Translation.X.Should().BeApproximately(.5f, 1e-5f);
    }
    [TestCase(false)] [TestCase(true)]
    public void GlbAdditionalBuffersRequireTheirOwnUri(bool secondHasUri)
    {
        var root = JsonNode.Parse(File.ReadAllText(Gltf()))!;
        var buffers = root["buffers"]!.AsArray(); var first = buffers[0]!.AsObject();
        var uri = first["uri"]!.GetValue<string>(); var bin = Convert.FromBase64String(uri[(uri.IndexOf(',') + 1)..]);
        first.Remove("uri"); var second = first.DeepClone().AsObject();
        if (secondHasUri) second["uri"] = uri;
        buffers.Add(second);
        root["bufferViews"]![1]!["buffer"] = 1;
        var json = Encoding.UTF8.GetBytes(root.ToJsonString()); var padding = (4 - json.Length % 4) % 4;
        var path = Path.Combine(_folder, "multiple.glb");
        using (var stream = File.Create(path))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(0x46546c67u); writer.Write(2u); writer.Write(28 + json.Length + padding + bin.Length);
            writer.Write(json.Length + padding); writer.Write(0x4e4f534au); writer.Write(json);
            for (var i = 0; i < padding; i++) writer.Write((byte)' ');
            writer.Write(bin.Length); writer.Write(0x004e4942u); writer.Write(bin);
        }
        if (secondHasUri) GltfAnimationSource.Load(path).Sample(0, .5f)[0].Translation.X.Should().BeApproximately(.5f, 1e-5f);
        else
        {
            Action load = () => GltfAnimationSource.Load(path);
            load.Should().Throw<InvalidDataException>().WithMessage("*first GLB buffer*URI*");
        }
    }
    [TestCase("rootdummy")] [TestCase("ROOTDUMMY")]
    public void RetargetPreservesCalibrationAndScalesRootDisplacement(string root)
    {
        var source = GltfAnimationSource.Load(Gltf()); var rig = Rig(); var pose = rig.Sample(0);
        rig.AnimationRoot = root;
        pose[1] = pose[1] with { Position = new(0, 0, 1), Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, 2.4f) };
        var retarget = new AnimationRetarget(rig, pose, source, 0, 0, [new("rootdummy", "Root")]);
        var baked = retarget.Bake(source, 0, 30, 2);
        baked.Keys.Should().HaveCount(31);
        baked.Sample(1)[1].Position.Should().Be(new Vector3(2, 0, 1));
        Math.Abs(Quaternion.Dot(baked.Sample(0)[1].Orientation, pose[1].Orientation)).Should().BeApproximately(1, 1e-5f);
    }
    [TestCase("LINEAR")] [TestCase("STEP")] [TestCase("CUBICSPLINE")]
    public void GltfIndexedSamplingPreservesExactKeysEndpointsAndInteriorSamples(string interpolation)
    {
        var source = GltfAnimationSource.Load(Gltf(interpolation));
        source.Sample(0, -1)[0].Translation.X.Should().Be(0);
        source.Sample(0, 0)[0].Translation.X.Should().Be(0);
        source.Sample(0, 1)[0].Translation.X.Should().Be(1);
        source.Sample(0, 2)[0].Translation.X.Should().Be(1);
        var expected = interpolation == "STEP" ? 0 : interpolation == "CUBICSPLINE" ? .84375f : .75f;
        source.Sample(0, .75f)[0].Translation.X.Should().BeApproximately(expected, 1e-5f);
    }
    [Test] public void LongBakeRejectsTheKeyLimitBeforeSamplingAndSupportsTheMaximumValidRate()
    {
        var path = Gltf(); var json = JsonNode.Parse(File.ReadAllText(path))!;
        var uri = json["buffers"]![0]!["uri"]!.GetValue<string>();
        var bytes = Convert.FromBase64String(uri[(uri.IndexOf(',') + 1)..]);
        BitConverter.GetBytes(600f).CopyTo(bytes, 4);
        json["buffers"]![0]!["uri"] = "data:application/octet-stream;base64," + Convert.ToBase64String(bytes);
        File.WriteAllText(path, json.ToJsonString());
        var source = GltfAnimationSource.Load(path); var rig = Rig();
        var calibration = new AnimationRetarget(rig, rig.Sample(0), source, 0, 0, [new("rootdummy", "Root")]);
        // A non-finite transform would fail if a rejected bake entered the sampling loop.
        var track = source.Animations[0].Tracks[0]; var saved = track.Values[1]; track.Values[1] = new(float.NaN);
        Action tooManyKeys = () => calibration.Bake(source, 0, 60, 1);
        tooManyKeys.Should().Throw<InvalidDataException>().WithMessage("*keyframes*lower bake rate*");
        track.Values[1] = saved;
        var baked = calibration.Bake(source, 0, 30, 1);
        baked.Keys.Should().HaveCount(AnimationProject.MaxKeyframes);
        baked.Keys[^1].Time.Should().Be(600);
        baked.Sample(600)[1].Position.X.Should().BeApproximately(1, 1e-5f);
        baked.Sample(300.01f)[1].Position.X.Should().BeApproximately(300.01f / 600, 1e-5f);
    }
    [Test] public void BakeBudgetsSourceSamplingEvenForASmallTargetRig()
    {
        var path = Gltf(duration: 600); var json = JsonNode.Parse(File.ReadAllText(path))!;
        for (var i = 2; i < 256; i++) json["nodes"]!.AsArray().Add(new JsonObject { ["name"] = "Bone" + i });
        File.WriteAllText(path, json.ToJsonString());
        var source = GltfAnimationSource.Load(path); var rig = Rig();
        var calibration = new AnimationRetarget(rig, rig.Sample(0), source, 0, 0, [new("rootdummy", "Root")]);
        var track = source.Animations[0].Tracks[0]; var saved = track.Values[1]; track.Values[1] = new(float.NaN);
        Action overBudget = () => calibration.Bake(source, 0, 30, 1);
        overBudget.Should().Throw<InvalidDataException>().WithMessage("*source and target transform budget*lower bake rate*");
        track.Values[1] = saved;
        var baked = calibration.Bake(source, 0, 1, 1);
        baked.Keys.Should().HaveCount(601); baked.Sample(600)[1].Position.X.Should().BeApproximately(1, 1e-5f);
    }
    private static AnimationProject DenseRig()
    {
        var project = new AnimationProject { Name = "Dense", ModelName = "hero", AnimationRoot = "rootdummy", Duration = 600,
            Joints = [new("rootdummy", -1, new(Vector3.Zero, Quaternion.Identity, 1))] };
        for (var i = 0; i < AnimationProject.MaxKeyframes; i++)
            project.Keys.Add(new(i / 30f, [new(Vector3.Zero, Quaternion.Identity, 1)]));
        return project;
    }
    [Test] public void DenseMdlImportPreservesExactAndInterpolatedPosesAtTheKeyLimit()
    {
        var project = DenseRig();
        foreach (var key in project.Keys) key.Pose[0] = key.Pose[0] with { Position = new(key.Time, 0, 0) };
        var imported = AnimationMdl.Import(AnimationMdl.Export(project), project);
        imported.Keys.Should().HaveCount(AnimationProject.MaxKeyframes);
        foreach (var time in new[] { 0f, .01f, 299.5f, 599.99f, 600f })
            imported.Sample(time)[0].Position.X.Should().BeApproximately(time, .001f);
    }
    [Test] public void BakeRejectsAnUnsaveablePoseCountBeforeSampling()
    {
        var rig = Rig();
        while (rig.Joints.Count < 46) rig.Joints.Add(new("bone" + rig.Joints.Count, 0, new(Vector3.Zero, Quaternion.Identity, 1)));
        var source = GltfAnimationSource.Load(Gltf(duration: 600));
        var calibration = new AnimationRetarget(rig, rig.Sample(0), source, 0, 0, [new("rootdummy", "Root")]);
        source.Animations[0].Tracks[0].Values[0] = new(float.NaN);
        Action bake = () => calibration.Bake(source, 0, 30, 1);
        bake.Should().Throw<InvalidDataException>().WithMessage("*64 MB*Reduce the bake rate*");
    }
    [Test] public void LongBuilderNamesGetStableDistinctNativeClipNames()
    {
        var target = InstallFixture(); var project = Rig(); project.Name = "SaluteWithSaber";
        var first = AnimationInstall.Prepare(_folder, project, [target]); first.Apply();
        first.AnimationName.Length.Should().BeLessThanOrEqualTo(12);
        first.ConstantName.Should().Be("SaluteWithSaber");
        project.Name = "SaluteWithStaff";
        var second = AnimationInstall.Prepare(_folder, project, [target]); second.Apply();
        second.AnimationName.Should().NotBe(first.AnimationName); second.AnimationName.Length.Should().BeLessThanOrEqualTo(12);
        var model = new MdlReader().Parse(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(target)!, "an_hero.mdl")));
        model.Animations.Should().OnlyContain(animation => animation.Name.Length <= 16);
        project.Name = "SaluteWithSaber"; project.Duration = 2;
        AnimationInstall.Prepare(_folder, project, [target]).AnimationName.Should().Be(first.AnimationName);
        File.ReadAllText(Path.Combine(_folder, "SWLOR.Game.Server/Service/AnimationService/AuthoredAnimation.cs"))
            .Should().Contain("SaluteWithSaber").And.Contain("SaluteWithStaff");
    }
    [TestCase("Wave", "Wave_in")] [TestCase("Wave_in", "Wave")] [TestCase("Wave_out", "Wave")]
    public void InstallationReservesEveryPlaybackPhaseWhenAllocatingNames(string firstName, string secondName)
    {
        var target = InstallFixture(); var project = Rig(); project.Name = firstName;
        var first = AnimationInstall.Prepare(_folder, project, [target]); first.Apply();
        project.Name = secondName;
        var second = AnimationInstall.Prepare(_folder, project, [target]); second.Apply();
        var names = new[] { first.AnimationName, first.AnimationName + "_in", first.AnimationName + "_out",
            second.AnimationName, second.AnimationName + "_in", second.AnimationName + "_out" };
        names.Distinct(StringComparer.OrdinalIgnoreCase).Should().HaveCount(6);
        var model = new MdlReader().Parse(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(target)!, "an_hero.mdl")));
        model.Animations.Select(animation => animation.Name).Should().BeEquivalentTo(names);
        AnimationInstall.Prepare(_folder, project, [target]).AnimationName.Should().Be(second.AnimationName);
    }

    [TestCase(14)] [TestCase(15)] [TestCase(16)]
    public void FullLengthTargetNamesAllocateDistinctBanksAndReuseThemOnUpdate(int length)
    {
        var original = InstallFixture(); var source = File.ReadAllText(original);
        var names = new[] { "humanoid" + new string('a', length - 8), "humanoid" + new string('b', length - 8) };
        var targets = names.Select(name => Write("SWLOR_Haks/sw_cr_creature/" + name + ".mdl", source.Replace("hero", name))).ToArray();
        var collision = Write("SWLOR_Haks/sw_cr_creature/an_humanoid_0001.mdl", source.Replace("hero", "an_humanoid_0001"));
        var before = File.ReadAllBytes(collision);
        var project = Rig();
        AnimationInstall.Prepare(_folder, project, targets).Apply();
        var banks = targets.Select(path => new MdlReader().Parse(File.ReadAllBytes(path)).SuperModel).ToArray();
        banks.Should().OnlyHaveUniqueItems().And.OnlyContain(name => name.Length <= 16);
        banks.Should().NotContain("an_humanoid_0001");
        File.ReadAllBytes(collision).Should().Equal(before);
        File.Delete(collision);
        project.Duration = 2;
        AnimationInstall.Prepare(_folder, project, targets).Apply();
        targets.Select(path => new MdlReader().Parse(File.ReadAllBytes(path)).SuperModel).Should().Equal(banks);
        foreach (var bank in banks)
        {
            var model = new MdlReader().Parse(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(original)!, bank + ".mdl")));
            model.Animations.Single(animation => animation.Name == "sw_wave").Length.Should().Be(2);
        }
    }

    private string InstallFixture()
    {
        Write("Build/hakbuilder.json", "{\"HakList\":[{\"Path\":\"../SWLOR_Haks/sw_cr_creature\"}]}");
        Write("SWLOR.NWN.API/NWScript/Enum/Animation.cs", "enum Animation { Existing = 21, Reserved = 22 }");
        return Write("SWLOR_Haks/sw_cr_creature/hero.mdl", """
            # preserve this comment and geometry
            newmodel hero
            setsupermodel hero NULL
            setanimationscale 1
            beginmodelgeom hero
            node dummy hero
              parent NULL
            endnode
            node dummy rootdummy
              parent hero
            endnode
            node dummy upper
              parent rootdummy
            endnode
            node dummy lower
              parent upper
              position 1 0 0
            endnode
            node dummy hand
              parent lower
              position 1 0 0
            endnode
            endmodelgeom hero
            donemodel hero
            """);
    }
    [TestCase(false)] [TestCase(true)]
    public void ExitPoseFollowsInheritedIdlePastEmptyDeclarations(bool emptyLocalIdle)
    {
        var target = InstallFixture();
        var text = File.ReadAllText(target).Replace("setsupermodel hero NULL", "setsupermodel hero idlebase")
            .Replace("setanimationscale 1", "setanimationscale 2");
        if (emptyLocalIdle) text = text.Replace("donemodel hero", "newanim pause1 hero\nlength 1\ndoneanim pause1 hero\ndonemodel hero");
        File.WriteAllText(target, text);
        var idleRig = Rig(); idleRig.ModelName = "idlebase";
        idleRig.Joints[0] = idleRig.Joints[0] with { Name = "idlebase" };
        Write("SWLOR_Haks/sw_cr_creature/idlebase.mdl", "newmodel idlebase\nsetsupermodel idlebase NULL\n" +
            AnimationMdl.ExportGeometry(idleRig) +
            "newanim pause1 idlebase\nlength 1\nnode dummy lower\nparent upper\npositionkey\n0 0 3 0\nendlist\nendnode\ndoneanim pause1 idlebase\ndonemodel idlebase\n");
        AnimationInstall.Prepare(_folder, Rig(), [target]).Apply();
        var overlay = new MdlReader().Parse(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(target)!, "an_hero.mdl")));
        var exit = overlay.Animations.Single(a => a.Name == "sw_wave_out");
        var pose = MdlAnimationPose.Sample(exit, exit.Length, MdlAnimationPose.BindPose(overlay));
        pose["lower"].Position.Should().Be(new Vector3(0, 3, 0), "inherited translations are scaled once by the target at runtime");
    }

    [AvaloniaTest] public async Task OversizedProjectOpenAndExternalReloadKeepTheCurrentDocument()
    {
        var path = Path.Combine(_folder, "oversized.swlanim");
        using (var stream = File.Create(path)) stream.SetLength((long)AnimationProject.MaximumFileBytes + 1);
        var vm = new AnimationEditorDocumentViewModel(new Prompts { ExternalChoice = ExternalChangeChoice.Reload },
            new OutputLogService(), initial: Rig());
        var original = vm.Project;
        vm.PickOpenPath = (_, _) => Task.FromResult<string?>(path);
        await vm.OpenProjectCommand.ExecuteAsync(null);
        vm.Status.Should().Contain("64 MB"); vm.Project.Should().BeSameAs(original);
        var saved = Path.Combine(_folder, "saved.swlanim");
        vm.PickSavePath = (_, _) => Task.FromResult<string?>(saved);
        (await vm.TrySaveAsync()).Should().BeTrue();
        using (var stream = File.Open(saved, FileMode.Truncate)) stream.SetLength((long)AnimationProject.MaximumFileBytes + 1);
        vm.PositionX = .5m;
        var edited = vm.Project;
        (await vm.TrySaveAsync()).Should().BeFalse();
        vm.Status.Should().Contain("64 MB"); vm.Project.Should().BeSameAs(edited); vm.IsDirty.Should().BeTrue();
        new FileInfo(saved).Length.Should().Be(AnimationProject.MaximumFileBytes + 1L);
        vm.ApproveApplicationClose(); vm.OnClose();
    }
    [AvaloniaTest] public async Task OversizedMdlImportKeepsTheCurrentDocument()
    {
        var path = Path.Combine(_folder, "oversized.mdl");
        using (var stream = File.Create(path)) stream.SetLength((long)AnimationMdl.MaximumFileBytes + 1);
        var vm = new AnimationEditorDocumentViewModel(new Prompts(), new OutputLogService(), initial: Rig());
        var original = vm.Project;
        vm.PickOpenPath = (_, _) => Task.FromResult<string?>(path);
        await vm.ImportMdlCommand.ExecuteAsync(null);
        vm.Status.Should().Contain("MDL exceeds 64 MB");
        vm.Project.Should().BeSameAs(original); vm.IsDirty.Should().BeFalse();
        vm.OnClose().Should().BeTrue();
    }
    [AvaloniaTest] public async Task OversizedLooseRigAndPreviewKeepTheCurrentDocument()
    {
        var path = Path.Combine(_folder, "oversized.mdl");
        using (var stream = File.Create(path)) stream.SetLength((long)AnimationMdl.MaximumFileBytes + 1);
        var vm = new AnimationEditorDocumentViewModel(new Prompts(), new OutputLogService(), initial: Rig());
        var original = vm.Project;
        vm.PickOpenPath = (_, _) => Task.FromResult<string?>(path);
        await vm.LoadRigFileCommand.ExecuteAsync(null);
        vm.Status.Should().Contain("Rig model exceeds 64 MB"); vm.Project.Should().BeSameAs(original);
        await vm.AttachPreviewCommand.ExecuteAsync(null);
        vm.Status.Should().Contain("Preview model exceeds 64 MB"); vm.Project.Should().BeSameAs(original);
        vm.IsDirty.Should().BeFalse(); vm.OnClose().Should().BeTrue();
    }
    [Test] public async Task BoundedSourceReadsRejectOversizedFilesAndAcceptTheExactLimit()
    {
        var path = Write("bounded.bin", "12345");
        Action sync = () => AnimationSourceFile.ReadBytes(path, 4, "Source");
        Func<Task> asyncRead = () => AnimationSourceFile.ReadBytesAsync(path, 4, "Source");
        sync.Should().Throw<InvalidDataException>();
        await asyncRead.Should().ThrowAsync<InvalidDataException>();
        AnimationSourceFile.ReadBytes(path, 5, "Source").Should().Equal(Encoding.UTF8.GetBytes("12345"));
        (await AnimationSourceFile.ReadBytesAsync(path, 5, "Source")).Should().Equal(Encoding.UTF8.GetBytes("12345"));
        var gltf = Path.Combine(_folder, "oversized.gltf");
        using (var stream = File.Create(gltf)) stream.SetLength(128L * 1024 * 1024 + 1);
        Action load = () => GltfAnimationSource.Load(gltf);
        load.Should().Throw<InvalidDataException>().WithMessage("*128 MB*");
    }
    [Test] public void InstallationRejectsOversizedInputsAndChecksSnapshotsWithoutAllocatingTheChangedFile()
    {
        var target = InstallFixture(); var original = File.ReadAllBytes(target);
        var plan = AnimationInstall.Prepare(_folder, Rig(), [target]);
        AnimationSourceFile.Matches(target, original).Should().BeTrue();
        using (var stream = File.Open(target, FileMode.Truncate)) stream.SetLength(AnimationMdl.MaximumFileBytes + 1L);
        AnimationSourceFile.Matches(target, original).Should().BeFalse();
        Action apply = plan.Apply;
        apply.Should().Throw<IOException>().WithMessage("*changed after*");
        Action prepare = () => AnimationInstall.Prepare(_folder, Rig(), [target]);
        prepare.Should().Throw<InvalidDataException>().WithMessage("*64 MB*");
        File.Exists(Path.Combine(Path.GetDirectoryName(target)!, "an_hero.mdl")).Should().BeFalse();
        var larger = Enumerable.Range(0, 20_000).Select(i => (byte)i).ToArray();
        File.WriteAllBytes(target, larger);
        AnimationSourceFile.Matches(target, larger).Should().BeTrue();
        larger[16_385] ^= 1;
        AnimationSourceFile.Matches(target, larger).Should().BeFalse();
    }
    [TestCase(false)] [TestCase(true)]
    public async Task MdlFileImportPreservesBomEncodedTransforms(bool unicode)
    {
        var path = Path.Combine(_folder, "pose.mdl");
        var source = Rig(); var pose = source.Sample(0);
        pose[4] = pose[4] with { Position = new(.25f, .5f, .75f) }; source.SetKey(0, pose);
        await File.WriteAllTextAsync(path, AnimationMdl.Export(source), unicode ? Encoding.Unicode : new UTF8Encoding(true));
        var imported = await AnimationMdl.ImportFileAsync(path, Rig());
        imported.Sample(.5f)[4].Position.Should().Be(pose[4].Position);
    }
    [TestCase("an_hero")] [TestCase("ab_hero_001")]
    public void InstallationRejectsGeneratedBanksBeforePreparingAnyWrites(string bankName)
    {
        var target = InstallFixture();
        AnimationInstall.Prepare(_folder, Rig(), [target]).Apply();
        var firstBank = Path.Combine(Path.GetDirectoryName(target)!, "an_hero.mdl");
        var bank = Path.Combine(Path.GetDirectoryName(target)!, bankName + ".mdl");
        if (bank != firstBank)
        {
            File.WriteAllText(bank, File.ReadAllText(firstBank).Replace("an_hero", bankName));
            File.WriteAllBytes(firstBank, AnimationInstall.PatchSupermodel(File.ReadAllBytes(firstBank), "an_hero", bankName));
        }
        var before = Directory.EnumerateFiles(_folder, "*", SearchOption.AllDirectories)
            .ToDictionary(path => path, File.ReadAllBytes);
        var next = Rig(); next.Name = "Other";
        foreach (var selection in new[] { new[] { target, bank }, new[] { bank, target }, new[] { bank } })
        {
            Action prepare = () => AnimationInstall.Prepare(_folder, next, selection);
            prepare.Should().Throw<InvalidDataException>().WithMessage("*Generated animation banks*");
        }
        foreach (var (path, bytes) in before) File.ReadAllBytes(path).Should().Equal(bytes);
        Directory.EnumerateFiles(_folder, "*", SearchOption.AllDirectories).Should().BeEquivalentTo(before.Keys);
        AnimationInstall.Prepare(_folder, next, [target]).Apply();
    }
    [Test] public void InstallUsesNamesPreservesGeometryAndSupportsMoreClipsAndUpdates()
    {
        var target = InstallFixture(); var original = File.ReadAllText(target);
        var project = Rig(); var plan = AnimationInstall.Prepare(_folder, project, [target]);
        plan.AnimationName.Should().Be("sw_wave"); plan.CodeExample.Should().Be("NamedAnimation.Queue(creature, AuthoredAnimation.Wave);");
        plan.Apply();
        var constants = Path.Combine(_folder, "SWLOR.Game.Server/Service/AnimationService/AuthoredAnimation.cs");
        File.ReadAllText(constants).Should().Contain("SWLOR.Game.Server.Service.AnimationService.AnimationClip");
        plan.Changes.Should().NotContain(change => change.Path.Contains("SWLOR.NWN.API"), "authored clips are application data, not NWScript API declarations");
        File.ReadAllText(target).Should().Be(original.Replace("setsupermodel hero NULL", "setsupermodel hero an_hero"));
        var overlay = Path.Combine(Path.GetDirectoryName(target)!, "an_hero.mdl");
        var installed = new MdlReader().Parse(File.ReadAllBytes(overlay));
        installed.Animations.Select(a => a.Name).Should().Equal("sw_wave", "sw_wave_in", "sw_wave_out");
        AnimationProject.FromModel(installed).Joints.Select(j => j.Name).Should().Equal("an_hero", "rootdummy", "upper", "lower", "hand");
        // A Windows Git checkout can normalize generated text to CRLF between editor sessions.
        File.WriteAllText(overlay, File.ReadAllText(overlay).Replace("\r\n", "\n").Replace("\n", "\r\n"));
        project.Name = "Point";
        var second = AnimationInstall.Prepare(_folder, project, [target]); second.AnimationName.Should().Be("sw_point"); second.Apply();
        var targetBytes = File.ReadAllBytes(target);
        project.Name = "Wave"; project.Duration = 2;
        var update = AnimationInstall.Prepare(_folder, project, [target]); update.AnimationName.Should().Be("sw_wave"); update.Apply();
        var model = new MdlReader().Parse(File.ReadAllBytes(overlay)); model.Animations.Should().HaveCount(6);
        model.Animations.Single(a => a.Name == "sw_wave").Length.Should().Be(2);
        File.ReadAllBytes(target).Should().Equal(targetBytes);
    }
    [Test] public void InstallationRefusesConcurrentEditsWithoutWritingAnyOutputs()
    {
        var target = InstallFixture(); var plan = AnimationInstall.Prepare(_folder, Rig(), [target]);
        File.AppendAllText(target, "\n# external edit");
        Action act = plan.Apply; act.Should().Throw<IOException>();
        File.Exists(Path.Combine(_folder, "design/animations/registry.json")).Should().BeFalse();
        File.ReadAllText(target).Should().EndWith("# external edit");
    }
    [TestCase(false)] [TestCase(true)]
    public void InstallationSharesItsInputBudgetAcrossTargetsAndSupermodels(bool supermodel)
    {
        var target = InstallFixture(); var original = File.ReadAllText(target);
        var other = Write("SWLOR_Haks/sw_cr_creature/other.mdl", original.Replace("hero", "other"));
        if (supermodel) File.WriteAllText(target, original.Replace("setsupermodel hero NULL", "setsupermodel hero other"));
        var config = Path.Combine(_folder, "Build", "hakbuilder.json");
        var exactBudget = (int)(new FileInfo(config).Length + new FileInfo(target).Length + new FileInfo(other).Length);
        string[] targets = supermodel ? [target] : [target, other];
        Action rejected = () => AnimationInstall.Prepare(_folder, Rig(), targets, exactBudget - 1);
        rejected.Should().Throw<InvalidDataException>().WithMessage("*remaining aggregate budget*");
        Directory.GetFiles(Path.GetDirectoryName(target)!, "an_*.mdl").Should().BeEmpty();
        var plan = AnimationInstall.Prepare(_folder, Rig(), targets, exactBudget);
        plan.Inputs.Values.Sum(bytes => bytes.Length).Should().Be(exactBudget,
            "the same target is read several times, but each unique snapshot consumes its budget only once");
    }
    [Test] public void InstallationRechecksInputsAfterStagingBeforePublishingAnyOutput()
    {
        var input = Write("config.json", "original"); var output = Write("output.mdl", "existing model");
        var inputs = new ChangingInputs(() => File.WriteAllText(input, "external change")) { [input] = File.ReadAllBytes(input) };
        var plan = new AnimationInstallPlan
        {
            AnimationName = "sw_wave", ConstantName = "Wave", Inputs = inputs,
            Changes = [new(output, File.ReadAllBytes(output), Encoding.ASCII.GetBytes("new model"))]
        };
        Action apply = plan.Apply; apply.Should().Throw<IOException>().WithMessage("*changed after*preview*");
        File.ReadAllText(output).Should().Be("existing model");
        File.ReadAllText(input).Should().Be("external change");
        Directory.GetFiles(_folder, "*.tmp").Should().BeEmpty();
    }
    [Test] public void InstallationBoundsTheCombinedOutputsEvenWhenEveryIndividualOutputFits()
    {
        var first = InstallFixture(); var original = File.ReadAllText(first);
        var second = Write("SWLOR_Haks/sw_cr_creature/other.mdl", original.Replace("hero", "other"));
        var normal = AnimationInstall.Prepare(_folder, Rig(), [first, second]);
        var largest = normal.Changes.Max(change => change.After.Length);
        Action prepare = () => AnimationInstall.Prepare(_folder, Rig(), [first, second], AnimationInstall.MaximumInputBytes, largest);
        prepare.Should().Throw<InvalidDataException>().WithMessage("*outputs exceed the aggregate*");
        Directory.GetFiles(Path.GetDirectoryName(first)!, "an_*.mdl").Should().BeEmpty();
        File.ReadAllText(first).Should().Be(original);
        var total = normal.Changes.Sum(change => change.After.Length);
        AnimationInstall.Prepare(_folder, Rig(), [first, second], AnimationInstall.MaximumInputBytes, total)
            .Changes.Sum(change => change.After.Length).Should().Be(total);
    }
    [Test] public void InstallationPreviewRejectsTooManyMissingResolutionDependencies()
    {
        var target = InstallFixture();
        var layers = Enumerable.Range(0, AnimationInstall.MaximumAbsentReservations + 1)
            .Select(i => new { Path = "../SWLOR_Haks/absent" + i })
            .Append(new { Path = "../SWLOR_Haks/sw_cr_creature" });
        Write("Build/hakbuilder.json", JsonSerializer.Serialize(new { HakList = layers }));
        Action prepare = () => AnimationInstall.Prepare(_folder, Rig(), [target]);
        prepare.Should().Throw<InvalidDataException>().WithMessage("*too many missing model dependencies*");
        Directory.GetDirectories(Path.Combine(_folder, "SWLOR_Haks")).Should().HaveCount(1);
    }
    private sealed class ChangingInputs(Action afterFirstRead) : Dictionary<string, byte[]>, IEnumerable<KeyValuePair<string, byte[]>>
    {
        private int _reads;
        IEnumerator<KeyValuePair<string, byte[]>> IEnumerable<KeyValuePair<string, byte[]>>.GetEnumerator()
        {
            using var entries = base.GetEnumerator();
            while (entries.MoveNext()) yield return entries.Current;
            if (_reads++ == 0) afterFirstRead();
        }
    }
    [TestCase("hero", false)] [TestCase("an_hero", false)] [TestCase("base", false)] [TestCase("hero", true)]
    public void InstallationRejectsNewHigherPriorityModelsBeforePublishing(string resref, bool duringStaging)
    {
        var target = InstallFixture(); var original = File.ReadAllText(target);
        Write("Build/hakbuilder.json", "{\"HakList\":[{\"Path\":\"../SWLOR_Haks/overrides\"},{\"Path\":\"../SWLOR_Haks/sw_cr_creature\"}]}");
        if (resref == "base")
        {
            Write("SWLOR_Haks/sw_cr_creature/base.mdl", original.Replace("hero", "base"));
            original = original.Replace("setsupermodel hero NULL", "setsupermodel hero base"); File.WriteAllText(target, original);
        }
        var plan = AnimationInstall.Prepare(_folder, Rig(), [target]);
        void CreateShadow() => Write("SWLOR_Haks/overrides/" + resref + ".mdl", original);
        if (duringStaging)
        {
            var inputs = new ChangingInputs(CreateShadow);
            foreach (var input in plan.Inputs) inputs.Add(input.Key, input.Value);
            plan = new() { AnimationName = plan.AnimationName, ConstantName = plan.ConstantName,
                Changes = plan.Changes, Inputs = inputs, AbsentInputs = plan.AbsentInputs };
        }
        else CreateShadow();
        Action apply = plan.Apply; apply.Should().Throw<IOException>().WithMessage("*created after*preview*");
        File.ReadAllText(target).Should().Be(original);
        File.Exists(Path.Combine(_folder, "design/animations/registry.json")).Should().BeFalse();
        File.Exists(Path.Combine(Path.GetDirectoryName(target)!, "an_hero.mdl")).Should().BeFalse();
        Directory.GetFiles(_folder, "*.tmp", SearchOption.AllDirectories).Should().BeEmpty();
    }
    [Test] public void BinarySupermodelPatchChangesOnlyItsFixedHeaderField()
    {
        var original = Enumerable.Range(0, 1000).Select(i => (byte)i).ToArray(); Array.Clear(original, 0, 4);
        var patched = AnimationInstall.PatchSupermodel(original, "hero", "an_hero");
        patched[..180].Should().Equal(original[..180]); patched[244..].Should().Equal(original[244..]);
        Encoding.ASCII.GetString(patched, 180, 64).TrimEnd('\0').Should().Be("an_hero");
    }
    [Test] public void RealNwnRigRoundTripsAndRetainsAllBinaryGeometry()
    {
        var corpus = Environment.GetEnvironmentVariable("SWLOR_ANIMATION_CORPUS");
        if (string.IsNullOrWhiteSpace(corpus)) Assert.Ignore("Set SWLOR_ANIMATION_CORPUS to a local HAK source root for model corpus verification.");
        foreach (var name in new[] { "a_ba", "a_fa" })
        {
            var file = Directory.EnumerateFiles(corpus!, name + ".mdl", SearchOption.AllDirectories).First();
            var data = File.ReadAllBytes(file); var model = new MdlReader().Parse(data);
            var rig = AnimationProject.FromModel(model); rig.Name = "CorpusPose";
            var imported = AnimationMdl.Import(AnimationMdl.Export(rig), rig);
            var expected = rig.Sample(0); var actual = imported.Sample(0);
            for (var i = 0; i < expected.Length; i++)
            {
                Vector3.Distance(expected[i].Position, actual[i].Position).Should().BeLessThan(1e-5f);
                Math.Abs(Quaternion.Dot(expected[i].Orientation, actual[i].Orientation)).Should().BeApproximately(1, 1e-5f);
            }
            var patched = AnimationInstall.PatchSupermodel(data, model.Name, "an_" + name);
            new MdlReader().Parse(patched).GetMeshNodes().Count().Should().Be(model.GetMeshNodes().Count());
            patched[244..].Should().Equal(data[244..]);
        }
    }
    [Test] public void RealNwnModelsAcceptAnInstalledOverlayWithoutLosingTheirSkeletons()
    {
        var corpus = Environment.GetEnvironmentVariable("SWLOR_ANIMATION_CORPUS");
        if (string.IsNullOrWhiteSpace(corpus)) Assert.Ignore("Set SWLOR_ANIMATION_CORPUS for real installation verification.");
        var sources = Directory.EnumerateFiles(corpus!, "*.mdl", SearchOption.AllDirectories)
            .GroupBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key!, g => g.First(), StringComparer.OrdinalIgnoreCase);
        Write("Build/hakbuilder.json", "{\"HakList\":[{\"Path\":\"../SWLOR_Haks/models\"}]}");
        var copied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string CopyChain(string name)
        {
            var destination = Path.Combine(_folder, "SWLOR_Haks/models", name + ".mdl");
            if (!copied.Add(name)) return destination;
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(sources[name + ".mdl"], destination);
            var model = new MdlReader().Parse(File.ReadAllBytes(destination));
            // Local gameplay corpora may already contain this editor's overlays. Reconstruct
            // the pre-install chain in the fixture only, so this remains a fresh-install test.
            if (model.SuperModel.Equals("an_" + name, StringComparison.OrdinalIgnoreCase))
            {
                var installed = new MdlReader().Parse(File.ReadAllBytes(sources[model.SuperModel + ".mdl"]));
                File.WriteAllBytes(destination, AnimationInstall.PatchSupermodel(File.ReadAllBytes(destination), name, installed.SuperModel));
                model.SuperModel = installed.SuperModel;
            }
            if (!string.IsNullOrEmpty(model.SuperModel) && !model.SuperModel.Equals("NULL", StringComparison.OrdinalIgnoreCase)) CopyChain(model.SuperModel);
            return destination;
        }
        foreach (var name in new[] { "a_ba", "a_fa" })
        {
            var target = CopyChain(name); var original = File.ReadAllBytes(target);
            var project = AnimationProject.FromModel(new MdlReader().Parse(original)); project.Name = name == "a_ba" ? "MalePose" : "FemalePose";
            AnimationInstall.Prepare(_folder, project, [target]).Apply();
            var overlay = new MdlReader().Parse(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(target)!, "an_" + name + ".mdl")));
            var rig = AnimationProject.FromModel(overlay);
            rig.Joints.Skip(1).Select(j => (j.Name, j.Parent)).Should().Equal(project.Joints.Skip(1).Select(j => (j.Name, j.Parent)));
            overlay.Animations.Should().HaveCount(3);
            File.ReadAllBytes(target)[244..].Should().Equal(original[244..]);
        }
    }
    [Test] public void InstallationSupportsOverOneThousandClipsAcrossBoundedBanks()
    {
        var rig = Rig();
        while (rig.Joints.Count < 57) rig.Joints.Add(new("joint" + rig.Joints.Count, 1, new(Vector3.UnitZ * .01f, Quaternion.Identity, 1)));
        rig.SetKey(0, rig.Joints.Select(j => j.Rest).ToArray());
        var count = AnimationInstall.ClipsPerBank * 4;
        var banks = new[] { "an_hero", "ab_hero_001", "ab_hero_002", "ab_hero_003" };
        Write("Build/hakbuilder.json", "{\"HakList\":[{\"Path\":\"../SWLOR_Haks/models\"}]}");
        var target = Write("SWLOR_Haks/models/hero.mdl", "newmodel hero\nsetsupermodel hero an_hero\n" + AnimationMdl.ExportGeometry(rig) + "donemodel hero\n");
        var registrations = new List<AnimationRegistration>();
        for (var bankIndex = 0; bankIndex < banks.Length; bankIndex++)
        {
            var bank = banks[bankIndex]; var project = rig.Clone(); project.ModelName = bank;
            project.Joints[0] = project.Joints[0] with { Name = bank };
            var text = new System.Text.StringBuilder($"# SWLOR authored animations for hero\nnewmodel {bank}\nsetsupermodel {bank} {(bankIndex == banks.Length - 1 ? "NULL" : banks[bankIndex + 1])}\n");
            text.Append(AnimationMdl.ExportGeometry(project));
            for (var i = bankIndex * AnimationInstall.ClipsPerBank; i < (bankIndex + 1) * AnimationInstall.ClipsPerBank; i++)
            {
                var name = "sw_clip" + i;
                registrations.Add(new("Clip" + i, name, 1, ["SWLOR_Haks/models/hero.mdl"]));
                foreach (var suffix in new[] { "", "_in", "_out" }) text.Append(AnimationMdl.Export(project, name + suffix, bank));
            }
            text.AppendLine($"donemodel {bank}");
            Write("SWLOR_Haks/models/" + bank + ".mdl", text.ToString());
        }
        Write("design/animations/registry.json", System.Text.Json.JsonSerializer.Serialize(registrations));
        var nativeBytes = File.ReadAllBytes(target);
        rig.Name = "AnotherClip";
        var plan = AnimationInstall.Prepare(_folder, rig, [target]); plan.Apply();
        File.ReadAllBytes(target).Should().Equal(nativeBytes, "extra banks link behind the first overlay without repatching the native model");
        var first = new MdlReader().Parse(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(target)!, "an_hero.mdl")));
        first.SuperModel.Should().Be("ab_hero_004");
        var extraPath = Path.Combine(Path.GetDirectoryName(target)!, first.SuperModel + ".mdl");
        var extra = new MdlReader().Parse(File.ReadAllBytes(extraPath));
        extra.SuperModel.Should().Be("ab_hero_001");
        extra.Animations.Should().HaveCount(3);
        var allNames = Directory.EnumerateFiles(Path.GetDirectoryName(target)!, "*.mdl")
            .SelectMany(path => new MdlReader().Parse(File.ReadAllBytes(path)).Animations.Select(a => a.Name)).ToArray();
        allNames.Should().HaveCount((count + 1) * 3).And.OnlyHaveUniqueItems();
        allNames.Should().Contain("sw_clip1023_out");
        var extraBefore = File.ReadAllBytes(extraPath);
        rig.Name = "Clip800"; rig.Duration = 2;
        AnimationInstall.Prepare(_folder, rig, [target]).Apply();
        File.ReadAllBytes(extraPath).Should().Equal(extraBefore, "updating an older clip must find its own bank");
        var older = new MdlReader().Parse(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(target)!, "ab_hero_003.mdl")));
        older.Animations.Single(a => a.Name == "sw_clip800").Length.Should().Be(2);
    }

    [Test] public void NewBanksForRigsSharingANamePrefixRemainDistinctWithinOnePlan()
    {
        Write("Build/hakbuilder.json", "{\"HakList\":[{\"Path\":\"../SWLOR_Haks/models\"}]}");
        var targets = new List<string>();
        foreach (var name in new[] { "humanoid_one", "humanoid_two" })
        {
            var rig = Rig(); rig.ModelName = name; rig.Joints[0] = rig.Joints[0] with { Name = name };
            var bank = "an_" + name;
            targets.Add(Write("SWLOR_Haks/models/" + name + ".mdl", $"newmodel {name}\nsetsupermodel {name} {bank}\n" + AnimationMdl.ExportGeometry(rig) + $"donemodel {name}\n"));
            rig.ModelName = bank; rig.Joints[0] = rig.Joints[0] with { Name = bank };
            var clips = string.Concat(Enumerable.Range(0, AnimationInstall.ClipsPerBank * 3)
                .Select(i => $"newanim old{i} {bank}\nlength 1\ndoneanim old{i} {bank}\n"));
            Write("SWLOR_Haks/models/" + bank + ".mdl", $"# SWLOR authored animations for {name}\nnewmodel {bank}\nsetsupermodel {bank} NULL\n" +
                AnimationMdl.ExportGeometry(rig) + clips + $"donemodel {bank}\n");
        }
        Write("design/animations/registry.json", JsonSerializer.Serialize(new[] {
            new AnimationRegistration("Existing", "sw_existing", 1, targets.Select(t => Path.GetRelativePath(_folder, t).Replace('\\', '/')).ToArray()) }));
        var plan = AnimationInstall.Prepare(_folder, Rig(), targets); plan.Apply();
        var newBanks = plan.Changes.Where(c => Path.GetFileName(c.Path).StartsWith("ab_", StringComparison.Ordinal)).ToArray();
        newBanks.Should().HaveCount(2);
        newBanks.Select(c => Path.GetFileName(c.Path)).Should().OnlyHaveUniqueItems();
        foreach (var target in targets)
        {
            var rootBank = new MdlReader().Parse(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(target)!, "an_" + Path.GetFileName(target))));
            var bank = new MdlReader().Parse(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(target)!, rootBank.SuperModel + ".mdl")));
            bank.Animations.Should().HaveCount(3);
        }
    }

    [Test] public void InstallationRejectsTargetsOutsideConfiguredSources()
    {
        var target = InstallFixture(); var other = Write("outside.mdl", File.ReadAllText(target));
        Action act = () => AnimationInstall.Prepare(_folder, Rig(), [other]); act.Should().Throw<InvalidDataException>();
    }
    [Test] public void InstallationRejectsSameNamesWithDifferentParentage()
    {
        var target = InstallFixture();
        File.WriteAllText(target, File.ReadAllText(target).Replace("parent lower", "parent rootdummy"));
        Action act = () => AnimationInstall.Prepare(_folder, Rig(), [target]);
        act.Should().Throw<InvalidDataException>().WithMessage("*hierarchy*");
    }
    [Test] public void InstallationRemapsAModelRootRegardlessOfDeclarationCase()
    {
        var target = InstallFixture(); var project = Rig(); project.AnimationRoot = "HERO";
        AnimationInstall.Prepare(_folder, project, [target]).Apply();
        File.ReadAllText(Path.Combine(Path.GetDirectoryName(target)!, "an_hero.mdl")).Should().Contain("animroot an_hero");
    }
    [Test] public void InstallationPreservesTargetRestScaleAndAppliesAuthoredScaleDelta()
    {
        var target = InstallFixture();
        File.WriteAllText(target, File.ReadAllText(target).Replace("node dummy hand", "node dummy hand\nscale 2"));
        var project = Rig();
        project.Joints[4] = project.Joints[4] with { Rest = project.Joints[4].Rest with { Scale = .5f } };
        var pose = project.Sample(0); pose[4] = pose[4] with { Scale = .75f }; project.SetKey(0, pose);
        AnimationInstall.Prepare(_folder, project, [target]).Apply();
        var model = new MdlReader().Parse(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(target)!, "an_hero.mdl")));
        var sampled = MdlAnimationPose.Sample(model.Animations.Single(a => a.Name == "sw_wave"), 0);
        sampled["hand"].Scale.Should().Be(3);
        AnimationProject.FromModel(model).Joints.Single(j => j.Name == "hand").Rest.Scale.Should().Be(2);
    }

    [AvaloniaTest] public void GuidedViewPresentsBothStartingPathsAndKeepsTechnicalControlsInAdvanced()
    {
        var vm = new AnimationEditorDocumentViewModel(new Prompts(), new OutputLogService(), initial: Rig());
        var view = new AnimationEditorDocumentView { DataContext = vm };
        var window = new Window { Content = view, Width = 1280, Height = 900 };
        window.Show(); window.UpdateLayout();
        view.FindControl<Grid>("GuidedEditor")!.IsVisible.Should().BeTrue();
        view.FindControl<Grid>("AdvancedEditor")!.IsVisible.Should().BeFalse();
        var movement = view.FindControl<Button>("ExistingMovementButton")!;
        var poses = view.FindControl<Button>("BuildPosesButton")!;
        movement.IsEffectivelyVisible.Should().BeTrue(); poses.IsEffectivelyVisible.Should().BeTrue();
        movement.Bounds.Width.Should().Be(poses.Bounds.Width); movement.Bounds.Height.Should().Be(poses.Bounds.Height);
        vm.BuildFromPosesCommand.Execute(null); var project = vm.Project.Serialize();
        vm.IsAdvanced = true; window.UpdateLayout();
        view.FindControl<Grid>("AdvancedEditor")!.IsVisible.Should().BeTrue();
        vm.IsAdvanced = false; vm.Project.Serialize().Should().Be(project); vm.CanUndo.Should().BeTrue();
        vm.ApproveApplicationClose(); vm.OnClose(); window.Close();
    }
    [AvaloniaTest] public async Task GuidedPoseWorkflowSupportsAdjustmentsNavigationUndoAndSave()
    {
        var vm = new AnimationEditorDocumentViewModel(new Prompts(), new OutputLogService(), initial: Rig());
        var untouched = vm.Project.Serialize();
        vm.HasSelectedBodyPart.Should().BeFalse(); vm.AdjustBodyPartCommand.Execute("bend+"); vm.ResetBodyPartCommand.Execute(null);
        vm.Project.Serialize().Should().Be(untouched, "a hidden technical joint must not be edited without a body-part selection");
        vm.BuildFromPosesCommand.Execute(null);
        vm.Project.Keys.Select(key => key.Time).Should().Equal(0, 1, 2);
        vm.BeginnerStep.Should().Be(1); vm.Playhead.Should().Be(1);
        vm.BodyParts.Should().Contain(part => part.Name == "Whole body");
        var initial = vm.Project.Serialize(); vm.AdjustBodyPartCommand.Execute("bend+");
        vm.Project.Sample(1)[1].Orientation.Should().NotBe(Quaternion.Identity);
        vm.Project.Sample(0)[1].Orientation.Should().Be(Quaternion.Identity);
        vm.Project.Sample(2)[1].Orientation.Should().Be(Quaternion.Identity);
        vm.Undo(); vm.Project.Serialize().Should().Be(initial); vm.Redo();
        vm.AddPoseAfterCommand.Execute(null); vm.Playhead.Should().Be(1.5); vm.Project.Keys.Should().HaveCount(4);
        vm.NextPoseCommand.Execute(null); vm.Playhead.Should().Be(2);
        vm.PreviousPoseCommand.Execute(null); vm.Playhead.Should().Be(1.5);
        vm.RemovePoseCommand.Execute(null); vm.Project.Keys.Should().HaveCount(3);
        var path = Path.Combine(_folder, "guided.swlanim"); vm.PickSavePath = (_, _) => Task.FromResult<string?>(path);
        (await vm.TrySaveAsync()).Should().BeTrue();
        AnimationProject.Deserialize(File.ReadAllText(path)).Serialize().Should().Be(vm.Project.Serialize());
        vm.OnClose().Should().BeTrue();
    }
    [AvaloniaTest] public async Task GuidedViewOpensMountedProjectWithMoreJointsThanThePreviousPose()
    {
        var target = InstallFixture(); var archive = Path.Combine(_folder, "models.hak");
        ResourceIndexTests.WriteSingleResourceHak(archive, "hero", "mdl", File.ReadAllBytes(target));
        var resources = new ResourceIndex(null, [new("models", archive)]); await resources.InitializationTask;
        var project = AnimationProject.FromModel(new MdlReader().Parse(File.ReadAllBytes(target)));
        var path = Write("hero.swlanim", project.Serialize());
        var vm = new AnimationEditorDocumentViewModel(new Prompts(), new OutputLogService(), resources);
        var window = new Window { Content = new AnimationEditorDocumentView { DataContext = vm }, Width = 1280, Height = 900 };
        window.Show(); window.UpdateLayout();
        vm.PickOpenPath = (_, _) => Task.FromResult<string?>(path); await vm.OpenProjectCommand.ExecuteAsync(null);
        vm.Status.Should().Be("Animation project opened."); vm.Pose.Should().HaveCount(project.Joints.Count);
        vm.HasModelPreview.Should().BeFalse(); vm.ShowRigFallback.Should().BeTrue(); vm.BeginnerStep.Should().Be(1);
        ((AnimationEditorDocumentView)window.Content!).FindControl<AnimationRigControl>("BeginnerRig")!.IsVisible.Should().BeTrue();
        vm.OnClose().Should().BeTrue(); window.Close();
    }
    [AvaloniaTest] public async Task GuidedMovementsCopyInheritedMotionAndClearWhenTheCharacterChanges()
    {
        var target = InstallFixture();
        File.WriteAllText(target, File.ReadAllText(target).Replace("setsupermodel hero NULL", "setsupermodel hero base").Replace("setanimationscale 1", "setanimationscale 2"));
        var source = Rig(); source.ModelName = "base"; source.Joints[0] = source.Joints[0] with { Name = "base" };
        var last = source.Sample(0); last[1] = last[1] with { Position = Vector3.UnitX };
        source.SetKey(0, source.Sample(0)); source.SetKey(1, last);
        Write("SWLOR_Haks/sw_cr_creature/base.mdl", "newmodel base\n" + AnimationMdl.ExportGeometry(source) + AnimationMdl.Export(source, "walk") + "donemodel base\n");
        var vm = new AnimationEditorDocumentViewModel(new Prompts { ExternalChoice = ExternalChangeChoice.Reload }, new OutputLogService());
        vm.PickOpenPath = (_, _) => Task.FromResult<string?>(target); await vm.LoadRigFileCommand.ExecuteAsync(null);
        vm.StarterMovements.Should().ContainSingle().Which.Name.Should().Be("Walking");
        var before = vm.Project.Serialize(); vm.ChooseMovementCommand.Execute(null); vm.UseMovementCommand.Execute(null); vm.Stop();
        vm.Project.Keys.Should().HaveCount(21); vm.Project.Sample(1)[1].Position.X.Should().BeApproximately(2, 1e-5f);
        vm.Undo(); vm.Project.Serialize().Should().Be(before);
        var compatible = Write("compatible.swlanim", vm.Project.Serialize());
        vm.PickOpenPath = (_, _) => Task.FromResult<string?>(compatible); await vm.OpenProjectCommand.ExecuteAsync(null);
        vm.StarterMovements.Should().ContainSingle().Which.Name.Should().Be("Walking", "compatible projects retain the local supermodel folder");
        var external = vm.Project.Clone(); external.Name = "External"; File.WriteAllText(compatible, external.Serialize());
        vm.PositionX = .5m; (await vm.TrySaveAsync()).Should().BeFalse();
        vm.Project.Name.Should().Be("External"); vm.StarterMovements.Should().ContainSingle().Which.Name.Should().Be("Walking");
        var different = Rig(); different.ModelName = "other"; different.Joints[0] = different.Joints[0] with { Name = "other" };
        var projectPath = Write("other.swlanim", different.Serialize()); vm.PickOpenPath = (_, _) => Task.FromResult<string?>(projectPath);
        await vm.OpenProjectCommand.ExecuteAsync(null);
        vm.StarterMovements.Should().BeEmpty(); vm.HasStarterMovements.Should().BeFalse(); vm.HasModelPreview.Should().BeFalse();
        vm.OnClose().Should().BeTrue();
    }
    [AvaloniaTest] public async Task ModelPlaybackReusesGeometryAndScrubbingRestoresAnExactPose()
    {
        var target = InstallFixture();
        var geometry = "node trimesh visible\nparent hand\nverts 3\n0 0 0\n1 0 0\n0 1 0\nfaces 1\n0 1 2 0 0 0 0 0\nendnode\n";
        File.WriteAllText(target, File.ReadAllText(target).Replace("endmodelgeom hero", geometry + "endmodelgeom hero"));
        var vm = new AnimationEditorDocumentViewModel(new Prompts(), new OutputLogService());
        vm.PickOpenPath = (_, _) => Task.FromResult<string?>(target); await vm.LoadRigFileCommand.ExecuteAsync(null);
        vm.HasModelPreview.Should().BeTrue(); vm.ShowRigFallback.Should().BeFalse(); vm.SetPreviewVisible(true);
        vm.BuildFromPosesCommand.Execute(null); vm.AdjustBodyPartCommand.Execute("bend+");
        vm.PlayCommand.Execute(null);
        vm.IsAnimationPlaying.Should().BeTrue(); vm.PreviewAnimationName.Should().Be("authored");
        var scene = vm.PreviewScene!; var model = scene.Instances[0].Model!;
        var frames = model.Meshes.Single().AnimationFrames["authored"];
        frames.Count.Should().BeInRange(2, 240); frames.Distinct().Count().Should().BeGreaterThan(1);
        for (var i = 0; i < 150; i++) vm.Playhead = i / 100.0;
        vm.PreviewScene.Should().BeSameAs(scene); vm.PreviewScene!.Instances[0].Model.Should().BeSameAs(model);
        vm.Stop(); vm.IsAnimationPlaying.Should().BeFalse(); vm.Playhead = .5;
        vm.PreviewAnimationName.Should().BeNull(); vm.PreviewScene.Should().NotBeSameAs(scene);
        var mdl = new MdlReader().Parse(File.ReadAllBytes(target)); var node = mdl.GetMeshNodes().Single();
        var sampled = vm.Project.Joints.Select((joint, i) => (joint.Name, vm.Pose[i])).ToDictionary(p => p.Name, p => p.Item2);
        vm.PreviewScene!.Instances[0].Model!.Meshes.Single().Transform.Should().Be(MdlMeshBuilder.ComposeNodeTransform(node, sampled));
        vm.ApproveApplicationClose(); vm.OnClose();
    }
    [AvaloniaTest] public async Task CharactersWithoutMovementsCanStillStartFromPoses()
    {
        var target = InstallFixture(); var vm = new AnimationEditorDocumentViewModel(new Prompts(), new OutputLogService());
        vm.PickOpenPath = (_, _) => Task.FromResult<string?>(target); await vm.LoadRigFileCommand.ExecuteAsync(null);
        vm.ChooseMovementCommand.Execute(null); vm.ShowMovementChoices.Should().BeTrue(); vm.HasStarterMovements.Should().BeFalse();
        vm.BuildFromPosesCommand.Execute(null); vm.Project.Keys.Should().HaveCount(3);
        vm.ApproveApplicationClose(); vm.OnClose();
    }
    [Test] public void GltfWithoutAnimationsExplainsWhyItCannotBeUsed()
    {
        var path = Gltf(); var json = JsonNode.Parse(File.ReadAllText(path))!.AsObject(); json.Remove("animations");
        File.WriteAllText(path, json.ToJsonString());
        Action load = () => GltfAnimationSource.Load(path);
        load.Should().Throw<InvalidDataException>().WithMessage("No skeletal animations in this source.");
    }
    [AvaloniaTest] public void ViewLoadsAndEditsUndoRedoThroughTheDocumentContract()
    {
        var vm = new AnimationEditorDocumentViewModel(new Prompts(), new OutputLogService(), initial: Rig());
        var view = new AnimationEditorDocumentView { DataContext = vm };
        var window = new Window { Content = view, Width = 1280, Height = 900 };
        window.Show(); window.UpdateLayout();
        ViewLocator.ResolveViewType(vm.GetType()).Should().Be(typeof(AnimationEditorDocumentView));
        view.FindControl<AnimationRigControl>("Rig").Should().NotBeNull();
        vm.SelectedJoint = 4; vm.PositionZ = .25m; vm.IsDirty.Should().BeTrue();
        vm.Undo(); vm.PositionZ.Should().Be(0); vm.IsDirty.Should().BeFalse();
        vm.Redo(); vm.PositionZ.Should().Be(.25m);
        vm.ApproveApplicationClose(); vm.OnClose().Should().BeTrue();
        window.Close();
    }
    [AvaloniaTest] public async Task SaveProtectsExternalChangesAndCancelledCloseKeepsDocumentOpen()
    {
        var prompts = new Prompts(); var vm = new AnimationEditorDocumentViewModel(prompts, new OutputLogService(), initial: Rig());
        var path = Path.Combine(_folder, "wave.swlanim"); vm.PickSavePath = (_, _) => Task.FromResult<string?>(path);
        vm.PositionX = .5m; (await vm.TrySaveAsync()).Should().BeTrue(); vm.IsDirty.Should().BeFalse();
        File.AppendAllText(path, "\n "); var external = File.ReadAllBytes(path);
        vm.PositionX = 1; (await vm.TrySaveAsync()).Should().BeFalse(); File.ReadAllBytes(path).Should().Equal(external);
        vm.OnClose().Should().BeFalse(); vm.IsDirty.Should().BeTrue();
        vm.ApproveApplicationClose(); vm.OnClose();
    }
    [AvaloniaTest] public async Task SaveDoesNotAcceptAnExternalEditMadeWhileOverwriteIsBeingConfirmed()
    {
        var latest = Rig().Serialize() + "\n  ";
        var prompts = new Prompts
        {
            ExternalChoice = ExternalChangeChoice.Overwrite,
            OnExternalChange = path => File.WriteAllText(path, latest)
        };
        var vm = new AnimationEditorDocumentViewModel(prompts, new OutputLogService(), initial: Rig());
        var path = Path.Combine(_folder, "concurrent-save.swlanim");
        vm.PickSavePath = (_, _) => Task.FromResult<string?>(path);
        vm.PositionX = .5m;
        (await vm.TrySaveAsync()).Should().BeTrue();
        File.AppendAllText(path, "\n ");
        vm.PositionX = 1;
        (await vm.TrySaveAsync()).Should().BeFalse();
        File.ReadAllText(path).Should().Be(latest, "confirming one version must not silently accept a newer external edit");
        vm.Status.Should().Contain("changed while saving");
        vm.IsDirty.Should().BeTrue();
        vm.ApproveApplicationClose(); vm.OnClose();
    }
    [AvaloniaTest] public async Task SwitchingSourceClipsRequiresFreshCalibrationBeforeBake()
    {
        var path = Gltf();
        var json = JsonNode.Parse(File.ReadAllText(path))!;
        var clips = json["animations"]!.AsArray(); var second = clips[0]!.DeepClone(); second["name"] = "Second"; clips.Add(second);
        File.WriteAllText(path, json.ToJsonString());
        var vm = new AnimationEditorDocumentViewModel(new Prompts(), new OutputLogService(), initial: Rig());
        vm.PickOpenPath = (_, _) => Task.FromResult<string?>(path);
        await vm.LoadSourceCommand.ExecuteAsync(null);
        vm.Mappings.Single(m => m.Target == "rootdummy").Source = "Root";
        vm.LockCalibrationCommand.Execute(null);
        vm.SourceClip = 1;
        await vm.BakeCommand.ExecuteAsync(null);
        vm.Project.Keys.Should().BeEmpty(); vm.Status.Should().Contain("Lock calibration");
        vm.LockCalibrationCommand.Execute(null);
        await vm.BakeCommand.ExecuteAsync(null);
        vm.Project.Keys.Should().HaveCount(31);
        vm.ApproveApplicationClose(); vm.OnClose();
    }
    [AvaloniaTest] public async Task CloseSaveKeepsEditsBlockedUntilTheDocumentIsSaved()
    {
        var vm = new AnimationEditorDocumentViewModel(new Prompts { CloseChoice = UnsavedChangesChoice.Save }, new OutputLogService(), initial: Rig());
        var pickedPath = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var closed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        vm.PickSavePath = (_, _) => pickedPath.Task; vm.CloseRequested += document => closed.TrySetResult(document.OnClose());
        vm.PositionX = .5m; var original = vm.Project.Serialize();
        vm.OnClose().Should().BeFalse(); vm.IsBusy.Should().BeTrue();
        vm.PositionX = 9; vm.AnimationName = "ChangedWhileSaving"; vm.Project.Serialize().Should().Be(original);
        var path = Path.Combine(_folder, "close.swlanim"); pickedPath.SetResult(path);
        (await closed.Task.WaitAsync(TimeSpan.FromSeconds(5))).Should().BeTrue();
        vm.IsBusy.Should().BeFalse(); vm.IsDirty.Should().BeFalse();
        AnimationProject.Deserialize(File.ReadAllText(path)).Serialize().Should().Be(original);
    }
    [AvaloniaTest] public async Task OpeningAnotherProjectClearsPreviousInstallationTargets()
    {
        var path = Write("other.swlanim", Rig().Serialize());
        var vm = new AnimationEditorDocumentViewModel(new Prompts(), new OutputLogService(), initial: Rig()) { TargetPaths = "previous-model.mdl" };
        var notified = false; vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(vm.TargetPaths)) notified = true; };
        vm.PickOpenPath = (_, _) => Task.FromResult<string?>(path); await vm.OpenProjectCommand.ExecuteAsync(null);
        vm.TargetPaths.Should().BeEmpty(); notified.Should().BeTrue(); vm.OnClose().Should().BeTrue();
    }
    [AvaloniaTest] public async Task FailedRelockCannotReuseThePreviousCalibration()
    {
        var path = Gltf(); var json = JsonNode.Parse(File.ReadAllText(path))!;
        json["nodes"]!.AsArray().Add(new JsonObject { ["name"] = "Hand" });
        File.WriteAllText(path, json.ToJsonString());
        foreach (var invalidMapping in new[] { "", "Hand" })
        {
            var vm = new AnimationEditorDocumentViewModel(new Prompts(), new OutputLogService(), initial: Rig());
            vm.PickOpenPath = (_, _) => Task.FromResult<string?>(path); await vm.LoadSourceCommand.ExecuteAsync(null);
            var row = vm.Mappings.Single(m => m.Target == "rootdummy"); row.Source = "Root";
            vm.LockCalibrationCommand.Execute(null); vm.Status.Should().Contain("Calibration locked");
            row.Source = invalidMapping; vm.LockCalibrationCommand.Execute(null);
            vm.Status.Should().Contain(invalidMapping == "" ? "Map at least one" : "ambiguous");
            var before = vm.Project.Serialize(); await vm.BakeCommand.ExecuteAsync(null);
            vm.Status.Should().Contain("Lock calibration"); vm.Project.Serialize().Should().Be(before);
            row.Source = "Root"; vm.LockCalibrationCommand.Execute(null); await vm.BakeCommand.ExecuteAsync(null);
            vm.Project.Keys.Should().HaveCount(31); vm.ApproveApplicationClose(); vm.OnClose();
        }
    }
    [AvaloniaTest] public async Task RejectedDragAtKeyLimitKeepsUndoSaveAndCloseUsable()
    {
        var vm = new AnimationEditorDocumentViewModel(new Prompts(), new OutputLogService(), initial: DenseRig());
        var original = vm.Project.Serialize(); vm.Playhead = .01; vm.EditMode = "Move";
        vm.BeginDrag(); vm.Drag(Vector3.UnitX, 0);
        vm.IsDirty.Should().BeTrue("application close and Save All must include an uncommitted drag");
        vm.Project.Serialize().Should().Be(original, "dragging changes the preview until release");
        vm.EndDrag();
        vm.Project.Serialize().Should().Be(original); vm.Pose[0].Position.Should().Be(Vector3.Zero);
        vm.IsDirty.Should().BeFalse(); vm.Status.Should().Contain("limits");
        vm.Playhead = 0; vm.BeginDrag(); vm.Drag(Vector3.UnitX, 0); vm.EndDrag();
        vm.CanUndo.Should().BeTrue(); vm.Undo(); vm.Project.Serialize().Should().Be(original);
        vm.PickSavePath = (_, _) => Task.FromResult<string?>(Path.Combine(_folder, "dense.swlanim"));
        (await vm.TrySaveAsync()).Should().BeTrue(); vm.OnClose().Should().BeTrue();
    }
    [AvaloniaTest] public async Task PoseEditsAndHistoryInvalidateCalibration()
    {
        var source = Gltf();
        foreach (var operation in new[] { "position", "rotation", "scale", "reset", "paste", "undo", "redo", "drag" })
        {
            var vm = new AnimationEditorDocumentViewModel(new Prompts(), new OutputLogService(), initial: Rig());
            vm.PickOpenPath = (_, _) => Task.FromResult<string?>(source);
            await vm.LoadSourceCommand.ExecuteAsync(null);
            vm.Mappings.Single(m => m.Target == "rootdummy").Source = "Root";
            vm.PositionX = 1; vm.CopyPoseCommand.Execute(null); vm.PositionX = .5m;
            if (operation == "redo") vm.Undo();
            vm.LockCalibrationCommand.Execute(null);
            switch (operation)
            {
                case "position": vm.PositionX = 2; break;
                case "rotation": vm.RotationZ = 30; vm.RotateCommand.Execute(null); break;
                case "scale": vm.JointScale = 2; break;
                case "reset": vm.ResetPoseCommand.Execute(null); break;
                case "paste": vm.PastePoseCommand.Execute(null); break;
                case "undo": vm.Undo(); break;
                case "redo": vm.Redo(); break;
                case "drag": vm.EditMode = "Move"; vm.BeginDrag(); vm.Drag(Vector3.UnitX, 0); vm.EndDrag(); break;
            }
            var edited = vm.Project.Serialize();
            await vm.BakeCommand.ExecuteAsync(null);
            vm.Status.Should().Contain("Lock calibration", operation);
            vm.Project.Serialize().Should().Be(edited, operation);
            vm.ApproveApplicationClose(); vm.OnClose();
        }
    }
    [AvaloniaTest] public async Task PackedHakRigPrefillsItsLooseSourceAndNeverAnArchive()
    {
        var target = InstallFixture(); var archive = Path.Combine(_folder, "models.hak");
        ResourceIndexTests.WriteSingleResourceHak(archive, "hero", "mdl", File.ReadAllBytes(target));
        var resources = new ResourceIndex(null, [new("models", archive)]);
        await resources.InitializationTask;
        var vm = new AnimationEditorDocumentViewModel(new Prompts(), new OutputLogService(), resources, _folder) { RigResource = "hero" };
        await vm.LoadRigCommand.ExecuteAsync(null);
        vm.TargetPaths.Should().Be(Path.GetFullPath(target));
        File.Delete(target); vm.TargetPaths = "stale target";
        await vm.LoadRigCommand.ExecuteAsync(null);
        vm.TargetPaths.Should().BeEmpty(); vm.Project.ModelName.Should().Be("hero");
        vm.OnClose().Should().BeTrue();
    }
    [AvaloniaTest] public async Task LooseRigOutsideHakSourcesDoesNotBecomeAnInstallationTarget()
    {
        var target = InstallFixture();
        var loose = Write("loose/outsider.mdl", File.ReadAllText(target).Replace("hero", "outsider"));
        var vm = new AnimationEditorDocumentViewModel(new Prompts(), new OutputLogService(), repositoryRoot: _folder);
        vm.PickOpenPath = (_, _) => Task.FromResult<string?>(loose);
        await vm.LoadRigFileCommand.ExecuteAsync(null);
        vm.Project.ModelName.Should().Be("outsider"); vm.TargetPaths.Should().BeEmpty();
        vm.OnClose().Should().BeTrue();
    }

    [AvaloniaTest] public async Task StarterMovementsUseMountedWinnersBeyondEightAnimationBanks()
    {
        var target = InstallFixture();
        File.WriteAllText(target, File.ReadAllText(target).Replace("setsupermodel hero NULL", "setsupermodel hero bank0"));
        for (var i = 0; i < 10; i++)
        {
            var name = "bank" + i; var parent = i == 9 ? "base" : "bank" + (i + 1);
            var rig = Rig(); rig.ModelName = name; rig.Joints[0] = rig.Joints[0] with { Name = name };
            Write("SWLOR_Haks/sw_cr_creature/" + name + ".mdl", $"newmodel {name}\nsetsupermodel {name} {parent}\n" +
                AnimationMdl.ExportGeometry(rig) + $"donemodel {name}\n");
        }
        string Movement(float distance)
        {
            var rig = Rig(); rig.ModelName = "base"; rig.Joints[0] = rig.Joints[0] with { Name = "base" };
            var last = rig.Sample(0); last[1] = last[1] with { Position = new Vector3(distance, 0, 0) };
            rig.SetKey(0, rig.Sample(0)); rig.SetKey(1, last);
            return "newmodel base\n" + AnimationMdl.ExportGeometry(rig) + AnimationMdl.Export(rig, "walk") + "donemodel base\n";
        }
        Write("SWLOR_Haks/sw_cr_creature/base.mdl", Movement(9));
        var archive = Path.Combine(_folder, "winner.hak");
        ResourceIndexTests.WriteSingleResourceHak(archive, "base", "mdl", Encoding.ASCII.GetBytes(Movement(2)));
        var resources = new ResourceIndex(null, [new("winner", archive)]); await resources.InitializationTask;
        var vm = new AnimationEditorDocumentViewModel(new Prompts(), new OutputLogService(), resources);
        vm.PickOpenPath = (_, _) => Task.FromResult<string?>(target); await vm.LoadRigFileCommand.ExecuteAsync(null);
        vm.StarterMovements.Should().ContainSingle(); vm.UseMovementCommand.Execute(null); vm.Stop();
        vm.Project.Sample(1)[1].Position.X.Should().BeApproximately(2, .00001f,
            "the mounted base overrides the adjacent file, even after ten banks");
        vm.ApproveApplicationClose(); vm.OnClose();
    }
    [AvaloniaTest] public async Task ExternalReloadRetainsOnlyAMatchingPreviewModel()
    {
        var modelPath = InstallFixture(); var projectPath = Path.Combine(_folder, "project.swlanim");
        var vm = new AnimationEditorDocumentViewModel(new Prompts { ExternalChoice = ExternalChangeChoice.Reload }, new OutputLogService());
        vm.PickOpenPath = (_, _) => Task.FromResult<string?>(modelPath);
        vm.PickSavePath = (_, _) => Task.FromResult<string?>(projectPath);
        await vm.LoadRigFileCommand.ExecuteAsync(null); vm.SetPreviewVisible(true); vm.PreviewScene.Should().NotBeNull();
        (await vm.TrySaveAsync()).Should().BeTrue();
        var changed = vm.Project.Clone(); changed.Name = "OtherPose"; File.WriteAllText(projectPath, changed.Serialize());
        vm.PositionX = .5m; (await vm.TrySaveAsync()).Should().BeFalse();
        vm.PreviewScene.Should().NotBeNull("a matching local model can survive a pose-only reload");
        changed.ModelName = "other"; changed.Joints[0] = changed.Joints[0] with { Name = "other" };
        File.WriteAllText(projectPath, changed.Serialize());
        vm.PositionX = 1; (await vm.TrySaveAsync()).Should().BeFalse();
        vm.Project.ModelName.Should().Be("other"); vm.PreviewScene.Should().BeNull(); vm.IsDirty.Should().BeFalse();
        vm.OnClose().Should().BeTrue();
    }
    [AvaloniaTest] public async Task ExternalReloadDiscardsPosesCopiedFromThePreviousRig()
    {
        var path = Path.Combine(_folder, "project.swlanim");
        var vm = new AnimationEditorDocumentViewModel(new Prompts { ExternalChoice = ExternalChangeChoice.Reload },
            new OutputLogService(), initial: Rig());
        vm.PickSavePath = (_, _) => Task.FromResult<string?>(path);
        vm.PositionX = .5m; vm.CopyPoseCommand.Execute(null); (await vm.TrySaveAsync()).Should().BeTrue();
        var changed = Rig(); changed.Joints[4] = changed.Joints[4] with { Name = "foot", Parent = 1 };
        File.WriteAllText(path, changed.Serialize()); vm.PositionX = 1; vm.TargetPaths = "previous-model.mdl";
        (await vm.TrySaveAsync()).Should().BeFalse(); vm.Project.Joints[4].Name.Should().Be("foot");
        vm.TargetPaths.Should().BeEmpty();
        vm.PastePoseCommand.Execute(null);
        vm.Project.Keys.Should().BeEmpty(); vm.IsDirty.Should().BeFalse(); vm.OnClose().Should().BeTrue();
    }
    [AvaloniaTest] public async Task ExternalReloadRetainsAttachedPreviewWhenMountedModelCannotBeUsed()
    {
        var modelPath = InstallFixture(); var original = File.ReadAllText(modelPath);
        var projectPath = Path.Combine(_folder, "project.swlanim");
        foreach (var mounted in new[] { original.Replace("parent lower", "parent upper"),
                     original.Replace("node dummy hand", "node dummy hand\nscale 2"), "invalid model data" })
        {
            var archive = Path.Combine(_folder, Guid.NewGuid().ToString("N") + ".hak");
            ResourceIndexTests.WriteSingleResourceHak(archive, "hero", "mdl", Encoding.UTF8.GetBytes(mounted));
            var resources = new ResourceIndex(null, [new("models", archive)]);
            await resources.InitializationTask;
            var vm = new AnimationEditorDocumentViewModel(new Prompts { ExternalChoice = ExternalChangeChoice.Reload },
                new OutputLogService(), resources, initial: Rig());
            vm.PickOpenPath = (_, _) => Task.FromResult<string?>(modelPath);
            vm.PickSavePath = (_, _) => Task.FromResult<string?>(projectPath);
            await vm.AttachPreviewCommand.ExecuteAsync(null);
            vm.SetPreviewVisible(true); vm.PreviewScene.Should().NotBeNull();
            (await vm.TrySaveAsync()).Should().BeTrue();
            var changed = vm.Project.Clone(); changed.Name = "OtherPose";
            File.WriteAllText(projectPath, changed.Serialize());
            vm.PositionX = .5m; (await vm.TrySaveAsync()).Should().BeFalse();
            vm.Project.Name.Should().Be("OtherPose");
            vm.PreviewScene.Should().NotBeNull("the compatible attached model must survive mounted validation or parsing failures");
            vm.IsDirty.Should().BeFalse(); vm.OnClose().Should().BeTrue();
        }
    }
    [AvaloniaTest] public async Task PreviewAttachmentRequiresMatchingParentsAndRestTransforms()
    {
        var modelPath = InstallFixture(); var original = File.ReadAllText(modelPath);
        var vm = new AnimationEditorDocumentViewModel(new Prompts(), new OutputLogService(), initial: Rig());
        vm.PickOpenPath = (_, _) => Task.FromResult<string?>(modelPath); vm.SetPreviewVisible(true);
        foreach (var invalid in new[] { original.Replace("parent lower", "parent upper"), original.Replace("node dummy hand", "node dummy hand\nscale 2") })
        {
            File.WriteAllText(modelPath, invalid); await vm.AttachPreviewCommand.ExecuteAsync(null);
            vm.PreviewScene.Should().BeNull(); vm.Status.Should().Contain("hierarchy");
        }
        File.WriteAllText(modelPath, original); await vm.AttachPreviewCommand.ExecuteAsync(null);
        vm.PreviewScene.Should().NotBeNull(); vm.OnClose().Should().BeTrue();
    }
    [AvaloniaTest] public async Task RootMotionScaleDoesNotDistortTheCalibrationSkeleton()
    {
        var path = Gltf(); var vm = new AnimationEditorDocumentViewModel(new Prompts(), new OutputLogService(), initial: Rig());
        vm.PickOpenPath = (_, _) => Task.FromResult<string?>(path); await vm.LoadSourceCommand.ExecuteAsync(null); vm.Playhead = .5;
        var positions = vm.SourcePositions(); vm.RootScale = 100;
        vm.SourcePositions().Should().Equal(positions);
        Vector3.Distance(positions[0], positions[1]).Should().BeApproximately(1, 1e-5f);
        vm.ApproveApplicationClose(); vm.OnClose();
    }
    [AvaloniaTest] public async Task StaticGltfPoseBakesAsOneHeldKeyForOneSecond()
    {
        var path = Gltf(); var json = JsonNode.Parse(File.ReadAllText(path))!;
        json["accessors"]![0]!["count"] = 1; json["accessors"]![1]!["count"] = 1;
        File.WriteAllText(path, json.ToJsonString());
        var vm = new AnimationEditorDocumentViewModel(new Prompts(), new OutputLogService(), initial: Rig());
        vm.PickOpenPath = (_, _) => Task.FromResult<string?>(path); await vm.LoadSourceCommand.ExecuteAsync(null);
        vm.Duration.Should().Be(1); vm.Mappings.Single(m => m.Target == "rootdummy").Source = "Root";
        vm.PositionX = .5m; var reference = vm.Pose[0].Position;
        vm.LockCalibrationCommand.Execute(null); await vm.BakeCommand.ExecuteAsync(null);
        vm.Project.Keys.Should().HaveCount(1); vm.Project.Duration.Should().Be(1);
        vm.Project.Sample(.75f)[0].Position.Should().Be(reference);
        vm.ApproveApplicationClose(); vm.OnClose();
    }
    private sealed class Prompts : IEditorPromptService
    {
        public ExternalChangeChoice ExternalChoice { get; init; } = ExternalChangeChoice.Cancel;
        public UnsavedChangesChoice CloseChoice { get; init; } = UnsavedChangesChoice.Cancel;
        public Action<string>? OnExternalChange { get; init; }
        public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string filePath)
        {
            OnExternalChange?.Invoke(filePath);
            return Task.FromResult(ExternalChoice);
        }
        public Task<UnsavedChangesChoice> ConfirmCloseAsync(string documentTitle) => Task.FromResult(CloseChoice);
        public Task<bool> ConfirmDestructiveAsync(string headline, string message, string confirmLabel) => Task.FromResult(false);
        public Task<string?> PromptForTextAsync(string headline, string message, string initialValue, string confirmLabel) => Task.FromResult<string?>(null);
    }
}
