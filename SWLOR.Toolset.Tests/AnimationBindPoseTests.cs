using System.Numerics;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Animation;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests;

public class AnimationBindPoseTests
{
    [Test]
    public void ExportLeavesUnchangedBoneLengthsAndScalesToTheWearer()
    {
        var project = Rig();
        var pose = project.Sample(0);
        pose[1] = pose[1] with { Position = pose[1].Position + new Vector3(0, 0, -.1f) };
        pose[2] = pose[2] with { Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, .2f) };
        project.SetKey(0, pose);
        project.SetKey(1, project.Joints.Select(j => j.Rest).ToArray());
        var animation = Parse(project, AnimationMdl.Export(project)).Animations.Single();
        var nodes = Nodes(animation.GeometryRoot!).ToDictionary(n => n.Name);
        foreach (var name in new[] { "neck_g", "head_g" })
        {
            nodes[name].PositionValues.Should().BeEmpty();
            nodes[name].ScaleValues.Should().BeEmpty();
            nodes[name].OrientationValues.Should().NotBeEmpty();
        }
        nodes["rootdummy"].PositionValues.Should().HaveCount(2);

        var wearer = Rig();
        wearer.Joints[2] = wearer.Joints[2] with { Rest = new(new(0, -.04f, .3f), Quaternion.Identity, .9f) };
        wearer.Joints[3] = wearer.Joints[3] with { Rest = new(new(0, .01f, .05f), Quaternion.Identity, 1.1f) };
        var bind = MdlAnimationPose.BindPose(Parse(wearer, ""));
        foreach (var time in new[] { 0f, .5f, 1f })
        {
            var actual = MdlAnimationPose.Sample(animation, time, bind);
            foreach (var name in new[] { "neck_g", "head_g" })
            {
                actual[name].Position.Should().Be(bind[name].Position);
                actual[name].Scale.Should().Be(bind[name].Scale);
            }
        }
    }

    [Test]
    public void ExportPreservesIntentionalConstantOffsetsAndScaleChangesOnAttachmentNodes()
    {
        var project = Rig();
        var pose = project.Sample(0);
        pose[4] = pose[4] with { Position = pose[4].Position + Vector3.UnitY * .02f, Scale = 1.2f };
        project.SetKey(0, pose);
        var animation = Parse(project, AnimationMdl.Export(project)).Animations.Single();
        var carrier = Nodes(animation.GeometryRoot!).Single(n => n.Name == "rhand");
        carrier.PositionValues.Should().ContainSingle().Which.Should().Be(pose[4].Position);
        carrier.ScaleValues.Should().ContainSingle().Which.Should().Be(1.2f);
    }

    [Test]
    public void ExportNeverKeysASkeletonBoneOffsetOrScale()
    {
        // A bone offset belongs to the wearer's appearance. Keying one here would
        // latch the authoring rig's proportions onto every other body.
        var project = Rig();
        var pose = project.Sample(0);
        pose[3] = pose[3] with { Position = pose[3].Position + Vector3.UnitY * .02f, Scale = 1.2f };
        project.SetKey(0, pose);
        var animation = Parse(project, AnimationMdl.Export(project)).Animations.Single();
        var head = Nodes(animation.GeometryRoot!).Single(n => n.Name == "head_g");
        head.PositionValues.Should().BeEmpty();
        head.ScaleValues.Should().BeEmpty();
        head.OrientationValues.Should().NotBeEmpty();
    }

    [Test]
    public void InstallationUsesDestinationBindForMainEntryAndExitTracks()
    {
        var folder = Path.Combine(Path.GetTempPath(), "swlor-animation-binds-" + Guid.NewGuid().ToString("N"));
        try
        {
            var source = Rig();
            var targetRig = Rig();
            targetRig.Joints[2] = targetRig.Joints[2] with
                { Rest = new(new(0, -.04f, .3f), Quaternion.Identity, .9f) };
            targetRig.Joints[3] = targetRig.Joints[3] with
                { Rest = new(new(0, .01f, .05f), Quaternion.Identity, 1.1f) };
            var target = Path.Combine(folder, "SWLOR_Haks", "models", "hero.mdl");
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.WriteAllText(target, "newmodel hero\nsetsupermodel hero NULL\nsetanimationscale 0.62\n" +
                AnimationMdl.ExportGeometry(targetRig) + "donemodel hero\n");
            Directory.CreateDirectory(Path.Combine(folder, "Build"));
            File.WriteAllText(Path.Combine(folder, "Build", "hakbuilder.json"),
                "{\"HakList\":[{\"Path\":\"../SWLOR_Haks/models\"}]}");
            var pose = source.Sample(0);
            pose[1] = pose[1] with { Position = pose[1].Position + new Vector3(0, 0, -.1f) };
            pose[2] = pose[2] with { Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, .2f) };
            source.SetKey(0, pose);
            source.SetKey(1, source.Joints.Select(j => j.Rest).ToArray());
            AnimationInstall.Prepare(folder, source, [target]).Apply();
            var bank = new MdlReader().Parse(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(target)!, "an_hero.mdl")));
            bank.Animations.Should().HaveCount(3);
            foreach (var animation in bank.Animations)
            foreach (var node in Nodes(animation.GeometryRoot!).Where(n => n.Name is "neck_g" or "head_g"))
            {
                node.PositionValues.Should().BeEmpty(animation.Name + "/" + node.Name);
                node.ScaleValues.Should().BeEmpty(animation.Name + "/" + node.Name);
            }
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

    private static AnimationProject Rig() => new()
    {
        Name = "TestGesture", ModelName = "hero", AnimationRoot = "rootdummy",
        Joints = [new("hero", -1, new(Vector3.Zero, Quaternion.Identity, 1)),
            new("rootdummy", 0, new(Vector3.UnitZ, Quaternion.Identity, 1)),
            new("neck_g", 1, new(new(0, -.08f, .47f), Quaternion.Identity, 1)),
            new("head_g", 2, new(new(0, .02f, .08f), Quaternion.Identity, 1)),
            new("rhand", 1, new(new(.15f, 0, .3f), Quaternion.Identity, 1))]
    };

    private static MdlModel Parse(AnimationProject rig, string clips) => new MdlReader().Parse(Encoding.UTF8.GetBytes(
        $"newmodel {rig.ModelName}\n" + AnimationMdl.ExportGeometry(rig) + clips + $"donemodel {rig.ModelName}\n"));

    private static IEnumerable<MdlNode> Nodes(MdlNode node) => new[] { node }.Concat(node.Children.SelectMany(Nodes));
}
