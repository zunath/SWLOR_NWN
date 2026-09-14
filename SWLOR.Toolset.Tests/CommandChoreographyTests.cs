using System.Numerics;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.AnimationDrafts;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Animation;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests;

public class CommandChoreographyTests
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

    [TestCase("Leadership", "leadership")]
    [TestCase("Mimicry", "mimicry")]
    public void CommandsHaveCompleteDistinctMotionsAndNativeGrips(string category, string folder)
    {
        var recipes = ChoreographyAuthor.Read(File.ReadAllText(Path.Combine(Root, "design/animations", folder, "choreographies.json")));
        using var inventory = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "design/animations/active-abilities.json")));
        recipes.Select(recipe => recipe.Id).Should().BeEquivalentTo(inventory.RootElement.EnumerateArray()
            .Where(entry => entry.GetProperty("Category").GetString() == category).Select(entry => entry.GetProperty("Id").GetString()));
        var model = new MdlReader().Parse(File.ReadAllBytes(Path.Combine(Root, "SWLOR_Haks/sw_cr_creature/a_ba.mdl")));
        var bind = MdlAnimationPose.BindPose(model);
        var trajectories = new Dictionary<string, Vector3[]>();
        foreach (var recipe in recipes)
        {
            var project = ChoreographyAuthor.Bake(model, recipe); // Includes 120 Hz floor and neutral recovery validation.
            var hands = new[] { "lhand_g", "rhand_g" }.Select(name => project.Joints.FindIndex(joint => joint.Name == name)).ToArray();
            foreach (var beat in recipe.Beats)
            {
                var source = model.Animations.Single(animation => animation.Name == beat.SourceAnimation);
                var expected = MdlAnimationPose.Sample(source, source.Length * beat.SourceTime, bind);
                foreach (var hand in hands)
                    Math.Abs(Quaternion.Dot(project.Sample(beat.Time)[hand].Orientation,
                        expected[project.Joints[hand].Name].Orientation)).Should().BeGreaterThan(.99999f,
                        recipe.Id + " must preserve the native local grip without a wrist twist");
            }
            trajectories[recipe.Id] = Enumerable.Range(1, 19).SelectMany(frame =>
            {
                var world = AnimationRig.World(project.Joints, project.Sample(project.Duration * frame / 20f));
                return hands.Select(hand => world[hand].Translation);
            }).ToArray();
        }
        var ids = trajectories.Keys.ToArray();
        for (var i = 0; i < ids.Length; i++)
        for (var j = i + 1; j < ids.Length; j++)
            Math.Sqrt(trajectories[ids[i]].Zip(trajectories[ids[j]], Vector3.DistanceSquared).Average())
                .Should().BeGreaterThan(.04, ids[i] + " and " + ids[j] + " must have distinct normalized hand paths");
    }

}
