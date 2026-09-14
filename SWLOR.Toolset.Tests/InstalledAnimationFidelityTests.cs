using System.Numerics;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.AnimationDrafts;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Animation;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests;

[TestFixture]
public class InstalledAnimationFidelityTests
{
    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        }
    }

    [TestCase("a_ba")]
    [TestCase("a_fa")]
    public void EveryInstalledClipMatchesItsCanonicalRetargetedMotion(string modelName)
    {
        var cache = new Dictionary<string, MdlModel>(StringComparer.OrdinalIgnoreCase);
        long loadedBytes = 0;
        MdlModel? Load(string name)
        {
            if (cache.TryGetValue(name, out var existing)) return existing;
            var path = AnimationInstall.FindTargetSource(Root, name);
            if (path == null) return null;
            var bytes = InstalledMotionLibrary.ReadBank(path, ref loadedBytes);
            AnimationBankSource.IsBinary(bytes).Should().BeTrue($"the shipped model {name} must be compiled");
            var model = new MdlReader().Parse(bytes);
            cache.Add(name, model);
            return model;
        }

        var target = Load(modelName) ?? throw new FileNotFoundException("Initialize the HAK submodule before validating installed motion.");
        var targetRig = AnimationProject.FromModel(target);
        var bind = MdlAnimationPose.BindPose(target);
        var installed = new InstalledMotionLibrary(target, Load);
        var neutral = MdlAnimationPose.SampleIdle(target, Load, maxDepth: AnimationInstall.MaximumModelChainDepth);
        var entries = JsonSerializer.Deserialize<AnimationRegistration[]>(File.ReadAllText(
            Path.Combine(Root, "design", "animations", "registry.json")))!;
        entries.Should().NotBeEmpty();

        foreach (var entry in entries)
        {
            entry.Targets.Should().Contain($"SWLOR_Haks/sw_cr_creature/{modelName}.mdl");
            var project = AnimationProject.Deserialize(File.ReadAllText(Path.Combine(Root, entry.ProjectPath!)));
            var expected = project.Clone();
            if (expected.Keys.Count == 0) expected.SetKey(0, project.Sample(0));
            // Retarget every stored key before interpolation, as the installer does. Comparing only
            // male source poses would miss female bind offsets and inherited animation scale errors.
            for (var i = 0; i < project.Joints.Count; i++)
            {
                var sourceJoint = project.Joints[i];
                var destination = sourceJoint.Parent < 0 ? targetRig.Joints[0] : targetRig.Joints.Single(j =>
                    j.Name.Equals(sourceJoint.Name, StringComparison.OrdinalIgnoreCase));
                foreach (var key in expected.Keys)
                {
                    var value = key.Pose[i];
                    key.Pose[i] = value with
                    {
                        Position = (destination.Rest.Position + value.Position - sourceJoint.Rest.Position) / target.Scale,
                        Orientation = Quaternion.Normalize(destination.Rest.Orientation * Quaternion.Inverse(sourceJoint.Rest.Orientation) * value.Orientation),
                        Scale = destination.Rest.Scale * value.Scale / sourceJoint.Rest.Scale
                    };
                }
            }

            var main = installed.Resolve(entry.AnimationName);
            main.Animation.Length.Should().BeApproximately(project.Duration, .0001f);
            foreach (var fraction in new[] { 0f, .25f, .5f, .75f, 1f })
                Compare(main.Animation, main.Owner.Name, project.Duration * fraction,
                    expected.Sample(project.Duration * fraction), "main");

            var enter = installed.Resolve(entry.AnimationName + "_in");
            enter.Animation.Length.Should().BeApproximately(.001f, .0001f);
            Compare(enter.Animation, enter.Owner.Name, 0, expected.Sample(0), "entry");
            var exit = installed.Resolve(entry.AnimationName + "_out");
            exit.Animation.Length.Should().BeApproximately(.2f, .0001f);
            Compare(exit.Animation, exit.Owner.Name, 0, expected.Sample(project.Duration), "exit start");
            var idle = project.Joints.Select(j =>
            {
                var destination = j.Parent < 0 ? targetRig.Joints[0] : targetRig.Joints.Single(t =>
                    t.Name.Equals(j.Name, StringComparison.OrdinalIgnoreCase));
                var pose = neutral.TryGetValue(destination.Name, out var value) ? value : destination.Rest;
                return pose with { Position = pose.Position / target.Scale };
            }).ToArray();
            Compare(exit.Animation, exit.Owner.Name, exit.Animation.Length, idle, "neutral recovery");

            void Compare(MdlAnimation animation, string owner, float time, PosedNode[] wanted, string phase)
            {
                var actual = MdlAnimationPose.Sample(animation, time, bind);
                for (var i = 0; i < project.Joints.Count; i++)
                {
                    var joint = project.Joints[i];
                    var name = joint.Parent < 0 ? owner : joint.Name;
                    actual.ContainsKey(name).Should().BeTrue($"{entry.Name}/{modelName}/{phase} must contain {name}");
                    var value = actual[name];
                    // Half a millimetre in game units and ~0.16 degrees permit native float/axis-angle
                    // quantization, while rejecting wrong poses, wrist flips and retarget scale drift.
                    var distance = Vector3.Distance(value.Position, wanted[i].Position) * target.Scale;
                    var rotation = Math.Abs(Quaternion.Dot(Quaternion.Normalize(value.Orientation), Quaternion.Normalize(wanted[i].Orientation)));
                    if (distance > .0005f || rotation < .999999f || Math.Abs(value.Scale - wanted[i].Scale) > .0001f)
                        Assert.Fail($"{entry.Name}/{modelName}/{phase} at {time:0.####}s joint {name}: " +
                            $"position error {distance}, rotation dot {rotation}, scale {value.Scale} expected {wanted[i].Scale}.");
                }
            }
        }
    }
}
