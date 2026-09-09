using NUnit.Framework;
using System.Numerics;
using System.Text.Json;
using FluentAssertions;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Animation;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests;

public class FishingAnimationTests
{
    private static string Root
    {
        get
        {
            var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln"))) root = root.Parent;
            return root?.FullName ?? throw new DirectoryNotFoundException();
        }
    }

    /// <summary>Checks source foot clearance before optional installed-bank duration and idle-exit validation.</summary>
    [TestCase(6)] [TestCase(7)] [TestCase(8)]
    public void ActivityDurationMatchesInstalledClipsOnBothHumanoidRigs(int seconds)
    {
        var clip = Fishing.GetFishingAnimation(seconds);
        clip.Duration.Should().Be(seconds);
        var registrations = JsonSerializer.Deserialize<AnimationRegistration[]>(
            File.ReadAllText(Path.Combine(Root, "design/animations/registry.json")))!;
        var registration = registrations.Single(r => r.AnimationName == clip.Name);
        registration.Targets.Should().HaveCount(2);
        var project = AnimationProject.Deserialize(File.ReadAllText(Path.Combine(Root, registration.ProjectPath!)));
        project.Duration.Should().Be(seconds);
        // A full-motion sample checks feet throughout interpolated cast and reel phases.
        for (var time = 0f; time <= seconds; time += 1f / 60)
        {
            var world = AnimationRig.World(project.Joints, project.Sample(time));
            foreach (var foot in new[] { "lfoot_g", "rfoot_g" })
                world[project.Joints.FindIndex(j => j.Name == foot)].Translation.Z.Should().BeGreaterThan(.125f);
        }
        foreach (var targetPath in registration.Targets)
        {
            var fullPath = Path.Combine(Root, targetPath);
            if (!File.Exists(fullPath)) Assert.Ignore("Initialize the HAK submodule to validate installed assets.");
            var target = new MdlReader().Parse(File.ReadAllBytes(fullPath));
            var bankBytes = File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(fullPath)!, target.SuperModel + ".mdl"));
            AnimationBankSource.IsBinary(bankBytes).Should().BeTrue();
            var bank = new MdlReader().Parse(bankBytes);
            var installed = bank.Animations.Single(a => a.Name == clip.Name);
            installed.Length.Should().Be(seconds);
            bank.Animations.Should().Contain(a => a.Name == clip.StartName).And.Contain(a => a.Name == clip.EndName);
            var neutral = MdlAnimationPose.SampleIdle(target, name =>
            {
                var source = AnimationInstall.FindTargetSource(Root, name);
                return source == null ? null : new MdlReader().Parse(File.ReadAllBytes(source));
            }, maxDepth: 32);
            var start = MdlAnimationPose.Sample(installed, 0, MdlAnimationPose.BindPose(target));
            var finish = MdlAnimationPose.Sample(installed, seconds, MdlAnimationPose.BindPose(target));
            foreach (var joint in AnimationProject.FromModel(target).Joints.Where(j => j.Parent >= 0))
            {
                Vector3.Distance(start[joint.Name].Position, finish[joint.Name].Position).Should().BeLessThan(.0001f);
                Math.Abs(Quaternion.Dot(start[joint.Name].Orientation, finish[joint.Name].Orientation)).Should().BeGreaterThan(.9999f);
            }
            // Retargeting preserves relative motion; the exit restores each model's own native idle.
            var exit = bank.Animations.Single(a => a.Name == clip.EndName);
            {
                var pose = MdlAnimationPose.Sample(exit, exit.Length, MdlAnimationPose.BindPose(target));
                foreach (var joint in AnimationProject.FromModel(target).Joints.Where(j => j.Parent >= 0))
                {
                    var expected = neutral.TryGetValue(joint.Name, out var value) ? value : joint.Rest;
                    Vector3.Distance(pose[joint.Name].Position * target.Scale, expected.Position).Should().BeLessThan(.0002f);
                    Math.Abs(Quaternion.Dot(pose[joint.Name].Orientation, expected.Orientation)).Should().BeGreaterThan(.9999f);
                }
            }
        }

    }
}
