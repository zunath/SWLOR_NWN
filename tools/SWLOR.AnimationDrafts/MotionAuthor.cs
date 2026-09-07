using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using SWLOR.Toolset.Domain.Animation;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.AnimationDrafts;

internal sealed record Recipe(string Workbook, string Model, JsonObject ReadyShield, JsonObject ReadyBlade, Motion[] Motions)
{
    public static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };
}
internal sealed record Motion(string Id, string Name, int BibleRow, string Reference, string Observation,
    string Interpretation, bool Shield, Beat[] Beats, string AbilityDefinition, bool Loop = false);
internal sealed record Beat(float Time, string Label, JsonObject Pose);

internal static class MotionAuthor
{
    public static AnimationProject Bake(AnimationProject rig, PosedNode[] neutral, Recipe recipe, Motion motion)
    {
        var beats = motion.Beats;
        if (!motion.Loop && beats.Length < 3)
            throw new InvalidDataException($"{motion.Id}: non-looping motions require at least three beats for entry and release.");
        if (beats.Length < 2 || beats[0].Time != 0 || beats[^1].Time is <= 0 or > 10 ||
            beats.Zip(beats.Skip(1)).Any(p => !float.IsFinite(p.Second.Time) || p.Second.Time <= p.First.Time))
            throw new InvalidDataException($"{motion.Id}: beats must increase from zero to a duration of at most ten seconds.");
        var ready = motion.Shield ? recipe.ReadyShield : recipe.ReadyBlade;
        var poses = beats.Select(beat => Read(ready, beat.Pose)).ToArray();
        var result = rig.Clone(); result.Keys.Clear(); result.Events.Clear(); result.Name = motion.Id;
        result.Duration = beats[^1].Time; result.Transition = .15f;
        var times = new SortedSet<float>(beats.Select(b => b.Time));
        for (var i = 0; i / 20f < result.Duration; i++) times.Add(i / 20f);
        var segment = 0;
        foreach (var time in times)
        {
            while (segment < beats.Length - 2 && time > beats[segment + 1].Time) segment++;
            var fraction = Math.Clamp((time - beats[segment].Time) / (beats[segment + 1].Time - beats[segment].Time), 0, 1);
            var eased = fraction * fraction * (3 - 2 * fraction);
            var p = Pose.Lerp(poses[segment], poses[segment + 1], eased);
            var pose = PoseRig(rig, p, motion.Id, time);
            // A one-shot must release its authored stance even when no walking/attack follows it.
            // Channels retain a closed guard loop; their installed exit phase releases the stance.
            if (!motion.Loop)
            {
                var weight = time <= beats[1].Time ? time / beats[1].Time :
                    time >= beats[^2].Time ? (result.Duration - time) / (result.Duration - beats[^2].Time) : 1f;
                weight = weight * weight * (3 - 2 * weight);
                var fromWorld = AnimationRig.World(rig.Joints, neutral);
                var toWorld = AnimationRig.World(rig.Joints, pose);
                for (var i = 0; i < pose.Length; i++)
                    pose[i] = new(Vector3.Lerp(neutral[i].Position, pose[i].Position, weight),
                        Quaternion.Slerp(neutral[i].Orientation, pose[i].Orientation, weight),
                        float.Lerp(neutral[i].Scale, pose[i].Scale, weight));
                // Rotation interpolation alone arcs the feet through the floor while stepping
                // between idle and the wider guard stance. Keep their world-space path grounded.
                foreach (var foot in new[] { "lfoot_g", "rfoot_g" })
                {
                    var i = Index(rig, foot);
                    var target = Vector3.Lerp(fromWorld[i].Translation, toWorld[i].Translation, weight);
                    pose = AnimationRig.SolveLimb(rig.Joints, pose, i, target, target + new Vector3(0, 1, .3f));
                    Matrix4x4.Decompose(fromWorld[i], out _, out var startRotation, out _);
                    Matrix4x4.Decompose(toWorld[i], out _, out var endRotation, out _);
                    Matrix4x4.Decompose(AnimationRig.World(rig.Joints, pose)[rig.Joints[i].Parent], out _, out var parent, out _);
                    pose[i] = pose[i] with { Orientation = Quaternion.Normalize(Quaternion.Inverse(parent) * Quaternion.Slerp(startRotation, endRotation, weight)) };
                }
                if (weight == 0) pose = (PosedNode[])neutral.Clone();
            }
            result.SetKey(time, pose);
        }
        result.Validate();
        var start = result.Sample(0); var finish = result.Sample(result.Duration);
        for (var i = 0; i < start.Length; i++)
            if (Vector3.Distance(start[i].Position, finish[i].Position) > .0001f ||
                Math.Abs(Quaternion.Dot(start[i].Orientation, finish[i].Orientation)) < .99999f)
                throw new InvalidDataException($"{motion.Id}: last pose must return to its ready pose.");
        for (var t = 0f; t <= result.Duration; t += 1f / 120)
        {
            var world = AnimationRig.World(rig.Joints, result.Sample(t));
            foreach (var name in new[] { "lfoot_g", "rfoot_g" })
                if (world[Index(rig, name)].Translation.Z < .125f)
                    throw new InvalidDataException($"{motion.Id}: foot penetrates the floor at {t:0.000}s.");
        }
        return result;
    }

    private static Pose Read(JsonObject ready, JsonObject patch)
    {
        Vector3 V(string name)
        {
            var a = (patch[name] ?? ready[name])?.AsArray() ?? throw new InvalidDataException("Missing pose field: " + name);
            if (a.Count != 3) throw new InvalidDataException("Pose vectors need three components: " + name);
            var v = new Vector3(a[0]!.GetValue<float>(), a[1]!.GetValue<float>(), a[2]!.GetValue<float>());
            if (!AnimationProject.Finite(v)) throw new InvalidDataException("Nonfinite pose: " + name);
            return v;
        }
        var blade = V("blade");
        if (blade.LengthSquared() < .01f) throw new InvalidDataException("Blade direction is zero.");
        return new(V("root"), V("chest"), V("hips"), V("leftHand"), V("rightHand"), blade,
            V("leftFoot"), V("rightFoot"), V("shield"));
    }

    private static PosedNode[] PoseRig(AnimationProject rig, Pose p, string name, float time)
    {
        var pose = rig.Joints.Select(j => j.Rest).ToArray();
        var root = Index(rig, "rootdummy");
        pose[root] = pose[root] with { Position = p.Root, Orientation = Quaternion.Identity };
        SetLocal("torso_g", Rotation(p.Chest)); SetLocal("pelvis_g", Rotation(p.Hips));
        SetWorld("head_g", Quaternion.CreateFromAxisAngle(Vector3.UnitZ, Radians(p.Chest.Y * .2f)));
        var centre = new Vector3(p.Root.X, p.Root.Y, 0);
        Solve("lfoot_g", p.LeftFoot, new Vector3(-.42f, 1.1f, .4f) + centre);
        Solve("rfoot_g", p.RightFoot, new Vector3(.42f, 1.1f, .4f) + centre);
        SetWorld("lfoot_g", Quaternion.CreateFromAxisAngle(Vector3.UnitZ, Radians(8)));
        SetWorld("rfoot_g", Quaternion.CreateFromAxisAngle(Vector3.UnitZ, Radians(-12)));
        // Keep bent elbows below the hands and near the ribs. Wide, shoulder-height poles
        // produce an outward elbow flare even when the hand is in a low guard.
        Solve("lhand_g", p.LeftHand, new Vector3(-.36f, -.12f, p.Root.Z - .18f) + centre);
        Solve("rhand_g", p.RightHand, new Vector3(.36f, -.12f, p.Root.Z - .18f) + centre);
        // Preserve the native hand's roll around the blade, rather than arbitrarily twisting
        // the wrist when the sword changes direction. Native +Z points toward the wrist/elbow.
        var world = AnimationRig.World(rig.Joints, pose);
        var blade = Vector3.Normalize(p.Blade);
        var towardElbow = world[Index(rig, "rforearm_g")].Translation - p.RightHand;
        var wrist = towardElbow - blade * Vector3.Dot(towardElbow, blade);
        if (wrist.LengthSquared() < .0001f) wrist = Vector3.UnitZ - blade * blade.Z;
        if (wrist.LengthSquared() < .0001f) wrist = Vector3.UnitX - blade * blade.X;
        wrist = Vector3.Normalize(wrist);
        var across = Vector3.Normalize(Vector3.Cross(blade, wrist));
        SetWorld("rhand_g", Quaternion.CreateFromRotationMatrix(new Matrix4x4(
            across.X, across.Y, across.Z, 0, blade.X, blade.Y, blade.Z, 0,
            wrist.X, wrist.Y, wrist.Z, 0, 0, 0, 0, 1)));
        // Actual AShLw meshes face -X and their TOP is +Y (not +Z). Apply both axes,
        // including for sword moves: a shield can still be equipped in the off hand.
        SetWorld("lhand_g", Rotation(p.Shield) *
            Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -MathF.PI / 2) *
            Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2));
        return pose;

        void Solve(string joint, Vector3 target, Vector3 pole)
        {
            var i = Index(rig, joint);
            pose = AnimationRig.SolveLimb(rig.Joints, pose, i, target, pole);
            var error = Vector3.Distance(AnimationRig.World(rig.Joints, pose)[i].Translation, target);
            if (error > .012f) throw new InvalidDataException($"{name} at {time:0.000}s: {joint} target exceeds reach by {error:0.000}m.");
        }
        void SetLocal(string joint, Quaternion orientation)
        {
            var i = Index(rig, joint); pose[i] = pose[i] with { Orientation = Quaternion.Normalize(orientation) };
        }
        void SetWorld(string joint, Quaternion orientation)
        {
            var i = Index(rig, joint);
            var world = AnimationRig.World(rig.Joints, pose);
            Matrix4x4.Decompose(world[rig.Joints[i].Parent], out _, out var parent, out _);
            pose[i] = pose[i] with { Orientation = Quaternion.Normalize(Quaternion.Inverse(parent) * orientation) };
        }
    }

    private static int Index(AnimationProject rig, string name)
    {
        var i = rig.Joints.FindIndex(j => j.Name == name);
        return i >= 0 ? i : throw new InvalidDataException("Rig is missing " + name);
    }
    private static float Radians(float degrees) => degrees * MathF.PI / 180;
    private static Quaternion Rotation(Vector3 degrees) =>
        Quaternion.CreateFromAxisAngle(Vector3.UnitZ, Radians(degrees.Y)) *
        Quaternion.CreateFromAxisAngle(Vector3.UnitY, Radians(degrees.Z)) *
        Quaternion.CreateFromAxisAngle(Vector3.UnitX, Radians(-degrees.X));

    private sealed record Pose(Vector3 Root, Vector3 Chest, Vector3 Hips, Vector3 LeftHand, Vector3 RightHand,
        Vector3 Blade, Vector3 LeftFoot, Vector3 RightFoot, Vector3 Shield)
    {
        public static Pose Lerp(Pose a, Pose b, float t) => new(
            Vector3.Lerp(a.Root, b.Root, t), Vector3.Lerp(a.Chest, b.Chest, t), Vector3.Lerp(a.Hips, b.Hips, t),
            Vector3.Lerp(a.LeftHand, b.LeftHand, t), Vector3.Lerp(a.RightHand, b.RightHand, t), Vector3.Lerp(a.Blade, b.Blade, t),
            Vector3.Lerp(a.LeftFoot, b.LeftFoot, t), Vector3.Lerp(a.RightFoot, b.RightFoot, t), Vector3.Lerp(a.Shield, b.Shield, t));
    }
}
