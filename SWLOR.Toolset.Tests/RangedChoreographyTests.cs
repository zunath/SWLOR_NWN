using System.Numerics;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.AnimationDrafts;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Animation;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests;

public class RangedChoreographyTests
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

    private static Choreography[] Recipes(string folder) => ChoreographyAuthor.Read(
        File.ReadAllText(Path.Combine(Root, "design/animations", folder, "choreographies.json")));

    private static MdlModel Model(string name) => new MdlReader().Parse(File.ReadAllBytes(
        Path.Combine(Root, "SWLOR_Haks/sw_cr_creature", name + ".mdl")));

    private static AnimationProject Bake(Choreography recipe)
    {
        var sources = recipe.Beats.Where(beat => beat.SourceModel != null).Select(beat => beat.SourceModel!).Distinct()
            .ToDictionary(name => name, Model);
        return ChoreographyAuthor.Bake(Model("a_ba"), recipe, sources);
    }

    private static Vector3 Hand(AnimationProject project, string name, float time)
    {
        var joint = project.Joints.FindIndex(j => j.Name == name);
        return AnimationRig.World(project.Joints, project.Sample(time))[joint].Translation;
    }

    [TestCase("Pistol", "pistol")]
    [TestCase("Rifle", "rifle")]
    [TestCase("Throwing", "throwing")]
    [TestCase("Espionage", "espionage")]
    [TestCase("Beast Mastery", "beast-mastery")]
    public void CategoriesCoverCurrentAbilitiesWithNativeWristsRecoveryAndDifferentMotionPaths(string category, string folder)
    {
        var recipes = Recipes(folder);
        using var inventory = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "design/animations/active-abilities.json")));
        recipes.Select(recipe => recipe.Id).Should().BeEquivalentTo(inventory.RootElement.EnumerateArray()
            .Where(entry => entry.GetProperty("Category").GetString() == category).Select(entry => entry.GetProperty("Id").GetString()));
        var model = Model("a_ba");
        var bind = MdlAnimationPose.BindPose(model);
        var sources = recipes.SelectMany(recipe => recipe.Beats).Where(beat => beat.SourceModel != null)
            .Select(beat => beat.SourceModel!).Distinct().ToDictionary(name => name, Model);
        var trajectories = new Dictionary<string, Vector3[]>();
        foreach (var recipe in recipes)
        {
            var project = ChoreographyAuthor.Bake(model, recipe, sources);
            var hands = new[] { "lhand_g", "rhand_g" }.Select(name => project.Joints.FindIndex(joint => joint.Name == name)).ToArray();
            foreach (var beat in recipe.Beats)
            {
                var owner = beat.SourceModel == null ? model : sources[beat.SourceModel];
                var source = owner.Animations.Single(animation => animation.Name == beat.SourceAnimation);
                var expected = MdlAnimationPose.Sample(source, source.Length * beat.SourceTime, bind);
                foreach (var hand in hands)
                    Math.Abs(Quaternion.Dot(project.Sample(beat.Time)[hand].Orientation,
                        expected[project.Joints[hand].Name].Orientation)).Should().BeGreaterThan(.99999f,
                        recipe.Id + " must preserve the native local wrist grip at " + beat.Label);
            }
            var first = project.Sample(0);
            var last = project.Sample(project.Duration);
            for (var joint = 0; joint < first.Length; joint++)
            {
                Vector3.Distance(first[joint].Position, last[joint].Position).Should().BeLessThan(.00001f);
                Math.Abs(Quaternion.Dot(first[joint].Orientation, last[joint].Orientation)).Should().BeGreaterThan(.99999f);
            }
            trajectories[recipe.Id] = Enumerable.Range(1, 39).SelectMany(frame =>
            {
                var world = AnimationRig.World(project.Joints, project.Sample(project.Duration * frame / 40f));
                return hands.Select(hand => world[hand].Translation);
            }).ToArray();
        }
        var ids = trajectories.Keys.ToArray();
        for (var i = 0; i < ids.Length; i++)
        for (var j = i + 1; j < ids.Length; j++)
            Math.Sqrt(trajectories[ids[i]].Zip(trajectories[ids[j]], Vector3.DistanceSquared).Average())
                .Should().BeGreaterThan(.025, ids[i] + " and " + ids[j] + " need different normalized hand paths, not merely different durations or names");
    }

    [TestCase("PiercingToss", .2f, .49f)]
    [TestCase("ConcussiveToss", .48f, .8f)]
    [TestCase("FlashToss", .2f, .53f)]
    [TestCase("PinningToss", .24f, .57f)]
    [TestCase("SeveringToss", .22f, .56f)]
    public void ThrowsHaveVisibleForwardDeliveryInsteadOfAStaticReadyPose(string id, float loadTime, float releaseTime)
    {
        var project = Bake(Recipes("throwing").Single(recipe => recipe.Id == id));
        var load = Hand(project, "rhand_g", loadTime);
        var release = Hand(project, "rhand_g", releaseTime);
        release.Y.Should().BeGreaterThan(load.Y + .3f);
        Vector3.Distance(load, release).Should().BeGreaterThan(.4f);
    }

    [Test]
    public void PerfectFlurryHasThreeForwardCastsAndTwoCompleteHandResets()
    {
        var project = Bake(Recipes("throwing").Single(recipe => recipe.Id == "PerfectFlurry"));
        foreach (var (load, release) in new[] { (.2f, .43f), (.65f, .89f), (1.12f, 1.38f) })
            Hand(project, "rhand_g", release).Y.Should().BeGreaterThan(Hand(project, "rhand_g", load).Y + .3f);
        Hand(project, "rhand_g", .43f).Z.Should().BeGreaterThan(Hand(project, "rhand_g", .89f).Z + .2f,
            "the flurry alternates high and low delivery rather than repeating one identical pose");
    }

    [Test]
    public void TrapsUseDifferentArmingActionsAfterReachingTheGround()
    {
        var razor = Bake(Recipes("espionage").Single(recipe => recipe.Id == "RazorTrap"));
        var shock = Bake(Recipes("espionage").Single(recipe => recipe.Id == "ShockTrap"));
        Hand(razor, "lhand_g", 1.18f).X.Should().BeLessThan(Hand(razor, "lhand_g", .8f).X - .2f,
            "razor trap lays its trigger line sideways");
        Hand(shock, "lhand_g", 1.23f).Z.Should().BeGreaterThan(Hand(shock, "lhand_g", 1.02f).Z + .1f,
            "shock trap lifts between two distinct arming taps");
        Hand(shock, "lhand_g", 1.44f).Z.Should().BeLessThan(Hand(shock, "lhand_g", 1.23f).Z - .1f);
        Hand(razor, "lhand_g", .8f).Z.Should().BeLessThan(.5f);
        Hand(shock, "lhand_g", 1.02f).Z.Should().BeLessThan(.5f);
    }

    [Test]
    public void CompanionCareSeparatesOfferingFromSoothingAndRepeatedTamingSignals()
    {
        var recipes = Recipes("beast-mastery");
        var reward = Bake(recipes.Single(recipe => recipe.Id == "Reward"));
        // The low offering target is clamped to the native arm's reachable length.
        // Measure the baked pouch-to-offering travel, rather than the requested IK target.
        var pouch = Hand(reward, "lhand_g", .35f);
        foreach (var time in new[] { 1.12f, 1.55f })
            Hand(reward, "lhand_g", time).Y.Should().BeGreaterThan(pouch.Y + .30f,
                "reward must extend visibly from the pouch and hold the offering");
        Hand(reward, "lhand_g", 1.55f).Y.Should().BeGreaterThan(Hand(reward, "lhand_g", 1.96f).Y + .10f,
            "the offering hand must withdraw after the hold");
        var soothe = Bake(recipes.Single(recipe => recipe.Id == "SoothePet"));
        Hand(soothe, "lhand_g", .38f).Z.Should().BeGreaterThan(Hand(soothe, "lhand_g", .95f).Z + .2f,
            "soothing lowers the reassurance signal");
        Hand(soothe, "lhand_g", 1.43f).X.Should().BeLessThan(Hand(soothe, "lhand_g", .95f).X - .15f,
            "soothing finishes by easing outward, not simply replaying a throw");
        var tame = Bake(recipes.Single(recipe => recipe.Id == "Tame"));
        foreach (var (raised, lowered) in new[] { (.85f, 1.45f), (2.05f, 2.65f) })
            Hand(tame, "lhand_g", raised).Z.Should().BeGreaterThan(Hand(tame, "lhand_g", lowered).Z + .2f,
                "the taming invitation needs two distinct slow calming descents");
    }
}
