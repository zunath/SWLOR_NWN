using System.Numerics;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.AnimationDrafts;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Animation;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests;

public class HeavyVibrobladeChoreographyTests
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

    [Test]
    public void EveryActiveHeavyAbilityHasItsOwnTwoHandedRecipe()
    {
        var recipes = ChoreographyAuthor.Read(File.ReadAllText(Path.Combine(Root,
            "design/animations/heavy-vibroblade/choreographies.json")));
        using var inventory = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "design/animations/active-abilities.json")));
        var ids = inventory.RootElement.EnumerateArray()
            .Where(entry => entry.GetProperty("Category").GetString() == "Heavy Vibroblade")
            .Select(entry => entry.GetProperty("Id").GetString()).ToArray();
        recipes.Select(recipe => recipe.Id).Should().BeEquivalentTo(ids);
        recipes.Should().HaveCount(12);
        foreach (var recipe in recipes)
        {
            recipe.Beats.Skip(1).SkipLast(1).Should().OnlyContain(beat => beat.SourceAnimation.StartsWith("2h"),
                "both arms must come from a native heavy-weapon grip");
            recipe.Beats.Should().OnlyContain(beat => beat.LeftHand == null && beat.RightHand == null,
                "independent hand IK can separate the supporting hand from the greatsword hilt");
            recipe.Beats.Should().NotContain(beat => beat.SourceAnimation == "2hstab",
                "these motions must not append the previously reported extra stab");
        }
        foreach (var id in new[] { "AbsoluteDefense", "BastionStance", "BlazingSpikes", "Flash", "Rampart", "SoulDevourer", "SoulStorm" })
            foreach (var source in recipes.Single(recipe => recipe.Id == id).Beats
                         .Where(beat => beat.SourceAnimation.StartsWith("2hslash")).GroupBy(beat => beat.SourceAnimation))
                source.Select(beat => beat.SourceTime).Distinct().Should().ContainSingle(
                    "a held native blade pose is useful for wards, but buffs must not traverse a damaging slash");
    }

    [Test]
    public void HeavyMotionsRetainNativeArmGripsAndRecoverWithoutGroundPenetration()
    {
        var modelPath = Path.Combine(Root, "SWLOR_Haks/sw_cr_creature/a_ba.mdl");
        if (!File.Exists(modelPath)) Assert.Ignore("Initialize the HAK submodule to validate native heavy-weapon poses.");
        var model = new MdlReader().Parse(File.ReadAllBytes(modelPath));
        var bind = MdlAnimationPose.BindPose(model);
        var trajectories = new Dictionary<string, Vector3[]>();
        foreach (var recipe in ChoreographyAuthor.Read(File.ReadAllText(Path.Combine(Root,
            "design/animations/heavy-vibroblade/choreographies.json"))))
        {
            // Bake validates interpolated feet at 120 Hz and exact native idle recovery.
            var project = ChoreographyAuthor.Bake(model, recipe);
            var hands = new[] { "lhand_g", "rhand_g" }.Select(name => project.Joints.FindIndex(joint => joint.Name == name)).ToArray();
            trajectories[recipe.Id] = Enumerable.Range(1, 19).SelectMany(frame =>
            {
                var world = AnimationRig.World(project.Joints, project.Sample(project.Duration * frame / 20f));
                return hands.Select(hand => world[hand].Translation);
            }).ToArray();
            if (recipe.Id is "Earthshatter" or "SoulStrike")
            {
                // The editable rig contains the grip joint, not the render-only weaponr attachment.
                // Test the actual sword-hand trajectory rather than inferring direction from a clip name.
                var low = recipe.Beats.Single(beat => beat.Label == (recipe.Id == "Earthshatter"
                    ? "Drive the blade into the ground ahead" : "Draw the blade low before the rising cut"));
                var high = recipe.Beats.Single(beat => beat.Label == (recipe.Id == "Earthshatter"
                    ? "Raise both hands and blade overhead" : "Drive the cut upward through the target"));
                var swordHand = project.Joints.FindIndex(joint => joint.Name == "rhand_g");
                swordHand.Should().BeGreaterThanOrEqualTo(0);
                var lowWorld = AnimationRig.World(project.Joints, project.Sample(low.Time))[swordHand];
                var highWorld = AnimationRig.World(project.Joints, project.Sample(high.Time))[swordHand];
                (highWorld.Translation.Z - lowWorld.Translation.Z).Should().BeGreaterThan(.5f);
                (recipe.Id == "Earthshatter" ? high.Time < low.Time : low.Time < high.Time).Should().BeTrue();
            }
            project.Keys.Count.Should().BeLessThanOrEqualTo((int)Math.Ceiling(recipe.Duration * 120) + recipe.Beats.Length);
            foreach (var beat in recipe.Beats)
            {
                var source = model.Animations.Single(animation => animation.Name == beat.SourceAnimation);
                var expected = MdlAnimationPose.Sample(source, source.Length * beat.SourceTime, bind);
                var pose = project.Sample(beat.Time);
                foreach (var joint in new[] { "lbicep_g", "lforearm_g", "lhand_g", "rbicep_g", "rforearm_g", "rhand_g" })
                {
                    var actual = pose[project.Joints.FindIndex(item => item.Name == joint)];
                    Math.Abs(Quaternion.Dot(actual.Orientation, expected[joint].Orientation)).Should().BeGreaterThan(.99999f,
                        recipe.Id + " must preserve the native two-handed arm chain at " + beat.Label);
                    Vector3.Distance(actual.Position, expected[joint].Position).Should().BeLessThan(.00001f);
                }
            }
        }
        var ids = trajectories.Keys.ToArray();
        for (var i = 0; i < ids.Length; i++)
        for (var j = i + 1; j < ids.Length; j++)
            Math.Sqrt(trajectories[ids[i]].Zip(trajectories[ids[j]], Vector3.DistanceSquared).Average())
                .Should().BeGreaterThan(.05, ids[i] + " and " + ids[j] + " must have distinct normalized hand paths");
    }
}
