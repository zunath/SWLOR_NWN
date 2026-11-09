using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Animation;

namespace SWLOR.Toolset.Tests;

public class PerformancePropMotionTests
{
    private static readonly string[] States = ["Loop", "Low", "Mid", "High", "Flourish"];
    private static readonly Vector3 BowDirection = Vector3.Normalize(new(-.6f, .35f, .72f));
    private static readonly Vector3 DrumNormal = Vector3.Normalize(new(.0660850566f, -.2372743147f, .9691922745f));
    private static readonly Vector3 RibbonDirection = Vector3.Normalize(new(.7167083083f, -.6646320291f, -.2111716523f));

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln"))) directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        }
    }

    private static AnimationProject Project(string family, string state) => AnimationProject.Deserialize(
        File.ReadAllText(Path.Combine(Root, "design/animations/performance", family + state + ".swlanim")));

    [TestCaseSource(nameof(States))]
    public void ViolinBowSlidesAlongItsLengthWithVisibleVerticalTravel(string state)
    {
        var project = Project("Violin", state);
        var hand = project.Joints.FindIndex(j => j.Name == "rhand");
        var violin = project.Joints.FindIndex(j => j.Name == "lhand_g");
        var elbow = project.Joints.FindIndex(j => j.Name == "rforearm_g");
        var worlds = project.Keys.Select(k => AnimationRig.World(project.Joints, k.Pose)).ToArray();
        var origin = worlds[0][hand].Translation;
        foreach (var world in worlds)
        {
            var delta = world[hand].Translation - origin;
            (delta - BowDirection * Vector3.Dot(delta, BowDirection)).Length().Should().BeLessThan(.001f,
                "a bow stroke must slide along the hair, rather than waving across the strings");
            Vector3.Distance(world[violin].Translation, new(-.25f, .22f, 1.26f)).Should().BeLessThan(.001f,
                "the accepted violin placement must stay put");
            Vector3.Distance(Vector3.TransformNormal(Vector3.UnitY, world[hand]),
                Vector3.TransformNormal(Vector3.UnitY, worlds[0][hand])).Should().BeLessThan(.001f,
                "the bow must retain its angle through each stroke");
            world[elbow].Translation.X.Should().BeGreaterThan(.20f,
                "the bow elbow must stay beside the player rather than crossing the chest");
            world[hand].Translation.Z.Should().BeLessThan(worlds[0][hand].Translation.Z + .001f,
                "the draw must use the bow hair above the grip, rather than lifting its frog past the strings");
        }
        (worlds.Max(w => w[hand].Translation.Z) - worlds.Min(w => w[hand].Translation.Z)).Should().BeGreaterThan(.15f);
        var wrist = project.Joints.FindIndex(j => j.Name == "rhand_g");
        foreach (var key in project.Keys) WristAngle(project, key.Pose[wrist].Orientation, wrist).Should().BeLessThan(60);
    }

    [TestCaseSource(nameof(States))]
    public void MicrophoneWristsFollowTheirForearmsAndTheMicrophoneStaysAtTheLips(string state)
    {
        var project = Project("Vocals", state);
        var wrist = project.Joints.FindIndex(j => j.Name == "rhand_g");
        var microphone = project.Joints.FindIndex(j => j.Name == "lhand_g");
        foreach (var key in project.Keys)
        {
            WristAngle(project, key.Pose[wrist].Orientation, wrist).Should().BeLessThan(.1f,
                "the free hand must not inherit a nearly 180-degree sword-aim twist");
            WristAngle(project, key.Pose[microphone].Orientation, microphone).Should().BeLessThan(60,
                "forearm roll must carry the gripping hand's twist");
            var world = AnimationRig.World(project.Joints, key.Pose);
            Vector3.Distance(world[microphone].Translation, new(-.11f, .22f, 1.64f)).Should().BeLessThan(.001f);
        }
    }

    [TestCaseSource(nameof(States))]
    public void TambourinePlayingFacePointsOutwardAndItsRibbonsHangDown(string state)
    {
        var project = Project("Tambourine", state);
        var hand = project.Joints.FindIndex(j => j.Name == "rhand");
        var tap = project.Joints.FindIndex(j => j.Name == "lhand_g");
        var wrist = project.Joints.FindIndex(j => j.Name == "rhand_g");
        var distances = new List<float>();
        foreach (var key in project.Keys)
        {
            var world = AnimationRig.World(project.Joints, key.Pose);
            var normal = Vector3.Normalize(Vector3.TransformNormal(DrumNormal, world[hand]));
            normal.Y.Should().BeGreaterThan(.95f, "the playing face must point toward the audience and tapping hand");
            Vector3.Normalize(Vector3.TransformNormal(RibbonDirection, world[hand])).Z.Should().BeLessThan(-.99f,
                "the actual ribbon strips run diagonally across model space");
            var centre = Vector3.Transform(new(-.0744766926f, -.1460790976f, -.0100544039f), world[hand]);
            distances.Add(Vector3.Dot(world[tap].Translation - centre, normal));
            var radial = world[tap].Translation - centre - normal * distances[^1];
            radial.Length().Should().BeLessThan(.11f, "the tapping palm must approach the skin inside its rim");
            Vector3.Dot(Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitX, world[tap])), normal)
                .Should().BeLessThan(-.6f, "the palm must face the playing surface");
            Vector3.Dot(Vector3.Normalize(Vector3.TransformNormal(-Vector3.UnitZ, world[tap])), normal)
                .Should().BeLessThan(-.3f, "the tapping fingers must approach the skin instead of pointing away from it");
            WristAngle(project, key.Pose[tap].Orientation, tap).Should().BeLessThan(60,
                "palm contact must not bend the tapping wrist backward");
            WristAngle(project, key.Pose[wrist].Orientation, wrist).Should().BeLessThan(60,
                "the hand attachment must carry the drum rotation rather than bending the wrist backward");
        }
        distances.Min().Should().BeInRange(.035f, .055f,
            "the wrist must approach closely enough for the palm to touch the skin without passing through it");
        (distances.Max() - distances.Min()).Should().BeGreaterThan(.10f, "the free hand must approach and recoil from the outward face");
    }

    [TestCase("Vocals")]
    [TestCase("Violin")]
    [TestCase("Tambourine")]
    public void AccentsReturnToTheLoopHoldWithoutAnArmSnap(string family)
    {
        var loop = Project(family, "Loop");
        foreach (var state in States)
        {
            var project = Project(family, state);
            foreach (var time in new[] { 0f, project.Duration })
            {
                var pose = project.Sample(time);
                var ready = loop.Sample(0);
                for (var joint = 0; joint < pose.Length; joint++)
                {
                    Vector3.Distance(pose[joint].Position, ready[joint].Position).Should().BeLessThan(.0001f);
                    MathF.Abs(Quaternion.Dot(pose[joint].Orientation, ready[joint].Orientation)).Should().BeGreaterThan(.99999f);
                }
            }
        }
    }

    private static float WristAngle(AnimationProject project, Quaternion actual, int wrist) =>
        2 * MathF.Acos(Math.Clamp(MathF.Abs(Quaternion.Dot(Quaternion.Normalize(actual),
            project.Joints[wrist].Rest.Orientation)), 0, 1)) * 180 / MathF.PI;
}
