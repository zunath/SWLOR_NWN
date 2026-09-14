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
    [Test]
    public void LaterInstallsSortTheWholeBankWithoutChangingClipContents()
    {
        const string first = "newanim zebra bank\n  length 2\ndoneanim zebra bank\n";
        const string second = "newanim apple bank\n  length 7\ndoneanim apple bank\n";
        var source = "newmodel bank\n" + first + second + "donemodel bank\n";
        var sorted = AnimationInstall.SortAnimationBlocks(source);
        sorted.Should().Be("newmodel bank\n" + second + first + "donemodel bank\n");
        AnimationInstall.SortAnimationBlocks(sorted).Should().Be(sorted);
    }

    [Test]
    public void BankOrderingUsesLowercaseAsciiWithoutChangingMixedCaseBlockPayloads()
    {
        const string letter = "newanim SW_criplshot Bank\n  length 7\ndoneanim SW_criplshot Bank\n";
        const string underscore = "newanim sw_crip_defe Bank\n  length 2\ndoneanim sw_crip_defe Bank\n";
        const string prefix = "newanim Sw_Aimedshot Bank\n  length 3\ndoneanim Sw_Aimedshot Bank\n";
        var source = "newmodel Bank\n" + letter + underscore + prefix + "donemodel Bank\n";
        var expected = "newmodel Bank\n" + prefix + underscore + letter + "donemodel Bank\n";

        AnimationInstall.SortAnimationBlocks(source).Should().Be(expected);
        AnimationInstall.SortAnimationBlocks(expected).Should().Be(expected);
    }

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
            var inherited = target;
            MdlModel? bank = null;
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (!string.IsNullOrEmpty(inherited.SuperModel) && !inherited.SuperModel.Equals("NULL", StringComparison.OrdinalIgnoreCase))
            {
                visited.Add(inherited.SuperModel).Should().BeTrue("the animation chain must be acyclic");
                visited.Count.Should().BeLessThanOrEqualTo(AnimationInstall.MaximumModelChainDepth);
                var bankPath = AnimationInstall.FindTargetSource(Root, inherited.SuperModel);
                bankPath.Should().NotBeNull();
                var bankBytes = File.ReadAllBytes(bankPath!);
                AnimationBankSource.IsBinary(bankBytes).Should().BeTrue();
                inherited = new MdlReader().Parse(bankBytes);
                if (inherited.Animations.Any(animation => animation.Name == clip.Name)) { bank = inherited; break; }
            }
            bank.Should().NotBeNull("fishing may occupy any owned bank after library rebalancing");
            bank!.Animations.Select(animation => animation.Name.ToLowerInvariant())
                .Should().BeInAscendingOrder(StringComparer.Ordinal);
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
