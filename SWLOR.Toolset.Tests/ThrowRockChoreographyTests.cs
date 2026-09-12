using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.AnimationDrafts;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Animation;

namespace SWLOR.Toolset.Tests;

public class ThrowRockChoreographyTests
{
    [Test]
    public void ThrowKeepsItsFootingAndGripWithoutSnappingTheForearms()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        var root = directory?.FullName ?? throw new DirectoryNotFoundException();
        var model = new MdlReader().Parse(File.ReadAllBytes(Path.Combine(root, "SWLOR_Haks/sw_cr_creature/a_ba.mdl")));
        var recipe = ChoreographyAuthor.Read(File.ReadAllText(Path.Combine(root,
            "design/animations/force/choreographies.json"))).Single(value => value.Id == "ThrowRock");
        var project = ChoreographyAuthor.Bake(model, recipe);
        int Joint(string name) => project.Joints.FindIndex(joint => joint.Name == name);
        static float Degrees(Quaternion first, Quaternion second) => 2 * MathF.Acos(Math.Clamp(
            Math.Abs(Quaternion.Dot(Quaternion.Normalize(first), Quaternion.Normalize(second))), 0, 1)) * 180 / MathF.PI;

        var gather = project.Sample(.5f);
        var planted = AnimationRig.World(project.Joints, gather);
        var feet = new[] { Joint("lfoot_g"), Joint("rfoot_g") };
        var hands = new[] { Joint("lhand_g"), Joint("rhand_g") };
        var arms = new[] { "lbicep_g", "rbicep_g", "lforearm_g", "rforearm_g" }.Select(Joint).ToArray();
        const float step = 1 / 120f;
        var previous = project.Sample(0);
        for (var time = step; time < project.Duration; time += step)
        {
            var pose = project.Sample(time);
            foreach (var arm in arms)
                (Degrees(previous[arm].Orientation, pose[arm].Orientation) / step).Should().BeLessThan(500,
                    $"{project.Joints[arm].Name} at {time:0.000}s must not snap during the throw");
            if (time >= .5f && time <= 2.25f)
            {
                var world = AnimationRig.World(project.Joints, pose);
                foreach (var foot in feet)
                    Vector3.Distance(world[foot].Translation, planted[foot].Translation).Should().BeLessThan(.002f,
                        "the caster must keep the same footing while gathering and releasing the rock");
                foreach (var hand in hands)
                    Degrees(pose[hand].Orientation, gather[hand].Orientation).Should().BeLessThan(.1f,
                        "the directed gesture must retain its local grip through the release");
            }
            previous = pose;
        }
    }
}
