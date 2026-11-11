using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using Nwn.Formats.NativeModels;
using Nwn.Preview.Scene;
using SWLOR.Toolset.Domain.Animation;

namespace SWLOR.Toolset.Tests;

public class PerformancePropMotionTests
{
    private static readonly string[] States = ["Loop", "Low", "Mid", "High", "Flourish"];
    // Measured on the native bow hair and the fingerboard-to-bridge string surface.
    private static readonly Vector3 BowHairStart = new(.0023665f, .0244655f, .0301885f);
    private static readonly Vector3 BowHairEnd = new(-.344768f, .213982f, .4864385f);
    private static readonly Vector3 ViolinStringContact = new(.17881617f, .04933465f, .245f);
    private static readonly Vector3 ViolinStringDirection = Vector3.Normalize(new(.57010706f, .13283711f, .81076029f));
    private static readonly HashSet<string> TorchJoints = ["lbicep_g", "lforearm_g", "lforearm", "lhand_g", "lhand", "lshould_g"];
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
    public void ViolinBowHairStaysOnTheStringThroughoutTheStroke(string state)
    {
        var project = Project("Violin", state);
        var hand = project.Joints.FindIndex(j => j.Name == "rhand");
        var violin = project.Joints.FindIndex(j => j.Name == "lhand");
        var support = project.Joints.FindIndex(j => j.Name == "lhand_g");
        var elbow = project.Joints.FindIndex(j => j.Name == "rforearm_g");
        var native = new MdlReader().Parse(File.ReadAllBytes(Path.Combine(Root, "SWLOR_Haks/sw_cr_creature/a_ba_non_combat.mdl")));
        var torch = MdlAnimationPose.Sample(native.Animations.Single(a => a.Name == "torchl"), 0, MdlAnimationPose.BindPose(native));
        var worlds = Enumerable.Range(0, (int)MathF.Ceiling(project.Duration * 120) + 1)
            .Select(i =>
            {
                var pose = project.Sample(MathF.Min(i / 120f, project.Duration));
                Vector3.Distance(AnimationRig.World(project.Joints, pose)[support].Translation, new(-.25f, .22f, 1.26f))
                    .Should().BeLessThan(.001f, "the draft's left-arm inputs must remain unchanged");
                // BowedStrings keeps the native left torch hold, unlike Vocals. Test
                // contact with the rendered violin rather than the hidden draft arm.
                for (var joint = 0; joint < project.Joints.Count; joint++)
                    if (TorchJoints.Contains(project.Joints[joint].Name))
                        pose[joint] = pose[joint] with { Orientation = torch[project.Joints[joint].Name].Orientation };
                return AnimationRig.World(project.Joints, pose);
            }).ToArray();
        var fractions = new List<float>();
        foreach (var world in worlds)
        {
            var start = Vector3.Transform(BowHairStart, world[hand]);
            var end = Vector3.Transform(BowHairEnd, world[hand]);
            var direction = Vector3.Normalize(end - start);
            var target = Vector3.Transform(ViolinStringContact, world[violin]);
            var along = Vector3.Dot(target - start, direction);
            (target - start - direction * along).Length().Should().BeLessThan(.003f,
                "moving the bow along its own axis is insufficient if its hair misses the strings");
            var fraction = along / Vector3.Distance(start, end);
            fraction.Should().BeInRange(.15f, .90f, "the string must remain on playable hair, clear of frog and tip");
            fractions.Add(fraction);
            MathF.Abs(Vector3.Dot(direction, Vector3.Normalize(Vector3.TransformNormal(ViolinStringDirection, world[violin]))))
                .Should().BeLessThan(.03f, "the draw must cross the string instead of following it");
            world[elbow].Translation.X.Should().BeGreaterThan(.20f,
                "the bow elbow must stay beside the player rather than crossing the chest");
        }
        (fractions.Max() - fractions.Min()).Should().BeGreaterThan(.35f, "the bow must visibly draw in both directions");
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
