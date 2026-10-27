using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.AnimationDrafts;
using Nwn.Formats.NativeModels;
using Nwn.Preview.Scene;
using SWLOR.Toolset.Domain.Animation;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests;

public class TwinPistolTravelChoreographyTests
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

    private static MdlModel Model(string name)
    {
        var path = Path.Combine(Root, "SWLOR_Haks/sw_cr_creature", name + ".mdl");
        if (!File.Exists(path)) Assert.Ignore("Initialize native models.");
        return new MdlReader().Parse(File.ReadAllBytes(path));
    }

    private static Choreography Recipe(string file, string id) => ChoreographyAuthor.Read(File.ReadAllText(
        Path.Combine(Root, "design/animations/twin-pistol", file))).Single(recipe => recipe.Id == id);

    private static AnimationProject Bake(Choreography recipe)
    {
        var sources = recipe.Beats.SelectMany(beat => new[] { beat.SourceModel, beat.GripModel }).OfType<string>().Distinct()
            .ToDictionary(name => name, Model);
        return ChoreographyAuthor.Bake(Model("a_ba"), recipe, sources);
    }

    private static float Degrees(Quaternion a, Quaternion b) =>
        2 * MathF.Acos(Math.Clamp(Math.Abs(Quaternion.Dot(Quaternion.Normalize(a), Quaternion.Normalize(b))), 0, 1)) * 180 / MathF.PI;

    private static float ThighSwing(AnimationProject project, float start, float end)
    {
        var thigh = project.Joints.FindIndex(joint => joint.Name == "lthigh_g");
        var samples = Enumerable.Range(0, 60).Select(i => project.Sample(start + (end - start) * i / 59f)[thigh].Orientation).ToArray();
        return samples.SelectMany(a => samples.Select(b => Degrees(a, b))).Max();
    }

    [Test]
    public void PistolRushTravelRunsOnNativeStridesInsteadOfGlidingLikeBlasterSlide()
    {
        var recipe = Recipe("motions.json", "PistolRushTravel");
        var project = Bake(recipe);
        var stride = recipe.Beats.Where(beat => beat.SourceAnimation == "run").ToArray();
        var run = ThighSwing(project, stride[0].Time, stride[^1].Time);
        var slide = ThighSwing(Bake(Recipe("motions.json", "BlasterSlideTravel")), .18f, 1.25f);
        run.Should().BeGreaterThan(30, "Pistol Rush must show running legs, not a held lean");
        run.Should().BeGreaterThan(slide * 3, "the rush must read differently from Blaster Slide's held slide");
        var first = project.Sample(0); var last = project.Sample(project.Duration);
        for (var joint = 0; joint < first.Length; joint++)
            Math.Abs(Quaternion.Dot(first[joint].Orientation, last[joint].Orientation)).Should().BeGreaterThan(.99999f);
    }

    [Test]
    public void PistolRushTravelKeepsBothPistolsAimedWithTheNativeTwinPistolGrip()
    {
        var recipe = Recipe("motions.json", "PistolRushTravel");
        var project = Bake(recipe);
        var cycle = Recipe("motions.json", "TwinPistolCycle");
        var reference = Bake(cycle);
        var aim = cycle.Beats.Single(beat => beat.Label == "Both pistols on target").Time;
        var med = Model("a_ba_med_weap");
        var bowshot = med.Animations.Single(animation => animation.Name == "bowshot");
        var bind = MdlAnimationPose.BindPose(Model("a_ba"));
        foreach (var hook in new[] { "lhand", "rhand" })
        {
            var index = project.Joints.FindIndex(joint => joint.Name == hook);
            var target = AnimationRig.World(reference.Joints, reference.Sample(aim))[index];
            // Weapons attached to the hand hooks point their barrels along the hook's -Z axis.
            var aimed = -Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitZ, target));
            foreach (var beat in recipe.Beats.Where(beat => beat.GripAnimation != null))
            {
                var world = AnimationRig.World(project.Joints, project.Sample(beat.Time))[index];
                var barrel = -Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitZ, world));
                (MathF.Acos(Math.Clamp(Vector3.Dot(barrel, aimed), -1, 1)) * 180 / MathF.PI)
                    .Should().BeLessThan(30, $"{hook} must keep the pistol aimed ahead at {beat.Label}");
                barrel.Y.Should().BeGreaterThan(.85f, $"{hook} must point its pistol forward at {beat.Label}");
                var grip = MdlAnimationPose.Sample(bowshot, bowshot.Length * beat.GripTime, bind)[hook + "_g"].Orientation;
                Degrees(project.Sample(beat.Time)[project.Joints.FindIndex(joint => joint.Name == hook + "_g")].Orientation, grip)
                    .Should().BeLessThan(.1f, $"{hook} must keep the native local twin-pistol grip at {beat.Label}");
            }
        }
    }

    [Test]
    public void GripSourceIsOptionalAndRequiresADirectedHand()
    {
        ChoreographyBeat[] beats = [new(0, "Start", "pause1"), new(.5f, "Run", "run", GripAnimation: "bowshot"), new(1, "Finish", "pause1")];
        Action read = () => ChoreographyAuthor.Read(System.Text.Json.JsonSerializer.Serialize(new[] { new Choreography("Bad", "Test", 1, beats) }));
        read.Should().Throw<InvalidDataException>().WithMessage("*grip source*");
        System.Text.Json.JsonSerializer.Serialize(new ChoreographyBeat(0, "Start", "pause1"), BulkMotionAuthor.Json)
            .Should().NotContain("Grip", "omitted grip fields must not change the hash of older recipes");
    }
}
