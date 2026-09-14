using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.AnimationDrafts;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Animation;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests;

public class BerserkerStanceChoreographyTests
{
    [Test]
    public void BracedStanceRetainsNativeElbowBendsAndGripsWhileShiftingItsWeight()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln"))) directory = directory.Parent;
        var root = directory?.FullName ?? throw new DirectoryNotFoundException();
        var model = new MdlReader().Parse(File.ReadAllBytes(Path.Combine(root, "SWLOR_Haks/sw_cr_creature/a_ba.mdl")));
        var recipe = ChoreographyAuthor.Read(File.ReadAllText(Path.Combine(root,
            "design/animations/vibroblade/choreographies.json"))).Single(entry => entry.Id == "BerserkerStance");
        var project = ChoreographyAuthor.Bake(model, recipe);
        var ready = MdlAnimationPose.Sample(model.Animations.Single(clip => clip.Name == "1hreadyr"), 0,
            MdlAnimationPose.BindPose(model));
        int Joint(string name) => project.Joints.FindIndex(joint => joint.Name == name);
        static float Degrees(Quaternion a, Quaternion b) => 2 * MathF.Acos(Math.Clamp(
            Math.Abs(Quaternion.Dot(Quaternion.Normalize(a), Quaternion.Normalize(b))), 0, 1)) * 180 / MathF.PI;

        // Hand positions alone cannot detect the inverted elbows reported in game.
        // Check local forearm rotation throughout the hold, including between keys.
        var holdStart = recipe.Beats[1].Time;
        var holdEnd = recipe.Beats[^2].Time;
        for (var time = holdStart; time <= holdEnd; time += 1 / 120f)
        {
            var pose = project.Sample(time);
            foreach (var name in new[] { "lforearm_g", "rforearm_g", "lhand_g", "rhand_g" })
                Degrees(pose[Joint(name)].Orientation, ready[name].Orientation).Should().BeLessThan(15,
                    $"{name} at {time:0.000}s must keep its native elbow bend/grip instead of rolling backwards");
        }

        var torso = Joint("torso_g");
        var pelvis = Joint("rootdummy");
        var startWorld = AnimationRig.World(project.Joints, project.Sample(holdStart));
        project.Keys.Max(key => Degrees(key.Pose[torso].Orientation, ready["torso_g"].Orientation))
            .Should().BeGreaterThan(12, "the stance should still lean into its aggressive posture");
        project.Keys.Where(key => key.Time >= holdStart && key.Time <= holdEnd)
            .Max(key => Vector3.Distance(startWorld[pelvis].Translation,
                AnimationRig.World(project.Joints, key.Pose)[pelvis].Translation))
            .Should().BeGreaterThan(.035f, "fixing the arms must retain the deliberate body weight shift");
    }
}
