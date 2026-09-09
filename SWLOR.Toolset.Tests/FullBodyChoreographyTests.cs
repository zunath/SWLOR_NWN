using System.Numerics;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.AnimationDrafts;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Animation;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests;

public class FullBodyChoreographyTests
{
    private static MdlModel NativeModel()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln"))) directory = directory.Parent;
        var path = Path.Combine(directory?.FullName ?? throw new DirectoryNotFoundException(), "SWLOR_Haks/sw_cr_creature/a_ba.mdl");
        if (!File.Exists(path)) Assert.Ignore("Initialize native models.");
        return new MdlReader().Parse(File.ReadAllBytes(path));
    }

    [Test]
    public void HandAndTorsoDirectionRetainTheNativeStrikeLegMotionBetweenBeats()
    {
        var model = NativeModel();
        ChoreographyBeat[] beats = [new(0, "Idle", "pause1"), new(.2f, "Load", "2hslashr", 0),
            new(1.2f, "Release", "2hslashr", 1), new(1.4f, "Recover", "pause1")];
        var recipe = new Choreography("NativeLegs", "Keep the strike's real lower-body movement under arm direction.", 1.4f, beats);
        var baseline = ChoreographyAuthor.Bake(model, recipe);
        beats[1] = beats[1] with { RightHand = [.24f, .1f, 1.15f], TorsoDegrees = [3, 0, -5] };
        beats[2] = beats[2] with { RightHand = [.28f, .4f, 1.2f], TorsoDegrees = [-4, 0, 6] };
        var directed = ChoreographyAuthor.Bake(model, recipe);
        var lower = new[] { "lthigh_g", "lshin_g", "lfoot_g", "rthigh_g", "rshin_g", "rfoot_g" }
            .Select(name => directed.Joints.FindIndex(joint => joint.Name == name)).ToArray();
        lower.Should().OnlyContain(index => index >= 0);
        var native = model.Animations.Single(clip => clip.Name == "2hslashr");
        var bind = MdlAnimationPose.BindPose(model);
        var nonLinearMotion = 0f;
        foreach (var key in directed.Keys.Where(key => key.Time > .2f && key.Time < 1.2f))
        {
            var expected = baseline.Sample(key.Time);
            var actual = directed.Sample(key.Time);
            var fraction = key.Time - .2f;
            var sampled = MdlAnimationPose.Sample(native, fraction * native.Length, bind);
            foreach (var joint in lower)
            {
                Vector3.Distance(expected[joint].Position, actual[joint].Position).Should().BeLessThan(.00001f);
                Math.Abs(Quaternion.Dot(expected[joint].Orientation, actual[joint].Orientation)).Should().BeGreaterThan(.99999f,
                    "arm direction must not replace the native leg curve with interpolation between just two poses");
                var sparse = Quaternion.Slerp(baseline.Sample(.2f)[joint].Orientation,
                    baseline.Sample(1.2f)[joint].Orientation, fraction * fraction * (3 - 2 * fraction));
                nonLinearMotion = Math.Max(nonLinearMotion, 1 - Math.Abs(Quaternion.Dot(sparse, actual[joint].Orientation)));
            }
            var hand = directed.Joints.FindIndex(joint => joint.Name == "rhand_g");
            Math.Abs(Quaternion.Dot(actual[hand].Orientation, sampled["rhand_g"].Orientation)).Should().BeGreaterThan(.99999f,
                "a continuous hand path must keep the sampled local equipment grip throughout the span");
        }
        nonLinearMotion.Should().BeGreaterThan(.001f, "this source must exercise visible motion lost by sparse endpoint interpolation");
    }

    [Test]
    public void ExplicitWeightShiftBendsTheLegsWhileFeetAndHandTargetsStayInPlace()
    {
        var model = NativeModel();
        var rig = AnimationProject.FromModel(model);
        var bind = MdlAnimationPose.BindPose(model);
        var native = MdlAnimationPose.Sample(model.Animations.Single(clip => clip.Name == "pause1"), 0, bind);
        var idle = rig.Joints.Select(joint => native.GetValueOrDefault(joint.Name, joint.Rest)).ToArray();
        var before = AnimationRig.World(rig.Joints, idle);
        int Joint(string name) => rig.Joints.FindIndex(joint => joint.Name == name);
        var handTarget = before[Joint("rhand_g")].Translation + new Vector3(0, .025f, .01f);
        var target = new[] { handTarget.X, handTarget.Y, handTarget.Z };
        var recipe = new Choreography("WeightShift", "Load the stance while keeping the feet planted and the weapon steady.", 1,
            [new(0, "Idle", "pause1"), new(.3f, "Load", "pause1", RightHand: target, RootOffset: [.035f, .045f, -.045f]),
                new(.7f, "Hold", "pause1", RightHand: target, RootOffset: [.035f, .045f, -.045f]), new(1, "Recover", "pause1")]);
        var project = ChoreographyAuthor.Bake(model, recipe);
        var loaded = AnimationRig.World(project.Joints, project.Sample(.3f));
        Vector3.Distance(loaded[Joint("rootdummy")].Translation - before[Joint("rootdummy")].Translation,
            new(.035f, .045f, -.045f)).Should().BeLessThan(.0001f);
        Vector3.Distance(loaded[Joint("rhand_g")].Translation, handTarget).Should().BeLessThan(.001f);
        var shin = Joint("rshin_g");
        Math.Abs(Quaternion.Dot(project.Sample(.3f)[shin].Orientation, idle[shin].Orientation)).Should().BeLessThan(.999f,
            "the weight shift must visibly flex the knees instead of translating rigid legs");
        foreach (var time in Enumerable.Range(0, 121).Select(frame => frame / 120f))
        {
            var world = AnimationRig.World(project.Joints, project.Sample(time));
            foreach (var foot in new[] { Joint("lfoot_g"), Joint("rfoot_g") })
            {
                Vector3.Distance(world[foot].Translation, before[foot].Translation).Should().BeLessThan(.001f,
                    "planted feet must not slide during weight shift, hold, or recovery");
                Matrix4x4.Decompose(before[foot], out _, out var expectedRotation, out _);
                Matrix4x4.Decompose(world[foot], out _, out var actualRotation, out _);
                Math.Abs(Quaternion.Dot(expectedRotation, actualRotation)).Should().BeGreaterThan(.99999f,
                    "ankles must counter-rotate to keep each sole at its native orientation");
            }
        }
        foreach (var time in new[] { 0f, project.Duration })
        for (var joint = 0; joint < idle.Length; joint++)
        {
            var actual = project.Sample(time)[joint];
            Vector3.Distance(actual.Position, idle[joint].Position).Should().BeLessThan(.00001f);
            Math.Abs(Quaternion.Dot(actual.Orientation, idle[joint].Orientation)).Should().BeGreaterThan(.99999f);
        }
    }

    [TestCase(false, 1f, 0f)]
    [TestCase(true, 1f, 0f)]
    [TestCase(false, .8f, .2f)]
    [TestCase(true, .8f, .2f)]
    public void ResetSourcePhasesRecoverDirectlyWithoutReplayingTheAttack(bool shiftWeight, float firstPhase, float lastPhase)
    {
        var model = NativeModel();
        var rig = AnimationProject.FromModel(model);
        var idle = MdlAnimationPose.Sample(model.Animations.Single(clip => clip.Name == "pause1"), 0, MdlAnimationPose.BindPose(model));
        var source = new MdlNode { Name = "test_attack" };
        foreach (var joint in rig.Joints)
        {
            var rest = idle.GetValueOrDefault(joint.Name, joint.Rest);
            var node = new MdlNode { Name = joint.Name, PositionTimes = [0], PositionValues = [rest.Position],
                OrientationTimes = [0], OrientationValues = [rest.Orientation], ScaleTimes = [0], ScaleValues = [rest.Scale] };
            if (joint.Name == "rforearm_g")
            {
                Quaternion Turn(float degrees) => Quaternion.Normalize(rest.Orientation * Quaternion.CreateFromAxisAngle(Vector3.UnitX, degrees * MathF.PI / 180));
                node.OrientationTimes = [0, .2f, .5f, .8f, 1];
                node.OrientationValues = [Turn(0), Turn(0), Turn(90), Turn(20), Turn(20)];
            }
            source.Children.Add(node);
        }
        model.Animations.Add(new MdlAnimation { Name = "test_attack", Length = 1, GeometryRoot = source });
        var idlePose = rig.Joints.Select(joint => idle.GetValueOrDefault(joint.Name, joint.Rest)).ToArray();
        var idleWorld = AnimationRig.World(rig.Joints, idlePose);
        var hand = rig.Joints.FindIndex(joint => joint.Name == "lhand_g");
        var handTarget = idleWorld[hand].Translation + new Vector3(0, .025f, .01f);
        var recipe = new Choreography("RecoverAttack", "Return directly from follow-through to ready, without repeating the attack in reverse.", 1.4f,
            [new(0, "Idle", "pause1"),
                new(.2f, "Follow through", "test_attack", firstPhase, RootOffset: shiftWeight ? [.02f, .025f, -.03f] : null),
                new(1.2f, "Ready", "test_attack", lastPhase, LeftHand: [handTarget.X, handTarget.Y, handTarget.Z],
                    RootOffset: shiftWeight ? [-.015f, .015f, -.02f] : null),
                new(1.4f, "Idle", "pause1")]);
        var project = ChoreographyAuthor.Bake(model, recipe);
        var elbow = rig.Joints.FindIndex(joint => joint.Name == "rforearm_g");
        var previousAngle = 21f;
        foreach (var key in project.Keys.Where(key => key.Time >= .2f && key.Time <= 1.2f))
        {
            var angle = 2 * MathF.Acos(Math.Clamp(Math.Abs(Quaternion.Dot(Quaternion.Normalize(key.Pose[elbow].Orientation),
                Quaternion.Normalize(idlePose[elbow].Orientation))), 0, 1)) * 180 / MathF.PI;
            angle.Should().BeLessThanOrEqualTo(previousAngle + .01f,
                "recovery must lower the forearm monotonically, without visiting the source attack's 90-degree interior pose");
            previousAngle = angle;
            if (!shiftWeight) continue;
            var world = AnimationRig.World(project.Joints, key.Pose);
            foreach (var name in new[] { "lfoot_g", "rfoot_g" })
            {
                var foot = rig.Joints.FindIndex(joint => joint.Name == name);
                Vector3.Distance(world[foot].Translation, idleWorld[foot].Translation).Should().BeLessThan(.001f,
                    "resetting a source phase must still compensate the authored body shift at the planted feet");
            }
        }
        Vector3.Distance(AnimationRig.World(project.Joints, project.Sample(1.2f))[hand].Translation, handTarget).Should().BeLessThan(.001f);
        foreach (var key in project.Keys)
            Math.Abs(Quaternion.Dot(key.Pose[hand].Orientation, idlePose[hand].Orientation)).Should().BeGreaterThan(.99999f,
                "the direct recovery and hand fade must preserve the local equipment grip");
    }

    [Test]
    public void QuickDrawLowersTheMuzzleWithoutReplayingTheShotBackward()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln"))) directory = directory.Parent;
        var root = directory?.FullName ?? throw new DirectoryNotFoundException();
        var recipe = ChoreographyAuthor.Read(File.ReadAllText(Path.Combine(root, "design/animations/pistol/choreographies.json")))
            .Single(entry => entry.Id == "QuickDraw");
        var sources = recipe.Beats.Where(beat => beat.SourceModel != null).Select(beat => beat.SourceModel!).Distinct()
            .ToDictionary(name => name, name => new MdlReader().Parse(File.ReadAllBytes(Path.Combine(root, "SWLOR_Haks/sw_cr_creature", name + ".mdl"))));
        var project = ChoreographyAuthor.Bake(NativeModel(), recipe, sources);
        var recover = recipe.Beats.Single(beat => beat.Label == "Recover sight line");
        var lower = recipe.Beats.Single(beat => beat.Label == "Lower muzzle");
        recover.SourceTime.Should().BeGreaterThan(lower.SourceTime, "this recipe must exercise a reset from the completed shot to its ready pose");
        var forearm = project.Joints.FindIndex(joint => joint.Name == "rforearm_g");
        var first = Quaternion.Normalize(project.Sample(recover.Time)[forearm].Orientation);
        var next = Quaternion.Normalize(project.Sample(recover.Time + 1 / 30f)[forearm].Orientation);
        var angle = 2 * MathF.Acos(Math.Clamp(Math.Abs(Quaternion.Dot(first, next)), 0, 1)) * 180 / MathF.PI;
        TestContext.Progress.WriteLine($"QuickDraw forearm recovery, first 33 ms: {angle:F3} degrees.");
        angle.Should().BeLessThan(10, "lowering the muzzle must not replay the native recoil backward by over 40 degrees in the first 33 ms");
        var hand = project.Joints.FindIndex(joint => joint.Name == "rhand_g");
        Vector3.Distance(AnimationRig.World(project.Joints, project.Sample(lower.Time))[hand].Translation,
            new Vector3(lower.RightHand![0], lower.RightHand[1], lower.RightHand[2])).Should().BeLessThan(.005f,
                "the recovery must reach its authored lowered-muzzle target, allowing the existing ground-clearance correction");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void UndirectedNativeReverseMotionRemainsAvailable(bool shiftWeight)
    {
        var model = NativeModel();
        var recipe = new Choreography("ReverseNative", "Use a native downward action in reverse to create a rising strike.", 1.4f,
            [new(0, "Idle", "pause1"), new(.2f, "Low", "2hslashr", 1, RootOffset: shiftWeight ? [0, 0, -.015f] : null),
                new(1.2f, "Rise", "2hslashr", 0, RootOffset: shiftWeight ? [0, 0, -.015f] : null), new(1.4f, "Idle", "pause1")]);
        var project = ChoreographyAuthor.Bake(model, recipe);
        var native = model.Animations.Single(clip => clip.Name == "2hslashr");
        var bind = MdlAnimationPose.BindPose(model);
        var forearm = project.Joints.FindIndex(joint => joint.Name == "rforearm_g");
        foreach (var key in project.Keys.Where(key => key.Time >= .2f && key.Time <= 1.2f))
        {
            var expected = MdlAnimationPose.Sample(native, (1 - (key.Time - .2f)) * native.Length, bind);
            Math.Abs(Quaternion.Dot(key.Pose[forearm].Orientation, expected["rforearm_g"].Orientation)).Should().BeGreaterThan(.99999f,
                "pure native phase traversal is an existing authoring feature, including rising strikes and loop wraps");
        }
    }

    [Test]
    public void AnUnreachableBodyLiftIsRestrictedInsteadOfDraggingTheFeet()
    {
        var model = NativeModel();
        var recipe = new Choreography("ReachLimit", "Do not pull straight legs off their original contact points.", 1,
            [new(0, "Idle", "pause1"), new(.5f, "Lift", "pause1", RootOffset: [0, 0, .1f]), new(1, "Recover", "pause1")]);
        var project = ChoreographyAuthor.Bake(model, recipe);
        var before = AnimationRig.World(project.Joints, project.Sample(0));
        var after = AnimationRig.World(project.Joints, project.Sample(.5f));
        var root = project.Joints.FindIndex(joint => joint.Name == "rootdummy");
        (after[root].Translation.Z - before[root].Translation.Z).Should().BeLessThan(.1f);
        foreach (var name in new[] { "lfoot_g", "rfoot_g" })
        {
            var foot = project.Joints.FindIndex(joint => joint.Name == name);
            Vector3.Distance(before[foot].Translation, after[foot].Translation).Should().BeLessThan(.0001f);
        }
    }

    [TestCase(.151f, 0, 0)]
    [TestCase(0, -.151f, 0)]
    [TestCase(0, 0, .101f)]
    public void ExcessiveWeightShiftIsRejected(float x, float y, float z)
    {
        var recipe = new Choreography("BadShift", "Invalid authoring input.", 1,
            [new(0, "Idle", "pause1"), new(.5f, "Move", "pause1", RootOffset: [x, y, z]), new(1, "Recover", "pause1")]);
        Action read = () => ChoreographyAuthor.Read(JsonSerializer.Serialize(new[] { recipe }, BulkMotionAuthor.Json));
        read.Should().Throw<InvalidDataException>().WithMessage("*15 cm*");
    }

    [Test]
    public void RootOffsetCannotChangeNeutralEndpointsAndItsOmissionPreservesRecipeProvenance()
    {
        var beat = new ChoreographyBeat(0, "Idle", "pause1");
        JsonSerializer.Serialize(beat, BulkMotionAuthor.Json).Should().NotContain("RootOffset");
        var recipe = new Choreography("BadEndpoint", "Neutral bookends are required.", 1,
            [beat with { RootOffset = [0, 0, 0] }, new(.5f, "Hold", "pause1"), new(1, "Recover", "pause1")]);
        Action read = () => ChoreographyAuthor.Read(JsonSerializer.Serialize(new[] { recipe }, BulkMotionAuthor.Json));
        read.Should().Throw<InvalidDataException>().WithMessage("*endpoints*");
    }

    [Test]
    public void OptionalWeightShiftDoesNotInvalidateTheEndorsedAbsoluteDefenseRecipeHash()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln"))) directory = directory.Parent;
        var root = directory?.FullName ?? throw new DirectoryNotFoundException();
        var recipe = ChoreographyAuthor.Read(File.ReadAllText(Path.Combine(root, "design/animations/heavy-vibroblade/choreographies.json")))
            .Single(entry => entry.Id == "AbsoluteDefense");
        recipe.Beats.Should().OnlyContain(beat => beat.RootOffset == null);
        var hash = BulkMotionAuthor.ProjectHash(JsonSerializer.Serialize(recipe, BulkMotionAuthor.Json));
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "design/animations/active-manifest.json")));
        hash.Should().Be(manifest.RootElement.GetProperty("Animations").EnumerateArray().Single(entry => entry.GetProperty("Id").GetString() == recipe.Id)
            .GetProperty("ChoreographySha256").GetString(), "adding an optional authoring control must not invalidate preserved native choreography provenance");
    }

    [Test]
    public void HandOverridesFadeTheElbowPlaneDuringEntryAndRecoveryWithoutWeakeningFullTargets()
    {
        var model = NativeModel();
        var recipe = new Choreography("SoftEntry", "Raise and lower the hands without snapping the elbows into a different bend plane.", 1.4f,
            [new(0, "Idle", "pause1"),
                new(.5f, "Reach", "pause1", LeftHand: [-.21f, .24f, 1.2f], RightHand: [.21f, .24f, 1.2f], RootOffset: [.02f, .02f, -.035f]),
                new(.9f, "Hold", "pause1", LeftHand: [-.21f, .24f, 1.2f], RightHand: [.21f, .24f, 1.2f], RootOffset: [.02f, .02f, -.035f]),
                new(1.4f, "Recover", "pause1")]);
        var project = ChoreographyAuthor.Bake(model, recipe);
        float Angle(Quaternion a, Quaternion b) => 2 * MathF.Acos(Math.Clamp(Math.Abs(Quaternion.Dot(
            Quaternion.Normalize(a), Quaternion.Normalize(b))), 0, 1)) * 180 / MathF.PI;
        foreach (var name in new[] { "lbicep_g", "lforearm_g", "rbicep_g", "rforearm_g" })
        {
            var joint = project.Joints.FindIndex(joint => joint.Name == name);
            Angle(project.Sample(0)[joint].Orientation, project.Sample(1 / 30f)[joint].Orientation).Should().BeLessThan(10,
                name + " must not switch immediately from its native bend plane to the authored pole");
            Angle(project.Sample(project.Duration - 1 / 30f)[joint].Orientation, project.Sample(project.Duration)[joint].Orientation)
                .Should().BeLessThan(10, name + " must fade its bend plane back to native recovery");
        }
        var grip = project.Sample(0);
        foreach (var (name, target) in new[] { ("lhand_g", new Vector3(-.21f, .24f, 1.2f)), ("rhand_g", new Vector3(.21f, .24f, 1.2f)) })
        {
            var joint = project.Joints.FindIndex(joint => joint.Name == name);
            foreach (var time in new[] { .5f, .7f, .9f })
                Vector3.Distance(AnimationRig.World(project.Joints, project.Sample(time))[joint].Translation, target).Should().BeLessThan(.001f,
                    "fading a missing endpoint must not weaken fully authored hand contact");
            foreach (var key in project.Keys)
                Math.Abs(Quaternion.Dot(key.Pose[joint].Orientation, grip[joint].Orientation)).Should().BeGreaterThan(.99999f,
                    "elbow blending must not alter the native local wrist grip");
        }
    }

    [Test]
    public void NearlyStraightArmsFadeIntoAndOutOfTheAuthoredPoseWithoutFlipping()
    {
        var model = NativeModel();
        var rig = AnimationProject.FromModel(model);
        var native = MdlAnimationPose.Sample(model.Animations.Single(clip => clip.Name == "pause1"), 0, MdlAnimationPose.BindPose(model));
        var idle = rig.Joints.Select(joint => native.GetValueOrDefault(joint.Name, joint.Rest)).ToArray();
        var world = AnimationRig.World(rig.Joints, idle);
        var hand = rig.Joints.FindIndex(joint => joint.Name == "rhand_g");
        var elbow = rig.Joints[hand].Parent; var shoulder = rig.Joints[elbow].Parent;
        var length = Vector3.Distance(world[shoulder].Translation, world[elbow].Translation) +
            Vector3.Distance(world[elbow].Translation, world[hand].Translation);
        var target = world[shoulder].Translation + Vector3.Normalize(world[hand].Translation - world[shoulder].Translation) * (length - .000001f);
        var recipe = new Choreography("StraightArm", "Reach near full extension and recover smoothly.", 2,
            [new(0, "Idle", "pause1"), new(1, "Extend", "pause1", RightHand: [target.X, target.Y, target.Z], RootOffset: [0, 0, 0]),
                new(2, "Recover", "pause1")]);
        var project = ChoreographyAuthor.Bake(model, recipe);
        for (var frame = 1; frame <= 240; frame++)
        foreach (var joint in new[] { shoulder, elbow })
        {
            var before = Quaternion.Normalize(project.Sample((frame - 1) / 120f)[joint].Orientation);
            var after = Quaternion.Normalize(project.Sample(frame / 120f)[joint].Orientation);
            Math.Abs(Quaternion.Dot(before, after)).Should().BeGreaterThan(.999f,
                "a nearly collinear arm must not switch abruptly between bend directions");
        }
        Vector3.Distance(AnimationRig.World(project.Joints, project.Sample(1))[hand].Translation, target).Should().BeLessThan(.001f);
    }

    [TestCase("beast-mastery", "CallBeast", 0f, 1 / 30f)]
    [TestCase("vibroknife", "AssassinsStance", .65f, 2 / 3f)]
    [TestCase("katar", "GuardCounter", 5 / 6f, .85f)]
    public void AbilityHandOverrideBoundariesDoNotSnapTheForearm(string category, string id, float before, float after)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln"))) directory = directory.Parent;
        var root = directory?.FullName ?? throw new DirectoryNotFoundException();
        var recipe = ChoreographyAuthor.Read(File.ReadAllText(Path.Combine(root, "design/animations", category, "choreographies.json")))
            .Single(entry => entry.Id == id);
        var project = ChoreographyAuthor.Bake(NativeModel(), recipe);
        var joint = project.Joints.FindIndex(joint => joint.Name == "rforearm_g");
        var first = Quaternion.Normalize(project.Sample(before)[joint].Orientation);
        var second = Quaternion.Normalize(project.Sample(after)[joint].Orientation);
        var angle = 2 * MathF.Acos(Math.Clamp(Math.Abs(Quaternion.Dot(first, second)), 0, 1)) * 180 / MathF.PI;
        angle.Should().BeLessThan(10, id + " previously snapped its forearm by 35–142 degrees when hand control appeared or disappeared");
    }
}
