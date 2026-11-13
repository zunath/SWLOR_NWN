using System.Numerics;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.AnimationDrafts;
using Nwn.Formats.NativeModels;
using Nwn.Preview.Scene;
using SWLOR.Toolset.Domain.Animation;

namespace SWLOR.Toolset.Tests;

public class PerformancePropMotionTests
{
    private static readonly string[] States = ["Loop", "Low", "Mid", "High", "Flourish"];
    // The Wookiee sets have no middle accent: the body bank is full, and the server plays the low one for it.
    private static readonly string[] WookieeStates = ["Loop", "Low", "High", "Flourish"];
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
    public void MicrophoneWristsFollowTheirForearmsAndTheMicrophoneStaysAtTheLips(string state) =>
        MicrophoneStaysAtTheLips("Vocals", state, 1.63f);

    [TestCaseSource(nameof(States))]
    public void LowMouthMicrophoneSitsTenCentimetresLowerAndStaysThere(string state) =>
        MicrophoneStaysAtTheLips("VocalsLowMouth", state, 1.53f);

    [TestCaseSource(nameof(WookieeStates))]
    public void MidMouthMicrophoneSitsFiveCentimetresLowerAndStaysThere(string state) =>
        MicrophoneStaysAtTheLips("VocalsMidMouth", state, 1.58f);

    [TestCaseSource(nameof(States))]
    public void LowMouthSingingDiffersFromOrdinarySingingOnlyInTheMicrophoneArm(string state) =>
        VariantMovesOnly("Vocals", "VocalsLowMouth", state, "lbicep_g", "lforearm_g", "lhand_g");

    [TestCaseSource(nameof(WookieeStates))]
    public void MidMouthSingingDiffersFromOrdinarySingingOnlyInTheMicrophoneArm(string state) =>
        VariantMovesOnly("Vocals", "VocalsMidMouth", state, "lbicep_g", "lforearm_g", "lhand_g");

    [TestCaseSource(nameof(WookieeStates))]
    public void WidePipesDifferFromOrdinaryPipesOnlyInTheArms(string state) =>
        VariantMovesOnly("Pipes", "PipesWide", state, "lbicep_g", "lforearm_g", "lhand_g", "rbicep_g", "rforearm_g", "rhand_g");

    [TestCaseSource(nameof(WookieeStates))]
    public void WidePipesHoldTheHandsFiveCentimetresFurtherOutOnEachSide(string state)
    {
        var ordinary = Project("Pipes", state);
        var wide = Project("PipesWide", state);
        foreach (var (hand, direction) in new[] { ("rhand_g", 1f), ("lhand_g", -1f) })
        {
            var joint = ordinary.Joints.FindIndex(j => j.Name == hand);
            for (var key = 0; key < ordinary.Keys.Count; key++)
            {
                var from = AnimationRig.World(ordinary.Joints, ordinary.Keys[key].Pose)[joint].Translation;
                var to = AnimationRig.World(wide.Joints, wide.Keys[key].Pose)[joint].Translation;
                ((to.X - from.X) * direction).Should().BeApproximately(.05f, .012f, hand);
                MathF.Abs(to.Z - from.Z).Should().BeLessThan(.012f, hand);
            }
        }
    }

    private static void VariantMovesOnly(string family, string variant, string state, params string[] joints)
    {
        // The robe layers copy the ordinary clip's garment tracks, so nothing else may move.
        var ordinary = Project(family, state);
        var lowered = Project(variant, state);
        lowered.Keys.Count.Should().Be(ordinary.Keys.Count);
        var arm = new HashSet<string>(joints);
        var moved = new HashSet<string>();
        for (var key = 0; key < ordinary.Keys.Count; key++)
        {
            lowered.Keys[key].Time.Should().Be(ordinary.Keys[key].Time);
            for (var joint = 0; joint < ordinary.Joints.Count; joint++)
            {
                var expected = ordinary.Keys[key].Pose[joint];
                var actual = lowered.Keys[key].Pose[joint];
                Vector3.Distance(actual.Position, expected.Position).Should().BeLessThan(.000001f, ordinary.Joints[joint].Name);
                if (MathF.Abs(Quaternion.Dot(actual.Orientation, expected.Orientation)) < .999999f)
                    moved.Add(ordinary.Joints[joint].Name);
            }
        }
        moved.Should().BeSubsetOf(arm);
        moved.Should().Contain(joints[0]);
    }

    private static void MicrophoneStaysAtTheLips(string family, string state, float height)
    {
        var project = Project(family, state);
        var wrist = project.Joints.FindIndex(j => j.Name == "rhand_g");
        var microphone = project.Joints.FindIndex(j => j.Name == "lhand_g");
        var head = project.Joints.FindIndex(j => j.Name == "head_g");
        var hold = AnimationRig.World(project.Joints, project.Keys[0].Pose);
        Vector3.Distance(hold[microphone].Translation, new(-.11f, .225f, height)).Should().BeLessThan(.001f);
        var atLips = InHeadSpace(hold[microphone].Translation, hold[head]);
        foreach (var key in project.Keys)
        {
            WristAngle(project, key.Pose[wrist].Orientation, wrist).Should().BeLessThan(.1f,
                "the free hand must not inherit a nearly 180-degree sword-aim twist");
            WristAngle(project, key.Pose[microphone].Orientation, microphone).Should().BeLessThan(60,
                "forearm roll must carry the gripping hand's twist");
            var world = AnimationRig.World(project.Joints, key.Pose);
            Vector3.Distance(InHeadSpace(world[microphone].Translation, world[head]), atLips).Should().BeLessThan(.003f,
                "the microphone must stay at the lips while the head turns, nods and sways");
        }
    }

    [TestCaseSource(nameof(States))]
    public void PipesStayAtTheMouthWithBothElbowsBelowTheHands(string state)
    {
        var project = Project("Pipes", state);
        var right = project.Joints.FindIndex(j => j.Name == "rhand_g");
        var left = project.Joints.FindIndex(j => j.Name == "lhand_g");
        var head = project.Joints.FindIndex(j => j.Name == "head_g");
        var rightElbow = project.Joints.FindIndex(j => j.Name == "rforearm_g");
        var leftElbow = project.Joints.FindIndex(j => j.Name == "lforearm_g");
        var hold = AnimationRig.World(project.Joints, project.Keys[0].Pose);
        var rightAtLips = InHeadSpace(hold[right].Translation, hold[head]);
        var leftAtLips = InHeadSpace(hold[left].Translation, hold[head]);
        var fitted = RelativeRotation(hold[right], hold[head]);
        foreach (var key in project.Keys)
        {
            var world = AnimationRig.World(project.Joints, key.Pose);
            var slide = InHeadSpace(world[right].Translation, world[head]) - rightAtLips;
            MathF.Abs(slide.X).Should().BeLessThan(.05f, "the pipes only slide across the lips from note to note");
            MathF.Abs(slide.Y).Should().BeLessThan(.004f, "the pipes must not drift away from the mouth");
            MathF.Abs(slide.Z).Should().BeLessThan(.012f, "the pipes must stay level with the lips");
            Vector3.Distance(InHeadSpace(world[left].Translation, world[head]) - leftAtLips, slide).Should().BeLessThan(.003f,
                "both hands carry the pipes together");
            MathF.Abs(Quaternion.Dot(RelativeRotation(world[right], world[head]), fitted)).Should().BeGreaterThan(.9999f,
                "the pipes keep their fitted angle at the mouth");
            world[rightElbow].Translation.Z.Should().BeLessThan(world[right].Translation.Z - .08f,
                "a raised elbow beside the head reads as a chicken wing, not a pipe player");
            world[leftElbow].Translation.Z.Should().BeLessThan(world[left].Translation.Z - .08f);
            WristAngle(project, key.Pose[right].Orientation, right).Should().BeLessThan(60,
                "lowering the elbow must not break the wrist that holds the pipes");
        }
    }

    [TestCase("Vocals", .05f)]
    [TestCase("Pipes", .035f)]
    [TestCase("Tambourine", .05f)]
    public void LoopsShiftWeightTurnTheHeadAndLiftAHeel(string family, float sway)
    {
        // A performer who only moves the hands looks like a statue holding a prop.
        var project = Project(family, "Loop");
        var root = project.Joints.FindIndex(j => j.Name == "rootdummy");
        var head = project.Joints.FindIndex(j => j.Name == "head_g");
        var foot = project.Joints.FindIndex(j => j.Name == "lfoot_g");
        var worlds = project.Keys.Select(key => AnimationRig.World(project.Joints, key.Pose)).ToArray();
        var across = worlds.Select(world => world[root].Translation.X).ToArray();
        (across.Max() - across.Min()).Should().BeGreaterThan(sway, "the weight shifts from foot to foot");
        var level = Quaternion.CreateFromRotationMatrix(worlds[0][head]);
        worlds.Max(world => 2 * MathF.Acos(Math.Clamp(MathF.Abs(Quaternion.Dot(Quaternion.CreateFromRotationMatrix(world[head]), level)), 0, 1))
            * 180 / MathF.PI).Should().BeGreaterThan(4, "the head turns and nods with the music");
        (worlds.Max(world => world[foot].Translation.Z) - worlds[0][foot].Translation.Z).Should().BeGreaterThan(.008f,
            "a heel lifts in time");
    }

    [TestCase(90f, false)]
    [TestCase(360f, true)]
    public void AWholeBodyTurnCarriesTheMicrophoneWithTheHeadOnce(float yaw, bool ramp)
    {
        var folder = Path.Combine(Root, "design/animations/performance");
        var recipe = JsonSerializer.Deserialize<Recipe>(File.ReadAllText(Path.Combine(folder, "props-recipe.json")), Recipe.Json)!;
        var motion = recipe.Motions.Single(m => m.Id == "VocalsLoop");
        motion.LeftHandFollowsHead.Should().BeTrue();
        var duration = motion.Beats[^1].Time;
        var turning = motion with
        {
            Beats = motion.Beats.Select(beat =>
            {
                var pose = System.Text.Json.Nodes.JsonNode.Parse(beat.Pose.ToJsonString())!.AsObject();
                pose["rootYaw"] = ramp ? yaw * beat.Time / duration : yaw;
                return beat with { Pose = pose };
            }).ToArray()
        };
        var rig = Project("Vocals", "Loop");
        var still = MotionAuthor.Bake(rig, rig.Sample(0), recipe, motion);
        var turned = MotionAuthor.Bake(rig, rig.Sample(0), recipe, turning);
        var microphone = rig.Joints.FindIndex(j => j.Name == "lhand_g");
        var head = rig.Joints.FindIndex(j => j.Name == "head_g");
        Matrix4x4 InHead(AnimationProject project, int key)
        {
            var world = AnimationRig.World(project.Joints, project.Keys[key].Pose);
            Matrix4x4.Invert(world[head], out var inverse).Should().BeTrue();
            return world[microphone] * inverse;
        }
        turned.Keys.Count.Should().Be(still.Keys.Count);
        for (var key = 0; key < still.Keys.Count; key++)
        {
            var expected = InHead(still, key);
            var actual = InHead(turned, key);
            Vector3.Distance(actual.Translation, expected.Translation).Should().BeLessThan(.003f,
                $"a turn must not move the microphone off the lips at {still.Keys[key].Time:0.000}s");
            foreach (var axis in new[] { Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ })
                Vector3.Distance(Vector3.TransformNormal(axis, actual), Vector3.TransformNormal(axis, expected)).Should().BeLessThan(.02f,
                    $"a turn must not twist the grip at {still.Keys[key].Time:0.000}s");
        }
    }

    [Test]
    public void TheRecipeBakesEveryCommittedProject()
    {
        // Covers the unchanged violin clips too: controls a recipe omits must not change its bake.
        var folder = Path.Combine(Root, "design/animations/performance");
        var recipe = JsonSerializer.Deserialize<Recipe>(File.ReadAllText(Path.Combine(folder, "props-recipe.json")), Recipe.Json)!;
        recipe.Motions.Select(motion => motion.Id).Should().BeEquivalentTo(
            new[] { "Vocals", "Pipes", "Violin", "Tambourine", "VocalsLowMouth" }
                .SelectMany(family => States.Select(state => family + state))
                .Concat(new[] { "VocalsMidMouth", "PipesWide" }.SelectMany(family => WookieeStates.Select(state => family + state))));
        foreach (var motion in recipe.Motions)
        {
            var committed = AnimationProject.Deserialize(File.ReadAllText(Path.Combine(folder, motion.Id + ".swlanim")));
            var baked = MotionAuthor.Bake(committed, committed.Sample(0), recipe, motion);
            baked.Keys.Count.Should().Be(committed.Keys.Count, motion.Id);
            for (var key = 0; key < baked.Keys.Count; key++)
            {
                baked.Keys[key].Time.Should().BeApproximately(committed.Keys[key].Time, .000001f, motion.Id);
                for (var joint = 0; joint < committed.Joints.Count; joint++)
                {
                    var expected = committed.Keys[key].Pose[joint];
                    var actual = baked.Keys[key].Pose[joint];
                    if (Vector3.Distance(actual.Position, expected.Position) > .00001f ||
                        MathF.Abs(Quaternion.Dot(actual.Orientation, expected.Orientation)) < .999999f)
                        Assert.Fail($"{motion.Id} differs from its recipe at {committed.Keys[key].Time:0.000}s on {committed.Joints[joint].Name}.");
                }
            }
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
    [TestCase("VocalsLowMouth")]
    [TestCase("VocalsMidMouth")]
    [TestCase("Pipes")]
    [TestCase("PipesWide")]
    [TestCase("Violin")]
    [TestCase("Tambourine")]
    public void AccentsReturnToTheLoopHoldWithoutAnArmSnap(string family)
    {
        var loop = Project(family, "Loop");
        foreach (var state in family is "VocalsMidMouth" or "PipesWide" ? WookieeStates : States)
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

    private static Vector3 InHeadSpace(Vector3 point, Matrix4x4 head)
    {
        Matrix4x4.Invert(head, out var inverse);
        return Vector3.Transform(point, inverse);
    }

    private static Quaternion RelativeRotation(Matrix4x4 joint, Matrix4x4 head) => Quaternion.Normalize(
        Quaternion.Inverse(Quaternion.CreateFromRotationMatrix(head)) * Quaternion.CreateFromRotationMatrix(joint));

    private static float WristAngle(AnimationProject project, Quaternion actual, int wrist) =>
        2 * MathF.Acos(Math.Clamp(MathF.Abs(Quaternion.Dot(Quaternion.Normalize(actual),
            project.Joints[wrist].Rest.Orientation)), 0, 1)) * 180 / MathF.PI;
}
